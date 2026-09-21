# Implementation Plan: Make the pages read what the server actually sends

**Branch**: `005-page-json-casing` | **Date**: 2026-09-20 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/005-page-json-casing/spec.md`

## Summary

Both embedded pages are blank on Jellyfin 12 because they read one set of field names and the host
sends another. The host offers either naming and picks by what the caller asks for; the plugin's
endpoints ask for neither, so they inherit a default the pages cannot read.

**The plugin states the naming for its own responses.** Each JSON-returning controller declares
`[Produces(JsonDefaults.CamelCaseMediaType)]` — the host's own negotiation handle, measured in the
`Jellyfin.Extensions` 12.0.0 metadata as `application/json; profile="CamelCase"`. The pages are not
changed to compensate. A reflection test enforces the declaration structurally, so an endpoint added
later without it fails the suite rather than a person's browser.

Alongside it the feature settles the convention that would have prevented the defect: PascalCase
route segments with no `api` segment, one `const` source for the route prefix, and a recorded
document a future author finds before adding an endpoint. `001`'s and `002`'s API contracts are
amended to the renamed routes.

The test that makes recurrence visible is a **committed JSON fixture per response**, asserted from
both sides: C# serializes the DTO through the host's own `JsonDefaults.CamelCaseOptions` and matches
the fixture's names; node runs the pages' `render` and `renderStatus` against the same fixture in a
string-capturing fake DOM written in this repository.

## Technical Context

**Language/Version**: C# on `net10.0`; ES5-style JavaScript in the embedded pages; Node 22 for the
page tests.

**Primary Dependencies**: `Jellyfin.Controller` / `Jellyfin.Model` / `Jellyfin.Data` /
`Jellyfin.Database.Implementations` 12.0.0 (`ExcludeAssets=runtime`), `Microsoft.Data.Sqlite`
10.0.11. **No new direct dependency.** `Jellyfin.Extensions` 12.0.0, which carries `JsonDefaults`,
already resolves transitively through `Jellyfin.Controller` (`packages.lock.json` line 45).

**Storage**: unchanged. `FR-009` forbids any change to stored data or server-side behaviour beyond
the naming of responses.

**Testing**: xunit 2.9.3 + NSubstitute 5.3.0 for C#; `node:test` + `node:assert` + `node:vm` for the
pages. No assertion library, no browser framework, no `package.json` (`FR-017`, and `002` recorded
the absence as a deliberate constraint).

**Target Platform**: Jellyfin 12.0.x server, verified on a 12.1.0 install.

**Project Type**: Jellyfin server plugin with two embedded web pages.

**Performance Goals**: none. Nothing in this feature is on a hot path.

**Constraints**: hermetic tests — no network, no installation step, no Jellyfin server
(constitution III, `FR-017`, `SC-009`). `TreatWarningsAsErrors` stays on. The web UI has no build
step, so neither page can read a C# constant at load time.

**Scale/Scope**: 3 controllers, 11 routes, 13 DTO records, 37 distinct field names read across two
pages, 2 contract documents amended.

## Constitution Check

*Checked against `.specify/memory/constitution.md` v1.3.0. Re-checked after Phase 1 design; verdicts
unchanged.*

| Principle | Verdict | Evidence |
| --- | --- | --- |
| **I. Spec-Driven Development** | **Pass** | `spec.md` exists and was grilled (ten clarifications, 2026-09-20). Every artifact here traces to an `FR-`. The route rename supersedes `001`/`002` by amending their contracts (`FR-015`), not by drifting from them. |
| **II. Test-Driven Development** | **Pass** | The mandatory `before_implement` hook runs `/speckit-tdd-run` from `tdd/test-list.md`. Both new predicates — the `[Produces]` reflection scan and the contract-route scan — get an accepting/rejecting `[Theory]` table written from the requirement before the predicate, as the TDD profile's standing rule demands. |
| **III. Hermetic Tests** | **Pass** | No new network call. The fake DOM is written in this repository; the fixtures are synthetic and committed. `SC-009` restates the rule as a success criterion. |
| **IV. Jellyfin Compatibility** | **Pass** | GUID unchanged. No `PluginConfiguration` change, so no `XmlSerializer` round-trip risk and no migration. Plugin Pages stays optional; only the payload's construction changes, not its content. The route rename breaks no installed client: the plugin's own pages are the only callers and it has never been released. |
| **V. Respectful Sources and Privacy** | **Pass (not engaged)** | No source, rate limit, budget, `User-Agent` or outgoing request is touched. Fixtures carry no keys and no personal data. |
| **VI. Simplicity** | **Pass, with one recorded cost** | No new direct dependency; `[Produces]` is the host's own facility, used instead of inventing one (`003` `FR-016`). One attribute per controller replaces the alternative of ~50 `[JsonPropertyName]`s. The one addition is the fake DOM — see Complexity Tracking. |

**Technical Constraints**: `net10.0`, xunit + NSubstitute, embedded web resources with no build step
— all held. No assertion library is added.

**Development Workflow**: Spec Kit order followed. CI on `main` (`dotnet build` zero warnings,
`dotnet test`, `node --test`) is the final gate, plus the real-server pass `spec.md` puts inside this
feature.

## Project Structure

### Documentation (this feature)

```text
specs/005-page-json-casing/
├── plan.md                      # This file
├── spec.md
├── NOTES.md
├── research.md                  # Phase 0 output — R1..R10
├── data-model.md                # Phase 1 output — the 37-name enumeration
├── quickstart.md                # Phase 1 output — suite pass + real-server pass
├── contracts/
│   ├── http-surface.md          # The convention FR-011 requires; copied to docs/ on implementation
│   └── page-sandbox.md          # What the stand-in covers and does not (FR-018)
├── checklists/
└── tasks.md                     # Phase 2 — NOT created by /speckit-plan
```

### Source code (repository root)

```text
src/Jellyfin.Plugin.NewReleases/
├── Api/
│   ├── PluginRoutes.cs          # NEW — the single const source for the route prefix (FR-013)
│   ├── ReleasesController.cs    # [Produces(camelCase profile)]; routes renamed
│   ├── AdminController.cs       # [Produces(camelCase profile)]; routes renamed
│   ├── UserViewController.cs    # route from PluginRoutes; text/html, the one exception
│   └── Dtos.cs                  # unchanged — the names were never the problem
├── Integration/
│   └── PluginPagesRegistrationService.cs   # payload built from PluginRoutes.UserViewAbsolute
└── Web/
    ├── user-view.html           # API literal + three data-action values follow the rename;
    │                            # exposes render on NewReleasesInternals
    └── admin.html               # API literal follows the rename;
                                 # exposes renderStatus on NewReleasesInternals

