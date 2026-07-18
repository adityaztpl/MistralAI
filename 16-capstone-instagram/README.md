# 16-capstone-instagram: Full-Stack Instagram Content Creator Capstone

This capstone turns the existing `src/instagram_content_creator` CrewAI + Mistral package into a full-stack product-prep project:

- ASP.NET Core API facade for authenticated users and tenants.
- Background job wrapper around the Python crew.
- React or Angular dashboard for brand/topic runs and history.
- Evaluation loop for captions, hashtags, visual briefs, and human feedback.
- Auth, tenancy, cost controls, deployment, and observability.

Use this capstone as a portfolio project and interview discussion anchor. The goal is not only "generate captions"; the goal is to show you can wrap an AI workflow in a safe, observable, multi-user application.

## Existing asset

The repository already contains a runnable Python app:

- `src/instagram_content_creator/crew.py` - CrewAI crew definition.
- `src/instagram_content_creator/main.py` - CLI entry point.
- `src/instagram_content_creator/config/agents.yaml` - agent roles.
- `src/instagram_content_creator/config/tasks.yaml` - task prompts and output files.
- `projects/instagram-content-creator.md` - standalone app setup and usage.

The capstone keeps that package as the AI engine and adds a product shell around it.

## Capstone goals

By the end, you should have a design you can explain and, if desired, implement:

- Create content-generation runs from a web UI.
- Persist run metadata, status, inputs, output file paths, scores, and feedback.
- Execute CrewAI/Mistral work asynchronously.
- Show progress and history in a dashboard.
- Evaluate content quality with a rubric.
- Capture human feedback and feed it back into future prompts.
- Enforce authentication, tenant isolation, rate limits, and cost budgets.
- Deploy the API, worker, SPA, database, and Python runtime cleanly.

## Target architecture

```text
React/Angular SPA
   |
   | JWT + JSON/SSE
   v
ASP.NET Core API
   |       \
   |        \ enqueue
   |         v
Postgres   Background worker
   |         |
   |         | starts Python crew
   |         v
   |      src/instagram_content_creator
   |         |
   |         v
   |      Mistral API
   |
   v
Object storage / run output files
```

For more detailed diagrams, see [`architecture.md`](architecture.md).

## Module order

| Order | Guide | Outcome |
| --- | --- | --- |
| 1 | [Wrap with ASP.NET](01-wrap-with-aspnet.md) | API facade, DTOs, run persistence, worker boundary |
| 2 | [SPA dashboard](02-spa-dashboard.md) | Create-run form, run list, detail view, feedback UX |
| 3 | [Eval metrics](03-eval-metrics.md) | Quality rubric, automated scoring sketch, human feedback loop |
| 4 | [Auth, tenancy, cost](04-auth-tenancy-cost.md) | Tenant isolation, rate limits, budgets, model/provider controls |
| 5 | [Deployment](05-deployment.md) | Local compose, cloud topology, CI/CD, observability |
| 6 | [Architecture](architecture.md) | Mermaid diagrams for interviews and README |

## Code sketches

These are intentionally realistic sketches, not drop-in production code:

- [`examples/ContentRunsController.cs`](examples/ContentRunsController.cs) - ASP.NET API endpoints.
- [`examples/RunJobService.cs`](examples/RunJobService.cs) - queued background worker around the Python crew.
- [`examples/Dashboard.tsx`](examples/Dashboard.tsx) - React dashboard shape.
- [`examples/eval_scores.py`](examples/eval_scores.py) - rubric scoring and feedback aggregation.

## Milestones

### Milestone 1 - Product shell

Deliver:

- ASP.NET project with `/api/content-runs`.
- In-memory or SQLite/Postgres run store.
- Run create/list/detail endpoints.
- Stub worker that marks a run complete.
- SPA page that creates and lists runs.

Interview story:

- "I separated request/response API work from long-running AI generation."
- "The API returns a run id immediately and the worker updates status."

