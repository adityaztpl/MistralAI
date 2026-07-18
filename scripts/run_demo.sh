#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."

if [[ -z "${MISTRAL_API_KEY:-}${MISTRAL_AI_KEY:-}" ]]; then
  echo "Set MISTRAL_API_KEY (or MISTRAL_AI_KEY) first." >&2
  exit 2
fi

OUT_DIR="${1:-outputs/demo}"
MODEL="${MODEL:-mistral/mistral-small-latest}"

instagram-content \
  -d "Lumen Brew — specialty coffee for remote workers who care about ritual and focus" \
  -t "Morning brew routines that unlock deep work" \
  -n "${POSTS:-2}" \
  --model "$MODEL" \
  --output-dir "$OUT_DIR"

echo "Wrote content pack to $OUT_DIR/final_content_pack.md"