tests/Jellyfin.Plugin.NewReleases.Tests/
├── Api/
│   ├── ResponseNamingTests.cs   # NEW — DTO serialized through JsonDefaults.CamelCaseOptions
│   │                            #       vs. the committed fixture (FR-006, US3-AS1)
│   ├── HttpSurfaceTests.cs      # NEW — the [Produces] reflection scan (FR-010, SC-004),
│   │                            #       the PluginRoutes/page-literal guard (FR-013, SC-007),
│   │                            #       and the contract-document scan (FR-015, SC-008)
│   ├── ReleasesControllerTests.cs   # route assertions follow the rename
│   └── AdminControllerTests.cs      # route assertions follow the rename
└── Acceptance/                  # route assertions follow the rename

tests/web/
├── fake-dom.js                  # NEW — the string-capturing stand-in (FR-017, FR-018)
├── load-page.js                 # extended to install the fake DOM
├── render.test.js               # NEW — user-view render against the fixture (FR-008, US3-AS3)
├── render-status.test.js        # NEW — admin renderStatus against the fixture (US2-AS1..3)
└── exposure.test.js             # updated: render and renderStatus join the exposed set

tests/fixtures/pages/            # NEW — releases.json, artists.json, admin-status.json, status.json

docs/
└── http-surface.md              # NEW — the convention (FR-011), linked from CLAUDE.md

specs/001-track-new-releases/contracts/http-api.md   # amended to the renamed routes (FR-015)
specs/002-report-data-age/contracts/http-api.md      # amended to the renamed routes (FR-015)
```

**Structure Decision**: the repository's existing single-project layout is kept — one plugin project,
one xunit project mirroring its folders, and the `tests/web/` node suite alongside. Nothing here
needs a new project or a new folder convention; the only new test folder is `tests/fixtures/pages/`,
which follows the existing `tests/fixtures/<source>/` shape.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
| --- | --- | --- |
| A fake DOM (`tests/web/fake-dom.js`) is new test infrastructure written by hand, against principle VI's "prefer not doing it" | `FR-008` requires the list-versus-empty decision to be exercised against a real response shape, and that decision lives in `render`, which touches elements. `FR-017` forbids a third-party library, and `002` recorded that constraint deliberately. Without it the one function that consumes a server response keeps the one thing it has now: no test. | **jsdom or similar**: forbidden by `FR-017` and by `002`'s recorded constraint, and it would be the plugin's first browser-test dependency. **Asserting on the page source as text**: the TDD profile forbids it and it proves nothing about behaviour. **Leaving `render` untested**: it is exactly what let this defect reach a real server. The cost is bounded by writing *no parser* — elements capture written markup as strings, which is all the two behaviours need. |

**Two spec corrections, raised by this plan and applied to `spec.md` on 2026-09-20**:

- `SC-007` and `FR-013` said the route prefix must live in "exactly one place". Not reachable: the
  pages are static resources with no build step and `admin.html` never passes through plugin code.
  Both now require **one authoritative source** plus a test that fails when any derived literal
  disagrees with it. Reasoning in [`research.md` R4](./research.md).
- `SC-003` said 24 field names. The mechanical enumeration `FR-002` requires found **37**; `SC-003`
  and the two other places that carried the estimate now say 37. Reasoning in
  [`research.md` R9](./research.md).
