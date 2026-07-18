"""
Minimal ReAct-style agent with safe tool execution.

Run:
    python react_agent.py

This example avoids external model dependencies so the control loop is visible.
In a real app, the `planner` function would be replaced by a model call that
returns structured tool calls.

What it demonstrates:
- ReAct loop: think/act/observe/final.
- Narrow tools with typed argument validation.
- Loop limits.
- Read-only vs side-effect policy.
- Trace logging for agent steps.
"""

from __future__ import annotations

import json
from dataclasses import dataclass
from typing import Any, Callable, Literal


ToolKind = Literal["read", "calculate", "side_effect"]


@dataclass(frozen=True)
class ToolSpec:
    name: str
    description: str
    kind: ToolKind
    handler: Callable[[dict[str, Any]], dict[str, Any]]
    required_args: set[str]


@dataclass
class Step:
    thought: str
    action: str | None = None
    action_input: dict[str, Any] | None = None
    observation: dict[str, Any] | None = None


POLICIES = {
    "api keys": "API keys must be rotated every 90 days and stored in the approved secrets manager.",
    "streaming": "Streaming endpoints must include request IDs, cancellation, and final metadata events.",
    "pii": "PII must be redacted from prompts and logs unless explicitly approved.",
}


def lookup_policy(args: dict[str, Any]) -> dict[str, Any]:
    topic = str(args["topic"]).lower()
    return {
        "topic": topic,
        "policy": POLICIES.get(topic, "No policy found."),
        "source": f"policy://{topic.replace(' ', '-')}",
    }


def estimate_llm_cost(args: dict[str, Any]) -> dict[str, Any]:
    input_millions = float(args["input_tokens_millions"])
    output_millions = float(args["output_tokens_millions"])
    input_rate = 0.15
    output_rate = 0.60
    total = input_millions * input_rate + output_millions * output_rate
    return {
        "input_cost": round(input_millions * input_rate, 4),
        "output_cost": round(output_millions * output_rate, 4),
        "total_cost": round(total, 4),
        "currency": "USD",
    }


def create_refund(args: dict[str, Any]) -> dict[str, Any]:
    # This tool is intentionally side-effecting to demonstrate blocking.
    return {
        "refund_id": "REF-123",
        "invoice_id": args["invoice_id"],
        "status": "created",
    }


TOOLS = {
    "lookup_policy": ToolSpec(
        name="lookup_policy",
        description="Read a short internal policy snippet by topic.",
        kind="read",
        handler=lookup_policy,
        required_args={"topic"},
    ),
    "estimate_llm_cost": ToolSpec(
        name="estimate_llm_cost",
        description="Estimate LLM cost from input/output token volumes.",
        kind="calculate",
        handler=estimate_llm_cost,
        required_args={"input_tokens_millions", "output_tokens_millions"},
    ),
    "create_refund": ToolSpec(
        name="create_refund",
        description="Create a refund. Requires explicit human approval.",
        kind="side_effect",
        handler=create_refund,
        required_args={"invoice_id"},
    ),
}


def validate_tool_call(tool_name: str, args: dict[str, Any], approved_actions: set[str]) -> ToolSpec:
    if tool_name not in TOOLS:
        raise ValueError(f"unknown tool: {tool_name}")

    tool = TOOLS[tool_name]
    missing = tool.required_args - set(args)
    if missing:
        raise ValueError(f"{tool_name} missing required args: {sorted(missing)}")

    if tool.kind == "side_effect" and tool_name not in approved_actions:
        raise PermissionError(f"{tool_name} requires human approval")

    return tool


def execute_tool(tool_name: str, args: dict[str, Any], approved_actions: set[str]) -> dict[str, Any]:
    tool = validate_tool_call(tool_name, args, approved_actions)
    result = tool.handler(args)
    return {
        "tool": tool.name,
        "kind": tool.kind,
        "result": result,
    }


def planner(question: str, steps: list[Step]) -> Step:
    """
    Deterministic stand-in for an LLM planner.

    A real planner prompt would tell the model:
    - use tools for exact facts/calculations
    - do not call side-effect tools without approval
    - return structured tool calls
    """
    lowered = question.lower()
    observed_tools = {step.action for step in steps if step.observation}

    if "api key" in lowered and "lookup_policy" not in observed_tools:
        return Step(
            thought="Need authoritative policy before answering.",
            action="lookup_policy",
            action_input={"topic": "api keys"},
        )

    if "cost" in lowered and "estimate_llm_cost" not in observed_tools:
        return Step(
            thought="Need deterministic cost calculation.",
            action="estimate_llm_cost",
            action_input={
                "input_tokens_millions": 12,
                "output_tokens_millions": 3,
            },
        )

    if "refund" in lowered and "create_refund" not in observed_tools:
        return Step(
            thought="A refund is a side effect and requires approval.",
            action="create_refund",
            action_input={"invoice_id": "INV-1009"},
        )

    return Step(thought="Enough observations are available to answer.")


def synthesize_answer(question: str, steps: list[Step]) -> str:
    observations = [step.observation for step in steps if step.observation]
    policies = [
        obs["result"]["policy"]
        for obs in observations
        if obs["tool"] == "lookup_policy"
    ]
    costs = [
        obs["result"]
        for obs in observations
        if obs["tool"] == "estimate_llm_cost"
    ]
    denied = [
        obs["error"]
        for obs in observations
        if "error" in obs
    ]

    parts = [f"Question: {question}"]
    if policies:
        parts.append(f"Policy: {policies[0]}")
    if costs:
        parts.append(f"Estimated cost: ${costs[0]['total_cost']:.2f} {costs[0]['currency']}.")
    if denied:
        parts.append(f"Action not performed: {denied[0]}.")
    parts.append("All tool results were produced by application-owned functions.")
    return "\n".join(parts)


def run_agent(question: str, approved_actions: set[str] | None = None, max_steps: int = 5) -> dict[str, Any]:
    approved_actions = approved_actions or set()
    steps: list[Step] = []

    for _ in range(max_steps):
        step = planner(question, steps)
        if not step.action:
            return {
                "answer": synthesize_answer(question, steps),
                "steps": [step.__dict__ for step in steps],
            }

        try:
            observation = execute_tool(step.action, step.action_input or {}, approved_actions)
        except Exception as exc:  # noqa: BLE001 - example logs sanitized errors.
            observation = {
                "tool": step.action,
                "error": str(exc),
            }

        step.observation = observation
        steps.append(step)

        if "error" in observation:
            return {
                "answer": synthesize_answer(question, steps),
                "steps": [step.__dict__ for step in steps],
            }

    return {
        "answer": "I stopped because the agent reached its step limit.",
        "steps": [step.__dict__ for step in steps],
    }


def main() -> None:
    questions = [
        "What is our API key policy and what would 12M input plus 3M output tokens cost?",
        "Please refund invoice INV-1009.",
    ]

    for question in questions:
        print("\n=== Agent run ===")
        result = run_agent(question)
        print(result["answer"])
        print("\nTrace:")
        print(json.dumps(result["steps"], indent=2))

    print("\n=== Approved side-effect run ===")
    approved = run_agent("Please refund invoice INV-1009.", approved_actions={"create_refund"})
    print(json.dumps(approved, indent=2))


if __name__ == "__main__":
    main()
