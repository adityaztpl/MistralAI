# ASP.NET Core Advanced

This guide focuses on production design and senior-level interview topics: architecture, CQRS, performance, resiliency, distributed systems, security, testing, and observability.

## 1. Clean Architecture

Clean Architecture organizes code so business rules do not depend on infrastructure details such as databases, web frameworks, or external services.

### Common project layout

```text
Catalog.Api/
  Program.cs
  Controllers/
  Middleware/

Catalog.Application/
  Products/
    CreateProductCommand.cs
    ProductResponse.cs
  Abstractions/
    IAppDbContext.cs
    ICurrentUser.cs

Catalog.Domain/
  Products/
    Product.cs
    ProductCreatedDomainEvent.cs

Catalog.Infrastructure/
  Persistence/
    AppDbContext.cs
  Auth/
  Caching/
  ExternalServices/
```

### Dependency direction

```text
Api --------------> Application <-------------- Infrastructure
                       |
                       v
                    Domain
```

The API references Application. Infrastructure references Application to implement interfaces. Domain should be the most independent project.

### Interview talking points

- Dependencies point inward toward business rules.
- Infrastructure implements interfaces defined by Application.
- Clean Architecture improves testability and change isolation, but it can be overkill for very small APIs.
- Avoid turning architecture into ceremony; vertical slices can keep features cohesive.

## 2. CQRS and MediatR

CQRS separates commands that change state from queries that read state. MediatR helps dispatch request objects to handlers.

### Command

```csharp
public sealed record CreateProductCommand(
    string Name,
    decimal Price,
    int CategoryId) : IRequest<ProductResponse>;
```

### Handler

```csharp
public sealed class CreateProductCommandHandler
    : IRequestHandler<CreateProductCommand, ProductResponse>
{
    private readonly AppDbContext _db;

    public CreateProductCommandHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ProductResponse> Handle(
        CreateProductCommand request,
        CancellationToken cancellationToken)
    {
        var product = new Product
        {
            Name = request.Name,
            Price = request.Price,
            CategoryId = request.CategoryId
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync(cancellationToken);

        return new ProductResponse(product.Id, product.Name, product.Price);
    }
}
```

### Endpoint

```csharp
app.MapPost("/api/products", async (
    CreateProductCommand command,
    ISender sender,
    CancellationToken cancellationToken) =>
{
    var response = await sender.Send(command, cancellationToken);
    return Results.Created($"/api/products/{response.Id}", response);
});
```

### Pipeline behaviors

MediatR pipeline behaviors are useful for validation, logging, metrics, transactions, and authorization.

```csharp
public sealed class LoggingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling {RequestName}", typeof(TRequest).Name);
        var response = await next();
        _logger.LogInformation("Handled {RequestName}", typeof(TRequest).Name);
        return response;
    }
}
```

### Interview talking points

- Commands should express intent: `CreateOrder`, not `SaveOrder`.
- Queries should avoid tracking and return read models/DTOs.
- CQRS can be simple in one database or advanced with separate read/write stores.
- MediatR is optional; do not use it if direct service calls are clearer.

## 3. Performance

Performance work starts with measurement. For APIs, common bottlenecks are database queries, serialization, network calls, and excessive allocations.

### EF Core `AsNoTracking`

Use `AsNoTracking` for read-only queries because EF does not need change tracking.

```csharp
var products = await db.Products
    .AsNoTracking()
    .Where(product => product.IsActive)
    .Select(product => new ProductResponse(product.Id, product.Name, product.Price))
    .ToListAsync(cancellationToken);
```

### Projection instead of loading entities

```csharp
var page = await db.Products
    .AsNoTracking()
    .OrderBy(product => product.Id)
    .Skip((request.Page - 1) * request.PageSize)
    .Take(request.PageSize)
    .Select(product => new ProductListItem(
        product.Id,
        product.Name,
        product.Price))
    .ToListAsync(cancellationToken);
```

### Compiled queries

Compiled queries reduce repeated query translation overhead for hot paths.

```csharp
private static readonly Func<AppDbContext, int, Task<ProductResponse?>> GetProductById =
    EF.CompileAsyncQuery((AppDbContext db, int id) =>
        db.Products
            .AsNoTracking()
            .Where(product => product.Id == id)
            .Select(product => new ProductResponse(product.Id, product.Name, product.Price))
            .FirstOrDefault());
```

