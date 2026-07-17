# pgvector Notes for RAG

These notes show a pragmatic PostgreSQL + pgvector setup for a multi-tenant RAG system. Adjust dimensions, index type, and metadata fields to your embedding model and workload.

---

## 1. Extension and schema

```sql
CREATE EXTENSION IF NOT EXISTS vector;
CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TABLE rag_documents (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id TEXT NOT NULL,
  source_uri TEXT NOT NULL,
  title TEXT NOT NULL,
  document_version TEXT NOT NULL,
  content_hash TEXT NOT NULL,
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  deleted_at TIMESTAMPTZ,
  UNIQUE (tenant_id, source_uri, document_version)
);

CREATE TABLE rag_chunks (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  document_id UUID NOT NULL REFERENCES rag_documents(id) ON DELETE CASCADE,
  tenant_id TEXT NOT NULL,
  chunk_key TEXT NOT NULL,
  section_path TEXT NOT NULL,
  content TEXT NOT NULL,
  token_count INTEGER NOT NULL,
  embedding_model TEXT NOT NULL,
  embedding vector(1536) NOT NULL,
  metadata JSONB NOT NULL DEFAULT '{}'::jsonb,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  UNIQUE (tenant_id, document_id, chunk_key)
);
```

Notes:

- `tenant_id` is duplicated on chunks for fast filtering.
- `embedding_model` helps with re-index migrations.
- `metadata` can contain ACL groups, page numbers, headings, and freshness fields.
- Use the vector dimension required by your embedding model.

---

## 2. Indexes

Start with normal metadata indexes.

```sql
CREATE INDEX rag_documents_tenant_source_idx
ON rag_documents (tenant_id, source_uri);

CREATE INDEX rag_chunks_tenant_doc_idx
ON rag_chunks (tenant_id, document_id);

CREATE INDEX rag_chunks_metadata_gin_idx
ON rag_chunks USING gin (metadata);
```

### HNSW index

HNSW is often a strong default for low-latency ANN search.

```sql
CREATE INDEX rag_chunks_embedding_hnsw_idx
ON rag_chunks
USING hnsw (embedding vector_cosine_ops)
WITH (m = 16, ef_construction = 64);
```

At query time:

```sql
SET hnsw.ef_search = 80;
```

Higher `ef_search` usually improves recall with more latency.

### IVFFlat index

IVFFlat may be useful for large datasets, but it usually needs enough rows before building.

```sql
CREATE INDEX rag_chunks_embedding_ivfflat_idx
ON rag_chunks
USING ivfflat (embedding vector_cosine_ops)
WITH (lists = 100);
```

At query time:

```sql
SET ivfflat.probes = 10;
```

Tune `lists` and `probes` with a retrieval eval set.

---

## 3. Authorized vector search

Filter by tenant and ACL before returning context to the model.

```sql
SELECT
  c.id,
  c.chunk_key,
  d.title,
  d.source_uri,
  c.section_path,
  c.content,
  1 - (c.embedding <=> $1::vector) AS similarity,
  c.metadata
FROM rag_chunks c
JOIN rag_documents d ON d.id = c.document_id
WHERE c.tenant_id = $2
  AND d.deleted_at IS NULL
  AND c.embedding_model = $3
  AND (
    c.metadata->'acl_group_ids' IS NULL
    OR c.metadata->'acl_group_ids' ?| $4::text[]
  )
ORDER BY c.embedding <=> $1::vector
LIMIT $5;
```

Parameters:

1. query embedding
2. tenant ID
3. embedding model
4. authorized group IDs
5. top-k

Security rule: never search all tenants and filter after generation.

---

## 4. Hybrid search sketch

Use Postgres full-text search plus pgvector when you want exact-term support.

```sql
ALTER TABLE rag_chunks
ADD COLUMN search_vector tsvector
GENERATED ALWAYS AS (
  to_tsvector('english', section_path || ' ' || content)
) STORED;

CREATE INDEX rag_chunks_search_vector_idx
ON rag_chunks USING gin (search_vector);
```

Keyword query:

