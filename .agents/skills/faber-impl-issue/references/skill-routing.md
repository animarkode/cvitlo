# Skill routing — `area:*` → project skills

Read when composing a task brief. Pick from the rows the task actually touches, then **verify every name against the currently available skills list**. Never invent a skill name; write `None` when nothing applies. A brief that lists a skill the session cannot load wastes the implementer's first tool call.

| Label | Backend / infra skills | Notes |
|---|---|---|
| `area:auth` | `faber-keycloak`, `faber-fastendpoints`, `faber-erroror`, `faber-feature-template` | Security-sensitive: prefer more deliberation, not a different model |
| `area:identity` | `faber-keycloak`, `faber-backend-patterns` | Realm-export changes come in pairs — infra and test |
| `area:users` | `faber-feature-template`, `faber-backend-patterns`, `faber-fastendpoints`, `faber-erroror` | |
| `area:resumes` | `faber-feature-template`, `faber-backend-patterns`, `faber-fastendpoints`, `faber-erroror`, `efcore-patterns` | Add `angular-*` rows below when the slice is full-stack |
| `area:notifications` | `mailpit-integration`, `faber-backend-patterns` | |
| `area:infra` | `aspire`, `aspire-configuration`, `aspire-service-defaults` | Ports are hard-pinned in `AppHost/Program.cs` |
| `area:tests-infra` | `tdd`, `testcontainers-integration-tests`, `aspire-integration-testing`, `dotnet-slopwatch` | `faber-testing` replaces this row after #118 |

| Label | Frontend skills |
|---|---|
| `area:ui-shared` | `angular-component`, `tailwind-design-system` |
| `area:ux` | `tailwind-design-system` |
| any frontend task | add by what it touches: `angular-signals` (state), `angular-http` (API calls), `angular-routing` (routes/guards), `angular-di` (services/tokens), `angular-testing` (specs); reactive forms follow `.agents/rules/frontend-patterns.md` |

## Cross-cutting

- New module from scratch → `docs/operations/migrations.md` (DbContext registration) and `.agents/rules/backend-patterns.md`.
- Refactor or module redesign → `codebase-design` (vocabulary); `/improve-codebase-architecture` is user-invoked.
- After a .NET change → `dotnet-slopwatch`.
- Any new .NET feature or bug fix → `tdd`, ahead of the domain skills.
- A task that only edits markdown, config, or CI → `None`.

Keep the list short. Three skills an implementer actually reads beat six it skims.
