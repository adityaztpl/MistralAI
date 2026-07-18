# Mistral APIs: Chat, Models, Tokens, Limits, and SDK Notes

This guide prepares you to explain and implement Mistral API integrations in interviews. It covers
the direct API, OpenAI-compatible usage, model selection, streaming, structured outputs, tokens,
rate-limit behavior, and SDK tradeoffs.

## 1. API surface overview

The everyday production paths are:

- **Chat completions** for normal assistant responses.
- **Streaming chat completions** for low-latency UX.
- **Embeddings** for semantic search and retrieval.
- **Function/tool calling** for model-directed structured actions.
- **Agents and conversations** for stateful, tool-enabled workflows.
- **Models endpoint** for available models and metadata.

The most common base URL is:

```text
https://api.mistral.ai/v1
```

The chat completion endpoint follows an OpenAI-style shape:

```text
POST /v1/chat/completions
Authorization: Bearer $MISTRAL_API_KEY
Content-Type: application/json
```

## 2. Minimal chat completion request

```json
{
  "model": "mistral-small-latest",
  "messages": [
    {
      "role": "system",
      "content": "You are a concise assistant for backend engineers."
    },
    {
      "role": "user",
      "content": "Explain idempotency keys in one paragraph."
    }
  ],
  "temperature": 0.2,
  "max_tokens": 400
}
```

Response shape is broadly:

```json
{
  "id": "cmpl_...",
  "object": "chat.completion",
  "created": 1730000000,
  "model": "mistral-small-latest",
  "choices": [
    {
      "index": 0,
      "message": {
        "role": "assistant",
        "content": "..."
      },
      "finish_reason": "stop"
    }
  ],
  "usage": {
    "prompt_tokens": 123,
    "completion_tokens": 80,
    "total_tokens": 203
  }
}
```

## 3. Message roles

| Role | Purpose | Interview note |
| --- | --- | --- |
| `system` | Global behavior and safety instructions | Keep policy and app rules here |
| `user` | User request | Treat as untrusted input |
| `assistant` | Prior model response | Store only what you need |
| `tool` | Tool result returned by your code | Validate and bound output size |

Good system prompts are short, explicit, and testable:

```text
You are a support assistant. Use only provided context. If context is insufficient,
say what is missing. Return JSON matching the requested schema.
```

## 4. Important chat parameters

| Parameter | What it does | Practical default |
| --- | --- | --- |
| `model` | Selects model | Use env/config, not hard-coded everywhere |
| `messages` | Conversation input | Trim history before context overflow |
| `temperature` | Randomness | `0.0-0.3` for factual workflows |
| `top_p` | Nucleus sampling | Usually leave at default if setting temperature |
| `max_tokens` | Completion cap | Set to protect cost and latency |
| `stream` | SSE partial output | `true` for chat UX |
| `tools` | Function schemas | Use precise JSON schema |
| `tool_choice` | Force/disable/auto tool use | `auto` for most apps |
| `parallel_tool_calls` | Allow multiple tool calls | Disable when tools conflict |
| `response_format` | JSON or JSON schema mode | Use for machine-readable output |
| `random_seed` | Determinism aid | Useful in tests, not a guarantee |
| `safe_prompt` | Inject safety prompt | Consider for public-facing apps |
| `prompt_cache_key` | Provider prompt caching hint | Use for repeated long prefixes |

## 5. Streaming

Set:

```json
{ "stream": true }
```

Streaming responses arrive as server-sent event-style lines:

```text
data: {"choices":[{"delta":{"content":"Idempotency"}}]}
data: {"choices":[{"delta":{"content":" keys"}}]}
data: [DONE]
```

Why streaming matters:

- Reduces perceived latency.
- Lets users cancel long generations.
- Supports progressive rendering and typing indicators.
- Enables early moderation or partial UI updates in advanced systems.

Server responsibilities:

- Disable proxy buffering for streaming routes.
- Flush after each token/frame.
- Handle client disconnects and cancellation tokens.
- Avoid logging every token at high volume.

Browser responsibilities:

- Parse chunks incrementally.
- Support cancellation with `AbortController`.
- Render partial text safely.
- Distinguish model completion from network failure.

## 6. JSON output and structured responses

For extraction workflows, request JSON explicitly in the prompt and API payload.

Example:

```json
{
  "model": "mistral-small-latest",
  "messages": [
    {
      "role": "system",
      "content": "Return only valid JSON with keys: severity, summary, action_items."
    },
    {
      "role": "user",
      "content": "Parse this incident review..."
    }
  ],
  "response_format": {
    "type": "json_object"
  }
}
```

Interview nuance:

- JSON mode improves syntax validity.
- You still validate semantic correctness.
- You still need schema validation in application code.
- You still handle refusals and empty outputs.

## 7. OpenAI-compatible endpoint

Many libraries can point at Mistral by changing:

1. API key.
2. Base URL.
3. Model name.

Example using an OpenAI-compatible Python client:

