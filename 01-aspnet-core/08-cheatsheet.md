# ASP.NET Core Dense Interview Cheatsheet

A final rapid-review guide. Use it after reading the deep dives.

## 1. Startup

`CreateBuilder` loads config/logging/DI/hosting. Register services before `Build`. Configure middleware and endpoints after `Build`. `Run` starts Kestrel.

## 2. Middleware order

`UseAuthentication` before `UseAuthorization`. Put exception handling early. Put CORS before auth endpoints when browser callers need preflight handling.

## 3. DI lifetimes

Singleton once per app, scoped once per request, transient per resolution. Never inject scoped services directly into singletons.

## 4. Controllers vs minimal APIs

Controllers fit MVC conventions and filters. Minimal APIs fit compact endpoints and route groups. Both are production-ready.

## 5. Model binding

Sources: route, query, header, body, form/files, services. Binding constructs values; validation enforces rules.

## 6. Status codes

`201` create, `204` no body success, `400` validation, `401` unauthenticated, `403` forbidden, `404` missing, `409` conflict, `429` rate limited.

## 7. EF Core essentials

`DbContext` is scoped and not thread-safe. Tracking for updates. `AsNoTracking` for reads. `SaveChangesAsync` persists tracked changes transactionally.

## 8. EF performance

Project DTOs, page, deterministic order, inspect SQL, avoid lazy loading, use indexes, use split queries for multiple collections, measure before compiled queries.

## 9. Concurrency

Use row version or concurrency token. Set original token on update. Catch `DbUpdateConcurrencyException`. Return `409`.

## 10. Auth

Authentication says who. Authorization says allowed. Validate JWT issuer, audience, lifetime, signature. JWT payload is not encrypted.

## 11. Refresh tokens

Hash server-side, rotate every use, revoke on logout, detect reuse, revoke token family on suspected theft.

## 12. Authorization

Use policies for endpoint permissions. Use resource authorization for ownership/tenant checks. Endpoint policy is not object-level authorization.

## 13. CORS

Browser policy, not auth. Exact origins. Do not use wildcard with credentials. Postman/curl ignore CORS.

## 14. OWASP

BOLA, broken auth, mass assignment, unrestricted resources, function-level auth, injection, misconfiguration, unsafe API consumption.

## 15. Caching

Memory cache is per instance. Distributed cache is shared. Output cache stores responses. Keys need tenant/user/query dimensions. Invalidate after writes.

## 16. Background services

Hosted services are singleton. Create scopes for scoped services. Honor cancellation. Use durable queues for durable work.

## 17. Clean Architecture

Dependencies point inward. Domain is independent. Infrastructure implements application interfaces. Avoid ceremony for tiny APIs.

## 18. CQRS

Commands change state. Queries read state. MediatR is optional. Useful when workflows and cross-cutting behaviors grow.

## 19. Testing

Unit test domain logic. Integration test pipeline with `WebApplicationFactory`. Fake auth. Prefer SQLite/Testcontainers over EF InMemory for relational behavior.

## 20. Observability

Logs events, metrics trends, traces request flow. Use structured logs and low-cardinality metric labels. Connect with trace ids.

## 21. Performance debugging

Check RED metrics, traces, query plans, dependency latency, runtime counters, deployment changes, then fix and verify p95/p99.

## Interview checklist

- [ ] Explain the concept without code.
- [ ] Implement the smallest working example.
- [ ] Name two production pitfalls.
- [ ] Name one test that catches a regression.
- [ ] Describe the operational signal that reveals a production issue.

## Explain this prompts

- Explain request flow from Kestrel to endpoint.
- Explain DI lifetimes with EF Core.
- Explain DTOs and mass assignment.
- Explain EF tracking and `AsNoTracking`.
- Explain optimistic concurrency.
- Explain JWT validation.
- Explain refresh token rotation.
- Explain object authorization.
- Explain N+1.
- Explain `WebApplicationFactory`.
- Explain logs versus metrics versus traces.
- Explain p95 latency regression investigation.

## 37. Last-minute Q&A drill

### What happens at startup?

Builder loads configuration, logging, services, and hosting defaults. Services are registered. `Build` creates the app. Middleware and endpoints are mapped. `Run` starts Kestrel.

### Why does middleware order matter?

Each middleware wraps the next. Earlier middleware can short-circuit or prepare state needed by later middleware.

### Why is `DbContext` scoped?

It is a unit-of-work object that tracks changes, is not thread-safe, and should not share tracked state across requests.

### Why not return EF entities?

Entities leak schema/internal fields, can create serialization cycles, enable overexposure, and couple API contracts to persistence.

### What is a DTO?

A request or response contract tailored to the API boundary. DTOs prevent over-posting and let contracts evolve independently.

### What is `ProblemDetails`?

RFC 7807 machine-readable error shape with title, status, detail, type, instance, and extensions.

### What is BOLA?

Broken Object Level Authorization: the API fails to verify that the authenticated user can access the specific object id requested.

### What is N+1?

One query loads parent rows, then one query per parent loads related data. Fix with projection, eager loading, or batching.

### What is a concurrency token?

A value included in update/delete predicates to detect whether a row changed since it was read.

### What is cache-aside?

