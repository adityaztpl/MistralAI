"""
Simple multi-agent workflow with LangGraph.

This example uses a supervisor-style graph with three specialist nodes:
researcher -> architect -> reviewer.

Install:
    pip install langgraph langchain-core langchain-openai

Run:
    export OPENAI_API_KEY=...
    python multi_agent.py
"""

from __future__ import annotations

from typing import TypedDict

from langchain_core.prompts import ChatPromptTemplate
from langchain_openai import ChatOpenAI
from langgraph.graph import END, StateGraph


class MultiAgentState(TypedDict):
    task: str
    research_notes: str
    architecture: str
    review: str
    final: str


def call_specialist(role: str, instruction: str, task: str, context: str = "") -> str:
    model = ChatOpenAI(model="gpt-4.1-mini", temperature=0.2)
    prompt = ChatPromptTemplate.from_messages(
        [
            ("system", f"You are the {role}. Be practical and specific."),
            (
                "human",
                """
Task:
{task}

Context:
{context}

Instruction:
{instruction}
""".strip(),
            ),
        ]
    )
    response = (prompt | model).invoke(
        {"task": task, "context": context, "instruction": instruction}
    )
    return response.content or ""


def researcher(state: MultiAgentState) -> dict:
    notes = call_specialist(
        role="research agent",
        instruction="List the core requirements, risks, and important unknowns.",
        task=state["task"],
    )
    return {"research_notes": notes}


def architect(state: MultiAgentState) -> dict:
    architecture = call_specialist(
        role="solution architect agent",
        instruction="Propose a concrete service architecture and data flow.",
        task=state["task"],
        context=state["research_notes"],
    )
    return {"architecture": architecture}


def reviewer(state: MultiAgentState) -> dict:
    review = call_specialist(
        role="reviewer agent",
        instruction="Find gaps, security concerns, scaling concerns, and missing tests.",
        task=state["task"],
        context=state["architecture"],
    )
    return {"review": review}


def synthesize(state: MultiAgentState) -> dict:
    final = (
        "# Final Multi-Agent Recommendation\n\n"
        "## Research Notes\n"
        f"{state['research_notes']}\n\n"
        "## Architecture\n"
        f"{state['architecture']}\n\n"
        "## Review Findings\n"
        f"{state['review']}\n"
    )
    return {"final": final}


def build_graph():
    builder = StateGraph(MultiAgentState)
    builder.add_node("researcher", researcher)
    builder.add_node("architect", architect)
    builder.add_node("reviewer", reviewer)
    builder.add_node("synthesize", synthesize)

    builder.set_entry_point("researcher")
    builder.add_edge("researcher", "architect")
    builder.add_edge("architect", "reviewer")
    builder.add_edge("reviewer", "synthesize")
    builder.add_edge("synthesize", END)

    return builder.compile()


def main() -> None:
    graph = build_graph()
    result = graph.invoke(
        {
            "task": "Design a document Q&A system for an ASP.NET Core and React product.",
            "research_notes": "",
            "architecture": "",
            "review": "",
            "final": "",
        }
    )
    print(result["final"])


if __name__ == "__main__":
    main()