```python
from openai import OpenAI
import os

client = OpenAI(
    api_key=os.environ["MISTRAL_API_KEY"],
    base_url="https://api.mistral.ai/v1",
)

response = client.chat.completions.create(
    model="mistral-large-latest",
    messages=[{"role": "user", "content": "Hello from an OpenAI-compatible client"}],
)
print(response.choices[0].message.content)
```

Use this compatibility path when:

- Your framework already expects OpenAI-style APIs.
- You want provider portability.
- You are migrating gradually.

Prefer provider-native SDKs when:

- You need first-class access to Mistral-specific APIs.
- You want typed convenience models for agents, files, or advanced features.
- Your team standardizes on official SDK support.

## 8. Model selection

Think in terms of task requirements:

| Task | Model characteristic |
| --- | --- |
| Simple classification | Small, low-latency, low-cost |
| Support answer with RAG | Medium reasoning and strong instruction following |
| Complex synthesis | Larger model, larger context, better reasoning |
| Tool-heavy workflow | Function-calling capable model |
| Code tasks | Code-specialized model when available |
| High-volume extraction | Cheapest model that passes evals |

Selection process:

1. Build an evaluation set.
2. Test candidate models blind.
3. Measure quality, latency, and cost.
4. Pick the smallest/cheapest model that passes.
5. Re-evaluate when prompts, data, or model versions change.

## 9. Token basics

Tokens are model-specific chunks of text. They drive:

- Context window usage.
- Latency.
- Cost.
- Truncation behavior.

Rules of thumb:

- English text often averages around four characters per token, but this is approximate.
- Code, JSON, tables, and non-English text can differ substantially.
- Count tokens using provider/model-compatible tokenizers when precision matters.

Token budget equation:

```text
system prompt
+ conversation history
+ retrieved context
+ tool schemas
+ user request
+ expected completion
<= model context window
```

Common strategies:

- Summarize old conversation turns.
- Keep retrieved chunks short.
- Use metadata filters before retrieval.
- Avoid dumping full documents into prompts.
- Cap completion length.
- Cache long static system/developer prompts.

## 10. Rate limits

Rate limits can apply by:

- Requests per minute.
- Tokens per minute.
- Concurrent requests.
- Model-specific quotas.
- Account/project tier.

Robust client behavior:

- Treat HTTP `429` as backpressure.
- Respect `Retry-After` when present.
- Use exponential backoff with jitter.
- Retry idempotent requests only.
- Queue background ingestion instead of blocking user traffic.
- Apply local rate limits before hitting provider limits.

Do not:

- Retry every failure immediately.
- Fan out unlimited embedding calls.
- Hide repeated provider failures from observability.
- Run production traffic with a single unbounded global key.

## 11. Error handling

Plan for:

- `400`: invalid payload, model name, schema, or context length.
- `401/403`: missing, invalid, or unauthorized key.
- `408/499`: timeout or client cancellation.
- `429`: rate limit.
- `500/502/503/504`: provider or network transient issue.

Application behavior:

- Return user-friendly messages.
- Preserve technical details in logs.
- Attach request ids.
- Make retries bounded.
- Use circuit breakers for sustained outages.
- Provide fallback paths for non-critical AI features.

## 12. SDK notes

Official SDKs give convenience and typed features. Raw HTTP gives portability and transparent behavior.

Use official SDK when:

- The team wants less boilerplate.
- You use Mistral-specific resources.
- You value typed models and examples.

Use raw HTTP when:

- You need a tiny dependency footprint.
- You are writing framework adapters.
- You need complete control over streaming parsing and retries.

Use OpenAI-compatible clients when:

- You already use LangChain, LlamaIndex, Semantic Kernel, or similar.
- You need provider switching.
- Your system abstracts providers behind a shared interface.

## 13. Production client checklist

- Load `MISTRAL_API_KEY` from a secret source.
- Configure model names by environment.
- Set connect/read timeouts.
- Add bounded retries for 429/5xx.
- Respect `Retry-After`.
- Capture latency, status, model, and token usage.
- Redact prompts if they may contain sensitive data.
- Validate structured outputs.
- Add load tests for streaming and cancellation.
- Put provider calls behind an internal interface.

## 14. Interview questions

1. How would you migrate an OpenAI-compatible app to Mistral?
2. Why should temperature be low for extraction?
3. How do you prevent prompt/context overflow?
4. When would you stream responses?
5. What should be retried and what should not?
6. How do you choose a model for a production workflow?
7. How do you track cost per tenant?
8. What does function calling actually execute?
9. How do you validate JSON mode output?
10. How do you handle provider outage gracefully?

## 15. Strong answers in one sentence

- **Migration**: change the base URL, key, model names, and run evals because compatibility does not guarantee identical behavior.
- **Streaming**: use it for perceived latency and cancellation, but design for SSE buffering and disconnects.
- **Rate limits**: combine local throttling, queues, exponential backoff, and visibility into token/request usage.
- **Model choice**: pick the smallest model that meets measured quality, latency, and reliability requirements.
- **Tokens**: budget prompt, history, retrieval context, tools, and completion together.

