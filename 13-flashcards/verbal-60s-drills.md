# 60-Second Verbal Drills

Practice these out loud. Timebox to 60 seconds, then compare against the model answer. A strong answer includes a definition, a concrete architecture implication, a risk, and a metric or validation step.

## 1. Explain RAG to a product manager.

**Model 60-second answer:** RAG lets an AI answer using our current company knowledge instead of relying only on what the model learned during training. We index trusted documents, retrieve the most relevant passages for a question, and ask the model to answer using that evidence with citations. The value is freshness, access control, and auditability. The risks are bad retrieval, stale documents, and unsupported answers, so we measure retrieval quality and citation accuracy.

## 2. Explain JWT auth in an ASP.NET SPA architecture.

**Model 60-second answer:** The user signs in through a trusted identity flow, and the API validates tokens on every request. Authentication proves who the user is; authorization decides what they can access. The SPA can store session state for UX, but the server enforces policies. I would validate issuer, audience, expiration, signature, tenant membership, and scopes, and I would avoid putting provider or LLM secrets in the frontend.

## 3. Angular signals vs RxJS.

**Model 60-second answer:** Signals are best for synchronous UI state and derived values that templates read. RxJS is best for asynchronous streams such as HTTP, websockets, debounced search, retries, and cancellation. I would not replace all RxJS with signals; I would use signals for local state and RxJS where time and concurrency are central, bridging intentionally at service/component boundaries.

## 4. React Query vs Zustand.

**Model 60-second answer:** React Query manages server state: fetching, caching, retries, stale data, mutations, and invalidation. Zustand manages lightweight client state like selected workspace or UI preferences. I avoid copying server data into Zustand because it creates two sources of truth. The decision starts by asking who owns the data: backend or frontend.

## 5. Controllers vs Minimal APIs.

**Model 60-second answer:** Controllers give a familiar MVC class/action structure with mature conventions and filters. Minimal APIs reduce ceremony and work well for focused vertical slices. I would use either in production as long as validation, auth, cancellation, logging, errors, and tests are handled. The syntax matters less than keeping business logic out of the HTTP boundary.

## 6. REST vs gRPC vs SignalR.

**Model 60-second answer:** REST is my default for browser-facing and public APIs because it is compatible and easy to debug. gRPC is strong for internal service-to-service calls with typed contracts and low latency. SignalR is for real-time bidirectional UX like notifications or chat streaming. Many systems use all three for different paths.

## 7. Prompt injection.

**Model 60-second answer:** Prompt injection is when untrusted user or retrieved content tries to override instructions or trigger unsafe behavior. I mitigate it by separating instructions from data, treating retrieved documents as untrusted, validating tool calls in code, enforcing permissions outside the model, not exposing secrets, and evaluating attack cases.

## 8. Streaming chat UX.

**Model 60-second answer:** Streaming improves perceived latency by showing tokens as they arrive. The backend owns the LLM call and streams through SSE, chunked HTTP, WebSockets, or SignalR. The UI needs loading state, partial text rendering, cancellation, retry/error handling, and a final persisted message. The system also needs token budgets and observability.

## 9. Hybrid search.

**Model 60-second answer:** Hybrid search combines keyword retrieval, vector similarity, metadata filtering, and often reranking. It works well for RAG because semantic search improves recall while keywords handle product names, IDs, and exact terms. I would enforce ACL filters before retrieval and measure recall, precision, citation correctness, and latency.

## 10. Human-in-the-loop agents.

**Model 60-second answer:** For risky actions, the agent should propose a plan and arguments, then pause for human approval before execution. The server validates authorization and tool arguments, records an audit log, and resumes the workflow after approval. This keeps the model helpful without letting it perform irreversible actions unsupervised.

## 11. EF Core N+1 problem.

**Model 60-second answer:** N+1 means loading a list and then separately loading related data for each item. It creates many database round trips and poor latency. I fix it with projections, includes, joins, split queries, or batching, and I verify with query logs and performance tests.

## 12. Frontend route guards.

