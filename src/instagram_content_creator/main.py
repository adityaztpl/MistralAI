"""CLI entrypoint for the Instagram content creator crew."""

from __future__ import annotations

import argparse
import datetime
from pathlib import Path

from dotenv import load_dotenv

from instagram_content_creator.crew import InstagramContentCreatorCrew


def parse_args() -> argparse.Namespace:
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
    return parser.parse_args()


def run() -> None:
    load_dotenv()
    args = parse_args()

    Path("outputs").mkdir(parents=True, exist_ok=True)

    inputs = {
        "current_date": datetime.datetime.now().strftime("%Y-%m-%d"),
        "instagram_description": args.description,
        "topic_of_the_week": args.topic,
        "num_posts": str(args.posts),
    }

    print("Starting Instagram Content Creator crew (Mistral AI)...")
    print(f"  Account: {args.description[:80]}{'...' if len(args.description) > 80 else ''}")
    print(f"  Topic:   {args.topic}")
    print(f"  Posts:   {args.posts}")
    print()

    result = InstagramContentCreatorCrew().crew().kickoff(inputs=inputs)

    print("\nDone. Outputs written to ./outputs/")
    print("  - market_research.md")
    print("  - content_strategy.md")
    print("  - visual_content.md")
    print("  - captions.md")
    print("  - final_content_pack.md")
    print("\nFinal pack preview:\n")
    print(result)


if __name__ == "__main__":
    run()
