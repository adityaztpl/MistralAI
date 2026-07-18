using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Npgsql;
using Pgvector;

public static class RagEndpoints
{
    public static IEndpointRouteBuilder MapRagEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api")
            .RequireAuthorization()
            .WithTags("RAG");

        group.MapPost("/ingest", async (
            IngestRequest request,
            ClaimsPrincipal user,
            RagService rag,
            CancellationToken ct) =>
        {
            var tenantId = GetTenantId(user);
            var result = await rag.IngestAsync(tenantId, request, ct);
            return Results.Created($"/api/documents?source={Uri.EscapeDataString(request.Source)}", result);
        });

        group.MapPost("/chat", async (
            ChatRequest request,
            ClaimsPrincipal user,
            RagService rag,
            CancellationToken ct) =>
        {
            var response = await rag.AnswerAsync(GetTenantId(user), request, ct);
            return Results.Ok(response);
        });

        group.MapPost("/chat/stream", async (
            HttpContext http,
            ChatRequest request,
            ClaimsPrincipal user,
            RagService rag,
            CancellationToken ct) =>
        {
            http.Response.Headers.CacheControl = "no-cache";
            http.Response.Headers.Connection = "keep-alive";
            http.Response.Headers.ContentType = "text/event-stream; charset=utf-8";

            await foreach (var item in rag.StreamAnswerAsync(GetTenantId(user), request, ct))
            {
                await http.Response.WriteAsync($"event: {item.Event}\n", ct);
                await http.Response.WriteAsync($"data: {JsonSerializer.Serialize(item.Data)}\n\n", ct);
                await http.Response.Body.FlushAsync(ct);
            }
        });

        return app;
    }

    private static string GetTenantId(ClaimsPrincipal user) =>
        user.FindFirst("tenant_id")?.Value ?? "demo";
}

public sealed record IngestRequest(
    string Source,
    string Title,
    string Content,
    Dictionary<string, object>? Metadata);

public sealed record IngestResponse(
    Guid BatchId,
    string Source,
    string Title,
    int ChunkCount,
    IReadOnlyList<Guid> DocumentIds);

public sealed record ChatRequest(
    string Message,
    string? ConversationId,
    int TopK = 6,
    Dictionary<string, string>? Filters = null);

public sealed record ChatResponse(
    string Answer,
    IReadOnlyList<Citation> Citations,
    string Model,
    int PromptContextCount);

public sealed record Citation(
    Guid Id,
    string Source,
    string Title,
    int ChunkIndex,
    double Distance,
    string Excerpt,
    Dictionary<string, object> Metadata);

public sealed record SseEnvelope(string Event, object Data);

public sealed class RagService(
    RagOptions options,
    RagRepository repository,
    OpenAiCompatibleClient llm,
    ILogger<RagService> logger)
{
    public async Task<IngestResponse> IngestAsync(string tenantId, IngestRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Source))
        {
            throw new BadHttpRequestException("source is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new BadHttpRequestException("title is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Content) || request.Content.Length < 20)
        {
            throw new BadHttpRequestException("content must contain at least 20 characters.");
        }

        var chunks = TextChunker.Split(request.Content, maxChars: 1_500, overlapChars: 180).ToList();
        if (chunks.Count == 0)
        {
            throw new BadHttpRequestException("content did not produce any chunks.");
        }

        var batchId = await repository.CreateIngestionBatchAsync(
            tenantId,
            request.Source,
            request.Title,
            chunks.Count,
            ct);

        var ids = new List<Guid>(chunks.Count);
        for (var i = 0; i < chunks.Count; i++)
        {
            var embedding = await llm.CreateEmbeddingAsync(options.EmbeddingModel, chunks[i], ct);
            ValidateEmbedding(embedding);

            var documentId = await repository.InsertDocumentAsync(
                tenantId,
                request.Source,
                request.Title,
                chunkIndex: i,
                content: chunks[i],
                tokenEstimate: EstimateTokens(chunks[i]),
                metadata: request.Metadata ?? new Dictionary<string, object>(),
                embedding,
                ct);

            ids.Add(documentId);
        }

        logger.LogInformation(
            "Ingested {ChunkCount} chunks for source {Source} in tenant {TenantId}",
            ids.Count,
            request.Source,
            tenantId);

        return new IngestResponse(batchId, request.Source, request.Title, ids.Count, ids);
    }

    public async Task<ChatResponse> AnswerAsync(string tenantId, ChatRequest request, CancellationToken ct)
    {
        var (citations, messages) = await BuildGroundedMessagesAsync(tenantId, request, ct);
        var answer = await llm.CreateChatCompletionAsync(options.ChatModel, messages, stream: false, ct);
        return new ChatResponse(answer, citations, options.ChatModel, citations.Count);
    }

    public async IAsyncEnumerable<SseEnvelope> StreamAnswerAsync(
        string tenantId,
        ChatRequest request,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var (citations, messages) = await BuildGroundedMessagesAsync(tenantId, request, ct);
        yield return new SseEnvelope("citations", citations);

        await foreach (var token in llm.StreamChatCompletionAsync(options.ChatModel, messages, ct))
        {
            yield return new SseEnvelope("token", new { text = token });
        }

        yield return new SseEnvelope("done", new { model = options.ChatModel, citationCount = citations.Count });
    }

    private async Task<(IReadOnlyList<Citation> Citations, IReadOnlyList<ChatMessage> Messages)> BuildGroundedMessagesAsync(
        string tenantId,
        ChatRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            throw new BadHttpRequestException("message is required.");
        }

        var questionEmbedding = await llm.CreateEmbeddingAsync(options.EmbeddingModel, request.Message, ct);
        ValidateEmbedding(questionEmbedding);

        var citations = await repository.SearchAsync(
            tenantId,
            questionEmbedding,
            Math.Clamp(request.TopK, 1, 12),
            request.Filters ?? new Dictionary<string, string>(),
            ct);

        var context = PromptBuilder.BuildContext(citations);
        var messages = new[]
        {
            new ChatMessage("system", """
                You are a retrieval-augmented assistant.
                Answer using only the supplied context.
                If the answer is not in the context, say what information is missing.
                Cite every factual claim with bracketed citation numbers like [1] or [2].
                Keep answers concise, specific, and useful for an engineer.
                """),
            new ChatMessage("user", $"""
                Context:
                {context}

                User question:
                {request.Message}
                """)
        };

        return (citations, messages);
    }

    private void ValidateEmbedding(IReadOnlyList<float> embedding)
    {
        if (embedding.Count != options.EmbeddingDimensions)
        {
            throw new InvalidOperationException(
                $"Expected embedding dimension {options.EmbeddingDimensions}, got {embedding.Count}.");
        }
    }

    private static int EstimateTokens(string text) => Math.Max(1, text.Length / 4);
}

