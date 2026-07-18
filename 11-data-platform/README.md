# Data Platform for Full-Stack and GenAI Apps

This section covers the practical data layer skills that show up in senior full-stack and GenAI interviews: SQL modeling, indexes, EF Core migrations, caching, queues, Docker Compose, CI, and Azure deployment.

## Why this matters

AI applications still rely on ordinary data platform fundamentals:

- Chat sessions, documents, users, permissions, feedback, and audit trails need relational modeling.
- RAG quality depends on ingestion status, chunk metadata, vector indexes, and eval tables.
- Low latency often comes from good indexes and caching, not a larger model.
- Background jobs handle ingestion, embedding, notifications, exports, and retries.
- CI/CD and deployment quality determine whether the system is reproducible.

## Files

| Guide | Focus |
|---|---|
| [01 SQL design and indexing](01-sql-design-and-indexing.md) | Schema design, indexes, query plans, RAG tables |
| [02 EF Core migrations pitfalls](02-ef-core-migrations-pitfalls.md) | Migration discipline, zero-downtime patterns, data changes |
| [03 Redis caching patterns](03-redis-caching-patterns.md) | Cache-aside, invalidation, distributed locks, rate limits |
| [04 Queues and background jobs](04-queues-and-background-jobs.md) | Hangfire, Worker Services, RabbitMQ, Azure Service Bus concepts |
| [05 Docker Compose and CI](05-docker-compose-and-ci.md) | Local dependencies, integration tests, GitHub Actions |
| [06 Azure deploy .NET](06-azure-deploy-dotnet.md) | App Service/Container Apps, SQL, Key Vault, observability |
| [07 Cheatsheet](07-cheatsheet.md) | Fast recall for interviews |

## Examples

- [schema_notes_rag.sql](examples/schema_notes_rag.sql) - PostgreSQL schema for notes + RAG metadata + pgvector.
- [github-actions-dotnet.yml](examples/github-actions-dotnet.yml) - CI pipeline for restore, build, test, and optional Docker build.

## Interview framing

When asked about data platform design, structure your answer:

```text
Access patterns -> schema -> indexes -> consistency -> failure handling -> operations
```

Example:

> For a RAG chat app, I would model documents, chunks, conversations, messages, citations, feedback, and ingestion jobs separately. I would index tenant-scoped query paths, store embedding model/dimension on chunks, and use a background worker for ingestion. Redis can cache hot read models and rate limits, but the relational database remains the source of truth. Migrations need to be backward compatible with rolling deployments.
