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

---

## Phase 8: Advanced Production Modules and Instagram Capstone

This phase extends the original 24-week path with sections `07` through `16`. Use it after the core ASP.NET, frontend, GenAI, integration, and interview-prep material, or interleave individual weeks when you need focused practice.

### Week 25: Runnable project blueprints

**Read**

- `07-runnable-projects/README.md`

**Goals**

- Study runnable, interview-ready project blueprints that combine backend, frontend, database, auth, observability, and AI integration.
- Practice explaining local topology, setup, demo flow, tests, and trade-offs.
- Use the blueprints as portfolio starting points or timed implementation drills.

**Build**

- Run or walk through the ASP.NET + React RAG project.
- Run or walk through the ASP.NET + Angular notes project.
- Add one meaningful test or hardening improvement to a blueprint.
- Write a demo script that starts from setup and ends with interview trade-offs.

**Interview focus**

- Explaining a runnable app quickly.
- Local Docker topology.
- Auth, tenant ownership, validation, and observability in a small project.
- Turning a scaffold into a production-ready implementation.

---

### Week 26: Mistral APIs and production usage

**Read**

- `08-mistral/README.md`

**Goals**

- Build fluency with Mistral chat completions, streaming, embeddings, function calling, agents, cost, and limits.
- Practice Python and .NET client examples.
- Explain when to use Mistral directly, through OpenAI-compatible endpoints, or behind an orchestration framework.

**Build**

- Run a chat completion example.
- Run a streaming example.
- Run or inspect an embeddings/RAG example.
- Explain a tool-calling loop with deterministic server-side execution.

**Interview focus**

- Mistral model choice.
- OpenAI-compatible API usage.
- Streaming and cancellation.
- Embeddings and RAG.
- Function calling, cost controls, and rate limits.

---

### Week 27: Take-home project kits

**Read**

- `09-take-home-kits/README.md`
- `09-take-home-kits/submission-checklist.md`

**Goals**

- Practice timed project delivery.
- Learn how to scope an impressive but finishable submission.
- Build README, tests, screenshots, and trade-off notes.

**Build**

- Complete either the 2-hour Notes API or 8-hour RAG Chat kit.
- Write a submission README with setup, architecture, trade-offs, and known limitations.
- Add at least one meaningful test per risky behavior.

**Interview focus**

- Timeboxing.
- Communicating trade-offs.
- Showing test judgment.
- Explaining what you would do next.

---

### Week 28: Debugging playbooks

**Read**

- `10-debugging-playbooks/README.md`
- Pick three playbooks most relevant to your target role.

**Goals**

- Practice systematic troubleshooting.
- Move from symptom to hypothesis to evidence to fix.
- Build vocabulary for production incidents.

**Build**

- Reproduce one auth, one frontend, and one GenAI failure locally or as a written simulation.
- Write a short incident note: impact, root cause, fix, prevention.

**Interview focus**

- JWT 401 loops.
- CORS preflight failures.
- RAG wrong-doc retrieval.
- SSE stalls.
- EF Core N+1 queries.

---

### Week 29: Data platform depth

**Read**

- `11-data-platform/README.md`

**Goals**

- Strengthen SQL, EF migrations, Redis caching, queues, Docker Compose, CI, and Azure deployment foundations.
- Understand the persistence layer behind full-stack AI applications.

**Build**

- Add an EF migration with a safe rollback note.
- Add a queued background job.
- Add Redis cache-aside for a read-heavy endpoint.
- Run the stack with Docker Compose.

**Interview focus**

- Index design.
- EF migration pitfalls.
- Cache invalidation.
- Queue retries and idempotency.
- Deployment shape for .NET services.

---

### Week 30: Decision sheets

**Read**

- `12-decision-sheets/README.md`

**Goals**

- Practice choosing technologies based on requirements, not slogans.
- Build concise comparison answers for architecture interviews.

**Practice**

- Explain Angular vs React for a team scenario.
- Explain Controllers vs Minimal APIs for an enterprise API.
- Explain LangChain vs LangGraph vs Semantic Kernel.
- Explain RAG vs fine-tuning vs tools.
- Explain SQL vs vector vs hybrid search.

**Interview focus**

- Requirements first.
- Forces and constraints.
- Default recommendation.
- Conditions that would change your decision.
- Validation metric or prototype.

