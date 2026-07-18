# Debugging Playbook: EF Core N+1 Queries

## Symptoms

- Endpoint is fast with 5 rows but slow with 500 rows.
- Database logs show one query for the list and then one query per item.
- CPU is low but request latency is high due to many round trips.
- JSON serialization triggers lazy loading.
- Adding a navigation property to the response suddenly slows the endpoint.

## Reproduce

1. Enable EF Core SQL logging in development.
2. Hit the slow endpoint with realistic row counts.
3. Count SQL statements and total duration.
4. Use MiniProfiler, Application Insights dependency telemetry, or database logs.
5. Compare query count before and after removing navigation properties from the response.

EF logging:

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString)
        .EnableSensitiveDataLogging(builder.Environment.IsDevelopment())
        .LogTo(Console.WriteLine, LogLevel.Information));
```

## Diagnose

Common causes:

- Lazy loading proxies enabled and navigation properties accessed in a loop.
- Mapping entities to DTOs after materialization while touching navigations.
- Missing `Include`/`ThenInclude` for required related data.
- Using `Include` for too much data, causing cartesian explosion instead.
- Returning EF entities directly from API.

Problematic pattern:

```csharp
var notes = await db.Notes.ToListAsync();
return notes.Select(note => new NoteDto(
    note.Id,
    note.Title,
    note.Tags.Select(tag => tag.Name).ToList()));
```

If `Tags` lazy-loads, this becomes one query for notes plus one query per note.

## Fix

### Prefer projection for API responses

```csharp
var notes = await db.Notes
    .AsNoTracking()
    .Where(n => !n.IsArchived)
    .OrderByDescending(n => n.UpdatedAt)
    .Select(n => new NoteDto(
        n.Id,
        n.Title,
        n.Tags.OrderBy(t => t.Name).Select(t => t.Name).ToList()))
    .ToListAsync(cancellationToken);
```

Projection usually produces the data shape you need without loading full entity graphs.

### Use Include intentionally

```csharp
var notes = await db.Notes
    .AsNoTracking()
    .Include(n => n.Tags)
    .Where(n => ids.Contains(n.Id))
    .ToListAsync(cancellationToken);
```

### Use split queries for large graphs

```csharp
var orders = await db.Orders
    .AsNoTracking()
    .Include(o => o.Items)
    .Include(o => o.Payments)
    .AsSplitQuery()
    .ToListAsync(cancellationToken);
```

Split queries can avoid cartesian explosion, but still inspect query count and transaction consistency needs.

### Disable lazy loading for APIs

Avoid lazy loading proxies in web APIs unless there is a strong reason. Make data loading explicit.

## Prevention

- Do not return EF entities directly from controllers.
- Use DTO projections for read endpoints.
- Add performance tests or query-count assertions for important endpoints.
- Monitor database dependency count per request.
- Review new navigation property usage carefully.
- Use `.AsNoTracking()` for read-only queries.
- Add indexes that match filters/sorts after fixing query shape.

## Interview phrasing

> I would enable SQL logging or dependency tracing and count the queries for one request. If I see one parent query plus repeated child queries, I would look for lazy loading or navigation access during DTO mapping/serialization. The usual fix is an explicit projection that selects the exact DTO shape, with `Include` or split queries only when appropriate. To prevent recurrence I would avoid returning EF entities, add query-count/performance tests on hot endpoints, and monitor database calls per request.
