# 07 - Spring Boot Interview Questions

Use these questions for active recall. Answer out loud before reading the bullets. Strong answers combine definition, mechanism, production implication, and a trade-off.

## 1. What is the difference between Spring Framework and Spring Boot?

- Spring Framework provides the core container, dependency injection, MVC, transactions, AOP, data integration, and many abstractions.
- Spring Boot adds opinionated auto-configuration, starter dependencies, embedded servers, Actuator, and production conventions.
- Boot reduces setup but does not remove the need to understand Spring fundamentals.

## 2. What does `@SpringBootApplication` do?

- It combines `@SpringBootConfiguration`, `@EnableAutoConfiguration`, and `@ComponentScan`.
- It marks the main configuration class, enables conditional auto-configuration, and scans components from its package downward.
- Package placement matters because sibling/parent packages may not be scanned.

## 3. Why prefer constructor injection?

- Required dependencies are explicit and can be `final`.
- It improves testability because collaborators can be passed directly.
- It avoids hidden mutable state and makes circular dependencies more obvious.

## 4. What is auto-configuration?

- Conditional bean registration based on classpath, properties, existing beans, and environment.
- It is implemented through Boot auto-configuration classes and conditions.
- You can inspect it with debug logs or Actuator conditions.

## 5. How do profiles work?

- Profiles activate profile-specific properties and beans.
- They can be set with `spring.profiles.active`, environment variables, command-line args, or deployment config.
- Use them for environment/vendor differences, not uncontrolled feature sprawl.

## 6. Why not return JPA entities from controllers?

- Entities expose persistence shape and can leak fields.
- Lazy relationships can trigger serialization errors or N+1 queries.
- DTOs protect API contracts, validation rules, and versioning.

## 7. How do you handle validation in Boot 3?

- Use Jakarta Bean Validation annotations from `jakarta.validation` on request DTOs.
- Add `@Valid` to controller parameters.
- Map validation failures to structured errors, often `ProblemDetail`.

## 8. What is `ProblemDetail`?

- A Spring 6 representation of RFC 7807 problem details.
- It provides structured fields such as type, title, status, detail, and instance.
- It helps clients parse errors consistently.

## 9. What is the role of `@ControllerAdvice`?

- It centralizes MVC advice such as exception handling and binding customization.
- `@RestControllerAdvice` combines advice with response body behavior.
- It keeps controllers focused on happy-path request handling.

## 10. What is Actuator used for?

- Operational endpoints such as health, metrics, info, env, mappings, and conditions.
- It supports readiness/liveness probes and monitoring integrations.
- Sensitive endpoints must be secured and exposed intentionally.

## 11. What is Spring Data JPA?

- A repository abstraction over JPA providers such as Hibernate.
- It provides CRUD, derived queries, paging, sorting, and custom queries.
- It does not eliminate SQL or database tuning concerns.

## 12. Explain N+1 queries.

- One query loads parent rows, then one additional query per parent loads children.
- It often appears with lazy relationships during iteration/serialization.
- Fixes include fetch joins, entity graphs, projections, batch fetching, or query redesign.

## 13. What does `@Transactional` do?

- It opens/joins a transaction around a proxied method invocation.
- It controls propagation, isolation, read-only hints, timeout, and rollback rules.
- By default, unchecked exceptions trigger rollback.

## 14. What is the self-invocation transaction problem?

- Spring proxy AOP applies advice when calls go through the proxy.
- A method calling another method on `this` bypasses the proxy.
- The inner method annotation may not take effect.

## 15. Why use Flyway or Liquibase?

- They version schema changes in source control.
- They make deployments repeatable and auditable.
- They are safer than relying on Hibernate `ddl-auto=update` in production.

## 16. How do you design zero-downtime migrations?

- Use expand-and-contract changes.
- Add nullable/new structures first, deploy app that writes both or handles both, backfill, then enforce/remove old structures later.
- Keep rolling deployments and old/new app versions in mind.

## 17. How does Spring Security 6 configuration differ from older versions?

- It favors declaring `SecurityFilterChain` beans over extending `WebSecurityConfigurerAdapter`.
- Authorization is configured with lambdas and request matchers.
- Stateless JWT APIs often use OAuth2 resource server configuration.

## 18. What must be validated in a JWT?

- Signature, expiration, issuer, audience/client where applicable, token type, and claims used for authorization.
- Authorities/scopes should be mapped deliberately.
- Decoding without verification is insecure.

## 19. What is CORS?

- A browser security policy controlling cross-origin JavaScript requests.
- Servers respond with allowed origins, methods, headers, and credentials rules.
- It is not authentication and does not affect server-to-server calls.

## 20. When should caching be used?

