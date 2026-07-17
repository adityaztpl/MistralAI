# ASP.NET Core Interview Prep

This section is a practical, interview-focused, hands-on curriculum for modern ASP.NET Core API development using .NET 8/9 style patterns. It moves from fundamentals to production architecture, with deep dives into EF Core, security, testing, performance, and observability.

## Learning order

1. **[01-basics.md](./01-basics.md)** - hosting, DI, middleware, routing, minimal APIs, controllers, model binding, validation, configuration, logging, REST, OpenAPI, request lifecycle, typed results, and fundamentals labs.
2. **[02-intermediate.md](./02-intermediate.md)** - EF Core persistence, DTOs, authentication, authorization, CORS, filters, exception handling, caching, SignalR, background services, versioning, transactions, and secure API practice.
3. **[03-advanced.md](./03-advanced.md)** - Clean Architecture, CQRS/MediatR, performance, rate limiting, health checks, gRPC, Polly, Redis, multi-tenancy, threat modeling, outbox/idempotency, deployment, testing, and OpenTelemetry.
4. **[04-ef-core-deep-dive.md](./04-ef-core-deep-dive.md)** - tracking, change tracker states, concurrency, indexes, raw SQL, transactions, compiled queries, diagnostics, and performance.
5. **[05-security-auth.md](./05-security-auth.md)** - Identity, JWTs, refresh tokens, claims, roles, policies, resource authorization, CORS, CSRF, OWASP API risks, secrets, audit logging, and security drills.
6. **[06-testing.md](./06-testing.md)** - unit tests, integration tests, `WebApplicationFactory`, fake auth, EF provider choice, Testcontainers patterns, seeding, and CI strategy.
7. **[07-performance-observability.md](./07-performance-observability.md)** - OpenTelemetry, structured logs, metrics, traces, caching, N+1 diagnosis, profiling tools, runtime counters, and SLO-driven operations.
8. **[08-cheatsheet.md](./08-cheatsheet.md)** - dense final review for interviews.

## Example code map

### Basics

- [`examples/01-basics/Program.cs`](./examples/01-basics/Program.cs)
- [`examples/01-basics/WeatherController.cs`](./examples/01-basics/WeatherController.cs)

### Intermediate

- [`examples/02-intermediate/ProductsController.cs`](./examples/02-intermediate/ProductsController.cs)
- [`examples/02-intermediate/AppDbContext.cs`](./examples/02-intermediate/AppDbContext.cs)
- [`examples/02-intermediate/JwtAuthSetup.cs`](./examples/02-intermediate/JwtAuthSetup.cs)

### Advanced

- [`examples/03-advanced/CreateProductCommand.cs`](./examples/03-advanced/CreateProductCommand.cs)
- [`examples/03-advanced/ExceptionMiddleware.cs`](./examples/03-advanced/ExceptionMiddleware.cs)
- [`examples/03-advanced/RateLimitingSetup.cs`](./examples/03-advanced/RateLimitingSetup.cs)

### EF Core deep dive

- [`examples/04-ef-core/OrderRepository.cs`](./examples/04-ef-core/OrderRepository.cs)
- [`examples/04-ef-core/ConcurrencyExample.cs`](./examples/04-ef-core/ConcurrencyExample.cs)

### Security

- [`examples/05-security/RefreshTokenService.cs`](./examples/05-security/RefreshTokenService.cs)
- [`examples/05-security/PermissionPolicies.cs`](./examples/05-security/PermissionPolicies.cs)

### Testing

- [`examples/06-testing/ProductsControllerTests.cs`](./examples/06-testing/ProductsControllerTests.cs)

### Performance

- [`examples/07-performance/CachingExamples.cs`](./examples/07-performance/CachingExamples.cs)
- [`examples/07-performance/CompiledQueryExample.cs`](./examples/07-performance/CompiledQueryExample.cs)

## Master checklist

### Fundamentals

- [ ] Explain startup, hosting, `Program.cs`, DI, middleware, and routing.
- [ ] Compare minimal APIs and controllers.
- [ ] Explain model binding sources and validation behavior.
- [ ] Choose status codes for CRUD, validation, auth, conflict, and rate limiting.
- [ ] Configure OpenAPI metadata accurately.

### EF Core

- [ ] Explain `DbContext` lifetime, tracking, states, and `SaveChangesAsync`.
- [ ] Use DTO projections, `AsNoTracking`, pagination, split queries, and compiled queries.
- [ ] Implement row-version concurrency and return `409 Conflict`.
- [ ] Design indexes for filters, joins, sorts, uniqueness, and tenants.
- [ ] Use raw SQL safely with parameters.

### Security

- [ ] Explain authentication versus authorization.
- [ ] Validate JWT issuer, audience, lifetime, and signing key.
- [ ] Implement refresh token rotation and revocation.
- [ ] Use policy-based and resource-based authorization.
- [ ] Map OWASP API risks to ASP.NET Core mitigations.
- [ ] Configure CORS safely.

### Testing

- [ ] Unit test domain and application logic.
- [ ] Integration test routing, middleware, auth, serialization, EF, and `ProblemDetails`.
- [ ] Use `WebApplicationFactory` and fake auth.
- [ ] Use SQLite or Testcontainers for relational behavior.
- [ ] Test validation, auth failures, ownership, and concurrency.

### Production readiness

- [ ] Add rate limiting, health checks, structured logs, traces, and metrics.
- [ ] Use `HttpClientFactory` with timeouts and safe retries.
- [ ] Add cache key discipline and invalidation.
- [ ] Explain outbox, idempotency keys, and distributed consistency tradeoffs.
- [ ] Diagnose latency with traces, metrics, logs, query plans, and runtime counters.

## Suggested practice projects

1. **In-memory task API**: minimal API CRUD, validation, typed results, Swagger, `ProblemDetails`.
2. **Product catalog API**: EF Core, DTOs, JWT auth, policies, filters, exception middleware, row-version concurrency.
3. **Secure auth service**: Identity, access tokens, refresh-token rotation, revocation, CORS, security logging.
4. **Order processing service**: Clean Architecture, CQRS, outbox, background workers, Redis cache, health checks.
5. **Realtime dashboard**: SignalR notifications, background service events, OpenTelemetry instrumentation.
6. **Performance lab**: intentionally slow EF queries, N+1 detection, caching, compiled queries, counters, and tracing.
