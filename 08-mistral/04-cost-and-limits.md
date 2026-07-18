# Cost and Limits for Mistral Applications

Cost and limits questions are senior-engineering questions. They test whether you can turn a model
demo into an operational system with predictable spend, graceful degradation, and useful monitoring.

## 1. Main cost drivers

Cost is usually driven by:

- Input tokens.
- Output tokens.
- Embedding tokens.
- Model choice.
- Retries.
- Repeated prompts.
- Unbounded chat history.
- Over-retrieval in RAG.
- Tool loops in agents.
- Failed requests that still consumed work.

Simple equation:

```text
monthly_cost =
  chat_input_tokens * input_price
+ chat_output_tokens * output_price
+ embedding_tokens * embedding_price
+ fixed platform/storage/network costs
```

## 2. Latency drivers

Latency is usually driven by:

- Network round trips.
- Model size.
- Prompt length.
- Output length.
- Retrieval query time.
- Tool call fan-out.
- Rate-limit queueing.
- Streaming proxy buffering.

Optimize perceived latency separately from total latency. Streaming may not reduce total generation
time, but it can make the product feel much faster.

## 3. Budget levers

| Lever | Impact | Risk |
| --- | --- | --- |
| Smaller model | Lower cost and latency | Quality may drop |
| Shorter prompt | Lower cost and latency | Missing instructions/context |
| Fewer retrieved chunks | Lower cost | Lower recall |
| Lower max tokens | Lower output cost | Truncated answers |
| Cache static prompts | Lower input cost | Cache invalidation |
| Cache embeddings | Lower ingestion/query cost | Stale model versions |
| Batch embeddings | Lower overhead | Larger failure batch |
| Summarize history | Lower input tokens | Summary drift |

## 4. Rate-limit handling

Rate limits can be per minute, per model, per account, per organization, or based on tokens.

Client strategy:

1. Apply local queueing for bulk work.
2. Use per-model concurrency limits.
3. Respect provider `Retry-After` headers when present.
4. Use exponential backoff with jitter.
5. Retry only safe, idempotent requests.
6. Surface persistent throttling to operators.

Backoff sketch:

```text
base = 250ms
delay = min(max_delay, base * 2^attempt) + random(0, jitter)
```

## 5. Retry policy

Retry:

- `429` with backoff.
- `500/502/503/504` with bounded attempts.
- Network timeouts if the operation is safe.

Do not blindly retry:

- Invalid requests.
- Authentication failures.
- Tool side effects.
- Requests where the client timed out but the server may still complete a write.

For side effects, use idempotency keys and operation records.

## 6. Prompt cost control

Use:

- Short system prompts.
- Reusable prompt modules.
- Prompt caching keys for repeated long prefixes when available.
- Conversation summarization.
- History windows.
- Retrieval compression.
- Strict output formats.

Avoid:

- Re-sending entire documents.
- Re-sending every prior turn forever.
- Including unused tool schemas.
- Asking for verbose chain-of-thought.
- Returning huge tool outputs to the model.

## 7. RAG cost control

Ingestion cost:

- Chunk once.
- Batch embeddings.
- Skip duplicate chunks by content hash.
- Version embeddings by model and chunker.
- Queue background re-indexing.

Query cost:

- Embed query once.
- Retrieve with metadata filters.
- Rerank only top candidates.
- Deduplicate chunks.
- Compress context.
- Cache common query embeddings when privacy allows.

## 8. Agent cost control

Agent workflows can accidentally multiply cost.

Controls:

- Max tool-call iterations.
- Max tool calls per turn.
- Max wall-clock duration.
- Tool allowlist by user scope.
- Stop conditions.
- Cost budget per task.
- Human approval for expensive operations.

Example:

```text
max_iterations = 6
max_parallel_tools = 3
max_retrieved_chunks = 8
max_output_tokens = 800
task_budget_usd = 0.25
```

## 9. Multi-tenant cost attribution

Track usage by:

- Tenant id.
- User id.
- Feature.
- Model.
- Endpoint.
- Conversation id.
- Request id.

Store:

- Input token count.
- Output token count.
- Embedding token count.
- Provider latency.
- Provider status.
- Estimated cost.

Use attribution for:

- Billing.
- Abuse detection.
- Feature ROI.
- Model migration decisions.
- Customer support.

## 10. Forecasting

Start with usage assumptions:

```text
daily_active_users = 10,000
chat_requests_per_user = 4
avg_input_tokens = 2,000
avg_output_tokens = 300
days_per_month = 30
```

Then calculate:

```text
monthly_requests = dau * requests_per_user * days
monthly_input_tokens = monthly_requests * avg_input_tokens
monthly_output_tokens = monthly_requests * avg_output_tokens
```

Add:

- Retry overhead.
- Evaluation traffic.
- Internal/admin traffic.
- Embedding ingestion.
- Traffic growth.
- Peak concurrency.

## 11. Limit-aware UX

Good UX under pressure:

- Shows progress during queueing.
- Streams output.
- Allows cancellation.
- Explains temporary capacity issues.
- Offers retry later.
- Falls back to search results when answer generation fails.

Bad UX:

- Spins forever.
- Retries silently for minutes.
- Loses user input.
- Returns raw provider errors.

## 12. Observability

Dashboards:

- Requests by model.
- Tokens by model.
- Cost by tenant.
- 429 rate.
- Retry attempts.
- Timeouts.
- Streaming disconnects.
- Average context size.
- Average tool iterations.

Alerts:

- Cost spike.
- 429 spike.
- 5xx spike.
- Latency p95 regression.
- Eval score regression.
- No-answer rate spike.

## 13. Governance

For enterprise environments:

- Separate dev/stage/prod keys.
- Limit who can create production keys.
- Rotate keys.
- Enforce budget alerts.
- Review prompts that include sensitive data.
- Define retention for prompts and outputs.
- Document data processing responsibilities.

## 14. Interview questions

1. What causes LLM cost to spike?
2. How do you handle 429s?
3. How do you estimate cost before launch?
4. How do you attribute cost by tenant?
5. How do agents multiply cost?
6. How does streaming affect latency?
7. How do you keep RAG prompts small?
8. What should your fallback be during provider outage?

## 15. Strong answers

- **429s**: local throttling, respect retry headers, exponential backoff with jitter, and queue bulk work.
- **Cost**: measure tokens by feature and tenant, cap output, cache repeated inputs, and choose the smallest passing model.
- **Agents**: cap iterations and tool calls because every loop can add model and tool cost.
- **Forecasting**: estimate request volume, token volume, retries, evals, and growth; then validate with load tests.

