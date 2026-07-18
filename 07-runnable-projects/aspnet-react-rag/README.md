# ASP.NET Core + React + pgvector RAG Tutorial

This project is a full-stack Retrieval-Augmented Generation scaffold built for interview preparation.
It combines an ASP.NET Core Minimal API, a React single-page app, PostgreSQL with `pgvector`, and an
OpenAI-compatible LLM client pattern that works well with Mistral models.

The goal is not to hide behind a framework template. The goal is to show the moving parts you should
be able to explain in a technical screen:

- How documents are chunked, embedded, stored, and retrieved.
- How vector search is expressed in PostgreSQL.
- How a chat endpoint builds a grounded prompt from retrieved context.
- How server-sent events stream tokens to the browser.
- How Docker Compose wires API, web, database, and optional cache dependencies.
- How you would harden this design for production.

## Architecture

```text
Browser React SPA
  | POST /api/chat/stream
  | POST /api/ingest
  v
ASP.NET Core Minimal API
  | calls embeddings + chat completions
  | reads/writes documents
  v
PostgreSQL + pgvector
  | optional conversation/session cache
  v
Redis
```

## Local services

| Service | Port | Purpose |
| --- | ---: | --- |
| `web` | `5173` | React/Vite chat UI |
| `api` | `8080` | ASP.NET Core API |
| `postgres` | `5432` | Documents and embeddings |
| `redis` | `6379` | Optional conversation cache or rate-limit backing store |

## Prerequisites

- Docker Engine or Docker Desktop with Compose v2.
- A Mistral API key in `MISTRAL_API_KEY`.
- Optional local tooling: .NET SDK 8+, Node 20+, `psql`, `curl`.

## Quick start

```bash
cd 07-runnable-projects/aspnet-react-rag
export MISTRAL_API_KEY="your-key"
docker compose up --build
```

Open the UI at <http://localhost:5173>. The API health check is at
<http://localhost:8080/health>.

## Environment variables

| Variable | Example | Notes |
| --- | --- | --- |
| `MISTRAL_API_KEY` | `...` | Required for live embeddings and chat calls |
| `LLM_BASE_URL` | `https://api.mistral.ai/v1` | OpenAI-compatible base URL |
| `CHAT_MODEL` | `mistral-small-latest` | Chat model used for answers |
| `EMBEDDING_MODEL` | `mistral-embed` | Embedding model |
| `POSTGRES_CONNECTION` | `Host=postgres;Port=5432;Database=rag;Username=rag;Password=rag` | API connection string |
| `AUTH_DEMO_TOKEN` | `dev-token` | Demo bearer token for local calls |

## Database model

`infra/init.sql` creates:

- `vector` extension from pgvector.
- `documents` table containing source metadata, raw content, token estimate, and embedding vector.
- ivfflat vector index for approximate nearest-neighbor search.
- GIN index for metadata filtering.

The embedding dimension in this scaffold is `1024`, matching the commonly used Mistral embedding
shape. If your provider/model returns a different dimension, update both the table type and client
validation.

## API design

### `GET /health`

Returns API health, model names, and UTC time. In a production app, split liveness and readiness:

- Liveness: process is up.
- Readiness: dependencies are reachable and migrations are current.

### `POST /api/ingest`

Accepts a source, title, metadata, and content. The endpoint:

1. Validates content length.
2. Splits content into overlapping chunks.
3. Calls the embedding model once per chunk.
4. Inserts each chunk into PostgreSQL with the embedding.
5. Returns chunk ids and counts.

Example:

```bash
curl -X POST http://localhost:8080/api/ingest \
  -H "Authorization: Bearer dev-token" \
  -H "Content-Type: application/json" \
  -d '{
    "source": "handbook",
    "title": "Incident Review Policy",
    "content": "Every severity-one incident requires a timeline, contributing factors, customer impact, and follow-up owners within five business days.",
    "metadata": { "team": "platform", "classification": "internal" }
  }'
```

### `POST /api/chat`

Non-streaming chat endpoint. It:

1. Embeds the user question.
2. Retrieves the top matching document chunks with vector distance.
3. Builds a grounded system/developer prompt.
4. Calls the chat completion model.
5. Returns answer text plus citations.

This is the easiest endpoint to test from an HTTP client.

### `POST /api/chat/stream`

Streaming endpoint using Server-Sent Events. It emits:

- `event: citations` with JSON citation metadata before text streaming.
- `event: token` with partial text deltas.
- `event: done` when complete.
- `event: error` if the provider or API fails.

SSE is simpler than WebSockets for one-way model output and works well behind standard HTTP
infrastructure as long as buffering is disabled.

