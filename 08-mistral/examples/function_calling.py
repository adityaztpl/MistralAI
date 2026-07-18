#!/usr/bin/env python3
"""Mistral function-calling loop with local tools.

The model proposes tool calls. This program validates and executes the local tool,
then sends the tool result back for a final answer.
"""

from __future__ import annotations

import json
import os
from typing import Any, Callable

import requests


BASE_URL = os.getenv("MISTRAL_BASE_URL", "https://api.mistral.ai/v1")
MODEL = os.getenv("MISTRAL_CHAT_MODEL", "mistral-large-latest")

ORDERS = {
    "ORD-1001": {"status": "shipped", "carrier": "UPS", "eta": "2026-07-21"},
    "ORD-1002": {"status": "processing", "carrier": None, "eta": None},
}


def api_key() -> str:
    value = os.getenv("MISTRAL_API_KEY")
    if not value:
        raise RuntimeError("Set MISTRAL_API_KEY before running this example.")
    return value


def chat(payload: dict[str, Any]) -> dict[str, Any]:
    response = requests.post(
        f"{BASE_URL.rstrip('/')}/chat/completions",
        headers={"Authorization": f"Bearer {api_key()}", "Content-Type": "application/json"},
        json=payload,
        timeout=60,
    )
    if not response.ok:
        raise RuntimeError(f"{response.status_code}: {response.text}")
    return response.json()


def get_order_status(order_id: str) -> str:
    if not order_id.startswith("ORD-"):
        return json.dumps({"error": "invalid_order_id", "message": "Order ids must start with ORD-."})

    order = ORDERS.get(order_id)
    if not order:
        return json.dumps({"error": "not_found", "message": "No order with that id belongs to this user."})

    return json.dumps({"order_id": order_id, **order})


TOOLS = [
    {
        "type": "function",
        "function": {
            "name": "get_order_status",
            "description": "Look up one order status for the authenticated demo user.",
            "parameters": {
                "type": "object",
                "properties": {
                    "order_id": {
                        "type": "string",
                        "description": "Order id such as ORD-1001.",
                    }
                },
                "required": ["order_id"],
                "additionalProperties": False,
            },
        },
    }
]

TOOL_REGISTRY: dict[str, Callable[..., str]] = {"get_order_status": get_order_status}


def execute_tool_call(tool_call: dict[str, Any]) -> dict[str, Any]:
    function = tool_call["function"]
    name = function["name"]
    if name not in TOOL_REGISTRY:
        return {"role": "tool", "tool_call_id": tool_call["id"], "name": name, "content": json.dumps({"error": "unknown_tool"})}

    try:
        arguments = json.loads(function.get("arguments") or "{}")
    except json.JSONDecodeError:
        arguments = {}

    if set(arguments) - {"order_id"} or not isinstance(arguments.get("order_id"), str):
        content = json.dumps({"error": "invalid_arguments", "message": "Expected a string order_id."})
    else:
        content = TOOL_REGISTRY[name](**arguments)

    return {"role": "tool", "tool_call_id": tool_call["id"], "name": name, "content": content}


def main() -> None:
    messages: list[dict[str, Any]] = [
        {
            "role": "system",
            "content": "You are a support assistant. Use tools for live order status. Keep answers concise.",
        },
        {"role": "user", "content": "Where is order ORD-1001?"},
    ]

    for _ in range(5):
        result = chat(
            {
                "model": MODEL,
                "messages": messages,
                "tools": TOOLS,
                "tool_choice": "auto",
                "temperature": 0.1,
            }
        )
        assistant_message = result["choices"][0]["message"]
        messages.append(assistant_message)

        tool_calls = assistant_message.get("tool_calls") or []
        if not tool_calls:
            print(assistant_message.get("content", ""))
            return

        for tool_call in tool_calls:
            tool_result = execute_tool_call(tool_call)
            print(f"tool -> {tool_result['name']}: {tool_result['content']}")
            messages.append(tool_result)

    raise RuntimeError("Tool loop exceeded maximum iterations.")


if __name__ == "__main__":
    main()

