# Performance and Observability Deep Dive

A measurement-first guide to ASP.NET Core performance, OpenTelemetry, caching, profiling, N+1 diagnosis, and incident response.

## 1. Performance mindset

Start with evidence, not guesses.

```text
symptom -> metric -> trace/log evidence -> hypothesis -> experiment -> fix -> verify
```

Common bottlenecks: slow SQL, N+1 queries, missing indexes, remote HTTP latency, blocking calls, huge payloads, cache stampedes, connection pool exhaustion, and GC pressure.

## 2. RED and USE metrics

For requests, track Rate, Errors, Duration. For resources, track Utilization, Saturation, Errors. Watch p50, p95, and p99 because averages hide tail latency.

## 3. OpenTelemetry setup

```csharp
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(builder.Environment.ApplicationName))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation(options => options.RecordException = true)
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddSource("Catalog.Api")
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter("Catalog.Api")
        .AddOtlpExporter());
```

## 4. Logs, metrics, traces

Logs answer what happened. Metrics answer how often/how much. Traces answer where time went. Mature systems connect all three with trace ids.

## 5. Structured logging and custom metrics

```csharp
_logger.LogInformation("Order {OrderId} submitted by user {UserId}", order.Id, userId);

public static readonly Counter<long> OrdersCreated = Meter.CreateCounter<long>("orders.created");
OrdersCreated.Add(1, new KeyValuePair<string, object?>("plan", tenantPlan));
```

Use low-cardinality tags. Do not use user ids, order ids, or emails as metric labels.

## 6. EF Core performance

Use projection, no tracking, pagination, query tags, and query plan review.

```csharp
var products = await db.Products
    .TagWith("Products.Search")
    .AsNoTracking()
    .Where(product => product.IsActive)
    .OrderBy(product => product.Name)
    .Select(product => new ProductListItem(product.Id, product.Name, product.Price))
    .Take(50)
    .ToListAsync(cancellationToken);
```

N+1 symptoms: many similar queries and latency growing with parent row count.

## 7. Caching strategy

Caches: memory, distributed, output, CDN, browser. Cache keys must include tenant, user/permission when personalized, culture, query parameters, and API version.

Cache-aside:

```text
cache hit -> return
cache miss -> DB -> set cache -> return
write -> update DB -> invalidate/update cache
```

## 8. Cache stampede and output cache

Stampede mitigations: per-key locking, TTL jitter, background refresh, stale-while-revalidate, distributed locks.

```csharp
builder.Services.AddOutputCache(options =>
{
    options.AddPolicy("CatalogList", policy => policy
        .Expire(TimeSpan.FromSeconds(30))
        .SetVaryByQuery("categoryId", "page", "pageSize"));
});
```

## 9. Async and ThreadPool

Blocking code starves the thread pool.

Bad: `.Result`, `.Wait()`, `Thread.Sleep`.
Good: `await`, cancellation tokens, asynchronous I/O.

Symptoms: high latency with low CPU, rising thread pool queue length, request queue growth.

## 10. HttpClientFactory and resilience

```csharp
builder.Services.AddHttpClient<IInventoryClient, InventoryClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Inventory:BaseUrl"]!);
    client.Timeout = TimeSpan.FromSeconds(5);
})
.AddStandardResilienceHandler(options =>
{
    options.Retry.MaxRetryAttempts = 2;
    options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(8);
});
```

Retry only transient safe operations; use idempotency keys for retried writes.

## 11. Profiling and counters

Tools: `dotnet-counters`, `dotnet-trace`, `dotnet-dump`, `dotnet-gcdump`, query store, explain plans, Application Insights, Jaeger, Grafana Tempo, Prometheus.

Watch CPU, allocation rate, GC heap, thread pool queue length, request rate, failed requests, dependency latency, and DB connection waits.

## 12. Incident playbook

1. Identify endpoint/timeframe.
2. Compare RED metrics before/after deployment.
3. Inspect traces for slow spans.
4. Check logs by trace id.
5. Inspect DB query duration and plans.
6. Check dependency latency.
7. Review migrations/config/feature flags.
8. Roll back or mitigate.
9. Verify recovery.
10. Add tests/alerts.

## Interview checklist

- [ ] Explain the concept without code.
- [ ] Implement the smallest working example.
- [ ] Name two production pitfalls.
- [ ] Name one test that catches a regression.
- [ ] Describe the operational signal that reveals a production issue.

## Explain this prompts

- Explain p95 versus average latency.
- Explain how traces, metrics, and logs complement each other.
- Explain N+1 detection.
- Explain cache key security.
- Explain cache stampede mitigation.
- Explain thread pool starvation.
- Explain why retries can worsen outages.
- Explain liveness versus readiness.

## Hands-on lab: remove an N+1 query

Scenario: `GET /api/categories` returns categories and product counts. It executes one category query plus one product query per category.

### Bad shape

