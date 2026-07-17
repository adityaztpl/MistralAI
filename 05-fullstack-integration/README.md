# Fullstack Integration Index

This section connects ASP.NET Core, React/Angular, and GenAI into deployable application patterns.

Use it after you understand each individual track:

- ASP.NET Core APIs and auth
- React or Angular frontend architecture
- RAG fundamentals
- Streaming chat
- Security, observability, and deployment

---

## Recommended reading order

1. [Architecture](architecture.md) - core fullstack GenAI architecture and request lifecycle.
2. [Sample API contracts](sample-api-contracts.md) - endpoint shapes, DTOs, streaming events, and clients.
3. [End-to-end tutorial](end-to-end-tutorial.md) - build a Notes + RAG chat app step by step.
4. [Auth flow deep dive](auth-flow-deep-dive.md) - PKCE/JWT, tenant filtering, tool auth, and tests.
5. [Streaming deep dive](streaming-sse-websocket.md) - fetch streams, SSE, WebSocket, SignalR, cancellation, and proxy concerns.
6. [Deployment checklist](deployment-checklist.md) - production readiness gates for fullstack GenAI apps.
7. [Project ideas](project-ideas.md) - portfolio projects from beginner to advanced.

---

## What this section teaches

| Topic | You should be able to explain |
|---|---|
| Trust boundary | Why browser clients call your backend, not the LLM provider |
| Auth | How OIDC/JWT claims become tenant and ACL filters |
| API design | How chat, RAG, upload, ingestion, streaming, and feedback contracts fit together |
| RAG integration | How source docs become chunks/vectors and how query-time retrieval works |
| Streaming | How tokens move from provider to API to SPA with cancellation |
| Frontend UX | Loading, partial output, citations, feedback, and error states |
| Deployment | Secrets, queues, vector stores, proxies, observability, cost controls |
| Evaluation | Retrieval metrics, generation quality, citation accuracy, feedback loops |

---

## Capstone target

Build the Notes + RAG chat app from [end-to-end-tutorial.md](end-to-end-tutorial.md).

Minimum version:

- [ ] Notes CRUD.
- [ ] Document or note ingestion.
- [ ] Chunking and embeddings.
- [ ] Tenant-safe vector search.
- [ ] RAG answer endpoint with citations.
- [ ] Streaming chat UI.
- [ ] Stop generation.
- [ ] Feedback capture.
- [ ] Deployment/readiness checklist.

Portfolio version:

- [ ] Real auth provider.
- [ ] pgvector/Qdrant/Azure AI Search.
- [ ] Hybrid search or reranking.
- [ ] RAG eval set.
- [ ] Admin usage/quality dashboard.
- [ ] Prompt injection and cross-tenant tests.
- [ ] Cloud deployment diagram.

---

## Interview-ready one-minute summary

> A production fullstack GenAI app uses the backend as the trust boundary. The React or Angular SPA handles user interaction, streaming rendering, citations, feedback, and auth redirects. The ASP.NET Core API validates tokens, maps claims to tenant/user context, enforces authorization, builds prompts, performs retrieval, calls model providers, streams responses, logs usage, and executes tools safely. Ingestion runs asynchronously, chunks and embeds documents, and stores vectors with tenant and ACL metadata. Quality is measured with retrieval and generation evals, and deployment includes secrets, rate limits, proxy streaming config, cost controls, and observability.

---

## Practice prompts

Use these after reading the section:

1. Design the auth flow for a multi-tenant RAG app.
2. Implement a streaming endpoint in ASP.NET Core.
3. Build a React or Angular chat UI that supports stop generation.
4. Explain SSE vs WebSocket for AI chat.
5. Design document upload plus async ingestion.
6. Explain how citations are generated and validated.
7. Create a deployment checklist for the Notes + RAG app.
8. Diagnose why a RAG answer cites the wrong document.
9. Prevent cross-tenant data leakage in vector search.
10. Add evals and observability to a GenAI feature.

