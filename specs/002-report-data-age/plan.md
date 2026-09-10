# Implementation Plan: Report the age of the data, not the age of the run

**Branch**: `002-report-data-age` | **Date**: 2026-09-08 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/002-report-data-age/spec.md`

## Summary

The New Releases page reports when a refresh last *ran*. It must report when the releases were
last *checked*. A refresh completes normally when every source is cooling down, disabled or out of
budget — it contacts nobody, recomputes ownership locally, records a completed run — and the page
then claims the data was refreshed moments ago over releases that are hours or days old.

The fix is a read-time change with no schema change. `artist_source.last_complete_at` already
records, per artist and source, the instant a catalogue fetch last finished completely, and it is
already written only on a `Complete` outcome. Reporting `MAX(last_complete_at)` over currently
enabled sources gives `FR-002` directly: a run that confirmed nothing writes nothing, so the value
does not move. Two response fields are renamed because their names are what invited the defect,
the staleness copy is rewritten to name the releases rather than the job, and the empty state is
regated on stored data instead of run history.

The requirements that decide *when* the line shows and *what* it says live in the page, where
nothing tests them, so this feature also brings the page's decision-making under test. Node's
built-in runner does that with no dependency and no packaged change; see `research.md` R8 and R9.

## Technical Context

**Language/Version**: C# on `net9.0`, matching the Jellyfin 10.11 host. Nullable and implicit
usings on, `LangVersion` latest, `TreatWarningsAsErrors` on.

**Primary Dependencies**: unchanged — `Jellyfin.Controller` and `Jellyfin.Model` 10.11.11 with
`ExcludeAssets=runtime`, `Microsoft.Data.Sqlite` 9.0.19. **No dependency is added**, so
constitution VI's seven-day-old pinned-version rule does not engage.

**Storage**: SQLite under the plugin data directory. **No migration.** `schema_version` stays at
1; `001_initial.sql` remains the only migration script.

**Testing**: xunit 2.9.3 and NSubstitute 5.3.0 for the server, per
`.specify/memory/tdd-profile.md`; the acceptance rig, stub HTTP handler and stub clock all already
exist. For the page, `node --test` with `node:test`, `node:assert` and `node:vm` — all standard
library, on the Node 22 already present here and preinstalled on the CI runner. **No
`package.json`, no npm install, no framework** (research.md R8). The TDD profile gains a second
ecosystem entry and CI gains one step.

**Target Platform**: Jellyfin 10.11.11 server; the user view is an HTML fragment served to Plugin
Pages, the admin page a standard Jellyfin configuration page. No build step, no framework.

**Project Type**: Jellyfin server plugin — single project plus one test project.

**Performance Goals**: the new aggregate is `MAX` over `artist_source`, two rows per library
artist. It is read once per list request, alongside the existing 5 000-row list query, and is
not the dominant cost. `001`'s `SC-005` budget of 500 ms for the list request continues to apply
unchanged.

**Constraints**: no network in tests (constitution III); the age must never be reported younger
than the data warrants (`SC-002`); every reader of the value must report the same instant
(`FR-011`).

**Scale/Scope**: two source adapters, libraries up to a few thousand artists. Twelve functional
requirements, six success criteria, two user stories. Roughly: two repository reads, three
response shapes, two embedded pages, and edits to three `001` tests.

## Constitution Check

*GATE: passed before Phase 0, re-checked after Phase 1 design. No violations; Complexity Tracking
is empty.*

| Principle | Verdict | Evidence |
| --------- | ------- | -------- |
| **I. Spec-Driven Development** | Pass | `spec.md` written, grilled over 4 rounds and 8 questions, all decision points at a final status before this plan started. Scope is exactly the spec: no fetching, matching or scheduling change. |
| **II. Test-Driven Development** | Pass | Every behaviour below gets a test that fails first, recorded in `tdd/cycle-log.md`. Three `001` tests assert the old rule and are **rewritten to assert the new one, not deleted or weakened** — `A20` most sharply, since it pins the exact behaviour being replaced. The page-side requirements `FR-006`, `FR-007`, `FR-010` and `FR-012` would otherwise have shipped with no test at all; US3 closes that, which is why the runner is in this feature and not a later one. |
| **III. Hermetic Tests** | Pass | No new network path. The existing stub handler, stub clock and temp-SQLite helpers cover the server side. The page-side runner is the Node standard library, so the suite still passes with no network and nothing installed — `FR-014` makes that a requirement rather than a habit. |
| **IV. Jellyfin Compatibility** | Pass | GUID untouched. No schema change, so every released version migrates forward trivially. `PluginConfiguration` is not touched. Plugin Pages stays optional. The renamed response fields are internal to the plugin's own embedded pages — see `research.md` R2. The page-side tests add **no** packaged file and no runtime script fetch, so they add no load-order or missing-resource failure mode; `research.md` R9 records why the obvious `Web/nr-core.js` split was rejected for exactly that reason. |
| **V. Respectful Sources and Privacy** | Pass | No outgoing request changes. Rate limits, budgets, cooldowns and `User-Agent` untouched. Nothing new leaves the server. |
| **VI. Simplicity** | Pass | No new dependency in either language, no new table, no new column, no new write path. Two read queries, a copy change, one extracted seam. Constitution VI's seven-day pinned-version rule does not engage because nothing is added to pin. Vitest, Jest and jsdom were all rejected as too much machinery for five pure functions (`research.md` R8). The "no build step, no framework" constraint on the web UI holds: the runner is test-time only and the packaged output is byte-identical but for the seam. |

### Deviations

None.

### One correction noticed in passing

`.specify/memory/tdd-profile.md` still says *"Constitution principle not applied:
`.specify/memory/constitution.md` is still the template."* That is stale — the constitution has
been at version 1.2.0 since 2026-09-06 and principle II is written. Correcting the profile note is
a task in this feature because the note misleads the loop that runs next.

## Project Structure

### Documentation (this feature)

```text
specs/002-report-data-age/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0 output — 7 decisions, all resolved from the built system
├── data-model.md        # Phase 1 output — no schema change; the derived value
├── quickstart.md        # Phase 1 output — 2 automated steps, 4 manual
├── contracts/
│   ├── http-api.md      # Renamed fields; the admin addition
│   └── staleness-line.md # When the line shows, its wording, the unit ladder
├── checklists/
│   └── requirements.md  # 16/16 passing
└── tasks.md             # Phase 2 — NOT created by /speckit-plan
```

### Source code (repository root)

Only these files change. `src/` = `src/Jellyfin.Plugin.NewReleases/`,
`tests/` = `tests/Jellyfin.Plugin.NewReleases.Tests/`.

```text
src/
├── Storage/
│   ├── ArtistRepository.cs        # + GetReleasesLastCheckedAtAsync(enabledSources, ct)
│   └── ReleaseRepository.cs       # + HasAnyAsync(ct)
├── Api/
│   ├── Dtos.cs                    # rename 2 fields in ListResponse and StatusResponse;
│   │                              #   + releasesLastCheckedAt on AdminStatusResponse
│   ├── ReleasesController.cs      # read the new value; regate the empty state on stored data
│   └── AdminController.cs         # report the same value beside lastRun
└── Web/
    ├── user-view.html             # new wording, the unit ladder, the new field names
    └── admin.html                 # show the data age beside the last run

