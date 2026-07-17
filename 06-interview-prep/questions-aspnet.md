# ASP.NET Core Interview Questions

## How to use this file

For each question, practice:

1. A concise 30-second answer.
2. A deeper implementation explanation.
3. A production trade-off.
4. A GenAI/fullstack connection when relevant.

---

## 1. ASP.NET Core fundamentals

### Q1: What happens when an HTTP request enters an ASP.NET Core app?

**Strong answer:** The request flows through the middleware pipeline in registration order. Middleware can inspect, modify, short-circuit, or pass the request to the next middleware. Routing selects an endpoint, model binding creates action parameters, filters can run around actions, the controller/minimal API executes, and a response is written.

```text
Kestrel -> Middleware -> Routing -> Auth -> Endpoint -> Action -> Response
```

**Talking points:**

- Middleware order matters.
- Authentication should run before authorization.
- Exception handling should be early.
- Endpoint handlers should stay thin.

**Red flags:**

- Saying controllers receive requests directly.
- Not understanding middleware order.

---

### Q2: Explain dependency injection lifetimes.

| Lifetime | Created | Use for | Avoid |
|---|---|---|---|
| Singleton | Once per app | Stateless services, caches, config | Capturing scoped services |
| Scoped | Once per request scope | DbContext, unit-of-work services | Background singleton use |
| Transient | Every resolution | Lightweight stateless objects | Expensive clients |

**Interview example:**

```csharp
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddTransient<IEmailTemplateRenderer, EmailTemplateRenderer>();
```

**GenAI angle:** LLM provider clients are often singleton or typed `HttpClient` services, while chat orchestration may be scoped because it depends on request user context.

---

### Q3: What is middleware and when would you write custom middleware?

Middleware is code that runs in the request pipeline.

Use custom middleware for:

- Correlation IDs
- Request logging
- Tenant resolution
- Global exception handling
- Rate limiting headers
- Security headers

```csharp
public sealed class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var id = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                 ?? Guid.NewGuid().ToString("N");
        context.Response.Headers["X-Correlation-ID"] = id;
        using var scope = context.RequestServices
            .GetRequiredService<ILogger<CorrelationIdMiddleware>>()
            .BeginScope(new Dictionary<string, object> { ["CorrelationId"] = id });
        await _next(context);
    }
}
```

---

### Q4: Controllers vs Minimal APIs?

**Controllers** are good for larger APIs with filters, versioning, conventions, and complex binding.

**Minimal APIs** are good for small services, lightweight endpoints, and high signal-to-noise routing.

**Balanced answer:** Both use the same ASP.NET Core infrastructure. Choose based on team conventions and complexity rather than performance mythology.

---

### Q5: How does model binding and validation work?

ASP.NET Core binds route, query, header, and body values to action parameters or DTOs. Data annotations and custom validators populate `ModelState`.

```csharp
public sealed record ChatRequest(
    [property: Required, MinLength(1), MaxLength(8000)] string Message);

[HttpPost]
public IActionResult Chat(ChatRequest request)
{
    if (!ModelState.IsValid) return ValidationProblem(ModelState);
    return Ok();
}
```

For `[ApiController]`, invalid model state automatically returns 400 unless customized.

---

## 2. Web APIs and data access

### Q6: How do you design a RESTful endpoint?

Use nouns for resources, HTTP methods for actions, status codes consistently, idempotency where appropriate, pagination for collections, and structured errors.

```text
GET    /api/documents
POST   /api/documents
GET    /api/documents/{id}
DELETE /api/documents/{id}
POST   /api/chat/rag-query
```

GenAI note: Chat operations are often commands rather than pure REST resources, so `POST /api/chat` is acceptable.

---

### Q7: How do you handle errors in ASP.NET Core?

- Global exception middleware.
- Problem Details responses.
- Domain-specific exceptions mapped to status codes.
- Logging with correlation IDs.
- Do not leak stack traces in production.

```csharp
builder.Services.AddProblemDetails();
app.UseExceptionHandler();
```

