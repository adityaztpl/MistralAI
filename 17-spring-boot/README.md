# Spring Boot 3 Interview and Prep Curriculum

This curriculum is designed for modern Spring Boot 3.x, Spring Framework 6.x, and Java 17+. It is intentionally interview-focused: every topic connects framework mechanics to production decisions, failure modes, and the kind of trade-off questions senior interviewers ask.

## How to use this folder

1. Read the files in order unless you are preparing for a specific interview area.
2. Type the examples into a small Boot 3 project instead of only reading them.
3. For every checklist item, explain the idea out loud in two levels: junior explanation first, then production-level trade-offs.
4. After each guide, build a tiny feature: controller -> service -> repository -> migration -> test.
5. Revisit `06-cheatsheet.md` during the final week and answer `07-interview-questions.md` without looking.

## Learning order

### Week 1: Spring Boot basics, inversion of control, REST, validation, profiles, Actuator

- Read the matching guide completely.
- Create or extend one runnable Boot 3 sample application.
- Write at least one unit test and one Spring integration/slice test.
- Update your personal notes with one production incident this topic could cause.
- Practice explaining the topic in 90 seconds, then in 5 minutes.

### Week 2: Data access with JPA, schema migration, transactions, DTO mapping

- Read the matching guide completely.
- Create or extend one runnable Boot 3 sample application.
- Write at least one unit test and one Spring integration/slice test.
- Update your personal notes with one production incident this topic could cause.
- Practice explaining the topic in 90 seconds, then in 5 minutes.

### Week 3: Security, JWT, CORS, testing slices, integration tests, caching

- Read the matching guide completely.
- Create or extend one runnable Boot 3 sample application.
- Write at least one unit test and one Spring integration/slice test.
- Update your personal notes with one production incident this topic could cause.
- Practice explaining the topic in 90 seconds, then in 5 minutes.

### Week 4: Architecture, resilience, observability, messaging, cloud integrations

- Read the matching guide completely.
- Create or extend one runnable Boot 3 sample application.
- Write at least one unit test and one Spring integration/slice test.
- Update your personal notes with one production incident this topic could cause.
- Practice explaining the topic in 90 seconds, then in 5 minutes.

### Week 5: Database depth: PostgreSQL and MSSQL behavior, performance, migrations

- Read the matching guide completely.
- Create or extend one runnable Boot 3 sample application.
- Write at least one unit test and one Spring integration/slice test.
- Update your personal notes with one production incident this topic could cause.
- Practice explaining the topic in 90 seconds, then in 5 minutes.

### Week 6: AWS integration, deployment trade-offs, system-design interview practice

- Read the matching guide completely.
- Create or extend one runnable Boot 3 sample application.
- Write at least one unit test and one Spring integration/slice test.
- Update your personal notes with one production incident this topic could cause.
- Practice explaining the topic in 90 seconds, then in 5 minutes.

## Files in this curriculum

- `01-basics.md` - Boot fundamentals, core annotations, dependency injection, REST, validation, exceptions, DTOs, OpenAPI.
- `02-intermediate.md` - Data access, transactions, migrations, security/JWT, CORS, caching, scheduling, WebClient, testing, MapStruct.
- `03-advanced.md` - Architecture, CQRS-lite, AOP, resilience, observability, messaging, multi-tenancy, performance, virtual threads.
- `04-data-mssql-postgresql.md` - MSSQL vs PostgreSQL in Spring, dialects, HikariCP, indexes, JSON, pagination, stored procedures, profiles.
- `05-aws-with-spring.md` - AWS SDK v2 integration: S3, RDS, Secrets Manager, Parameter Store, SQS, Cognito, IAM roles, Spring Cloud AWS, LocalStack.
- `06-cheatsheet.md` - Fast review reference for annotations, test slices, dependencies, config, and interview phrases.
- `07-interview-questions.md` - 40+ interview questions with concise but substantial answers.

## Master checklist

