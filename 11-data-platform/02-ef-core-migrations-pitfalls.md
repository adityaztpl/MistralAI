# EF Core Migrations Pitfalls

## Interview-ready summary

EF Core migrations are code changes plus data operations plus deployment choreography. The migration file is not the whole story. Safe teams plan for rollback, rolling deployments, locks, data volume, and compatibility between old and new app versions.

## Migration workflow

1. Change the model intentionally.
2. Generate migration.
3. Read the migration file line by line.
4. Adjust destructive or large operations.
5. Test against a copy of realistic data.
6. Generate SQL script for review when deploying manually.
7. Deploy with app compatibility in mind.

Commands:

```bash
dotnet ef migrations add AddNotesArchiveFlag --project src/App.Infrastructure --startup-project src/App.Api
dotnet ef database update --project src/App.Infrastructure --startup-project src/App.Api
dotnet ef migrations script --idempotent -o artifacts/migrations.sql
```

## Pitfall 1: destructive changes generated silently

EF may generate `DropColumn`, `DropTable`, or column recreation when a rename was intended.

Bad migration:

```csharp
migrationBuilder.DropColumn(name: "FullName", table: "Users");
migrationBuilder.AddColumn<string>(name: "DisplayName", table: "Users", nullable: false);
```

Better:

```csharp
migrationBuilder.RenameColumn(
    name: "FullName",
    table: "Users",
    newName: "DisplayName");
```

Always inspect migrations before committing.

## Pitfall 2: adding non-null columns to large tables

This can lock or rewrite large tables.

Safer expand/backfill/contract:

1. Add nullable column.
2. Deploy app that writes both old and new columns.
3. Backfill in batches.
4. Add constraint/default.
5. Deploy app that reads new column.
6. Drop old column later.

Example:

```csharp
migrationBuilder.AddColumn<string>(
    name: "NormalizedTitle",
    table: "Notes",
    nullable: true);
```

Backfill in batches outside the migration for very large data sets.

## Pitfall 3: long-running data migrations inside schema migration

Avoid huge `UPDATE` statements in an app startup migration. They can block deployment and lock tables.

Prefer:

- Separate background backfill job.
- Idempotent SQL script with batches.
- Operational runbook and progress table.

Batch idea:

```sql
UPDATE notes
SET normalized_title = lower(title)
WHERE normalized_title IS NULL
  AND id IN (
    SELECT id FROM notes WHERE normalized_title IS NULL LIMIT 1000
  );
```

## Pitfall 4: applying migrations automatically on every app startup

`Database.Migrate()` on startup is convenient for demos but risky for multi-instance production deployments.

Risks:

- Multiple app instances race to migrate.
- App startup becomes dependent on schema locks.
- Failed migration can break deployment.
- App identity may have excessive DDL permissions.

Better production pattern:

- Run migrations as a deployment step/job.
- Use least-privilege runtime DB user.
- Generate and review SQL scripts.
- Keep app compatible with previous and next schema during rolling deploys.

## Pitfall 5: indexes created without considering downtime

Large index builds can block writes depending on database/provider.

PostgreSQL:

```sql
CREATE INDEX CONCURRENTLY notes_user_updated_idx
ON notes (user_id, updated_at DESC);
```

EF migrations do not directly wrap `CREATE INDEX CONCURRENTLY` well because it cannot run inside a transaction. You may need:

```csharp
migrationBuilder.Sql(
    "CREATE INDEX CONCURRENTLY IF NOT EXISTS notes_user_updated_idx ON notes (user_id, updated_at DESC);",
    suppressTransaction: true);
```

## Pitfall 6: migration history drift

Drift happens when:

- Manual DB changes are not represented in migrations.
- Branches generate conflicting migration order.
- A migration was edited after being applied.
- Different environments point to different databases.

Prevention:

- Do not edit applied migrations except in disposable dev DBs.
- Use idempotent scripts for deployments.
- Keep migration history table backed up.
- Review generated SQL in CI.

## Pitfall 7: enum/status changes

Adding a new status may require:

- Database check constraint update.
- Application validation update.
- Backward-compatible readers.
- UI display fallback.

If older app versions do not understand the new status, rolling deployments can fail.

## Testing migrations

High-value checks:

- Apply all migrations to empty DB.
- Apply latest migration to previous schema with seed data.
- Run app tests after migration.
- Verify rollback strategy, even if rollback means forward-fix.
- Check generated SQL for destructive operations.

CI idea:

```bash
dotnet ef database update --connection "$TEST_DATABASE"
dotnet test
```

## Zero-downtime pattern: expand and contract

Example: rename `body` to `content`.

1. Expand: add `content` nullable.
2. App v1.1: write both `body` and `content`, read `content ?? body`.
3. Backfill `content` from `body`.
4. App v1.2: read `content` only.
5. Contract: drop `body` after all instances are updated.

## Interview phrasing

> I treat migrations as deployment artifacts, not just generated code. I inspect generated migrations for destructive operations, use expand/backfill/contract for risky changes, avoid long data updates in startup migrations, and test migrations against realistic data. For production, I prefer reviewed SQL or a controlled migration job rather than every web instance applying migrations on startup.
