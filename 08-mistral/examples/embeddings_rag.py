#!/usr/bin/env python3
"""Tiny in-memory RAG demo with Mistral embeddings.

This uses a list as the vector store so the retrieval mechanics are easy to inspect.
For production, persist chunks and vectors in pgvector, a search engine, or a vector DB.
"""

from __future__ import annotations

import math
import os
from dataclasses import dataclass
from typing import Any

import requests


BASE_URL = os.getenv("MISTRAL_BASE_URL", "https://api.mistral.ai/v1")
CHAT_MODEL = os.getenv("MISTRAL_CHAT_MODEL", "mistral-small-latest")
EMBEDDING_MODEL = os.getenv("MISTRAL_EMBEDDING_MODEL", "mistral-embed")


@dataclass(frozen=True)
class Chunk:
    id: int
    title: str
    source: str
    text: str
    embedding: list[float]


DOCUMENTS = [
    (
        "Incident Review Policy",
        "handbook",
        "Severity-one incidents require an incident commander, customer impact summary, "
        "minute-by-minute timeline, contributing factors, and follow-up owners within five business days.",
    ),
    (
        "Refund Policy",
        "billing",
        "Annual plan refunds are available within thirty days of purchase if usage is below the published threshold.",
    ),
    (
        "Deployment Runbook",
        "platform",
        "Production deployments require a green canary, dashboard watch, rollback owner, and post-deploy smoke test.",
    ),
]


def api_key() -> str:
    value = os.getenv("MISTRAL_API_KEY")
    if not value:
        raise RuntimeError("Set MISTRAL_API_KEY before running this example.")
    return value


def post(path: str, payload: dict[str, Any]) -> dict[str, Any]:
    response = requests.post(
        f"{BASE_URL.rstrip('/')}/{path.lstrip('/')}",
        headers={"Authorization": f"Bearer {api_key()}", "Content-Type": "application/json"},
        json=payload,
        timeout=60,
    )
    if not response.ok:
        raise RuntimeError(f"{response.status_code}: {response.text}")
    return response.json()


def embed(text: str) -> list[float]:
    result = post("embeddings", {"model": EMBEDDING_MODEL, "input": [text]})
    return [float(value) for value in result["data"][0]["embedding"]]


def cosine_similarity(left: list[float], right: list[float]) -> float:
    dot = sum(a * b for a, b in zip(left, right))
    left_norm = math.sqrt(sum(a * a for a in left))
    right_norm = math.sqrt(sum(b * b for b in right))
    if left_norm == 0 or right_norm == 0:
        return 0.0
    return dot / (left_norm * right_norm)


def build_index() -> list[Chunk]:
    chunks: list[Chunk] = []
    for index, (title, source, text) in enumerate(DOCUMENTS, start=1):
        chunks.append(Chunk(index, title, source, text, embed(text)))
    return chunks


def retrieve(index: list[Chunk], question: str, top_k: int = 2) -> list[tuple[Chunk, float]]:
    query_embedding = embed(question)
    scored = [(chunk, cosine_similarity(query_embedding, chunk.embedding)) for chunk in index]
    return sorted(scored, key=lambda item: item[1], reverse=True)[:top_k]


def answer(question: str, retrieved: list[tuple[Chunk, float]]) -> str:
    context = "\n\n".join(
        f"[{position}] title={chunk.title} source={chunk.source} score={score:.3f}\n{chunk.text}"
        for position, (chunk, score) in enumerate(retrieved, start=1)
    )
    result = post(
        "chat/completions",
        {
            "model": CHAT_MODEL,
            "messages": [
                {
                    "role": "system",
                    "content": (
                        "Answer only from the context. If the context is insufficient, say so. "
                        "Cite sources with bracket numbers like [1]."
                    ),
                },
                {"role": "user", "content": f"Context:\n{context}\n\nQuestion:\n{question}"},
            ],
            "temperature": 0.1,
            "max_tokens": 500,
        },
    )
    return result["choices"][0]["message"]["content"]


def main() -> None:
    print("Embedding local documents...")
    index = build_index()

    question = "What must be included in a severity-one incident review?"
    retrieved = retrieve(index, question)

    print("\nRetrieved chunks:")
    for chunk, score in retrieved:
        print(f"- {chunk.title} ({chunk.source}) score={score:.3f}")

    print("\nAnswer:")
    print(answer(question, retrieved))


if __name__ == "__main__":
    main()

