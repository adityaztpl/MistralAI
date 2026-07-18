# 07 - Runnable Projects

This folder contains interview-ready, runnable project blueprints that combine backend, frontend,
database, authentication, observability, and AI integration patterns. The examples are intentionally
small enough to explain at a whiteboard and realistic enough to discuss tradeoffs in a senior-level
system design or implementation interview.

## Projects

| Project | Stack | What it demonstrates |
| --- | --- | --- |
| [aspnet-react-rag](./aspnet-react-rag/README.md) | ASP.NET Core Minimal API, React, PostgreSQL + pgvector, optional Redis | Retrieval-augmented generation, embeddings, streaming chat, citations, containerized local development |
| [aspnet-angular-notes](./aspnet-angular-notes/README.md) | ASP.NET Core Minimal API, Angular, PostgreSQL, JWT | Notes CRUD, token auth, validation, multi-tenant ownership checks, simple production hardening |

## Repository conventions

These projects are scaffolds rather than generated framework output. They focus on the files that are
most useful for explaining the architecture:

- `README.md` files contain the tutorial, interview prompts, architecture notes, and test checklist.
- `docker-compose.yml` files define the local topology and environment variables.
- `api/Program.cs` files show the service wiring and endpoint flow.
- Front-end files show the core UI logic without burying the example in generated assets.

When turning one of these into a complete application, add the surrounding framework files:

- ASP.NET: `.csproj`, `Properties/launchSettings.json`, unit/integration test projects.
- React/Vite: `package.json`, `index.html`, `src/main.tsx`, styling, test config.
- Angular: `package.json`, `angular.json`, app module or standalone bootstrap, routing.

## Prerequisites

Install the following tools:

1. **Docker Desktop** or Docker Engine with Compose v2.
2. **.NET SDK 8 or newer** for local API runs outside containers.
3. **Node.js 20 or newer** for React/Angular local development outside containers.
4. **A Mistral API key** exported as `MISTRAL_API_KEY` for AI examples.
5. Optional: `psql`, `curl`, and an HTTP client such as Bruno, Insomnia, or Postman.

No secrets are checked in. Use environment variables, `.env` files excluded by git, Docker secrets,
or your deployment platform secret manager.

## How to run the RAG project

```bash
cd 07-runnable-projects/aspnet-react-rag
export MISTRAL_API_KEY="replace-me"
docker compose up --build
```

Then open:

- React UI: <http://localhost:5173>
- API health: <http://localhost:8080/health>
- Postgres: `localhost:5432`, database `rag`, user `rag`

Typical demo flow:

1. Ingest two or three short documents using the `/ingest` API or the README curl examples.
2. Ask a question in the chat UI.
3. Watch the streamed answer appear token by token.
4. Inspect citations returned by the API.
5. Explain where embeddings, vector search, prompt construction, and streaming happen.

## How to run the notes project

```bash
cd 07-runnable-projects/aspnet-angular-notes
docker compose up --build
```

Then open:

- Angular UI: <http://localhost:4200>
- API health: <http://localhost:8081/health>
- Postgres: `localhost:5433`, database `notes`, user `notes`

Typical demo flow:

1. Register or sign in to receive a JWT.
2. Create, edit, filter, and delete notes.
3. Show that each query is scoped to the authenticated user.
4. Explain validation, password hashing, JWT claims, and database indexes.

## Interview talking points

Use these projects to demonstrate:

- How you decompose a full-stack feature into API, data model, UI, and infrastructure pieces.
- How you keep secrets out of source control while preserving a reproducible local setup.
- How Docker Compose models production dependencies without becoming a production deployment.
- How to distinguish demo shortcuts from production requirements.
- How to debug cross-service issues with health checks, logs, and network names.
- How to reason about latency, caching, retries, backpressure, and idempotency.

## Production-readiness checklist

Before promoting either scaffold to production, add:

- HTTPS termination and strict CORS allowlists.
- OpenTelemetry traces, structured logs, metrics, and dashboards.
- Centralized secret management.
- Database migrations with rollback strategy.
- Rate limiting and abuse protection.
- Integration tests against real containers.
- CI checks for build, test, lint, format, dependency scanning, and container scanning.
- IaC for cloud infrastructure.
- Blue/green or rolling deployment strategy.
- Error budget and operational runbooks.

