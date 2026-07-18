// Minimal .NET 8 Mistral client using raw HttpClient.
//
// Run inside a console project:
//   dotnet new console -n MistralDemo
//   cp 08-mistral/examples/dotnet_mistral_client.cs MistralDemo/Program.cs
//   cd MistralDemo
//   export MISTRAL_API_KEY="..."
//   dotnet run

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;

var client = MistralClient.FromEnvironment();

var answer = await client.ChatAsync(new[]
{
    new ChatMessage("system", "You are a concise senior engineer."),
    new ChatMessage("user", "Explain why tool calls must be authorized by application code.")
});

Console.WriteLine(answer);

Console.WriteLine("\nStreaming:");
await foreach (var token in client.StreamChatAsync(new[]
{
    new ChatMessage("system", "Answer in three short bullets."),
    new ChatMessage("user", "What makes a good RAG citation?")
}))
{
    Console.Write(token);
}
Console.WriteLine();

var embedding = await client.CreateEmbeddingAsync("RAG systems retrieve context before generation.");
Console.WriteLine($"\nEmbedding dimensions: {embedding.Count}");

public sealed record ChatMessage(string Role, string Content);

public sealed class MistralClient(HttpClient http, string chatModel, string embeddingModel)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static MistralClient FromEnvironment()
    {
        var apiKey = Environment.GetEnvironmentVariable("MISTRAL_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Set MISTRAL_API_KEY before running this example.");
        }

        var baseUrl = Environment.GetEnvironmentVariable("MISTRAL_BASE_URL") ?? "https://api.mistral.ai/v1";
        var chatModel = Environment.GetEnvironmentVariable("MISTRAL_CHAT_MODEL") ?? "mistral-small-latest";
        var embeddingModel = Environment.GetEnvironmentVariable("MISTRAL_EMBEDDING_MODEL") ?? "mistral-embed";

        var http = new HttpClient
        {
            BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(90)
        };
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        return new MistralClient(http, chatModel, embeddingModel);
    }

    public async Task<string> ChatAsync(IReadOnlyList<ChatMessage> messages, CancellationToken ct = default)
    {
        using var response = await http.PostAsJsonAsync("chat/completions", new
        {
            model = chatModel,
            messages,
            temperature = 0.2,
            max_tokens = 600
        }, JsonOptions, ct);

        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Chat failed: {(int)response.StatusCode} {body}");
        }

        using var document = JsonDocument.Parse(body);
        return document.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;
    }

    public async IAsyncEnumerable<string> StreamChatAsync(
        IReadOnlyList<ChatMessage> messages,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
        {
            Content = JsonContent.Create(new
            {
                model = chatModel,
                messages,
                temperature = 0.2,
                max_tokens = 600,
                stream = true
            }, options: JsonOptions)
        };

        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        var error = response.IsSuccessStatusCode ? null : await response.Content.ReadAsStringAsync(ct);
        if (error is not null)
        {
            throw new InvalidOperationException($"Stream failed: {(int)response.StatusCode} {error}");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);
        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var data = line["data:".Length..].Trim();
            if (data == "[DONE]")
            {
                yield break;
            }

            using var document = JsonDocument.Parse(data);
            var delta = document.RootElement.GetProperty("choices")[0].GetProperty("delta");
            if (delta.TryGetProperty("content", out var content))
            {
                var token = content.GetString();
                if (!string.IsNullOrEmpty(token))
                {
                    yield return token;
                }
            }
        }
    }

    public async Task<IReadOnlyList<float>> CreateEmbeddingAsync(string text, CancellationToken ct = default)
    {
        using var response = await http.PostAsJsonAsync("embeddings", new
        {
            model = embeddingModel,
            input = new[] { text }
        }, JsonOptions, ct);

        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Embedding failed: {(int)response.StatusCode} {body}");
        }

        using var document = JsonDocument.Parse(body);
        return document.RootElement
            .GetProperty("data")[0]
            .GetProperty("embedding")
            .EnumerateArray()
            .Select(value => value.GetSingle())
            .ToArray();
    }
}

