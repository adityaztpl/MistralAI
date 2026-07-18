#!/usr/bin/env python3
"""Stream a Mistral chat completion from an OpenAI-compatible endpoint."""

from __future__ import annotations

import json
import os
import sys
from typing import Any

import requests


BASE_URL = os.getenv("MISTRAL_BASE_URL", "https://api.mistral.ai/v1")
MODEL = os.getenv("MISTRAL_CHAT_MODEL", "mistral-small-latest")


def api_key() -> str:
    value = os.getenv("MISTRAL_API_KEY")
    if not value:
        raise RuntimeError("Set MISTRAL_API_KEY before running this example.")
    return value


def stream_chat(messages: list[dict[str, str]]) -> None:
    payload: dict[str, Any] = {
        "model": MODEL,
        "messages": messages,
        "temperature": 0.2,
        "max_tokens": 700,
        "stream": True,
    }

    with requests.post(
        f"{BASE_URL.rstrip('/')}/chat/completions",
        headers={"Authorization": f"Bearer {api_key()}", "Content-Type": "application/json"},
        json=payload,
        timeout=90,
        stream=True,
    ) as response:
        if not response.ok:
            raise RuntimeError(f"{response.status_code}: {response.text}")

        for raw_line in response.iter_lines(decode_unicode=True):
            if not raw_line or not raw_line.startswith("data:"):
                continue

            data = raw_line[len("data:") :].strip()
            if data == "[DONE]":
                print()
                return

            try:
                event = json.loads(data)
            except json.JSONDecodeError:
                continue

            delta = event.get("choices", [{}])[0].get("delta", {})
            token = delta.get("content")
            if token:
                print(token, end="", flush=True)


def main() -> None:
    try:
        stream_chat(
            [
                {
                    "role": "system",
                    "content": "You are a practical interviewer. Answer in crisp bullets.",
                },
                {
                    "role": "user",
                    "content": "Give me five signs that a RAG system is production-ready.",
                },
            ]
        )
    except KeyboardInterrupt:
        print("\nCancelled by user.", file=sys.stderr)


if __name__ == "__main__":
    main()

