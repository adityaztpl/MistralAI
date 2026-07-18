# Rubric: 8-Hour RAG Chat

A strong RAG take-home demonstrates grounded answers, not just an LLM call. Reviewers look for end-to-end thinking across ingestion, retrieval, generation, citations, UX, and evaluation.

## Scorecard

| Area | Weight | Strong signal | Weak signal |
|---|---:|---|---|
| Ingestion | 15% | Repeatable ingest, useful chunking, metadata preserved | Hardcoded chunks, no metadata, manual setup only |
| Retrieval | 20% | Relevant chunks, filters, scores/debug info, no-answer handling | Top-k vector search with no evaluation or filtering |
| Generation/citations | 20% | Prompt constrains answer, citations validated, uncertainty handled | Hallucinated answers, citations missing or decorative |
| API/backend design | 10% | Clear contracts, cancellation/timeouts, provider keys server-side | Frontend calls provider directly, fragile endpoint shapes |
| UI/UX | 10% | Usable chat, source inspection, errors/loading states | Bare form with no sources or failure states |
| Tests/evals | 15% | API tests plus retrieval eval with expected sources | Manual demo only |
| README/trade-offs | 10% | Runnable setup, architecture, limitations, next steps | Setup unclear, no explanation of choices |

## Excellent

- Ingestion command creates a searchable index from sample docs.
- Chunking strategy is documented and appropriate for the data.
- Retrieval eval includes at least 5 questions and expected source ids.
- Chat response includes citations tied to retrieved chunks.
- Unsupported questions produce an honest no-answer response.
- UI shows answer, source snippets, and loading/error states.
- Provider keys never reach the browser.
- README includes commands, environment variables, demo flow, and trade-offs.

## Good

- Main RAG path works with sample documents.
- Citations are present but citation validation may be basic.
- Tests cover ingestion or chat but eval is small.
- UI is simple but clear.
- Known limitations are documented.

## Needs improvement

- Answer comes directly from LLM without retrieval.
- Documents are pasted into prompts manually.
- No citations or sources.
- No repeatable ingest process.
- No tests/evals.
- Setup requires guessing hidden steps.

## Red flags

- LLM API key embedded in frontend code.
- Model instructed to ignore source limitations.
- Retrieved content can override system instructions.
- No tenant/security filtering mentioned for private docs.
- Prompt grows unbounded with conversation history.
- App fabricates citation ids.

## Reviewer questions to prepare for

- How did you choose chunk size and overlap?
- How do you know retrieval is good?
- What happens when the answer is not in the docs?
- How would you enforce document permissions?
- How would you reduce latency and cost?
- Why did you choose this vector store?
- How would you debug wrong-document retrieval?

## Self-grade checklist

```text
[ ] Ingest command is documented and repeatable.
[ ] Source metadata survives chunking.
[ ] Chat endpoint returns answer and citations.
[ ] UI shows source snippets.
[ ] Unsupported question returns uncertainty.
[ ] Provider keys stay server-side.
[ ] At least one retrieval eval exists.
[ ] README includes limitations and next steps.
```