```csharp
var categories = await db.Categories.AsNoTracking().ToListAsync(cancellationToken);
var responses = new List<CategoryResponse>();
foreach (var category in categories)
{
    var count = await db.Products.CountAsync(product => product.CategoryId == category.Id, cancellationToken);
    responses.Add(new CategoryResponse(category.Id, category.Name, count));
}
```

### Better shape

```csharp
var responses = await db.Categories
    .AsNoTracking()
    .Select(category => new CategoryResponse(
        category.Id,
        category.Name,
        category.Products.Count(product => product.IsActive)))
    .ToListAsync(cancellationToken);
```

### Verify

- Inspect SQL count in logs/traces.
- Test with multiple categories.
- Check p95 latency with realistic data.
- Ensure indexes support the aggregate/filter.

## Cache correctness lab

Scenario: product details are cached by product id only: `products:{id}`. A tenant B user receives tenant A data because ids overlap.

### Fix

```csharp
public static string ProductDetail(string tenantId, int productId) =>
    $"tenant:{tenantId}:products:{productId}:detail:v1";
```

### Checklist

- [ ] Include tenant id.
- [ ] Include user/permission dimension when response is personalized.
- [ ] Include query string dimensions.
- [ ] Include API version or shape version.
- [ ] Invalidate after writes.
- [ ] Avoid caching forbidden/not-found responses without careful TTLs.

## Observability field guide

| Symptom | First signal | Follow-up |
| --- | --- | --- |
| p95 latency high | request duration metric | traces by slow endpoint |
| error rate high | 5xx metric | logs by trace id |
| DB CPU high | DB metrics | query plan and EF tags |
| low CPU but high latency | thread pool queue | blocking call search |
| dependency outage | outbound HTTP spans | retry/circuit metrics |
| memory growth | GC heap counters | dump/gcdump |
| user-specific bug | structured logs | trace id and user/tenant context |

## Runtime counter cheat sheet

Watch with `dotnet-counters`:

- `cpu-usage`: high CPU means compute, JSON, compression, or DB client work may dominate.
- `gc-heap-size`: rising heap can indicate retained objects or high allocation pressure.
- `alloc-rate`: high allocation can increase GC pauses.
- `threadpool-thread-count`: rising threads can indicate blocking.
- `threadpool-queue-length`: queued work means requests wait for threads.
- `requests-per-second`: traffic rate.
- `current-requests`: in-flight pressure.
- `failed-requests`: server-side failure trend.

## Resilience review scenarios

### Retry storm

A downstream service times out. Every request retries three times with no jitter. Traffic triples and the dependency gets worse.

Fix:

- Retry fewer times.
- Use exponential backoff and jitter.
- Add circuit breaker.
- Add timeout budget.
- Avoid retries for unsafe writes without idempotency.

### Timeout mismatch

API timeout is 100 seconds but gateway timeout is 30 seconds. Work continues after client receives failure.

Fix:

- Set app timeouts shorter than upstream deadlines.
- Pass cancellation tokens.
- Stop expensive work when `RequestAborted` is cancelled.

### Cache stampede

A hot key expires and hundreds of requests hit the database.

Fix:

- Per-key lock.
- TTL jitter.
- Background refresh.
- Stale-while-revalidate.

## SLO-driven dashboard checklist

- [ ] Request rate by endpoint.
- [ ] Error rate by endpoint/status family.
- [ ] p50/p95/p99 latency by endpoint.
- [ ] Dependency latency for database and HTTP clients.
- [ ] DB connection pool waits or timeout count.
- [ ] Thread pool queue length.
- [ ] GC allocation rate and heap size.
- [ ] Cache hit ratio.
- [ ] Queue length and oldest message age.
- [ ] Deployment markers.

## Load test design notes

- Use production-like data volume.
- Include realistic authentication and tenant distribution.
- Warm caches intentionally, then test cold-cache behavior separately.
- Track server and dependency metrics, not just client latency.
- Define pass/fail thresholds before the test.
- Preserve traces for slow samples.
- Include failure scenarios for dependencies if safe.

## Code review checklist for performance

- [ ] Any new list endpoint has pagination.
- [ ] Queries project DTOs instead of entities.
- [ ] New cache keys include tenant/user/query dimensions.
- [ ] New outbound HTTP calls use `HttpClientFactory`.
- [ ] Timeouts and cancellation tokens are used.
- [ ] Retries are safe and bounded.
- [ ] Logs are structured and not high-volume in loops.
- [ ] Metrics avoid high-cardinality labels.
- [ ] Health checks are cheap.
- [ ] Large responses are justified or streamed.

## Final performance flashcards

- p95 beats average for user pain.
- Traces show where time went.
- Metrics show trend and alert state.
- Logs explain discrete events.
- N+1 latency grows with rows.
- Cache invalidation is part of write design.
- Retries require timeouts.
- Circuit breakers protect dependencies.
- Blocking calls can starve the thread pool.
- Query plans matter more than LINQ elegance.