public sealed class RagRepository(NpgsqlDataSource dataSource)
{
    public async Task<Guid> CreateIngestionBatchAsync(
        string tenantId,
        string source,
        string title,
        int chunkCount,
        CancellationToken ct)
    {
        await using var command = dataSource.CreateCommand("""
            insert into ingestion_batches (tenant_id, source, title, chunk_count, status)
            values (@tenant_id, @source, @title, @chunk_count, 'completed')
            returning id;
            """);
        command.Parameters.AddWithValue("tenant_id", tenantId);
        command.Parameters.AddWithValue("source", source);
        command.Parameters.AddWithValue("title", title);
        command.Parameters.AddWithValue("chunk_count", chunkCount);
        return (Guid)(await command.ExecuteScalarAsync(ct))!;
    }

    public async Task<Guid> InsertDocumentAsync(
        string tenantId,
        string source,
        string title,
        int chunkIndex,
        string content,
        int tokenEstimate,
        Dictionary<string, object> metadata,
        IReadOnlyList<float> embedding,
        CancellationToken ct)
    {
        await using var command = dataSource.CreateCommand("""
            insert into documents
                (tenant_id, source, title, chunk_index, content, token_estimate, metadata, embedding)
            values
                (@tenant_id, @source, @title, @chunk_index, @content, @token_estimate, cast(@metadata as jsonb), @embedding)
            returning id;
            """);

        command.Parameters.AddWithValue("tenant_id", tenantId);
        command.Parameters.AddWithValue("source", source);
        command.Parameters.AddWithValue("title", title);
        command.Parameters.AddWithValue("chunk_index", chunkIndex);
        command.Parameters.AddWithValue("content", content);
        command.Parameters.AddWithValue("token_estimate", tokenEstimate);
        command.Parameters.AddWithValue("metadata", JsonSerializer.Serialize(metadata));
        command.Parameters.AddWithValue("embedding", new Vector(embedding.ToArray()));

        return (Guid)(await command.ExecuteScalarAsync(ct))!;
    }

