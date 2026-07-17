# EF Core Deep Dive

A deep interview and hands-on guide to EF Core tracking, concurrency, indexes, raw SQL, and performance in ASP.NET Core APIs.

## 1. Mental model

EF Core maps C# types to relational structures, translates LINQ to SQL, tracks entity changes, and persists those changes through `SaveChangesAsync`.

```text
LINQ expression -> EF query pipeline -> provider SQL -> database plan -> rows -> DTO/entity materialization
```

Strong interview answer: EF is both a query translator and a unit-of-work/change-tracking engine. Performance depends on the LINQ shape and the database execution plan.

## 2. DbContext lifetime

`DbContext` is normally scoped per request.

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
```

Why scoped:

- It tracks changes for one unit of work.
- It is not thread-safe.
- It should not hold stale entities across requests.
- It should be disposed quickly so connections return to the pool.

Pitfall: background services are singletons, so create a scope per job before resolving a context.

## 3. Model configuration

Prefer explicit configuration for production systems.

```csharp
modelBuilder.Entity<Order>(entity =>
{
    entity.HasKey(order => order.Id);
    entity.Property(order => order.OrderNumber).HasMaxLength(32).IsRequired();
    entity.HasIndex(order => order.OrderNumber).IsUnique();
    entity.Property(order => order.Total).HasPrecision(18, 2);
    entity.Property(order => order.RowVersion).IsRowVersion();
    entity.HasMany(order => order.Items)
        .WithOne(item => item.Order)
        .HasForeignKey(item => item.OrderId)
        .OnDelete(DeleteBehavior.Cascade);
});
```

Configure max lengths, decimal precision, delete behavior, indexes, concurrency tokens, value conversions, and query filters intentionally.

## 4. Change tracker states

States: `Detached`, `Unchanged`, `Added`, `Modified`, `Deleted`.

```csharp
var product = await db.Products.SingleAsync(product => product.Id == id, cancellationToken);
product.Price += 10;
Console.WriteLine(db.Entry(product).State);
await db.SaveChangesAsync(cancellationToken);
```

Inspect changes:

```csharp
foreach (var entry in db.ChangeTracker.Entries())
{
    Console.WriteLine($"{entry.Entity.GetType().Name}: {entry.State}");
}
```

`SaveChangesAsync` detects changes, creates SQL commands, wraps relational operations in a transaction, and updates generated values.

## 5. Tracking versus no tracking

Use tracking for updates and no tracking for read-only APIs.

```csharp
var response = await db.Products
    .AsNoTracking()
    .Where(product => product.Id == id)
    .Select(product => new ProductResponse(product.Id, product.Name, product.Price))
    .SingleOrDefaultAsync(cancellationToken);
```

`AsNoTrackingWithIdentityResolution` avoids change tracking but reuses instances for repeated identities in a result graph.

## 6. Projection and relationship loading

Projection controls payload and columns.

```csharp
var orders = await db.Orders
    .AsNoTracking()
    .Where(order => order.CustomerId == customerId)
    .Select(order => new OrderListItem(
        order.Id,
        order.OrderNumber,
        order.Items.Sum(item => item.UnitPrice * item.Quantity)))
    .ToListAsync(cancellationToken);
```

Use `Include` for entity graphs, projection for API reads. Use `AsSplitQuery` when multiple collection includes risk cartesian explosion.

## 7. Pagination

Always bound list endpoints.

```csharp
var page = Math.Max(request.Page, 1);
var pageSize = Math.Clamp(request.PageSize, 1, 100);
var items = await query
    .OrderBy(order => order.CreatedAt)
    .ThenBy(order => order.Id)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync(cancellationToken);
```

Offset pagination is simple. Keyset pagination is better for deep scrolling and high-volume tables.

## 8. Indexes

Indexes speed reads at write/storage cost.

```csharp
modelBuilder.Entity<Order>().HasIndex(order => new { order.TenantId, order.Status, order.CreatedAt });
modelBuilder.Entity<Order>().HasIndex(order => order.OrderNumber).IsUnique();
```

Design rules:

- Index foreign keys used in joins.
- Put equality columns before range/sort columns in composite indexes.
- Use unique indexes to enforce invariants.
- Include tenant id in multi-tenant indexes.
- Review query plans for hot paths.

## 9. Optimistic concurrency

Optimistic concurrency detects lost updates.

```csharp
entity.Property(product => product.RowVersion).IsRowVersion();
_db.Entry(product).Property(existing => existing.RowVersion).OriginalValue = request.RowVersion;

try
{
    await _db.SaveChangesAsync(cancellationToken);
}
catch (DbUpdateConcurrencyException)
{
    return Results.Conflict(new ProblemDetails { Title = "Resource was modified." });
}
```

Strategies: store wins, client wins, merge, or domain-specific resolution.

## 10. Raw SQL and bulk operations

Use raw SQL for provider-specific or hard-to-translate queries, but parameterize values.

```csharp
var orders = await db.Orders
    .FromSqlInterpolated($"SELECT * FROM Orders WHERE Total >= {minTotal}")
    .AsNoTracking()
    .ToListAsync(cancellationToken);
```

Set-based operations:

```csharp
await db.Products
    .Where(product => product.DiscontinuedAt < cutoff)
    .ExecuteUpdateAsync(setters => setters.SetProperty(product => product.IsActive, false), cancellationToken);
