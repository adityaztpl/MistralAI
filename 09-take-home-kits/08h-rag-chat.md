# 8-Hour Take-Home: RAG Chat

## Interview-ready summary

Build a small retrieval-augmented chat application that answers questions over a provided document set. The strongest submissions show that RAG is more than vector search: ingestion, metadata, access boundaries, retrieval evaluation, prompt construction, streaming, citations, and failure handling all matter.

## Timebox

Maximum: 8 focused hours.

Recommended split:

| Phase | Time | Goal |
|---|---:|---|
| Prompt analysis and design note | 30 min | Lock scope and architecture |
| Project setup | 45 min | API, UI, vector store, sample docs |
| Ingestion pipeline | 75 min | Parse, chunk, embed, index |
| Retrieval and generation API | 120 min | Query rewrite, retrieve, rerank/filter, prompt, cite |
| Chat UI | 90 min | Ask questions, stream answer, show sources |
| Tests and evaluation | 90 min | Unit/integration tests plus small retrieval eval |
| Observability and polish | 45 min | Logs, errors, cancellation, loading states |
| README and cleanup | 45 min | Setup, assumptions, trade-offs, demo script |

## Product brief

Create a chat app for asking questions over a small knowledge base. The reviewer should be able to:

1. Add or load documents.
2. Ask a question.
3. Receive a grounded answer.
4. Inspect citations/source snippets.
5. See when the system does not know.
6. Run tests and a small evaluation set.

## Required features

### Ingestion

- Accept Markdown, text, or PDF documents. Markdown/text is sufficient if the prompt does not mandate PDF.
- Preserve metadata: source id, title, path/URL, section heading, chunk index, updated time.
- Chunk documents with a documented strategy.
- Generate embeddings.
- Store vectors and metadata.
- Provide a repeatable seed or ingest command.

### Retrieval

- Embed the user query.
- Retrieve top candidates.
- Apply metadata filters if available.
- Optionally rerank candidates.
- Deduplicate near-identical chunks.
- Return source snippets with scores for debugging.

### Generation

- Build a prompt that instructs the model to answer only from context.
- Include citations in the answer.
- Return a fallback when context is insufficient.
- Avoid exposing hidden prompt text to the UI.
- Support cancellation/timeouts.

### Chat UX

- User can type a question and submit.
- UI displays streaming or near-streaming answer updates.
- UI displays source cards with title, section, snippet, and score if appropriate.
- UI handles loading, error, empty state, and no-answer state.
- UI keeps a short conversation history or clearly documents stateless behavior.

### Tests and evaluation

- At least one API test for chat.
- At least one ingestion/chunking test.
- At least one retrieval test with known expected source.
- A tiny evaluation file with 5-10 questions and expected citations.

## Architecture options

### Option A: ASP.NET Core + Semantic Kernel + React/Angular

```text
React/Angular UI
  -> ASP.NET Core API
      -> Ingestion service
      -> Retrieval service
      -> Semantic Kernel chat service
      -> Vector DB or pgvector
```

Best when the role emphasizes .NET full-stack.

### Option B: FastAPI RAG service + React UI

```text
React UI
  -> FastAPI API
      -> loaders/chunkers
      -> embeddings provider
      -> vector store
      -> chat completion provider
```

Best when the role emphasizes Python AI application engineering.

### Option C: Single backend only

If time is tight, build a strong API with a minimal HTML or Swagger-based demo. Document that UI polish was intentionally scoped out.

## Suggested API contract

### Ingest documents

```http
POST /api/documents/ingest
Content-Type: application/json

{
  "sourcePath": "data/handbook.md",
  "tenantId": "demo",
  "tags": ["handbook"]
}
```

Response:

```json
{
  "documentId": "handbook-2026",
  "chunksCreated": 42,
  "embeddingModel": "mistral-embed",
  "embeddingDimension": 1024,
  "durationMs": 1840
}
```

### Ask question

```http
POST /api/chat
Content-Type: application/json

{
  "conversationId": "demo-1",
  "question": "What is the vacation carryover policy?",
  "filters": {
    "tenantId": "demo",
    "tags": ["handbook"]
  }
}
```

Response for non-streaming:

```json
{
  "answer": "Employees may carry over up to 40 hours of unused vacation with manager approval [1].",
  "citations": [
    {
      "id": "1",
      "documentId": "handbook-2026",
      "title": "Employee Handbook",
      "section": "Vacation carryover",
      "snippet": "Employees may carry over up to 40 hours...",
      "score": 0.84
    }
  ],
  "usage": {
    "inputTokens": 1720,
    "outputTokens": 86
  }
}
```

Streaming can use Server-Sent Events:

```text
event: token
data: {"text":"Employees "}

event: citation
data: {"id":"1","title":"Employee Handbook"}

event: done
data: {"finishReason":"stop"}
```

