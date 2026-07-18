"""Teaching sketch for 16-capstone-instagram.

Deterministic evaluation helpers for Instagram content runs. This can be used
after the CrewAI artifacts are written, then extended with an LLM-as-judge step
if needed.
"""

from __future__ import annotations

import json
import re
from dataclasses import asdict, dataclass, field
from pathlib import Path


EXPECTED_ARTIFACTS = {
    "market_research": "market_research.md",
    "content_strategy": "content_strategy.md",
    "visual_content": "visual_content.md",
    "captions": "captions.md",
    "final_pack": "final_content_pack.md",
}

WEIGHTS = {
    "brand_alignment": 0.20,
    "topic_relevance": 0.15,
    "caption_quality": 0.20,
    "hashtag_quality": 0.10,
    "visual_usefulness": 0.10,
    "strategy_coherence": 0.10,
    "safety_compliance": 0.10,
    "completeness": 0.05,
}

BANNED_CLAIM_PATTERNS = [
    re.compile(r"\bguaranteed\b", re.IGNORECASE),
    re.compile(r"\bcure(s|d)?\b", re.IGNORECASE),
    re.compile(r"\brisk[- ]free\b", re.IGNORECASE),
]


@dataclass(frozen=True)
class RunInputs:
    brand_description: str
    topic: str
    number_of_posts: int
    industry: str | None = None


@dataclass
class EvalScore:
    brand_alignment: int = 3
    topic_relevance: int = 3
    caption_quality: int = 3
    hashtag_quality: int = 3
    visual_usefulness: int = 3
    strategy_coherence: int = 3
    safety_compliance: int = 5
    completeness: int = 1
    weighted_score: float = 0.0
    warnings: list[str] = field(default_factory=list)
    rationale: list[str] = field(default_factory=list)

    def finalize(self) -> "EvalScore":
        weighted = 0.0
        for name, weight in WEIGHTS.items():
            weighted += getattr(self, name) * weight

        # Convert 1-5 scale to 0-100.
        self.weighted_score = round((weighted / 5.0) * 100, 2)
        return self


def score_run(output_dir: Path, inputs: RunInputs) -> EvalScore:
    artifacts = _load_artifacts(output_dir)
    score = EvalScore()

    _score_completeness(score, artifacts, inputs)
    _score_topic_relevance(score, artifacts, inputs)
    _score_caption_quality(score, artifacts, inputs)
    _score_hashtags(score, artifacts)
    _score_visuals(score, artifacts)
    _score_strategy(score, artifacts)
    _score_safety(score, artifacts)
    _score_brand_alignment(score, artifacts, inputs)

    return score.finalize()


def write_score(output_dir: Path, inputs: RunInputs) -> Path:
    score = score_run(output_dir, inputs)
    path = output_dir / "eval_score.json"
    path.write_text(json.dumps(asdict(score), indent=2), encoding="utf-8")
    return path


def _load_artifacts(output_dir: Path) -> dict[str, str]:
    artifacts: dict[str, str] = {}
    for kind, filename in EXPECTED_ARTIFACTS.items():
        path = output_dir / filename
        if path.exists():
            artifacts[kind] = path.read_text(encoding="utf-8")
    return artifacts


def _score_completeness(
    score: EvalScore,
    artifacts: dict[str, str],
    inputs: RunInputs,
) -> None:
    missing = [kind for kind in EXPECTED_ARTIFACTS if kind not in artifacts]
    if missing:
        score.warnings.append(f"Missing artifacts: {', '.join(missing)}")

    final_pack = artifacts.get("final_pack", "")
    post_mentions = len(re.findall(r"\bpost\s+\d+\b", final_pack, re.IGNORECASE))

    if not missing and post_mentions >= inputs.number_of_posts:
        score.completeness = 5
        score.rationale.append("All expected artifacts exist and requested posts are represented.")
    elif len(missing) <= 1:
        score.completeness = 3
        score.rationale.append("Most artifacts exist, but output completeness needs review.")
    else:
        score.completeness = 1
        score.rationale.append("Multiple expected artifacts are missing.")


def _score_topic_relevance(
    score: EvalScore,
    artifacts: dict[str, str],
    inputs: RunInputs,
) -> None:
    all_text = _all_text(artifacts)
    topic_terms = _keywords(inputs.topic)
    hits = sum(1 for term in topic_terms if term in all_text.lower())

    if hits >= min(3, len(topic_terms)):
        score.topic_relevance = 5
    elif hits >= 1:
        score.topic_relevance = 3
        score.warnings.append("Topic appears only weakly in generated artifacts.")
    else:
        score.topic_relevance = 1
        score.warnings.append("Generated artifacts do not appear topic-relevant.")


