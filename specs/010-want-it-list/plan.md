# Implementation Plan: Want it list

**Branch**: `010-want-it-list` | **Date**: 2026-11-10 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/010-want-it-list/spec.md`

## Summary

A third decision kind, `WantIt`, in the existing `decision` table. No new table, no new endpoint
shape beyond one action.

1. **Storage**: migration `004` rebuilds `decision` so `kind` also allows `WantIt` (SQLite cannot
   alter a CHECK). Rows are copied (R1). **Clear Archive** deletes only `Ignore` and `HaveIt`
   rows (R2).
2. **Read**: `GET /Releases` gains `wanted=true`. The three views are the same join with a
   different condition on `decision.kind` and `ownership_state` (R3).
3. **Write**: `POST /Releases/{id}/WantIt` is one more `DecideAsync` call. Restore, Ignore and
   Have it already replace or delete the row (R4).
4. **Page**: a third tab and a per-view button set (R5). The domain vocabulary gains two terms (R6).

## Technical Context

**Language/Version**: C# on `net10.0`. Page JavaScript in `Web/user-view.html`.

**Primary Dependencies**: `Jellyfin.*` 12.0.0 (`ExcludeAssets=runtime`), `Microsoft.Data.Sqlite`
10.0.11. **No new dependency.**

**Storage**: SQLite. Migration `004_decision_want_it.sql` ([data-model.md](./data-model.md)).

**Testing**: xunit 2.9.3 + NSubstitute 5.3.0 and `node --test` for the page. No network. No new
fixture; the response fixtures in `tests/fixtures/pages/` gain one `wanted` sample (R7).

**Target Platform**: Jellyfin 12.0.x server.

**Project Type**: Jellyfin server plugin with embedded web pages.

**Performance Goals**: none new. The Want-it view is the List query with one different condition.

**Constraints**: Hermetic tests (III). Routes follow [docs/http-surface.md](../../docs/http-surface.md).
`TreatWarningsAsErrors` stays on. Existing installs migrate forward with their decisions intact.

**Scale/Scope**: 1 migration, 1 enum member, changes in `ArchiveRepository`, `ReleaseRepository`,
`ReleasesController`, `Dtos.cs` (none expected), `user-view.html`, `CONTEXT.md`.

## Constitution Check

*Checked against `.specify/memory/constitution.md` v2.0.0. Re-checked after Phase 1 design; no
verdict changed.*

| Principle | Verdict | Evidence |
| --- | --- | --- |
| **I. Spec-Driven Development** | **Pass** | Grilled on 2026-11-10 (4 answers). Every research decision traces to an `FR-`. The `001` contract is amended in [contracts/](./contracts/), not changed silently. |
| **III. Hermetic Tests** | **Pass** | Repository tests use a temporary database file. Controller tests substitute Jellyfin services. No live call. |
| **IV. Jellyfin Compatibility** | **Pass** | GUID and configuration unchanged. `004` migrates forward from every released version and keeps all rows. No operator step. |
| **V. Respectful Sources and Privacy** | **Pass** | No source request, no new data leaves the server. |
| **VI. Simplicity** | **Pass** | One enum member, one condition per view, one route. No table, interface or dependency added. |

**Technical Constraints**: Held. **Development Workflow**: Spec Kit order is followed. CI gate:
build with zero warnings, `dotnet test`, `node --test`. `CHANGELOG.md` gets an entry at release.

## Project Structure

### Documentation (this feature)

```text
specs/010-want-it-list/
├── plan.md              # This file
├── spec.md
├── research.md          # Phase 0: R1..R7
├── data-model.md        # Phase 1: decision.kind, the three views
├── quickstart.md        # Phase 1: suite gate + real-server pass
├── contracts/
│   └── http-api.md      # amends 001: wanted view, WantIt action, page buttons
├── checklists/
└── tasks.md             # Phase 2: NOT created by /speckit-plan
```

### Source code (repository root)

```text
src/Jellyfin.Plugin.NewReleases/
├── Model/Enums.cs                          # DecisionKind.WantIt
├── Storage/
│   ├── Migrations/004_decision_want_it.sql # NEW: rebuild decision with the wider CHECK
│   ├── ArchiveRepository.cs                # ClearAsync keeps WantIt
│   └── ReleaseRepository.cs                # ReleaseFilter.Wanted, view condition, archived DTO rule
├── Api/ReleasesController.cs               # wanted query, WantIt action
└── Web/user-view.html                      # third tab, per-view buttons, empty state, announcement

docs/domain_knowledge/CONTEXT.md            # Want it, Want-it list; Restore, Clear Archive reworded

tests/Jellyfin.Plugin.NewReleases.Tests/
├── Storage/                                # 004 migration keeps rows; Clear keeps WantIt
├── Api/                                    # wanted view, WantIt action, 400, Forbid, routes
├── Acceptance/                             # US1..US3 through the real repository and controller
└── Matching/ or Support/                   # none

tests/web/                                  # buttons per view, empty state, announcement
```

**Structure Decision**: Existing single plugin project plus its test project. No new file except
the migration.

## Complexity Tracking

None. No principle is violated.
