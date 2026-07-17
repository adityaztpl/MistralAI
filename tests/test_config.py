"""Tests that do not require a live Mistral API key."""

from pathlib import Path

import yaml

ROOT = Path(__file__).resolve().parents[1]
CONFIG = ROOT / "src" / "instagram_content_creator" / "config"


def test_agents_yaml_has_expected_roles():
    data = yaml.safe_load((CONFIG / "agents.yaml").read_text())
    assert set(data) == {
        "market_researcher",
        "content_strategist",
        "visual_creator",
        "copywriter",
    }
    for agent in data.values():
        assert agent["role"]
        assert "\n" not in agent["role"]
        assert agent["goal"]
        assert agent["backstory"]


def test_tasks_yaml_pipeline_order():
    data = yaml.safe_load((CONFIG / "tasks.yaml").read_text())
    assert list(data.keys()) == [
        "market_research",
        "content_strategy",
        "visual_content_creation",
        "copywriting",
        "final_content_pack",
    ]
    assert data["market_research"]["agent"] == "market_researcher"
    assert data["content_strategy"]["context"] == ["market_research"]
    assert data["visual_content_creation"]["context"] == ["content_strategy"]
    assert data["copywriting"]["context"] == [
        "content_strategy",
        "visual_content_creation",
    ]
    assert "{num_posts}" in data["content_strategy"]["description"]


def test_resolve_mistral_api_key_prefers_primary(monkeypatch):
    monkeypatch.setenv("MISTRAL_API_KEY", "primary")
    monkeypatch.setenv("MISTRAL_AI_KEY", "alias")
    from instagram_content_creator.crew import resolve_mistral_api_key

    assert resolve_mistral_api_key() == "primary"


def test_resolve_mistral_api_key_falls_back_to_alias(monkeypatch):
    monkeypatch.delenv("MISTRAL_API_KEY", raising=False)
    monkeypatch.setenv("MISTRAL_AI_KEY", "alias")
    from instagram_content_creator.crew import resolve_mistral_api_key

    assert resolve_mistral_api_key() == "alias"


def test_build_mistral_llm_requires_api_key(monkeypatch):
    monkeypatch.delenv("MISTRAL_API_KEY", raising=False)
    monkeypatch.delenv("MISTRAL_AI_KEY", raising=False)
    from instagram_content_creator.crew import build_mistral_llm

    try:
        build_mistral_llm()
        assert False, "expected EnvironmentError"
    except EnvironmentError as exc:
        assert "Mistral API key" in str(exc)


def test_build_mistral_llm_uses_env(monkeypatch):
    monkeypatch.setenv("MISTRAL_API_KEY", "test-key")
    monkeypatch.setenv("MISTRAL_MODEL", "mistral/mistral-small-latest")
    monkeypatch.setenv("MISTRAL_TEMPERATURE", "0.2")

    from instagram_content_creator.crew import build_mistral_llm

    llm = build_mistral_llm()
    assert llm.model == "mistral/mistral-small-latest"
    assert llm.temperature == 0.2


def test_cli_rejects_invalid_post_count(monkeypatch):
    monkeypatch.setenv("MISTRAL_API_KEY", "test-key")
    from instagram_content_creator.main import run

    code = run(
        [
            "-d",
            "brand",
            "-t",
            "topic",
            "-n",
            "0",
        ]
    )
    assert code == 2


def test_cli_requires_api_key(monkeypatch):
    monkeypatch.delenv("MISTRAL_API_KEY", raising=False)
    monkeypatch.delenv("MISTRAL_AI_KEY", raising=False)
    from instagram_content_creator.main import run

    code = run(["-d", "brand", "-t", "topic"])
    assert code == 2


def test_crew_uses_templated_output_paths(monkeypatch, tmp_path):
    monkeypatch.setenv("MISTRAL_API_KEY", "test-key")
    from instagram_content_creator.crew import InstagramContentCreatorCrew

    creator = InstagramContentCreatorCrew(output_dir=tmp_path)
    crew = creator.crew()
    assert creator.output_dir == str(tmp_path).rstrip("/")
    assert [t.output_file for t in crew.tasks] == [
        "{output_dir}/market_research.md",
        "{output_dir}/content_strategy.md",
        "{output_dir}/visual_content.md",
        "{output_dir}/captions.md",
        "{output_dir}/final_content_pack.md",
    ]
