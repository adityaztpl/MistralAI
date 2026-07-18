# Debugging Playbook: RAG Returns Wrong Documents

## Symptoms

- The answer is fluent but cites irrelevant or stale documents.
- Correct source exists in the corpus but is not in top-k retrieval results.
- Similar questions produce unstable citations.
- Vector scores look close together, making ranking ambiguous.
- Users report that the system answers from another product, tenant, region, or version.
- Manual keyword search finds the right page, but vector search does not.

## Reproduce

1. Capture the exact user question, tenant/user context, filters, conversation history, and timestamp.
2. Run the same query in a local or staging retrieval debug endpoint.
3. Log the top 20 chunks before generation: chunk id, source title, heading, score, metadata, and snippet.
4. Run a keyword search for the same query to compare semantic vs lexical behavior.
5. Try query variants:
   - Original user wording.
   - Keyword-only version.
   - Expanded version with product names and acronyms.
6. Check whether the expected document exists in the index and when it was embedded.

Example debug output:

```json
{
  "query": "How do I rotate a service token?",
  "filters": { "tenantId": "demo", "product": "api" },
  "results": [
    { "rank": 1, "chunkId": "billing-003", "score": 0.82, "title": "Billing tokens" },
    { "rank": 2, "chunkId": "api-token-007", "score": 0.79, "title": "API token rotation" }
  ]
}
```

## Diagnose

Work down the pipeline.

### 1. Corpus and ingestion

- Is the correct document indexed?
- Did ingestion fail silently?
- Are duplicate/stale versions present?
- Did the parser drop headings, tables, or code blocks?
- Are source IDs stable across re-ingestion?

Useful checks:

```sql
SELECT source_uri, title, updated_at, COUNT(*) AS chunks
FROM rag_chunks
WHERE source_uri ILIKE '%token%'
GROUP BY source_uri, title, updated_at
ORDER BY updated_at DESC;
```

### 2. Chunking

- Is the answer split away from the heading that gives it meaning?
- Are chunks too small to carry context?
- Are chunks too large and diluted?
- Is overlap missing around boundary facts?
- Is boilerplate repeated in every chunk?

### 3. Metadata filters

- Are tenant, product, region, version, or ACL filters applied before generation?
- Are filters too broad, allowing wrong documents?
- Are filters too narrow, excluding correct documents?
- Is metadata type mismatched (`tenant_id` string vs array)?

### 4. Embeddings and search

- Did the query and documents use the same embedding model?
- Did the embedding dimension change?
- Is the similarity metric correct for the model (cosine vs dot product)?
- Is the approximate nearest neighbor index stale or under-tuned?
- Is top-k too low?

### 5. Reranking and prompt assembly

- Is reranking favoring long chunks or exact keywords incorrectly?
- Are retrieved chunks sorted differently before prompt construction?
- Is context trimmed after retrieval, removing the best chunk?
- Are citation IDs mapped to the wrong chunks?

## Fix

Choose the fix that matches the evidence.

### If the correct document is missing

- Fix ingestion errors.
- Add content hash checks and ingestion status reporting.
- Re-ingest affected sources.
- Add alerts for failed source loads.

### If chunking is poor

- Switch to structure-aware chunking for Markdown/HTML.
- Include headings and parent titles in chunk text or metadata.
- Tune chunk size and overlap by document type.
- Remove boilerplate before embedding.

### If retrieval is semantically weak

- Add hybrid search: keyword BM25 plus vector.
- Increase top-k before reranking.
- Add query rewriting for acronyms and product names.
- Use a reranker to improve top results.

### If metadata is wrong

- Normalize metadata at ingestion.
- Require tenant/ACL filters in retrieval APIs.
- Add tests that assert forbidden tenant chunks never appear.

### If prompt assembly is wrong

- Validate citation IDs against retrieved chunks.
- Log final context block IDs and token counts.
- Trim low-score chunks first rather than newest or arbitrary chunks.

## Prevention

- Maintain a golden retrieval eval set with expected source IDs.
- Track Recall@k and no-answer accuracy in CI or nightly jobs.
- Log retrieval traces for sampled production requests.
- Version embedding model, chunker, and index schema.
- Add ingestion dashboards: documents loaded, chunks created, failures, stale sources.
- Add a debug endpoint or admin view for top-k chunks.
- Code review checklist: metadata preserved, filters applied, citations validated.

## Interview phrasing

> I would not start by changing the prompt. I would first inspect the retrieval trace: exact query, filters, top-k chunks, scores, and metadata. If the right document is absent, I would debug ingestion, chunking, embeddings, and filters. If it is present but not used, I would inspect reranking and prompt assembly. The prevention step is a small retrieval eval set with expected source IDs and logging that lets us compare wrong answers to retrieval evidence.
