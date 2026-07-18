# .NET GenAI with ASP.NET Core and Semantic Kernel

## Interview-ready summary

Semantic Kernel (SK) is Microsoft's orchestration SDK for integrating LLMs into .NET applications. It provides connectors to model providers, prompt functions, native plugins, planners, memory/vector integrations, and streaming APIs. For .NET teams, SK fits naturally into ASP.NET Core dependency injection, configuration, logging, authentication, and enterprise deployment practices.

---

## 1. ASP.NET Core GenAI architecture

```mermaid
flowchart LR
  UI[Angular/React SPA] --> API[ASP.NET Core API]
  API --> Auth[Identity / Entra ID / OAuth]
  API --> SK[Semantic Kernel service]
  SK --> LLM[Mistral/OpenAI/Azure OpenAI]
  SK --> Plugins[Native plugins/functions]
  SK --> RAG[RAG service]
  RAG --> VS[(Vector store)]
  RAG --> Docs[(Document storage)]
  API --> Logs[Observability]
```

### Responsibilities

| Layer | Responsibilities |
|---|---|
| SPA | Chat UI, streaming display, citations, feedback |
| API Controller | Auth, request validation, HTTP/SSE endpoints |
| Application service | Prompt assembly, orchestration, business rules |
| Semantic Kernel | Model connector, plugins, function invocation |
| RAG service | Retrieval, citation metadata, context formatting |
| Data layer | Vector store, source docs, chat history |

---

## 2. Calling LLMs from ASP.NET Core

### NuGet packages

Typical packages vary by provider, but a .NET SK app commonly uses:

```xml
<PackageReference Include="Microsoft.SemanticKernel" Version="*" />
<PackageReference Include="Microsoft.SemanticKernel.Connectors.OpenAI" Version="*" />
```

Use the latest stable versions for real projects.

### Dependency injection setup

```csharp
using Microsoft.SemanticKernel;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddKernel()
    .AddOpenAIChatCompletion(
        modelId: builder.Configuration["AI:ChatModel"]!,
        apiKey: builder.Configuration["AI:ApiKey"]!);

builder.Services.AddScoped<ChatService>();

var app = builder.Build();
app.MapControllers();
app.Run();
```

### Chat service

```csharp
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

public sealed class ChatService
{
    private readonly IChatCompletionService _chat;
    private readonly Kernel _kernel;

    public ChatService(IChatCompletionService chat, Kernel kernel)
    {
        _chat = chat;
        _kernel = kernel;
    }

    public async Task<string> AskAsync(string question, CancellationToken cancellationToken)
    {
        var history = new ChatHistory();
        history.AddSystemMessage("You are a concise technical assistant.");
        history.AddUserMessage(question);

        var response = await _chat.GetChatMessageContentAsync(
            history,
            kernel: _kernel,
            cancellationToken: cancellationToken);

        return response.Content ?? string.Empty;
    }
}
```

---

## 3. Semantic Kernel concepts

| Concept | Description |
|---|---|
| Kernel | DI-like container for AI services, plugins, and execution |
| Chat completion service | Abstraction over provider chat models |
| Prompt function | Prompt template exposed as callable function |
| Native function | C# method exposed to the model/kernel |
| Plugin | Collection of related functions |
| Kernel arguments | Named values passed into functions/prompts |
| Function invocation filters | Hooks for logging, auth, validation |

---

## 4. Plugins and functions

Native plugins expose C# methods.

```csharp
using System.ComponentModel;
using Microsoft.SemanticKernel;

public sealed class TicketPlugin
{
    [KernelFunction]
    [Description("Create a support ticket for a user issue.")]
    public string CreateTicket(
        [Description("Short issue title")] string title,
        [Description("Detailed issue description")] string description)
    {
        // In production, call an application service with auth and validation.
        var id = $"TICKET-{Random.Shared.Next(1000, 9999)}";
        return $"Created {id}: {title}";
    }
}
```

