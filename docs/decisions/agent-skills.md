# Agent skills

**Date:** 2026-11-12
**Status:** Adopted (issue #111)

Which agent skills Faber keeps, how they are named and tiered, how an issue becomes a PR, and how
tests are named. Other tasks of #111 apply these decisions to the files; this record is the reason
behind them.

## Criteria

A skill stays only if it:

- describes the stack we actually use;
- does not contradict our conventions;
- is worth its context cost;
- comes from a maintained source whose license allows vendoring into this Apache-2.0 repo.

## Naming rule

Project-authored skills have plain, general names that say what they cover: `fastendpoints`,
`error-or`, `feature-template`, `keycloak`, `backend-patterns`, `github-workflow`,
`implement-issue`, `code-comments`. There is no project prefix. A `faber-` prefix was tried and dropped because the
names read worse and the repo already scopes them. Agents keep `faber-implementer` and
`faber-reviewer`, because plugin agents such as `code-reviewer` would otherwise collide with them.

Consequence: a skill that is not in `skills-lock.json` is project-authored, and it must be listed
in the skills table in `AGENTS.md`. Every other skill must be in `skills-lock.json`. Before a new
project skill is named, check that no vendored or plugin skill uses the same name.

## Decisions per skill

"Tier after" is the tier in `.claude/settings.json` once the decision is applied.

| # | Skill | Source | License | Decision | Tier after | Reason |
|---|---|---|---|---|---|---|
| 1 | `angular-component` | analogjs/angular-skills | MIT | keep | on | |
| 2 | `angular-di` | analogjs/angular-skills | MIT | keep | on | |
| 3 | `angular-forms` | analogjs/angular-skills | MIT | drop | — | Teaches Signal Forms; the app uses reactive forms |
| 4 | `angular-http` | analogjs/angular-skills | MIT | keep | on | |
| 5 | `angular-routing` | analogjs/angular-skills | MIT | keep | on | |
| 6 | `angular-signals` | analogjs/angular-skills | MIT | keep | on | |
| 7 | `angular-testing` | analogjs/angular-skills | MIT | keep | on | Vitest, matches `ng test` |
| 8 | `angular-tooling` | analogjs/angular-skills | MIT | keep | name-only | |
| 9 | `api-design` | aaronontheweb/dotnet-skills | MIT | drop | — | Library public-API and binary-compat design; we ship an app |
| 10 | `aspire` | github/awesome-copilot | MIT | keep | name-only | |
| 11 | `aspire-configuration` | aaronontheweb/dotnet-skills | MIT | keep | name-only | |
| 12 | `aspire-deployment` | Microsoft, via #89 | MIT | keep, add to lock | user-invocable-only | Only on explicit request |
| 13 | `aspire-init` | Microsoft, via #89 | MIT | drop | — | One-time setup, already done |
| 14 | `aspire-integration-testing` | aaronontheweb/dotnet-skills | MIT | keep | name-only | Used by `AppHostModelTests` |
| 15 | `aspire-monitoring` | Microsoft, via #89 | MIT | keep, add to lock | name-only | |
| 16 | `aspire-orchestration` | Microsoft, via #89 | MIT | keep, add to lock | name-only | |
| 17 | `aspire-service-defaults` | aaronontheweb/dotnet-skills | MIT | keep | name-only | |
| 18 | `configuring-opentelemetry-dotnet` | dotnet/skills | MIT | drop | — | Duplicates `opentelemetry-net-instrumentation`; mentions Serilog |
| 19 | `cqrs-implementation` | wshobson/agents | MIT | drop | — | Pattern we do not use (549 lines) |
| 20 | `csharp-docs` | github/awesome-copilot | MIT | drop | — | Was auto-on and demands XML docs on everything |
| 21 | `dependency-injection-patterns` | aaronontheweb/dotnet-skills | MIT | keep | name-only | |
| 22 | `dotnet-backend-patterns` | Rewritten from wshobson/agents | Faber | rename to `backend-patterns`, Faber-authored, leave lock | name-only | Already a Faber rewrite; 1279 lines is too heavy for `on`; overlap with `.claude/rules/backend-patterns.md` goes to #113 |
| 23 | `dotnet-testing-advanced-aspire-testing` | kevintsengtw/dotnet-testing-agent-skills | MIT | drop | — | Written in Chinese; duplicates #14 |
| 24 | `dotnet-testing-advanced-testcontainers-database` | kevintsengtw/dotnet-testing-agent-skills | MIT | drop | — | Written in Chinese; duplicates #48 |
| 25 | `dotnet-testing-advanced-xunit-upgrade-guide` | kevintsengtw/dotnet-testing-agent-skills | MIT | drop | — | Written in Chinese; migration done, Moq and FluentAssertions |
| 26 | `dotnet-testing-bogus-fake-data` | kevintsengtw/dotnet-testing-agent-skills | MIT | drop | — | Written in Chinese |
| 27 | `dotnet-testing-fluentvalidation-testing` | kevintsengtw/dotnet-testing-agent-skills | MIT | drop | — | Written in Chinese |
| 28 | `dotnet-testing-nsubstitute-mocking` | kevintsengtw/dotnet-testing-agent-skills | MIT | drop | — | Written in Chinese; Moq examples |
| 29 | `dotnet-testing-test-data-builder-pattern` | kevintsengtw/dotnet-testing-agent-skills | MIT | drop | — | Written in Chinese |
| 30 | `dotnet-testing-test-naming-conventions` | kevintsengtw/dotnet-testing-agent-skills | MIT | drop | — | Written in Chinese; contradicts our naming rule, suggests Chinese names |
| 31 | `efcore-patterns` | aaronontheweb/dotnet-skills | MIT | keep | name-only | |
| 32 | `errorOr-patterns` | Faber | Apache-2.0 | rename to `error-or` | on | |
| 33 | `feature-template` | Faber | Apache-2.0 | keep | on | |
| 34 | `faber-module-bootstrapping` | Faber | Apache-2.0 | drop (owner's call) | — | Modules exist; the migration and DbContext registration procedure moves to `docs/operations/migrations.md` |
| 35 | `fastendpoints` | Faber | Apache-2.0 | keep | on | |
| 36 | `find-skills` | vercel-labs/skills | MIT | keep | user-invocable-only | |
| 37 | `github-workflow` | Faber | Apache-2.0 | keep | name-only | |
| 38 | `impl-gh-issue` | Faber | Apache-2.0 | rename to `implement-issue` | user-invocable-only | |
| 39 | `keycloak-aspnet-integration` | Faber | Apache-2.0 | rename to `keycloak` | on | |
| 40 | `mailpit-integration` | aaronontheweb/dotnet-skills | MIT | keep | name-only | |
| 41 | `microsoft-extensions-configuration` | aaronontheweb/dotnet-skills | MIT | keep | name-only | Options Pattern |
| 42 | `migrate-xunit-to-xunit-v3` | dotnet/skills | MIT | drop | — | Migration done |
| 43 | `modern-csharp-coding-standards` | aaronontheweb/dotnet-skills | MIT | keep | name-only | |
| 44 | `opentelemetry-net-instrumentation` | aaronontheweb/dotnet-skills | MIT | keep | name-only | |
| 45 | `optimizing-ef-core-queries` | dotnet/skills | MIT | keep | name-only | |
| 46 | `tailwind-design-system` | wshobson/agents | MIT | keep | name-only | |
| 47 | `tdd` | mattpocock/skills | MIT | keep | on | |
| 48 | `testcontainers-integration-tests` | aaronontheweb/dotnet-skills | MIT | keep | name-only | |
| 49 | `vertical-slice` | codewithmukesh/dotnet-claude-kit | MIT | drop | — | Built on MediatR; `feature-template` covers slices |
| 50 | `dotnet-slopwatch` | aaronontheweb/dotnet-skills | MIT | adopt | name-only | Catches disabled tests, suppressed warnings, empty catches; the `Slopwatch.Cmd` tool is a follow-up |
| 51 | `grilling` | mattpocock/skills | MIT | adopt | name-only | Stress-tests a plan in numbered rounds |
| 52 | `grill-me` | mattpocock/skills | MIT | adopt | user-invocable-only | `/grill-me` entry to `grilling` |
| 53 | `codebase-design` | mattpocock/skills | MIT | adopt | name-only | Deep-module vocabulary |
| 54 | `improve-codebase-architecture` | mattpocock/skills | MIT | adopt | user-invocable-only | Architecture review before the #110 refactors |
| 55 | `ub-mode` | unclebob/ub-mode (Robert C. Martin) | MIT | adopt | user-invocable-only | Its `crapper`, `dryer` and `mutator` tools are not .NET; follow-up |
| 56 | `skill-creator` | anthropics/skills | Apache-2.0 (LICENSE.txt in the skill directory; the repo has no top-level license) | adopt | user-invocable-only | Vendored so Codex and Junie get it too |
| 57 | `simplify` | Claude Code built-in | — | keep | on | |
| 58 | `code-comments` | Faber | Apache-2.0 | add | name-only | Replaces `csharp-docs` (#113): the commenting rule with examples from this repo, used for sweeps and review |

Result: 40 project skills plus the built-in `simplify`. The other built-ins (`update-config`,
`keybindings-help`, `fewer-permission-prompts`, `loop`, `schedule`, `claude-api`, `init`, `review`,
`security-review`) stay `user-invocable-only`.

## Tier policy

- `on`: skills that must apply every time. The Faber skills for our deliberate choices, the Angular
  skills for the stack, `tdd` and `simplify`.
- `name-only`: on-stack reference the model loads by name when relevant.
- `user-invocable-only`: workflows and one-off tools the user triggers.

`.claude/settings.json` is the single source of truth for tiers. Plugin-namespaced overrides
(`plugin:skill`) depend on plugins an individual developer has installed, so they go in the ignored
`.claude/settings.local.json`, not in the tracked file.

## Candidate evaluation

`wshaddix/dotnet-skills` has no license file (GitHub API `license: null`, last push 2026-02-27), so
none of its skills, agents or commands can be vendored: skills `dotnet-xunit`,
`dotnet-testing-strategy`, `dotnet-integration-testing`, `dotnet-agent-gotchas`,
`dotnet-csharp-code-smells`, `dotnet-structured-logging`, `dotnet-architecture-patterns`,
`dotnet-xml-docs`; agents `dotnet-code-review-agent`, `dotnet-testing-specialist`; commands
`code-review`, `test-verify-backend`.

From the MIT upstream `aaronontheweb/dotnet-skills`:

- `testcontainers`: already have it.
- `slopwatch`: adopted as `dotnet-slopwatch`, its upstream name.
- `project-structure`: rejected. It teaches SourceLink and `RELEASE_NOTES.md`; we use release-please.

Other rejected candidates:

- `wondelai/skills` `clean-code`: overlaps `ub-mode` and teaches a comment style that #113 owns.
- `wondelai/skills` `clean-architecture` and `wshobson/agents` `architecture-patterns`: layered or
  hexagonal, which contradicts vertical slices.
- `mattpocock/skills` `domain-modeling`: writes `GLOSSARY.md` and `docs/adr/`; we use `docs/decisions/`.
- `wshobson/agents` `architecture-decision-records`: its own ADR format.

## Gaps

Faber skills still to write:

- `testing` (or a more specific name, chosen when it is written): NSubstitute, Shouldly, xUnit v3, Testcontainers fixtures and the naming rule below.
  Written after #118.

Follow-ups:

- Pin `Slopwatch.Cmd` in `.config/dotnet-tools.json`.
- Decide on ub-mode's tools or a .NET equivalent (for example aaronontheweb `crap-analysis`).

## Test naming

One rule for every test:

- Class: `{Subject}Tests`.
- Method: `{Subject}_{Condition}_Should{Outcome}`.
- `{Condition}` is omitted only for configuration and invariant tests.
- No `DisplayName`.

Test names in vendored skills' code samples are illustrations, not guidance; this rule wins.

Examples:

- `UpdateSkillTests.UpdateSkill_NoToken_ShouldReturnUnauthorized`
- `UpdateSkillTests.UpdateSkill_ValidRequest_ShouldModifyFields`
- `ResumesCacheKeysTests.Pdf_AcronymInTemplateName_ShouldKeepAcronymTogether`
- `SessionResilienceTests.Refresh_KeycloakTimesOut_ShouldKeepTheSession`
- `RealmSecurityTests.WebClient_ShouldRequirePkce`

Chosen over `Should_When`, `Given_When_Then` and plain sentences because it is the most widely used
.NET convention (Osherove's `UnitOfWork_StateUnderTest_ExpectedBehavior`). The details live in
`AGENTS.md` and `.claude/rules/backend-patterns.md`. Existing tests are renamed in #118.

## Workflow: issue to PR

| Step | Owner |
|---|---|
| Triage and tier | `/implement-issue <N>` (tiers 1-3) |
| Plan (tier 2-3) | The session writes `.claude/plans/{YYYY-MM-DD}-{issue}-{slug}.md` (gitignored; replaces `docs/plans/`, which #113 deletes). Optional `/grill-me` stress-tests the plan; `superpowers:brainstorming` only at tier 3 when scope is ambiguous |
| Approval | The user, explicitly |
| Implementation | One `faber-implementer` subagent per task, foreground, in order |
| Review | Tier 2: the session reviews each diff. Tier 3: `faber-reviewer` per task plus a fix loop |
| Branch review | Built-in `code-review` skill |
| PR | Only on the user's confirmation, following `github-workflow` |

Not used: the wshaddix agents and commands (no license). The other superpowers execution skills are
not part of this path.

## Vendored skills

- Vendored skills keep their upstream name, so `skills update` never renames a dir that `.claude/settings.json` points at. `opentelemetry-net-instrumentation` is the one old exception: its frontmatter says `OpenTelemetry-NET-Instrumentation`.
- A local edit to a vendored skill is allowed only when it points the skill at this repo. Every one is listed here and must be re-applied after an update. The lock hash covers the local files.
  - `tdd`: the refactor checklist points at `AGENTS.md` and `.agents/rules/backend-patterns.md`.
  - `aspire`: a note under the frontmatter says that `aspire-init` and `aspireify` are not vendored.
- `aspire-deployment`, `aspire-monitoring` and `aspire-orchestration` were refreshed to `microsoft/aspire-skills` HEAD (2026-11-12) when they joined the lock. The new content adds deployment targets such as Radius. We don't use them, and `aspire-deployment` stays `user-invocable-only`.
- Known upstream issue: `skill-creator/eval-viewer/generate_review.py` serves `POST /api/feedback` on 127.0.0.1 with no Host or Origin check. A page open in the same browser during an eval review could overwrite that run's `feedback.json`. It is not patched locally, because that would break the lock hash. Report it upstream.

## Single source of truth

Six places must agree:

1. `.agents/skills/` (storage) and the `.claude/skills/` symlinks into it.
2. `skills-lock.json`.
3. `.claude/settings.json`.
4. This document.
5. The skills table in `AGENTS.md`.
6. `THIRD_PARTY_NOTICES.md`: one row per upstream source in the lock, with its license link and copyright holder. `scripts/verify-third-party-notices.sh` checks it against the lock in CI.

Adding or removing a skill updates all six in one commit.
