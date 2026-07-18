# 04 - Spring Data with MSSQL and PostgreSQL

This guide compares Microsoft SQL Server and PostgreSQL from a Spring Boot perspective. Both are excellent relational databases, but dialect details affect DDL, SQL, indexing, pagination, JSON, migrations, and performance.

## Outcomes

- [ ] Configure Spring Boot profiles for PostgreSQL and MSSQL.
- [ ] Understand Hibernate dialects and database-specific behavior.
- [ ] Tune HikariCP based on database capacity.
- [ ] Design indexes and pagination strategies.
- [ ] Use JSON columns and stored procedures carefully.
- [ ] Switch datasources by profile without changing application code.

## Quick comparison

| Area | PostgreSQL | MSSQL | Spring impact |
| --- | --- | --- | --- |
| Identity/UUID | PostgreSQL supports `uuid` natively and functions/extensions for generation. | MSSQL uses `uniqueidentifier` and `NEWID()`/`NEWSEQUENTIALID()`. In Java, map both to `UUID`, but review default generation and index fragmentation. | Use profiles, migrations, and dialect-aware tests. |
| Pagination | PostgreSQL uses `limit/offset` and supports keyset pagination well. | MSSQL uses `offset fetch next` for modern versions. Large offsets are expensive in both; keyset pagination is often better. | Use profiles, migrations, and dialect-aware tests. |
| JSON | PostgreSQL `jsonb` is powerful and indexable with GIN. | MSSQL stores JSON in text columns with JSON functions and computed columns for indexing. Hibernate 6 can map JSON with `@JdbcTypeCode(SqlTypes.JSON)` when dialect support aligns. | Use profiles, migrations, and dialect-aware tests. |
| Case sensitivity | PostgreSQL behavior depends on collations and operators such as `ILIKE`. | MSSQL commonly uses case-insensitive collations by default. Do not assume search semantics port unchanged. | Use profiles, migrations, and dialect-aware tests. |
| Upsert | PostgreSQL has `insert on conflict`. | MSSQL has `merge`, but it has caveats; many teams prefer explicit update-then-insert patterns with locking or database-specific SQL. | Use profiles, migrations, and dialect-aware tests. |
| Date/time | Prefer `Instant` for moments. | PostgreSQL `timestamptz` and MSSQL `datetimeoffset`/`datetime2` have different semantics and precision. Define conventions explicitly. | Use profiles, migrations, and dialect-aware tests. |

## Identity/UUID

PostgreSQL supports `uuid` natively and functions/extensions for generation. MSSQL uses `uniqueidentifier` and `NEWID()`/`NEWSEQUENTIALID()`. In Java, map both to `UUID`, but review default generation and index fragmentation.

**Interview angle**

- Mention the Java mapping and the database DDL separately.
- Explain what must be tested against the real engine.
- Avoid pretending dialects make all SQL portable.

## Pagination

PostgreSQL uses `limit/offset` and supports keyset pagination well. MSSQL uses `offset fetch next` for modern versions. Large offsets are expensive in both; keyset pagination is often better.

**Interview angle**

- Mention the Java mapping and the database DDL separately.
- Explain what must be tested against the real engine.
- Avoid pretending dialects make all SQL portable.

## JSON

PostgreSQL `jsonb` is powerful and indexable with GIN. MSSQL stores JSON in text columns with JSON functions and computed columns for indexing. Hibernate 6 can map JSON with `@JdbcTypeCode(SqlTypes.JSON)` when dialect support aligns.

**Interview angle**

- Mention the Java mapping and the database DDL separately.
- Explain what must be tested against the real engine.
- Avoid pretending dialects make all SQL portable.

## Case sensitivity

PostgreSQL behavior depends on collations and operators such as `ILIKE`. MSSQL commonly uses case-insensitive collations by default. Do not assume search semantics port unchanged.

**Interview angle**

- Mention the Java mapping and the database DDL separately.
- Explain what must be tested against the real engine.
- Avoid pretending dialects make all SQL portable.

## Upsert

PostgreSQL has `insert ... on conflict`. MSSQL has `merge`, but it has caveats; many teams prefer explicit update-then-insert patterns with locking or database-specific SQL.

**Interview angle**

- Mention the Java mapping and the database DDL separately.
- Explain what must be tested against the real engine.
- Avoid pretending dialects make all SQL portable.

## Date/time

Prefer `Instant` for moments. PostgreSQL `timestamptz` and MSSQL `datetimeoffset`/`datetime2` have different semantics and precision. Define conventions explicitly.

**Interview angle**

