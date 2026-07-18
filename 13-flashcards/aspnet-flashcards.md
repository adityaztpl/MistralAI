# ASP.NET Core Flashcards

Practice answering each in 20-60 seconds. Add a concrete example when possible.

## 1. What is ASP.NET Core middleware?

**Answer:** Middleware is code in the HTTP pipeline that can inspect, modify, short-circuit, or pass a request to the next component. It matters for cross-cutting concerns like routing, auth, CORS, exception handling, compression, rate limiting, and logging. Order matters: authentication must run before authorization, and exception handling should wrap downstream code.

## 2. How do controllers differ from Minimal APIs?

**Answer:** Controllers use an MVC class/action model with attributes, filters, and conventions. Minimal APIs map routes directly to delegates or handlers with less ceremony. Controllers fit large MVC-style APIs; Minimal APIs fit focused services and vertical slices. In both, keep business logic out of the endpoint.

## 3. What is dependency injection in ASP.NET Core?

**Answer:** DI is the built-in mechanism for providing services to classes and endpoints. It improves testability and separation of concerns. Register services with lifetimes: singleton for app-wide stateless/shared services, scoped for per-request services like DbContext, and transient for lightweight stateless objects.

## 4. Explain singleton vs scoped vs transient.

**Answer:** Singleton creates one instance for the app lifetime, scoped creates one per request scope, and transient creates a new instance each resolution. The danger is injecting scoped services into singletons, which can capture request-specific state incorrectly.

## 5. What request pipeline order do you usually care about?

**Answer:** Typical order includes exception handling, HTTPS redirection, static files if needed, routing, CORS, authentication, authorization, endpoint mapping, and custom middleware. The exact order depends on the app, but auth and CORS placement are common interview traps.

## 6. What is model binding?

**Answer:** Model binding maps route values, query strings, headers, form fields, and request bodies into action parameters or DTOs. It reduces manual parsing but requires validation and explicit contracts to avoid overposting or ambiguous input.

## 7. What is model validation?

**Answer:** Validation checks whether input DTOs satisfy rules before business logic runs. ASP.NET can use data annotations, FluentValidation, or custom validators. Production APIs should return consistent validation errors and never rely only on frontend validation.

## 8. Why avoid exposing EF entities from API responses?

**Answer:** EF entities often contain persistence concerns, navigation properties, internal fields, and cycles. DTOs create a stable API contract, avoid overposting, reduce payloads, and prevent leaking implementation details.

## 9. What is EF Core change tracking?

**Answer:** Change tracking records entity state so SaveChanges can generate inserts, updates, and deletes. It is useful for unit-of-work style updates but should be disabled with AsNoTracking for read-only queries to reduce memory and CPU.

## 10. What is AsNoTracking?

**Answer:** AsNoTracking tells EF Core not to track returned entities. Use it for read-only queries, especially lists and projections. It improves performance but means EF will not automatically detect changes to those objects.

## 11. What is the N+1 query problem?

**Answer:** N+1 happens when an app loads a list and then lazily or separately loads related data per item, causing many database round trips. Fix with projection, Include where appropriate, explicit joins, split queries, or batching.

## 12. When should you use Include vs projection?

**Answer:** Use Include when you need entity graphs for updates or domain behavior. Prefer projection into DTOs for API reads because it selects only needed columns, avoids over-fetching, and shapes the response directly.

## 13. What are migrations?

**Answer:** Migrations are versioned schema changes generated from EF Core model changes. They let teams evolve the database repeatably. Review migrations before applying them; generated SQL can be inefficient or destructive.

## 14. How do you handle transactions?

**Answer:** A single SaveChanges call is transactional by default for relational providers. Use explicit transactions when multiple SaveChanges or external consistency boundaries must be coordinated. Avoid long transactions around network calls.

## 15. What is optimistic concurrency?

**Answer:** Optimistic concurrency assumes conflicts are rare and detects them with row versions or concurrency tokens. On conflict, the app can retry, merge, or ask the user to resolve. It prevents blind overwrites.

## 16. How does authentication differ from authorization?

**Answer:** Authentication proves who the user is. Authorization decides what the authenticated principal can do. In ASP.NET, authentication populates ClaimsPrincipal; authorization policies evaluate roles, claims, requirements, and resources.

## 17. What is JWT bearer auth?

**Answer:** JWT bearer auth validates a signed token sent with the request, usually in the Authorization header. The API validates issuer, audience, lifetime, and signature, then maps claims to the user principal.

## 18. What are claims?

**Answer:** Claims are key/value statements about a user or client, such as subject, email, tenant, role, or scope. Authorization should use claims carefully and verify they come from a trusted issuer.

## 19. What is policy-based authorization?

**Answer:** Policy-based authorization defines named rules that combine requirements, claims, roles, or custom handlers. It is clearer than scattering role checks and supports complex domain authorization.

## 20. How should multi-tenant authorization be enforced?

**Answer:** Authenticate the user, resolve tenant context safely, enforce tenant/ACL filters in queries and tools, and audit access. Never trust tenant IDs from the client without verifying membership.

## 21. What is CORS?