Register the plugin:

```csharp
builder.Services.AddKernel()
    .Plugins.AddFromType<TicketPlugin>("tickets");
```

Tool/function calling safety:

- Keep functions narrow.
- Validate arguments.
- Enforce authorization in application services.
- Require human approval for destructive actions.
- Log every function invocation.

---

## 5. Prompt functions

```csharp
var prompt = """
You are a support assistant.
Classify the issue into one of: billing, auth, outage, bug, other.

Issue:
{{$input}}
""";

var classify = kernel.CreateFunctionFromPrompt(prompt);
var result = await kernel.InvokeAsync(classify, new KernelArguments
{
    ["input"] = "Customer says they were charged twice."
});

Console.WriteLine(result.GetValue<string>());
```

Prompt function use cases:

- Classification
- Summarization
- Email drafting
- Query rewriting
- RAG answer generation

---

## 6. RAG with Semantic Kernel

Semantic Kernel can be used as the orchestration layer while your application owns retrieval.

```mermaid
sequenceDiagram
  participant Controller
  participant RagService
  participant VectorStore
  participant Kernel
  participant LLM
  Controller->>RagService: question + user context
  RagService->>VectorStore: retrieve authorized chunks
  VectorStore-->>RagService: chunks + metadata
  RagService->>Kernel: prompt with context
  Kernel->>LLM: chat completion
  LLM-->>Kernel: answer
  Kernel-->>RagService: answer
  RagService-->>Controller: answer + citations
```

### RAG service skeleton

```csharp
public sealed record RetrievedChunk(
    string Id,
    string Title,
    string Url,
    string Content,
    double Score);

public sealed record RagAnswer(
    string Answer,
    IReadOnlyList<RetrievedChunk> Citations);

public interface IVectorSearch
{
    Task<IReadOnlyList<RetrievedChunk>> SearchAsync(
        string query,
        string tenantId,
        CancellationToken cancellationToken);
}
```

Prompt:

```csharp
var prompt = """
You are a grounded documentation assistant.
Use only the context below.
If the context does not answer the question, say you do not know.
Include citation IDs like [doc-1].

Question:
{{$question}}

Context:
{{$context}}
""";
```

---

## 7. Streaming to Angular/React

Streaming improves perceived latency. ASP.NET Core can stream with:

- Server-Sent Events (SSE)
- WebSockets/SignalR
- Chunked HTTP responses

### SSE flow

```mermaid
sequenceDiagram
  participant Browser
  participant API
  participant SK
  participant LLM
  Browser->>API: POST /api/chat/stream
  API->>SK: GetStreamingChatMessageContentsAsync
  SK->>LLM: Streaming request
  loop token
    LLM-->>SK: delta
    SK-->>API: delta
    API-->>Browser: data: {"delta":"..."}
  end
  API-->>Browser: event: done
```

### Controller pattern

```csharp
[HttpPost("stream")]
public async Task Stream([FromBody] ChatRequest request, CancellationToken ct)
{
    Response.ContentType = "text/event-stream";

    await foreach (var chunk in _chat.StreamAsync(request.Message, ct))
    {
        await Response.WriteAsync($"data: {JsonSerializer.Serialize(new { delta = chunk })}\n\n", ct);
        await Response.Body.FlushAsync(ct);
    }

    await Response.WriteAsync("event: done\ndata: {}\n\n", ct);
}
```

### React SSE-ish fetch reader

```tsx
const response = await fetch("/api/chat/stream", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify({ message }),
});

const reader = response.body!.getReader();
const decoder = new TextDecoder();

while (true) {
  const { done, value } = await reader.read();
  if (done) break;
  const text = decoder.decode(value, { stream: true });
  appendToken(text);
}
```

---

## 8. SK vs LangChain for .NET shops