```sql
SELECT
  c.id,
  ts_rank(c.search_vector, plainto_tsquery('english', $1)) AS keyword_score
FROM rag_chunks c
WHERE c.tenant_id = $2
  AND c.search_vector @@ plainto_tsquery('english', $1)
ORDER BY keyword_score DESC
LIMIT 50;
```

Application-side reciprocal rank fusion:

```python
def reciprocal_rank_fusion(result_lists, k=60):
    scores = {}
    for results in result_lists:
        for rank, row in enumerate(results, start=1):
            chunk_id = row["id"]
            scores[chunk_id] = scores.get(chunk_id, 0.0) + 1.0 / (k + rank)
    return sorted(scores.items(), key=lambda item: item[1], reverse=True)
```

---

## 5. Upsert ingestion transaction

```sql
BEGIN;

INSERT INTO rag_documents (
  tenant_id,
  source_uri,
  title,
  document_version,
  content_hash,
  updated_at
)
VALUES ($1, $2, $3, $4, $5, now())
ON CONFLICT (tenant_id, source_uri, document_version)
DO UPDATE SET
  title = EXCLUDED.title,
  content_hash = EXCLUDED.content_hash,
  updated_at = now()
RETURNING id;

-- Application receives document_id, deletes old chunks for this version if needed.
DELETE FROM rag_chunks
WHERE tenant_id = $1
  AND document_id = $document_id;

-- Bulk insert chunks using COPY or batched inserts.
INSERT INTO rag_chunks (
  document_id,
  tenant_id,
  chunk_key,
  section_path,
  content,
  token_count,
  embedding_model,
  embedding,
  metadata
)
VALUES
  ($document_id, $1, $6, $7, $8, $9, $10, $11::vector, $12::jsonb);

COMMIT;
```

For large ingestion jobs, batch inserts and monitor queue lag.

---

## 6. App-side notes

### Python query shape

```python
from pgvector.psycopg import register_vector
import psycopg


def search_chunks(conn, query_embedding, tenant_id, groups, model, limit=8):
    register_vector(conn)
    with conn.cursor() as cur:
        cur.execute(
            """
            SELECT c.chunk_key, d.title, d.source_uri, c.content,
                   1 - (c.embedding <=> %s) AS similarity
            FROM rag_chunks c
            JOIN rag_documents d ON d.id = c.document_id
            WHERE c.tenant_id = %s
              AND d.deleted_at IS NULL
              AND c.embedding_model = %s
              AND (
                c.metadata->'acl_group_ids' IS NULL
                OR c.metadata->'acl_group_ids' ?| %s
              )
            ORDER BY c.embedding <=> %s
            LIMIT %s
            """,
            (query_embedding, tenant_id, model, groups, query_embedding, limit),
        )
        return cur.fetchall()
```

### C# query shape

Use Npgsql plus the pgvector extension package appropriate for your stack. Keep the same principles:

- Parameterize vectors and filters.
- Apply tenant/ACL filters in SQL.
- Return chunk IDs, titles, URLs, content, and scores.
- Log chunk IDs and scores, not full private content by default.

---

## 7. Operational checklist

- [ ] Run `ANALYZE` after large ingestion jobs.
- [ ] Track index build time and query latency.
- [ ] Tune HNSW/IVFFlat with recall@k evals.
- [ ] Version embedding model and chunker.
- [ ] Back up source docs and vector tables.
- [ ] Monitor table/index bloat.
- [ ] Add tenant and ACL fields to every chunk.
- [ ] Include tenant/ACL/index version in caches.
- [ ] Test deletion and re-index paths.
- [ ] Keep a rollback path for embedding model migrations.

---

## 8. Interview talking points

- pgvector is attractive when the team already operates Postgres and wants SQL joins, transactions, backups, and metadata filters.
- Dedicated vector DBs may be better for very high QPS, very large corpora, or advanced vector-native operations.
- Security depends on metadata filtering and app authorization, not on vector search itself.
- Index metric and embedding model must match.
- Re-embedding requires versioning and evals before promotion.