**Answer:** CORS is a browser security mechanism controlling whether a frontend origin can call an API. Configure allowed origins, methods, headers, and credentials deliberately. CORS is not API authentication.

## 22. What is CSRF and when does it matter?

**Answer:** CSRF tricks a browser into sending authenticated cookie-based requests. It matters when using cookies for auth. Mitigate with SameSite cookies, antiforgery tokens, and avoiding unsafe GET actions.

## 23. How do you design API errors?

**Answer:** Use consistent error shapes, appropriate status codes, correlation IDs, validation details, and safe messages. Do not leak stack traces or secrets. ProblemDetails is a common ASP.NET pattern.

## 24. What is cancellation in ASP.NET Core?

**Answer:** Cancellation uses CancellationToken, often HttpContext.RequestAborted, to stop work when clients disconnect or deadlines expire. Pass it to EF, HTTP calls, and LLM requests to save resources.

## 25. What is rate limiting?

**Answer:** Rate limiting restricts request volume by user, IP, tenant, route, or API key. It protects availability and cost. Good APIs return clear 429 responses and include quotas where appropriate.

## 26. What is output caching vs response caching?

**Answer:** Response caching relies on HTTP cache semantics and clients/proxies. Output caching stores server-generated responses on the server according to policies. Use carefully with auth and tenant-specific data.

## 27. How do you add observability to an ASP.NET API?

**Answer:** Use structured logs, metrics, traces, correlation IDs, health checks, and OpenTelemetry. Capture latency, error rate, dependency calls, DB queries, and user/tenant dimensions without logging sensitive data.

## 28. What is health check design?

**Answer:** Liveness says the process is alive; readiness says it can serve traffic and dependencies are usable enough. Do not make liveness depend on every downstream system or orchestrators may restart healthy processes unnecessarily.

## 29. How do you test ASP.NET APIs?

**Answer:** Use unit tests for pure services and integration tests with WebApplicationFactory for routing, auth, filters, serialization, and database behavior. Mock external dependencies but test real HTTP contracts.

## 30. What is a background service?

**Answer:** A BackgroundService runs long-lived work outside request handling, such as queue consumers or scheduled tasks. It must handle cancellation, errors, retries, dependency scopes, and observability.

## 31. How should long-running work be handled?

**Answer:** Do not hold HTTP requests for heavy work unless streaming/progress is needed. Enqueue a job, return 202 with a status endpoint, make work idempotent, and notify via polling or SignalR.

## 32. What is SignalR best for?

**Answer:** SignalR is for real-time server-to-client or bidirectional messaging: chat, notifications, presence, streaming updates. It should complement durable REST commands rather than replace persistent state.

## 33. When use gRPC in a .NET system?

**Answer:** Use gRPC for internal service-to-service calls needing strong contracts, low latency, compact payloads, or streaming. Use REST for public/browser APIs unless gRPC-Web is justified.

## 34. What is the options pattern?

**Answer:** The options pattern binds configuration sections to typed classes, often validated at startup. It avoids scattering string keys and makes configuration injectable and testable.

## 35. What are common secret management rules?

**Answer:** Never commit secrets. Load from environment variables, managed secret stores, or platform identity. Rotate keys, scope permissions, and avoid logging tokens or connection strings.

## 36. How do you protect against SQL injection with EF Core?

**Answer:** Use LINQ and parameterized queries. Avoid string-concatenated SQL. If raw SQL is needed, use parameter APIs and keep user input out of SQL text.

## 37. What is MediatR/CQRS used for?

**Answer:** MediatR and CQRS can organize commands and queries into vertical slices. They help separate read/write concerns and cross-cutting behaviors, but add ceremony if the app is simple.

## 38. What is idempotency?

**Answer:** Idempotency means retrying the same operation does not create duplicate side effects. Use idempotency keys for payment/order/action endpoints and store request outcomes.

## 39. How do you handle pagination?

**Answer:** Use limit/offset for simple lists; use cursor/keyset pagination for large changing datasets. Include stable ordering and avoid unbounded responses.

## 40. What are common performance checks?

**Answer:** Measure p95 latency, DB query count, slow queries, allocation hotspots, payload sizes, cache hit rate, and downstream dependency latency. Optimize after measurement.

## 41. How should an ASP.NET API call an LLM provider?

**Answer:** Server-side only. The API owns auth, prompt construction, retrieval, tool execution, rate limits, logging, cost budgets, and streaming. The frontend should never hold provider secrets.

## 42. How do you stream LLM output to clients?

**Answer:** Use SSE, chunked HTTP, or SignalR. Support cancellation, partial failure handling, token/cost limits, and frontend rendering that can stop generation.

## 43. What is the BFF pattern?

**Answer:** Backend-for-Frontend creates an API tailored to one frontend experience. It hides backend complexity, owns tokens/cookies, composes services, and can reduce frontend security risk.

## 44. What is a good API versioning strategy?

**Answer:** Avoid breaking changes where possible. Version public contracts when necessary through URL/header/media type conventions, document deprecation, and support clients during migration.

## 45. What makes a production-ready endpoint?

**Answer:** Clear contract, validation, auth, cancellation, logging, metrics, consistent errors, tests, rate limits if needed, and no leakage of persistence or secret details.