| Dimension | Semantic Kernel | LangChain |
|---|---|---|
| Primary ecosystem | .NET and Microsoft stack | Python/JS AI ecosystem |
| ASP.NET Core DI | Natural | Usually separate service |
| Enterprise .NET integration | Strong | Requires bridging |
| Breadth of AI integrations | Good, growing | Very broad |
| Agent graphs | SK planners/processes; evolving | LangGraph is mature for graph agents |
| Team fit | Best for C# teams | Best for Python-heavy AI teams |
| Deployment | Same API service possible | Often Python microservice |

Practical recommendation:

- Use SK when your core platform is ASP.NET Core and the team wants C# end-to-end.
- Use LangChain/LangGraph when AI workflows are complex, Python libraries are central, or data science teams own the stack.
- Use both when appropriate: ASP.NET Core API can call a Python LangGraph service.

---

## 9. Production considerations

### Security

- Store provider keys in Key Vault/Secrets Manager, not appsettings committed to Git.
- Validate request size.
- Redact PII before logging.
- Filter RAG retrieval by tenant/user ACLs.
- Add prompt injection tests.
- Use human approval for sensitive tool calls.

### Reliability

- Use cancellation tokens.
- Set provider timeouts.
- Retry transient failures with backoff.
- Add fallback model/provider where justified.
- Capture partial streaming errors gracefully.

### Cost and latency

- Track tokens per endpoint/user/tenant.
- Cache embeddings and stable prompt outputs.
- Keep retrieved context compact.
- Stream answers when possible.
- Use smaller models for classification/routing.

### Observability

- Correlate request ID across API, retrieval, and provider call.
- Log model, latency, token usage, prompt version, retrieved chunk IDs.
- Emit metrics for error rates and time to first token.

---

## 10. Interview questions

### Q: Why use Semantic Kernel in ASP.NET Core?

It integrates AI orchestration with .NET dependency injection, configuration, logging, and enterprise deployment practices. It lets C# teams expose plugins/functions and call chat models without running a separate Python service.

### Q: How would you stream LLM output to a frontend?

Use the model provider's streaming API through SK, expose an ASP.NET Core endpoint that writes SSE or WebSocket messages, flush each token/chunk, and support cancellation when the user stops generation.

### Q: How do plugins differ from normal services?

Plugins are normal C# functions described so the kernel/model can invoke them. They should still delegate to application services for authorization, validation, and side effects.

### Q: How would you build RAG in a .NET app?

Ingest documents, chunk and embed them, store vectors with metadata, retrieve authorized chunks for the user's query, format context into an SK prompt, generate an answer, validate citations, and return answer plus sources.

---

## 11. Semantic Kernel architecture in ASP.NET Core

For production, wrap SK behind application services instead of calling it directly from controllers.

```mermaid
flowchart TD
  C[Controller/Minimal API] --> A[Application service]
  A --> P[Prompt/version service]
  A --> K[Semantic Kernel]
  A --> R[RAG service]
  A --> G[Guardrail service]
  A --> O[Telemetry]
  K --> L[LLM provider]
  K --> Plugins[Plugins]
  Plugins --> Domain[Domain services]
```

### Recommended responsibilities

| Component | Responsibility |
|---|---|
| Controller | Auth, request DTO, cancellation token, HTTP response |
| Application service | Orchestration, budgets, validation, error handling |
| Kernel | Model connector and plugin invocation |
| Plugin | Thin adapter exposing safe functions |
| Domain service | Authorization, business rules, data access |
| RAG service | Retrieval, context packing, citations |
| Telemetry service | Traces, metrics, audit logs |

Avoid putting business rules only in plugin descriptions. Descriptions help the model choose tools; they do not enforce policy.

---

## 12. Configuration and provider abstraction

Use strongly typed options.

```csharp
public sealed class AiOptions
{
    public string ChatModel { get; init; } = "";
    public string EmbeddingModel { get; init; } = "";
    public int MaxInputTokens { get; init; } = 24000;
    public int MaxOutputTokens { get; init; } = 2000;
    public double Temperature { get; init; } = 0.2;
}
```