- For read-heavy, stable, expensive data where stale reads are acceptable or bounded.
- Keys, TTLs, invalidation, and metrics must be designed.
- Avoid caching sensitive/user-specific data without key isolation.

## 21. What is the difference between fixedRate and fixedDelay scheduling?

- `fixedRate` schedules based on start times.
- `fixedDelay` waits a delay after the previous execution completes.
- Long tasks and cluster duplication must be considered.

## 22. How do you make scheduled jobs safe in multiple instances?

- Make them idempotent.
- Use distributed locks, leader election, database claims, or external schedulers when only one instance should run.
- Add metrics and logs for skips, duration, and failures.

## 23. What is WebClient?

- A modern HTTP client from Spring WebFlux that supports reactive APIs.
- It can be used in MVC apps but blocking should be deliberate.
- Configure timeouts, error mapping, and resilience.

## 24. What is the difference between `@WebMvcTest` and `@SpringBootTest`?

- `@WebMvcTest` loads an MVC slice for controllers, JSON, validation, and advice.
- `@SpringBootTest` loads the full application context.
- Use the smallest test that covers the risk.

## 25. Why use Testcontainers?

- It runs real infrastructure such as PostgreSQL/MSSQL in containers for tests.
- It catches dialect and integration behavior H2 may miss.
- It is slower than unit tests but valuable for persistence correctness.

## 26. What is MapStruct?

- A compile-time mapper generator.
- It avoids reflection and produces type-safe mapping code.
- Use it for repetitive mappings, not hidden business logic.

## 27. What is hexagonal architecture?

- An architecture style that separates core domain/application logic from adapters.
- Ports define required or offered capabilities; adapters implement delivery/persistence/integration.
- Spring can wire adapters without infecting the domain when that separation is worthwhile.

## 28. What is CQRS lite?

- Separating command/write models from query/read models without necessarily using event sourcing.
- It can simplify validation and optimize reads.
- Avoid it when simple CRUD is enough.

## 29. What is Spring AOP good for?

- Cross-cutting concerns such as transactions, logging, metrics, auditing, and security checks.
- It is proxy-based by default and has visibility/self-invocation limits.
- Do not hide important business logic in aspects.

## 30. What is a circuit breaker?

- A resilience pattern that stops calls to a failing dependency after a threshold.
- It moves through closed, open, and half-open states.
- It should be combined with timeouts and monitored fallback behavior.

## 31. How are metrics, logs, and traces different?

- Metrics are aggregated numeric measurements.
- Logs are event records.
- Traces follow requests across services with spans and correlation IDs.

## 32. What is high-cardinality metric data?

- Tags/labels with many unique values such as user ID or request ID.
- They can explode storage and query cost.
- Use low-cardinality dimensions for metrics and put request IDs in logs/traces.

## 33. Compare Kafka and SQS.

- Kafka is a distributed log with partitions, offsets, replay, and consumer groups.
- SQS is a managed queue with visibility timeout, at-least-once delivery, and DLQs.
- Both require idempotent consumers, but ordering and replay models differ.

## 34. What is multi-tenancy?

- Serving multiple tenants/customers from one application model.
- Patterns include row discriminator, schema per tenant, and database per tenant.
- It affects security, caching, migrations, routing, and observability.

## 35. How do virtual threads affect Spring apps?

- They make blocking cheaper for many workloads on Java 21+.
- They do not increase database connections or remove downstream limits.
- They require load testing and awareness of pinned threads/synchronization.

## 36. What differs between PostgreSQL and MSSQL in Spring?

- Dialect, pagination SQL, JSON support, UUID types, date/time precision, locking hints, functions, and collations.
- Repositories may look portable while native queries and migrations are not.
- Test against supported real engines.

## 37. How do you size HikariCP?

- Consider database max connections, number of app replicas, average query latency, and workload.
- Start small and measure active/pending connections and acquisition timeouts.
- Oversized pools can overload the database.

## 38. How should JSON columns be used?

- For flexible metadata or integration payloads, not core relational invariants.
- PostgreSQL `jsonb` is more capable than MSSQL text JSON functions.
- Index queried paths and validate shape.

## 39. What is keyset pagination?

- Pagination based on the last seen sort key rather than offset.
- It is efficient for deep pages and changing datasets.
- It requires deterministic ordering and different API tokens/parameters.

## 40. How should Spring apps use AWS credentials?

- Use IAM roles and `DefaultCredentialsProvider`.
- Avoid static access keys in files or environment when role-based auth exists.
- Scope IAM permissions to required actions/resources.

## 41. How do you upload to S3 safely?

- Validate content type/size, generate safe keys, use private buckets, and consider server-side encryption.
- Use pre-signed URLs when clients upload/download directly.
- Do not trust original filenames as keys.