```

Bulk updates bypass tracked entity hooks and domain events.

## 11. Transactions and diagnostics

One `SaveChangesAsync` is transactional for relational providers. Keep explicit transactions short and avoid remote calls inside them.

```csharp
await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
await DoWorkAsync(cancellationToken);
await transaction.CommitAsync(cancellationToken);
```

Diagnostics:

```csharp
var sql = query.ToQueryString();
var tagged = db.Orders.TagWith("Orders.Search");
```

Enable sensitive data logging only in development.

## 12. Performance checklist

- Project DTOs for reads.
- Use `AsNoTracking` for read-only queries.
- Page with deterministic ordering.
- Avoid lazy-loading N+1.
- Inspect generated SQL and query plans.
- Align indexes with filters/sorts.
- Use split queries for multiple collection includes.
- Use compiled queries only for measured hot paths.
- Pass cancellation tokens.
- Keep transactions short.

## Interview checklist

- [ ] Explain the concept without code.
- [ ] Implement the smallest working example.
- [ ] Name two production pitfalls.
- [ ] Name one test that catches a regression.
- [ ] Describe the operational signal that reveals a production issue.

## Explain this prompts

- Explain how EF Core knows which columns to update.
- Explain why `DbContext` is scoped.
- Explain `AsNoTracking` and when not to use it.
- Explain N+1 and cartesian explosion.
- Explain how a row-version token becomes a SQL concurrency check.
- Explain how to design an index for tenant/status/date queries.
- Explain safe raw SQL.
- Explain why EF InMemory can miss relational bugs.

## Hands-on lab: diagnose and fix a slow endpoint

Scenario: `GET /api/orders?status=Paid&page=1&pageSize=50` has regressed from p95 90 ms to p95 900 ms.

### Step 1: capture evidence

- Add a query tag such as `Orders.Search.Paid`.
- Capture the generated SQL with `ToQueryString()` in a local reproduction.
- Inspect traces to see whether time is in SQL, serialization, or an external call.
- Check row counts returned versus row counts scanned.
- Compare query plan before and after the release.

### Step 2: common findings

| Finding | Likely cause | Fix |
| --- | --- | --- |
| table scan | missing index | add composite index matching tenant/status/date |
| many repeated child queries | lazy loading N+1 | projection or eager loading |
| huge row payload | returning entities | DTO projection |
| sort spill | index does not support order | adjust composite index |
| deep offset slow | large `Skip` | keyset pagination |
| high DB waits | long transactions | shorten transaction and tune query |

### Step 3: improved query

```csharp
var pageSize = Math.Clamp(request.PageSize, 1, 100);

var items = await db.Orders
    .TagWith("Orders.Search.Paid")
    .AsNoTracking()
    .Where(order => order.TenantId == tenantId && order.Status == OrderStatus.Paid)
    .OrderByDescending(order => order.CreatedAt)
    .ThenByDescending(order => order.Id)
    .Take(pageSize)
    .Select(order => new OrderListItem(
        order.Id,
        order.OrderNumber,
        order.CustomerEmail,
        order.CreatedAt,
        order.Items.Sum(item => item.Quantity * item.UnitPrice)))
    .ToListAsync(cancellationToken);
```

### Step 4: verify

- Unit test query composition if logic is complex.
- Integration test response shape and paging behavior.
- Run the query against production-like data.
- Verify p95 and p99 latency after deployment.

## Pitfall matrix

| Pitfall | Symptom | Prevention |
| --- | --- | --- |
| returning entities | oversized JSON, cycles, leaked fields | response DTOs |
| unbounded `GET` list | high memory and slow requests | mandatory paging |
| client-side filtering | app CPU spike, too many rows | keep filters translatable |
| lazy loading in API | N+1 queries | disable lazy loading or project |
| missing decimal precision | money rounding surprises | configure precision explicitly |
| no concurrency token | lost updates | row version/application token |
| long-lived context | stale data, memory growth | scoped contexts only |
| context in singleton | runtime lifetime bug | scope factory in singleton workers |
| raw SQL concat | SQL injection | interpolated SQL/parameters |
| broad query filters | hidden missing data | test with/without filters deliberately |

## Code review checklist for EF Core PRs

- [ ] Does every list endpoint page and order deterministically?
- [ ] Are read endpoints using DTO projections?
- [ ] Does any query call `ToListAsync` before applying filters?
- [ ] Are `Include`s necessary, or would projection be clearer?
- [ ] Are tenant/user filters applied before materialization?
- [ ] Are cache keys tenant-aware for cached query results?
- [ ] Are decimal precision and string lengths configured?
- [ ] Are foreign keys and common filters indexed?
- [ ] Does a write path need optimistic concurrency?
- [ ] Are transactions short and free of remote calls?
- [ ] Is raw SQL parameterized?
- [ ] Do integration tests use a relational provider where behavior matters?

## Whiteboard: concurrency SQL

When a row version is configured, EF includes the original token in the update predicate.

```sql
UPDATE Products
SET Name = @p0, Price = @p1
WHERE Id = @id AND RowVersion = @originalRowVersion;
```

If zero rows are affected, EF throws `DbUpdateConcurrencyException`. That does not mean the database failed; it means the row no longer matches the original state the client edited.

## Whiteboard: query filter risk

```csharp
modelBuilder.Entity<Order>()
    .HasQueryFilter(order => order.TenantId == _tenantContext.TenantId);
```

Benefits:

- Reduces accidental tenant leakage.
- Keeps simple queries tenant-scoped.

Risks:

- Admin/reporting code may need explicit bypass.
- Tests must prove tenant context is trustworthy.
- Required navigations plus filters can remove parent rows unexpectedly.
- Cache keys still need tenant id because filters do not protect cached data.

## Mini flashcards

- `DbContext` is not thread-safe.
- `DbContext` lifetime is usually scoped.
- `AsNoTracking` is for read-only queries.
- Projection often avoids `Include`.
- `SaveChangesAsync` is transactional for one relational save.
- Row version conflicts should usually return `409`.
- Unique indexes enforce invariants under race conditions.
- Raw SQL must be parameterized.
- EF InMemory is not relational.
- Measure before using compiled queries.
