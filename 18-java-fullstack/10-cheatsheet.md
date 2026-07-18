# 10 - Java Fullstack Cheatsheet

Fast recall for Spring Boot + React/Angular + SQL + AWS interviews.

## Architecture

```text
SPA -> API controller -> service/use case -> repository -> database
```

Rules:

- Controllers handle HTTP.
- DTOs protect API contracts.
- Services own transactions and business rules.
- Repositories own persistence queries.
- Database enforces critical constraints.
- Frontend validation is UX, not security.

## Layered vs clean architecture

| Layered | Clean/hexagonal |
|---|---|
| Controller -> service -> repository | Adapters -> use cases -> ports/domain |
| Fast and familiar | Strong boundaries |
| Good for CRUD | Good for complex domains |
| Risk of entity leakage | More classes/indirection |

## REST conventions

```text
GET    /api/v1/products
GET    /api/v1/products/{id}
POST   /api/v1/products
PUT    /api/v1/products/{id}
PATCH  /api/v1/products/{id}
DELETE /api/v1/products/{id}
```

Status:

- `200` read/update with body
- `201` created
- `204` no content
- `400` validation/bad request
- `401` unauthenticated
- `403` forbidden
- `404` not found
- `409` conflict
- `429` rate limited
- `500` unexpected server error

## Problem Details

```json
{
  "type": "https://api.example.com/problems/validation-error",
  "title": "Validation failed",
  "status": 400,
  "detail": "One or more fields are invalid.",
  "traceId": "abc123",
  "errors": {
    "name": ["must not be blank"]
  }
}
```

## Spring annotations

| Annotation | Use |
|---|---|
| `@SpringBootApplication` | app entry |
| `@RestController` | JSON controller |
| `@RequestMapping` | base route |
| `@GetMapping` | GET endpoint |
| `@PostMapping` | POST endpoint |
| `@Valid` | validate request body |
| `@RestControllerAdvice` | exception translation |
| `@Service` | use-case/business service |
| `@Transactional` | transaction boundary |
| `@Repository` | persistence component |
| `@Entity` | JPA entity |
| `@Version` | optimistic locking |
| `@PreAuthorize` | method authorization |

## Validation

Common Bean Validation:

- `@NotNull`
- `@NotBlank`
- `@Size`
- `@Email`
- `@Min`
- `@Max`
- `@DecimalMin`
- `@Pattern`

Layering:

```text
frontend checks -> DTO validation -> service/domain rules -> DB constraints
```

## Transactions

Put `@Transactional` on service methods.

Read-only:

```java
@Transactional(readOnly = true)
```

Write:

```java
@Transactional
```

Avoid:

- transaction logic in controllers
- long transactions over network calls
- lazy loading during JSON serialization

## JPA pitfalls

- N+1 queries
- lazy loading outside transaction
- returning entities from controllers
- `ddl-auto=update` in production
- missing indexes
- missing optimistic locking
- careless cascade deletes
- using `EAGER` everywhere

## Pagination

Request:

```text
GET /api/v1/products?page=0&size=20&sort=createdAt,desc
```

Response:

```json
{
  "items": [],
  "page": 0,
  "size": 20,
  "totalItems": 0,
  "totalPages": 0,
  "hasNext": false
}
```

Cap `size`. Whitelist sort fields.

## Auth

Authentication: who are you?

Authorization: what can you do?

JWT checks:

- signature
- issuer
- audience
- expiration
- roles/scopes
- algorithm

Ownership:

```java
findByIdAndOwnerId(id, currentUser.id())
```

## SPA token storage

| Storage | Note |
|---|---|
| memory | safer but lost on refresh |
| localStorage | easy, XSS risk |
| sessionStorage | tab-scoped, XSS risk |
| HTTP-only cookie | JS cannot read, CSRF concerns |
| BFF session | strong containment, more infra |

## CORS

CORS is browser policy, not authentication.

Allow:

- exact origins
- methods
- headers
- credentials only when needed

Never combine wildcard origin with credentials.

## React quick notes