## Prompting strategy

The API builds a prompt like this:

```text
You are a grounded assistant. Answer only from the supplied context.
If the context is insufficient, say what is missing.
Always cite sources as [1], [2], ...

Context:
[1] Source=handbook Title=Incident Review Policy
Every severity-one incident requires...

Question:
What must be included in a severity-one incident review?
```

Key decisions:

- Keep context excerpts small enough to fit the model context window.
- Include stable citation ids that can be mapped back to records.
- Ask the model to admit missing context.
- Keep safety and output rules outside retrieved user-controlled text.

## Chunking

This scaffold uses simple paragraph-aware chunking with overlap. In production, choose chunking based
on document shape:

- Markdown/docs: split by heading, then by token budget.
- PDFs: preserve page numbers for citations.
- Code: split by symbols/classes/functions.
- Support tickets: split by conversation turn and timestamp.
- Tables: store row/column semantics separately from prose text.

## Retrieval strategy

Baseline query:

```sql
select id, source, title, content, metadata, embedding <=> $1 as distance
from documents
where deleted_at is null
order by embedding <=> $1
limit 6;
```

Enhancements to discuss in interviews:

- Metadata filters for tenant, product, language, ACLs, and document type.
- Hybrid search: vector similarity plus full-text rank.
- Re-ranking with a cross-encoder or LLM scoring pass.
- Query expansion for acronyms and product names.
- Cache embeddings for repeated questions.
- Deduplicate chunks from the same document.

## Security sketch

The sample uses a demo bearer token so the code stays focused. In production:

- Use an identity provider such as Entra ID, Auth0, Cognito, or Okta.
- Validate JWT signature, issuer, audience, expiration, and scopes.
- Enforce tenant and document ACL filters before retrieval.
- Do not rely on prompt instructions for authorization.
- Audit ingestion, deletion, and admin actions.
- Redact sensitive content before logging prompts or completions.

## Observability

Log structured events for:

- Request id, user id, tenant id, endpoint, status, latency.
- Embedding latency and token counts.
- Retrieval latency, top distances, and number of candidates.
- Chat model latency, finish reason, and provider request id.
- SSE disconnects and cancellations.

Metrics to track:

- p50/p95/p99 API latency.
- Provider error rate and retry counts.
- Average retrieved context size.
- No-answer rate.
- Citation click-through rate.
- Cost per successful answer.

## Testing checklist

Unit tests:

- Chunking respects max length and overlap.
- Prompt builder includes citations and refusal instruction.
- Vector serialization validates dimensions.
- Auth handler rejects missing or wrong token.

Integration tests:

- Containerized PostgreSQL accepts `vector` extension.
- `/ingest` inserts expected chunks.
- `/chat` returns citations when relevant content exists.
- `/chat/stream` emits `citations`, `token`, and `done` events.

Manual tests:

- Ask a question with known answer.
- Ask a question outside ingested context.
- Cancel the browser request mid-stream.
- Restart API while database persists.

## Scaling notes

For larger deployments:

1. Move ingestion into a queue-backed worker.
2. Store raw files in object storage and chunks in Postgres.
3. Add a document status table: uploaded, parsing, embedded, searchable, failed.
4. Use batch embedding calls when the provider supports them.
5. Partition by tenant or document namespace.
6. Add rate limits per user and per tenant.
7. Track model cost by request and customer.
8. Use read replicas only if vector extension and indexes support your query profile.

## Common failure modes

- **Embedding dimension mismatch**: the provider returns vectors that do not match `vector(1024)`.
- **Poor citations**: chunks are too large, too small, or missing source metadata.
- **Hallucination**: prompt does not force no-answer behavior, or too little context was retrieved.
- **Slow answers**: embedding, vector search, and chat calls are serialized; cache or parallelize where safe.
- **SSE buffering**: reverse proxy buffers response; disable buffering for streaming routes.
- **Secret leakage**: logs include full prompt with sensitive retrieved text.

## Interview checklist

Be ready to explain:

- Why you chose pgvector instead of a managed vector database.
- How you would support per-tenant authorization.
- How token limits influence chunking and retrieval count.
- Why SSE is enough for chat streaming.
- How to measure answer quality offline.
- How to recover from partial ingestion failures.
- How to migrate to a different model provider.
- How to keep costs predictable.

## Suggested extensions

- Add file upload and background parsing.
- Add hybrid full-text/vector search.
- Add source filters in the UI.
- Add conversation memory with Redis.
- Add OpenTelemetry tracing.
- Add integration tests with Testcontainers.
- Add a reranker step before prompt construction.

