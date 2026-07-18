# Debugging Playbook: Token/Context Blowup

## Symptoms

- LLM calls fail with context length exceeded errors.
- Latency and cost increase as conversation continues.
- Answers become worse because the prompt contains too much irrelevant history.
- RAG app retrieves many chunks but generation truncates the useful ones.
- Streaming starts slowly due to huge prompt construction.
- Logs show very high input token counts for ordinary questions.

## Reproduce

1. Capture the full prompt payload for a failing request in a safe redacted environment.
2. Count tokens per section: system, developer rules, chat history, retrieved context, tool schemas, user message.
3. Replay the same conversation with fewer history turns.
4. Reduce top-k retrieval to see when the error disappears.
5. Test with the exact model context window configured in production.

Token budget table:

| Section | Budget example |
|---|---:|
| System/developer instructions | 800 |
| Tool schemas | 1,500 |
| Conversation summary | 700 |
| Recent turns | 1,500 |
| Retrieved context | 5,000 |
| User question | 500 |
| Output reserve | 1,500 |

## Diagnose

Check for:

- Full conversation history sent every turn without summarization.
- Retrieved chunks too large or too many.
- Duplicate chunks from same document.
- Tool schemas included when tools are not needed.
- Verbose hidden instructions repeated per request.
- Large JSON objects or database rows pasted into prompts.
- No output token reserve.
- Mismatch between tokenizer used for counting and actual model.

Prompt assembly bugs often hide in helper layers. Log a redacted prompt plan:

```json
{
  "model": "example-large-context",
  "contextWindow": 32000,
  "inputTokens": 30120,
  "reservedOutputTokens": 2048,
  "sections": {
    "system": 900,
    "history": 12000,
    "retrieval": 16000,
    "tools": 900,
    "user": 320
  }
}
```

## Fix

### Enforce a prompt budget

```text
max_input_tokens = model_context_window - reserved_output_tokens - safety_margin
```

Allocate per section, then trim intentionally.

### Compact history

- Keep the last N turns verbatim.
- Summarize older turns into a structured memory.
- Preserve user preferences, decisions, and unresolved tasks.
- Drop chit-chat and repeated assistant text.

### Trim retrieval context

- Retrieve more candidates than needed, rerank, then include fewer chunks.
- Use parent-child retrieval: rank small chunks, include focused parent sections.
- Deduplicate by source and semantic similarity.
- Include metadata and snippets, not entire documents.

### Gate tool schemas

Only include tools relevant to the current step. Tool definitions can consume large token budgets.

### Fail gracefully

If the request still exceeds budget:

- Return a clear error asking the user to narrow scope.
- Offer to summarize or start a new thread.
- Log budget breakdown for debugging.

## Prevention

- Add token counting to prompt builder tests.
- Set per-section budgets in code/config.
- Track input tokens, output tokens, and cost by endpoint.
- Alert on p95 input token spikes.
- Include a no-regression eval for long conversations.
- Version prompt templates and retrieval settings.
- Review any change that adds tool schemas or retrieved context.

## Interview phrasing

> I would break the prompt into sections and measure token counts instead of guessing. Then I would enforce a budget with output reserve, summarize older history, rerank and deduplicate retrieval context, and include only relevant tools. The prevention is token-budget tests and production metrics for input tokens, latency, and cost so context blowups are visible before they become outages.
