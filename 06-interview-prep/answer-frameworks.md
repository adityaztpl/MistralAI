# Answer Frameworks: Behavioral, Technical, Coding, and System Design

Use frameworks to make answers clear under pressure. Do not sound robotic; use the structure to organize your thinking.

---

## 1. The universal answer rule

Start with the direct answer, then add depth.

```text
Short answer -> Why -> Example -> Trade-off -> How I would validate
```

Bad pattern:

```text
Long background -> tangent -> partial answer
```

Good pattern:

```text
I would use fetch-based SSE streaming here because the app needs authenticated one-way token streaming. The main trade-off is manual parsing, but it keeps normal HTTP auth and AbortController cancellation.
```

---

## 2. Behavioral framework: STAR+

Classic STAR:

```text
Situation -> Task -> Action -> Result
```

STAR+ for senior engineers:

```text
Situation -> Goal -> Constraints -> Options -> Action -> Result -> Reflection
```

### Template

```text
The situation was...
The goal was...
The constraints were...
I considered...
I decided to...
The result was...
What I learned was...
```

### Example

```text
The situation was that our support team needed faster answers from scattered internal docs. The goal was to reduce lookup time without exposing private content or giving unsupported answers. The constraints were a short pilot timeline and strict tenant boundaries. I considered a frontend-only prototype, but chose a backend-owned RAG pipeline because auth, prompt construction, provider keys, and retrieval filtering needed to be server-side. I implemented tenant-filtered retrieval, streaming responses, citations, and feedback capture. The pilot reduced lookup time and exposed exact-code retrieval failures, which we addressed with hybrid search. I learned that RAG evals should be part of the launch gate, not a follow-up.
```

---

## 3. Behavioral framework: conflict

Use:

```text
Shared goal -> Disagreement -> Evidence -> Decision -> Relationship/result
```

### Template

```text
We agreed on the goal: ...
The disagreement was about ...
I tried to understand their concern: ...
We compared options using ...
We decided ...
The result was ...
```

### Strong signals

- You listened.
- You used evidence.
- You changed your mind when appropriate.
- You preserved trust.

---

## 4. Behavioral framework: failure

Use:

```text
Decision -> Impact -> Ownership -> Fix -> Prevention -> Learning
```

### Template

```text
I made/owned a decision to...
The impact was...
I noticed it through...
I fixed it by...
I prevented recurrence by...
I learned...
```

### GenAI example angle

```text
We launched dense-vector retrieval and missed exact error codes. I owned the retrieval evaluation gap, added exact-code questions to the eval set, implemented hybrid search, and created a release gate for retrieval changes.
```

---

## 5. Technical explanation framework

Use:

```text
Definition -> Mental model -> Example -> Trade-off -> Failure mode
```

### Example: dependency injection lifetimes

```text
Dependency injection lifetimes control how long service instances live. Singleton is one per app, scoped is one per request scope, and transient is new per resolution. A DbContext is usually scoped because it represents a unit of work for a request. A common failure is injecting a scoped service into a singleton, which can capture request state incorrectly.
```

### Example: RAG

```text
RAG retrieves relevant external context and gives it to a model so answers can be grounded in private or current knowledge. The mental model is two pipelines: ingestion creates chunks and embeddings, while query-time retrieval finds authorized chunks, builds a prompt, generates an answer, and validates citations. The main trade-off is retrieval quality versus latency and cost. A common failure mode is retrieving irrelevant chunks and then blaming the model for hallucination.
```

---

## 6. API design framework

Use:

```text
Resource/use case -> Contract -> Validation -> Auth -> Errors -> Observability
```

### Template

```text
I would model this as...
The endpoint would be...
The request/response shape would be...
Validation rules are...
Authorization is...
Errors use...
I would log/measure...
```

### Example: document upload

```text
I would model upload as an asynchronous document ingestion workflow. The endpoint is POST /api/documents with multipart form data. The response is 202 Accepted with documentId, status, and ingestionJobId. Validation checks file size and type. Authorization comes from the current user and tenant, not request fields. Errors use Problem Details. I would log document ID, tenant ID, job ID, parser warnings, chunks indexed, and ingestion duration.
```

---

## 7. Frontend answer framework

Use:

```text
State -> Effects/API -> UX states -> Errors -> Performance -> Accessibility/security
```

### Template

```text
I would store state as...
The API effect would...
The UI states are...
Errors are handled by...
For performance...
For accessibility/security...
```

### Example: streaming chat UI

```text
I would store messages with role, content, status, citations, and error fields. Sending a message appends the user message and an empty assistant message, then starts a fetch stream with AbortController. Delta events append to the active assistant message; citation events update the source panel; error events preserve partial output and mark the answer incomplete. For performance, I would batch token updates and avoid re-rendering all markdown every token. For security, I would sanitize model-rendered markdown and treat citations as structured backend data.
```

---

## 8. Coding interview framework

Use:

```text
Clarify -> Types -> Simple solution -> Edge cases -> Tests -> Optimize/harden
```

### During coding

Say:

- "I will define the data shape first."
- "I will implement the simple correct version."
- "Then I will handle edge cases."
- "Here are the tests I would add."

### After coding

Explain:

- Complexity.
- Trade-offs.
- Failure cases.
- Production changes.

### Example edge-case checklist

