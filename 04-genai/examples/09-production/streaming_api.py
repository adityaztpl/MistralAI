"""
Streaming chat API example with FastAPI-style Server-Sent Events.

Run local demo without installing anything:
    python streaming_api.py

Run as an API:
    pip install fastapi uvicorn
    uvicorn streaming_api:app --reload

Then:
    curl -N -X POST http://localhost:8000/chat/stream \
      -H "content-type: application/json" \
      -d '{"message":"How should streaming APIs behave?"}'

What it demonstrates:
- SSE event formatting.
- Progress events before token streaming.
- Cancellation-friendly async generator.
- Final metadata event with citations and token usage.
- Provider call is simulated so the example is runnable.
"""

from __future__ import annotations

import asyncio
import json
import time
from dataclasses import dataclass
from typing import AsyncIterator


try:
    from fastapi import FastAPI, Request
    from fastapi.responses import StreamingResponse
    from pydantic import BaseModel
except ImportError:  # The script can still run its CLI demo.
    FastAPI = None  # type: ignore[assignment]
    Request = object  # type: ignore[assignment,misc]
    StreamingResponse = None  # type: ignore[assignment]
    BaseModel = object  # type: ignore[assignment,misc]


@dataclass(frozen=True)
class Chunk:
    id: str
    title: str
    text: str


DOCS = [
    Chunk(
        id="streaming-policy",
        title="Streaming Chat Requirements",
        text="Streaming chat APIs must include request IDs, support cancellation, and emit final metadata.",
    ),
    Chunk(
        id="guardrails-policy",
        title="Guardrails",
        text="Generated answers must validate citations before source cards are shown.",
    ),
]


if FastAPI:
    app = FastAPI(title="Streaming GenAI Example")
else:
    app = None


class ChatRequest(BaseModel):  # type: ignore[misc, valid-type]
    message: str


def sse(event: str, data: dict[str, object]) -> str:
    return f"event: {event}\ndata: {json.dumps(data)}\n\n"


def retrieve_context(message: str) -> list[Chunk]:
    lowered = message.lower()
    if "guardrail" in lowered or "citation" in lowered:
        return [DOCS[1], DOCS[0]]
    return [DOCS[0], DOCS[1]]


async def simulated_llm_stream(message: str, context: list[Chunk]) -> AsyncIterator[str]:
    answer = (
        "Streaming APIs should send progress and token events, support cancellation, "
        "and finish with metadata such as citations and token usage "
        f"[{context[0].id}]."
    )
    for token in answer.split():
        await asyncio.sleep(0.035)
        yield token + " "


async def stream_chat_events(message: str, request: Request | None = None) -> AsyncIterator[str]:
    started = time.perf_counter()
    request_id = f"req-{int(started * 1000)}"

    yield sse("metadata", {"request_id": request_id, "stage": "accepted"})

    context = retrieve_context(message)
    yield sse(
        "metadata",
        {
            "request_id": request_id,
            "stage": "retrieved",
            "citations": [{"id": chunk.id, "title": chunk.title} for chunk in context],
        },
    )

    output_tokens = 0
    async for token in simulated_llm_stream(message, context):
        if request is not None and await request.is_disconnected():
            # In a real provider call, propagate cancellation to the model client.
            yield sse("error", {"request_id": request_id, "message": "client disconnected"})
            return
        output_tokens += 1
        yield sse("delta", {"request_id": request_id, "text": token})

    elapsed_ms = int((time.perf_counter() - started) * 1000)
    yield sse(
        "done",
        {
            "request_id": request_id,
            "elapsed_ms": elapsed_ms,
            "usage": {
                "input_tokens_estimate": len(message.split()) + sum(len(c.text.split()) for c in context),
                "output_tokens_estimate": output_tokens,
            },
            "citations": [{"id": chunk.id, "title": chunk.title} for chunk in context],
        },
    )


if app:

    @app.post("/chat/stream")
    async def chat_stream(request_body: ChatRequest, request: Request) -> StreamingResponse:
        return StreamingResponse(
            stream_chat_events(request_body.message, request),
            media_type="text/event-stream",
            headers={
                "Cache-Control": "no-cache",
                "X-Accel-Buffering": "no",
            },
        )


async def cli_demo() -> None:
    async for event in stream_chat_events("How should streaming APIs behave?"):
        print(event, end="")


if __name__ == "__main__":
    asyncio.run(cli_demo())