tests/
├── web/                           # NEW — page-side tests, run by `node --test`
│   ├── staleness.test.js          # the unit ladder and its boundaries, the clock case
│   ├── esc.test.js                # escaping of source-supplied titles and names
│   └── page-helpers.test.js       # groupOf, healthText, artistLink
├── Storage/
│   ├── ArtistRepositoryTests.cs   # the aggregate, enabled-source filtering, partial/failed
│   └── ReleaseRepositoryTests.cs  # HasAnyAsync
├── Api/
│   ├── ReleasesControllerTests.cs # U118 rewritten; empty-state gate; list/status agreement
│   └── AdminControllerTests.cs    # run and data age both present and independent
└── Acceptance/
    ├── ConfigureAndRunTests.cs    # A20 inverted; purge empty state
    └── BrowseReleasesTests.cs     # A5 under the renamed fields

specs/001-track-new-releases/      # amended, not superseded
├── spec.md                        # FR-015 restated
├── contracts/http-api.md          # renamed fields
└── tdd/test-list.md               # U118 and A20 rows restated

docs/domain_knowledge/CONTEXT.md   # canonical wording line 64
.specify/memory/tdd-profile.md     # stale constitution note; + the node ecosystem entry
.github/workflows/build.yml        # + a `node --test` step beside `dotnet test`
```

**Structure Decision**: no new project, folder or layer. The feature lands in the two repositories
that already own the tables, the two controllers that already serve the values, and the two
embedded pages that already render them. Tests mirror the source folders, as `001` established.

## Phase 0: research

Complete — [research.md](./research.md). Seven questions, all answered from the built system
rather than from assumption:

1. **R1** the datapoint is `MAX(artist_source.last_complete_at)` over enabled sources
2. **R2** rename `lastRefreshedAt` and `hasCompletedRefresh`; no external consumer exists
3. **R3** Purge does not clear the confirmation timestamps
4. **R4** the unit ladder's exact thresholds
5. **R5** a server-wide `EXISTS` gates the empty state
6. **R6** the admin page reports the same instant from the same call
7. **R7** the seven `001` artifacts this feature must amend
8. **R8** the page-side runner is Node's standard library — zero dependencies
9. **R9** one extracted seam plus a sandbox loader, so nothing ships differently

No `NEEDS CLARIFICATION` remained after the grill session, so Phase 0 resolved none.

## Phase 1: design and contracts

Complete — [data-model.md](./data-model.md),
[contracts/http-api.md](./contracts/http-api.md),
[contracts/staleness-line.md](./contracts/staleness-line.md),
[quickstart.md](./quickstart.md).

**Post-design constitution re-check: pass.** The design added two read queries, one response
field, two renames, one extracted seam, and a test runner drawn entirely from the standard
library. It added no dependency, no table, no interface with one implementation and
no configuration value. Nothing in the design moved a decision out of `spec.md`.

## Risks

| Risk | Handling |
| ---- | -------- |
| A rewritten `001` test looks like a weakened test to the next audit | Each rewrite asserts the *opposite* rule rather than dropping an assertion, and the cycle log records why. `A20` in particular is inverted, not deleted. |
| The rename touches more files than the behaviour change | Contained: the fields appear in one DTO file, two controllers, two pages, one contract and three tests. `research.md` R2 states the reason the churn is worth it. |
| Rotation lag still misleads on a large library | Accepted and recorded as a known limit in `spec.md`. The per-artist timestamp already exists if the trade is ever revisited. |
| Two specs disagree until `001` is amended | `001`'s amendments are tasks in this feature, not follow-up work, so the disagreement never outlives the branch. |
| A second test ecosystem is a second thing to maintain | It is the standard library and one CI step, with no manifest to drift. The TDD profile records the command so the loop and the audit both find it. |
| The sandbox loader depends on the page's script being extractable | Each page exposes its helpers on one named object, so the loader has a stable anchor rather than parsing the script's shape. A page that stops exposing it fails the tests loudly. |
| US3 tempts scope creep toward jsdom and the render path | Explicitly out of scope in `spec.md` Assumptions, with the reason. Keyboard and screen-reader criteria stay manual and are named as still-manual. |

## Complexity Tracking

No constitution violations. Table intentionally empty.
