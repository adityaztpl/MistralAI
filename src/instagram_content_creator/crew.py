"""Crew definition for the Instagram content creator."""

from __future__ import annotations

import os
from pathlib import Path

from crewai import Agent, Crew, LLM, Process, Task
from crewai.project import CrewBase, agent, crew, task
from dotenv import load_dotenv

load_dotenv()

DEFAULT_MODEL = "mistral/mistral-large-latest"
MISTRAL_BASE_URL = "https://api.mistral.ai/v1"
OUTPUT_DIR = Path(os.getenv("OUTPUT_DIR", "outputs"))


def resolve_mistral_api_key() -> str | None:
    """Prefer MISTRAL_API_KEY; accept MISTRAL_AI_KEY as an alias."""
    return os.getenv("MISTRAL_API_KEY") or os.getenv("MISTRAL_AI_KEY")


def normalize_mistral_model(model: str) -> tuple[str, str]:
    """Return (crewai model id, bare mistral model name).

    CrewAI's native ``mistral/`` LiteLLM route injects ``cache_breakpoint``
    fields that the Mistral API rejects. We call Mistral through its
    OpenAI-compatible endpoint instead.
    """
    bare = model.split("/", 1)[-1]
    return f"openai/{bare}", bare


def build_mistral_llm(
    *,
    model: str | None = None,
    temperature: float | None = None,
    max_tokens: int | None = None,
) -> LLM:
    """Configure Mistral via its OpenAI-compatible API."""
    api_key = resolve_mistral_api_key()
    if not api_key:
        raise EnvironmentError(
            "Mistral API key not found. Set MISTRAL_API_KEY (or MISTRAL_AI_KEY) "
            "in your environment or .env file. See .env.example."
        )

    requested = model or os.getenv("MISTRAL_MODEL", DEFAULT_MODEL)
    crewai_model, _bare = normalize_mistral_model(requested)

    return LLM(
        model=crewai_model,
        api_key=api_key,
        base_url=os.getenv("MISTRAL_BASE_URL", MISTRAL_BASE_URL),
        temperature=(
            temperature
            if temperature is not None
            else float(os.getenv("MISTRAL_TEMPERATURE", "0.7"))
        ),
        max_tokens=(
            max_tokens
            if max_tokens is not None
            else int(os.getenv("MISTRAL_MAX_TOKENS", "4096"))
        ),
    )


@CrewBase
class InstagramContentCreatorCrew:
    """Multi-agent crew that researches, plans, and writes Instagram content."""

    agents_config = "config/agents.yaml"
    tasks_config = "config/tasks.yaml"

    def __init__(
        self,
        *,
        model: str | None = None,
        temperature: float | None = None,
        max_tokens: int | None = None,
        output_dir: str | Path | None = None,
    ) -> None:
        # Keep as a string so kickoff can interpolate {output_dir} into task
        # output paths. CrewAI strips leading "/" from non-templated paths.
        self.output_dir = str(output_dir or OUTPUT_DIR).rstrip("/")
        Path(self.output_dir).mkdir(parents=True, exist_ok=True)
        self.llm = build_mistral_llm(
            model=model,
            temperature=temperature,
            max_tokens=max_tokens,
        )

    def _task_output(self, filename: str) -> str:
        return f"{{output_dir}}/{filename}"

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
            output_file=self._task_output("market_research.md"),
        )

    @task
    def content_strategy(self) -> Task:
        return Task(
            config=self.tasks_config["content_strategy"],  # type: ignore[index]
            output_file=self._task_output("content_strategy.md"),
        )

    @task
    def visual_content_creation(self) -> Task:
        return Task(
            config=self.tasks_config["visual_content_creation"],  # type: ignore[index]
            output_file=self._task_output("visual_content.md"),
        )

    @task
    def copywriting(self) -> Task:
        return Task(
            config=self.tasks_config["copywriting"],  # type: ignore[index]
            output_file=self._task_output("captions.md"),
        )

    @task
    def final_content_pack(self) -> Task:
        return Task(
            config=self.tasks_config["final_content_pack"],  # type: ignore[index]
            output_file=self._task_output("final_content_pack.md"),
        )

    @crew
    def crew(self) -> Crew:
        """Create the sequential Instagram content crew."""
        return Crew(
            agents=self.agents,
            tasks=self.tasks,
            process=Process.sequential,
            verbose=True,
            tracing=False,
        )