    public async Task<IReadOnlyList<Citation>> SearchAsync(
        string tenantId,
        IReadOnlyList<float> embedding,
        int topK,
        Dictionary<string, string> filters,
        CancellationToken ct)
    {
        var sql = new StringBuilder("""
            select id, source, title, chunk_index, content, metadata::text, embedding <=> @embedding as distance
            from documents
            where tenant_id = @tenant_id
              and deleted_at is null
            """);

        await using var command = dataSource.CreateCommand();
        command.Parameters.AddWithValue("tenant_id", tenantId);
        command.Parameters.AddWithValue("embedding", new Vector(embedding.ToArray()));
        command.Parameters.AddWithValue("limit", topK);

        var filterIndex = 0;
        foreach (var (key, value) in filters)
        {
            var keyParam = $"filter_key_{filterIndex}";
            var valueParam = $"filter_value_{filterIndex}";
            sql.AppendLine($"and metadata ->> @{keyParam} = @{valueParam}");
            command.Parameters.AddWithValue(keyParam, key);
            command.Parameters.AddWithValue(valueParam, value);
            filterIndex++;
        }

        sql.AppendLine("order by embedding <=> @embedding");
        sql.AppendLine("limit @limit;");
        command.CommandText = sql.ToString();

        var results = new List<Citation>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var content = reader.GetString(4);
            var metadataJson = reader.GetString(5);
            results.Add(new Citation(
                Id: reader.GetGuid(0),
                Source: reader.GetString(1),
                Title: reader.GetString(2),
                ChunkIndex: reader.GetInt32(3),
                Distance: reader.GetDouble(6),
                Excerpt: content.Length <= 320 ? content : content[..320] + "...",
                Metadata: JsonSerializer.Deserialize<Dictionary<string, object>>(metadataJson) ?? new()));
        }

        return results;
    }
}

public sealed class OpenAiCompatibleClient(HttpClient http, ILogger<OpenAiCompatibleClient> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<float>> CreateEmbeddingAsync(string model, string input, CancellationToken ct)
    {
        using var response = await http.PostAsJsonAsync("embeddings", new
        {
            model,
            input
        }, JsonOptions, ct);

        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Embedding request failed: {(int)response.StatusCode} {body}");
        }

        using var document = JsonDocument.Parse(body);
        return document.RootElement
            .GetProperty("data")[0]
            .GetProperty("embedding")
            .EnumerateArray()
            .Select(value => value.GetSingle())
            .ToArray();
    }

    public async Task<string> CreateChatCompletionAsync(
        string model,
        IReadOnlyList<ChatMessage> messages,
        bool stream,
        CancellationToken ct)
    {
        using var response = await http.PostAsJsonAsync("chat/completions", new
        {
            model,
            messages,
            stream,
            temperature = 0.2
        }, JsonOptions, ct);

        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Chat request failed: {(int)response.StatusCode} {body}");
        }

        using var document = JsonDocument.Parse(body);
        return document.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;
    }

    public async IAsyncEnumerable<string> StreamChatCompletionAsync(
        string model,
        IReadOnlyList<ChatMessage> messages,
        [EnumeratorCancellation] CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
        {
            Content = JsonContent.Create(new
            {
                model,
                messages,
                stream = true,
                temperature = 0.2
            }, options: JsonOptions)
        };

        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Streaming chat request failed: {(int)response.StatusCode} {error}");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);
        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var payload = line["data:".Length..].Trim();
            if (payload == "[DONE]")
            {
                yield break;
            }

            string? token = null;
            try
            {
                using var document = JsonDocument.Parse(payload);
                token = document.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("delta")
                    .TryGetProperty("content", out var content)
                        ? content.GetString()
                        : null;
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "Skipping malformed streaming payload.");
            }

            if (!string.IsNullOrEmpty(token))
            {
                yield return token;
            }
        }
    }
}

public sealed record ChatMessage(string Role, string Content);

public static class TextChunker
{
    public static IEnumerable<string> Split(string text, int maxChars, int overlapChars)
    {
        var normalized = text.Replace("\r\n", "\n").Trim();
        if (normalized.Length <= maxChars)
        {
            yield return normalized;
            yield break;
        }

        var start = 0;
        while (start < normalized.Length)
        {
            var remaining = normalized.Length - start;
            var take = Math.Min(maxChars, remaining);
            var end = start + take;

            if (end < normalized.Length)
            {
                var paragraphBreak = normalized.LastIndexOf("\n\n", end, take, StringComparison.Ordinal);
                var sentenceBreak = normalized.LastIndexOf(". ", end, take, StringComparison.Ordinal);
                var bestBreak = Math.Max(paragraphBreak, sentenceBreak);
                if (bestBreak > start + maxChars / 2)
                {
                    end = bestBreak + 1;
                }
            }

            yield return normalized[start..end].Trim();
            if (end >= normalized.Length)
            {
                yield break;
            }

            start = Math.Max(0, end - overlapChars);
        }
    }
}

public static class PromptBuilder
{
    public static string BuildContext(IReadOnlyList<Citation> citations)
    {
        if (citations.Count == 0)
        {
            return "No relevant context was found.";
        }

        var builder = new StringBuilder();
        for (var i = 0; i < citations.Count; i++)
        {
            var citation = citations[i];
            builder.AppendLine($"[{i + 1}] Source={citation.Source}; Title={citation.Title}; Chunk={citation.ChunkIndex}; Distance={citation.Distance:0.0000}");
            builder.AppendLine(citation.Excerpt);
            builder.AppendLine();
        }

        return builder.ToString();
    }
}

