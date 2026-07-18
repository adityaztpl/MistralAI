#!/usr/bin/env python3
"""Basic Mistral chat completion using raw HTTP.

Run:
    export MISTRAL_API_KEY="..."
    python 08-mistral/examples/chat_completion.py
"""

from __future__ import annotations

import os
import time
from typing import Any

import requests


BASE_URL = os.getenv("MISTRAL_BASE_URL", "https://api.mistral.ai/v1")
MODEL = os.getenv("MISTRAL_CHAT_MODEL", "mistral-small-latest")


def require_api_key() -> str:
    api_key = os.getenv("MISTRAL_API_KEY")
    if not api_key:
        raise RuntimeError("Set MISTRAL_API_KEY before running this example.")
    return api_key


def post_with_retries(path: str, payload: dict[str, Any], attempts: int = 4) -> dict[str, Any]:
    headers = {
        "Authorization": f"Bearer {require_api_key()}",
        "Content-Type": "application/json",
    }

    for attempt in range(attempts):
        response = requests.post(
            f"{BASE_URL.rstrip('/')}/{path.lstrip('/')}",
            headers=headers,
            json=payload,
            timeout=45,
        )

        if response.status_code < 400:
            return response.json()

        if response.status_code not in {429, 500, 502, 503, 504} or attempt == attempts - 1:
            raise RuntimeError(f"Mistral request failed: {response.status_code} {response.text}")

        retry_after = response.headers.get("Retry-After")
        delay = float(retry_after) if retry_after else min(8.0, 0.5 * (2**attempt))
        time.sleep(delay)

    raise AssertionError("unreachable")


def main() -> None:
    payload = {
        "model": MODEL,
        "messages": [
            {
                "role": "system",
                "content": "You are a concise senior backend engineer. Prefer concrete tradeoffs.",
            },
            {
                "role": "user",
                "content": "Explain idempotency keys for payment APIs in four bullets.",
            },
        ],
        "temperature": 0.2,
        "max_tokens": 500,
    }

    result = post_with_retries("chat/completions", payload)
    message = result["choices"][0]["message"]["content"]
    usage = result.get("usage", {})

    print(message)
    print("\n--- usage ---")
    print(usage)


if __name__ == "__main__":
    main()

