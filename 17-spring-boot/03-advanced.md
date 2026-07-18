# 03 - Advanced Spring Boot

This guide prepares for senior-level design and operations discussions. The goal is to reason about boundaries, failures, observability, performance, and deployment realities rather than only annotation syntax.

## Outcomes

- [ ] Design Spring applications with clean or hexagonal boundaries when useful.
- [ ] Apply CQRS-lite without overengineering.
- [ ] Use AOP responsibly.
- [ ] Add resilience and observability with production trade-offs.
- [ ] Discuss async messaging, multi-tenancy, performance, and virtual threads.

## Clean architecture and hexagonal Spring

Clean and hexagonal architecture keep business rules independent from frameworks and delivery mechanisms. Spring becomes an adapter and composition tool rather than the center of the domain model. This is especially useful in systems where business rules outlive HTTP, database, or message broker choices.

**Interview focus**

- Ports and adapters.
- Inbound adapters such as controllers and message listeners.
- Outbound adapters such as repositories, HTTP clients, and queues.

**Production checklist**

- [ ] Define use cases in application services.
- [ ] Keep domain decisions in domain objects or services, not controllers.
- [ ] Let Spring annotations live mostly at adapter/configuration boundaries when the domain benefits from independence.

**Code example**

```text
product/domain        Product, Money, StockPolicy
product/application   CreateProductUseCase, ReserveStockUseCase
product/port/out      ProductStore, EventPublisher
product/adapter/web   ProductController
product/adapter/jpa   JpaProductStore
```

**Common pitfalls**

- Creating excessive layers for simple CRUD.
- Claiming clean architecture while the domain still imports JPA and Web classes everywhere.

## CQRS lite

CQRS lite separates command paths that change state from query paths that read optimized views without necessarily introducing event sourcing or separate databases. In Spring services, this often means separate request models, service methods, repositories/projections, and transaction settings.

**Interview focus**

- Command versus query responsibilities.
- Read models/projections.
- When full CQRS is overkill.

**Production checklist**

- [ ] Keep writes authoritative and validated.
- [ ] Use projections for read-heavy screens.
- [ ] Avoid premature eventual consistency unless there is a real need.

**Code example**

```java
@Transactional
public ProductId handle(CreateProductCommand command) {
    Product product = Product.create(command);
    productStore.save(product);
    return product.id();
}

@Transactional(readOnly = true)
public Page<ProductSummary> search(ProductSearchQuery query) {
    return productReadModel.search(query.term(), query.pageable());
}
```

**Common pitfalls**

- Calling every DTO split CQRS.
- Duplicating business rules in read models.

## AOP

Spring AOP applies cross-cutting behavior around matched method executions, usually through proxies. It is useful for logging, metrics, authorization checks, retries, auditing, and transactions, but can make flow harder to follow if overused.

**Interview focus**

- Proxy-based AOP limitations.
- Pointcuts and advice types.
- Transactions as a common Spring AOP use case.

**Production checklist**

- [ ] Use AOP for cross-cutting concerns only.
- [ ] Keep pointcuts narrow and tested.
- [ ] Document aspects because they are not visible at call sites.

**Code example**

```java
@Around("@annotation(audited)")
public Object audit(ProceedingJoinPoint pjp, Audited audited) throws Throwable {
    long start = System.nanoTime();
    try {
        return pjp.proceed();
    } finally {
        metrics.record(audited.value(), System.nanoTime() - start);
    }
}
```

**Common pitfalls**

- Using AOP to hide important business branching.
- Expecting aspects on private methods or self-invocation with proxy AOP.

## Resilience4j

Resilience4j provides circuit breakers, retries, rate limiters, bulkheads, and time limiters. The goal is to fail fast, limit damage, and recover gracefully when dependencies degrade. Each pattern has trade-offs; combining them blindly can cause worse outages.

**Interview focus**

- Circuit breaker states: closed, open, half-open.
- Retry versus circuit breaker versus timeout.
- Bulkhead isolation.

**Production checklist**

- [ ] Always set timeouts before retries.
- [ ] Retry only safe/idempotent operations or use idempotency keys.
- [ ] Emit metrics for breaker state and fallback usage.

**Code example**

```java
@CircuitBreaker(name = "pricing", fallbackMethod = "fallbackPrice")
@TimeLimiter(name = "pricing")
public CompletableFuture<PriceQuote> quote(Product product) {
    return CompletableFuture.supplyAsync(() -> pricingClient.quote(product.sku()));
}
```

**Common pitfalls**

