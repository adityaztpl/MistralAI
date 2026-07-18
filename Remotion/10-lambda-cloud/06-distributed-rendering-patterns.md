# Distributed rendering patterns

> This guide is part of the local Remotion curriculum dump and should be read alongside the official Remotion Lambda and Cloud Run docs.

## Outcome

Design a rendering subsystem that survives retries, scale, duplicate requests, worker restarts, and partial cloud failures.

## Core idea

A production render platform is a workflow engine around Remotion. The composition code is only one part. The system needs job records, idempotency keys, queues, priority, cancellation, progress polling, artifact storage, cleanup, and user-visible status transitions.

Cloud rendering changes the bottleneck from one machine to an orchestrated fleet. A render request must identify a deployed site or bundle, a function or service version, a composition id, input props, codec/output settings, and a storage destination. The app then tracks progress, exposes the resulting artifact, and cleans up temporary resources.

## Implementation workflow


1. Accept render requests through an API that validates props and calculates an idempotency key.
2. Store a job row before starting cloud work.
3. Use a queue for execution so web requests return quickly.
4. Let workers start cloud renders, persist render ids, and schedule polling or await webhooks.
5. Make status transitions explicit and monotonic: created -> queued -> rendering -> encoding -> completed or failed.
6. Implement cancellation as a product state even if the underlying cloud job cannot always stop instantly.
7. Use dead-letter handling for jobs that fail repeatedly.


## Practical example


```sql
create table render_jobs (
  id uuid primary key,
  tenant_id uuid not null,
  idempotency_key text not null,
  composition_id text not null,
  input_props_json jsonb not null,
  status text not null,
  provider text not null,
  provider_render_id text,
  provider_bucket text,
  output_url text,
  error_code text,
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now(),
  unique (tenant_id, idempotency_key)
);
```

```ts
const transitions = {
  created: ['queued', 'failed'],
  queued: ['rendering', 'cancelled', 'failed'],
  rendering: ['encoding', 'completed', 'failed', 'cancelled'],
  encoding: ['completed', 'failed'],
  completed: [],
  failed: ['queued'],
  cancelled: [],
} as const;
```


## Operational checklist


- Every render request has an idempotency key.
- Workers can restart without losing provider render ids.
- Status polling is rate-limited and backoff-aware.
- Job records include enough context to debug after logs expire.
- Priority queues prevent large final renders from starving short previews.


## Risks and trade-offs


- Duplicate job starts are expensive and confuse users.
- Polling every second across thousands of jobs can create avoidable load.
- Retrying invalid props wastes money; classify errors before retrying.
- A single global queue can become unfair without tenant or priority controls.


## Interview-ready explanation

For an interview or design review, explain that Remotion Lambda and Cloud Run are not separate animation systems. They run the same Remotion compositions but move rendering into cloud infrastructure. The hard parts become deployment versioning, permissions, regional latency, concurrency limits, artifact storage, webhooks, and cost controls.

## Practice tasks

1. Diagram a render request from API call through cloud execution to final S3 or GCS artifact.
2. Pick one composition and define the minimal `inputProps` payload that should be accepted from an external product.
3. Write a retry policy that distinguishes user input errors from transient cloud failures.
4. Estimate cost for a batch of 1,000 renders using duration, resolution, concurrency, and storage assumptions.


## Official docs

- Lambda progress: <https://www.remotion.dev/docs/lambda/getrenderprogress>
- Webhooks: <https://www.remotion.dev/docs/lambda/validatewebhooksignature>
- Cloud Run render progress concepts: <https://www.remotion.dev/docs/cloudrun/rendermediaoncloudrun>
