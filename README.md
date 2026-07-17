# Instagram Content Creator

Multi-agent Instagram content generator built with **CrewAI** and **Mistral AI**
(via Mistral's OpenAI-compatible API).

Give it a brand description and weekly topic — a crew of four agents researches trends, plans a content calendar, designs visual concepts, and writes captions with hashtags.

## Agents

| Agent | Job |
| --- | --- |
| Market Researcher | Trends, formats, audience angles, hashtag clusters |
| Content Strategist | Weekly calendar with themes, formats, and goals |
| Visual Creator | Prompt-ready image/reel concepts per post |
| Copywriter | Hooks, captions, CTAs, hashtags, alt text |

## Setup

```bash
python -m venv .venv
source .venv/bin/activate
pip install -e ".[dev]"
cp .env.example .env
```

Add your Mistral API key to `.env` (either variable name works):

```
MISTRAL_API_KEY=your_mistral_api_key
# or: MISTRAL_AI_KEY=your_mistral_api_key
```

Get a key at [console.mistral.ai](https://console.mistral.ai/).

## Run

```bash
instagram-content \
  --description "A specialty coffee brand for remote workers who care about ritual and quality" \
  --topic "Morning brew routines for deep work" \
  --posts 3
```

Useful flags:

```bash
instagram-content -d "..." -t "..." -n 2 \
  --model mistral/mistral-small-latest \
  --output-dir outputs/demo
```

Or:

```bash
python -m instagram_content_creator.main \
  -d "A specialty coffee brand for remote workers" \
  -t "Morning brew routines for deep work" \
  -n 3
```

## Outputs

Files are written to `outputs/`:

- `market_research.md` — research brief
- `content_strategy.md` — content calendar
- `visual_content.md` — visual concepts / image prompts
- `captions.md` — captions, CTAs, hashtags
- `final_content_pack.md` — combined ready-to-publish pack

## Configuration

| Variable | Default | Description |
| --- | --- | --- |
| `MISTRAL_API_KEY` | (required*) | Mistral API key |
| `MISTRAL_AI_KEY` | (alias) | Accepted if `MISTRAL_API_KEY` is unset |
| `MISTRAL_MODEL` | `mistral/mistral-large-latest` | LiteLLM model id |
| `MISTRAL_TEMPERATURE` | `0.7` | Creativity |
| `MISTRAL_MAX_TOKENS` | `4096` | Max response tokens |

Agents and tasks live in:

- `src/instagram_content_creator/config/agents.yaml`
- `src/instagram_content_creator/config/tasks.yaml`

## Sample output

See [`examples/sample_run/`](examples/sample_run/) for a real generated content pack.

## Tests

```bash
pytest
```
