# GenAI Interview Questions

## 1. Foundations

### Q1: What is a token?

A token is the unit of text processed by an LLM. It can be a word, subword, punctuation mark, or whitespace pattern. Context limits, latency, and pricing are token-based.

**Strong follow-up:** Token count affects how much chat history and retrieved context can fit in a request. I budget tokens for system prompt, user prompt, retrieved chunks, and expected answer.

---

### Q2: What is an embedding?

An embedding is a numeric vector representing semantic meaning. Similar texts should have nearby vectors, which enables semantic search, clustering, deduplication, and RAG retrieval.

```text
"reset password" -> [0.12, -0.31, ...]
"change login secret" -> nearby vector
```

---

### Q3: Temperature vs top-p?

Temperature changes randomness by flattening or sharpening token probabilities. Top-p samples from the smallest token set whose cumulative probability reaches p.

Use low temperature for factual RAG, extraction, classification, and code. Use higher temperature for brainstorming.

---

### Q4: What is a context window?

The context window is the maximum number of tokens the model can consider in one call, including input and output. Larger windows help but do not replace retrieval, summarization, or memory management.

---

### Q5: What causes hallucinations?

- Missing or irrelevant context.
- Ambiguous prompt.
- Retrieval failures.
- Model prior knowledge overriding private facts.
- No citation/schema validation.
- High randomness.

**Mitigation:** RAG, low temperature, citations, schema validation, answer verification, and honest fallback.

---

## 2. Prompt engineering

### Q6: What makes a good prompt?

A good prompt has clear role/task, constraints, data boundaries, output format, examples when needed, and explicit behavior for missing evidence.

```text
Use only the provided context.
If context is insufficient, say "I do not know."
Return JSON matching this schema...
```

---

### Q7: What is prompt injection?

Prompt injection occurs when untrusted content tries to override instructions, such as a retrieved document saying "ignore previous instructions." Mitigate by treating documents as data, separating instructions from content, restricting tool permissions, and validating outputs.

---

### Q8: How do you get structured output?

Use provider-native structured output or function calling when available, otherwise use JSON/Pydantic-style schemas with validation and retry. Always validate in application code.

---

## 3. RAG

### Q9: Explain RAG end to end.

RAG has two pipelines:

```mermaid
flowchart LR
  D[Documents] --> C[Chunk]
  C --> E[Embed]
  E --> V[(Vector store)]
  Q[Question] --> QE[Embed query]
  QE --> V
  V --> R[Retrieve/rerank]
  R --> P[Prompt with context]
  P --> L[LLM]
  L --> A[Answer + citations]
```

Ingestion parses documents, chunks them, embeds chunks, and stores vectors with metadata. At query time, the system retrieves authorized relevant chunks, optionally reranks them, builds a grounded prompt, generates an answer, and validates citations.

---

### Q10: How do you choose chunk size?

It depends on content type. FAQs can be small, technical docs medium, policy/legal docs larger. Preserve headings and metadata. Use overlap when context spans boundaries. Evaluate retrieval quality rather than guessing.

---

### Q11: What is hybrid search?

Hybrid search combines dense vector retrieval with sparse keyword/BM25 retrieval. It improves results for exact terms like IDs, error codes, product names, and logs while preserving semantic matching.

---

### Q12: What is reranking?

Reranking takes a larger candidate set from retrieval and rescoring it for query relevance, often using a cross-encoder, LLM, or provider rerank model. It improves precision before sending context to the LLM.

---

### Q13: How do you evaluate RAG?

Retrieval:

- Recall@k
- Precision@k
- MRR/NDCG

Generation:

- Faithfulness
- Answer relevance
- Context precision/recall
- Citation accuracy

Use a golden question set and inspect failures by category.

---

### Q14: Why are citations hard?

The model may cite nonexistent or weak sources. Return structured citation metadata from retrieval and validate that cited IDs exist and support answer claims. Prefer citations tied to exact chunks.

---

## 4. Tools, agents, and workflows

### Q15: Tools vs RAG?

RAG retrieves knowledge from documents. Tools execute functions or query authoritative systems. Use tools for current data, calculations, side effects, and account-specific facts.

---

### Q16: Fine-tuning vs RAG?

Use RAG for private/current/source-citable knowledge. Use fine-tuning for behavior, style, format consistency, or repeated task patterns. They can be combined.

---

### Q17: What is an agent?

An agent is an LLM-driven loop that decides which steps/tools to use. Production agents need constraints, state, loop limits, tool validation, observability, and often human approval.

---

### Q18: What is LangGraph useful for?

LangGraph models LLM workflows as state graphs with nodes, edges, conditional routing, checkpoints, and human-in-the-loop. It is useful for agentic RAG, tool workflows, and multi-agent orchestration.

---

## 5. Production and safety

### Q19: How do you secure tool calling?

The application owns execution:

- Validate arguments.
- Enforce auth.
- Use least-privilege credentials.
- Add idempotency keys.
- Require approval for risky actions.
- Audit calls.

---

### Q20: What observability do GenAI apps need?

- Prompt/model versions.
- Token counts and cost.
- Latency per stage.
- Retrieved chunk IDs/scores.
- Tool calls.
- Citation validation.
- User feedback.
- Eval results over time.

---

### Q21: How do you reduce latency?

- Stream responses.
- Reduce prompt/context size.
- Cache embeddings and retrieval.
- Use smaller models for routing.
- Parallelize independent retrieval/tool calls.
- Rerank only when needed.
- Tune top-k.

---

### Q22: How do you handle provider outages?

- Timeouts and retries with backoff.
- Fallback model/provider where acceptable.
- Graceful user-facing error.
- Queue non-interactive tasks.
- Circuit breaker.
- Observability and alerting.

---

## 6. Scenario questions

### Scenario: Users say the RAG bot gives confident wrong answers.

Investigate:

1. Are relevant chunks retrieved?
2. Are chunks authorized and fresh?
3. Is context too noisy?
4. Does prompt require grounded answers?
5. Are citations validated?
6. Are evals tracking faithfulness?

Fixes:

- Improve chunking/metadata.
- Add hybrid search/reranking.
- Lower temperature.
- Add "insufficient context" behavior.
- Add faithfulness evaluation.

---

### Scenario: Retrieval fails for error code `AUTH-403-X9`.

Likely issue: semantic embeddings may not preserve exact codes. Add keyword/BM25 search, exact-match boosting, or metadata indexing.

---

### Scenario: A tool agent sends an email without approval.

Design flaw: risky side effects need human-in-the-loop approval. Tool execution should enforce policy, not trust the model.

