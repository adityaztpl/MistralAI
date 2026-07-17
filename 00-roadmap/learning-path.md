# Week-by-Week Learning Path: Fullstack + GenAI

This roadmap moves from fundamentals to production-ready GenAI fullstack architecture. It assumes 8-12 focused hours per week. If you have more time, build the weekly deliverable fully; if less, read the material and implement the smallest working slice.

---

## Phase 1: Foundations

### Week 1: Web/API fundamentals and developer workflow

**Goals**

- Understand HTTP, REST, JSON, status codes, auth headers.
- Set up a clean Git workflow.
- Build a small ASP.NET Core API.

**Build**

- `/health` endpoint.
- CRUD endpoint for notes.
- Request validation and Problem Details.

**Interview focus**

- Request/response lifecycle.
- REST endpoint design.
- Error handling.

---

### Week 2: ASP.NET Core core concepts

**Goals**

- Middleware pipeline.
- Dependency injection lifetimes.
- Configuration and options.
- Logging and correlation IDs.

**Build**

- Custom correlation ID middleware.
- Scoped service with validation.
- Typed `HttpClient` for a fake external API.

**Read**

- `06-interview-prep/questions-aspnet.md`

---

### Week 3: Data access and background processing

**Goals**

- EF Core basics.
- Migrations.
- Read vs write models.
- Background ingestion jobs.

**Build**

- Document metadata table.
- Upload endpoint that queues ingestion.
- Background worker that marks jobs complete.

**Interview focus**

- EF tracking vs no-tracking.
- Async/cancellation.
- Background workers vs request processing.

---

### Week 4: Frontend fundamentals

**Goals**

- Choose Angular or React as primary frontend.
- Understand component state, routing, forms, API clients.
- Handle loading/error states.

**Build**

- SPA with login placeholder.
- Notes/document list page.
- Form validation.

**Read**

- `06-interview-prep/questions-angular-react.md`

---

## Phase 2: GenAI Basics

### Week 5: LLM foundations

**Goals**

- Tokens, context windows, embeddings.
- Temperature and sampling.
- Prompt engineering basics.
- Hallucination mitigation.

**Build**

- Prompt playground endpoint.
- UI to change temperature/model/max tokens.
- Log token usage and latency.

**Read**

- `04-genai/01-foundations.md`
- `06-interview-prep/questions-genai.md`

---

### Week 6: Provider APIs and streaming

**Goals**

- Mistral/OpenAI-style chat APIs.
- Server-side provider calls.
- Streaming responses.
- Cancellation.

**Build**

- `POST /api/chat`
- `POST /api/chat/stream`
- React/Angular streaming chat UI with stop button.

**Interview focus**

- Why browser does not call provider directly.
- SSE vs WebSocket.
- Time to first token.

---

### Week 7: Embeddings and semantic search

**Goals**

- Embedding models.
- Similarity search.
- Vector stores.
- Metadata.

**Build**

- Ingest a few Markdown docs.
- Chunk and embed documents.
- Store vectors locally with Chroma/FAISS or Postgres pgvector.
- Search endpoint that returns top chunks.

**Code**

- `04-genai/examples/01-rag/basic_rag.py`

---

## Phase 3: RAG

### Week 8: Basic RAG

**Goals**

- End-to-end retrieve -> prompt -> answer flow.
- Citations.
- Grounded prompting.

**Build**

- Document Q&A endpoint.
- Answer includes citations.
- UI source panel.

**Read**

- `04-genai/02-rag.md`

---

### Week 9: Advanced retrieval

**Goals**

- Chunking strategies.
- Hybrid search.
- Reranking.
- Query rewriting.

**Build**

- Hybrid retriever.
- Metadata filters.
- Rerank top 20 to top 5.

**Code**

- `04-genai/examples/01-rag/hybrid_retriever.py`

---

### Week 10: RAG evaluation

**Goals**

- Golden datasets.
- Recall@k.
- Faithfulness.
- Context precision.
- Citation validation.

**Build**

- 20-question eval set.
- Script that runs retrieval and generation.
- Dashboard/report of failures.

**Interview focus**

- "How do you know the RAG system is good?"
- Retrieval vs generation failure categories.

---

## Phase 4: Frameworks and Orchestration

### Week 11: LangChain

**Goals**

- LCEL chains.
- Prompt templates.
- Document loaders/splitters.
- Retrievers.
- Structured outputs.

**Build**

- LCEL RAG chain.
- Structured support-ticket classifier.

**Read/code**

- `04-genai/03-langchain.md`
- `04-genai/examples/02-langchain/lcel_chain.py`
- `04-genai/examples/02-langchain/rag_chain.py`

---

### Week 12: Tools and agents

**Goals**

- Tool calling.
- ReAct pattern.
- Agent risks.
- Server-side execution.

**Build**

- Tool that creates a draft support ticket.
- Human confirmation before submission.

**Code**

- `04-genai/examples/02-langchain/tool_agent.py`

