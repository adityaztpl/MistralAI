# 02 - Intermediate Spring Boot

This guide moves from simple REST services into durable application work: persistence, transactions, migrations, security, caching, integration calls, and tests.

## Outcomes

- [ ] Model data with Spring Data JPA and understand generated SQL.
- [ ] Use migrations and transactions safely.
- [ ] Secure APIs with Spring Security 6 and JWT concepts.
- [ ] Add CORS, caching, scheduling, WebClient, and MapStruct deliberately.
- [ ] Design a practical test strategy.

## Spring Data JPA

Spring Data JPA provides repository abstractions over JPA/Hibernate. It removes boilerplate for common persistence operations but does not remove the need to understand SQL, transactions, fetch plans, constraints, and database indexes.

**Interview focus**

- `JpaRepository` versus `CrudRepository`.
- Derived queries, JPQL, native queries, projections, specifications.
- Hibernate as JPA provider and SQL generation behavior.

**Production checklist**

- [ ] Keep repositories persistence-focused.
- [ ] Review generated SQL for non-trivial methods.
- [ ] Use database constraints as final integrity enforcement.

**Code example**

```java
interface ProductRepository extends JpaRepository<ProductEntity, UUID> {
    Optional<ProductEntity> findBySku(String sku);

    @Query("select p from ProductEntity p where lower(p.name) like lower(concat('%', :term, '%'))")
    Page<ProductEntity> search(@Param("term") String term, Pageable pageable);
}
```

**Common pitfalls**

- Assuming repository method names always produce efficient SQL.
- Loading large object graphs because a method returns entities without a fetch plan.

## Entities and relationships

Entities model persisted state and identity. Relationships require clear ownership, cardinality, fetch strategy, cascade rules, and JSON serialization boundaries. In interviews, relationship questions often test whether you understand both object graphs and relational tables.

**Interview focus**

- Owning side in `@OneToMany`/`@ManyToOne`.
- Lazy versus eager loading.
- Cascade and orphan removal semantics.

**Production checklist**

- [ ] Default to lazy relationships for collections.
- [ ] Avoid serializing entities directly to JSON.
- [ ] Use helper methods to keep both sides of bidirectional relationships consistent.

**Code example**

```java
@OneToMany(mappedBy = "product", cascade = CascadeType.ALL, orphanRemoval = true)
private Set<ProductTagEntity> tags = new LinkedHashSet<>();

@ManyToOne(fetch = FetchType.LAZY, optional = false)
@JoinColumn(name = "product_id")
private ProductEntity product;
```

**Common pitfalls**

- Using `CascadeType.REMOVE` from child to parent accidentally.
- Calling lazy associations outside a transaction and hitting `LazyInitializationException`.

## Transactions

`@Transactional` defines a unit of work around database operations. Spring uses proxies by default, so transaction boundaries apply when external callers invoke proxied methods. Rollback defaults to unchecked exceptions.

**Interview focus**

- Propagation, isolation, readOnly, rollback rules.
- Self-invocation proxy limitation.
- Optimistic versus pessimistic locking.

**Production checklist**

- [ ] Put transaction boundaries at service/use-case level.
- [ ] Keep transactions short and avoid remote calls inside them.
- [ ] Use `readOnly=true` for read use cases when appropriate.

**Code example**

```java
@Service
class InventoryService {
    @Transactional
    public ProductResponse adjustStock(UUID id, int delta) {
        ProductEntity product = products.findById(id).orElseThrow();
        product.adjustStock(delta);
        return mapper.toResponse(product);
    }
}
```

**Common pitfalls**

- Annotating private methods and expecting transactions.
- Catching exceptions inside a transaction and accidentally committing invalid state.

## Flyway and Liquibase

Migration tools make schema evolution explicit and repeatable. Flyway is SQL-first and convention-oriented. Liquibase supports XML/YAML/JSON/SQL changelogs and richer database-agnostic metadata. In production, migrations should be reviewed like application code.

**Interview focus**

- Why `ddl-auto=update` is not production migration management.
- Versioned migrations, repeatable migrations, checksums.
- Expand-and-contract deployment for zero downtime.

**Production checklist**

- [ ] Store migrations in source control.
- [ ] Run migrations in CI against real database engines when possible.
- [ ] Design backward-compatible migrations for rolling deployments.

**Code example**

```sql
-- V1__create_product.sql
create table product (
  id uuid primary key,
  sku varchar(40) not null unique,
  name varchar(120) not null,
  price numeric(12,2) not null,
  version bigint not null default 0
);
```

**Common pitfalls**

- Editing already-applied migrations instead of adding a new one.
- Adding non-null columns with no default to large tables during peak traffic.

