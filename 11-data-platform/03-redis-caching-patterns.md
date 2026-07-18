# Redis Caching Patterns

## Interview-ready summary

Redis improves latency and protects downstream systems when used deliberately. The hard parts are key design, invalidation, consistency expectations, stampede prevention, and observability. The database remains the source of truth unless you intentionally design otherwise.

## When to cache

Cache data that is:

- Read frequently.
- Expensive to compute or fetch.
- Tolerant of short staleness.
- Shared across users or repeated within a user/session.

Do not cache just because Redis exists. Start with the query and latency profile.

## Pattern 1: Cache-aside

Application reads cache first, falls back to database, then populates cache.

```csharp
public async Task<NoteDto?> GetNoteAsync(Guid userId, Guid noteId, CancellationToken ct)
{
    var key = $"notes:{userId}:{noteId}:v1";
    var cached = await cache.GetStringAsync(key, ct);
    if (cached is not null)
        return JsonSerializer.Deserialize<NoteDto>(cached);

    var note = await db.Notes
        .AsNoTracking()
        .Where(n => n.UserId == userId && n.Id == noteId && n.DeletedAt == null)
        .Select(n => new NoteDto(n.Id, n.Title, n.Body, n.UpdatedAt))
        .SingleOrDefaultAsync(ct);

    if (note is not null)
    {
        await cache.SetStringAsync(
            key,
            JsonSerializer.Serialize(note),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) },
            ct);
    }

    return note;
}
```

Pros: simple, app-controlled.

Cons: stale data until TTL or invalidation.

## Pattern 2: Write-through/update cache on writes

After database write succeeds, update or delete the cache.

```csharp
await db.SaveChangesAsync(ct);
await cache.RemoveAsync($"notes:{userId}:{noteId}:v1", ct);
```

Prefer delete-on-write for simple correctness. Rebuilding the cache on next read avoids writing cache values that do not match transaction outcome.

## Pattern 3: Read-through with single-flight/stampede protection

Problem: many requests miss cache at once and all query DB.

Mitigations:

- Short distributed lock around rebuild.
- In-process single-flight for one node.
- Stale-while-revalidate.
- Jittered TTLs.

Pseudo-pattern:

```text
if cache hit: return value
if lock acquired: load from DB, set cache, release lock
else: wait briefly and retry cache, or return stale value
```

## Key design

Good keys are:

- Namespaced by feature.
- Include tenant/user where needed.
- Include version for schema changes.
- Avoid raw untrusted long input.

Examples:

```text
notes:{userId}:{noteId}:v1
notes:list:{userId}:archived=false:page=1:size=20:v2
rag:retrieval:{tenantId}:{queryHash}:{filterHash}:v1
rate:{userId}:chat:20260718T0430
```

## TTL strategy

| Data | TTL guidance |
|---|---|
| User profile read model | 5-30 min with invalidation on update |
| Feature flags/config | 30-300 sec, tolerate short stale |
| Search results | 30-120 sec if query volume justifies it |
| RAG retrieval results | Short TTL, include model/index version in key |
| Rate limit counters | Window duration |
| Distributed locks | Very short, with safe expiry |

Add TTL jitter to avoid synchronized expiry:

```csharp
var ttl = TimeSpan.FromMinutes(5) + TimeSpan.FromSeconds(Random.Shared.Next(0, 60));
```

## Caching in RAG apps

Useful caches:

- Document ingestion status by source.
- Hot document metadata/read models.
- Embedding results for repeated exact text during ingestion.
- Retrieval results for repeated questions, if index version and filters are in the key.
- Model responses only for deterministic, non-sensitive, policy-approved use cases.

Be careful caching generated answers. They may include user-specific context, stale policy, or sensitive data.

## Distributed locks

Use locks for coordination, not correctness of money/security workflows.

Risks:

- Lock expires while work continues.
- Network partition leaves ambiguous ownership.
- Long locks reduce throughput.

Prefer idempotent jobs and database uniqueness constraints where possible.

## Rate limiting with Redis

Fixed window example concept:

```text
INCR rate:user123:chat:202607180435
EXPIRE rate:user123:chat:202607180435 60
```

Sliding windows/token buckets are fairer but more complex. In interviews, mention the trade-off.

## Observability

Track:

- Hit rate by key prefix.
- Cache latency.
- Rebuild latency.
- Evictions.
- Memory usage.
- Error rate/timeouts.
- Stampede lock contention.

A high hit rate is not always good if cached data is stale or incorrectly scoped.

## Common mistakes

- Caching per-user sensitive data without user id in key.
- No invalidation path.
- Infinite TTL for mutable data.
- Cache key missing version/model/filter.
- Treating Redis as the source of truth accidentally.
- No timeout/fallback when Redis is unavailable.
- Serializing huge objects that should be paginated.

## Interview phrasing

> I would use cache-aside for hot read models, invalidate on writes, and design keys with tenant/user scope plus a version. I would set TTLs with jitter, add stampede protection for expensive rebuilds, and keep the database as source of truth. For RAG, I would include embedding/index version and filters in cache keys so stale or unauthorized retrieval results are not reused incorrectly.
