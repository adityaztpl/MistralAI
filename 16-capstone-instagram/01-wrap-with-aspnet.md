# 01 - Wrap the CrewAI/Mistral Crew with ASP.NET Core

The existing Python package is a command-line AI workflow. A full-stack capstone needs an API facade that turns that workflow into a multi-user product capability.

The most important design decision: **do not run the multi-agent crew inside the HTTP request thread**. Content generation can take seconds or minutes, may fail due to provider limits, and needs retries, cancellation, and audit events.

## Learning goals

- Design an API facade over a Python AI worker.
- Model asynchronous run lifecycle.
- Persist inputs, outputs, events, errors, and usage.
- Keep provider secrets server-side.
- Decide when to call Python as a process vs service.
- Explain reliability trade-offs in interviews.

## Target API surface

| Endpoint | Purpose |
| --- | --- |
| `POST /api/content-runs` | Create a run and enqueue work |
| `GET /api/content-runs` | List current tenant/user runs |
| `GET /api/content-runs/{id}` | Get run details and artifacts |
| `POST /api/content-runs/{id}/cancel` | Cancel queued/running work if supported |
| `POST /api/content-runs/{id}/feedback` | Capture human feedback |
| `GET /api/content-runs/{id}/events` | Poll or stream status events |

See [`examples/ContentRunsController.cs`](examples/ContentRunsController.cs) for a controller sketch.

## Request DTO

```json
{
  "brandDescription": "A specialty coffee brand for remote workers",
  "topic": "Morning brew routines for deep work",
  "numberOfPosts": 3,
  "tone": "warm, expert, concise",
  "audience": "remote workers and founders",
  "model": "mistral-large-latest"
}
```

Validation rules:

- `brandDescription`: required, 20-2000 chars.
- `topic`: required, 5-300 chars.
- `numberOfPosts`: 1-14.
- `tone`: optional, 0-200 chars.
- `audience`: optional, 0-300 chars.
- `model`: optional allowlisted model id.

Do not accept:

- tenant id from the body;
- user id from the body;
- API keys from the body;
- output directory paths from the body;
- arbitrary prompt templates from the body.

## Response DTO

```json
{
  "id": "run_01J...",
  "status": "Queued",
  "brandDescription": "...",
  "topic": "...",
  "numberOfPosts": 3,
  "createdAt": "2026-07-18T04:00:00Z",
  "links": {
    "self": "/api/content-runs/run_01J...",
    "events": "/api/content-runs/run_01J.../events"
  }
}
```

The API returns immediately. The dashboard polls or subscribes to updates.

## Persistence model

### `ContentRun`

| Field | Notes |
| --- | --- |
| `Id` | ULID/GUID generated server-side |
| `TenantId` | From validated caller context |
| `CreatedByUserId` | From validated caller context |
| `BrandDescription` | User input |
| `Topic` | User input |
| `NumberOfPosts` | Bounded integer |
| `Tone` | Optional user input |
| `Audience` | Optional user input |
| `Model` | Allowlisted model |
| `PromptVersion` | Version of agents/tasks/config |
| `Status` | Queued/Running/Succeeded/Failed/Cancelled |
| `OutputDirectory` | Server-generated path or object-storage prefix |
| `ErrorCode` | Safe error category |
| `ErrorMessage` | Redacted message for support |
| `CreatedAt`, `StartedAt`, `CompletedAt` | Lifecycle timestamps |
| `EstimatedCostCents` | Budget planning |

### `RunArtifact`

| Field | Notes |
| --- | --- |
| `RunId` | Parent |
| `Kind` | `market_research`, `content_strategy`, `visual_content`, `captions`, `final_pack` |
| `Path` | Object-storage key or safe local path |
| `ContentType` | Usually `text/markdown` |
| `SizeBytes` | Display and diagnostics |
| `CreatedAt` | Artifact timestamp |

### `RunEvent`

Store structured events:

- `RunQueued`;
- `RunStarted`;
- `CrewProcessLaunched`;
- `ArtifactWritten`;
- `EvalCompleted`;
- `RunFailed`;
- `RunCancelled`.

Events make debugging easier than a single mutable row.