Register:

```csharp
builder.Services.Configure<AiOptions>(
    builder.Configuration.GetSection("AI"));
```

### Provider abstraction guideline

Keep app code focused on capabilities:

- chat completion
- streaming chat
- embeddings
- structured output
- tool/plugin invocation
- token/cost accounting

Provider-specific settings should be isolated in infrastructure code.

---

## 13. Plugin design in enterprise apps

Plugins should delegate to authorized services.

```csharp
public sealed class InvoicePlugin
{
    private readonly IInvoiceService _invoiceService;
    private readonly IUserContext _userContext;

    public InvoicePlugin(IInvoiceService invoiceService, IUserContext userContext)
    {
        _invoiceService = invoiceService;
        _userContext = userContext;
    }

    [KernelFunction]
    [Description("Read invoice payment status for an authorized customer invoice.")]
    public async Task<string> GetInvoiceStatusAsync(
        [Description("Invoice ID such as INV-1234")] string invoiceId,
        CancellationToken cancellationToken)
    {
        var invoice = await _invoiceService.GetAuthorizedInvoiceAsync(
            _userContext.UserId,
            invoiceId,
            cancellationToken);

        return $"Invoice {invoice.Id} is {invoice.Status} with balance {invoice.Balance:C}.";
    }
}
```

### Plugin safety checklist

- [ ] Function is narrow.
- [ ] Parameters are typed and described.
- [ ] Domain service enforces authZ.
- [ ] Side effects require explicit approval.
- [ ] Cancellation token is honored.
- [ ] Errors are sanitized.
- [ ] Invocation is logged with request ID.

---

## 14. RAG implementation details in .NET

### Context packing record

```csharp
public sealed record RagContextChunk(
    string ChunkId,
    string Title,
    string Url,
    string Text,
    double Score,
    IReadOnlyDictionary<string, string> Metadata);
```

### Prompt context formatter

```csharp
public static string FormatContext(IEnumerable<RagContextChunk> chunks)
{
    var builder = new StringBuilder();

    foreach (var chunk in chunks)
    {
        builder.AppendLine($"[{chunk.ChunkId}]");
        builder.AppendLine($"Title: {chunk.Title}");
        builder.AppendLine($"URL: {chunk.Url}");
        builder.AppendLine("Text:");
        builder.AppendLine(chunk.Text);
        builder.AppendLine();
    }

    return builder.ToString();
}
```

### Citation validation

```csharp
public static IReadOnlyList<string> FindUnknownCitations(
    string answer,
    IReadOnlySet<string> retrievedChunkIds)
{
    var matches = Regex.Matches(answer, @"\[([A-Za-z0-9_.:#-]+)\]");

    return matches
        .Select(match => match.Groups[1].Value)
        .Where(id => !retrievedChunkIds.Contains(id))
        .Distinct()
        .Order()
        .ToArray();
}
```

Validate before returning source cards as trusted citations.

---

## 15. Streaming implementation details

### Server-Sent Events with cancellation

```csharp
[HttpPost("stream")]
public async Task StreamAsync([FromBody] ChatRequest request, CancellationToken ct)
{
    Response.Headers.CacheControl = "no-cache";
    Response.Headers.Connection = "keep-alive";
    Response.ContentType = "text/event-stream";

    await foreach (var chunk in _chatService.StreamAsync(request.Message, ct))
    {
        var payload = JsonSerializer.Serialize(new { type = "delta", text = chunk });
        await Response.WriteAsync($"data: {payload}\n\n", ct);
        await Response.Body.FlushAsync(ct);
    }

    await Response.WriteAsync("event: done\ndata: {}\n\n", ct);
    await Response.Body.FlushAsync(ct);
}
```

### Streaming pitfalls

