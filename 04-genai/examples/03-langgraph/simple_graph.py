"""
Minimal LangGraph state machine.

Install:
    pip install langgraph

Run:
    python simple_graph.py
"""

from __future__ import annotations

from typing import TypedDict

from langgraph.graph import END, StateGraph


class StudyState(TypedDict):
    topic: str
    level: str
    outline: str
    quiz: list[str]


def create_outline(state: StudyState) -> dict:
    topic = state["topic"]
    level = state["level"]
    return {
        "outline": (
            f"{topic} ({level})\n"
            f"1. Core concepts\n"
            f"2. Architecture and trade-offs\n"
            f"3. Production concerns\n"
            f"4. Interview talking points"
        )
    }


def create_quiz(state: StudyState) -> dict:
    topic = state["topic"]
    return {
        "quiz": [
            f"What problem does {topic} solve?",
            f"What are common failure modes in {topic} systems?",
            f"How would you evaluate a production {topic} implementation?",
        ]
    }


def build_graph():
    builder = StateGraph(StudyState)
    builder.add_node("create_outline", create_outline)
    builder.add_node("create_quiz", create_quiz)

    builder.set_entry_point("create_outline")
    builder.add_edge("create_outline", "create_quiz")
    builder.add_edge("create_quiz", END)

    return builder.compile()


def main() -> None:
    graph = build_graph()
    result = graph.invoke(
        {
            "topic": "RAG",
            "level": "intermediate",
            "outline": "",
            "quiz": [],
        }
    )

    print(result["outline"])
    print("\nQuiz:")
    for question in result["quiz"]:
        print(f"- {question}")


if __name__ == "__main__":
    main()