- Retrying every failure including validation errors.
- Fallbacks returning misleading success data.

## Observability with Micrometer and OpenTelemetry

Observability combines metrics, logs, and traces so teams can infer system behavior from outputs. Micrometer is the instrumentation facade used by Spring Boot. OpenTelemetry standardizes traces, metrics, and logs across tools.

**Interview focus**

- Metrics versus logs versus traces.
- RED/USE metrics.
- Trace/span/correlation IDs.

**Production checklist**

- [ ] Add low-cardinality metrics.
- [ ] Propagate trace IDs through logs and outbound calls.
- [ ] Create dashboards and alerts around symptoms, not every internal variable.

**Code example**

```java
Counter.builder("product.created")
    .tag("source", "api")
    .register(meterRegistry)
    .increment();
```

**Common pitfalls**

- High-cardinality tags such as user ID or request ID in metrics.
- Logging sensitive request payloads.

## Async and messaging concepts

Asynchronous processing decouples request latency from background work and improves resilience when combined with idempotency and observability. Spring supports `@Async`, application events, Kafka listeners, SQS listeners, and more. The platform choice changes delivery semantics.

**Interview focus**

- `@Async` thread pools and exception handling.
- Kafka partitions, offsets, consumer groups.
- SQS visibility timeout and at-least-once delivery.

**Production checklist**

- [ ] Design idempotent consumers.
- [ ] Use dead-letter queues/topics.
- [ ] Record message IDs and business keys to deduplicate.

**Code example**

```java
@KafkaListener(topics = "product-events", groupId = "inventory-service")
void onProductEvent(ProductEvent event) {
    handler.handle(event);
}
```

**Common pitfalls**

- Assuming exactly-once end-to-end processing.
- Doing long blocking work with an undersized listener thread pool.

## Multi-tenancy

Multi-tenancy means serving multiple customers/tenants from one application model. Common patterns are discriminator column, schema per tenant, or database per tenant. Spring implementation touches security, datasource routing, Hibernate filters, migrations, caching, and observability.

**Interview focus**

- Tenant identification from JWT/header/domain.
- Database-per-tenant versus schema-per-tenant versus row-level tenancy.
- Tenant context propagation.

**Production checklist**

- [ ] Validate tenant access server-side.
- [ ] Include tenant in cache keys and metrics carefully.
- [ ] Automate migrations for every tenant schema/database.

**Code example**

```java
class TenantContext {
    private static final ThreadLocal<String> CURRENT = new ThreadLocal<>();
    static void set(String tenant) { CURRENT.set(tenant); }
    static String get() { return CURRENT.get(); }
    static void clear() { CURRENT.remove(); }
}
```

**Common pitfalls**

- Trusting an arbitrary `X-Tenant-Id` header without authorization.
- Forgetting to clear ThreadLocal tenant context.

## Performance

Spring performance work starts with evidence: metrics, traces, SQL logs, heap/thread profiles, and load tests. Common bottlenecks include N+1 queries, missing indexes, oversized JSON, connection pool starvation, slow downstream calls, excessive full-context tests, and memory churn.

**Interview focus**

- Latency percentiles, throughput, saturation, and error rates.
- JVM heap, GC, thread pools, connection pools.
- SQL query plans and index usage.

**Production checklist**

- [ ] Set timeouts everywhere.
- [ ] Paginate large lists.
- [ ] Tune HikariCP to database capacity.
- [ ] Use projections to avoid loading unnecessary columns/relationships.

**Code example**

```yaml
spring:
  datasource:
    hikari:
      maximum-pool-size: 10
      connection-timeout: 2000
```

**Common pitfalls**

- Optimizing code before measuring.
- Increasing pool sizes until the database collapses.

## Virtual threads note

Java 21 virtual threads can improve scalability for blocking workloads by making blocking cheaper at the thread level. Spring Boot 3.2+ can enable virtual threads for supported executors. They do not remove database limits, remote service limits, synchronization bottlenecks, or the need for timeouts.

**Interview focus**

- Platform threads versus virtual threads.
- Good fit: blocking MVC/database/HTTP workloads.
- Limits: pinned threads, synchronized blocks, database connections.

**Production checklist**

- [ ] Use Java 21+ if enabling virtual threads.
- [ ] Load test before and after.
- [ ] Keep connection pools and downstream limits realistic.

**Code example**

```yaml
spring:
  threads:
    virtual:
      enabled: true
```

**Common pitfalls**

- Assuming virtual threads replace reactive programming in all cases.
- Forgetting that a blocked database connection is still a scarce resource.

## Advanced design review checklist

