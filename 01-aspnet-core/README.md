# ASP.NET Core Interview Prep

This section is a practical curriculum for modern ASP.NET Core API development using .NET 8/9 style patterns. It is written for interview preparation, but the examples are also useful as a compact reference while building real services.

## Learning order

1. **[01-basics.md](./01-basics.md)**  
   Learn the shape of an ASP.NET Core application: `Program.cs`, dependency injection, middleware, routing, controllers, minimal APIs, model binding, validation, configuration, logging, REST, and Swagger/OpenAPI.
2. **[02-intermediate.md](./02-intermediate.md)**  
   Add persistence, authentication, DTOs, error handling, caching, background work, SignalR, CORS, filters, and API versioning.
3. **[03-advanced.md](./03-advanced.md)**  
   Study architectural and production topics: Clean Architecture, CQRS/MediatR, performance, rate limiting, health checks, gRPC, Polly, Redis, multi-tenancy, security, testing, and OpenTelemetry.
4. **Examples**  
   Read the code examples after each guide:
   - [`examples/01-basics/Program.cs`](./examples/01-basics/Program.cs)
   - [`examples/01-basics/WeatherController.cs`](./examples/01-basics/WeatherController.cs)
   - [`examples/02-intermediate/ProductsController.cs`](./examples/02-intermediate/ProductsController.cs)
   - [`examples/02-intermediate/AppDbContext.cs`](./examples/02-intermediate/AppDbContext.cs)
   - [`examples/02-intermediate/JwtAuthSetup.cs`](./examples/02-intermediate/JwtAuthSetup.cs)
   - [`examples/03-advanced/CreateProductCommand.cs`](./examples/03-advanced/CreateProductCommand.cs)
   - [`examples/03-advanced/ExceptionMiddleware.cs`](./examples/03-advanced/ExceptionMiddleware.cs)
   - [`examples/03-advanced/RateLimitingSetup.cs`](./examples/03-advanced/RateLimitingSetup.cs)

## Interview prep checklist

Use this checklist to test whether you can explain and implement the material.

### Fundamentals

- [ ] Explain what happens when an ASP.NET Core app starts.
- [ ] Describe the role of `Program.cs`, `WebApplicationBuilder`, and `WebApplication`.
- [ ] Explain dependency injection lifetimes: singleton, scoped, transient.
- [ ] Explain middleware order and why `UseAuthentication()` must come before `UseAuthorization()`.
- [ ] Compare controllers and minimal APIs.
- [ ] Describe model binding sources: route, query string, headers, body, and services.
- [ ] Explain REST status codes for common CRUD operations.
- [ ] Configure Swagger/OpenAPI for development.

### Intermediate service design

- [ ] Model entities and relationships with EF Core.
- [ ] Explain migrations and when to use them.
- [ ] Use DTOs to protect persistence models from API contracts.
- [ ] Explain repository and Unit of Work tradeoffs with EF Core.
- [ ] Configure JWT bearer authentication and authorization policies.
- [ ] Implement CORS safely.
- [ ] Centralize exception handling with middleware.
- [ ] Add memory caching and discuss cache invalidation.
- [ ] Explain SignalR and background services.
- [ ] Version an API without breaking clients.

### Advanced production readiness

- [ ] Explain Clean Architecture dependencies and boundaries.
- [ ] Implement CQRS with MediatR.
- [ ] Use `AsNoTracking`, pagination, projections, and compiled queries.
- [ ] Add rate limiting and health checks.
- [ ] Explain gRPC use cases and HTTP/2 requirements.
- [ ] Use Polly for retries, timeouts, and circuit breakers.
- [ ] Configure Redis-backed distributed caching.
- [ ] Discuss tenant identification and tenant-safe data access.
- [ ] Name OWASP API risks and ASP.NET Core mitigations.
- [ ] Test APIs with xUnit and `WebApplicationFactory`.
- [ ] Instrument services with OpenTelemetry traces, metrics, and logs.

## Suggested practice projects

1. **In-memory task API**: minimal API CRUD, validation, Swagger.
2. **Product catalog API**: EF Core, DTOs, JWT auth, filters, exception middleware.
3. **Order processing service**: Clean Architecture, CQRS, background workers, Redis cache, health checks.
4. **Realtime dashboard**: SignalR notifications, background service events, OpenTelemetry instrumentation.