- Client disconnect not propagated to provider.
- Exceptions after partial output not represented in UI.
- Citations shown before validation.
- PII logged token-by-token.
- Reverse proxy buffering disables streaming.

For nginx/proxies/CDNs, verify buffering and idle timeout settings.

---

## 16. Observability with .NET

Use structured logs and OpenTelemetry-style spans.

```csharp
using var activity = _activitySource.StartActivity("ai.chat");
activity?.SetTag("ai.model", options.ChatModel);
activity?.SetTag("ai.prompt_version", promptVersion);
activity?.SetTag("tenant.id", tenantId);
```

Log per request:

- request ID / trace ID
- tenant/user hash
- prompt version
- model/provider
- token counts
- retrieval chunk IDs and scores
- plugin/function calls
- latency by stage
- validation failures
- user feedback

Redact:

- secrets
- access tokens
- raw PII
- sensitive document text unless explicitly sampled under policy

---

## 17. Cost controls in ASP.NET Core

### Middleware/service-level controls

- Per-user and per-tenant rate limits.
- Request size limits.
- Token budget validation.
- Output token caps.
- Cache stable responses.
- Use smaller models for classification.
- Queue heavy background tasks.

### Example budget record

```csharp
public sealed record AiBudget(
    int MaxHistoryTokens,
    int MaxRetrievedContextTokens,
    int MaxOutputTokens,
    decimal MaxEstimatedCostUsd);
```

Budget decisions should be visible in logs:

```text
request_id=abc prompt_tokens=8120 context_tokens=5400 max_output=1200 estimated_cost=0.0062
```

---

## 18. Testing strategy for .NET GenAI

### Unit tests

- Prompt builders.
- Context formatters.
- Citation validators.
- Plugin argument validation.
- Token budget logic.
- SSE event formatting.

### Integration tests

- RAG service with test vector store.
- Plugin authorization behavior.
- Streaming endpoint cancellation.
- Provider client wrapper with mocked responses.

### Evaluation tests

Run golden datasets separately from normal unit tests because they may call model providers and be slower/non-deterministic.

```mermaid
flowchart TD
  A[Unit tests] --> B[Integration tests]
  B --> C[Offline eval suite]
  C --> D[Canary telemetry]
```

---

## 19. .NET + Python hybrid pattern

Many enterprises use ASP.NET Core for product APIs and Python for advanced AI orchestration.

```mermaid
flowchart LR
  UI[SPA] --> API[ASP.NET Core API]
  API --> Auth[Auth/domain services]
  API --> AI[Python LangGraph service]
  AI --> LLM[Model provider]
  AI --> Vector[(Vector DB)]
  API --> Audit[(Audit logs)]
```

Use this when:

- LangGraph/Python ecosystem is needed.
- Data science team owns AI workflows.
- You want independent AI service deployment.

Keep:

- Auth decisions in the API/domain layer.
- Tenant scope passed explicitly.
- Audit logs correlated across services.
- DTO contracts versioned.

---

## 20. Additional interview questions

### Q: How should plugins access business data?

Through application/domain services that enforce authorization and business rules. Plugins should be thin adapters, not bypasses around the domain layer.

### Q: How do you test streaming?

Test event formatting, cancellation propagation, provider error handling, proxy buffering behavior, and UI handling of partial responses.

### Q: How do you prevent cross-tenant leakage in a .NET RAG app?

Resolve tenant/user scope in the API, apply filters in retrieval, include tenant/ACL in cache keys, validate citations are authorized, and redact logs/traces.

### Q: What belongs in middleware vs AI service?

Middleware handles HTTP-level concerns like auth, rate limits, request size, correlation IDs, and cancellation. The AI service handles prompt assembly, retrieval, model calls, validation, and eval hooks.

### Q: When would you split AI into a Python service?

When advanced Python AI libraries, LangGraph workflows, data-science ownership, or independent scaling outweigh the operational simplicity of staying entirely in .NET.

