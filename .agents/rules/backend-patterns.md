---
paths:
  - "src/api/**/*.cs"
  - "tests/**/*.cs"
---

# Backend Patterns — Faber

## Module Feature Structure

Canonical example: `src/api/Modules/Resumes/Faber.Modules.Resumes.Application/Features/Educations/CreateEducation/`

```text
Features/{Feature}/
├── {Feature}Endpoint.cs        # FastEndpoints endpoint (primary constructor for DI)
├── {Feature}Request.cs         # record
├── {Feature}Response.cs        # record
├── {Feature}Command.cs         # record implementing ICommand<ErrorOr<TResponse>>
├── {Feature}CommandHandler.cs  # extends CommandHandlerWithCatchError
├── {Feature}Mapper.cs          # static class with extension methods
└── {Feature}Validator.cs       # extends Validator<TRequest>
```

**Module boundaries:**
- `{Module}.PublicApi` — `I{Module}ModuleApi` interface (cross-module contract)
- `{Module}.Application` — `{Module}ModuleApi` implementation, features, groups
- Groups: `Groups/{Module}Group.cs` — extends FastEndpoints `Group`

**Module shape (not every module fits the four-project template):**
- `Documents` and `Notifications` are small integration-style modules with no Domain or Infrastructure project.
- `Auth` has no Domain project: its persisted types (sessions, keys) are infrastructure.
- `Resumes` and `Auth` read required configuration at registration and fail on start when it is missing.
- Existing project references are not perfectly pure: prefer `PublicApi` boundaries and check neighbouring precedent first.
- A module that owns a `DbContext` must also be registered with `Faber.Migrations` (see `docs/operations/migrations.md`).

## Naming Conventions

| Concept            | Pattern                                    | Example                                    |
|--------------------|--------------------------------------------|--------------------------------------------|
| Endpoint           | `{Feature}Endpoint`                        | `CreateEducationEndpoint`                  |
| Request / Response | `{Feature}Request` / `{Feature}Response`   | `CreateEducationRequest` / `...Response`   |
| Command            | `{Feature}Command`                         | `CreateEducationCommand`                   |
| Command Handler    | `{Feature}CommandHandler`                  | `CreateEducationCommandHandler`            |
| Validator          | `{Feature}Validator`                       | `CreateEducationValidator`                 |
| Mapper             | `{Feature}Mapper`                          | `CreateEducationMapper`                    |
| Group              | `{Module}Group`                            | `ResumesGroup`                             |
| Module API         | `I{Module}ModuleApi` / `{Module}ModuleApi` | `IResumesModuleApi` / `ResumesModuleApi`   |
| Tests              | `{Feature}Tests`                           | `CreateEducationTests`                     |
| Test data          | `{Purpose}Data` / `{Feature}Constants`     | `RegisteredUsersData` / `{Feature}Constants` |
| Options            | `{Name}Options` / `{Name}OptionsSetup`     | `KeycloakOptions` / `KeycloakOptionsSetup` |

## C# Patterns

**Use:** records for DTOs/commands/requests/responses · primary constructors for DI · file-scoped namespaces · `ErrorOr<T>` for results · static mapper classes · guard clauses / early returns · `CancellationToken ct` as last async parameter · Options Pattern with `IOptions<T>` / `IConfigureOptions<T>` · curly braces `{}` for all `if` blocks · `///` only on `PublicApi` and `Common` (see Comments).

**Avoid:** AutoMapper · repository pattern over EF Core · exceptions for flow control · `#region`/`#endregion` · `if-else` chains.

## Comments

- A comment says only what the code cannot: why, a constraint, a workaround with a link, a non-obvious order or invariant.
- Never restate a name or a signature. No change history ("was X, changed in #123"), no commented-out code.
- One to three lines. A longer explanation goes to `docs/decisions/` and the comment links to it.
- `///` only on `PublicApi` contracts and `Common`. When written it needs a `<summary>` (RCS1139 fails the build).
- `<param>`/`<returns>` only when they add information. Once one `<param>` exists on a member (a record's positional parameters included), every parameter needs one (CS1573); prefer none. A wrong `<param>` or bad `cref` fails the build (CS1572, CS1574).

## Logging

- Endpoints do not log: the request pipeline (ASP.NET Core + OpenTelemetry) already records method, path, status and duration.
- Handlers log work with external systems (Keycloak, cache, background sweeps, erasure) and unexpected failures.
- Expected results (`NotFound`, validation errors) are not logged as warnings.
- No `[START]`/`[SUCCESS]`/`[STEP]`/`[FAIL]`/`[HTTP ...]` markers. Never log credentials, tokens or e-mail addresses.

## Tests

**Test class pattern:** `[Collection<CollectionResumes>] [Priority(N)] public class {Feature}Tests(WebApp app) : TestBase`

**Test naming:**
- **Class:** `{Subject}Tests`, where the subject is what is tested: a feature, a production class, a behaviour, or a configuration (`UpdateSkillTests`, `ResumesCacheKeysTests`, `SessionResilienceTests`, `RealmSecurityTests`).
- **Method:** `{Subject}_{Condition}_Should{Outcome}`
  - Subject: what acts — the endpoint action, the method, the actor, or the configured part.
  - Condition: in which case. Always written, the happy path included (`ValidRequest`).
  - Outcome: what must happen, after `Should`.
  - Leave out `{Condition}` only when the outcome holds unconditionally (configuration and invariant tests).

| Subject | Example |
|---|---|
| Feature | `UpdateSkillTests.UpdateSkill_NoToken_ShouldReturnUnauthorized` |
| Feature, happy path | `UpdateSkillTests.UpdateSkill_ValidRequest_ShouldModifyFields` |
| Production class | `ResumesCacheKeysTests.Pdf_AcronymInTemplateName_ShouldKeepAcronymTogether` |
| Behaviour | `SessionResilienceTests.Refresh_KeycloakTimesOut_ShouldKeepTheSession` |
| Configuration | `RealmSecurityTests.WebClient_ShouldRequirePkce` |

- **No `DisplayName`** on `[Fact]`/`[Theory]`: the method name is the only name (a second name drifts; `-method`/`--filter` match the method name; xUnit shows `[Theory]` row values).
- Existing tests are migrated to this rule in #118; rationale in `docs/decisions/agent-skills.md`.

**Test file structure:** `Features/{Feature}/{Feature}Tests.cs`, `{Feature}Constants.cs` (imported via `using static`), `Data/{Description}Data.cs` + `{Feature}Faker.cs`

**Guidelines:**
- Testcontainers for all infrastructure — no shared or persistent test databases
- Mock only external services (email), not internal abstractions
- `ClassData` > `InlineData` — use Bogus for generated data; `InlineData` only for literals
- Data classes implement `IEnumerable<TheoryDataRow<T>>` with seeded fakers for deterministic data
- Register record types used in `TheoryDataRow` via `RecordSerializer` (`[assembly: RegisterXunitSerializer]`)
- Shouldly over FluentAssertions; NSubstitute over Moq
- Keycloak fields (username, email): `ShouldBe(expected, StringCompareShould.IgnoreCase)`
- `CancellationToken` via `TestContext.Current.CancellationToken`

---
Expanded guidance and examples: `backend-patterns`, `fastendpoints`, `error-or`.
