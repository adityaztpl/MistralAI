# System Design for AI Applications

## Interview framework

When asked to design an AI app, use this structure:

1. Clarify requirements.
2. Define users and core flows.
3. Draw high-level architecture.
4. Deep dive into retrieval/tools/model orchestration.
5. Discuss data, security, evaluation, scaling, cost, and observability.
6. Name trade-offs and failure modes.

---

## 1. Universal AI app architecture

```mermaid
flowchart LR
  Client[Web/Mobile Client] --> API[Backend API]
  API --> Auth[AuthN/AuthZ]
  API --> Orchestrator[AI Orchestrator]
  Orchestrator --> Retrieval[RAG Retrieval]
  Retrieval --> Vector[(Vector DB)]
  Retrieval --> Source[(Source Documents)]
  Orchestrator --> Tools[Tools/APIs]
  Orchestrator --> LLM[LLM Provider]
  API --> Store[(Conversation DB)]
  API --> Telemetry[Observability]
```

### Key design principle

The LLM is not the system. The system includes data pipelines, authorization, retrieval, prompts, tools, validation, UX, and evaluation.

---

## 2. Clarifying questions

Ask:

- Who are the users?
- What data can the model access?
- Does the answer need citations?
- Is the data private, tenant-scoped, or regulated?
- Are actions/tool calls required?
- Latency target? Cost constraints?
- Need streaming?
- What is the acceptable failure behavior?
- How will quality be measured?

---

## 3. Design prompt: Document Q&A system

### Requirements

- Users upload documents.
- Ask questions with citations.
- Tenant isolation.
- Streaming answers.
- Feedback collection.

### Architecture

```mermaid
flowchart TD
  UI[SPA] --> API[ASP.NET Core API]
  API --> Blob[(Blob storage)]
  API --> Queue[Ingestion queue]
  Queue --> Worker[Ingestion worker]
  Worker --> Parser[PDF/HTML/Markdown parser]
  Parser --> Chunker[Chunker]
  Chunker --> Embedder[Embedding model]
  Embedder --> Vector[(pgvector/Qdrant)]
  UI --> Chat[Chat endpoint]
  Chat --> Retriever[Retriever with ACL filters]
  Retriever --> Vector
  Chat --> LLM[LLM provider]
  Chat --> Db[(Conversations/feedback)]
```

### Query flow

1. Validate user token.
2. Resolve tenant and permissions.
3. Rewrite follow-up question if needed.
4. Retrieve top-k chunks with tenant/ACL filters.
5. Hybrid search and rerank if needed.
6. Build prompt with context and citation IDs.
7. Stream answer.
8. Validate citations.
9. Store message, retrieval metadata, usage.

### Trade-offs

| Choice | Pros | Cons |
|---|---|---|
| pgvector | Simple if already on Postgres | May need tuning at high scale |
| Managed vector DB | Scaling/features | More infra/vendor cost |
| Long context | Simpler retrieval | Higher cost/latency |
| Reranking | Better precision | Extra latency |

---

## 4. Design prompt: AI support agent

### Requirements

- Answer product questions.
- Create support tickets when needed.
- Escalate uncertain answers.
- Use internal knowledge base.

### Architecture

```mermaid
flowchart TD
  U[Customer] --> UI[Support chat UI]
  UI --> API[Support API]
  API --> Graph[Agent graph]
  Graph --> RAG[Knowledge base retriever]
  Graph --> Ticket[Ticket tool]
  Graph --> Account[Account lookup tool]
  Graph --> Human[Human escalation queue]
  Graph --> LLM[LLM]
```

### Agent graph

```mermaid
flowchart TD
  A[Classify intent] --> B{Needs account data?}
  B -->|yes| C[Account lookup tool]
  B -->|no| D[Retrieve docs]
  C --> D
  D --> E[Generate answer]
  E --> F{Confident and grounded?}
  F -->|yes| G[Return answer]
  F -->|no| H[Create ticket draft]
  H --> I[User confirmation]
  I --> J[Create ticket tool]
```

### Safety

- Ticket tool validates fields.
- User confirms before ticket creation.
- Account lookup enforces user identity.
- Tool calls audited.

---

## 5. Design prompt: Enterprise AI search

### Requirements