- Empty input.
- Null/missing fields.
- Long input.
- Duplicate IDs.
- Unauthorized access.
- Cancellation.
- Network failure.
- Partial stream.

---

## 9. System design framework for AI apps

Use:

```text
Requirements -> Scale -> APIs -> Architecture -> Data flow -> Deep dives -> Risks -> Metrics
```

### Whiteboard order

1. Clarify requirements.
2. Define users and core flows.
3. Estimate scale roughly.
4. Draw high-level architecture.
5. Deep dive into RAG/tools/streaming/auth.
6. Discuss data model.
7. Discuss security and privacy.
8. Discuss evaluation.
9. Discuss observability and cost.
10. Summarize trade-offs.

### 45-minute allocation

| Time | Topic |
|---:|---|
| 0-5 | Requirements |
| 5-10 | APIs and users |
| 10-20 | Architecture |
| 20-30 | Deep dive |
| 30-38 | Security/scale/cost |
| 38-43 | Evaluation/observability |
| 43-45 | Summary |

---

## 10. RAG answer framework

Use:

```text
Ingestion -> Retrieval -> Prompting -> Generation -> Validation -> Feedback/eval
```

### Template

```text
Ingestion parses documents, chunks them, embeds chunks, and stores vectors with metadata.
At query time, the system embeds or rewrites the question, retrieves authorized chunks, optionally does hybrid search/reranking, builds a grounded prompt, calls the model, returns structured citations, validates the answer, and records feedback/eval metadata.
```

### Must mention

- Tenant/ACL filters before prompt assembly.
- Chunking strategy matters.
- Hybrid search for exact terms.
- Reranking improves precision.
- Evaluation separates retrieval and generation.
- Citations should be structured and validated.

---

## 11. Streaming answer framework

Use:

```text
Transport choice -> Backend stream -> Client parser -> Cancellation -> Errors -> Observability
```

### Example

```text
For authenticated one-way chat streaming I would use fetch with an SSE-style response body. The ASP.NET endpoint validates auth, starts retrieval/model streaming, writes structured events, and flushes each chunk. The React or Angular client parses frames from ReadableStream and appends deltas to the active assistant message. Stop generation uses AbortController, which maps to the ASP.NET CancellationToken and provider cancellation. If an error happens after headers are sent, the server emits an error event and the UI keeps the partial answer. I would measure time to first token, stream duration, cancellation rate, and provider errors.
```

---

## 12. Security answer framework for GenAI

Use:

```text
Trust boundary -> Data authorization -> Prompt safety -> Tool safety -> Logging/privacy -> Tests
```

### Example

```text
The backend is the trust boundary. The browser never gets provider keys or controls tenant access. For RAG, data authorization is enforced in database/vector retrieval before prompt assembly. Prompt injection is mitigated by treating retrieved text as untrusted data, but the real controls are authorization, tool allowlists, argument validation, and human approval for risky actions. Logs should avoid sensitive prompt content unless policy allows it. I would add cross-tenant tests, prompt injection tests, and audit logs for retrieval/tool calls.
```

---

## 13. Evaluation answer framework

Use:

```text
Goal -> Dataset -> Retrieval metrics -> Generation metrics -> Online feedback -> Release gate
```

### Example

```text
First I define what quality means for the product, such as correct answers with valid citations. I create a golden dataset of representative questions and expected source chunks. Retrieval metrics include recall@k, MRR, and context precision. Generation metrics include faithfulness, answer relevance, and citation accuracy. Online, I track feedback, re-query rate, escalation rate, and human review samples. Prompt, model, chunking, or retriever changes must pass evals before rollout.
```

---

## 14. "I do not know" framework

When you do not know an answer:

```text
Admit -> Bound -> Reason -> Next step
```

Example:

```text
I have not implemented that exact provider feature before. My understanding is that the main concern would be streaming/cancellation semantics and response schema differences. I would start by isolating it behind a provider client interface, write contract tests around streaming events, and verify auth, timeout, and retry behavior before using it in the orchestrator.
```

This is much stronger than guessing.

---

## 15. Closing summary framework

At the end of system design or deep technical answers:

```text
To summarize, I would...
The key trade-off is...
The biggest risks are...
I would validate with...
```

Example:

```text
To summarize, I would deploy the SPA behind a CDN and use ASP.NET Core as the trust boundary for auth, retrieval, prompt construction, provider calls, and streaming. The key trade-off is retrieval quality versus latency and cost, especially around top-k, hybrid search, and reranking. The biggest risks are cross-tenant leakage, hallucinated citations, provider outages, and cost spikes. I would validate the system with security tests, RAG evals, streaming smoke tests, and production metrics for latency, quality, and spend.
```

---

## 16. Quick-reference cards

### Behavioral

```text
Situation -> Goal -> Constraints -> Action -> Result -> Learning
```

### Technical

```text
Definition -> Example -> Trade-off -> Failure mode
```

### Coding

```text
Clarify -> Types -> Simple solution -> Edge cases -> Tests
```

### System design

```text
Requirements -> Architecture -> Data flow -> Deep dives -> Risks -> Metrics
```

### RAG

```text
Ingest -> Chunk -> Embed -> Retrieve with ACL -> Rerank -> Prompt -> Generate -> Cite -> Evaluate
```

### Security

```text
Backend trust boundary -> Auth filters -> Tool validation -> Audit -> Tests
```

