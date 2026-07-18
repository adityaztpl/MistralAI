using System.Text;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace GenAiExamples.SemanticKernel;

public sealed class RagService
{
    private readonly IVectorSearch _vectorSearch;
    private readonly IChatCompletionService _chatCompletion;
    private readonly Kernel _kernel;
    private readonly ILogger<RagService> _logger;

    public RagService(
        IVectorSearch vectorSearch,
        IChatCompletionService chatCompletion,
        Kernel kernel,
        ILogger<RagService> logger)
    {
        _vectorSearch = vectorSearch;
        _chatCompletion = chatCompletion;
        _kernel = kernel;
        _logger = logger;
    }

    public async Task<RagAnswer> AnswerAsync(
        string question,
        string tenantId,
        CancellationToken cancellationToken)
    {
        var chunks = await _vectorSearch.SearchAsync(
            query: question,
            tenantId: tenantId,
            limit: 5,
            cancellationToken: cancellationToken);

        if (chunks.Count == 0)
        {
            return new RagAnswer(
                "I do not have enough indexed context to answer that question.",
                Array.Empty<RetrievedChunk>());
        }

        var context = FormatContext(chunks);
        var history = new ChatHistory();
        history.AddSystemMessage(
            "You are a grounded documentation assistant. " +
            "Use only provided context. If context is insufficient, say so. " +
            "Cite sources with IDs like [doc-1].");
        history.AddUserMessage($"""
Question:
{question}

Context:
{context}
""");

        var response = await _chatCompletion.GetChatMessageContentAsync(
            history,
            kernel: _kernel,
            cancellationToken: cancellationToken);

        var answer = response.Content?.Trim();
        if (string.IsNullOrWhiteSpace(answer))
        {
            answer = "The model returned an empty answer.";
        }

        _logger.LogInformation(
            "RAG answer generated for tenant {TenantId} with {ChunkCount} chunks",
            tenantId,
            chunks.Count);

        return new RagAnswer(answer, chunks);
    }

    private static string FormatContext(IReadOnlyList<RetrievedChunk> chunks)
    {
        var builder = new StringBuilder();

        foreach (var chunk in chunks)
        {
            builder.AppendLine($"[{chunk.Id}] {chunk.Title}");
            builder.AppendLine($"URL: {chunk.Url}");
            builder.AppendLine($"Score: {chunk.Score:0.000}");
            builder.AppendLine(chunk.Content);
            builder.AppendLine();
        }

        return builder.ToString();
    }
}

public interface IVectorSearch
{
    Task<IReadOnlyList<RetrievedChunk>> SearchAsync(
        string query,
        string tenantId,
        int limit,
        CancellationToken cancellationToken);
}

public sealed record RetrievedChunk(
    string Id,
    string Title,
    string Url,
    string Content,
    double Score);

public sealed record RagAnswer(
    string Answer,
    IReadOnlyList<RetrievedChunk> Citations);

/// <summary>
/// Small in-memory implementation for demos and tests. Replace with pgvector,
/// Azure AI Search, Qdrant, Chroma, or another production vector store.
/// </summary>
public sealed class InMemoryVectorSearch : IVectorSearch
{
    private static readonly RetrievedChunk[] Chunks =
    {
        new(
            "doc-api-keys",
            "API Key Policy",
            "https://docs.example.com/security/api-keys",
            "API keys must be rotated every 90 days. Create a new key, deploy it, verify traffic, and revoke the old key.",
            0.0),
        new(
            "doc-rag-eval",
            "RAG Evaluation",
            "https://docs.example.com/ai/rag-evaluation",
            "Evaluate RAG with faithfulness, context precision, context recall, answer relevance, and citation accuracy.",
            0.0)
    };

    public Task<IReadOnlyList<RetrievedChunk>> SearchAsync(
        string query,
        string tenantId,
        int limit,
        CancellationToken cancellationToken)
    {
        var queryTerms = query
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(term => term.Trim('.', ',', '?', '!', ':', ';').ToLowerInvariant())
            .Where(term => term.Length > 1)
            .ToHashSet();

        var results = Chunks
            .Select(chunk =>
            {
                var contentTerms = chunk.Content
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(term => term.Trim('.', ',', '?', '!', ':', ';').ToLowerInvariant())
                    .ToHashSet();
                var score = queryTerms.Count(term => contentTerms.Contains(term));
                return chunk with { Score = score };
            })
            .Where(chunk => chunk.Score > 0)
            .OrderByDescending(chunk => chunk.Score)
            .Take(limit)
            .ToArray();

        return Task.FromResult<IReadOnlyList<RetrievedChunk>>(results);
    }
}

