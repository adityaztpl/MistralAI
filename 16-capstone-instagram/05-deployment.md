# 05 - Deployment

Deployment is where the capstone becomes an operations story. You need to explain how the API, SPA, worker, Python runtime, database, storage, and secrets run outside your laptop.

## Learning goals

- Design a local development topology.
- Choose a production deployment shape.
- Package ASP.NET, SPA, and Python dependencies.
- Run database migrations safely.
- Manage secrets and provider keys.
- Add health checks, logs, metrics, and runbooks.

## Local development topology

Start simple:

```text
localhost:5173   React/Vite SPA
localhost:5000   ASP.NET API
local worker     HostedService in API process or separate worker
Postgres/SQLite  Run metadata
local outputs/   Generated artifacts
Mistral API      External provider
```

Suggested local commands:

```bash
# Python crew package
python -m venv .venv
source .venv/bin/activate
pip install -e ".[dev]"

# API
dotnet run --project src/InstagramCapstone.Api

# SPA
npm install
npm run dev
```

For the docs-only capstone, you do not need all projects to exist yet; this is the target structure.

## Production topology options

### Option A - Single container for API + worker + Python

Pros:

- easiest first deployment;
- one service to operate;
- local process invocation works.

Cons:

- API and worker scale together;
- Python and .NET dependencies share one image;
- long-running jobs can compete with API resources.

Use for demo/staging.

### Option B - Separate API and worker containers

Pros:

- scale API and workers independently;
- worker can have Python-heavy image;
- easier resource limits and job isolation.

Cons:

- requires queue;
- more deployment resources;
- more tracing and service configuration.

Use for production-like capstone.

### Option C - API + Python AI service

Pros:

- clean language boundary;
- Python service owns CrewAI lifecycle;
- easier to reuse AI service.

Cons:

- more service-to-service security;
- async job API still required;
- more operational overhead.

Use when the AI workflow becomes shared platform functionality.

## Recommended capstone deployment

For interviews, present Option B:

```text
Static SPA hosting/CDN
        |
        v
ASP.NET Core API container
        |
        +--> Postgres
        +--> Redis/queue
        +--> Object storage
        |
        v
.NET worker or Python worker container
        |
        v
Mistral API
```

This shows clean production thinking while staying implementable.

## Container packaging

### API image

Contains:

- ASP.NET API binaries;
- migrations bundle or migration runner;
- no provider secrets;
- no `.env`;
- health endpoints.

### Worker image

Contains:

- worker executable;
- Python runtime if invoking existing package;
- installed `instagram_content_creator` package;
- no provider secrets baked into layers;
- output directory mounted or object storage client.

### SPA artifact

Contains:

- static JS/CSS;
- public config only;
- no provider keys;
- CSP headers configured at host/CDN.

## Environment variables

API/worker:

```text
ConnectionStrings__AppDb=...
Auth__Authority=...
Auth__Audience=...
Mistral__ApiKey=...
Mistral__DefaultModel=mistral-large-latest
Storage__Bucket=...
Queue__ConnectionString=...
```

SPA:

```text
VITE_API_BASE_URL=https://api.example.com
VITE_AUTH_AUTHORITY=https://login.example.com
VITE_AUTH_CLIENT_ID=public-spa-client-id
```

The SPA variables are public. Do not put model-provider secrets there.

## Database migrations

Options:

- run migrations as a CI/CD step before deployment;
- use a one-shot migration job;
- use EF Core migration bundle;
- avoid auto-migrating from every API instance at startup in production.

Migration safety:

- backward-compatible schema changes;
- deploy code that can run with old and new schema during rollout;
- backfill in batches;
- avoid long locks;
- backup before destructive changes.

## Health checks

API:

- `/health/live` - process is alive;
- `/health/ready` - database reachable, queue reachable, config valid.

Worker:

- heartbeat metric;
- queue consumer lag;
- last successful dequeue;
- failed job count.

Provider:

- avoid aggressive provider health checks that spend tokens;
- validate key at startup only if provider supports cheap auth check;
- otherwise detect provider failures from real run attempts.

## Observability

### Logs

Use structured logs with:

- `run_id`;
- `tenant_id`;
- `user_id` where safe;
- `correlation_id`;
- `status`;
- `error_code`.

Do not log:

- raw provider key;
- raw JWT;
- full prompts by default;
- full generated content if it contains customer data.

### Metrics

- API request latency and error rate;
- run queue depth;
- run duration;
- success/failure rate;
- estimated cost by tenant;
- token usage;
- worker CPU/memory;
- provider rate-limit count.

### Traces

Trace across:

- create-run request;
- enqueue;
- dequeue;
- crew process;
- artifact write;
- eval scoring;
- feedback submission.

## CI/CD pipeline

Suggested stages:

1. Lint/format docs and code.
2. Run .NET tests.
3. Run Python tests.
4. Build API image.
5. Build worker image.
6. Build SPA artifact.
7. Run secret scan.
8. Run dependency scan.
9. Apply migrations in staging.
10. Deploy staging.
11. Smoke test create/list run with stub provider.
12. Manual approval for production.
13. Deploy production.
14. Monitor error/cost dashboards.

## Runbooks

### Run stuck in `Running`

1. Check worker heartbeat.
2. Check run events.
3. Check process timeout logs.
4. Mark run failed/cancelled if worker died.
5. Requeue only if idempotent.
6. Notify user with safe error.

### Provider key invalid

1. Confirm error category without exposing key.
2. Rotate key in secret manager.
3. Restart API/worker or refresh secret cache.
4. Run a low-cost smoke test.
5. Review recent usage for abuse.

### Cost spike

1. Disable run creation for affected tenant or model.
2. Inspect usage records by tenant/user.
3. Check for retries/loops.
4. Lower quotas or concurrency.
5. Rotate key if compromise suspected.

### Bad generated content

1. Preserve run artifacts and metadata.
2. Capture human feedback.
3. Categorize failure.
4. Patch prompt/eval/policy.
5. Add regression test.

## Interview prompts

- How would you deploy the Python CrewAI dependency with an ASP.NET API?
- How do API and worker scale differently?
- Where do migrations run?
- What does readiness mean for this app?
- What metrics reveal provider cost problems?
- How do you keep secrets out of images and browser bundles?
- How do you debug a failed content run?

## Done criteria

- [ ] Deployment topology is documented.
- [ ] API, worker, SPA, DB, queue, and storage responsibilities are clear.
- [ ] Secret injection is runtime-only.
- [ ] Health checks are defined.
- [ ] CI/CD stages include tests and scans.
- [ ] Runbooks exist for stuck jobs, provider auth, cost spike, and bad content.

