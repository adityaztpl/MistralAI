"""CLI entrypoint for the Instagram content creator crew."""

from __future__ import annotations

import argparse
import datetime
import os
import sys
from pathlib import Path

from dotenv import load_dotenv

from instagram_content_creator.crew import (
    DEFAULT_MODEL,
    InstagramContentCreatorCrew,
    resolve_mistral_api_key,
)

OUTPUT_FILES = (
    "market_research.md",
    "content_strategy.md",
    "visual_content.md",
    "captions.md",
    "final_content_pack.md",
)


def parse_args(argv: list[str] | None = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Generate Instagram content with CrewAI + Mistral AI",
    )
    parser.add_argument(
        "--description",
        "-d",
        required=True,
        help="Brand / Instagram account description",
    )
    parser.add_argument(
        "--topic",
        "-t",
        required=True,
        help="Topic or campaign focus for this week",
    )
    parser.add_argument(
        "--posts",
        "-n",
        type=int,
        default=3,
        help="Number of posts to plan (default: 3)",
    )
    parser.add_argument(
        "--model",
        "-m",
        default=None,
        help=f"Mistral model id (default: {DEFAULT_MODEL} or MISTRAL_MODEL)",
    )
    parser.add_argument(
        "--output-dir",
        "-o",
        default="outputs",
        help="Directory for generated markdown files (default: outputs)",
    )
    parser.add_argument(
        "--temperature",
        type=float,
        default=None,
        help="Sampling temperature (default: 0.7 or MISTRAL_TEMPERATURE)",
    )
    return parser.parse_args(argv)


def run(argv: list[str] | None = None) -> int:
    load_dotenv()
    os.environ.setdefault("CREWAI_TRACING_ENABLED", "false")
    try:
        from crewai.events.listeners.tracing.utils import (
            set_suppress_tracing_messages,
        )

        set_suppress_tracing_messages(True)
    except Exception:  # noqa: BLE001 - optional CrewAI helper
        pass
    args = parse_args(argv)

    if args.posts < 1 or args.posts > 14:
        print("error: --posts must be between 1 and 14", file=sys.stderr)
        return 2

    output_dir = Path(args.output_dir)
    if ".." in output_dir.parts:
        print("error: --output-dir must not contain '..'", file=sys.stderr)
        return 2

    if not resolve_mistral_api_key():
        print(
            "error: Mistral API key not found. Set MISTRAL_API_KEY "
            "(or MISTRAL_AI_KEY) in .env — see .env.example.",
            file=sys.stderr,
        )
        return 2

    output_dir.mkdir(parents=True, exist_ok=True)

    inputs = {
        "current_date": datetime.datetime.now().strftime("%Y-%m-%d"),
        "instagram_description": args.description.strip(),
        "topic_of_the_week": args.topic.strip(),
        "num_posts": str(args.posts),
        "output_dir": str(output_dir).rstrip("/"),
    }

    model = args.model or None
    print("Starting Instagram Content Creator crew (Mistral AI)...")
    print(
        f"  Account: {args.description[:80]}"
        f"{'...' if len(args.description) > 80 else ''}"
    )
    print(f"  Topic:   {args.topic}")
    print(f"  Posts:   {args.posts}")
    print(f"  Model:   {model or 'env/default'}")
    print(f"  Output:  {output_dir.resolve()}")
    print()

    try:
        result = (
            InstagramContentCreatorCrew(
                model=model,
                temperature=args.temperature,
                output_dir=output_dir,
            )
            .crew()
            .kickoff(inputs=inputs)
        )
    except Exception as exc:  # noqa: BLE001 - surface clean CLI errors
        print(f"error: crew failed: {exc}", file=sys.stderr)
        return 1

    missing = [name for name in OUTPUT_FILES if not (output_dir / name).is_file()]
    print(f"\nDone. Outputs written to {output_dir.resolve()}/")
    for name in OUTPUT_FILES:
        path = output_dir / name
        status = "ok" if path.is_file() else "MISSING"
        print(f"  [{status}] {name}")

    if missing:
        print(
            f"warning: expected files were not written: {', '.join(missing)}",
            file=sys.stderr,
        )

    print("\nFinal pack preview:\n")
    print(result)
    return 0 if not missing else 1


if __name__ == "__main__":
    raise SystemExit(run())
