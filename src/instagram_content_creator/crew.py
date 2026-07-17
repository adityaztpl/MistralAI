"""Crew definition for the Instagram content creator."""

from __future__ import annotations

import os
from pathlib import Path

from crewai import Agent, Crew, LLM, Process, Task
from crewai.project import CrewBase, agent, crew, task
from dotenv import load_dotenv

load_dotenv()

DEFAULT_MODEL = os.getenv("MISTRAL_MODEL", "mistral/mistral-large-latest")
OUTPUT_DIR = Path(os.getenv("OUTPUT_DIR", "outputs"))


def build_mistral_llm() -> LLM:
    """Configure Mistral via CrewAI's LiteLLM integration."""
    api_key = os.getenv("MISTRAL_API_KEY")
    if not api_key:
        raise EnvironmentError(
            "MISTRAL_API_KEY is not set. Copy .env.example to .env and add your key."
        )

    return LLM(
        model=os.getenv("MISTRAL_MODEL", DEFAULT_MODEL),
        api_key=api_key,
        temperature=float(os.getenv("MISTRAL_TEMPERATURE", "0.7")),
        max_tokens=int(os.getenv("MISTRAL_MAX_TOKENS", "4096")),
    )


@CrewBase
class InstagramContentCreatorCrew:
    """Multi-agent crew that researches, plans, and writes Instagram content."""

    agents_config = "config/agents.yaml"
    tasks_config = "config/tasks.yaml"

    def __init__(self) -> None:
        OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
        self.llm = build_mistral_llm()

    @agent
    def market_researcher(self) -> Agent:
        return Agent(
            config=self.agents_config["market_researcher"],  # type: ignore[index]
            llm=self.llm,
            verbose=True,
        )

    @agent
    def content_strategist(self) -> Agent:
        return Agent(
            config=self.agents_config["content_strategist"],  # type: ignore[index]
            llm=self.llm,
            verbose=True,
        )

    @agent
    def visual_creator(self) -> Agent:
        return Agent(
            config=self.agents_config["visual_creator"],  # type: ignore[index]
            llm=self.llm,
            verbose=True,
            allow_delegation=False,
        )

    @agent
    def copywriter(self) -> Agent:
        return Agent(
            config=self.agents_config["copywriter"],  # type: ignore[index]
            llm=self.llm,
            verbose=True,
            allow_delegation=False,
        )

    @task
    def market_research(self) -> Task:
        return Task(
            config=self.tasks_config["market_research"],  # type: ignore[index]
        )

    @task
    def content_strategy(self) -> Task:
        return Task(
            config=self.tasks_config["content_strategy"],  # type: ignore[index]
        )

    @task
    def visual_content_creation(self) -> Task:
        return Task(
            config=self.tasks_config["visual_content_creation"],  # type: ignore[index]
        )

    @task
    def copywriting(self) -> Task:
        return Task(
            config=self.tasks_config["copywriting"],  # type: ignore[index]
        )

    @task
    def final_content_pack(self) -> Task:
        return Task(
            config=self.tasks_config["final_content_pack"],  # type: ignore[index]
        )

    @crew
    def crew(self) -> Crew:
        """Create the sequential Instagram content crew."""
        return Crew(
            agents=self.agents,
            tasks=self.tasks,
            process=Process.sequential,
            verbose=True,
        )
