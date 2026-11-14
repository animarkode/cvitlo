# Database migrations: operator and contributor guide

This guide covers how Cvitlo's database schema is migrated in development and in a deployed stack, how to add a migration, and how to register a new module `DbContext`. For Keycloak's side of the database, see `docs/operations/keycloak.md`.

## 1. How migrations run

One job, `Cvitlo.Migrations` (`src/api/Jobs/Cvitlo.Migrations`), applies every module's EF Core migrations to the `cvitlodb` database. The API never migrates on its own start.

| Environment | How migrations run |
|---|---|
| Development | Automatically: the Aspire AppHost runs `Cvitlo.Migrations` once Postgres is up, before Keycloak and the API start |
| Production / Staging | The `cvitlo-migrations` container runs to completion before the API container starts |

The job exits 0 when every context migrated and non-zero on any failure, which must abort the deploy.

### Deploy-time flow

The `cvitlo-migrations` image is built next to `cvitlo-api` on release (`.github/workflows/build-images.yml`). In the production orchestrator, where `<OWNER>` is the repository owner's login in lowercase:

```yaml
services:
  cvitlo-migrations:
    image: ghcr.io/<OWNER>/cvitlo-migrations:${TAG}
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ConnectionStrings__cvitlodb: ${CVITLODB_CONNSTR}
    restart: "no"

  cvitloapi:
    image: ghcr.io/<OWNER>/cvitlo-api:${TAG}
    depends_on:
      cvitlo-migrations:
        condition: service_completed_successfully
```

### What is migrated

| DbContext | Schema | History table | Module Infrastructure project |
|---|---|---|---|
| `ResumesDbContext` | `resumes` | `migrations_history` | `Cvitlo.Resumes.Infrastructure` |
| `AuthDbContext` | `auth` | `migrations_history` | `Cvitlo.Auth.Infrastructure` |

Each module keeps its own schema and its own history table, named by the module's `DbConstants`. The legacy shared `DbContext` no longer exists. Leftover legacy tables and `__EFMigrationsHistory` rows in older databases are untouched and not managed by this job.

### Schemas EF Core does not own

Schemas created outside EF Core, such as Keycloak's, are listed in `Migrations:ExternalSchemas` (environment variables `Migrations__ExternalSchemas__0`, `__1`, ...). Before migrating, the job only makes sure each one exists, using EF's own `EnsureSchema` so a role without `CREATE` on the database still succeeds once the schema is there. It never touches their contents.

The AppHost declares `keycloak` this way, next to Keycloak's `KC_DB_SCHEMA`. A production deployment declares none unless it sets them. Blank names fail the job at start.

## 2. Adding a migration

`dotnet ef` is pinned in `.config/dotnet-tools.json`. Restore it first, then run from the repository root, with the module's Infrastructure project as `--project` and the API as `--startup-project`:

```bash
dotnet tool restore
```

```bash
dotnet ef migrations add <MIGRATION-NAME> --project src/api/Modules/<MODULE>/Cvitlo.<MODULE>.Infrastructure --startup-project src/api/Cvitlo.Api
```

To review the SQL before it is applied:

```bash
dotnet ef migrations script --project src/api/Modules/<MODULE>/Cvitlo.<MODULE>.Infrastructure --startup-project src/api/Cvitlo.Api
```

You do not run `database update` yourself in the normal flow: the AppHost applies pending migrations on the next start, and production applies them through the job above.

## 3. Registering a new module DbContext

A module that owns a `DbContext` must be registered with the migration job as well as with the API. All paths are under `src/api/Jobs/Cvitlo.Migrations` unless noted.

1. Add a `<ProjectReference>` to the module's Infrastructure project in `Cvitlo.Migrations.csproj`.
2. In `Program.cs`, register the context with `AddDbContext`, using Npgsql, `MigrationsHistoryTable(DbConstants.MigrationsHistoryTableName, DbConstants.SchemaName)` from the module's `DbConstants`, and `UseSnakeCaseNamingConvention()`. Alias the `DbConstants` type per module, as `ResumesDbConst` and `AuthDbConst` are.
3. In `MigrationWorker.cs`, add `succeeded &= await TryMigrateAsync<YourDbContext>("YourDbContext", stoppingToken);` next to the existing calls.
4. Assert the new history table in `tests/Jobs/Cvitlo.Migrations.IntegrationTests/MigrationWorkerTests.cs`.
