# Controllers vs Minimal APIs in ASP.NET Core

## The decision

ASP.NET Core supports both MVC-style controllers and Minimal APIs. The choice is about API surface area, team conventions, testability, filters, OpenAPI shape, endpoint grouping, and whether you want a lightweight function-first model or a class/action model.

## Quick default

- Choose **Minimal APIs** for small services, focused microservices, prototypes, health checks, internal APIs, BFF endpoints, and apps that prefer endpoint groups plus explicit handlers.
- Choose **Controllers** for large APIs with many resources, mature MVC conventions, complex filters, API versioning patterns, shared behaviors, and teams already organized around controller/action structure.

## Comparison

| Force | Controllers | Minimal APIs |
|---|---|---|
| Style | Class/action MVC model | Route-to-delegate or route-to-handler model |
| Ceremony | More files/attributes/classes | Less ceremony |
| Large API organization | Familiar resource grouping | Good with endpoint groups, but discipline required |
| Filters | MVC filters are mature | Endpoint filters are lightweight |
| Model binding | Rich MVC conventions | Strong support, less MVC surface |
| Testing | Controller unit tests are common; integration tests preferred | Handler/service tests plus integration tests |
| OpenAPI | Mature conventions | Strong support with endpoint metadata |
| Learning | Familiar to MVC/Web API developers | Simple start, newer conventions |

## When controllers fit best

Use controllers when:

- The API is resource-heavy and long-lived.
- The team already has controller patterns, base controllers, filters, conventions, and documentation.
- You need consistent action result handling across many endpoints.
- You rely on MVC-specific filters or conventions.
- You want a clear class-per-resource structure for onboarding.
- API versioning and documentation conventions are already controller-based.

## When Minimal APIs fit best

Use Minimal APIs when:

- You are building a small or medium service with focused endpoints.
- You want reduced ceremony and explicit route registration.
- You prefer vertical slices: endpoint -> request DTO -> validator -> handler -> response.
- You are building internal services, BFF endpoints, webhooks, health/readiness endpoints, or streaming endpoints.
- You want endpoint filters for lightweight cross-cutting behavior.
- You are comfortable using route groups to keep structure clean.

## Important nuance

Minimal APIs are not toy APIs. They can be production-grade if you organize them well:

```text
Features/
  Orders/
    CreateOrderEndpoint.cs
    CreateOrderRequest.cs
    CreateOrderValidator.cs
    OrderResponse.cs
```

Controllers are not old. They remain a good fit when MVC conventions reduce complexity for a large team.

## Trade-offs

Controllers:

- More ceremony, but more familiar structure.
- Good convention surface, but can become bloated if controllers contain business logic.
- Easy to overuse base controllers and inheritance.

Minimal APIs:

- Less ceremony, but organization is your responsibility.
- Easy to start with everything in `Program.cs`; production code should move endpoints into feature modules.
- Endpoint filters are useful but do not replace all MVC filter scenarios.

## Interview answer script

```text
I would use Minimal APIs for focused services and vertical-slice endpoints because they reduce ceremony and keep the endpoint close to its request/response types. I would use controllers if the API is large, already MVC-based, or depends heavily on controller conventions, filters, and versioning patterns.

In either case, I would keep business logic out of the endpoint/controller and put it in services or command/query handlers. The bigger architectural concern is not the syntax; it is validation, auth, cancellation, observability, and integration tests around the HTTP contract.
```

## Practical guidance

For both styles:

- Use DTOs at the boundary; do not expose EF entities directly.
- Validate inputs before calling business logic.
- Pass `CancellationToken` to async operations.
- Use typed results or consistent response types.
- Keep authorization policy names explicit.
- Add OpenAPI metadata and examples for public APIs.
- Prefer integration tests for routing, binding, auth, filters, and serialization.

## Red flags

- Putting EF queries directly in controllers for complex workflows.
- Returning different error shapes from every endpoint.
- Ignoring cancellation for long requests or LLM streaming.
- Building a large Minimal API entirely inside `Program.cs`.
- Choosing controllers only because Minimal APIs cannot scale; they can if structured.
