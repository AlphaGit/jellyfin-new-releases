# Implementation Plan: Polish the New Releases view

**Branch**: `007-user-view-polish` | **Date**: 2026-10-03 (re-planned after the 2026-10-03 clarification) | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/007-user-view-polish/spec.md`

## Summary

Four fixes to the New Releases view, plus the server support the covers need:

1. **Artist filter**: a native `<input list>` + `<datalist>` replaces the `<select>`. The person
   types and gets the browser's substring match. The filter applies only when the text equals an
   artist name (R1). Suggestions show the name alone. The server, the stored data and the
   Artists response do not change (2026-10-03 clarification; R3–R5 dropped).
2. **Covers**: `ReleaseDto.covers` holds cover URLs that `ToDto` builds from the stored source
   IDs, Deezer first and then Cover Art Archive (R6). The card loads them lazily, as decorative
   images with no referrer. It falls back to the next URL, then to a placeholder (R7). Nothing
   new is stored for covers.
3. **Buttons**: `align-items: stretch` and `width: 100%` in the actions column (R8).
4. **Source link**: `#00a4dc` (the Jellyfin accent), 5.96:1 against the card. Today it has
   1.81:1 (R9).

## Technical Context

**Language/Version**: C# on `net10.0`. ES5-style JavaScript in the embedded page. Node 22 for
the page tests.

**Primary Dependencies**: `Jellyfin.*` 12.0.0 (`ExcludeAssets=runtime`), `Microsoft.Data.Sqlite`
10.0.11. **No new direct dependency.**

**Storage**: SQLite. **No schema change.** The list query also selects the stored
`source_release_id` ([data-model.md](./data-model.md)).

**Testing**: xunit 2.9.3 + NSubstitute 5.3.0. `node:test` + the string-capturing fake DOM in
`tests/web/`. No new recorded fixture. The two artist-lookup fixtures committed in `e60275c` for the dropped
scope are removed.

**Target Platform**: Jellyfin 12.0.x server. The page runs in the Jellyfin web client under
Plugin Pages.

**Project Type**: Jellyfin server plugin with embedded web pages.

**Performance Goals**: The browser filters the native suggestion list. The page does an O(1)
map lookup per keystroke. One page open requests only the covers near the visible area.

**Constraints**: Hermetic tests (constitution III). No external assets except cover images
(constitution 1.4.0, V). The refresh sends no new request.
`TreatWarningsAsErrors` stays on.

**Scale/Scope**: 1 read-model field, 1 DTO field, 1 page (CSS, native suggestion list, card
markup). The `releases*.json` contract fixtures updated. 2 unused recorded fixtures removed.

## Constitution Check

*Checked against `.specify/memory/constitution.md` v1.4.0. Re-checked after Phase 1 design and
again after the 2026-10-03 re-plan. The verdicts did not change; the evidence got shorter.*

| Principle | Verdict | Evidence |
| --- | --- | --- |
| **I. Spec-Driven Development** | **Pass** | Grilled: ten clarifications on 2026-09-30, one on 2026-10-01, one on 2026-10-03 that removed the disambiguation scope. Every research decision traces to an `FR-`. The `001` contract is amended through [contracts/http-api.md](./contracts/http-api.md), not changed silently. |
| **II. Test-Driven Development** | **Pass, with one recorded limit** | `before_implement` runs `/speckit-tdd-run`. Every FR has a hermetic test ([quickstart.md](./quickstart.md) §1). Pixel equality (SC-003) and real lazy loading cannot be measured by a string-capturing DOM. Their tests assert the declarations, and the real-browser pass checks the pixels (R11). |
| **III. Hermetic Tests** | **Pass** | No new outgoing call. No cover URL is fetched in tests; the tests assert the strings. |
| **IV. Jellyfin Compatibility** | **Pass** | GUID unchanged. No `PluginConfiguration` change. No migration: the schema stays at `001`, the schema of 0.1.0 and 0.1.1. No operator action is needed. |
| **V. Respectful Sources and Privacy** | **Pass** | Covers: the browser loads them directly, within the 1.4.0 exception. `referrerpolicy="no-referrer"` keeps the server address out of the request. No other data goes with it. The server sends no new request. |
| **VI. Simplicity** | **Pass** | No dependency. The Artist filter is the native suggestion list. Covers reuse the stored IDs (no column, no fetch). No server code for a case Jellyfin cannot produce (two library artists with one name). |

**Technical Constraints**: Held. The Web UI has no build step and no framework. Its only
external assets are the permitted cover images.

**Development Workflow**: Spec Kit order is followed. The CI gate is `dotnet build` with zero
warnings, then `dotnet test` and `node --test`. `CHANGELOG.md` gets an entry at release time.

## Project Structure

### Documentation (this feature)

```text
specs/007-user-view-polish/
├── plan.md              # This file
├── spec.md
├── research.md          # Phase 0: R1..R11 (R2–R5 dropped)
├── data-model.md        # Phase 1: the derived read-model and wire fields
├── quickstart.md        # Phase 1: suite gate + real-browser pass
├── contracts/
│   ├── http-api.md      # covers; amends 001
│   └── user-view.md     # suggestion list, card markup, exposed functions, stylesheet rules
├── checklists/
└── tasks.md             # Phase 2: NOT created by /speckit-plan
```

### Source code (repository root)

```text
src/Jellyfin.Plugin.NewReleases/
├── Storage/
│   └── ReleaseRepository.cs     # sources JSON also selects source_release_id (R6)
├── Model/StoredRecords.cs       # SourceLink + SourceReleaseId
├── Api/
│   ├── Dtos.cs                  # ReleaseDto + Covers
│   └── ReleasesController.cs    # ToDto builds covers (R6)
└── Web/user-view.html           # datalist filter, cover column, button and link CSS (R1, R7–R10)

tests/Jellyfin.Plugin.NewReleases.Tests/
├── Storage/                     # source links carry their source_release_id
├── Api/                         # covers order and URL formats; ResponseNamingTests fixtures
└── Acceptance/                  # US2 through the real refresh and controller

tests/web/
├── artist-filter.test.js        # NEW: artistIndex, name applies with case ignored, free text, Clear
├── render.test.js               # + cover markup and nextCover
├── styles.test.js               # NEW: stylesheet declarations and computed contrast
└── exposure.test.js             # + artistIndex, nextCover

tests/fixtures/
├── musicbrainz/artist_lookup*.json   # REMOVE (committed in e60275c for the dropped scope), with their README entry
└── pages/releases*.json             # + covers (artists.json unchanged)

specs/001-track-new-releases/contracts/http-api.md   # amended with the two fields
```

**Structure Decision**: The existing single plugin project and single test project. No new
project and no new folder.

## Complexity Tracking

None. No principle is deviated from.
