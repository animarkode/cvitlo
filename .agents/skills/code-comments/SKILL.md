---
name: code-comments
description: The Faber commenting rule with bad/good examples from this repo. Use when writing or reviewing comments in C# or TypeScript, XML docs or TSDoc, and for comment sweeps.
---

# Code comments — Faber

The rule lives in `AGENTS.md` (`## Comments`), `.agents/rules/backend-patterns.md` and
`.agents/rules/frontend-patterns.md`. Read it there; this skill only adds the examples and the build traps.

In one line: a comment says what the code cannot (a why, a constraint, a workaround with a link, a
non-obvious order or invariant). Anything else is deleted.

## Where `///` and TSDoc are allowed

- C#: `///` only on `PublicApi` contracts and `Common`. Everywhere else use no `///`; use a `//` line only when it earns its place.
- TypeScript: TSDoc (`/** */`) only on public inputs and outputs of `shared/` components.
- Plain `//` is allowed anywhere, one to three lines. Longer goes to `docs/decisions/` and the comment links it.

## Build constraints (warnings are errors)

- RCS1139: a `///` block needs `<summary>`.
- CS1573: once one `<param>` is on a member, every parameter needs one. A record's positional parameters count. Prefer none.
- CS1572 / CS1574: a `<param>` for a name that does not exist, or a bad `cref`, fails the build.
- Removing a `<param>`: remove all `<param>`s of that member, never just one.
- Deleting a whole `///` block breaks nothing.

## Bad -> good

1. Param restates the name. `src/api/Modules/Resumes/Cvitlo.Resumes.Application/Caching/ResumesCacheKeys.cs:18`
   - Bad: `/// <param name="userId">The id of the user whose resume list is cached.</param>`
   - Good: delete the whole block (lines 17-19): the class is in `Application`, not `PublicApi`, so no `///` belongs there. A why worth keeping, like the `Build` field's summary on line 14 (the build id busts stale entries), becomes a `//` line.
2. Summary restates the type, outside `PublicApi`. `src/api/Modules/Auth/Cvitlo.Auth.Application/Features/Account/UnlinkAccount/UnlinkAccountRequest.cs:3`
   - Bad: `/// <summary>Represents a request to unlink an identity provider.</summary>`
   - Good: delete the whole block (lines 3-4); the type name already says it.
3. Record with a `<param>` per property, outside `PublicApi`. `src/api/Modules/Auth/Cvitlo.Auth.Application/Features/Account/GetProfile/GetProfileResponse.cs:3-9`
   - Bad: `/// <summary>Describes the user's profile.</summary>` followed by `/// <param name="FirstName">The user's first name.</param>` and five more like it.
   - Good: delete the whole block (lines 3-9), `<summary>` included; it is an `Application` type. If one property is truly non-obvious, say it in a `//` on that line.
4. Comment restates the code. `src/web/faber-app/src/app/core/services/theme.ts:74`
   - Bad: `// cookies unavailable` inside `catch { }`
   - Good: delete, or say what happens and why: `// Private mode can block cookies; the theme then just is not remembered.`
5. Pointer to code that is already clear. `src/web/faber-app/src/app/shared/components/fb-accordion-item/fb-accordion-item.ts:65`
   - Bad: `// Single-open enforcement happens reactively in the constructor effect().`
   - Good: delete; the `effect()` has its own why-comment.
6. Change history. `src/web/faber-app/src/app/modules/resumes/builder/builder-actions/builder-actions.spec.ts:75`
   - Bad: `// #425 — the button used to be disabled until a preview blob existed, ...`
   - Good: keep only the present-tense fact: `// The store renders a preview on demand, so the button is enabled without a cached one.`
7. Commented-out code: none is left in the repo today. Any `// var x = ...` or `// if (...)` line is deleted; git keeps it.

## Keep these (good comments)

- Workaround with the issue: `src/web/faber-app/src/app/shared/components/fb-rich-text/fb-rich-text.ts:228-230` explains why `emitUpdate:false` is needed (Tiptap fires `onUpdate`, which dirties the control, #423).
- Order / consequence: `src/api/Modules/Auth/Cvitlo.Auth.Application/Sessions/SessionRefresher.cs:121` says why the refused token is not handled here (`ValidateAsync` ends the session).
- Public contract with an invariant: `src/api/Modules/Auth/Cvitlo.Auth.PublicApi/Events/UserDeleted.cs:8-10` (the event may arrive more than once, so handlers must be idempotent).

## Review checklist

Flag a comment that:
- repeats a name, type or signature;
- has `<param>`/`<returns>` that add nothing, or only some params on a member;
- is a `///` outside `PublicApi` / `Common`, or TSDoc outside `shared/` inputs and outputs;
- tells history ("used to", "was", "changed in #N") or is commented-out code;
- is longer than three lines and should be a `docs/decisions/` link;
- is now wrong because the code changed (fix or delete, never leave it).

Do not flag a comment that gives a why, a workaround link, or an invariant. When unsure, keep it and report it.
