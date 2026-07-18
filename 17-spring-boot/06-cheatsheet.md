# 06 - Spring Boot Cheatsheet

Fast review for Spring Boot 3.x, Spring Framework 6.x, and Java 17+ interviews.

## Core annotations

- `@SpringBootApplication` = configuration + auto-configuration + component scan.
- `@Configuration` declares bean definitions.
- `@Bean` registers a method return value as a bean.
- `@Component` generic scanned bean.
- `@Service` application/business service.
- `@Repository` persistence component with exception translation.
- `@RestController` controller + response body.
- `@ControllerAdvice` / `@RestControllerAdvice` global MVC advice.
- `@ConfigurationProperties` typed external configuration.
- `@Profile` conditional bean/config activation.

## REST

- Use DTOs, not entities, at API boundaries.
- `@Valid @RequestBody` triggers Jakarta Bean Validation.
- Return 201 + Location for creation.
- Use 400 validation, 401 unauthenticated, 403 forbidden, 404 not found, 409 conflict, 422 only if your API standard chooses it.
- Use `ProblemDetail` for structured errors.
- Document contracts with OpenAPI/springdoc.

## JPA

- Repositories are abstractions; SQL still matters.
- Default lazy for collections; avoid eager object graphs.
- Fix N+1 with fetch joins, entity graphs, projections, batch size, or query redesign.
- `@Version` enables optimistic locking.
- Use migrations instead of production `ddl-auto=update`.
- Keep transaction boundaries at use-case/service level.

## Transactions

- Default rollback: unchecked exceptions and `Error`.
- Self-invocation bypasses proxy annotations.
- Keep transactions short; avoid remote calls inside.
- `readOnly=true` is a hint, not a universal write firewall.
- Choose isolation only when you understand anomalies and database support.

## Security

- Spring Security 6 uses `SecurityFilterChain` bean style.
- Stateless APIs use `SessionCreationPolicy.STATELESS`.
- JWT must be validated: signature, expiration, issuer, audience if applicable.
- CORS is browser policy, not auth.
- Method security can protect service methods with `@PreAuthorize`.

## Testing

- Unit tests: no Spring needed for pure domain/service logic.
- `@WebMvcTest`: controller slice, validation, serialization, exception mapping.
- `@DataJpaTest`: repository slice.
- `@SpringBootTest`: full context/integration.
- Testcontainers catches vendor-specific SQL behavior.

## Operations

- Expose Actuator endpoints intentionally.
- Use health probes for liveness/readiness.
- Metrics should avoid high-cardinality labels.
- Set timeouts for all external calls.
- Size HikariCP with database capacity and app replicas in mind.

## AWS

- Use AWS SDK v2 clients as singleton beans.
- Use `DefaultCredentialsProvider`; avoid static keys.
- S3 keys are object identifiers, not real folders.
- SQS is at-least-once; consumers must be idempotent.
- Cognito JWTs can be validated by OAuth2 resource server config.

## Common dependency starters

```xml
<dependency>spring-boot-starter-web</dependency>
<dependency>spring-boot-starter-validation</dependency>
<dependency>spring-boot-starter-data-jpa</dependency>
<dependency>spring-boot-starter-security</dependency>
<dependency>spring-boot-starter-oauth2-resource-server</dependency>
<dependency>spring-boot-starter-actuator</dependency>
<dependency>spring-boot-starter-cache</dependency>
<dependency>spring-boot-starter-test</dependency>
```

## Interview phrases that land well

- "I would put the transaction boundary at the use-case service because it defines the consistency unit."
- "I would not return entities directly because API shape, lazy loading, and persistence details should not leak."
- "I would verify this repository method with generated SQL and an execution plan, not only a unit test."
- "I would use a migration tool because schema is part of the deployed contract."
- "I would set timeouts before retries because retries without deadlines can amplify outages."
- "I would keep SQS consumers idempotent because delivery is at least once."
- "I would size the connection pool from total database capacity divided by replicas, then measure."
- "I would expose only safe Actuator endpoints publicly and secure diagnostics."

