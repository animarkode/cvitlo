# Faber Agent Guide

This is the single instruction file for Claude Code, Codex and JetBrains Junie. Claude Code
also loads `.claude/rules/*` (symlinks to `.agents/rules/`) when it reads files under `src/api/`,
`src/web/` or `tests/`.

## Project

Faber is a full-stack CV/resume builder:

- Backend: ASP.NET Core 8, FastEndpoints, EF Core/PostgreSQL, Redis, Keycloak,
  and OpenTelemetry.
- Sign-in: Keycloak 26.8 hosted login (password, passkeys, Google, GitHub); the
  API is a backend-for-frontend that keeps tokens in a server-side session and
  gives the browser an HttpOnly cookie. The realm is code
  (`infra/keycloak/config/faber.json`, applied by keycloak-config-cli on every
  start). See `docs/operations/keycloak.md`.
- Frontend: Angular 21 standalone components, Angular Material, Tailwind CSS,
  and signal-based state.
- Local orchestration: .NET Aspire 13.5.3. The `Aspire.AppHost.Sdk` version in
  `Faber.AppHost.csproj` must be bumped together with the `Aspire.*` packages
  (dependabot cannot see it) and the local Aspire CLI must be at least that version.

## Working conventions

- Preserve unrelated working-tree changes; keep changes scoped to the request.
- Do not commit credentials or other secrets.
- Secrets: code reads only `IConfiguration` (Options Pattern) and does not know
  where a value comes from. In development every secret is an AppHost parameter
  kept in its user-secrets (`Parameters:<name>`); generated ones need no manual
  step, and only external ones (Google, GitHub) are set by hand. In production
  the deployment injects every secret as an environment variable from the
  platform's secret store. Never set a secret in both places at once, and never
  in a shell profile.

## Git and PRs

- Commits and PR titles use Conventional Commits: `type(scope): description`.
- PR `type` is one of `feat`, `fix`, `perf`, `refactor`, `chore`, `test`, `docs`; `scope`
  is required and names the affected area, such as `resumes`. After creating or editing a
  PR with `gh`, verify the title with `gh pr view --json title`.
- Branches: `{issue-number}-{kebab-case-description}`.
- Split changes into commits by logical concern.
- Never add a `Co-Authored-By` trailer or any other AI attribution to commits or PR bodies.
- Labels, release-please and templates: `faber-github-workflow`.

## Common commands

Build the API project (or the whole solution with `Faber.sln`).

```bash
dotnet build src/api/Faber.Api/Faber.Api.csproj
```

Run all services through Aspire. The dashboard opens at a `https://localhost:15xxx` port shown in the console.

```bash
dotnet run --project src/api/Aspire/Faber.AppHost
```

Run an integration test project (Docker required).

```bash
DOCKER_HOST="${DOCKER_HOST:-unix:///var/run/docker.sock}" dotnet run --project tests/Integration/Modules/Auth/Faber.Modules.Auth.Application.Tests
```

Run one xUnit v3 test.

```bash
dotnet run --project <test.csproj> -- -method "*.TestName"
```

Frontend commands run from `src/web/faber-app`: `ng serve`, `ng build`, `ng test`.

## Migrations

`Faber.Migrations` applies every module's EF Core migrations. In development the AppHost runs
it once Postgres is up; in production its container runs before the API container. `dotnet ef`
is pinned in `.config/dotnet-tools.json`; run `dotnet tool restore` first. Adding a migration,
registering a module `DbContext` and the deploy flow: `docs/operations/migrations.md`.

## Workflow

Implement a GitHub issue with `/faber-impl-issue <N>`: it sizes the issue into a tier, writes
the plan to `.claude/plans/` (tiers 2-3), runs one `faber-implementer` subagent per task,
reviews, runs `code-review` on the branch, and opens a PR only when the user confirms.
`docs/decisions/agent-skills.md` has the full table and the skill inventory rules: six places
must agree, `faber-*` skills are Faber-authored, and every other skill is in `skills-lock.json`.

## Skills

| When | Skill | File |
|---|---|---|
| Creating any Endpoint, Group, or Validator | `faber-fastendpoints` | `.agents/skills/faber-fastendpoints/SKILL.md` |
| Working with ErrorOr, mapping errors to HTTP responses | `faber-erroror` | `.agents/skills/faber-erroror/SKILL.md` |
| Adding a feature to an existing module | `faber-feature-template` | `.agents/skills/faber-feature-template/SKILL.md` |
| Sign-in, sessions, the realm config, Keycloak | `faber-keycloak` | `.agents/skills/faber-keycloak/SKILL.md` |
| Backend module, EF Core, DI or integration-test patterns | `faber-backend-patterns` | `.agents/skills/faber-backend-patterns/SKILL.md` |
| Creating/labeling issues, opening PRs, releases | `faber-github-workflow` | `.agents/skills/faber-github-workflow/SKILL.md` |
| Implementing a GitHub issue end-to-end (user-invoked) | `faber-impl-issue` | `.agents/skills/faber-impl-issue/SKILL.md` |

In Claude Code load a skill with the `Skill` tool by name; other tools read the file. Skill
tiers live in `.claude/settings.json`.

## Backend: `src/api/` and `tests/`

- Modular monolith with vertical slices. Add a feature under the owning module's
  `Application/Features/{Feature}` directory.
- Typical files: endpoint, request/response records, command, command handler,
  mapper, and validator. Groups belong in `Groups/`.
- Keep cross-module contracts in `{Module}.PublicApi`; implementations,
  features, and groups belong in `{Module}.Application`.
- Use file-scoped namespaces, records for DTOs and commands, primary
  constructors for DI, `ErrorOr<T>` for expected results, static mapper classes,
  guard clauses, `CancellationToken` as the last async parameter, and the
  Options Pattern.
- Do not introduce AutoMapper, a repository layer over EF Core, exceptions for
  ordinary control flow, regions, or `if`/`else` chains.
- Tests use xUnit v3, Shouldly, NSubstitute, Bogus, and Testcontainers. Mock
  external services only (such as email), never infrastructure or internal abstractions.
- Test names: class `{Subject}Tests`; method `{Subject}_{Condition}_Should{Outcome}`;
  omit `{Condition}` only for configuration/invariant tests; no `DisplayName`. Examples:
  `UpdateSkill_NoToken_ShouldReturnUnauthorized`, `UpdateSkill_ValidRequest_ShouldModifyFields`,
  `RealmSecurityTests.WebClient_ShouldRequirePkce`.
- Full reference: `.agents/rules/backend-patterns.md`.

## Frontend: `src/web/faber-app/`

- Keep app-wide singletons in `core/`, reusable `fb-` components in `shared/`,
  and lazy-loaded areas in `modules/`.
- Use standalone components, `OnPush`, `inject()`, signals, native control flow,
  reactive forms, separate HTML/CSS files, and `NgOptimizedImage` for static
  images. Scaffold with `ng generate`.
- Use `httpResource()` for GET reads; use `HttpClient` observables for
  mutations. Do not add NgRx.
- Avoid `any`, inline templates/styles, constructor-based initialization,
  template arrow functions or complex template logic, and
  `@HostBinding`/`@HostListener`.
- Maintain WCAG AA and AXE compliance.
- Full reference: `.agents/rules/frontend-patterns.md`.

## Before changing code

Follow the nearest applicable instructions and existing local patterns, and read the matching
skill from the table above before writing code.