### Pagination

```csharp
public sealed record PageRequest(int Page = 1, int PageSize = 25);

public static IQueryable<T> Page<T>(this IQueryable<T> query, PageRequest request)
{
    var page = Math.Max(request.Page, 1);
    var pageSize = Math.Clamp(request.PageSize, 1, 100);
    return query.Skip((page - 1) * pageSize).Take(pageSize);
}
```

### Interview talking points

- Avoid returning unbounded lists.
- Use indexes aligned with query filters and sort order.
- Avoid N+1 queries by projecting or using `Include` intentionally.
- Use `AsNoTracking` for reads and tracking for updates.
- Benchmark and profile before optimizing deeply.

## 4. Rate limiting

Rate limiting protects APIs from accidental overload, abuse, and noisy clients. ASP.NET Core includes built-in rate limiting middleware.

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("Api", limiter =>
    {
        limiter.PermitLimit = 100;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

app.UseRateLimiter();

app.MapGet("/api/products", GetProducts)
    .RequireRateLimiting("Api");
```

### Algorithms

- **Fixed window**: simple counter per time window; can burst at window boundaries.
- **Sliding window**: smoother than fixed window.
- **Token bucket**: permits bursts while enforcing average rate.
- **Concurrency limiter**: limits simultaneous work.

### Interview talking points

- Return `429 Too Many Requests` and consider `Retry-After`.
- In distributed systems, in-memory rate limiting is per instance; use a shared store or gateway for global limits.
- Apply stricter limits to expensive or unauthenticated endpoints.

## 5. Health checks

Health checks report whether the service and dependencies are ready.

```csharp
builder.Services.AddHealthChecks()
    .AddSqlServer(builder.Configuration.GetConnectionString("Default")!)
    .AddRedis(builder.Configuration.GetConnectionString("Redis")!);

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
```

Custom check:

```csharp
public sealed class SearchHealthCheck : IHealthCheck
{
    private readonly ISearchClient _client;

    public SearchHealthCheck(ISearchClient client)
    {
        _client = client;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        return await _client.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("Search is unavailable.");
    }
}
```

### Interview talking points

- Liveness asks "should the process be restarted?"
- Readiness asks "can this instance receive traffic?"
- Do not make health checks so expensive that they become a source of load.

## 6. gRPC

gRPC is a high-performance RPC framework using Protocol Buffers and HTTP/2. It is useful for internal service-to-service communication when strong contracts and low latency matter.

### `.proto`

```proto
syntax = "proto3";

option csharp_namespace = "Catalog.Grpc";

service ProductCatalog {
  rpc GetProduct (GetProductRequest) returns (ProductReply);
}

message GetProductRequest {
  int32 id = 1;
}

message ProductReply {
  int32 id = 1;
  string name = 2;
  double price = 3;
}
```

### Service implementation

```csharp
public sealed class ProductCatalogService : ProductCatalog.ProductCatalogBase
{
    private readonly AppDbContext _db;

    public ProductCatalogService(AppDbContext db)
    {
        _db = db;
    }

    public override async Task<ProductReply> GetProduct(
        GetProductRequest request,
        ServerCallContext context)
    {
        var product = await _db.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(product => product.Id == request.Id, context.CancellationToken);

        if (product is null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Product not found."));
        }

        return new ProductReply
        {
            Id = product.Id,
            Name = product.Name,
            Price = Decimal.ToDouble(product.Price)
        };
    }
}
```

### Interview talking points

- gRPC is contract-first and efficient, but less browser-friendly than JSON REST.
- It requires HTTP/2 for normal gRPC.
- Use REST for public/browser APIs and gRPC for internal high-performance service calls when appropriate.

## 7. Polly resilience

Polly provides resilience policies such as retries, timeouts, circuit breakers, and hedging. In modern .NET, it integrates with `HttpClientFactory` through resilience handlers.

```csharp
builder.Services.AddHttpClient<IInventoryClient, InventoryClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Inventory:BaseUrl"]!);
})
.AddStandardResilienceHandler(options =>
{
    options.Retry.MaxRetryAttempts = 3;
    options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(10);
    options.CircuitBreaker.FailureRatio = 0.5;
});
```

Client:

```csharp
public sealed class InventoryClient : IInventoryClient
{
    private readonly HttpClient _httpClient;

