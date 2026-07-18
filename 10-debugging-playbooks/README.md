# Debugging Playbooks

This section contains practical interview-ready debugging playbooks. Each playbook follows the same structure:

```text
Symptoms -> Reproduce -> Diagnose -> Fix -> Prevention -> Interview phrasing
```

The goal is to practice moving from vague production symptoms to evidence-backed fixes. In interviews, this is often more valuable than memorizing a single framework API.

## Playbooks

| Playbook | Area | What it trains |
|---|---|---|
| [RAG returns wrong documents](rag-wrong-docs.md) | GenAI/RAG | Retrieval debugging, chunking, metadata, evals |
| [JWT 401 loop](jwt-401-loop.md) | Auth/API/SPA | Token validation, refresh flows, interceptor behavior |
| [Angular change detection thrash](change-detection-thrash.md) | Angular | Signals, zones, RxJS subscriptions, rendering cost |
| [SSE stalls](sse-stalls.md) | Streaming | Proxy buffering, cancellation, heartbeats, client parsing |
| [EF Core N+1](ef-n-plus-one.md) | .NET/data | Query shape, includes, projections, split queries |
| [Token/context blowup](token-context-blowup.md) | GenAI | Prompt budgets, history compaction, retrieval trimming |
| [CORS preflight failures](cors-preflight-failures.md) | Web/API | Browser CORS model, middleware order, headers |
| [React infinite rerender](react-infinite-rerender.md) | React | Effects, dependency arrays, state identity, memoization |
| [Vector dimension mismatch](vector-dim-mismatch.md) | RAG/vector DB | Embedding models, index schema, migrations |

## Debugging answer framework

When an interviewer asks "How would you debug this?" avoid jumping straight to a guessed fix. Use:

1. Clarify scope: who is affected, when it started, what changed.
2. Reproduce: smallest local or staging reproduction.
3. Observe: logs, traces, metrics, browser/network/devtools, database query logs.
4. Isolate: compare known-good vs bad path.
5. Fix: smallest safe change.
6. Verify: tests, regression checks, monitoring.
7. Prevent: guardrails, alerts, runbooks, code reviews.

## Evidence to mention often

- Request/response IDs across frontend, API, and worker logs.
- Browser Network tab for status codes, headers, preflights, and streaming chunks.
- SQL query logs and execution plans.
- Token counts, retrieved chunks, similarity scores, and prompt payloads.
- Version numbers for models, embedding dimensions, packages, and database extensions.
- Deployment diffs and config changes.

## Interview phrasing template

```text
I would first make the symptom measurable, then create a minimal reproduction. I would inspect <specific evidence>, because <reason>. Once isolated, I would apply the smallest fix, add a regression test or alert, and document the prevention step.
```
