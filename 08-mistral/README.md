# 08 - Mistral Prep Addon

This folder is an interview-ready field guide for building with Mistral APIs. It focuses on the
concepts engineers are commonly asked to explain: chat completions, streaming, embeddings, function
calling, retrieval-augmented generation, agents, model choice, cost control, limits, and production
readiness.

The examples use environment variables and contain no secrets.

```bash
export MISTRAL_API_KEY="replace-me"
```

## Contents

| File | Focus |
| --- | --- |
| [01-mistral-apis.md](./01-mistral-apis.md) | Chat, models, tokens, rate limits, OpenAI-compatible endpoint, SDK notes |
| [02-embeddings-function-calling.md](./02-embeddings-function-calling.md) | Embeddings, vector search, tool schemas, tool-call loops |
| [03-agents-and-rag-with-mistral.md](./03-agents-and-rag-with-mistral.md) | RAG architecture, agents, memory, orchestration, evaluation |
| [04-cost-and-limits.md](./04-cost-and-limits.md) | Cost drivers, rate-limit strategy, caching, retries, forecasting |
| [05-cheatsheet.md](./05-cheatsheet.md) | Fast interview reference and copy/paste snippets |

## Examples

| Example | Description |
| --- | --- |
| [examples/chat_completion.py](./examples/chat_completion.py) | Basic chat completion with retries and env vars |
| [examples/embeddings_rag.py](./examples/embeddings_rag.py) | Local in-memory RAG with Mistral embeddings |
| [examples/function_calling.py](./examples/function_calling.py) | Tool-call loop with JSON schema and local functions |
| [examples/streaming_chat.py](./examples/streaming_chat.py) | Token streaming from an OpenAI-compatible endpoint |
| [examples/dotnet_mistral_client.cs](./examples/dotnet_mistral_client.cs) | Minimal .NET client for chat and embeddings |

## Python setup

The examples use only the standard library plus `requests` so they are easy to inspect:

```bash
python -m venv .venv
source .venv/bin/activate
pip install requests
export MISTRAL_API_KEY="replace-me"
python 08-mistral/examples/chat_completion.py
```

If you prefer the official SDK, install the current SDK and adapt the payloads:

```bash
pip install "mistralai>=2"
```

## Core mental model

Mistral exposes several ways to build applications:

1. **Chat completions**: send messages and receive text, JSON, or tool calls.
2. **Streaming chat**: receive partial output as server-sent events.
3. **Embeddings**: convert text into vectors for search, clustering, dedupe, and RAG.
4. **Function calling / tool calling**: let the model choose structured calls, then your code executes them.
5. **Agents**: delegate orchestration, persistent conversations, built-in connectors, and MCP tool access to an agent layer.

For interviews, the most important distinction is this:

- The model can **suggest** a tool call.
- Your application or the agent runtime **executes** the tool.
- Authorization, validation, idempotency, and side-effect safety remain engineering responsibilities.

## What to memorize

- Base URL for direct API use: `https://api.mistral.ai/v1`.
- Chat endpoint shape: `POST /v1/chat/completions`.
- Streaming uses SSE-style `data:` frames and terminates with `[DONE]`.
- OpenAI-compatible clients can often be repointed by changing `base_url`, API key, and model name.
- Embeddings power retrieval but do not replace authorization or source-of-truth validation.
- Function calling is a loop: define tools, call model, execute requested tools, append tool results, call model again.
- RAG quality is usually limited by chunking, metadata, retrieval filters, prompt construction, and evaluation, not just model size.

## Interview preparation path

1. Read `01-mistral-apis.md` and run `chat_completion.py`.
2. Run `streaming_chat.py` and explain SSE framing.
3. Read `02-embeddings-function-calling.md` and run both RAG and function-calling examples.
4. Read `03-agents-and-rag-with-mistral.md` and compare hand-rolled orchestration with an agents platform.
5. Use `04-cost-and-limits.md` to answer scaling and budget questions.
6. Keep `05-cheatsheet.md` open for last-minute recall.

## Production checklist

- Centralize API key loading and do not log secrets.
- Set request timeouts and retry only safe failures.
- Capture provider request ids in logs.
- Track prompt, completion, and embedding token usage.
- Add rate limiting per user and tenant.
- Cache static prompt prefixes and embeddings.
- Add RAG evaluation sets and regression tests.
- Validate every tool-call argument before execution.
- Keep irreversible side effects behind explicit confirmation.
- Implement graceful degradation when model calls fail.