**Model 60-second answer:** Route guards protect user experience by preventing navigation to screens the user should not see, but they are not security boundaries. The backend must enforce authorization on every API call. A good design combines guards, interceptors, auth state, and server policies.

## 13. Choosing Angular or React.

**Model 60-second answer:** Angular is a strong default for large teams wanting a complete framework with DI, routing, forms, and conventions. React is strong when flexibility, incremental adoption, ecosystem breadth, or a React meta-framework matters. I would choose based on team skills, existing code, product complexity, and maintenance risk.

## 14. LLM observability.

**Model 60-second answer:** LLM observability captures traces for retrieval, prompts, model calls, tool calls, streaming, cost, latency, and outputs. It helps debug hallucinations, latency spikes, and cost regressions. I would redact sensitive data and connect traces to evals and user feedback.

## 15. Fine-tuning vs RAG.

**Model 60-second answer:** RAG is for fresh/private/source-grounded knowledge. Fine-tuning is for behavior, style, extraction, or repeated task performance when prompts are insufficient. Fine-tuning does not automatically provide citations or current facts, so I would not use it as the main solution for changing company documents.

## 16. Rate limits and cost control.

**Model 60-second answer:** I would limit requests by user, tenant, route, and model budget. For LLM apps I also track tokens, retrieval size, retries, and tool calls. The API should return clear 429s, expose quotas when useful, and alert on unusual spend or abuse patterns.

## 17. Whiteboard a support copilot.

**Model 60-second answer:** Start with requirements: grounded answers, ticket context, safe actions, escalation. The flow is SPA to API, retrieve docs and ticket history, call model, optionally propose tool actions, require approval for risky changes, stream answer, log feedback. Risks are hallucination, ACL leaks, bad actions, and cost; metrics include resolution rate, escalation rate, faithfulness, latency, and CSAT.

## 18. Explain Semantic Kernel.

**Model 60-second answer:** Semantic Kernel is a .NET-friendly SDK for integrating LLMs with application functions/plugins. It lets C# teams expose server-side capabilities to AI workflows while using DI, typed services, and existing observability. It does not remove the need for retrieval quality, tool validation, auth, and evals.

## 19. LangChain vs LangGraph.

**Model 60-second answer:** LangChain is useful for composing prompts, retrievers, tools, and chains. LangGraph is better when the workflow has explicit state, loops, retries, checkpoints, and human approval. I use LangChain for straightforward pipelines and LangGraph when control flow becomes the product risk.

## 20. Mobile offline sync.

**Model 60-second answer:** The client stores local changes with IDs, versions, timestamps, and sync status. When online, it pushes idempotent operations and pulls server changes. Conflicts are resolved with rules like last-write-wins for simple fields or merge/manual review for important content. Metrics include sync success, conflict rate, and data loss incidents.

## 21. API error design.

**Model 60-second answer:** Errors should have consistent status codes, machine-readable codes, user-safe messages, validation details, and correlation IDs. The API logs internal details but returns safe responses. This improves frontend handling, observability, and support.

## 22. Testing a RAG system.

**Model 60-second answer:** I would create an eval set with questions, expected sources, and answer rubrics. Then I measure retrieval recall, context precision, citation correctness, faithfulness, latency, and cost. I would run evals on chunking, embedding, prompt, and model changes before rollout.

## 23. Securing tools in agents.

**Model 60-second answer:** Tools run on the server with explicit allowlists, schemas, auth checks, tenant filters, idempotency, and audit logs. The model can propose a tool call, but normal application code decides whether it is valid and safe to execute.

## 24. Explaining useEffect simply.

**Model 60-second answer:** useEffect synchronizes a React component with something outside React, like a subscription, timer, network request, or DOM API. It is not for calculating values that can be derived during render. Many bugs come from using effects for state derivation instead of keeping render pure.

## 25. System design metrics.

**Model 60-second answer:** I always include product, reliability, cost, and safety metrics. For example: p95 latency, error rate, cost per request, retrieval recall, hallucination/faithfulness rate, user satisfaction, escalation rate, and incident counts. Metrics prove whether the design works beyond a diagram.
