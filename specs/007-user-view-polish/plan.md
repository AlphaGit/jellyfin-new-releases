# Implementation Plan: Polish the New Releases view

**Branch**: `007-user-view-polish` | **Date**: 2026-10-01 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/007-user-view-polish/spec.md`

## Summary

Four fixes to the New Releases view, plus the server support two of them need:

1. **Artist filter**: a native `<input list>` + `<datalist>` replaces the `<select>`. The person
   types and gets the browser's substring match. The filter applies only when the text equals a
   suggestion label (R1). Homonyms show their MusicBrainz disambiguation text. The server decides
   collisions across all library artists and sends the text only when one applies (R3). During
   refresh it fetches the text with one MusicBrainz lookup per colliding artist, and stores it
   in two new `library_artist` columns (R4, R5).
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

**Storage**: SQLite. Migration `002_artist_disambiguation.sql` adds two nullable columns to
`library_artist` ([data-model.md](./data-model.md)).

**Testing**: xunit 2.9.3 + NSubstitute 5.3.0. `node:test` + the string-capturing fake DOM in
`tests/web/`. One new recorded fixture: `tests/fixtures/musicbrainz/artist_lookup.json`.

**Target Platform**: Jellyfin 12.0.x server. The page runs in the Jellyfin web client under
Plugin Pages.

**Project Type**: Jellyfin server plugin with embedded web pages.

**Performance Goals**: The browser filters the native suggestion list. The page does an O(1)
map lookup per keystroke. One page open requests only the covers near the visible area.

**Constraints**: Hermetic tests (constitution III). No external assets except cover images
(constitution 1.4.0, V). The MusicBrainz rate limit and daily budget apply to the new lookup.
`TreatWarningsAsErrors` stays on.

**Scale/Scope**: 1 migration, 1 new source method, 1 refresh step, 2 DTO fields, 1 page (CSS,
native suggestion list, card markup). 2 contract fixtures updated, 1 recorded fixture added.

## Constitution Check

*Checked against `.specify/memory/constitution.md` v1.4.0. Re-checked after Phase 1 design. The
verdicts did not change.*

| Principle | Verdict | Evidence |
| --- | --- | --- |
| **I. Spec-Driven Development** | **Pass** | Grilled: ten clarifications on 2026-09-30, one on 2026-10-01. Every research decision traces to an `FR-`. The `001` contract is amended through [contracts/http-api.md](./contracts/http-api.md), not changed silently. |
| **II. Test-Driven Development** | **Pass, with one recorded limit** | `before_implement` runs `/speckit-tdd-run`. Every FR has a hermetic test ([quickstart.md](./quickstart.md) §1). Pixel equality (SC-003) and real lazy loading cannot be measured by a string-capturing DOM. Their tests assert the declarations, and the real-browser pass checks the pixels (R11). |
| **III. Hermetic Tests** | **Pass** | The new MusicBrainz call goes through a stubbed handler and a recorded, scrubbed fixture. No cover URL is fetched in tests; the tests assert the strings. |
| **IV. Jellyfin Compatibility** | **Pass** | GUID unchanged. No `PluginConfiguration` change. Migration `002` is additive and goes forward from `001`, the schema of 0.1.0 and 0.1.1. No operator action is needed. |
| **V. Respectful Sources and Privacy** | **Pass** | Covers: the browser loads them directly, within the 1.4.0 exception. `referrerpolicy="no-referrer"` keeps the server address out of the request. No other data goes with it. Lookup: only the MBID leaves the server. The lookup is sent only for colliding artists, through the existing rate limit, budget, breaker and `User-Agent`. |
| **VI. Simplicity** | **Pass** | No dependency. The Artist filter is the native suggestion list. Covers reuse the stored IDs (no column, no fetch). The disambiguation method is on the concrete `MusicBrainzSource`, not on `IReleaseSource` (no no-op Deezer member). |

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
├── research.md          # Phase 0: R1..R11 (R2 dropped)
├── data-model.md        # Phase 1: migration 002 and the derived wire fields
├── quickstart.md        # Phase 1: suite gate + real-browser pass
├── contracts/
│   ├── http-api.md      # covers, disambiguation, MusicBrainz lookup; amends 001
│   └── user-view.md     # suggestion list, card markup, exposed functions, stylesheet rules
├── checklists/
└── tasks.md             # Phase 2: NOT created by /speckit-plan
```

### Source code (repository root)

```text
src/Jellyfin.Plugin.NewReleases/
├── Storage/
│   ├── Migrations/002_artist_disambiguation.sql   # NEW (R5)
│   ├── ArtistRepository.cs      # + SetDisambiguationAsync, + read of both columns, collision candidates
│   └── ReleaseRepository.cs     # sources JSON also selects source_release_id (R6)
├── Model/StoredRecords.cs       # LibraryArtist + Disambiguation, DisambiguationMbid; SourceLink + SourceReleaseId
├── Sources/MusicBrainzSource.cs # + FetchArtistDisambiguationAsync (R4)
├── ScheduledTasks/RefreshNewReleasesTask.cs        # + disambiguation step after the rotation (R4)
├── Api/
│   ├── Dtos.cs                  # ReleaseDto + Covers; ArtistDto + Disambiguation
│   └── ReleasesController.cs    # ToDto builds covers; GetArtists computes server-wide collisions (R3)
└── Web/user-view.html           # datalist filter, cover column, button and link CSS (R1, R7–R10)

tests/Jellyfin.Plugin.NewReleases.Tests/
├── Storage/                     # migration 002, repository disambiguation round-trip
├── Sources/                     # artist lookup against the fixture
├── ScheduledTasks/              # disambiguation step: candidates, no refetch, MBID change, budget
├── Api/                         # covers order; disambiguation on collision; ResponseNamingTests fixtures
└── Acceptance/                  # US1 and US2 through the real controller

tests/web/
├── artist-filter.test.js        # NEW: artistLabel, artistIndex, exact label applies, free text, Clear
├── render.test.js               # + cover markup and nextCover
├── styles.test.js               # NEW: stylesheet declarations and computed contrast
└── exposure.test.js             # + artistLabel, artistIndex, nextCover

tests/fixtures/
├── musicbrainz/artist_lookup.json   # NEW, recorded and scrubbed
└── pages/releases*.json, artists.json   # + covers, + disambiguation

specs/001-track-new-releases/contracts/http-api.md   # amended with the two fields
```

**Structure Decision**: The existing single plugin project and single test project. No new
project and no new folder.

## Complexity Tracking

None. No principle is deviated from.
