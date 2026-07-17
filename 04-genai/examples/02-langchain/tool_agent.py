"""
LangChain tool-calling agent example.

Install:
    pip install langchain langchain-core langchain-openai

Run:
    export OPENAI_API_KEY=...
    python tool_agent.py
"""

from __future__ import annotations

from langchain.agents import AgentExecutor, create_tool_calling_agent
from langchain_core.prompts import ChatPromptTemplate, MessagesPlaceholder
from langchain_core.tools import tool
from langchain_openai import ChatOpenAI


@tool
def calculate_monthly_llm_cost(input_tokens_millions: float, output_tokens_millions: float) -> str:
    """Estimate monthly LLM cost using example token prices."""
    input_rate = 0.15
    output_rate = 0.60
    total = input_tokens_millions * input_rate + output_tokens_millions * output_rate
    return (
        f"Estimated monthly cost is ${total:.2f} "
        f"for {input_tokens_millions}M input and {output_tokens_millions}M output tokens."
    )


@tool
def lookup_policy(topic: str) -> str:
    """Look up a short internal policy snippet by topic."""
    policies = {
        "api keys": "API keys must be rotated every 90 days and stored in the secrets manager.",
        "pii": "PII must be redacted from logs and prompts unless explicitly approved.",
        "streaming": "Streaming endpoints must support cancellation and request correlation IDs.",
    }
    return policies.get(topic.lower(), "No policy found for that topic.")


def build_agent() -> AgentExecutor:
    tools = [calculate_monthly_llm_cost, lookup_policy]

    prompt = ChatPromptTemplate.from_messages(
        [
            (
                "system",
                "You are a production GenAI architect. Use tools when they provide exact facts or calculations.",
            ),
            ("human", "{input}"),
            MessagesPlaceholder("agent_scratchpad"),
        ]
    )

    model = ChatOpenAI(model="gpt-4.1-mini", temperature=0)
    agent = create_tool_calling_agent(model, tools, prompt)

    return AgentExecutor(agent=agent, tools=tools, verbose=True)


def main() -> None:
    executor = build_agent()
    result = executor.invoke(
        {
            "input": (
                "What is our API key policy, and what would 12M input tokens "
                "and 3M output tokens cost using the example rates?"
            )
        }
    )
    print(result["output"])


if __name__ == "__main__":
    main()