## Spring Security 6 and JWT

Spring Security 6 uses component-based configuration with `SecurityFilterChain` beans. JWT APIs are commonly implemented either as OAuth2 resource servers validating signed tokens or as custom filters for internally issued tokens. Resource server support is preferred when integrating with standards-based identity providers.

**Interview focus**

- Authentication versus authorization.
- Filter chain order and stateless sessions.
- JWT signature, issuer, audience, expiration, scopes/roles.

**Production checklist**

- [ ] Use HTTPS and validate tokens fully.
- [ ] Prefer OAuth2 resource server for Cognito/Auth0/Keycloak/Entra ID.
- [ ] Keep authorization decisions server-side; do not trust client UI hiding.

**Code example**

```java
@Bean
SecurityFilterChain api(HttpSecurity http) throws Exception {
    return http
        .csrf(csrf -> csrf.disable())
        .sessionManagement(sm -> sm.sessionCreationPolicy(SessionCreationPolicy.STATELESS))
        .authorizeHttpRequests(auth -> auth
            .requestMatchers(HttpMethod.GET, "/api/products/**").permitAll()
            .requestMatchers("/actuator/health/**").permitAll()
            .anyRequest().authenticated())
        .oauth2ResourceServer(oauth2 -> oauth2.jwt(Customizer.withDefaults()))
        .build();
}
```

**Common pitfalls**

- Disabling CSRF without understanding browser session context.
- Parsing JWTs without validating signatures.

## CORS

CORS is a browser-enforced policy that controls whether JavaScript from one origin can call another origin. It is not authentication. For Spring Security applications, configure CORS in the security chain so preflight requests are handled before authorization blocks them.

**Interview focus**

- Origin versus site versus host.
- Preflight OPTIONS requests.
- Credentials and wildcard origin limitations.

**Production checklist**

- [ ] Allow only known origins in production.
- [ ] Keep allowed methods and headers minimal.
- [ ] Handle CORS centrally instead of per-controller sprawl.

**Code example**

```java
@Bean
CorsConfigurationSource corsConfigurationSource() {
    CorsConfiguration config = new CorsConfiguration();
    config.setAllowedOrigins(List.of("https://app.example.com"));
    config.setAllowedMethods(List.of("GET", "POST", "PUT", "DELETE"));
    config.setAllowedHeaders(List.of("Authorization", "Content-Type"));
    UrlBasedCorsConfigurationSource source = new UrlBasedCorsConfigurationSource();
    source.registerCorsConfiguration("/**", config);
    return source;
}
```

**Common pitfalls**

- Using `*` with credentials.
- Trying to fix server-to-server calls with CORS; CORS is for browsers.

## Caching

Spring Cache abstracts cache access through annotations such as `@Cacheable`, `@CachePut`, and `@CacheEvict`. The hard part is not adding a cache; it is choosing keys, TTLs, invalidation, consistency, and observability.

**Interview focus**

- Cache-aside pattern.
- Key generation and cache names.
- Local Caffeine versus distributed Redis.

**Production checklist**

- [ ] Cache read-heavy, stable data.
- [ ] Define eviction on every write path that changes cached data.
- [ ] Measure hit rate, size, latency, and stale-read impact.

**Code example**

```java
@Cacheable(cacheNames = "productsBySku", key = "#sku")
public ProductResponse getBySku(String sku) {
    return repository.findBySku(sku)
        .map(mapper::toResponse)
        .orElseThrow(() -> new ProductNotFoundException(sku));
}

@CacheEvict(cacheNames = "productsBySku", key = "#request.sku()")
public ProductResponse update(UpdateProductRequest request) {
    ProductEntity product = repository.findBySku(request.sku()).orElseThrow();
    product.rename(request.name());
    return mapper.toResponse(product);
}
```

**Common pitfalls**

- Caching user-specific data with a key that omits user identity.
- Caching exceptions or nulls unintentionally.

## Scheduling

Spring scheduling runs methods on a scheduler with `@Scheduled`. Scheduled jobs must be idempotent, observable, and cluster-aware. In multi-instance deployments, a naive scheduled method may run on every pod.

**Interview focus**

- fixedRate versus fixedDelay versus cron.
- Thread pool sizing for scheduled tasks.
- Leader election or distributed locks.

**Production checklist**

- [ ] Make jobs idempotent and retry-safe.
- [ ] Add metrics/logs for start, success, failure, duration, and skipped runs.
- [ ] Use ShedLock, database locks, or platform scheduling when only one instance should run.

**Code example**

```java
@Scheduled(cron = "0 */5 * * * *")
void reconcileInventory() {
    log.info("inventory reconciliation started");
    service.reconcilePendingReservations();
}
```