- Vite env vars must start with `VITE_`.
- `AuthContext` centralizes auth state.
- API client attaches bearer token.
- Protected routes are UX only.
- TanStack Query handles server state.
- Map Problem Details `errors` to form fields.
- Never store secrets in frontend env.

## Angular quick notes

- Environment files contain public config only.
- Functional interceptors add auth headers.
- Guards protect routes for UX.
- `HttpErrorResponse.error` contains Problem Details body.
- Reactive forms can use `setErrors({ server: message })`.
- Use `switchMap` for cancellable search.
- Use `takeUntilDestroyed` or async pipe for cleanup.

## PostgreSQL vs SQL Server

| Concept | PostgreSQL | SQL Server |
|---|---|---|
| UUID | `uuid` | `uniqueidentifier` |
| Time | `timestamptz` | `datetime2` |
| JSON | `jsonb` | `nvarchar(max)` + JSON functions |
| Pagination | `limit/offset` | `offset/fetch` |
| Generated UUID | `gen_random_uuid()` | `newsequentialid()` |

## Index design

Design from query:

```sql
where owner_id = ?
  and status = ?
order by created_at desc
```

Index:

```text
(owner_id, status, created_at desc)
```

Unique:

```text
(owner_id, sku)
```

## Flyway

Naming:

```text
V1__init.sql
V2__add_product_indexes.sql
```

Rules:

- Do not edit applied migrations.
- Use new migration for change.
- Test against real DB.
- Prefer `ddl-auto=validate`.
- Use expand/contract for risky changes.

## Docker Compose

Commands:

```bash
docker compose up --build
docker compose logs -f api
docker compose down
docker compose down -v
```

Networking:

- host to DB: `localhost:5432`
- container to DB: `postgres:5432`

## AWS services

| Service | Use |
|---|---|
| S3 | SPA static assets |
| CloudFront | CDN, HTTPS, route fallback |
| ALB | API load balancing |
| ECS/Fargate | Spring Boot containers |
| Elastic Beanstalk | simpler API hosting |
| ECR | container images |
| RDS | PostgreSQL or SQL Server |
| Secrets Manager | DB passwords, JWT secrets |
| CloudWatch | logs, metrics, alarms |
| Cognito | optional managed auth |
| IAM | least privilege |

## ECS deploy flow

```text
build/test -> docker build -> push ECR -> render task definition -> update ECS service -> health checks
```

## SPA deploy flow

```text
npm build -> sync dist to S3 -> invalidate CloudFront -> browser loads new index.html
```

## Health checks

Spring:

```text
/actuator/health
/actuator/health/liveness
/actuator/health/readiness
```

Expose only needed actuator endpoints.

## Common bugs

| Symptom | Likely causes |
|---|---|
| 401 loop | expired token, bad issuer/audience, missing header, clock skew |
| CORS failure | origin mismatch, missing `Authorization` header, bad preflight |
| N+1 | lazy relationship in loop |
| Slow list | missing composite index, huge page, `%term%` scan |
| Flyway checksum mismatch | edited applied migration |
| ECS unhealthy | wrong port/path, DB unavailable, secrets missing |
| Angular form no field errors | Problem Details shape mismatch |
| React stale list | missing query invalidation |

## Senior trade-off lines

- "I would start modular before microservices."
- "DTOs keep the API contract separate from persistence."
- "The frontend validates for UX; the backend validates for trust."
- "A unique database constraint is the race-safe enforcement."
- "I would use Testcontainers because H2 hides dialect differences."
- "For production browser auth, I would evaluate Cognito PKCE or BFF cookies instead of long-lived localStorage tokens."
- "Migrations need to be compatible with rolling deploys."
- "Indexes should follow access patterns, not every possible column."

## Final readiness checklist

- [ ] Can explain Spring request lifecycle.
- [ ] Can design REST endpoints.
- [ ] Can implement Problem Details.
- [ ] Can secure endpoints with JWT.
- [ ] Can prevent IDOR.
- [ ] Can map validation errors in React/Angular.
- [ ] Can design PostgreSQL/SQL Server indexes.
- [ ] Can run Docker Compose locally.
- [ ] Can describe AWS deployment.
- [ ] Can debug auth, CORS, database, and ECS failures.