### Milestone 2 - Real CrewAI execution

Deliver:

- Worker invokes the Python package or CLI.
- Per-run output directory.
- Captures stdout/stderr safely.
- Stores output artifact paths.
- Handles failure and cancellation.
- Uses server-side `MISTRAL_API_KEY`.

Interview story:

- "The LLM provider key stays server-side."
- "The user never waits on an HTTP request for a multi-agent run."

### Milestone 3 - Dashboard and history

Deliver:

- Create-run form for brand, topic, number of posts, tone, audience.
- Run history with status, created date, cost estimate, and score.
- Detail page with generated sections.
- Copy/export buttons.
- Human feedback controls.

Interview story:

- "I designed UX around asynchronous status and review rather than pretending AI output is instant."

### Milestone 4 - Evaluation loop

Deliver:

- Rubric for captions, hashtags, brand voice, compliance, and usefulness.
- Python scoring script or service.
- Human thumbs up/down and notes.
- Prompt/version metadata on every run.
- Score dashboard.

Interview story:

- "I measured output quality and separated model failures from product failures."

### Milestone 5 - Multi-tenant hardening

Deliver:

- JWT auth.
- Tenant-scoped queries.
- Per-tenant run quotas.
- Cost budget and provider usage tracking.
- Audit logs.
- Secret manager integration.

Interview story:

- "I treated AI generation as a billable, tenant-scoped capability that needs policy."

### Milestone 6 - Deployment-ready

Deliver:

- Dockerfile(s).
- API + worker + SPA deployment plan.
- Database migrations.
- Health checks.
- Structured logs and traces.
- Runbook for failed jobs and key rotation.

Interview story:

- "I can explain how the system operates after the demo works."

## Suggested data model

| Table | Purpose |
| --- | --- |
| `Tenants` | Tenant metadata and budget settings |
| `Users` | App users mapped to auth provider subject |
| `ContentRuns` | Inputs, status, ownership, timestamps |
| `RunArtifacts` | Output file metadata and storage locations |
| `RunEvents` | Status changes and worker/audit events |
| `EvalScores` | Automated rubric scores |
| `HumanFeedback` | Reviewer ratings and notes |
| `UsageRecords` | Model calls, tokens, estimated cost |

## Status lifecycle

```text
Queued -> Running -> Succeeded
                 \-> Failed
Queued/Running -> Cancelled
```

Store status transitions as events. The current status on `ContentRuns` is a projection for fast UI reads.

## Interview checklists

### Backend

- Explain why the API enqueues a run instead of blocking.
- Explain how you pass inputs to Python safely.
- Explain how you avoid path traversal in output directories.
- Explain idempotency for create-run requests.
- Explain cancellation and retries.

### Frontend

- Explain loading, empty, error, and partial-complete states.
- Explain polling vs SSE vs WebSocket.
- Explain how feedback updates the UI optimistically or pessimistically.
- Explain accessibility for generated content review.

### GenAI

- Explain model choice and temperature.
- Explain prompt versioning.
- Explain eval metrics and human feedback.
- Explain hallucination or brand-safety controls.
- Explain how prompt injection could affect generated content if web retrieval is added.

### Production

- Explain tenant isolation.
- Explain cost controls.
- Explain secrets.
- Explain deployment topology.
- Explain observability and incident response.

## Definition of done

- [ ] API can create/list/detail content runs.
- [ ] Worker runs the existing CrewAI/Mistral engine asynchronously.
- [ ] SPA can submit inputs and inspect outputs.
- [ ] Outputs are persisted as artifacts with metadata.
- [ ] Automated eval scores are stored.
- [ ] Human feedback is captured.
- [ ] Auth and tenant scopes are enforced.
- [ ] Provider keys are server-side only.
- [ ] Cost and token usage are tracked.
- [ ] Architecture diagrams and README are interview-ready.