- Mention the Java mapping and the database DDL separately.
- Explain what must be tested against the real engine.
- Avoid pretending dialects make all SQL portable.

## Hibernate dialects

Hibernate dialects teach Hibernate how to generate database-specific SQL for pagination, locking, types, functions, and DDL. Spring Boot 3 usually detects the dialect from JDBC metadata, but setting it explicitly can be useful in constrained startup environments.

**Interview focus**

- Dialect auto-detection.
- Why generated DDL differs between vendors.
- Why native queries and migrations still need vendor awareness.

**Production checklist**

- [ ] Let Boot detect dialect when possible.
- [ ] Set dialect explicitly only when startup cannot inspect metadata or you need clarity.
- [ ] Run integration tests per supported vendor.

**Code example**

```yaml
spring:
  jpa:
    database-platform: org.hibernate.dialect.PostgreSQLDialect
```

**Common pitfalls**

- Assuming a repository tested on H2 behaves the same on MSSQL/PostgreSQL.
- Using Hibernate DDL generation as the production schema source.

## HikariCP connection pools

HikariCP is the default high-performance JDBC connection pool in Spring Boot. Pool size should reflect database capacity, query latency, app instance count, and workload. Oversized pools increase contention and can make outages worse.

**Interview focus**

- maximumPoolSize, minimumIdle, connectionTimeout, maxLifetime.
- Pool starvation symptoms.
- Database max connections divided across app replicas.

**Production checklist**

- [ ] Start with a small pool such as 10 per instance and measure.
- [ ] Set maxLifetime lower than database/load-balancer idle termination.
- [ ] Alert on active connections, pending threads, acquisition timeouts.

**Code example**

```yaml
spring:
  datasource:
    hikari:
      maximum-pool-size: 10
      minimum-idle: 2
      connection-timeout: 2000
      max-lifetime: 1740000
```

**Common pitfalls**

- Setting `maximum-pool-size: 100` on every pod.
- Ignoring slow queries and blaming the pool.

## Indexes

Indexes support read patterns but cost storage and write overhead. Design indexes from actual query predicates, joins, ordering, and uniqueness rules. PostgreSQL and MSSQL both have powerful indexing features, but syntax and optimizer behavior differ.

**Interview focus**

- Composite index column order.
- Covering indexes / included columns.
- Partial/filtered indexes.

**Production checklist**

- [ ] Create indexes in migrations.
- [ ] Verify with `EXPLAIN ANALYZE` or MSSQL actual execution plans.
- [ ] Keep unique constraints for business invariants such as SKU.

**Code example**

```sql
-- PostgreSQL
create unique index ux_product_sku on product (sku);
create index ix_product_status_updated on product (status, updated_at desc);

-- MSSQL
create unique index ux_product_sku on dbo.product (sku);
create index ix_product_status_updated on dbo.product (status, updated_at desc) include (name, price);
```

**Common pitfalls**

- Adding indexes for every column.
- Ignoring selectivity and sort order.

## JSON columns

JSON columns are useful for flexible metadata, integration payloads, and rare attributes, but they should not become an excuse to avoid modeling important relational data. PostgreSQL has first-class `jsonb`; MSSQL has JSON functions over text.

**Interview focus**

- When JSON is appropriate.
- Indexing JSON paths.
- Hibernate 6 JSON mapping.

**Production checklist**

- [ ] Keep frequently queried fields relational or indexed JSON paths.
- [ ] Validate JSON shape at application boundary.
- [ ] Version JSON payloads when used for integrations.

**Code example**

```java
@JdbcTypeCode(SqlTypes.JSON)
@Column(name = "attributes", columnDefinition = "jsonb")
private Map<String, Object> attributes = new LinkedHashMap<>();
```

**Common pitfalls**

- Querying deep JSON paths without indexes.
- Storing core business relationships as opaque JSON.

## Pagination

Offset pagination is simple but becomes slow and inconsistent on large changing datasets. Keyset pagination uses the last seen sort key and is often better for infinite scroll or high-volume APIs.

**Interview focus**

- Pageable abstraction.
- Stable sorting.
- Offset versus keyset trade-offs.

**Production checklist**

- [ ] Always define deterministic sort order.
- [ ] Limit maximum page size.
- [ ] Use keyset for deep browsing or high-volume feeds.

**Code example**

```java
Page<ProductEntity> findByStatus(ProductStatus status, Pageable pageable);

// Keyset idea: where (updated_at, id) < (:lastUpdatedAt, :lastId) order by updated_at desc, id desc
```

**Common pitfalls**