## Data model sketch

```sql
CREATE TABLE documents (
  id TEXT PRIMARY KEY,
  tenant_id TEXT NOT NULL,
  title TEXT NOT NULL,
  source_uri TEXT NOT NULL,
  content_hash TEXT NOT NULL,
  created_at TEXT NOT NULL,
  updated_at TEXT NOT NULL
);

CREATE TABLE chunks (
  id TEXT PRIMARY KEY,
  document_id TEXT NOT NULL REFERENCES documents(id),
  tenant_id TEXT NOT NULL,
  chunk_index INTEGER NOT NULL,
  heading TEXT,
  content TEXT NOT NULL,
  token_count INTEGER NOT NULL,
  embedding_model TEXT NOT NULL,
  embedding_dimension INTEGER NOT NULL,
  metadata_json TEXT NOT NULL
);
```

For pgvector add:

```sql
CREATE EXTENSION IF NOT EXISTS vector;
ALTER TABLE chunks ADD COLUMN embedding vector(1024);
CREATE INDEX chunks_embedding_cosine_idx
ON chunks USING hnsw (embedding vector_cosine_ops);
```

## Chunking guidance

Default approach:

- Split by Markdown headings first.
- Fall back to paragraphs and sentences.
- Target 500-900 tokens per chunk.
- Use 80-150 token overlap for policy/procedure docs.
- Keep title and heading in chunk metadata.
- Strip navigation/boilerplate.

Bad chunking often causes wrong answers even when the generation prompt is good.

## Prompt template

```text
You are a careful assistant answering questions from the provided sources.

Rules:
- Use only the context below.
- If the context does not contain the answer, say you do not know.
- Cite every factual claim with source ids like [1] or [2].
- Do not invent policy, dates, limits, prices, or names.
- Prefer concise answers, then add caveats if needed.

Question:
{question}

Context:
{numbered_context_blocks}
```

## Retrieval evaluation

Create `evals/questions.jsonl`:

```jsonl
{"question":"How many vacation hours can be carried over?","expected_sources":["handbook-vacation"],"must_include":["40 hours"]}
{"question":"Who approves production database access?","expected_sources":["security-access"],"must_include":["security lead"]}
```

Evaluation script should report:

- Recall@k for expected source ids.
- Whether generated answer contains required facts.
- Whether unsupported questions return an uncertainty answer.
- Average latency and token usage if available.

A small, honest eval beats a vague statement like "retrieval seems good."

## Failure modes to handle

| Failure | User-visible behavior | Engineering response |
|---|---|---|
| No documents indexed | Explain that ingestion is required | Return 400/409 with setup hint |
| No relevant chunks | Say answer is not in sources | Log query and top scores |
| Embedding provider failure | Friendly retryable error | Timeout, retry with backoff, alert/log |
| LLM timeout | Partial answer cancellation or error | Propagate cancellation token |
| Prompt too large | Trim/rerank context | Track token budget |
| Citation mismatch | Do not show unsupported citations | Validate citation ids against retrieved chunks |

## Security and privacy expectations

Even for a demo, mention:

- API owns provider keys; frontend never calls model providers directly.
- Tenant/user filters must be applied before retrieval results reach the prompt.
- Uploaded documents may contain sensitive data; avoid logging raw content in production.
- Rate limits and request size limits protect cost.
- Prompt injection in documents is possible; retrieved content is data, not instructions.

## README expectations

Include:

- Architecture diagram or bullet flow.
- Setup commands.
- Required environment variables.
- How to ingest sample docs.
- How to run the app.
- How to run tests/evals.
- Known limitations.
- Next steps.

## Common mistakes

- Returning answers without citations.
- Treating top-k vector search as the whole solution.
- Losing source metadata during chunking.
- Hardcoding a single happy-path document.
- Letting the UI call LLM APIs directly.
- Ignoring token limits until prompts fail.
- No strategy for "I do not know."
- No evaluation beyond manual clicking.

## Stretch goals

Only add these after the required path works:

- Hybrid search: keyword plus vector.
- Reranking with a cross-encoder or LLM judge.
- Conversation-aware query rewriting.
- Document upload UI.
- Per-source ACL filtering.
- Observability trace with retrieval scores and model usage.
- Feedback buttons that write eval labels.
- Docker Compose for API, UI, DB, vector store.

## Follow-up interview phrasing

> I built the RAG app around a clear ingestion-retrieval-generation pipeline. The key design choice was preserving source metadata through chunking so citations could be validated. I used a small eval set to measure whether expected documents were retrieved, because a plausible answer is not enough. I kept provider keys server-side and treated retrieved document text as untrusted data. With more time I would add hybrid search, tenant ACLs, and tracing across ingestion, retrieval, and generation.