## 42. How do Spring apps connect to RDS?

- Through normal JDBC/JPA datasource configuration.
- Credentials should come from secrets/IAM mechanisms.
- Tune connection pools and test failover behavior.

## 43. Secrets Manager versus Parameter Store?

- Secrets Manager is better for secrets requiring rotation and secret-specific management.
- Parameter Store is good for hierarchical config and simple secure strings.
- Both require IAM and caching/refresh strategy.

## 44. What makes SQS consumers reliable?

- Idempotency, visibility timeout sizing, DLQs, retries with backoff, and observability.
- Delete messages after successful processing.
- Expect duplicates and out-of-order delivery for standard queues.

## 45. How do you integrate Cognito with Spring Security?

- Configure OAuth2 resource server JWT issuer URI/JWK validation.
- Map Cognito groups/scopes/claims to authorities.
- Validate authorization server-side, not only in the frontend.

## 46. What is LocalStack useful for?

- Local integration tests for AWS-like APIs such as S3/SQS/Secrets.
- Fast developer feedback without real AWS resources.
- It does not fully replace staging tests for IAM, performance, or all edge cases.

## Final rapid-fire prompts

- [ ] Explain the request lifecycle through filters, controller, validation, service, transaction, repository, database, response, logs, and metrics.
- [ ] Design a create-product endpoint with duplicate SKU handling.
- [ ] Debug an N+1 query in production.
- [ ] Plan a zero-downtime not-null column migration.
- [ ] Secure an admin endpoint with JWT scopes.
- [ ] Make an SQS consumer idempotent.
- [ ] Tune a slow endpoint backed by PostgreSQL.
- [ ] Move from local config to AWS Secrets Manager safely.

# Scenario Questions and Answer Frameworks

## 47. A product creation endpoint sometimes creates duplicate products during client retries. How do you fix it?

- Add a unique database constraint on the natural/business key, such as SKU, because application checks alone race.
- Add idempotency keys for retryable create commands if clients may repeat the same request after timeouts.
- Return `409 Conflict` for true duplicate business keys and return the original response for a repeated idempotency key with the same request hash.
- Test concurrent requests, not only sequential happy paths.

## 48. Your endpoint is slow and traces show 101 SQL queries. What do you do?

- Identify N+1: one query for parent rows and one per row for a lazy association.
- Fix with projection, fetch join, `@EntityGraph`, batch fetching, or changing the response shape.
- Verify generated SQL and execution plan after the fix.
- Add a test that fails if the query count grows unexpectedly for the use case.

## 49. A migration adding a non-null column failed on production data. How should it have been done?

- Add the column nullable first or with a safe default, depending on data volume and lock behavior.
- Deploy code that writes the column for new/changed rows.
- Backfill existing rows in controlled batches.
- Add the not-null constraint after verifying no nulls remain.
- Consider rolling deployment compatibility between old and new app versions.

## 50. A downstream pricing service is timing out and your app threads are exhausted. What changes do you make?

- Set short connect/read/response timeouts.
- Add circuit breaker and bulkhead isolation for pricing calls.
- Retry only transient failures and only if the operation is safe.
- Return a clear degraded response or unavailable state instead of blocking indefinitely.
- Monitor timeout count, breaker state, and thread/connection pool saturation.

## 51. A scheduled job runs twice after scaling from one pod to three. What happened?

- `@Scheduled` runs in every application instance by default.
- Make the job idempotent and use distributed locking, leader election, database row claiming, or an external scheduler if only one execution is allowed.
- Add metrics and logs for claimed, skipped, completed, and failed work.

## 52. Users from one tenant can see cached data from another tenant. What was likely wrong?

- Cache keys omitted tenant/user identity or tenant context was not enforced before cache lookup.
- Include tenant in keys and validate authorization before reads.
- Clear or segregate caches after fixing the bug because bad entries may already exist.
- Review metrics/log tags to avoid high-cardinality explosion while still supporting incident investigation.

## 53. A JWT-authenticated endpoint accepted a forged token in a test environment. What should you check?

- Whether code decoded the JWT without verifying the signature.
- Whether issuer, audience, expiration, and algorithm constraints were validated.
- Whether test configuration accidentally disabled resource server validation.
- Whether authorization relied on client-provided claims without trusted mapping.

## 54. HikariCP acquisition timeouts are increasing. What are the likely causes?

- Slow queries or locks hold connections too long.
- Pool size is too small for measured workload or too large across replicas for database capacity.
- Transactions include network calls or long CPU work.
- Connections are leaked or not returned due to streaming/lifecycle bugs.
- Database is saturated; increasing the pool may make it worse.