- Returning unbounded lists.
- Sorting by non-indexed expressions for large pages.

## Stored procedures note

Stored procedures can encapsulate complex database logic or integrate with legacy systems. Spring can call them through `JdbcTemplate`, `SimpleJdbcCall`, or JPA stored procedure support. Use them deliberately because they move logic into database deployment and testing workflows.

**Interview focus**

- When stored procedures are justified.
- Calling procedures with Spring JDBC.
- Versioning and deployment of procedure changes.

**Production checklist**

- [ ] Keep procedure contracts documented.
- [ ] Test procedures against the real database engine.
- [ ] Avoid splitting one business invariant between app and procedure without clear ownership.

**Code example**

```java
SimpleJdbcCall call = new SimpleJdbcCall(jdbcTemplate)
    .withProcedureName("reserve_stock");
Map<String, Object> result = call.execute(Map.of("product_id", id, "quantity", quantity));
```

**Common pitfalls**

- Using procedures to hide all data access and making app behavior opaque.
- Not source-controlling procedure definitions.

## Switching datasources by profile

Use profile-specific YAML files such as `application-postgres.yml` and `application-mssql.yml`. Keep common JPA and migration settings in `application.yml` when possible. Activate with `SPRING_PROFILES_ACTIVE=postgres` or `mssql`.

```bash
SPRING_PROFILES_ACTIVE=postgres ./mvnw spring-boot:run
SPRING_PROFILES_ACTIVE=mssql ./mvnw spring-boot:run
```

## Data interview checklist

- [ ] I can explain dialect auto-detection and when to set dialect explicitly.
- [ ] I can map UUIDs, timestamps, numeric money values, and JSON columns with vendor differences.
- [ ] I can size HikariCP from database capacity and replica count.
- [ ] I can design indexes for repository queries and verify query plans.
- [ ] I can choose offset or keyset pagination.
- [ ] I can explain stored procedure trade-offs.
- [ ] I can switch between PostgreSQL and MSSQL with profiles and migrations.
- [ ] I can explain why H2 tests are insufficient for vendor-specific behavior.

## Type mapping reference

| Java type | PostgreSQL choice | MSSQL choice | Notes |
| --- | --- | --- | --- |
| `UUID` | `uuid` | `uniqueidentifier` | Prefer application-generated UUIDs or sequential UUID strategy when index locality matters. |
| `Instant` | `timestamp with time zone` / `timestamptz` | `datetimeoffset` or `datetime2` with UTC convention | Define a UTC convention and test precision loss. |
| `BigDecimal` | `numeric(12,2)` | `decimal(12,2)` | Never use floating point for money. |
| `String` | `varchar` / `text` | `varchar` / `nvarchar` | Use Unicode intentionally; collations affect comparison. |
| `boolean` | `boolean` | `bit` | JPQL hides much but native queries differ. |
| JSON metadata | `jsonb` | `nvarchar(max)` + JSON functions | PostgreSQL has stronger indexing/operators. |
| Enum | `varchar` or native enum | `varchar`/check constraint | `EnumType.STRING` is safer than ordinal. |

## Migration examples by vendor

### PostgreSQL create table

```sql
create table product (
  id uuid primary key,
  sku varchar(40) not null,
  name varchar(120) not null,
  price numeric(12,2) not null,
  quantity_on_hand integer not null default 0,
  status varchar(20) not null,
  attributes jsonb not null default '{}'::jsonb,
  version bigint not null default 0,
  created_at timestamptz not null,
  updated_at timestamptz not null,
  constraint ck_product_price_positive check (price > 0),
  constraint ck_product_quantity_non_negative check (quantity_on_hand >= 0)
);

create unique index ux_product_sku on product (sku);
create index ix_product_status_updated on product (status, updated_at desc);
create index ix_product_attributes_gin on product using gin (attributes);
```

### MSSQL create table

```sql
create table dbo.product (
  id uniqueidentifier not null constraint pk_product primary key,
  sku varchar(40) not null,
  name nvarchar(120) not null,
  price decimal(12,2) not null,
  quantity_on_hand int not null constraint df_product_quantity default 0,
  status varchar(20) not null,
  attributes nvarchar(max) not null constraint df_product_attributes default '{}',
  version bigint not null constraint df_product_version default 0,
  created_at datetime2 not null,
  updated_at datetime2 not null,
  constraint ck_product_price_positive check (price > 0),
  constraint ck_product_quantity_non_negative check (quantity_on_hand >= 0),
  constraint ck_product_attributes_json check (isjson(attributes) = 1)
);

go

create unique index ux_product_sku on dbo.product (sku);
create index ix_product_status_updated on dbo.product (status, updated_at desc) include (name, price);
```

