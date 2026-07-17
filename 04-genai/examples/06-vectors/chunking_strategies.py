"""
Compare chunking strategies for RAG ingestion.

Run:
    python chunking_strategies.py

What it demonstrates:
- Fixed-size chunking.
- Recursive separator chunking.
- Markdown heading-aware chunking.
- Parent-child retrieval metadata.
- Simple local retrieval evaluation for chunking choices.
"""

from __future__ import annotations

import hashlib
import math
import re
from dataclasses import dataclass
from typing import Iterable


DOCUMENT = """
# API Key Security Policy

## Purpose

API keys allow services to authenticate with Acme Cloud APIs. Keys must be
stored in the approved secrets manager and must never be committed to source
control.

## Rotation

API keys must be rotated every 90 days. To rotate safely, create a replacement
key, deploy the new key, verify that production traffic is using the new key,
and only then revoke the old key.

## Emergency Revocation

If a key is exposed in logs, chat, source control, or a ticket, revoke it
immediately. Notify the security team and create an incident if customer data
may have been accessed.

## Streaming Chat Requirements

Streaming chat endpoints must include request IDs, support cancellation, and
return a final metadata event containing citations and token usage.
""".strip()


@dataclass(frozen=True)
class Chunk:
    id: str
    text: str
    metadata: dict[str, str]


def tokenize(text: str) -> list[str]:
    return [
        token.strip(".,!?;:()[]{}\"'").lower()
        for token in text.split()
        if token.strip(".,!?;:()[]{}\"'")
    ]


def simple_embedding(text: str, dimensions: int = 96) -> list[float]:
    vector = [0.0] * dimensions
    for token in tokenize(text):
        digest = hashlib.sha256(token.encode("utf-8")).digest()
        index = int.from_bytes(digest[:4], "big") % dimensions
        sign = 1.0 if digest[4] % 2 == 0 else -1.0
        vector[index] += sign

    norm = math.sqrt(sum(value * value for value in vector))
    return vector if norm == 0 else [value / norm for value in vector]


def cosine(left: list[float], right: list[float]) -> float:
    return sum(a * b for a, b in zip(left, right))


def fixed_size_chunks(text: str, size_words: int = 35, overlap_words: int = 8) -> list[Chunk]:
    words = text.split()
    step = max(1, size_words - overlap_words)
    chunks: list[Chunk] = []
    for index, start in enumerate(range(0, len(words), step), start=1):
        body = " ".join(words[start : start + size_words])
        if body:
            chunks.append(
                Chunk(
                    id=f"fixed-{index}",
                    text=body,
                    metadata={"strategy": "fixed", "start_word": str(start)},
                )
            )
    return chunks


def recursive_chunks(text: str, max_chars: int = 280) -> list[Chunk]:
    separators = ["\n## ", "\n\n", ". ", " "]
    pieces = [text]

    for separator in separators:
        next_pieces: list[str] = []
        for piece in pieces:
            if len(piece) <= max_chars:
                next_pieces.append(piece)
                continue
            splits = piece.split(separator)
            rebuilt: list[str] = []
            for split in splits:
                if not split:
                    continue
                prefix = separator.strip() + " " if separator.startswith("\n##") else ""
                rebuilt.append(prefix + split.strip())
            next_pieces.extend(rebuilt)
        pieces = next_pieces

    chunks = []
    buffer = ""
    chunk_index = 1
    for piece in pieces:
        candidate = f"{buffer} {piece}".strip()
        if len(candidate) <= max_chars:
            buffer = candidate
            continue
        if buffer:
            chunks.append(
                Chunk(
                    id=f"recursive-{chunk_index}",
                    text=buffer,
                    metadata={"strategy": "recursive"},
                )
            )
            chunk_index += 1
        buffer = piece

    if buffer:
        chunks.append(
            Chunk(
                id=f"recursive-{chunk_index}",
                text=buffer,
                metadata={"strategy": "recursive"},
            )
        )

    return chunks


def markdown_heading_chunks(text: str) -> list[Chunk]:
    current_h1 = ""
    current_h2 = ""
    current_lines: list[str] = []
    chunks: list[Chunk] = []

    def flush() -> None:
        nonlocal current_lines
        body = "\n".join(current_lines).strip()
        if not body:
            return
        chunk_id = re.sub(r"[^a-z0-9]+", "-", f"{current_h1}-{current_h2}".lower()).strip("-")
        chunks.append(
            Chunk(
                id=chunk_id,
                text=f"Title: {current_h1}\nSection: {current_h2}\n\n{body}",
                metadata={
                    "strategy": "markdown_heading",
                    "h1": current_h1,
                    "h2": current_h2,
                },
            )
        )
        current_lines = []

    for line in text.splitlines():
        if line.startswith("# "):
            flush()
            current_h1 = line.removeprefix("# ").strip()
            current_h2 = ""
            continue
        if line.startswith("## "):
            flush()
            current_h2 = line.removeprefix("## ").strip()
            continue
        current_lines.append(line)

    flush()
    return chunks


def parent_child_chunks(text: str) -> list[Chunk]:
    """Small child chunks reference a larger parent section for prompt packing."""
    parents = markdown_heading_chunks(text)
    children: list[Chunk] = []
    for parent in parents:
        sentences = [sentence.strip() for sentence in parent.text.split(". ") if sentence.strip()]
        for index, sentence in enumerate(sentences, start=1):
            children.append(
                Chunk(
                    id=f"{parent.id}#child-{index}",
                    text=sentence,
                    metadata={
                        "strategy": "parent_child",
                        "parent_id": parent.id,
                        "parent_text": parent.text,
                    },
                )
            )
    return children


def retrieve(question: str, chunks: Iterable[Chunk], k: int = 3) -> list[tuple[float, Chunk]]:
    query_vector = simple_embedding(question)
    scored = [
        (cosine(query_vector, simple_embedding(chunk.text)), chunk)
        for chunk in chunks
    ]
    return sorted(scored, key=lambda item: item[0], reverse=True)[:k]


def evaluate_strategy(name: str, chunks: list[Chunk], questions: dict[str, str]) -> None:
    print(f"\n=== {name} ({len(chunks)} chunks) ===")
    for chunk in chunks:
        preview = " ".join(chunk.text.split())[:110]
        print(f"- {chunk.id}: {preview}...")

    print("\nRetrieval smoke test:")
    for question, expected_substring in questions.items():
        results = retrieve(question, chunks, k=2)
        hit = any(expected_substring.lower() in chunk.text.lower() for _, chunk in results)
        status = "PASS" if hit else "MISS"
        top = ", ".join(f"{chunk.id}:{score:.2f}" for score, chunk in results)
        print(f"{status} | {question} -> {top}")


def main() -> None:
    questions = {
        "How often do API keys rotate?": "90 days",
        "What should happen before revoking the old key?": "verify",
        "What must streaming endpoints support?": "cancellation",
    }

    strategies = {
        "fixed-size": fixed_size_chunks(DOCUMENT),
        "recursive": recursive_chunks(DOCUMENT),
        "markdown-heading": markdown_heading_chunks(DOCUMENT),
        "parent-child": parent_child_chunks(DOCUMENT),
    }

    for name, chunks in strategies.items():
        evaluate_strategy(name, chunks, questions)

    print(
        "\nInterview takeaway: choose chunking by content type and measure retrieval. "
        "Heading-aware chunks often improve citations; parent-child retrieval helps "
        "when precise search results need broader surrounding context."
    )


if __name__ == "__main__":
    main()
