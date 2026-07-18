# Data Platform Cheatsheet

## SQL design

- Start with access patterns.
- Use constraints for invariants.
- Use DTO/read models for API responses.
- Avoid returning database entities directly.
- Use UTC timestamps.
- Add `tenant_id`/`user_id` to multi-tenant tables and indexes.

## Index order

```text
Equality filters -> range filters -> sort columns
```

Examples:

```sql
CREATE INDEX notes_user_archive_updated_idx
ON notes (user_id, is_archived, updated_at DESC);

CREATE INDEX notes_active_updated_idx
ON notes (user_id, updated_at DESC)
WHERE deleted_at IS NULL;
```

## Query plans

Use:

```sql
EXPLAIN (ANALYZE, BUFFERS) SELECT ...;
```

Watch for:

- Sequential scan on large table.
- Sort spill.
- Bad row estimates.
- Too many nested loop iterations.
- Rows removed by filter.

## EF Core

- Inspect generated migrations.
- Use projections for read APIs.
- Use `.AsNoTracking()` for read-only queries.
- Avoid lazy loading in APIs.
- Use expand/backfill/contract for risky schema changes.
- Avoid `Database.Migrate()` on every production app startup.

## EF N+1 quick fix

Bad:

```csharp
var notes = await db.Notes.ToListAsync();
return notes.Select(n => n.Tags.Select(t => t.Name));
```

Better:

```csharp
return await db.Notes
    .AsNoTracking()
    .Select(n => new NoteDto(n.Id, n.Tags.Select(t => t.Name).ToList()))
    .ToListAsync();
```

## Redis

Patterns:

- Cache-aside for hot reads.
- Delete cache on writes.
- TTL with jitter.
- Single-flight/stampede protection for expensive rebuilds.
- Include tenant/user/version in keys.

Key examples:

```text
notes:{userId}:{noteId}:v1
rag:retrieval:{tenantId}:{queryHash}:{filterHash}:{indexVersion}
rate:{userId}:chat:{window}
```

## Queues/jobs

Use queues for:

- Ingestion.
- Embeddings.
- Email/notifications.
- Exports/imports.
- External API retries.

Must have:

- Idempotency.
- Retry/backoff.
- Dead-letter handling.
- Correlation IDs.
- Job status for user-visible work.

## Hangfire vs Worker + broker

| Option | Use when |
|---|---|
| Hangfire | Simple .NET background jobs, dashboard, scheduled tasks |
| Worker + RabbitMQ | Flexible routing, self-hosted broker, high throughput |
| Worker + Azure Service Bus | Azure managed messaging, DLQ, sessions, duplicate detection |

## RAG data reminders

Store on chunks:

- `document_id`
- `tenant_id`
- `source_uri`
- `heading`
- `chunk_index`
- `content`
- `token_count`
- `embedding_model`
- `embedding_dimension`
- `metadata`

Validate vector dimensions before insert/search.

## Docker/CI

- Compose for local Postgres/Redis/RabbitMQ/Qdrant.
- Add health checks.
- CI must restore, build, test.
- Integration tests need real dependencies or Testcontainers.
- Validate migrations in CI when possible.

## Azure

- App Service: simple APIs.
- Container Apps: API + worker + queue scaling.
- Key Vault + managed identity for secrets.
- Application Insights/OpenTelemetry for traces.
- Deployment slots/revisions for rollback.
- Migrations as controlled deployment step.

## Interview one-liners

- "I design indexes from access patterns, then verify with `EXPLAIN ANALYZE`."
- "Application validation is for UX; database constraints protect invariants."
- "RAG vector rows need metadata, model version, and tenant filters, not just embeddings."
- "Jobs must be idempotent because retries are normal, not exceptional."
- "Cache invalidation should be explicit; Redis is not the source of truth by accident."
- "Migrations are deployment choreography, especially under rolling deploys."
