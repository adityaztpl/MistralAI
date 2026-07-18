# SQL Design and Indexing

## Interview-ready summary

Good SQL design starts from access patterns. Tables should represent durable facts, constraints should protect invariants, and indexes should match the queries the application actually runs. For GenAI apps, relational metadata often matters as much as vector search.

## Design workflow

1. List core user journeys.
2. Identify entities and relationships.
3. Decide what must be transactional.
4. Add constraints for invariants.
5. Write expected queries.
6. Add indexes for those queries.
7. Inspect execution plans with realistic data.
8. Revisit after production telemetry.

## Example domain: notes + RAG

Entities:

- `users`: account identity.
- `notes`: user-authored notes.
- `note_tags`: normalized tags for filtering.
- `documents`: source documents for RAG.
- `document_chunks`: searchable chunks.
- `chat_sessions`: conversation container.
- `chat_messages`: user/assistant messages.
- `message_citations`: answer-to-source links.
- `ingestion_jobs`: background ingestion state.
- `feedback`: thumbs up/down and correction data.

## Normalization guidance

Normalize when:

- Data has independent lifecycle.
- You need constraints or joins.
- Many rows reference the same concept.
- Updates should not duplicate across many records.

Denormalize when:

- It is a read model derived from source data.
- You can rebuild it.
- It avoids a hot expensive join.
- You store a snapshot for audit purposes.

Example: store `message_citations.snippet` as a snapshot even though the chunk content exists elsewhere. The citation should reflect what the model saw at answer time.

## Constraints that matter

```sql
CREATE TABLE notes (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id uuid NOT NULL REFERENCES users(id),
  title text NOT NULL CHECK (length(trim(title)) BETWEEN 1 AND 120),
  body text,
  is_archived boolean NOT NULL DEFAULT false,
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz NOT NULL DEFAULT now(),
  deleted_at timestamptz
);
```

Use constraints for:

- Required fields.
- Valid enum-like statuses.
- Positive amounts/counts.
- Uniqueness within tenant/user boundaries.
- Foreign key integrity.

Do not rely only on application validation. Application validation gives good UX; database constraints protect data integrity.

## Index design

An index is useful when it matches filtering, joining, sorting, or uniqueness requirements.

### Common B-tree indexes

```sql
CREATE INDEX notes_user_updated_idx
ON notes (user_id, updated_at DESC)
WHERE deleted_at IS NULL;

CREATE INDEX note_tags_user_name_idx
ON note_tags (user_id, name);

CREATE UNIQUE INDEX note_tags_note_name_unique_idx
ON note_tags (note_id, name);
```

### Composite index order

For a query:

```sql
SELECT *
FROM notes
WHERE user_id = $1 AND is_archived = false
ORDER BY updated_at DESC
LIMIT 20;
```

Good index:

```sql
CREATE INDEX notes_user_archive_updated_idx
ON notes (user_id, is_archived, updated_at DESC);
```

Rule of thumb:

1. Equality filters first.
2. Range filters next.
3. Sort columns last if useful.
4. Include partial predicates for common subsets.

### Partial indexes

If most queries ignore deleted rows:

```sql
CREATE INDEX notes_active_user_updated_idx
ON notes (user_id, updated_at DESC)
WHERE deleted_at IS NULL;
```

Partial indexes reduce size and improve cache locality.

### Full-text search

For note search:

```sql
ALTER TABLE notes
ADD COLUMN search_vector tsvector GENERATED ALWAYS AS (
  setweight(to_tsvector('english', coalesce(title, '')), 'A') ||
  setweight(to_tsvector('english', coalesce(body, '')), 'B')
) STORED;

CREATE INDEX notes_search_idx ON notes USING gin (search_vector);
```

Query:

```sql
SELECT id, title
FROM notes
WHERE user_id = $1
  AND search_vector @@ plainto_tsquery('english', $2)
ORDER BY ts_rank(search_vector, plainto_tsquery('english', $2)) DESC
LIMIT 20;
```

## Vector indexing with pgvector

```sql
CREATE EXTENSION IF NOT EXISTS vector;

CREATE TABLE document_chunks (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  document_id uuid NOT NULL REFERENCES documents(id) ON DELETE CASCADE,
  tenant_id uuid NOT NULL,
  chunk_index int NOT NULL,
  heading text,
  content text NOT NULL,
  token_count int NOT NULL CHECK (token_count > 0),
  embedding_model text NOT NULL,
  embedding_dimension int NOT NULL,
  embedding vector(1024) NOT NULL,
  metadata jsonb NOT NULL DEFAULT '{}'::jsonb,
  created_at timestamptz NOT NULL DEFAULT now(),
  UNIQUE (document_id, chunk_index)
);

CREATE INDEX document_chunks_tenant_idx ON document_chunks (tenant_id);
CREATE INDEX document_chunks_embedding_hnsw_idx
ON document_chunks USING hnsw (embedding vector_cosine_ops);
```

Query with tenant filter:

```sql
SELECT id, document_id, heading, content,
       1 - (embedding <=> $1::vector) AS similarity
FROM document_chunks
WHERE tenant_id = $2
ORDER BY embedding <=> $1::vector
LIMIT 8;
```

Important: apply authorization/tenant filters before chunks reach the prompt. If the vector index cannot efficiently combine filtering and ANN search for your workload, consider separate collections per tenant/category or a vector DB with stronger filtered search behavior.

## Reading execution plans

Use:

```sql
EXPLAIN (ANALYZE, BUFFERS)
SELECT ...;
```

Look for:

- Sequential scans on large tables.
- High rows removed by filter.
- Sorts spilling to disk.
- Nested loops over large result sets.
- Misestimated row counts.
- Index scans that still read too many rows.

## Pagination

Offset pagination is simple but gets slow for deep pages:

```sql
SELECT id, title
FROM notes
WHERE user_id = $1
ORDER BY updated_at DESC, id DESC
LIMIT 20 OFFSET 10000;
```

Keyset pagination is better for feeds:

```sql
SELECT id, title, updated_at
FROM notes
WHERE user_id = $1
  AND (updated_at, id) < ($2, $3)
ORDER BY updated_at DESC, id DESC
LIMIT 20;
```

## Multi-tenancy

Options:

| Approach | Pros | Cons |
|---|---|---|
| Shared tables with tenant_id | Simple, efficient for many small tenants | Every query must filter correctly |
| Schema per tenant | Stronger isolation | Operational complexity |
| Database per tenant | Highest isolation | Expensive, complex migrations |

For interviews, shared tables with strict tenant filters and indexes are usually a reasonable default unless compliance requirements demand stronger isolation.

## Common mistakes

- Designing tables before knowing queries.
- Missing foreign keys because "the app handles it."
- Indexing every column.
- Not indexing foreign keys used in joins/deletes.
- Using offset pagination for deep feeds.
- Storing vector chunks without source metadata.
- Mixing embedding models in one vector index without tracking versions.

## Interview phrasing

> I start with access patterns, then design the schema and indexes to match them. I use constraints for invariants, DTO/read models for denormalized views, and `EXPLAIN ANALYZE` with realistic data to validate indexes. For RAG, I store document/chunk metadata, embedding model and dimension, and tenant filters alongside the vectors so retrieval can be secure and debuggable.