    public InventoryClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> IsInStockAsync(int productId, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync($"/api/inventory/{productId}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<bool>(cancellationToken);
    }
}
```

### Interview talking points

- Retry only transient failures and only when the operation is safe to retry.
- Combine retries with timeouts to avoid request pileups.
- Circuit breakers protect downstream services and your thread pool.
- Use idempotency keys for retried write operations.

## 8. Distributed caching with Redis

Distributed cache is shared across app instances. Redis is a common backing store.

```csharp
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "Catalog:";
});
```

Usage:

```csharp
public sealed class ProductCache
{
    private readonly IDistributedCache _cache;

    public ProductCache(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<ProductResponse?> GetAsync(int id, CancellationToken cancellationToken)
    {
        var json = await _cache.GetStringAsync($"products:{id}", cancellationToken);
        return json is null
            ? null
            : JsonSerializer.Deserialize<ProductResponse>(json);
    }

    public async Task SetAsync(ProductResponse product, CancellationToken cancellationToken)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        };

        await _cache.SetStringAsync(
            $"products:{product.Id}",
            JsonSerializer.Serialize(product),
            options,
            cancellationToken);
    }
}
```

### Interview talking points

- Redis enables shared cache state across multiple app instances.
- Cache keys need naming conventions and tenant awareness.
- Cache aside is common: check cache, load database on miss, then set cache.
- Plan invalidation around writes and data ownership.

## 9. Multi-tenancy basics

Multi-tenancy means one application serves multiple customers or tenants while keeping data isolated.

### Tenant resolution

Common tenant identifiers:

- Subdomain: `tenant-a.example.com`
- Header: `X-Tenant-Id`
- Route: `/tenants/{tenantId}/products`
- Auth claim: `tenant_id`

```csharp
public interface ITenantContext
{
    string TenantId { get; }
}

public sealed class HeaderTenantContext : ITenantContext
{
    public HeaderTenantContext(IHttpContextAccessor accessor)
    {
        TenantId = accessor.HttpContext?.Request.Headers["X-Tenant-Id"].ToString()
            ?? throw new InvalidOperationException("Tenant header is required.");
    }

    public string TenantId { get; }
}
```

### Tenant-safe EF Core filter

```csharp
public interface ITenantEntity
{
    string TenantId { get; set; }
}

public sealed class Product : ITenantEntity
{
    public int Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Product>()
        .HasQueryFilter(product => product.TenantId == _tenantContext.TenantId);
}
```

### Interview talking points

- Data isolation is the core multi-tenancy requirement.
- Strategies include shared database/shared schema, shared database/separate schema, and database per tenant.
- Tenant IDs must be validated against the authenticated user's allowed tenants.
- Include tenant ID in cache keys, logs, metrics, and authorization checks.

## 10. Security

Security is not a single feature; it is a set of defaults and habits.

### OWASP API risks to know

- Broken object level authorization.
- Broken authentication.
- Excessive data exposure.
- Unrestricted resource consumption.
- Broken function level authorization.
- Mass assignment.
- Security misconfiguration.
- Injection.
- Improper inventory management.
- Unsafe consumption of APIs.

### ASP.NET Core mitigations

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanEditProduct", policy =>
        policy.RequireAuthenticatedUser()
            .RequireClaim("scope", "products.write"));
});

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
```

### Secrets

Development:

```bash
dotnet user-secrets set "Jwt:SigningKey" "development-only-secret"
```

Production:

- Use a managed secret store such as Azure Key Vault, AWS Secrets Manager, GCP Secret Manager, or Kubernetes secrets.
- Rotate secrets.
- Never log tokens or connection strings.
- Do not commit secrets to Git.

### Input and output safety

- Validate request DTOs.
- Use parameterized queries or EF Core LINQ, not string-concatenated SQL.
- Return only necessary fields.
- Add authorization checks based on resource ownership.
- Use rate limiting for expensive endpoints.

### Interview talking points

- `401` means unauthenticated; `403` means authenticated but forbidden.
- Object-level authorization is checking access to the specific resource, not just the endpoint.
- Mass assignment happens when clients can set fields they should not control, such as `IsAdmin`.
- Secrets belong in secure configuration providers, not source code.