- Search across docs, tickets, and wiki.
- Respect permissions.
- Return answers and source links.
- Support exact IDs and semantic questions.

### Retrieval design

```mermaid
flowchart LR
  Q[Query] --> QR[Query rewrite]
  QR --> Dense[Vector search]
  QR --> Sparse[BM25 search]
  Dense --> Merge[Reciprocal rank fusion]
  Sparse --> Merge
  Merge --> ACL[Permission filter]
  ACL --> Rerank[Reranker]
  Rerank --> Answer[Grounded answer]
```

Important note: ACL filtering should happen inside search where possible. If not, over-retrieve and filter before prompt assembly, but never pass unauthorized chunks to the model.

---

## 6. Design prompt: Conversational SQL analytics

### Requirements

- Users ask questions about business metrics.
- System generates SQL.
- Results shown as table/chart.
- Prevent unsafe queries.

### Architecture

```mermaid
flowchart TD
  Q[Question] --> Sem[Semantic metric layer]
  Sem --> LLM[SQL generation model]
  LLM --> Val[SQL parser/validator]
  Val -->|valid| DB[(Read replica)]
  Val -->|invalid| Fix[Repair prompt or reject]
  DB --> Viz[Chart/table response]
```

### Guardrails

- Whitelisted tables/views only.
- Read-only DB user.
- Query timeout.
- Row limits.
- SQL parser validation.
- No PII fields unless authorized.
- Explanation of generated query.

---

## 7. Evaluation strategy

### Offline evals

- Golden question set.
- Expected citations.
- Faithfulness scorer.
- Retrieval recall@k.
- Regression tests when prompts/models change.

### Online evals

- User feedback.
- Escalation rate.
- Re-query rate.
- Citation click-through.
- Human review sampling.

### Example quality dashboard

```text
RAG Quality
  Recall@5:              0.86
  Faithfulness:          0.91
  Citation accuracy:     0.88
  Helpful feedback:      78%
  P95 latency:           4.8s
  Avg cost/request:      $0.004
```

---

## 8. Scaling considerations

### Ingestion scale

- Queue document processing.
- Deduplicate content.
- Incremental re-indexing.
- Batch embedding requests.
- Track version/hash per document.

### Query scale

- Cache query embeddings.
- Use approximate nearest neighbor indexes.
- Tune top-k and reranking.
- Stream final answer.
- Use regional provider endpoints if available.

### Cost scale

- Model routing by task complexity.
- Prompt/context compression.
- Token budgets.
- Per-tenant quotas.
- Batch offline tasks.

---

## 9. Security checklist

- Backend-only provider keys.
- Tenant/ACL filters before prompt assembly.
- Prompt injection red-team tests.
- Tool argument validation.
- Human approval for side effects.
- PII redaction and retention policy.
- Audit logs for AI decisions/actions.
- Secrets in vault.

---

## 10. Failure modes and responses

| Failure | Detection | Response |
|---|---|---|
| Retrieval misses relevant doc | Low recall@k, user feedback | Improve chunking, hybrid search, metadata |
| Hallucinated answer | Faithfulness eval, citation validation | Grounded prompt, lower temperature, verifier |
| Cross-tenant leak | Security tests, audit | Enforce filters, test isolation |
| Provider timeout | Logs/metrics | Retry, fallback, graceful error |
| Tool misuse | Audit/review | Validate, authorize, human approval |
| Cost spike | Usage metrics | Quotas, rate limits, token caps |

---

## 11. Whiteboard answer template

```text
I would split this into:
1. Client UX: streaming, citations, feedback.
2. API trust boundary: auth, validation, rate limits.
3. AI orchestration: prompt templates, model calls, tools.
4. Knowledge pipeline: ingestion, chunking, embeddings, vector store.
5. Evaluation/observability: quality, latency, cost, traces.
6. Security: tenant filters, prompt injection, tool permissions.
```

---

## 12. Practice prompts

1. Design a multi-tenant document Q&A platform.
2. Design a coding assistant for internal repositories.
3. Design an AI support agent with ticket creation.
4. Design a healthcare policy assistant with strict compliance.
5. Design an AI ops copilot that can read logs and suggest actions.
6. Design RAG evaluation infrastructure.