def _score_caption_quality(score: EvalScore, artifacts: dict[str, str]) -> None:
    captions = artifacts.get("captions", "")
    if not captions.strip():
        score.caption_quality = 1
        score.warnings.append("Captions artifact is empty.")
        return

    has_cta = bool(re.search(r"\b(comment|save|share|follow|try|visit|learn)\b", captions, re.IGNORECASE))
    average_line_length = _average_non_empty_line_length(captions)

    if has_cta and 40 <= average_line_length <= 240:
        score.caption_quality = 5
    elif has_cta:
        score.caption_quality = 4
    else:
        score.caption_quality = 3
        score.warnings.append("Captions may be missing clear calls to action.")


def _score_hashtags(score: EvalScore, artifacts: dict[str, str]) -> None:
    captions = artifacts.get("captions", "")
    hashtags = re.findall(r"#[A-Za-z0-9_]+", captions)
    unique = set(tag.lower() for tag in hashtags)

    if not hashtags:
        score.hashtag_quality = 1
        score.warnings.append("No hashtags found.")
    elif len(unique) < len(hashtags) * 0.6:
        score.hashtag_quality = 2
        score.warnings.append("Hashtags are repetitive.")
    elif 5 <= len(unique) <= 40:
        score.hashtag_quality = 5
    else:
        score.hashtag_quality = 3
        score.warnings.append("Hashtag count may need human review.")


def _score_visuals(score: EvalScore, artifacts: dict[str, str]) -> None:
    visuals = artifacts.get("visual_content", "")
    useful_terms = ["composition", "lighting", "palette", "shot", "reel", "image", "visual"]
    hits = sum(1 for term in useful_terms if term in visuals.lower())

    if hits >= 4:
        score.visual_usefulness = 5
    elif hits >= 2:
        score.visual_usefulness = 3
    else:
        score.visual_usefulness = 2
        score.warnings.append("Visual concepts may not be actionable enough.")


def _score_strategy(score: EvalScore, artifacts: dict[str, str]) -> None:
    strategy = artifacts.get("content_strategy", "")
    strategy_terms = ["goal", "audience", "calendar", "theme", "format", "cta"]
    hits = sum(1 for term in strategy_terms if term in strategy.lower())

    if hits >= 5:
        score.strategy_coherence = 5
    elif hits >= 3:
        score.strategy_coherence = 3
    else:
        score.strategy_coherence = 2
        score.warnings.append("Content strategy appears thin.")


def _score_safety(score: EvalScore, artifacts: dict[str, str]) -> None:
    all_text = _all_text(artifacts)
    matches = [
        pattern.pattern
        for pattern in BANNED_CLAIM_PATTERNS
        if pattern.search(all_text)
    ]

    if matches:
        score.safety_compliance = 2
        score.warnings.append("Potentially risky claims detected; human review required.")
    else:
        score.safety_compliance = 5


def _score_brand_alignment(
    score: EvalScore,
    artifacts: dict[str, str],
    inputs: RunInputs,
) -> None:
    all_text = _all_text(artifacts)
    brand_terms = _keywords(inputs.brand_description)
    hits = sum(1 for term in brand_terms[:10] if term in all_text.lower())

    if hits >= 5:
        score.brand_alignment = 5
    elif hits >= 2:
        score.brand_alignment = 3
    else:
        score.brand_alignment = 2
        score.warnings.append("Generated content may not reflect the brand description.")


def _keywords(text: str) -> list[str]:
    stop_words = {
        "the",
        "and",
        "for",
        "with",
        "that",
        "this",
        "from",
        "your",
        "you",
        "are",
        "who",
        "care",
    }
    words = re.findall(r"[A-Za-z][A-Za-z0-9-]{2,}", text.lower())
    return [word for word in words if word not in stop_words]


def _all_text(artifacts: dict[str, str]) -> str:
    return "\n\n".join(artifacts.values())


def _average_non_empty_line_length(text: str) -> float:
    lines = [line.strip() for line in text.splitlines() if line.strip()]
    if not lines:
        return 0
    return sum(len(line) for line in lines) / len(lines)


if __name__ == "__main__":
    demo_dir = Path("outputs/demo")
    demo_inputs = RunInputs(
        brand_description="A specialty coffee brand for remote workers who care about ritual and quality",
        topic="Morning brew routines for deep work",
        number_of_posts=3,
    )
    print(write_score(demo_dir, demo_inputs))