## 11. Testing with xUnit and `WebApplicationFactory`

ASP.NET Core supports integration testing with `Microsoft.AspNetCore.Mvc.Testing`.

### Minimal integration test

```csharp
public sealed class ProductsApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ProductsApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetProducts_ReturnsSuccess()
    {
        var response = await _client.GetAsync("/api/products");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("products", body, StringComparison.OrdinalIgnoreCase);
    }
}
```

### Override services for tests

```csharp
public sealed class TestApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(service =>
                service.ServiceType == typeof(DbContextOptions<AppDbContext>));

            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite("DataSource=:memory:"));
        });
    }
}
```

### What to test

- Endpoint status codes and response shapes.
- Validation errors.
- Authorization behavior.
- Database persistence behavior.
- Exception-to-ProblemDetails mapping.
- Contract compatibility for public APIs.

### Interview talking points

- Unit tests are good for pure business logic.
- Integration tests catch routing, middleware, DI, serialization, auth, and database wiring issues.
- Use realistic providers when provider behavior matters; EF InMemory is not relational.

## 12. Observability with OpenTelemetry

Observability helps you understand production behavior through logs, metrics, and traces.

### OpenTelemetry setup

```csharp
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(
            serviceName: builder.Environment.ApplicationName,
            serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString()))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddOtlpExporter());
```

### Custom activity source

```csharp
public static class CatalogTelemetry
{
    public static readonly ActivitySource ActivitySource = new("Catalog.Api");
}

public sealed class ProductService
{
    public async Task<ProductResponse> CreateAsync(CreateProductRequest request)
    {
        using var activity = CatalogTelemetry.ActivitySource.StartActivity("Create product");
        activity?.SetTag("product.name", request.Name);

        // Save product...
        await Task.CompletedTask;

        return new ProductResponse(1, request.Name, request.Price);
    }
}
```

### Interview talking points

- Logs explain events; metrics show trends; traces show request flow across services.
- Correlation IDs and trace IDs connect logs to traces.
- Instrument inbound requests, outbound HTTP calls, database calls, and queue processing.
- Avoid high-cardinality metric labels such as raw user IDs or request paths with IDs.

## 13. Production architecture decision records

Senior interviews test judgment: when to choose an approach, what tradeoffs it creates, and how to reverse it later. Architecture Decision Records (ADRs) keep reasoning visible.

```text
Title: Use vertical slices with MediatR for order workflows
Status: Accepted
Context: Order workflows need validation, metrics, and transaction behavior.
Decision: Commands and queries live beside handlers, validators, and endpoints.
Consequences:
  + Feature changes are localized.
  + Pipeline behaviors centralize validation and observability.
  - More files than direct controller/service calls.
  - New developers need conventions explained.
Review date: After three more workflows ship.
```

Interview talking points:

- Architecture is about constraints, not aesthetics.
- A good decision explains alternatives rejected.
- Reversibility matters.
- Fitness functions such as test speed, deployment frequency, and latency SLOs validate architecture.

Explain this prompt:

> The team wants Clean Architecture for every API, including a two-endpoint internal webhook receiver. What questions would you ask before agreeing?

## 14. Advanced data consistency patterns

Distributed systems make consistency a design choice. ASP.NET Core services often combine EF Core, message brokers, retries, and external APIs.

Transactional outbox:

```text
request handler
  -> update aggregate
  -> insert outbox message
  -> SaveChanges transaction commits
outbox publisher
  -> reads unpublished messages
  -> publishes to broker
  -> marks message published
```

```csharp
public sealed class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Type { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public string? Error { get; set; }
}
```

Idempotency keys protect retried POST operations:

```csharp
public sealed class IdempotencyRecord
{
    public string Key { get; set; } = string.Empty;
    public string RequestHash { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public string ResponseBody { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}
```

Pitfalls:

- Publishing before DB commit can create ghost events.
- Committing before publish can lose events without an outbox.
- Retrying non-idempotent writes can duplicate orders or charges.
- Exactly-once delivery is rarely available end-to-end; design for at-least-once.

## 15. Advanced security architecture

Use threat modeling to connect framework features to real risks.

