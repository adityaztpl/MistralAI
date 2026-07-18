# Mistral Cheatsheet

## Environment

```bash
export MISTRAL_API_KEY="replace-me"
export MISTRAL_BASE_URL="https://api.mistral.ai/v1"
export MISTRAL_CHAT_MODEL="mistral-small-latest"
export MISTRAL_EMBEDDING_MODEL="mistral-embed"
```

## Chat completion

```http
POST /v1/chat/completions
Authorization: Bearer $MISTRAL_API_KEY
Content-Type: application/json
```

```json
{
  "model": "mistral-small-latest",
  "messages": [
    { "role": "system", "content": "You are concise." },
    { "role": "user", "content": "Explain RAG." }
  ],
  "temperature": 0.2,
  "max_tokens": 500
}
```

## Streaming

```json
{
  "model": "mistral-small-latest",
  "messages": [{ "role": "user", "content": "Stream three tips." }],
  "stream": true
}
```

Read `data:` lines until:

```text
data: [DONE]
```

## Embeddings

```json
{
  "model": "mistral-embed",
  "input": ["Text to embed"]
}
```

Store:

- chunk text
- source id
- metadata
- ACL fields
- embedding vector
- embedding model
- chunk version

## Function calling loop

1. Define tools.
2. Send messages + tools.
3. Model returns `tool_calls`.
4. Validate arguments.
5. Execute tool.
6. Append `tool` result.
7. Call model again.

## Tool schema

```json
{
  "type": "function",
  "function": {
    "name": "get_ticket",
    "description": "Retrieve one support ticket visible to the current user.",
    "parameters": {
      "type": "object",
      "properties": {
        "ticket_id": { "type": "string" }
      },
      "required": ["ticket_id"],
      "additionalProperties": false
    }
  }
}
```

## RAG prompt

```text
You are grounded. Answer only from context.
If missing, say what is missing.
Cite sources as [1], [2].

Context:
[1] title=...
...

Question:
...
```

## Model selection

- Simple classification: small model.
- RAG support: small/medium model after evals.
- Complex synthesis: larger model.
- Code: code-specialized model when available.
- Tool workflows: function-calling capable model.

## Retry rules

Retry:

- `429`
- `500`
- `502`
- `503`
- `504`
- safe network timeouts

Do not retry:

- `400`
- `401`
- `403`
- non-idempotent writes without idempotency key

## Interview one-liners

- RAG grounds answers; agents orchestrate actions.
- Tool calls are model proposals, not automatic authorization.
- Vector search must be filtered by tenant and ACL before prompt construction.
- JSON mode improves syntax; application validation is still required.
- Cost is mostly tokens, model choice, retries, and unbounded context.
- Streaming improves perceived latency and needs cancellation handling.
- Start read-only, measure quality, then add controlled side effects.