## Calling Python: process vs service

### Option A - Launch Python process from worker

The ASP.NET worker runs:

```bash
python -m instagram_content_creator.main \
  --description "..." \
  --topic "..." \
  --posts 3 \
  --output-dir outputs/runs/<run-id>
```

Pros:

- Simple to implement.
- Reuses existing CLI.
- Easy local debugging.

Cons:

- Process management and timeouts are your responsibility.
- Need Python runtime and package installed with API/worker.
- Streaming progress is limited unless the CLI emits structured logs.

### Option B - Python worker service

ASP.NET enqueues a job; a Python service consumes it and runs CrewAI.

Pros:

- Natural dependency boundary.
- Python service owns CrewAI runtime.
- Easier to scale AI workers independently.

Cons:

- More infrastructure.
- Requires queue and service-to-service auth.
- More deployment pieces.

### Option C - HTTP call to internal Python API

ASP.NET calls a FastAPI service that wraps the crew.

Pros:

- Clear language boundary.
- Good if multiple products need the AI service.

Cons:

- Long-running HTTP calls still need async job design.
- Must secure internal network and auth.

For a prep capstone, Option A is a strong first milestone. Explain how you would evolve to Option B under load.

## Worker responsibilities

See [`examples/RunJobService.cs`](examples/RunJobService.cs).

The worker should:

- dequeue a run id;
- load the run and validate it is still queued;
- mark it running;
- create a server-owned output directory;
- launch Python with safe arguments;
- pass secrets through environment variables, not command-line args;
- apply timeout and cancellation;
- capture stdout/stderr with redaction;
- discover expected output files;
- store artifact records;
- run eval scoring;
- mark succeeded or failed;
- emit run events throughout.

## Safe process invocation

Rules:

- Use `ProcessStartInfo.ArgumentList`, not shell string concatenation.
- Do not let user input choose executable path.
- Do not let user input choose output directory.
- Keep API keys in environment variables.
- Enforce per-run timeout.
- Limit output log size.
- Redact secrets from logs.
- Use a low-privilege runtime identity.

## Idempotency

`POST /api/content-runs` can accidentally be double-submitted. Add one of:

- client-provided `Idempotency-Key` header scoped to user and tenant;
- server-side duplicate detection for same user/topic/time window;
- UI disabled state while submission is pending.

In interviews, mention that AI generation has cost, so accidental duplicate jobs matter.

## Cancellation

Cancellation layers:

- `Queued`: remove from queue or mark cancelled before worker starts.
- `Running`: signal process cancellation and kill after grace period.
- Provider call: pass cancellation where supported.
- UI: show "cancelling" and final "cancelled" or "failed".

Cancellation should update status and write an event.

## Error handling

Use safe categories:

| Error code | Meaning |
| --- | --- |
| `ValidationFailed` | Inputs rejected before enqueue |
| `BudgetExceeded` | Tenant/user budget prevents run |
| `ProviderAuthFailed` | Server-side provider key invalid |
| `ProviderRateLimited` | Model provider throttled |
| `CrewFailed` | CrewAI process returned non-zero |
| `TimedOut` | Run exceeded max duration |
| `ArtifactMissing` | Expected files not produced |
| `Cancelled` | User/system cancelled run |

Do not expose raw stack traces or provider responses to end users.

## Testing checklist

- [ ] Valid request creates queued run.
- [ ] Invalid post count returns validation error.
- [ ] Request cannot set tenant id.
- [ ] User sees only their tenant's runs.
- [ ] Worker transitions queued -> running -> succeeded.
- [ ] Worker handles Python non-zero exit.
- [ ] Worker handles timeout.
- [ ] Missing artifact marks run failed or degraded.
- [ ] Cancellation works for queued runs.
- [ ] Provider key is not logged.
- [ ] Duplicate idempotency key does not create duplicate billable runs.

## Interview prompts

- Why not call CrewAI directly from the controller?
- How do you pass user input safely to Python?
- How do you prevent one tenant from reading another tenant's output files?
- What would you store in the database vs object storage?
- How would you scale workers?
- How would you trace one run across API, worker, and provider calls?

