"""
Modern LangChain LCEL chain example.

Install:
    pip install langchain-core langchain-openai

Run:
    export OPENAI_API_KEY=...
    python lcel_chain.py
"""

from __future__ import annotations

from pydantic import BaseModel, Field

from langchain_core.prompts import ChatPromptTemplate
from langchain_openai import ChatOpenAI


class InterviewPrep(BaseModel):
    topic: str = Field(description="The interview topic")
    summary: str = Field(description="Short explanation of the topic")
    questions: list[str] = Field(description="Practical interview questions")
    red_flags: list[str] = Field(description="Common mistakes candidates make")


def build_chain():
    prompt = ChatPromptTemplate.from_messages(
        [
            (
                "system",
                "You are a senior engineer creating concise interview prep material.",
            ),
            (
                "human",
                "Create study notes for {topic}. Include exactly {question_count} questions.",
            ),
        ]
    )

    model = ChatOpenAI(model="gpt-4.1-mini", temperature=0.2)
    structured_model = model.with_structured_output(InterviewPrep)

    return prompt | structured_model


def main() -> None:
    chain = build_chain()
    result = chain.invoke(
        {
            "topic": "ASP.NET Core dependency injection",
            "question_count": 4,
        }
    )

    print(result.model_dump_json(indent=2))


if __name__ == "__main__":
    main()

