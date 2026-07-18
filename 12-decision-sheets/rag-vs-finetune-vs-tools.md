# RAG vs Fine-tuning vs Tools

## The decision

When an LLM lacks information or needs to act, you usually choose among:

- **RAG**: retrieve external knowledge and place it in context.
- **Fine-tuning**: update model behavior/style/task performance through training examples.
- **Tools/function calling**: let the model request deterministic actions or live data through APIs.

These are complementary, not mutually exclusive.

## Quick default

- Use **RAG** for private, changing, source-grounded knowledge.
- Use **fine-tuning** for repeated behavior, style, format, classification, domain language, or task performance when prompting is not enough.
- Use **tools** for actions, transactions, calculations, live data, permissions, and anything requiring deterministic system interaction.

## Comparison

| Need | RAG | Fine-tune | Tools |
|---|---|---|---|
| Private documents | Excellent | Poor alone | Useful for document APIs |
| Fresh data | Excellent if indexed/live | Poor unless retrained | Excellent for live APIs |
| Citations | Natural | Not natural | Possible if tool returns sources |
| Behavior/style | Medium | Excellent | Not the purpose |
| Deterministic action | Poor | Poor | Excellent |
| Cost per call | Retrieval + tokens | Usually lower prompt tokens | Tool + model overhead |
| Operational burden | Ingestion/eval/indexing | Dataset/training/eval | Tool auth/audit/safety |

## Use RAG when

- The answer depends on knowledge outside the model weights.
- The knowledge changes frequently.
- Users need citations or auditability.
- Data is tenant-specific or permissioned.
- You can retrieve a small relevant context.
- You need deletion/update behavior without retraining a model.

RAG failure modes:

- Bad chunking.
- Missing metadata.
- Weak hybrid search.
- No reranking.
- Context stuffing.
- No citation validation.
- Ignoring ACLs.
- No retrieval evaluation.

## Use fine-tuning when

- The model needs consistent tone, format, or domain-specific style.
- You have many high-quality input/output examples.
- The task is repeated and prompt-only performance has plateaued.
- You want shorter prompts for a stable behavior.
- The target is classification, extraction, routing, rewriting, or structured output.

Fine-tuning is usually not the right answer for rapidly changing factual knowledge. It does not guarantee memorization, freshness, or citations.

## Use tools when

- The model must fetch live data.
- The model must perform an action: create ticket, refund order, schedule meeting, update CRM.
- The answer depends on authorization or transactional state.
- The task requires deterministic computation.
- You need audit logs and idempotency.

Tool failure modes:

- Trusting model-supplied arguments without validation.
- Running tools client-side or without user authorization.
- No approval flow for risky actions.
- No idempotency keys.
- No timeouts or retries.
- Returning too much tool output into context.

## Common combined architecture

```text
User question
  -> classify intent
  -> retrieve policy/docs if knowledge needed
  -> call tools if live state/action needed
  -> generate answer with citations/tool results
  -> validate output and log trace/evaluation signals
```

Examples:

- Support answer: RAG over help docs + tool call for order status.
- Compliance assistant: RAG over policy docs + no action tools without approval.
- Coding assistant: RAG over repo docs + tools for issue tracker/build logs.
- Sales copilot: fine-tuned tone + RAG over product docs + CRM tools.

## Interview answer script

```text
I would use RAG when the model needs private or changing knowledge and users need source-grounded answers. I would use tools when the model needs live data or has to perform an action, because tools keep deterministic work in normal application code with auth and audit logs. I would consider fine-tuning only when I have enough examples and the problem is behavior, style, extraction, or repeated task performance, not fresh factual knowledge.

In production these often combine: RAG for knowledge, tools for actions, and maybe fine-tuning for consistent behavior. I would evaluate retrieval quality, tool safety, cost, latency, and hallucination rate before scaling.
```

## Strong closing line

"RAG gives the model context, tools give it capabilities, and fine-tuning changes its behavior; confusing those three leads to expensive and unsafe systems."