## Command snippets

```bash
# Run with a profile
SPRING_PROFILES_ACTIVE=postgres ./mvnw spring-boot:run

# Show auto-configuration report in logs
./mvnw spring-boot:run -Dspring-boot.run.arguments=--debug

# Run tests
./mvnw test

# Build without tests only when CI still runs tests elsewhere
./mvnw -DskipTests package
```

## Annotation quick map

| Annotation | Layer | Remember |
| --- | --- | --- |
| `@RestController` | API | JSON response body by default. |
| `@RequestMapping` | API | Class-level base path and shared metadata. |
| `@Valid` | API | Triggers Bean Validation on request DTOs. |
| `@Service` | Application | Good place for use-case orchestration and transactions. |
| `@Transactional` | Application/persistence | Proxy-based unit of work; default rollback on unchecked exceptions. |
| `@Repository` | Persistence | Persistence stereotype and exception translation. |
| `@Entity` | Persistence | JPA-managed persistent type. |
| `@Version` | Persistence | Optimistic locking column. |
| `@ConfigurationProperties` | Config | Typed config, supports validation. |
| `@Scheduled` | Jobs | Runs on scheduler; cluster behavior is your responsibility. |
| `@Async` | Async | Requires executor and proxy call path. |
| `@PreAuthorize` | Security | Method-level authorization expression. |

## Status code memory aid

- `200 OK`: successful read/update with body.
- `201 Created`: resource created; include `Location`.
- `202 Accepted`: work accepted asynchronously.
- `204 No Content`: successful delete/update without body.
- `400 Bad Request`: invalid syntax/validation.
- `401 Unauthorized`: missing/invalid authentication.
- `403 Forbidden`: authenticated but not allowed.
- `404 Not Found`: resource missing or intentionally hidden.
- `409 Conflict`: state conflict such as duplicate SKU or optimistic lock.
- `429 Too Many Requests`: rate limited.
- `500 Internal Server Error`: unexpected server bug.
- `503 Service Unavailable`: dependency/service temporarily unavailable.

## JPA relationship memory aid

- `@ManyToOne` is commonly the owning side because the table has the foreign key.
- `mappedBy` points to the field on the owning side.
- `cascade` means operations flow from parent to child; do not casually cascade from child to parent.
- `orphanRemoval` deletes children removed from the parent collection.
- Lazy loading needs an open persistence context and careful DTO mapping.

## Resilience pattern memory aid

- Timeout: prevents waiting forever.
- Retry: tries again when the operation is safe and failure is transient.
- Circuit breaker: stops calling a failing dependency temporarily.
- Bulkhead: limits concurrency to isolate failure.
- Rate limiter: controls request rate.
- Fallback: returns degraded response, queued work, or clear unavailable state.

## Database vendor reminders

- PostgreSQL: `uuid`, `jsonb`, `timestamptz`, `ILIKE`, `on conflict`, GIN indexes.
- MSSQL: `uniqueidentifier`, `nvarchar(max)` JSON with functions, `datetime2`/`datetimeoffset`, collations, `offset fetch`, included columns.
- Both: indexes cost writes, large offsets are expensive, query plans matter.

## AWS reminders

- `DefaultCredentialsProvider` checks environment, system properties, web identity, profiles, ECS/EC2 metadata, and related sources.
- ECS task role is for app AWS API calls; execution role is for pulling images/logging.
- SQS standard queues can duplicate and reorder messages.
- S3 bucket public access should be blocked by default.
- Secrets should not be logged, cached forever, or fetched per request.

## Last-minute whiteboard structure

1. API contract and validation.
2. Security/authz.
3. Use-case service and transaction boundary.
4. Persistence model, queries, indexes, migration.
5. External calls/messaging with resilience.
6. Observability and operations.
7. Testing strategy.
8. Deployment and rollback.