---

### Week 31: Flashcards and active recall

**Read**

- `13-flashcards/README.md`

**Goals**

- Convert passive notes into answers you can deliver under pressure.
- Practice 60-second explanations across ASP.NET, frontend, GenAI, Mistral, and system design.

**Practice**

- 20 mixed flashcards per day.
- 5 verbal drills recorded and reviewed.
- Rewrite weak answers using: definition -> why it matters -> example -> trade-off.

**Interview focus**

- Concision.
- Correctness.
- Example-driven explanations.
- Naming trade-offs and failure modes.

---

### Week 32: Whiteboard pack

**Read**

- `14-whiteboard-pack/README.md`

**Goals**

- Practice 35-45 minute full-stack + GenAI system-design conversations.
- Use repeatable structure for requirements, APIs, data, architecture, sequence, risks, metrics, and rollout.

**Practice**

- Whiteboard multi-tenant RAG.
- Whiteboard streaming chat.
- Whiteboard an agent with human-in-the-loop.
- Whiteboard rate limits and cost control.

**Interview focus**

- Clarifying scope.
- Drawing clean boundaries.
- Deep-diving tenant isolation, reliability, and AI evaluation.
- Ending with metrics and trade-offs.

---

### Week 33: Security lab

**Read**

- `15-security-lab/README.md`
- `15-security-lab/01-broken-jwt.md`
- `15-security-lab/02-idor-on-documents.md`
- `15-security-lab/03-prompt-injection.md`
- `15-security-lab/04-insecure-tool-calling.md`
- `15-security-lab/05-cors-and-xss-spa.md`
- `15-security-lab/06-secrets-and-config.md`
- `15-security-lab/07-lab-checklist.md`

**Goals**

- Learn from deliberately vulnerable sandbox samples.
- Explain broken behavior safely at a high level.
- Harden JWT validation, document authorization, prompt boundaries, tool gateways, browser rendering, and secret handling.

**Build**

- Write tests for tampered/expired/wrong-audience JWTs.
- Write cross-tenant IDOR tests.
- Add prompt-injection eval cases.
- Design a tool gateway policy.
- Add a secret-rotation runbook.

**Interview focus**

- Authentication vs authorization.
- IDOR prevention.
- Prompt injection defense layers.
- Tool authorization.
- CORS vs XSS.
- Secret management and rotation.

---

### Week 34: Instagram content creator capstone

**Read**

- `16-capstone-instagram/README.md`
- `16-capstone-instagram/01-wrap-with-aspnet.md`
- `16-capstone-instagram/02-spa-dashboard.md`
- `16-capstone-instagram/03-eval-metrics.md`
- `16-capstone-instagram/04-auth-tenancy-cost.md`
- `16-capstone-instagram/05-deployment.md`
- `16-capstone-instagram/architecture.md`

**Goals**

- Turn `src/instagram_content_creator` into a full-stack capstone architecture.
- Wrap CrewAI/Mistral generation with an ASP.NET API facade.
- Add a dashboard, run history, eval scoring, feedback, tenancy, cost controls, deployment plan, and interview diagrams.

**Build**

- Sketch or implement `POST /api/content-runs`.
- Add a background worker around the Python crew.
- Build a React or Angular dashboard for run creation/history.
- Score generated artifacts with the rubric.
- Add auth, budget, and tenant checks.
- Prepare a portfolio README and demo script.

**Interview focus**

- Why async job architecture fits AI generation.
- How API, worker, Python crew, Mistral, DB, and SPA interact.
- How evals and human feedback improve output quality.
- How tenant isolation and cost controls work.
- How to deploy and operate the capstone.

---

### Final advanced readiness checklist

- [ ] Can explain every section `07` through `16` from the root README.
- [ ] Can show at least one tested backend endpoint and one tested frontend flow.
- [ ] Can debug an auth, CORS, RAG, or streaming failure systematically.
- [ ] Can compare architecture options using the decision-sheet template.
- [ ] Can answer flashcards out loud without notes.
- [ ] Can whiteboard a multi-tenant AI system in 45 minutes.
- [ ] Can explain each security lab's broken pattern and hardened fix.
- [ ] Can present the Instagram capstone as a product, not only a script.

