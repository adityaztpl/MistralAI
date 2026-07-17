"""
Agentic RAG with LangGraph.

Flow:
    retrieve
      -> grade_documents
      -> (generate | rewrite_query -> retrieve | fallback)
      -> validate_citations
      -> (end | rewrite_query -> retrieve | fallback)

Install:
    pip install langgraph langchain-core langchain-openai

Run:
    export OPENAI_API_KEY=...
    python agentic_rag.py
"""

from __future__ import annotations

import re
from typing import TypedDict

from langchain_core.prompts import ChatPromptTemplate
from langchain_openai import ChatOpenAI
from pydantic import BaseModel, Field

from langgraph.graph import END, StateGraph


DOCUMENTS = [
    {
        "id": "api-key-policy",
        "text": "API keys must be rotated every 90 days. Revoke the old key only after verifying new-key traffic.",
    },
    {
        "id": "rag-evaluation",
        "text": "RAG systems should be evaluated with faithfulness, context precision, recall at k, and citation accuracy.",
    },
    {
        "id": "streaming",
        "text": "Streaming chat APIs should support cancellation, request IDs, and partial-error handling.",
    },
]


class RagState(TypedDict):
    question: str
    rewritten_question: str
    documents: list[dict[str, str]]
    attempts: int
    answer: str
    route: str
    citations_valid: bool
    validation_error: str


class DocumentGrade(BaseModel):
    relevant: bool = Field(description="Whether the documents can answer the question")
    reason: str


def keyword_retrieve(query: str, k: int = 2) -> list[dict[str, str]]:
    query_terms = {term.lower().strip(".,!?") for term in query.split()}
    scored = []
    for document in DOCUMENTS:
        doc_terms = {term.lower().strip(".,!?") for term in document["text"].split()}
        score = len(query_terms & doc_terms)
        scored.append((score, document))
    return [doc for score, doc in sorted(scored, key=lambda item: item[0], reverse=True)[:k] if score > 0]


def retrieve(state: RagState) -> dict:
    query = state["rewritten_question"] or state["question"]
    return {
        "documents": keyword_retrieve(query),
        "attempts": state["attempts"] + 1,
    }


def grade_documents(state: RagState) -> dict:
    if not state["documents"]:
        return {"route": "rewrite"}

    model = ChatOpenAI(model="gpt-4.1-mini", temperature=0).with_structured_output(DocumentGrade)
    prompt = ChatPromptTemplate.from_template(
        """
Question: {question}

Retrieved documents:
{documents}

Can these documents answer the question?
""".strip()
    )
    chain = prompt | model
    grade = chain.invoke(
        {
            "question": state["question"],
            "documents": "\n".join(f"[{d['id']}] {d['text']}" for d in state["documents"]),
        }
    )

    return {"route": "generate" if grade.relevant else "rewrite"}


def route_after_grade(state: RagState) -> str:
    if state["route"] == "generate":
        return "generate"
    if state["attempts"] >= 2:
        return "fallback"
    return "rewrite_query"


def rewrite_query(state: RagState) -> dict:
    model = ChatOpenAI(model="gpt-4.1-mini", temperature=0)
    prompt = ChatPromptTemplate.from_template(
        "Rewrite this into a concise documentation search query: {question}"
    )
    rewritten = (prompt | model).invoke({"question": state["question"]}).content
    return {"rewritten_question": rewritten or state["question"]}


def generate(state: RagState) -> dict:
    model = ChatOpenAI(model="gpt-4.1-mini", temperature=0)
    prompt = ChatPromptTemplate.from_template(
        """
Answer using only the documents.
Include citations by document id.

Question: {question}

Documents:
{documents}
""".strip()
    )
    answer = (prompt | model).invoke(
        {
            "question": state["question"],
            "documents": "\n".join(f"[{d['id']}] {d['text']}" for d in state["documents"]),
        }
    )
    return {"answer": answer.content or ""}


def validate_citations(state: RagState) -> dict:
    """Deterministically verify that cited IDs came from retrieved documents."""
    retrieved_ids = {document["id"] for document in state["documents"]}
    cited_ids = set(re.findall(r"\[([A-Za-z0-9_.:#-]+)\]", state["answer"]))

    if not cited_ids:
        return {
            "citations_valid": False,
            "validation_error": "answer did not include citations",
        }

    unknown = cited_ids - retrieved_ids
    if unknown:
        return {
            "citations_valid": False,
            "validation_error": f"unknown citation ids: {sorted(unknown)}",
        }

    return {
        "citations_valid": True,
        "validation_error": "",
    }


def route_after_validation(state: RagState) -> str:
    if state["citations_valid"]:
        return END
    if state["attempts"] >= 2:
        return "fallback"
    return "rewrite_query"


def fallback(state: RagState) -> dict:
    suffix = f" Citation validation failed: {state['validation_error']}." if state.get("validation_error") else ""
    return {
        "answer": (
            "I do not have enough relevant context to answer confidently. "
            "Try rephrasing the question or indexing more documentation."
            f"{suffix}"
        )
    }


def build_graph():
    builder = StateGraph(RagState)
    builder.add_node("retrieve", retrieve)
    builder.add_node("grade_documents", grade_documents)
    builder.add_node("rewrite_query", rewrite_query)
    builder.add_node("generate", generate)
    builder.add_node("validate_citations", validate_citations)
    builder.add_node("fallback", fallback)

    builder.set_entry_point("retrieve")
    builder.add_edge("retrieve", "grade_documents")
    builder.add_conditional_edges(
        "grade_documents",
        route_after_grade,
        {
            "generate": "generate",
            "rewrite_query": "rewrite_query",
            "fallback": "fallback",
        },
    )
    builder.add_edge("rewrite_query", "retrieve")
    builder.add_edge("generate", "validate_citations")
    builder.add_conditional_edges(
        "validate_citations",
        route_after_validation,
        {
            END: END,
            "rewrite_query": "rewrite_query",
            "fallback": "fallback",
        },
    )
    builder.add_edge("fallback", END)

    return builder.compile()


def main() -> None:
    graph = build_graph()
    result = graph.invoke(
        {
            "question": "How frequently should keys be changed?",
            "rewritten_question": "",
            "documents": [],
            "attempts": 0,
            "answer": "",
            "route": "",
            "citations_valid": False,
            "validation_error": "",
        },
        {"recursion_limit": 8},
    )
    print(result["answer"])


if __name__ == "__main__":
    main()

