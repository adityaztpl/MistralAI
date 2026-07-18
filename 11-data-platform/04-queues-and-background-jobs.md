# Queues and Background Jobs

## Interview-ready summary

Background processing is for work that should not block a request: ingestion, embeddings, email, exports, retries, and integration calls. Good queue design is about idempotency, retries, visibility, poison messages, ordering, and operational monitoring.

## When to use a background job

Use a job when work is:

- Slow or variable latency.
- Retryable after transient failures.
- Triggered by user action but not required for immediate response.
- Scheduled or recurring.
- Fan-out/fan-in oriented.
- Dependent on external systems.

Do not use a queue to hide synchronous correctness requirements. If the user needs a transaction to be complete before response, commit it first.

## Hangfire

Hangfire is a .NET background job library that stores jobs in SQL Server, PostgreSQL, Redis, or other storage.

Good for:

- Web apps that need simple background jobs.
- Retries and dashboard.
- Scheduled/recurring jobs.
- Small-to-medium workloads.

Example:

```csharp
builder.Services.AddHangfire(config =>
    config.UsePostgreSqlStorage(connectionString));
builder.Services.AddHangfireServer();

app.MapPost("/api/documents/{id}/ingest", (Guid id, IBackgroundJobClient jobs) =>
{
    jobs.Enqueue<DocumentIngestionJob>(job => job.IngestAsync(id, CancellationToken.None));
    return Results.Accepted($"/api/documents/{id}/status");
});
```

Job:

```csharp
public sealed class DocumentIngestionJob
{
    public async Task IngestAsync(Guid documentId, CancellationToken cancellationToken)
    {
        // Load source, chunk, embed, store chunks, update status.
    }
}
```

Caution: design jobs to be idempotent because retries can run the same job more than once.

## .NET Worker Service

A Worker Service is a long-running process, often deployed separately from the API.

Good for:

- Consuming RabbitMQ/Azure Service Bus messages.
- Heavy ingestion pipelines.
- Separating web scale from worker scale.
- Kubernetes/Container Apps deployments.

Sketch:

```csharp
public sealed class IngestionWorker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in queue.ReceiveAsync(stoppingToken))
        {
            try
            {
                await handler.HandleAsync(message, stoppingToken);
                await queue.CompleteAsync(message, stoppingToken);
            }
            catch (TransientException)
            {
                await queue.AbandonAsync(message, stoppingToken);
            }
            catch (Exception ex)
            {
                await queue.DeadLetterAsync(message, ex.Message, stoppingToken);
            }
        }
    }
}
```

## RabbitMQ concepts

- Exchange: routes messages.
- Queue: stores messages for consumers.
- Binding: connects exchange to queue with routing key.
- Acknowledgement: consumer confirms successful processing.
- Dead-letter exchange/queue: captures failed messages.
- Prefetch: controls how many messages a consumer receives before ack.

Best for:

- Flexible routing.
- Open-source/self-hosted deployments.
- High-throughput messaging.

## Azure Service Bus concepts

- Queue: point-to-point messaging.
- Topic/subscription: pub/sub.
- Peek-lock: receive message, process, then complete.
- Dead-letter queue: failed/unprocessable messages.
- Duplicate detection: broker-level duplicate suppression window.
- Sessions: ordered processing for related messages.
- Scheduled messages: deliver later.

Best for:

- Azure-native apps.
- Managed reliability.
- Enterprise messaging with dead-lettering and sessions.

## Job state model

For long-running user-visible work, store job state in your database:

```sql
CREATE TABLE ingestion_jobs (
  id uuid PRIMARY KEY,
  document_id uuid NOT NULL,
  status text NOT NULL CHECK (status IN ('queued','running','succeeded','failed','cancelled')),
  attempts int NOT NULL DEFAULT 0,
  last_error text,
  queued_at timestamptz NOT NULL DEFAULT now(),
  started_at timestamptz,
  completed_at timestamptz
);
```

API returns `202 Accepted` and status URL:

```http
HTTP/1.1 202 Accepted
Location: /api/ingestion-jobs/abc123
```

## Idempotency

Every retryable job needs an idempotency strategy:

- Natural unique key, such as `(document_id, content_hash, chunk_index)`.
- Upsert instead of blind insert.
- Outbox table for external side effects.
- Idempotency key for provider/API calls when supported.
- State machine transitions that tolerate duplicate messages.

Example:

```sql
CREATE UNIQUE INDEX document_chunks_document_hash_idx
ON document_chunks (document_id, content_hash, chunk_index);
```

## Retry strategy

Classify errors:

| Error | Retry? | Example |
|---|---|---|
| Transient | Yes with backoff | 429, timeout, temporary DB/network issue |
| Permanent | No, dead-letter | invalid document format, missing required data |
| Ambiguous | Retry idempotently | provider timeout after possible success |

Backoff:

```text
1 min -> 5 min -> 15 min -> 1 h -> dead-letter/manual review
```

## Outbox pattern

Use when a database transaction and message publish must be consistent.

1. Save business change and outbox event in same DB transaction.
2. Background publisher reads unsent outbox rows.
3. Publish to broker.
4. Mark outbox row sent.

This avoids "DB committed but message lost" problems.

## Monitoring

Track:

- Queue depth.
- Oldest message age.
- Processing duration.
- Retry count.
- Dead-letter count.
- Worker heartbeats.
- Throughput by job type.
- Cost for embedding/model jobs.

## Common mistakes

- Jobs are not idempotent.
- No dead-letter handling.
- Retrying permanent errors forever.
- No status endpoint for user-visible background work.
- Web request waits for queue processing anyway.
- No cancellation or timeout.
- No correlation IDs between API request and job logs.

## Interview phrasing

> I would put slow ingestion and embedding work behind a queue and return `202 Accepted` with a job status URL. The worker would be idempotent, use retries with backoff for transient failures, dead-letter permanent failures, and write job state for observability. For reliable integration events I would use the outbox pattern so database commits and message publication do not drift.