- [ ] Every endpoint has an explicit owner/use case and clear transaction boundary.
- [ ] Domain rules are testable without starting Spring when they are complex.
- [ ] Every external call has timeout, retry policy if appropriate, circuit breaker where justified, and error mapping.
- [ ] Every async consumer is idempotent and has a DLQ/retry story.
- [ ] Metrics use low-cardinality tags and include latency, errors, saturation, and business counters.
- [ ] Logs include correlation IDs and exclude secrets/PII.
- [ ] Database queries are paginated and key queries have indexes verified by plans.
- [ ] Cache keys include all dimensions that affect the value, including tenant/user when relevant.
- [ ] Deployment supports health probes, graceful shutdown, migrations, rollback, and configuration validation.
- [ ] Performance claims are backed by measurement.

## System-design interview framing

- Start with requirements: throughput, latency, consistency, availability, data retention, tenant model, security, and compliance.
- Draw boundaries: API layer, use cases, domain, persistence adapter, external service adapters, messaging, observability.
- State failure modes: database down, dependency slow, duplicate messages, partial deployment, stale cache, tenant isolation bug.
- Choose patterns by pressure: use outbox for reliable publication, circuit breaker for unstable dependency, projections for read scaling, queue for work decoupling.
- Explain operational controls: dashboards, alerts, feature flags, rate limits, backpressure, rollbacks, runbooks.

## Deep dive: choosing an architecture style

### When plain layered architecture is enough

- The service is mostly CRUD with limited business rules.
- The team is small and onboarding speed matters more than strict boundaries.
- The persistence model and API model are unlikely to diverge heavily.
- Most changes are adding fields, simple validations, and reporting queries.
- You still keep controllers thin, services transactional, repositories focused, and DTOs separate.

### When hexagonal boundaries start paying off

- Business rules are complex enough to test without Spring or infrastructure.
- Multiple delivery mechanisms exist: REST, messaging, scheduled jobs, batch imports, CLI tools.
- Multiple persistence/integration implementations are likely: database, external API, in-memory test adapter, legacy adapter.
- You need to isolate domain rules from JPA lifecycle, lazy loading, and framework annotations.
- Team boundaries map naturally to capabilities or bounded contexts.

### Interview answer pattern

1. Start with complexity and change pressure.
2. State the simplest architecture that meets the requirements.
3. Add ports/adapters only where the boundary protects a real decision.
4. Keep Spring as composition/infrastructure unless the domain is simple enough that annotation coupling is acceptable.
5. Explain how tests prove the boundary: domain tests without Spring, adapter tests with Spring/Testcontainers.

## Deep dive: outbox pattern with Spring

The outbox pattern stores domain events in the same database transaction as the business state change. A separate publisher reads unsent rows and publishes them to Kafka, SQS, SNS, or another broker. This avoids the classic problem where the database commit succeeds but message publication fails, or the message publishes before the database rolls back.

### Minimal schema idea

```sql
create table outbox_event (
  id uuid primary key,
  aggregate_type varchar(80) not null,
  aggregate_id varchar(120) not null,
  event_type varchar(120) not null,
  payload jsonb not null,
  created_at timestamp with time zone not null,
  published_at timestamp with time zone null,
  publish_attempts int not null default 0
);

create index ix_outbox_unpublished on outbox_event (published_at, created_at);
```

### Spring implementation sketch

```java
@Transactional
public ProductId createProduct(CreateProductCommand command) {
    Product product = Product.create(command);
    productStore.save(product);
    outboxStore.append(ProductCreatedEvent.from(product));
    return product.id();
}

@Scheduled(fixedDelayString = "${app.outbox.publish-delay:PT5S}")
void publishOutbox() {
    List<OutboxEvent> batch = outboxStore.claimNextBatch(100);
    for (OutboxEvent event : batch) {
        publisher.publish(event);
        outboxStore.markPublished(event.id());
    }
}
```

### Production concerns

- Claim rows with locking so multiple app instances do not publish the same row simultaneously.
- Consumers must still be idempotent because publisher retries can duplicate messages.
- Add metrics: unpublished count, publish latency, attempts, failures, DLQ count.
- Keep payload schemas versioned; messages become contracts.
- Decide cleanup/retention strategy for published rows.

## Deep dive: idempotency keys

Idempotency keys let clients safely retry commands such as payment capture, order creation, or stock reservation. The server stores the key, request fingerprint, status, and response reference. A duplicate request with the same key returns the original outcome instead of creating duplicate side effects.

### Good candidates

