# Whiteboard Pack

System design canvases for full-stack + GenAI interviews. Each canvas is designed for a 35-45 minute interview conversation.

## How to use a canvas

1. Spend 3 minutes clarifying requirements.
2. Draw the high-level architecture.
3. Walk one main sequence end-to-end.
4. Deep dive on data, auth, reliability, and failure modes.
5. Finish with metrics and trade-offs.

## Recommended answer structure

```text
Requirements -> Non-goals -> APIs -> Data model -> Architecture -> Sequence -> Deep dives -> Risks -> Metrics -> Rollout
```

## Canvases

1. [Auth and session design](01-auth-and-session-design.md)
2. [Multi-tenant RAG](02-multi-tenant-rag.md)
3. [Streaming chat](03-streaming-chat.md)
4. [Agent with human-in-the-loop](04-agent-with-hitl.md)
5. [Document ingestion pipeline](05-document-ingestion-pipeline.md)
6. [Realtime notifications](06-realtime-notifications.md)
7. [Rate limits and cost control](07-rate-limits-and-cost-control.md)
8. [Observability for LLM apps](08-observability-for-llm-apps.md)
9. [Support copilot end-to-end](09-support-copilot-e2e.md)
10. [Mobile offline sync notes](10-mobile-offline-sync-notes.md)

## Interview reminders

- Clarify tenant model and authorization early.
- Do not let the frontend call LLM providers directly.
- Retrieval quality, evaluation, cost, and observability are first-class.
- For realtime systems, distinguish transient delivery from durable state.
- For agents, deterministic code owns permissions and irreversible actions.