| Threat | API example | ASP.NET Core mitigation |
| --- | --- | --- |
| Spoofing | forged token | validate issuer/audience/signature, HTTPS |
| Tampering | changed route id | object authorization, concurrency tokens |
| Repudiation | user denies action | audit logs with user and trace id |
| Information disclosure | overbroad DTO | response shaping, classification |
| Denial of service | expensive search | rate limits, pagination, cancellation |
| Elevation of privilege | role claim abuse | trusted issuer, least privilege policies |

Resource authorization handler:

```csharp
public sealed class SameTenantOrderRequirement : IAuthorizationRequirement;

public sealed class SameTenantOrderHandler
    : AuthorizationHandler<SameTenantOrderRequirement, Order>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        SameTenantOrderRequirement requirement,
        Order resource)
    {
        var tenantId = context.User.FindFirst("tenant_id")?.Value;
        if (!string.IsNullOrWhiteSpace(tenantId) && tenantId == resource.TenantId)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
```

Security review checklist:

- [ ] All endpoints have explicit anonymous/authenticated decisions.
- [ ] Resource ownership is checked after loading resources.
- [ ] DTOs prevent mass assignment.
- [ ] Secrets are loaded from managed providers.
- [ ] Auth failures are logged without token values.
- [ ] CORS allows only known origins.
- [ ] Rate limits protect anonymous and expensive endpoints.
- [ ] Error responses do not leak stack traces.

## 16. Advanced deployment and runtime operations

Containerized ASP.NET Core services should:

- Listen on the expected port from `ASPNETCORE_URLS` or `HTTP_PORTS`.
- Expose `/health/live` for process health.
- Expose `/health/ready` for dependency readiness.
- Fail fast when required configuration is missing.
- Use graceful shutdown for in-flight requests and workers.

```csharp
builder.Services.Configure<HostOptions>(options =>
{
    options.ShutdownTimeout = TimeSpan.FromSeconds(30);
});
```

Environment variables use double underscores for nesting:

```text
ConnectionStrings__Default=...
Jwt__Issuer=https://issuer.example.com
Jwt__Audience=catalog-api
Logging__LogLevel__Microsoft.AspNetCore=Warning
```

Pitfalls:

- A readiness endpoint that checks slow dependencies too frequently can create load.
- Automatic migrations on every instance can race.
- Ignoring memory limits and GC behavior can cause container restarts.

## 17. Senior performance and reliability review

API surface:

- [ ] List endpoints require pagination and deterministic ordering.
- [ ] Expensive filters are indexed or rejected.
- [ ] Payloads are bounded and projected.
- [ ] OpenAPI docs include auth and error responses.

Data access:

- [ ] Read queries use `AsNoTracking` or no-tracking defaults.
- [ ] Write workflows use concurrency tokens where overwrites matter.
- [ ] N+1 checks exist for important endpoints.
- [ ] Migrations are reviewed and deployed explicitly.
- [ ] Hot query plans are inspected.

Resiliency:

- [ ] Outbound HTTP uses `HttpClientFactory`.
- [ ] Timeouts are shorter than gateway timeouts.
- [ ] Retries apply only to transient safe operations.
- [ ] Circuit breakers protect dependencies.
- [ ] Idempotency keys protect retried writes.

Observability:

- [ ] Logs are structured and include trace/user/tenant context.
- [ ] Traces include inbound requests, DB queries, HTTP calls, and queue work.
- [ ] Metrics track latency, error rate, request rate, and saturation.
- [ ] Dashboards align with SLOs.
- [ ] Alerts are actionable.

Explain this prompt:

> Production latency increased from p95 120 ms to p95 900 ms after a release. What evidence do you gather first?

## 18. Advanced interview drill

Practice answering these questions:

- What is the dependency rule in Clean Architecture?
- When is CQRS worth using?
- Why is `AsNoTracking` faster for reads?
- What is the difference between fixed-window and token-bucket rate limiting?
- What should readiness checks include that liveness checks should not?
- When would you choose gRPC over REST?
- What makes a retry policy dangerous?
- How do you prevent cache leakage between tenants?
- How do you enforce object-level authorization?
- What does `WebApplicationFactory` test that unit tests miss?
- How do traces, metrics, and logs complement each other?

