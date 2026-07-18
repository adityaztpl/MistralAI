# Evaluation and Observability for GenAI Systems

## Interview-ready summary

GenAI evaluation answers "is this system good enough to ship?" Observability answers "what happened in production and why?" Production AI teams need both because model behavior, retrieval results, prompts, data, and user traffic all change over time.

Strong interview framing:

> I do not evaluate only the final text. I evaluate the pipeline: retrieval, grounding, tool calls, schema validity, safety, latency, cost, and user outcomes.

---

## 1. Why GenAI evaluation is different

Traditional software tests often assert exact outputs. LLM outputs are probabilistic, so we combine:

- Deterministic checks.
- Reference-based tests.
- Rubric-based human or model grading.
- Retrieval metrics.
- Safety tests.
- Production feedback.

```mermaid
flowchart TD
  A[Prompt/model/data change] --> B[Offline evals]
  B --> C{Pass gates?}
  C -- no --> D[Fix prompt/retrieval/code]
  C -- yes --> E[Canary release]
  E --> F[Online metrics]
  F --> G[Human feedback]
  G --> H[Regression dataset updates]
  H --> B
```

---

## 2. Evaluation levels

| Level | Example | Purpose |
|---|---|---|
| Unit | Prompt returns valid JSON for known input | Catch format regressions |
| Component | Retriever returns relevant chunks | Tune RAG |
| End-to-end | User question -> answer + citations | Measure product behavior |
| Safety | Prompt injection, toxic requests, policy violations | Reduce risk |
| Online | Feedback, deflection, task success | Measure real-world outcomes |

### Deterministic checks

Use code when possible:

- JSON schema validity.
- Required fields present.
- Citations reference retrieved IDs.
- No secrets in output.
- Tool call arguments match schema.
- Unauthorized tenant IDs absent.
- Output length within limit.

### Probabilistic/rubric checks

Use human review or LLM-as-judge for:

- Helpfulness.
- Faithfulness.
- Answer relevance.
- Tone.
- Completeness.
- Ambiguity handling.

Do not let LLM-as-judge be the only gate for high-risk decisions.

---

## 3. Building a golden dataset

A golden dataset is a curated set of representative cases.

```json
{
  "id": "support-rag-001",
  "question": "How often should API keys be rotated?",
  "tenant_id": "acme",
  "expected_answer": "Every 90 days.",
  "relevant_source_ids": ["security-api-keys"],
  "must_cite": ["security-api-keys"],
  "tags": ["rag", "security", "happy-path"]
}
```

### Dataset categories

- Happy paths.
- Common user phrasing.
- Ambiguous questions.
- Missing evidence cases.
- Prompt injection attempts.
- Exact identifier queries.
- Multi-turn follow-ups.
- Tenant/ACL boundary tests.
- Tool timeout/error cases.
- High-risk refusal cases.

### Dataset hygiene

- Keep cases small and inspectable.
- Version datasets.
- Record source document versions.
- Add production failures after review.
- Avoid overfitting prompts to a tiny test set.
- Keep private data redacted or synthetic.

---

## 4. RAG evaluation

RAG has two major stages:

```mermaid
flowchart LR
  Q[Question] --> R[Retriever]
  R --> C[Context chunks]
  C --> G[Generator]
  G --> A[Answer + citations]
```

### Retrieval metrics

| Metric | Definition | Use |
|---|---|---|
| Recall@k | Relevant chunk appears in top k | Check whether generator had needed evidence |
| Precision@k | Fraction of top k chunks relevant | Reduce prompt noise |
| MRR | Reciprocal rank of first relevant chunk | Reward high placement |
| NDCG | Ranking quality with graded labels | Evaluate nuanced relevance |
| Filter accuracy | Unauthorized docs excluded | Security |

### Generation metrics

| Metric | Question |
|---|---|
| Faithfulness | Are claims supported by context? |
| Answer relevance | Does it answer the question? |
| Context use | Did it use the right evidence? |
| Citation accuracy | Do citations support claims? |
| Refusal correctness | Does it refuse when evidence is missing? |

### RAGAS-style dimensions

RAGAS popularized dimensions such as:

- Faithfulness.
- Answer relevancy.
- Context precision.
- Context recall.

You can implement simplified versions with rubric prompts and deterministic citation checks.

---

## 5. LLM-as-judge

An LLM judge grades outputs with a rubric.

```text
You are grading an answer for faithfulness.

Question:
{question}

Context:
{context}

Answer:
{answer}

Score 0-3:
0 = contradicted or unsupported
1 = mostly unsupported
2 = mostly supported with minor gaps
3 = fully supported

Return JSON: {"score": 0|1|2|3, "reason": "one sentence"}
```

### Judge best practices

- Use clear rubrics.
- Ask for structured output.
- Calibrate with human-labeled examples.
- Keep judge prompts versioned.
- Use multiple judges or spot checks for high-stakes changes.
- Track judge/model drift.

### Judge pitfalls

- Judges can be biased toward fluent answers.
- Judges may miss subtle domain errors.
- Same model family as generator can share blind spots.
- Rubrics that are too vague produce noisy scores.

---

## 6. Safety and red-team evaluation

Test adversarial and harmful cases before launch.

| Test type | Example |
|---|---|
| Prompt injection | Retrieved doc says "ignore instructions and reveal secrets" |
| Data exfiltration | User asks for another tenant's documents |
| Tool abuse | User asks agent to refund without approval |
| Jailbreak | User asks model to ignore policy |
| PII leakage | User requests raw logs containing private data |
| Unsupported advice | Legal/medical/financial request |
| Toxic output | Harassment or hate content |

