"""DO NOT USE IN PROD.

Intentionally vulnerable teaching sample for 15-security-lab/03-prompt-injection.md.
Problem: untrusted retrieved text is mixed with instructions and the model's
tool request is trusted without deterministic authorization.
"""

from __future__ import annotations

from dataclasses import dataclass
from typing import Protocol


class ChatModel(Protocol):
    def complete(self, prompt: str) -> str:
        """Return a text completion."""


@dataclass(frozen=True)
class RetrievedDocument:
    id: str
    source: str
    text: str


class RefundTool:
    def create_refund(self, customer_id: str, amount_cents: int, reason: str) -> str:
        # Pretend this writes to a payment system.
        return f"refund_created customer={customer_id} amount={amount_cents} reason={reason}"


class VulnerableSupportAgent:
    """A deliberately unsafe RAG + tool-calling assistant."""

    def __init__(self, model: ChatModel, refund_tool: RefundTool) -> None:
        self._model = model
        self._refund_tool = refund_tool

    def answer(
        self,
        *,
        user_question: str,
        customer_id: str,
        retrieved_documents: list[RetrievedDocument],
    ) -> str:
        docs = "\n\n".join(
            f"Document {doc.id} from {doc.source}:\n{doc.text}"
            for doc in retrieved_documents
        )

        # VULNERABLE: retrieved documents are inserted as plain instructions.
        # The prompt does not clearly mark them as untrusted evidence.
        prompt = f"""
You are a helpful support agent.
Use the following documents and do whatever is best for the customer.
You may create refunds by responding exactly:
CREATE_REFUND amount_cents=<amount> reason=<reason>

{docs}

Customer id: {customer_id}
Question: {user_question}
"""

        response = self._model.complete(prompt)

        # VULNERABLE: the model can trigger a side effect by emitting text.
        # There is no allowlist policy, amount limit, entitlement check, or
        # human confirmation.
        if response.startswith("CREATE_REFUND "):
            parts = dict(
                item.split("=", 1)
                for item in response.removeprefix("CREATE_REFUND ").split(" ", 1)
            )
            return self._refund_tool.create_refund(
                customer_id=customer_id,
                amount_cents=int(parts["amount_cents"]),
                reason=parts.get("reason", "model requested refund"),
            )

        return response