**Common pitfalls**

- Running long jobs on the default single scheduler thread.
- Not considering duplicate execution during rolling deployments.

## WebClient

WebClient is the modern Spring HTTP client built on reactive foundations. It can be used in non-reactive applications too, but blocking with `.block()` should be isolated and configured with timeouts. For simple synchronous clients in Spring Framework 6.1+, `RestClient` is also available.

**Interview focus**

- WebClient versus RestTemplate versus RestClient.
- Connection, response, and read timeouts.
- Error mapping and resilience around downstream calls.

**Production checklist**

- [ ] Configure base URLs and timeouts centrally.
- [ ] Map downstream errors to domain-specific exceptions.
- [ ] Avoid blocking event-loop threads in reactive applications.

**Code example**

```java
Mono<TaxQuote> quote = webClient.post()
    .uri("/quotes")
    .bodyValue(request)
    .retrieve()
    .onStatus(HttpStatusCode::is5xxServerError, res -> Mono.error(new TaxServiceUnavailableException()))
    .bodyToMono(TaxQuote.class);
```

**Common pitfalls**

- Using no timeouts, causing thread exhaustion during outages.
- Retrying POST calls that are not idempotent.

## Testing with MockMvc and `@SpringBootTest`

Spring Boot testing ranges from fast unit tests to slices and full application context tests. `MockMvc` tests MVC behavior without a real server. `@SpringBootTest` loads the full context and is appropriate for cross-layer integration, especially with Testcontainers.

**Interview focus**

- Test pyramid for Spring applications.
- `@WebMvcTest` versus `@SpringBootTest`.
- MockBean/Mockito versus real infrastructure.

**Production checklist**

- [ ] Use unit tests for domain/service logic.
- [ ] Use MVC slice tests for validation, status, serialization, and exception mapping.
- [ ] Use Testcontainers for database behavior when SQL matters.

**Code example**

```java
@WebMvcTest(ProductController.class)
class ProductControllerTest {
    @Autowired MockMvc mvc;
    @MockBean ProductService service;
}
```

**Common pitfalls**

- Using only full context tests, making feedback slow.
- Mocking repositories in tests that are supposed to verify query behavior.

## MapStruct

MapStruct generates type-safe mapping code at compile time. It is useful when DTO/entity mapping is repetitive and explicit. It is not mandatory; hand mapping can be clearer for small or rule-heavy transformations.

**Interview focus**

- Compile-time generated mapper versus reflection mapper.
- Mapping nested objects and update methods.
- Where mapping belongs in layered architecture.

**Production checklist**

- [ ] Keep mapping rules explicit and reviewed.
- [ ] Avoid hiding business decisions inside mapper expressions.
- [ ] Test important mappings when they include transformations.

**Code example**

```java
@Mapper(componentModel = "spring")
public interface ProductMapper {
    ProductResponse toResponse(ProductEntity entity);

    @Mapping(target = "id", ignore = true)
    ProductEntity toEntity(CreateProductRequest request);
}
```

**Common pitfalls**

- Letting mapper configuration become a second business layer.
- Forgetting annotation processor configuration in Maven/Gradle.

## Transaction interview scenarios

### A service method saves an entity then calls another service method in the same class annotated `@Transactional(REQUIRES_NEW)`. What happens?

Self-invocation bypasses the Spring proxy, so the inner transaction annotation is not applied unless called through another proxied bean or AspectJ-style weaving is used.

### A checked exception is thrown from a transactional method. Does it roll back?

By default, Spring rolls back on unchecked exceptions and `Error`, not checked exceptions. Configure `rollbackFor` when checked exceptions should roll back.

### Should a service call an external payment API inside a database transaction?

Usually no. Long transactions hold locks and connections while waiting on the network. Prefer state transitions, outbox, idempotency, and compensation patterns.

### What does `readOnly=true` do?

It is a hint to transaction infrastructure and providers. It can optimize flush behavior, but it is not a security boundary preventing writes in all cases.

## Intermediate readiness checklist

- [ ] I can write repository methods and explain when to use derived queries, JPQL, native SQL, projections, or specifications.
- [ ] I can explain N+1 and demonstrate fixes with fetch joins, entity graphs, projections, or query redesign.
- [ ] I can set a transaction boundary and justify its location.
- [ ] I can design a migration that works during rolling deployment.
- [ ] I can configure stateless security and explain JWT validation requirements.
- [ ] I can configure CORS without confusing it with authentication.
- [ ] I can add caching with a clear invalidation story.
- [ ] I can schedule jobs safely in multi-instance environments.
- [ ] I can call external services with timeouts and structured errors.
- [ ] I can pick the right Spring test style for a given risk.