---

### Q8: What is `HttpClientFactory` and why use it?

`IHttpClientFactory` manages `HttpClient` creation, handler lifetimes, DNS refresh, logging, resilience policies, and typed clients.

```csharp
builder.Services.AddHttpClient<IMistralClient, MistralClient>(client =>
{
    client.BaseAddress = new Uri("https://api.mistral.ai/");
    client.Timeout = TimeSpan.FromSeconds(60);
});
```

Use it for LLM providers and tool APIs.

---

### Q9: How should cancellation be handled?

Accept `CancellationToken` in controllers and pass it through all async calls.

```csharp
public async Task<IActionResult> Ask(ChatRequest request, CancellationToken ct)
{
    var answer = await _chatService.AskAsync(request.Message, ct);
    return Ok(answer);
}
```

Important for streaming: user stop/cancel should cancel provider requests and retrieval work.

---

### Q10: EF Core tracking vs no-tracking?

Tracking queries monitor entity changes for updates. No-tracking queries are faster and use less memory for read-only scenarios.

```csharp
var docs = await db.Documents
    .AsNoTracking()
    .Where(d => d.TenantId == tenantId)
    .ToListAsync(ct);
```

---

## 3. Authentication and authorization

### Q11: Authentication vs authorization?

Authentication proves who the user is. Authorization decides what the user can access or do.

```text
Authentication: "This is Alice"
Authorization: "Alice can query tenant A documents"
```

---

### Q12: How do JWT bearer tokens work in ASP.NET Core?

The API validates issuer, audience, signature, and expiry, then creates a `ClaimsPrincipal`.

```csharp
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = "https://login.example.com";
        options.Audience = "api://genai-api";
    });

app.UseAuthentication();
app.UseAuthorization();
```

---

### Q13: How would you implement tenant isolation?

- Resolve tenant from token claims or trusted route.
- Include tenant ID in every query.
- Use DB constraints/indexes.
- Filter vector retrieval by tenant before prompt assembly.
- Add tests for cross-tenant access.

---

## 4. Performance and reliability

### Q14: How do you improve API performance?

- Async I/O.
- Pagination.
- Caching.
- Compression.
- Efficient DB indexes.
- Avoid N+1 queries.
- Stream long responses.
- Measure before optimizing.

---

### Q15: How do you add rate limiting?

Use built-in rate limiting middleware or gateway-level controls.

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("chat", limiter =>
    {
        limiter.PermitLimit = 20;
        limiter.Window = TimeSpan.FromMinutes(1);
    });
});

app.UseRateLimiter();
```

GenAI angle: rate limits protect provider cost and prevent abuse.

---

### Q16: How do you handle background jobs?

Options:

- `BackgroundService`
- Queues with hosted workers
- Hangfire/Quartz
- Cloud queues/functions

For document ingestion, prefer a queue-backed worker so uploads return quickly and parsing/embedding can retry.

---

## 5. GenAI-specific ASP.NET Core questions

### Q17: How would you stream LLM tokens from ASP.NET Core?

Use provider streaming API, write SSE or WebSocket messages, flush each chunk, and pass cancellation tokens.

Key concerns:

- Response buffering disabled.
- Reverse proxy timeout configured.
- Error events after partial output.
- Client stop cancels backend work.

---

### Q18: Where should prompt templates live?

Options:

- Versioned code files for simple apps.
- Database/prompt registry for non-developer iteration.
- Hybrid: versioned templates plus feature flags.

Always log prompt version with each request.

---

### Q19: How do you secure AI tool calling?

- The model proposes tool calls; the backend executes them.
- Validate tool arguments.
- Enforce user authorization.
- Use idempotency keys.
- Require human approval for destructive actions.
- Audit tool calls.

---

### Q20: What would you log for a RAG chat request?

- Request ID, tenant ID, user ID hash.
- Model and prompt version.
- Token counts and latency.
- Retrieved chunk IDs and scores.
- Tool calls.
- Citation validation result.
- User feedback.

