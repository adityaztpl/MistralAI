"""
Hybrid retriever example: dense-ish semantic scores + BM25 keyword scores.

This is dependency-free for study. Production systems usually use a real
embedding model plus a lexical index such as Elasticsearch, OpenSearch,
Postgres full-text search, or a managed search service.

Run:
    python hybrid_retriever.py
"""

from __future__ import annotations

import hashlib
import math
from collections import Counter
from dataclasses import dataclass


@dataclass(frozen=True)
class Document:
    id: str
    title: str
    text: str


DOCUMENTS = [
    Document(
        id="auth-403",
        title="Troubleshooting HTTP 403",
        text="HTTP 403 means the user is authenticated but does not have permission for the resource.",
    ),
    Document(
        id="auth-401",
        title="Troubleshooting HTTP 401",
        text="HTTP 401 means authentication failed or the access token is missing, expired, or invalid.",
    ),
    Document(
        id="api-key-rotation",
        title="API Key Rotation",
        text="Rotate API keys every 90 days. Create a new key, deploy it, verify traffic, and revoke the old key.",
    ),
    Document(
        id="rag-eval",
        title="RAG Evaluation",
        text="Measure retrieval with recall at k and generation with faithfulness, answer relevance, and citation accuracy.",
    ),
]


def tokenize(text: str) -> list[str]:
    return [
        token.strip(".,!?;:()[]{}\"'").lower()
        for token in text.split()
        if token.strip(".,!?;:()[]{}\"'")
    ]


def semantic_embedding(text: str, dimensions: int = 96) -> list[float]:
    vector = [0.0] * dimensions
    for token in tokenize(text):
        digest = hashlib.blake2b(token.encode("utf-8"), digest_size=8).digest()
        index = int.from_bytes(digest[:4], "big") % dimensions
        vector[index] += 1.0

    norm = math.sqrt(sum(value * value for value in vector))
    return [value / norm for value in vector] if norm else vector


def cosine(left: list[float], right: list[float]) -> float:
    return sum(a * b for a, b in zip(left, right))


class BM25:
    def __init__(self, documents: list[Document], k1: float = 1.5, b: float = 0.75) -> None:
        self.documents = documents
        self.k1 = k1
        self.b = b
        self.tokens_by_doc = [tokenize(doc.title + " " + doc.text) for doc in documents]
        self.term_counts = [Counter(tokens) for tokens in self.tokens_by_doc]
        self.doc_lengths = [len(tokens) for tokens in self.tokens_by_doc]
        self.avg_doc_length = sum(self.doc_lengths) / len(self.doc_lengths)
        self.document_frequency = Counter(
            term
            for tokens in self.tokens_by_doc
            for term in set(tokens)
        )

    def idf(self, term: str) -> float:
        total = len(self.documents)
        containing = self.document_frequency.get(term, 0)
        return math.log(1 + (total - containing + 0.5) / (containing + 0.5))

    def score(self, query: str, document_index: int) -> float:
        query_terms = tokenize(query)
        counts = self.term_counts[document_index]
        doc_length = self.doc_lengths[document_index]

        score = 0.0
        for term in query_terms:
            frequency = counts.get(term, 0)
            if frequency == 0:
                continue

            numerator = frequency * (self.k1 + 1)
            denominator = frequency + self.k1 * (
                1 - self.b + self.b * doc_length / self.avg_doc_length
            )
            score += self.idf(term) * numerator / denominator

        return score


def min_max_normalize(scores: dict[str, float]) -> dict[str, float]:
    if not scores:
        return {}
    values = list(scores.values())
    low, high = min(values), max(values)
    if math.isclose(low, high):
        return {key: 1.0 for key in scores}
    return {key: (value - low) / (high - low) for key, value in scores.items()}


def hybrid_search(query: str, documents: list[Document], k: int = 3, alpha: float = 0.55) -> list[tuple[float, Document, dict[str, float]]]:
    """
    alpha controls semantic-vs-keyword weighting.
    alpha=1.0 means semantic only; alpha=0.0 means BM25 only.
    """
    query_embedding = semantic_embedding(query)
    document_embeddings = {
        document.id: semantic_embedding(document.title + " " + document.text)
        for document in documents
    }
    bm25 = BM25(documents)

    semantic_scores = {
        document.id: cosine(query_embedding, document_embeddings[document.id])
        for document in documents
    }
    keyword_scores = {
        document.id: bm25.score(query, index)
        for index, document in enumerate(documents)
    }

    semantic_norm = min_max_normalize(semantic_scores)
    keyword_norm = min_max_normalize(keyword_scores)

    results = []
    for document in documents:
        semantic = semantic_norm[document.id]
        keyword = keyword_norm[document.id]
        combined = alpha * semantic + (1 - alpha) * keyword
        results.append((combined, document, {"semantic": semantic, "keyword": keyword}))

    return sorted(results, key=lambda item: item[0], reverse=True)[:k]


def main() -> None:
    queries = [
        "403 permission problem",
        "how often do we revoke old API tokens",
        "faithfulness metric for RAG answers",
    ]

    for query in queries:
        print(f"\nQuery: {query}")
        for score, document, parts in hybrid_search(query, DOCUMENTS):
            print(
                f"- {document.id:<18} combined={score:.3f} "
                f"semantic={parts['semantic']:.3f} keyword={parts['keyword']:.3f} "
                f"title={document.title}"
            )


if __name__ == "__main__":
    main()

