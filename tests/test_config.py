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
        assert agent["llm"] == "mistral/mistral-large-latest"
        assert agent["role"]
        assert agent["goal"]
        assert agent["backstory"]


def test_tasks_yaml_pipeline_order_and_outputs():
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
    assert data["final_content_pack"]["output_file"] == "outputs/final_content_pack.md"


def test_build_mistral_llm_requires_api_key(monkeypatch):
    monkeypatch.delenv("MISTRAL_API_KEY", raising=False)
    from instagram_content_creator.crew import build_mistral_llm

    try:
        build_mistral_llm()
        assert False, "expected EnvironmentError"
    except EnvironmentError as exc:
        assert "MISTRAL_API_KEY" in str(exc)


def test_build_mistral_llm_uses_env(monkeypatch):
    monkeypatch.setenv("MISTRAL_API_KEY", "test-key")
    monkeypatch.setenv("MISTRAL_MODEL", "mistral/mistral-small-latest")
    monkeypatch.setenv("MISTRAL_TEMPERATURE", "0.2")

    from instagram_content_creator.crew import build_mistral_llm

    llm = build_mistral_llm()
    assert llm.model == "mistral/mistral-small-latest"
    assert llm.temperature == 0.2