- [ ] Explain what `@SpringBootApplication` expands to and why component scanning starts from its package.
- [ ] Use constructor injection and explain why field injection hurts tests and immutability.
- [ ] Design a REST endpoint with request DTO, validation, service call, response DTO, and `ProblemDetail` errors.
- [ ] Separate entity models from API DTOs and explain over-posting, lazy loading, and versioning concerns.
- [ ] Configure `application.yml` with environment variables, profiles, typed configuration properties, and safe defaults.
- [ ] Expose Actuator health/info/metrics safely without leaking sensitive endpoints.
- [ ] Model JPA relationships and explain owning side, cascade, orphan removal, fetch type, and N+1.
- [ ] Use `@Transactional` correctly, including rollback rules and self-invocation limitations.
- [ ] Apply Flyway or Liquibase migrations as the source of truth for schema changes.
- [ ] Secure APIs with Spring Security 6, stateless sessions, JWT resource server concepts, and method security.
- [ ] Explain CORS as a browser policy and configure it at the security filter chain level.
- [ ] Use caching only for stable reads and define eviction/invalidation explicitly.
- [ ] Schedule background jobs safely with idempotency and cluster-awareness.
- [ ] Call external APIs with `WebClient`, timeouts, retries, and error mapping.
- [ ] Write `@WebMvcTest`, repository tests, and full `@SpringBootTest` integration tests with Testcontainers when useful.
- [ ] Map DTOs with MapStruct and know when hand mapping is clearer.
- [ ] Describe clean/hexagonal architecture and keep domain logic independent of Spring annotations when valuable.
- [ ] Use AOP for cross-cutting concerns without hiding core business behavior.
- [ ] Apply Resilience4j circuit breakers, retries, bulkheads, rate limiters, and time limiters appropriately.
- [ ] Export metrics/traces/log correlation with Micrometer and OpenTelemetry concepts.
- [ ] Compare Kafka/SQS delivery semantics and design idempotent consumers.
- [ ] Explain database-specific differences between PostgreSQL and MSSQL that impact migrations and queries.
- [ ] Tune HikariCP based on database capacity instead of arbitrary large pool sizes.
- [ ] Use AWS SDK v2 clients with IAM roles and avoid hard-coded credentials.
- [ ] Prepare a deployment story: config, health checks, migrations, secrets, logs, metrics, rollbacks.

## Suggested mini-project

Build an inventory/order service with these features:

- Products with name, SKU, price, stock quantity, version field, and soft business status.
- REST endpoints for create, update, search, and stock reservation.
- PostgreSQL profile and MSSQL profile with Flyway migrations.
- JWT-protected admin endpoints and public read endpoints.
- Validation and global `ProblemDetail` exception responses.
- Caching for product lookup by SKU with explicit eviction on updates.
- Scheduled cleanup or reconciliation job that is idempotent.
- WebClient integration to a fake pricing/tax service with resilience.
- S3 upload endpoint for product images using pre-signed URLs.
- Tests for controller validation, service rules, repository queries, security, and a full happy path.

## Interview drill routine

1. Draw the request path from HTTP request to controller, validation, security filters, service transaction, repository, database, response, and logs/metrics.
2. Take one endpoint and list every failure mode: invalid input, unauthorized, forbidden, duplicate key, optimistic lock, timeout, deadlock, downstream 500, serialization error.
3. Explain how you would migrate a running system from a nullable column to a required column safely.
4. Explain how you would debug a slow endpoint using metrics, logs, traces, SQL plans, and thread dumps.
5. Compare two implementation choices and state the operational trade-off, not just the API syntax.

## Red flags interviewers look for

- Treating `@SpringBootApplication` as magic without knowing scanning and auto-configuration.
- Returning JPA entities directly from controllers in non-trivial APIs.
- Adding `@Transactional` everywhere without understanding boundaries.
- Ignoring migration tooling and relying on Hibernate `ddl-auto=update` in production.
- Missing timeouts for outbound HTTP calls.
- Using JWT without validating issuer, audience, expiration, and signature.
- Configuring a huge connection pool that overwhelms the database.
- Assuming retry fixes all failures; retries can amplify outages.
- Logging secrets, tokens, request bodies, or PII.
- Not knowing how to test security and validation paths.

## Final readiness rubric

- **Passable:** Can create REST endpoints, wire services/repositories, configure profiles, and answer common annotation questions.
- **Strong:** Can explain transactions, JPA performance, security chain behavior, validation, testing strategy, and database migrations.
- **Senior:** Can design boundaries, reason about failure modes, operate observability, tune persistence, secure cloud integrations, and describe trade-offs clearly.
