# Docker Compose and CI

## Interview-ready summary

Docker Compose makes local dependencies repeatable; CI proves the app builds and tests away from your machine. For take-homes and team projects, a small reliable setup beats an elaborate platform that reviewers cannot run.

## Docker Compose goals

Use Compose for dependencies, not necessarily every app process.

Good local dependencies:

- PostgreSQL.
- Redis.
- RabbitMQ.
- Azurite for Azure Storage emulation.
- Vector DB such as Qdrant.

Example:

```yaml
services:
  postgres:
    image: pgvector/pgvector:pg16
    environment:
      POSTGRES_DB: app
      POSTGRES_USER: app
      POSTGRES_PASSWORD: app
    ports:
      - "5432:5432"
    volumes:
      - postgres-data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U app -d app"]
      interval: 5s
      timeout: 3s
      retries: 20

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"

volumes:
  postgres-data:
```

## Compose best practices

- Pin major versions.
- Add health checks for services used by tests.
- Use named volumes for durable local data.
- Provide reset command:

```bash
docker compose down -v
docker compose up -d
```

- Keep secrets out of committed Compose files; dev passwords are fine only for local demo dependencies.
- Document ports.
- Avoid requiring every developer to run cloud services for basic tests.

## App container Dockerfile

For .NET:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish src/App.Api/App.Api.csproj -c Release -o /out --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /out .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "App.Api.dll"]
```

Adjust version to match the project target framework.

## CI pipeline stages

Minimum:

1. Checkout.
2. Setup SDK/runtime.
3. Restore dependencies.
4. Format/lint check.
5. Build.
6. Test.
7. Upload coverage/test results if useful.

For integration tests:

- Start PostgreSQL/Redis service containers.
- Wait for health.
- Apply migrations.
- Run tests.

## GitHub Actions .NET sketch

See [examples/github-actions-dotnet.yml](examples/github-actions-dotnet.yml).

Key ideas:

- Use `dotnet restore --locked-mode` when lock files exist.
- Use `dotnet build --no-restore`.
- Use `dotnet test --no-build`.
- Keep environment variables explicit.
- Cache NuGet packages if CI duration matters.

## Testcontainers alternative

Instead of Compose in CI, .NET tests can start containers programmatically using Testcontainers. This gives test-level isolation but adds test startup time.

Good for:

- Integration tests that need clean DB per test class.
- Avoiding CI-specific service YAML.
- Reproducible database versions.

## Migration checks in CI

Consider:

```bash
dotnet ef migrations script --idempotent -o artifacts/migrations.sql
dotnet ef database update --connection "$TEST_DATABASE"
```

Also check for pending model changes if your team has a pattern for it.

## Common mistakes

- CI only builds but does not run tests.
- Tests depend on a developer's local database.
- Compose service starts but is not healthy before tests run.
- Secrets are committed in workflow YAML.
- Docker image builds from unpinned or wrong SDK version.
- Integration tests share dirty state across runs.

## Interview phrasing

> I use Docker Compose to make local dependencies like Postgres, Redis, and RabbitMQ repeatable, with health checks and documented reset commands. CI restores, builds, tests, and optionally runs integration tests against service containers or Testcontainers. For EF projects, I also validate migrations against a real database so schema drift is caught before deployment.