## 55. An SQS consumer processed the same message twice and created duplicate side effects. Is SQS broken?

- No. SQS standard queues provide at-least-once delivery, so duplicates are expected.
- Consumers must be idempotent using message IDs, business keys, unique constraints, or processed-message tables.
- Delete messages only after successful processing.
- Use FIFO deduplication only when its ordering/throughput trade-offs fit.

## 56. A new Actuator endpoint exposed environment variables publicly. How do you prevent this?

- Expose only required endpoints with `management.endpoints.web.exposure.include`.
- Secure sensitive endpoints with Spring Security.
- Use `show-details: when_authorized` for health details.
- Review Actuator exposure as part of deployment/security checks.

## 57. A MapStruct mapper contains pricing business rules. Why is that risky?

- Mappers should translate between shapes; business rules belong in domain/application services where they are discoverable and testable.
- Hidden mapper expressions can create inconsistent behavior across use cases.
- Keep transformations explicit and test important mapping decisions.

## 58. A Spring Boot app works with H2 tests but fails in PostgreSQL. What lesson do you draw?

- H2 does not perfectly emulate PostgreSQL or MSSQL dialects, types, constraints, locking, and query plans.
- Use Testcontainers for repository/migration behavior that depends on the real engine.
- Keep unit tests fast, but do not use H2 as proof of production SQL correctness.

## 59. A team wants to store all product attributes in a JSON column. How do you respond?

- JSON is fine for flexible metadata, sparse attributes, or integration payloads.
- Core business fields used for filtering, constraints, relationships, and reporting should usually be relational.
- If querying JSON paths, add appropriate indexes and validate payload shape.
- Consider schema evolution and API contracts.

## 60. How do you explain graceful degradation?

- The system preserves core behavior while non-critical dependencies fail.
- Examples: show product without recommendations, accept an order for later fraud review, queue work instead of blocking.
- Degradation must be visible in metrics/logs and honest in the API response.
- Never silently return incorrect business data as a fallback.

## 61. How would you design tests for a secured product API?

- Unit test domain/service authorization-sensitive logic where applicable.
- Use `@WebMvcTest` with mock users/JWTs for endpoint status, validation, and access rules.
- Use full integration tests for the real security filter chain and token validation configuration.
- Test unauthenticated, authenticated-insufficient-scope, authorized, and malformed-token cases.

## 62. How do you prevent secrets from leaking in a Spring Boot app?

- Do not commit secrets; load from environment, vault, Secrets Manager, or platform secret mechanisms.
- Avoid logging config maps, request headers, tokens, or exception messages containing secrets.
- Restrict Actuator env/configprops endpoints.
- Use IAM roles and least privilege.
- Rotate credentials and rehearse revocation.

## 63. A full `@SpringBootTest` suite is slow. What do you do?

- Move pure logic to unit tests without Spring.
- Use test slices such as `@WebMvcTest` and `@DataJpaTest` where possible.
- Avoid excessive context variations because each unique context may start separately.
- Keep a smaller number of full integration tests for cross-layer confidence.

## 64. A message listener publishes an event and updates the database, but sometimes only one happens. How do you fix consistency?

- Use a transaction for database updates.
- Use outbox pattern when publishing must be reliably tied to the database state.
- Make downstream consumers idempotent.
- Add retry/DLQ handling and monitoring.

## 65. How do you choose between WebClient and RestClient?

- `RestClient` is a modern synchronous client for straightforward blocking MVC applications.
- `WebClient` supports reactive composition and is common where non-blocking flows or existing WebFlux dependencies matter.
- Either way, configure timeouts, error mapping, observability, and resilience.
- Do not block on event-loop threads in reactive applications.

## 66. What makes a senior Spring Boot answer different?

- It connects annotation behavior to runtime mechanics.
- It includes failure modes, observability, security, and deployment implications.
- It chooses patterns based on requirements instead of reciting names.
- It mentions how to test and operate the solution.

# Interview Self-Scoring Rubric

## For every answer, check whether you included

- Definition: what the concept is.
- Mechanism: how Spring/Java/database/AWS makes it work.
- Trade-off: when to use it and when not to.
- Failure mode: how it breaks in production.
- Test strategy: how to prove behavior.
- Operational signal: what metric/log/trace/alert would show health.

## Warning signs in your own answer

- You say "Spring handles it" without naming the mechanism.
- You rely on H2 for database-specific claims.
- You say "add retry" without mentioning timeout or idempotency.
- You discuss JWTs without signature validation.
- You discuss caching without invalidation.
- You discuss transactions without rollback or proxy limitations.
- You discuss AWS without IAM roles and least privilege.
- You discuss performance without measurement.