## Query plan workflow

### PostgreSQL

```sql
explain (analyze, buffers)
select id, sku, name, price
from product
where status = 'ACTIVE'
order by updated_at desc, id desc
limit 50;
```

Look for sequential scans on large tables, high actual row counts, sort spills, missing composite indexes, and estimates that differ wildly from actual rows. Update statistics or redesign predicates when necessary.

### MSSQL

Use the actual execution plan in SQL Server Management Studio or Azure Data Studio. Also inspect logical reads with:

```sql
set statistics io on;
set statistics time on;

select top (50) id, sku, name, price
from dbo.product
where status = 'ACTIVE'
order by updated_at desc, id desc;
```

Look for table scans, key lookups, spills, implicit conversions, parameter sniffing symptoms, and missing index suggestions. Treat missing index suggestions as hints, not commands.

## Pagination implementation notes

### Offset pagination API

```http
GET /api/products?page=0&size=50&sort=updatedAt,desc
```

Pros: simple, supported by `Pageable`, easy for admin screens. Cons: large offsets are slow and changing data can move rows between pages.

### Keyset pagination API

```http
GET /api/products?limit=50&afterUpdatedAt=2026-07-18T07:00:00Z&afterId=2b4d0000-0000-4000-8000-000000000001
```

Pros: stable and efficient for deep scrolling. Cons: less convenient for arbitrary page jumps and requires stable sort keys.

### Spring repository shape

```java
@Query("""
    select p
    from ProductEntity p
    where p.status = :status
      and (:afterUpdatedAt is null
        or p.updatedAt < :afterUpdatedAt
        or (p.updatedAt = :afterUpdatedAt and p.id < :afterId))
    order by p.updatedAt desc, p.id desc
    """)
List<ProductEntity> nextPage(ProductStatus status, Instant afterUpdatedAt, UUID afterId, Pageable limitOnly);
```

## Locking and concurrency

### Optimistic locking

Use `@Version` when conflicts are expected to be rare and users can retry. Hibernate includes the version in update statements and throws an optimistic lock exception if another transaction updated the row first.

### Pessimistic locking

Use database locks when conflicts are common and the operation must serialize, such as claiming limited inventory. Keep locked transactions short.

```java
@Lock(LockModeType.PESSIMISTIC_WRITE)
@Query("select p from ProductEntity p where p.id = :id")
Optional<ProductEntity> findForReservation(UUID id);
```

### Interview nuance

- PostgreSQL and MSSQL have different lock modes, wait behavior, deadlock detection, and hints.
- Pessimistic locks can reduce race conditions but increase contention and deadlocks.
- Optimistic locking preserves concurrency but pushes retry/conflict handling to the application.

## Profile switching checklist

- Common application code depends on repositories/services, not vendor-specific beans when avoidable.
- Vendor-specific SQL lives in migration folders or clearly named repository methods.
- `application-postgres.yml` and `application-mssql.yml` define URL, dialect, migration location, and pool name.
- CI runs at least smoke tests for every supported profile.
- Local developer docs explain how to start each database.
- Production deployment pins one profile and does not rely on accidental defaults.

## Testcontainers examples

### PostgreSQL

```java
@Testcontainers
@SpringBootTest
class ProductRepositoryPostgresTest {
    @Container
    static PostgreSQLContainer<?> postgres = new PostgreSQLContainer<>("postgres:16-alpine");

    @DynamicPropertySource
    static void datasource(DynamicPropertyRegistry registry) {
        registry.add("spring.datasource.url", postgres::getJdbcUrl);
        registry.add("spring.datasource.username", postgres::getUsername);
        registry.add("spring.datasource.password", postgres::getPassword);
    }
}
```

### MSSQL

```java
@Testcontainers
@SpringBootTest
class ProductRepositoryMssqlTest {
    @Container
    static MSSQLServerContainer<?> mssql = new MSSQLServerContainer<>("mcr.microsoft.com/mssql/server:2022-latest")
        .acceptLicense();
}
```

## Database production readiness checklist

- Migrations are reviewed and tested on production-like data volume.
- Long-running DDL is identified before deployment.
- Backups and restore drills exist.
- Connection pool saturation alerts exist.
- Slow query logging or equivalent telemetry is enabled.
- Index bloat/fragmentation maintenance is owned.
- Deadlocks are captured and investigated.
- Read replicas are used only with awareness of replication lag.
- Database credentials rotate without application code changes.
- Query behavior is tested on the same database engine used in production.
