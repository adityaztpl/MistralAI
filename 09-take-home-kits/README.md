# Take-Home Interview Kits

This section turns the curriculum into realistic take-home practice. Each kit is designed to simulate what a senior full-stack or GenAI interview loop may ask you to build: a small but complete product slice, a readable implementation, and a concise write-up that explains trade-offs.

## How to use these kits

1. Pick the kit that matches your available time.
2. Read the full brief once before writing code.
3. Create a tiny design note before implementation.
4. Timebox ruthlessly. The goal is a shippable slice, not a perfect platform.
5. Finish with the submission checklist and rubric.
6. Do a self-review as if you were the hiring panel.

## Kits

| Kit | Timebox | Best for | Main signals |
|---|---:|---|---|
| [2h Notes API](02h-notes-api.md) | 2 hours | Backend fundamentals under pressure | API design, validation, persistence, tests, pragmatic scope |
| [8h RAG Chat](08h-rag-chat.md) | 8 hours | Full-stack GenAI project | Retrieval quality, streaming UX, citations, auth boundaries, observability |
| [Weekend Support Agent](weekend-support-agent.md) | 2-3 days | Agentic + HITL systems | Workflow orchestration, tool safety, human approval, auditability, production judgment |

## Evaluation mindset

Most take-homes are not judged by feature count alone. Reviewers usually ask:

- Can I run it quickly?
- Is the core path correct and understandable?
- Did the candidate make good trade-offs for the timebox?
- Are failure modes handled thoughtfully?
- Does the write-up explain what was intentionally left out?
- Would I trust this person to extend the system with the team?

## Suggested repository structure

Use a structure like this for any kit unless the prompt specifies otherwise:

```text
project-root/
  README.md
  docs/
    design.md
    decisions.md
  src/
  tests/
  docker-compose.yml        # when useful
  .env.example
```

For .NET + SPA projects:

```text
project-root/
  api/
    src/
    tests/
  web/
    src/
    tests/
  docs/
  docker-compose.yml
  README.md
```

For Python RAG services:

```text
project-root/
  app/
    api/
    ingestion/
    retrieval/
    generation/
  tests/
  data/sample/
  evals/
  README.md
```

## Default tech stack assumptions

You can adapt, but these stacks are interview-friendly:

- Backend: ASP.NET Core Minimal APIs or controllers, EF Core, PostgreSQL or SQLite for small kits.
- Frontend: React + Vite or Angular standalone components.
- RAG: Python FastAPI or ASP.NET Core + Semantic Kernel; pgvector, Qdrant, Chroma, or simple in-memory vector search for prototypes.
- Queues/jobs: Hangfire, .NET Worker Service, RabbitMQ, Azure Service Bus concepts.
- Tests: xUnit/NUnit for .NET, pytest for Python, Vitest/Jest for frontend.

## Deliverable checklist quick view

- One command or a short sequence runs the project.
- `.env.example` documents every required variable.
- Seed/sample data is included.
- Happy path is tested.
- At least one validation/error case is tested.
- README includes assumptions, trade-offs, and next steps.
- No secrets, generated binaries, or large vendor files are committed.

See [submission-checklist.md](submission-checklist.md) for the full checklist.

## Rubrics

- [2h rubric](rubrics/02h-rubric.md)
- [8h rubric](rubrics/08h-rubric.md)
- [Weekend rubric](rubrics/weekend-rubric.md)

## How to talk about a take-home in the follow-up interview

Use this structure:

```text
Problem -> Constraints -> Architecture -> Trade-offs -> Failure modes -> Next iteration
```

Example:

> The prompt asked for a small RAG chat app. I optimized for retrieval correctness and runnable setup over visual polish. The backend owns ingestion, retrieval, prompt construction, and citation validation. The UI supports streaming and source inspection. I used a small evaluation set to catch wrong-document retrieval. With more time I would add tenant-scoped auth, background ingestion, and tracing across retrieval and generation.