---

### Week 13: LangGraph

**Goals**

- StateGraph.
- Typed state.
- Nodes and edges.
- Conditional routing.
- Checkpoints.

**Build**

- Agentic RAG graph:
  - retrieve
  - grade docs
  - rewrite query
  - generate
  - fallback

**Read/code**

- `04-genai/04-langgraph.md`
- `04-genai/examples/03-langgraph/simple_graph.py`
- `04-genai/examples/03-langgraph/agentic_rag.py`

---

### Week 14: Multi-agent and human-in-the-loop

**Goals**

- Supervisor/specialist patterns.
- Human approval.
- Loop limits.
- Checkpointed workflows.

**Build**

- Multi-agent research assistant.
- Reviewer step that checks citations.
- Approval before sending an email/ticket.

**Code**

- `04-genai/examples/03-langgraph/multi_agent.py`

---

## Phase 5: .NET GenAI and Fullstack Integration

### Week 15: Semantic Kernel for .NET

**Goals**

- Kernel setup.
- Chat completion services.
- Prompt functions.
- Native plugins.

**Build**

- ASP.NET Core API using Semantic Kernel.
- Plugin that looks up product policy.

**Read/code**

- `04-genai/05-dotnet-semantic-kernel.md`
- `04-genai/examples/04-dotnet-semantic-kernel/ChatController.cs`

---

### Week 16: .NET RAG and streaming

**Goals**

- RAG service in ASP.NET Core.
- SSE streaming.
- Cancellation tokens.
- Citation DTOs.

**Build**

- `/api/rag/query`
- `/api/chat/stream`
- UI with citations and stop-generation.

**Code**

- `04-genai/examples/04-dotnet-semantic-kernel/RagService.cs`
- `04-genai/examples/04-dotnet-semantic-kernel/StreamingChat.cs`

---

### Week 17: Fullstack architecture hardening

**Goals**

- Auth flow.
- Tenant isolation.
- Deployment topology.
- Observability.
- Cost controls.

**Build**

- Add JWT auth.
- Add tenant filter to document retrieval.
- Add request tracing and usage logs.

**Read**

- `05-fullstack-integration/architecture.md`
- `05-fullstack-integration/sample-api-contracts.md`

---

## Phase 6: Portfolio and Advanced Systems

### Week 18: Project 1 capstone - Document Q&A

**Build**

- Upload documents.
- Background ingestion.
- RAG chat with citations.
- Streaming UI.
- Feedback capture.

**Deliverables**

- Architecture diagram.
- README with trade-offs.
- Demo script.
- Eval results.

---

### Week 19: Project 2 capstone - Support agent

**Build**

- RAG over support docs.
- Tool call to create ticket draft.
- Human confirmation.
- Admin unresolved-questions dashboard.

**Focus**

- Tool safety.
- Escalation.
- UX around uncertainty.

---

### Week 20: Project 3 capstone - AI operations copilot

**Build**

- RAG over runbooks.
- Tool calls to fake logs/metrics APIs.
- Incident summary.
- Suggested next actions.
- Human approval for risky operations.

**Read**

- `05-fullstack-integration/project-ideas.md`

---

## Phase 7: Interview readiness

### Week 21: Backend/frontend interview drills

**Practice**

- ASP.NET Core pipeline and DI.
- Auth and tenant isolation.
- React/Angular streaming chat UI.
- API error handling.

**Deliverable**

- 10 recorded answers, 2 minutes each.

---

### Week 22: GenAI interview drills

**Practice**

- RAG end-to-end.
- Chunking, hybrid search, reranking.
- Tools vs RAG vs fine-tuning.
- Hallucination mitigation.
- Evaluation.

**Deliverable**

- Whiteboard RAG architecture from memory.

---

### Week 23: System design drills

**Practice**

- Document Q&A platform.
- Support agent.
- Conversational analytics.
- Enterprise AI search.

**Read**

- `06-interview-prep/system-design-ai-apps.md`

---

### Week 24: Polish and mock interviews

**Goals**

- Refine portfolio READMEs.
- Prepare STAR stories.
- Practice trade-off discussions.
- Review weak areas.

**Final checklist**

- [ ] Can explain every architecture diagram.
- [ ] Can run at least one RAG example.
- [ ] Can demo streaming chat.
- [ ] Can describe evaluation metrics.
- [ ] Can explain security controls.
- [ ] Can compare LangChain, LangGraph, and Semantic Kernel.
- [ ] Can design an AI app in 45 minutes.

---

## Optional advanced extensions

- Add OpenTelemetry tracing.
- Add LangSmith or custom eval dashboard.
- Add pgvector with HNSW/IVFFlat index tuning.
- Add Azure AI Search or Elasticsearch hybrid retrieval.
- Add Mistral embeddings/chat provider implementation.
- Add prompt/version registry.
- Add per-tenant cost reporting.
- Add red-team prompt injection tests.

