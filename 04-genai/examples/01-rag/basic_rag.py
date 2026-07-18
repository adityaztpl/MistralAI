"""
Basic RAG example with no required external services.

What it demonstrates:
- Chunking source documents.
- Creating simple deterministic embeddings.
- Vector similarity retrieval.
- Building a grounded prompt with citations.
- Optional LLM call if OPENAI_API_KEY is configured.

Run:
    python basic_rag.py

Optional:
    pip install openai
    export OPENAI_API_KEY=...
"""

from __future__ import annotations

import hashlib
import math
import os
from dataclasses import dataclass
from typing import Iterable


@dataclass(frozen=True)
class SourceDocument:
    id: str
    title: str
    uri: str
    text: str


@dataclass(frozen=True)
class Chunk:
    id: str
    document_id: str
    title: str
    uri: str
    text: str
    embedding: list[float]


DOCUMENTS = [
    SourceDocument(
        id="security-api-keys",
        title="API Key Security Policy",
        uri="https://docs.example.com/security/api-keys",
        text=(
            "API keys must be rotated every 90 days. Store keys only in the "
            "approved secrets manager. Never commit API keys to source control. "
            "To rotate a key, create a replacement key, deploy the new key, "
            "verify traffic, and then revoke the old key."
        ),
    ),
    SourceDocument(
        id="rag-architecture",
        title="RAG Architecture Guide",
        uri="https://docs.example.com/ai/rag",
        text=(
            "A retrieval augmented generation system has an ingestion pipeline "
            "and an answer pipeline. The ingestion pipeline chunks documents, "
            "embeds chunks, and stores vectors with metadata. The answer pipeline "
            "retrieves relevant chunks, builds a prompt, and asks the model to "
            "answer using only retrieved context."
        ),
    ),
    SourceDocument(
        id="incident-process",
        title="Incident Response Process",
        uri="https://docs.example.com/ops/incidents",
        text=(
            "Severity one incidents require an incident commander, a scribe, "
            "and status updates every 15 minutes. Customer-facing updates must "
            "be approved by support leadership before publication."
        ),
    ),
]


def tokenize(text: str) -> list[str]:
    return [
        token.strip(".,!?;:()[]{}\"'").lower()
        for token in text.split()
        if token.strip(".,!?;:()[]{}\"'")
    ]


def simple_embedding(text: str, dimensions: int = 128) -> list[float]:
    """Create a deterministic bag-of-words style embedding for local study."""
    vector = [0.0] * dimensions
    for token in tokenize(text):
        digest = hashlib.sha256(token.encode("utf-8")).digest()
        index = int.from_bytes(digest[:4], "big") % dimensions
        sign = 1.0 if digest[4] % 2 == 0 else -1.0
        vector[index] += sign

    norm = math.sqrt(sum(value * value for value in vector))
    if norm == 0:
        return vector
    return [value / norm for value in vector]


def chunk_document(document: SourceDocument, chunk_size_words: int = 45, overlap_words: int = 8) -> list[str]:
    words = document.text.split()
    chunks: list[str] = []
    step = max(1, chunk_size_words - overlap_words)

    for start in range(0, len(words), step):
        chunk_words = words[start : start + chunk_size_words]
        if chunk_words:
            chunks.append(" ".join(chunk_words))

    return chunks


def build_index(documents: Iterable[SourceDocument]) -> list[Chunk]:
    chunks: list[Chunk] = []
    for document in documents:
        for index, text in enumerate(chunk_document(document), start=1):
            chunk_id = f"{document.id}#chunk-{index}"
            chunks.append(
                Chunk(
                    id=chunk_id,
                    document_id=document.id,
                    title=document.title,
                    uri=document.uri,
                    text=text,
                    embedding=simple_embedding(text),
                )
            )
    return chunks


def cosine_similarity(left: list[float], right: list[float]) -> float:
    return sum(a * b for a, b in zip(left, right))


def retrieve(question: str, chunks: list[Chunk], k: int = 3) -> list[tuple[float, Chunk]]:
    query_embedding = simple_embedding(question)
    scored = [
        (cosine_similarity(query_embedding, chunk.embedding), chunk)
        for chunk in chunks
    ]
    return sorted(scored, key=lambda item: item[0], reverse=True)[:k]


def build_prompt(question: str, retrieved: list[tuple[float, Chunk]]) -> str:
    context = "\n\n".join(
        f"[{chunk.id}] {chunk.title}\nURL: {chunk.uri}\n{chunk.text}"
        for _, chunk in retrieved
    )

    return f"""
You are a grounded assistant.
Answer using only the context below.
If the context does not contain the answer, say "I do not know based on the provided context."
Include citations using chunk IDs like [security-api-keys#chunk-1].

Question:
{question}

Context:
{context}
""".strip()


def call_llm_if_configured(prompt: str) -> str | None:
    """Use OpenAI-compatible chat if available; otherwise return None."""
    if not os.environ.get("OPENAI_API_KEY"):
        return None

    try:
        from openai import OpenAI
    except ImportError as exc:
        raise RuntimeError("Install openai to enable LLM calls: pip install openai") from exc

    client = OpenAI()
    response = client.chat.completions.create(
        model=os.environ.get("OPENAI_MODEL", "gpt-4.1-mini"),
        messages=[
            {"role": "system", "content": "You answer with citations."},
            {"role": "user", "content": prompt},
        ],
        temperature=0.0,
    )
    return response.choices[0].message.content or ""


def extractive_fallback(question: str, retrieved: list[tuple[float, Chunk]]) -> str:
    """A deterministic fallback so the example runs without API keys."""
    best_score, best_chunk = retrieved[0]
    return (
        f"No LLM key configured, so returning the top retrieved chunk.\n\n"
        f"Question: {question}\n"
        f"Top score: {best_score:.3f}\n"
        f"Answer evidence: {best_chunk.text}\n"
        f"Citation: [{best_chunk.id}] {best_chunk.uri}"
    )


def main() -> None:
    chunks = build_index(DOCUMENTS)
    question = "How should we rotate API keys?"
    retrieved = retrieve(question, chunks, k=3)

    print("Retrieved chunks:")
    for score, chunk in retrieved:
        print(f"- {chunk.id} score={score:.3f} title={chunk.title}")

    prompt = build_prompt(question, retrieved)
    answer = call_llm_if_configured(prompt) or extractive_fallback(question, retrieved)

    print("\n--- Prompt ---")
    print(prompt)
    print("\n--- Answer ---")
    print(answer)


if __name__ == "__main__":
    main()

