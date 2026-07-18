# Azure Deploy: .NET API/Data Platform

## Interview-ready summary

A solid Azure deployment answer covers compute, configuration, secrets, database, networking, observability, migrations, and rollback. The exact service choice depends on scale and team maturity, but the principles stay the same.

## Common deployment options

| Option | Best for | Notes |
|---|---|---|
| Azure App Service | Simple .NET APIs/web apps | Managed platform, deployment slots, easy scaling |
| Azure Container Apps | Containerized APIs/workers, event-driven scale | Good for API + worker + queue workloads |
| Azure Kubernetes Service | Complex multi-service platforms | More operational overhead |
| Azure Functions | Event-driven jobs | Good for small queue/timer tasks, watch cold start/limits |

For most interview designs, App Service or Container Apps is a strong default.

## Reference architecture

```text
Frontend static hosting or SPA
  -> Azure Front Door/App Gateway optional
  -> App Service or Container App (.NET API)
      -> Azure SQL or PostgreSQL Flexible Server
      -> Azure Cache for Redis
      -> Azure Service Bus
      -> Key Vault
      -> Application Insights
  -> Worker Service for background jobs
```

## Configuration and secrets

- Store secrets in Key Vault.
- Use managed identity from App Service/Container App to read secrets.
- Use app settings for non-secret config.
- Never commit provider keys or connection strings.
- Rotate LLM/API keys intentionally.

Example settings:

```text
ConnectionStrings__Default
Redis__ConnectionString
ServiceBus__Namespace
OpenAI__Endpoint
OpenAI__Deployment
Rag__EmbeddingModel
Rag__EmbeddingDimension
```

## Database

Options:

- Azure SQL: strong Microsoft ecosystem, mature tooling.
- Azure Database for PostgreSQL Flexible Server: good for pgvector/RAG metadata when extension support fits.

Deployment considerations:

- Private networking if required.
- Firewall rules or private endpoints.
- Automated backups and point-in-time restore.
- Connection pooling.
- Migration deployment step.
- Read replicas only after measuring.

## Migrations in Azure

Prefer a pipeline step or one-off job:

```bash
dotnet ef migrations script --idempotent -o migrations.sql
# Execute with a deployment identity that has DDL permissions.
```

Avoid every app instance running migrations at startup in production.

For zero downtime, use expand/backfill/contract and deployment slots.

## App Service deployment flow

1. Build and test in CI.
2. Publish artifact or container image.
3. Deploy to staging slot.
4. Run smoke tests.
5. Warm up staging slot.
6. Swap staging to production.
7. Monitor errors, latency, dependencies.
8. Roll back by slot swap if needed.

## Container Apps deployment flow

1. Build container image.
2. Push to Azure Container Registry.
3. Deploy new revision.
4. Route small percentage of traffic if using revision splitting.
5. Observe health and logs.
6. Shift traffic or roll back revision.

Container Apps works well when API and worker are separate containers and workers scale based on Service Bus queue length.

## Observability

Use Application Insights/OpenTelemetry to capture:

- Request rate, duration, failures.
- Dependency calls to SQL, Redis, Service Bus, model providers.
- Traces with correlation IDs.
- Logs for business events and background jobs.
- Custom metrics: ingestion duration, retrieval no-hit rate, token usage, model cost.

Do not log raw prompts or documents by default. Log IDs, lengths, scores, and redacted summaries.

## Health checks

Expose:

```http
GET /health/live
GET /health/ready
```

- Liveness: process is alive.
- Readiness: dependencies needed to serve traffic are reachable.

ASP.NET Core:

```csharp
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>()
    .AddRedis(redisConnectionString);

app.MapHealthChecks("/health/ready");
```

## Scaling

Scale API by:

- CPU/memory/request latency.
- Connection pool limits.
- Downstream provider rate limits.

Scale workers by:

- Queue depth.
- Oldest message age.
- Job duration.
- Provider rate limits for embeddings/LLMs.

Protect model providers with rate limits, retries, and circuit breakers.

## Security

- Managed identity for Azure resources.
- Key Vault for secrets.
- HTTPS only.
- Restrictive CORS.
- AuthN/AuthZ at API.
- Private endpoints for DB/Redis where appropriate.
- Least-privilege database users.
- Audit logs for admin/risky actions.

## Common mistakes

- App settings differ silently across environments.
- Migrations run from every app instance.
- No staging slot or smoke test.
- No Application Insights dependency tracing.
- Runtime identity has DDL/admin privileges unnecessarily.
- Worker and API scale together even though workloads differ.
- Prompt/model provider failures are not monitored.

## Interview phrasing

> For a .NET API I would usually start with App Service for simplicity or Container Apps if I need separate API and worker scaling. Secrets live in Key Vault accessed by managed identity. The database is Azure SQL or PostgreSQL depending on vector needs. Migrations run as a controlled deployment step, not from every instance. I would deploy through a staging slot or new revision, smoke test, monitor App Insights, and roll back quickly if dependency errors or latency spike.
