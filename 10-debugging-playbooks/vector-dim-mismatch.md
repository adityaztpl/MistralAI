# Debugging Playbook: Vector Dimension Mismatch

## Symptoms

- Insert into vector DB fails with dimension error.
- Search fails after changing embedding model.
- Some documents search correctly while newer documents fail.
- pgvector error mentions expected dimension, such as `expected 1536 dimensions, not 1024`.
- Qdrant/Pinecone collection rejects vectors.
- RAG app works locally but fails against existing staging index.

## Reproduce

1. Log embedding model name and vector length for query and document embeddings.
2. Inspect vector store schema/collection dimension.
3. Try inserting a single known vector into the target collection.
4. Compare old and new deployments/config values.
5. Check whether partial re-ingestion mixed models in one index.

Quick check in code:

```python
vector = embed("test")
print(len(vector), embedding_model_name)
```

pgvector schema check:

```sql
SELECT atttypmod
FROM pg_attribute
WHERE attrelid = 'rag_chunks'::regclass
  AND attname = 'embedding';
```

In practice, also check migrations or `\d rag_chunks` for `vector(1024)` style declarations.

## Diagnose

Common causes:

- Embedding model changed from one dimension to another.
- Query embeddings use a different provider/model than document embeddings.
- Database migration created `vector(1536)` but app now emits 1024.
- Vector collection was created once and not recreated for new model.
- Old and new chunks coexist without model metadata.
- Tests use mock embeddings with the wrong length.

Questions to answer:

- What model embedded existing documents?
- What model embeds current queries?
- What dimension does the index require?
- Is the dimension fixed by schema or collection config?
- Is there a planned migration/reindex path?

## Fix

### Short-term unblock

- Revert to the embedding model that matches the existing index, or
- Point the app to a new collection/table with the new dimension.

### Correct migration

1. Create a new vector column/table/collection for the new dimension.
2. Store `embedding_model` and `embedding_dimension` with every chunk.
3. Re-embed documents into the new index.
4. Dual-read or switch traffic after validation.
5. Drop old vectors only after rollback window.

pgvector example:

```sql
ALTER TABLE rag_chunks ADD COLUMN embedding_v2 vector(1024);
ALTER TABLE rag_chunks ADD COLUMN embedding_v2_model text;

CREATE INDEX rag_chunks_embedding_v2_hnsw_idx
ON rag_chunks USING hnsw (embedding_v2 vector_cosine_ops);
```

### Guard in application code

```csharp
if (embedding.Length != options.ExpectedEmbeddingDimension)
{
    throw new InvalidOperationException(
        $"Embedding dimension mismatch. Expected {options.ExpectedEmbeddingDimension}, got {embedding.Length} from {options.ModelName}.");
}
```

Fail early with a clear message before corrupting the index.

## Prevention

- Store model name and dimension in chunk metadata.
- Validate vector length before insert and before search.
- Version vector collections by model/dimension.
- Add startup health check that embeds a probe string and compares length to index schema.
- Add migration runbook for embedding model changes.
- Include dimension in environment config names, e.g. `RAG_EMBEDDING_DIMENSION=1024`.
- Do not mix embeddings from different models in the same ANN index unless explicitly supported and evaluated.

## Interview phrasing

> I would verify the vector length emitted by the app and compare it to the vector store schema or collection dimension. Then I would check whether document and query embeddings use the same model. The safe fix is either reverting to the matching model or creating a new versioned index and re-embedding. To prevent recurrence I would store embedding model/dimension metadata, validate dimensions at startup and insert time, and treat embedding model changes as data migrations.