- Payment operations.
- Order submission.
- Inventory reservation.
- External webhooks.
- Message consumers where broker redelivery is expected.

### Bad candidates

- Simple reads where HTTP GET is already safe.
- Operations where duplicate semantics are intentionally meaningful.
- Requests with no stable client/business key and no way to compare payloads.

### Database shape

```sql
create table idempotency_record (
  key varchar(120) primary key,
  request_hash varchar(128) not null,
  status varchar(30) not null,
  response_body text null,
  created_at timestamp not null,
  expires_at timestamp not null
);
```

### Interview trade-off

Idempotency adds storage, cleanup, and concurrency handling, but it is often cheaper than reconciling duplicate money movement or duplicate orders. The key should be scoped to user/tenant and operation to prevent accidental collisions or replay across principals.

## Deep dive: graceful shutdown

Spring Boot supports graceful shutdown so in-flight requests can complete before the JVM exits. This matters in Kubernetes/ECS rolling deployments because the load balancer may still send traffic briefly while the app is terminating.

```yaml
server:
  shutdown: graceful
spring:
  lifecycle:
    timeout-per-shutdown-phase: 30s
```

### Production checklist

- Readiness should fail before the app stops accepting traffic.
- Long-running requests and consumers need bounded processing times.
- Message listeners should stop polling and finish/abandon work safely.
- Connection pools and SDK clients should close cleanly.
- Deployment termination grace period should exceed the app shutdown timeout.

## Deep dive: AOP and proxy mechanics

Spring AOP commonly uses JDK dynamic proxies for interfaces or CGLIB proxies for classes. Advice applies to method calls that pass through the proxy. This explains why `@Transactional`, `@Async`, caching annotations, and custom aspects can fail on private methods, final methods, or self-invocation.

### What to say in interviews

- "The annotation is implemented by infrastructure around a bean, not by changing Java method semantics." 
- "If `this.someMethod()` is called inside the same object, the proxy is bypassed." 
- "I would move the annotated method to another bean or call through a properly designed use-case boundary instead of injecting self as a workaround." 

## Deep dive: resilience composition order

A resilient outbound call generally needs a deadline first. Then decide if retries are safe. Then decide if a circuit breaker should stop calls during dependency failure. Then isolate concurrency with a bulkhead if one dependency can starve the whole app.

### Example policy for a pricing API

- Timeout: 800 ms per attempt.
- Retry: 2 attempts for 429, 502, 503, 504, connection reset; no retry for 400/401/403/404.
- Circuit breaker: open when failure rate exceeds 50% after at least 20 calls.
- Bulkhead: at most 25 concurrent pricing calls.
- Fallback: return "price unavailable" state, not a fake zero price.
- Metrics: attempts, final outcome, fallback count, breaker state, latency percentiles.

## Deep dive: performance investigation runbook

1. Confirm the symptom: p95/p99 latency, error rate, CPU, memory, queue depth, database wait, downstream latency.
2. Find whether the problem is global or endpoint-specific.
3. Check recent deployments, config changes, traffic changes, and database migrations.
4. Use traces to identify the slow span: controller, service, SQL, external HTTP, serialization, queue publish.
5. For SQL: inspect query count, query text, bind values, execution plan, locks, index usage, row counts.
6. For JVM: inspect GC logs/metrics, heap, thread pools, blocked threads, connection pool metrics.
7. For downstream calls: verify timeouts, retries, breaker state, remote errors, DNS/TLS issues.
8. Apply the smallest safe fix and keep a rollback path.
9. Add a regression test, dashboard, or alert if the issue was not previously observable.

## Deep dive: multi-tenancy implementation matrix

| Pattern | Pros | Cons | Spring concerns |
| --- | --- | --- | --- |
| Row discriminator | Efficient resource sharing, simpler operations | Strong risk of tenant filter bugs, noisy neighbors | Security-derived tenant context, mandatory predicates, cache key isolation |
| Schema per tenant | Better isolation, per-tenant migrations possible | Migration fan-out, connection/schema switching complexity | `AbstractRoutingDataSource`, Hibernate schema strategy, migration automation |
| Database per tenant | Strong isolation, easier export/delete per tenant | Higher cost, many pools, operational complexity | Dynamic datasource registry, secret management, pool limits, health checks |

### Tenant context rules

- Derive tenant from authenticated claims, not arbitrary input.
- Validate the user belongs to the tenant for every request.
- Propagate tenant to async tasks explicitly; ThreadLocal alone is not enough.
- Clear context in filters finally blocks.
- Include tenant in audit logs and business metrics with careful cardinality controls.