Application checks cache, loads source on miss, stores result, and invalidates/updates cache after writes.

## 38. Code snippets to memorize

### Policy

```csharp
options.AddPolicy("Products.Write", policy =>
    policy.RequireAuthenticatedUser().RequireClaim("scope", "products.write"));
```

### Projection

```csharp
var items = await db.Products.AsNoTracking()
    .Select(product => new ProductResponse(product.Id, product.Name, product.Price))
    .ToListAsync(cancellationToken);
```

### Concurrency

```csharp
db.Entry(product).Property(p => p.RowVersion).OriginalValue = request.RowVersion;
try { await db.SaveChangesAsync(cancellationToken); }
catch (DbUpdateConcurrencyException) { return Results.Conflict(); }
```

### Fake auth test handler idea

```csharp
var claims = new[] { new Claim("scope", "products.write"), new Claim("tenant_id", "tenant-a") };
var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
```

### OpenTelemetry shape

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation())
    .WithMetrics(metrics => metrics.AddAspNetCoreInstrumentation().AddRuntimeInstrumentation());
```

## 39. Decision tables

### Read query strategy

| Need | Choose |
| --- | --- |
| API read | projection + `AsNoTracking` |
| update aggregate | tracking query with includes needed by domain rule |
| hot measured query | compiled query after profiling |
| multiple collection include | split query consideration |
| deep infinite scroll | keyset pagination |

### Auth strategy

| Scenario | Choose |
| --- | --- |
| browser server-rendered app | secure cookies + antiforgery |
| SPA calling API | short access token, refresh strategy, strict CORS |
| machine-to-machine | client credentials / trusted issuer |
| per-resource ownership | resource authorization handler |
| admin permissions | policy/permission claims, audit logging |

### Test strategy

| Risk | Test |
| --- | --- |
| domain branch | unit test |
| endpoint routing/middleware | integration test |
| SQL provider behavior | SQLite/Testcontainers |
| auth claims | fake auth integration test |
| public contract | OpenAPI/client contract test |
| browser workflow | end-to-end test |

## 40. Red flags in interviews

Avoid saying:

- `DbContext` should be singleton for performance.
- CORS secures an API from attackers.
- JWT contents are secret because they are encoded.
- Unit tests with mocked controllers prove middleware works.
- EF InMemory proves SQL behavior.
- Retries are always safe.
- Health checks should test every dependency deeply every second.
- Returning entities is fine for public APIs.
- Roles are enough for all authorization.
- Caching has no correctness cost.

## 41. Senior-level follow-up prompts

- How would you handle schema migrations in a rolling deployment?
- How would you rotate JWT signing keys without logging everyone out?
- How would you prevent duplicate payment charges after client retries?
- How would you prove tenant isolation in tests?
- How would you diagnose a slow endpoint without reproducing locally?
- How would you design cache invalidation for product search?
- How would you handle a downstream dependency outage?
- How would you split a large controller without overengineering?
- How would you decide between REST and gRPC?
- How would you enforce security on a new admin feature?

## 42. One-hour practice plan

1. 10 minutes: whiteboard request pipeline and auth flow.
2. 10 minutes: implement a minimal CRUD endpoint with validation.
3. 10 minutes: add EF projection, paging, and row-version concurrency.
4. 10 minutes: add a policy and explain `401` versus `403`.
5. 10 minutes: write one `WebApplicationFactory` test with fake auth.
6. 10 minutes: explain how you would observe and optimize the endpoint.

## 43. Final confidence checklist

- [ ] I can explain middleware order.
- [ ] I can explain DI lifetimes.
- [ ] I can explain model binding and validation.
- [ ] I can design DTOs that prevent mass assignment.
- [ ] I can write an EF projection with paging.
- [ ] I can handle optimistic concurrency.
- [ ] I can configure JWT validation.
- [ ] I can implement resource authorization.
- [ ] I can explain CORS accurately.
- [ ] I can write integration tests with fake auth.
- [ ] I can diagnose N+1 queries.
- [ ] I can describe OpenTelemetry signals.
- [ ] I can make conservative architecture tradeoffs.

## 44. Tiny definitions

- **Kestrel**: cross-platform ASP.NET Core web server.
- **Middleware**: request/response component that can pass to or short-circuit the next component.
- **Endpoint metadata**: data used by auth, OpenAPI, filters, CORS, rate limiting, and caching.
- **Scoped service**: one instance per DI scope, usually one request.
- **Policy**: named authorization rule.
- **Concurrency token**: value used to detect stale updates.
- **Projection**: selecting exactly the response shape from a query.
- **Outbox**: table of messages committed with business data and published later.
- **SLO**: reliability target users care about.
- **High cardinality**: metric label values with too many unique possibilities.

## 45. If you freeze in an interview

Use this structure:

1. State the concept simply.
2. Give the common ASP.NET Core API example.
3. Name the main pitfall.
4. Name the production mitigation.
5. Name the test or metric that proves it works.

Example: "For authorization, endpoint policies check whether a user can call an operation, but resource authorization checks whether they can access this specific order. The pitfall is BOLA. The mitigation is tenant/user filtering plus `IAuthorizationService`. The proof is an integration test where user B attempts user A's order."
