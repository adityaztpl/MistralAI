"""
RAGAS-style evaluation without external dependencies.

Run:
    python ragas_style_eval.py

This is not a replacement for full RAGAS or human review. It demonstrates the
shape of an evaluation harness:
- golden examples
- retrieved contexts
- generated answers
- retrieval recall@k
- context precision
- citation validation
- simple lexical faithfulness and answer relevance checks
"""

from __future__ import annotations

import json
import re
from dataclasses import dataclass


@dataclass(frozen=True)
class RetrievedChunk:
    id: str
    text: str


@dataclass(frozen=True)
class EvalCase:
    id: str
    question: str
    expected_facts: list[str]
    relevant_chunk_ids: set[str]
    retrieved_chunks: list[RetrievedChunk]
    answer: str
    tags: list[str]


CASES = [
    EvalCase(
        id="rag-001",
        question="How often must API keys be rotated?",
        expected_facts=["90 days"],
        relevant_chunk_ids={"security-api-keys"},
        retrieved_chunks=[
            RetrievedChunk(
                id="security-api-keys",
                text="API keys must be rotated every 90 days. Verify new-key traffic before revoking old keys.",
            ),
            RetrievedChunk(
                id="streaming-policy",
                text="Streaming chat APIs must support cancellation and request IDs.",
            ),
        ],
        answer="API keys must be rotated every 90 days [security-api-keys].",
        tags=["happy-path", "security"],
    ),
    EvalCase(
        id="rag-002",
        question="What should streaming APIs support?",
        expected_facts=["cancellation", "request IDs"],
        relevant_chunk_ids={"streaming-policy"},
        retrieved_chunks=[
            RetrievedChunk(
                id="streaming-policy",
                text="Streaming chat APIs must support cancellation, request IDs, and partial-error handling.",
            ),
            RetrievedChunk(
                id="rag-eval",
                text="RAG systems should evaluate faithfulness and context precision.",
            ),
        ],
        answer="Streaming APIs should support cancellation and request IDs [streaming-policy].",
        tags=["happy-path", "production"],
    ),
    EvalCase(
        id="rag-003",
        question="Can support agents issue refunds without review?",
        expected_facts=["human approval"],
        relevant_chunk_ids={"tool-safety"},
        retrieved_chunks=[
            RetrievedChunk(
                id="tool-safety",
                text="Side-effecting tools such as refunds require explicit human approval before execution.",
            ),
            RetrievedChunk(
                id="billing-faq",
                text="Duplicate charges can be eligible for refund under the billing policy.",
            ),
        ],
        answer="Support agents can issue refunds immediately without review [billing-faq].",
        tags=["failure", "tool-safety"],
    ),
]


def normalize(text: str) -> set[str]:
    return {
        token
        for token in re.findall(r"[a-z0-9]+", text.lower())
        if token not in {"the", "a", "an", "and", "or", "to", "of", "in", "for", "with"}
    }


def citation_ids(answer: str) -> set[str]:
    return set(re.findall(r"\[([A-Za-z0-9_.:#-]+)\]", answer))


def retrieval_recall_at_k(case: EvalCase, k: int = 2) -> float:
    retrieved_ids = {chunk.id for chunk in case.retrieved_chunks[:k]}
    return 1.0 if case.relevant_chunk_ids & retrieved_ids else 0.0


def context_precision(case: EvalCase, k: int = 2) -> float:
    retrieved_ids = [chunk.id for chunk in case.retrieved_chunks[:k]]
    if not retrieved_ids:
        return 0.0
    relevant = sum(1 for chunk_id in retrieved_ids if chunk_id in case.relevant_chunk_ids)
    return relevant / len(retrieved_ids)


def citation_accuracy(case: EvalCase) -> float:
    cited = citation_ids(case.answer)
    if not cited:
        return 0.0
    retrieved = {chunk.id for chunk in case.retrieved_chunks}
    valid = cited <= retrieved
    relevant = cited <= case.relevant_chunk_ids
    return 1.0 if valid and relevant else 0.0


def answer_relevance(case: EvalCase) -> float:
    answer_terms = normalize(case.answer)
    question_terms = normalize(case.question)
    if not question_terms:
        return 0.0
    overlap = answer_terms & question_terms
    return len(overlap) / len(question_terms)


def lexical_faithfulness(case: EvalCase) -> float:
    """
    Cheap approximation: important answer terms should appear in retrieved context.
    Real systems often use stronger NLI/LLM judge/human grading.
    """
    context_terms = normalize(" ".join(chunk.text for chunk in case.retrieved_chunks))
    answer_terms = normalize(re.sub(r"\[[^\]]+\]", "", case.answer))
    if not answer_terms:
        return 0.0

    supported = answer_terms & context_terms
    unsupported = answer_terms - context_terms

    # Penalize strong unsupported terms, but do not expect perfect lexical match.
    raw = len(supported) / len(answer_terms)
    if {"immediately", "without", "review"} & unsupported:
        raw *= 0.4
    return round(raw, 3)


def expected_fact_coverage(case: EvalCase) -> float:
    answer = case.answer.lower()
    if not case.expected_facts:
        return 1.0
    covered = sum(1 for fact in case.expected_facts if fact.lower() in answer)
    return covered / len(case.expected_facts)


def score_case(case: EvalCase) -> dict[str, object]:
    scores = {
        "retrieval_recall_at_2": retrieval_recall_at_k(case, k=2),
        "context_precision_at_2": context_precision(case, k=2),
        "citation_accuracy": citation_accuracy(case),
        "answer_relevance": round(answer_relevance(case), 3),
        "faithfulness_approx": lexical_faithfulness(case),
        "expected_fact_coverage": expected_fact_coverage(case),
    }
    scores["pass"] = (
        scores["retrieval_recall_at_2"] == 1.0
        and scores["citation_accuracy"] == 1.0
        and scores["faithfulness_approx"] >= 0.6
        and scores["expected_fact_coverage"] >= 0.8
    )
    return {
        "case_id": case.id,
        "tags": case.tags,
        "scores": scores,
        "citations": sorted(citation_ids(case.answer)),
    }


def aggregate(results: list[dict[str, object]]) -> dict[str, float]:
    metric_names = [
        "retrieval_recall_at_2",
        "context_precision_at_2",
        "citation_accuracy",
        "answer_relevance",
        "faithfulness_approx",
        "expected_fact_coverage",
    ]
    summary = {}
    for metric in metric_names:
        summary[metric] = round(
            sum(result["scores"][metric] for result in results) / len(results), 3
        )
    summary["pass_rate"] = round(
        sum(1 for result in results if result["scores"]["pass"]) / len(results), 3
    )
    return summary


def main() -> None:
    results = [score_case(case) for case in CASES]

    print("--- Per-case results ---")
    print(json.dumps(results, indent=2))

    print("\n--- Aggregate scorecard ---")
    print(json.dumps(aggregate(results), indent=2))

    print(
        "\nInterview takeaway: evaluate retrieval and generation separately. "
        "Use deterministic checks for citations/schema, then add rubric or human "
        "judges for faithfulness and usefulness."
    )


if __name__ == "__main__":
    main()