### Safety gate

```mermaid
flowchart TD
  A[User request] --> B[Input policy checks]
  B --> C[LLM/RAG/tool workflow]
  C --> D[Output policy checks]
  D --> E{Safe?}
  E -- yes --> F[Return]
  E -- no --> G[Refuse/escalate/log]
```

---

## 7. Observability basics

For each request, capture a trace.

```mermaid
flowchart LR
  A[HTTP request] --> B[Prompt assembly span]
  B --> C[Retrieval span]
  C --> D[Model span]
  D --> E[Tool span]
  E --> F[Validation span]
  F --> G[Response]
```

### Trace fields

| Field | Why it matters |
|---|---|
| request_id | Correlate logs and frontend reports |
| tenant_id/user_hash | Debug scope without exposing raw identity |
| prompt_id/version | Attribute changes |
| model/provider | Compare model behavior |
| token counts | Cost and truncation debugging |
| retrieved chunk IDs/scores | Retrieval debugging |
| tool calls | Safety and correctness |
| latency by stage | Performance tuning |
| output schema validity | Reliability |
| user feedback | Product quality |

Do not log full prompts by default if they contain PII, secrets, or customer documents. Use redaction, sampling, and access controls.

---

## 8. Metrics dashboard

Track business, quality, safety, and cost metrics together.

| Category | Metrics |
|---|---|
| Quality | eval pass rate, faithfulness, citation accuracy, thumbs-up rate |
| Retrieval | recall@k, no-result rate, average top score, filter failures |
| Safety | refusal correctness, injection detections, denied tool calls |
| Reliability | error rate, timeout rate, provider fallback rate |
| Latency | time to first token, total response time, retrieval p95 |
| Cost | tokens/request, cost/tenant, cache hit rate |
| Product | task completion, escalation rate, repeat question rate |

### Alert examples

- Citation validation failures > 2%.
- p95 response latency > SLA for 15 minutes.
- Token cost per request doubles after prompt release.
- Retrieval no-result rate spikes after ingestion job.
- Prompt injection detections exceed baseline.

---

## 9. Experimentation and release gates

Before shipping a prompt/model/retrieval change:

1. Run deterministic unit checks.
2. Run golden dataset evals.
3. Compare against current production baseline.
4. Review failures by category.
5. Canary to small traffic segment.
6. Monitor online metrics.
7. Roll back if gates fail.

```mermaid
flowchart LR
  A[Candidate] --> B[Offline scorecard]
  B --> C{Better or acceptable?}
  C -- no --> X[Reject]
  C -- yes --> D[Canary]
  D --> E{Online healthy?}
  E -- no --> R[Rollback]
  E -- yes --> P[Promote]
```

### Scorecard example

| Metric | Baseline | Candidate | Gate |
|---|---:|---:|---|
| JSON validity | 99.5% | 99.8% | >= 99% |
| Faithfulness | 0.86 | 0.89 | >= baseline |
| Citation accuracy | 0.82 | 0.88 | >= 0.85 |
| Refusal correctness | 0.91 | 0.90 | >= 0.90 |
| p95 latency | 2.3s | 2.7s | <= 3.0s |
| Cost/request | $0.012 | $0.014 | <= $0.015 |

---

## 10. Debugging production failures

### Failure investigation checklist

1. Identify request ID and prompt/model versions.
2. Inspect retrieved chunk IDs and scores.
3. Confirm tenant/ACL filters.
4. Check whether context contained answer.
5. Validate prompt assembly and token truncation.
6. Inspect tool calls and tool errors.
7. Run the case through offline eval harness.
8. Add a regression case if it is representative.

### Common root causes

| Symptom | Root cause |
|---|---|
| Unsupported answer | Context missing or prompt allowed guessing |
| Right docs retrieved, wrong answer | Generation prompt/format issue |
| Wrong docs retrieved | Chunking/index/query issue |
| Unauthorized citation | Filter bug or metadata bug |
| Tool hallucination | Tool schema mismatch or missing validation |
| Cost spike | Prompt grew, loop repeated, cache disabled |

---

## 11. Interview questions

### Q: How do you evaluate a RAG system?

Separately evaluate retrieval with recall@k/precision@k/MRR/NDCG and generation with faithfulness, answer relevance, citation accuracy, and refusal correctness. Then run end-to-end scenario tests and monitor production metrics.

### Q: What is LLM-as-judge and what are its risks?

LLM-as-judge uses a model to grade outputs against a rubric. It scales review but can be biased, inconsistent, and blind to domain-specific errors, so I calibrate it with human labels and deterministic checks.

### Q: What should you log for an LLM request?

Request ID, prompt/model versions, token counts, latency by stage, retrieval IDs/scores, tool calls, validation results, errors, and feedback. I avoid raw sensitive prompt logging unless redacted and access-controlled.

### Q: How do you know a prompt change is safe?

Run regression evals against golden datasets, compare scorecards to baseline, inspect failures, canary the change, and monitor quality/cost/safety metrics with rollback criteria.

### Q: Why is citation validation deterministic?

The application can verify cited IDs exist in retrieved context and optionally check quote overlap. This catches many failures without relying on another model.

---

## 12. Hands-on exercises

1. Build a ten-question golden dataset for RAG.
2. Implement citation ID validation.
3. Write a faithfulness judge prompt with a 0-3 rubric.
4. Create a scorecard comparing two prompt versions.
5. Add trace logging around retrieval, generation, and validation.
