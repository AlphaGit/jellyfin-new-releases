# Implementation Plan: Fix four refresh defects found on the live server

**Branch**: `009-fix-refresh-defects` | **Date**: 2026-10-10 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/009-fix-refresh-defects/spec.md`

## Summary

Four fixes in the refresh, plus the ownership rule the joint-artist fix needs:

1. **Placeholders**: a fixed set of 13 MusicBrainz special-purpose MBIDs and Deezer `5080` (R1).
   The refresh task checks the snapshot MBIDs before any request, and each source match after
   it. A placeholder is `Unmatched("placeholder artist")` at every enabled source and gets no
   catalogue request (R2). The admin page gives it its own hint (R3).
2. **Joint artists**: one parser splits `a;b` (R4). The MusicBrainz browse uses the first ID with
   `inc=artist-credits` and keeps only releases credited to every ID. The credit is stored per
   source entry (migration `003`, R5). Ownership also compares the albums of credited library
   artists (R6).
3. **Retry**: `delay = max(Retry-After, backoff)` (R7).
4. **Interrupted runs**: a hosted service closes open runs at host start with `Interrupted` (R8).
   `001_initial.sql` allows the value for new installs. No migration (Clarifications).

## Technical Context

**Language/Version**: C# on `net10.0`. No page JavaScript change.

**Primary Dependencies**: `Jellyfin.*` 12.0.0 (`ExcludeAssets=runtime`), `Microsoft.Data.Sqlite`
10.0.11. **No new direct dependency.**

**Storage**: SQLite. Migration `003_source_entry_credit.sql` adds `source_entry.credit`. The
`refresh_run.outcome` check changes in `001_initial.sql` only ([data-model.md](./data-model.md)).

**Testing**: xunit 2.9.3 + NSubstitute 5.3.0, stubbed `HttpMessageHandler`. One new recorded
fixture, `musicbrainz/releases_joint_artist.json` (R9).

**Target Platform**: Jellyfin 12.0.x server.

**Project Type**: Jellyfin server plugin with embedded web pages.

**Performance Goals**: fewer requests, not more. On the live library the placeholder rule removes
about 2,830 catalogue pages per full pass. A joint artist costs its first member's pages
(Seven Lions: 2).

**Constraints**: Hermetic tests (III). Every MusicBrainz request carries one artist ID.
`TreatWarningsAsErrors` stays on.

**Scale/Scope**: 2 new small types (`PlaceholderArtists`, `MusicBrainzIds`), 1 hosted service,
1 migration, changes in `MusicBrainzSource`, `SourceHttpClient`, `RefreshNewReleasesTask`,
`ReleaseRepository`, `SourceStateRepository`, `AdminController`, `001_initial.sql`.

## Constitution Check

*Checked against `.specify/memory/constitution.md` v2.0.0. Re-checked after Phase 1 design; no
verdict changed.*

| Principle | Verdict | Evidence |
| --- | --- | --- |
| **I. Spec-Driven Development** | **Pass** | Grilled: 9 clarifications on 2026-10-10. One spec edge case (long retry hints) was corrected to match the code before planning. Every research decision traces to an `FR-`. The `001` contracts are amended in [contracts/](./contracts/), not changed silently. |
| **III. Hermetic Tests** | **Pass** | No live call. One new recorded fixture of public catalogue data; nothing to scrub. |
| **IV. Jellyfin Compatibility** | **Deviation, justified below** | GUID and configuration unchanged. `003` migrates forward from every released version. The `Interrupted` outcome reaches existing installs by no migration; see Complexity Tracking. |
| **V. Respectful Sources and Privacy** | **Pass** | Same endpoints, fewer requests, one `inc` value added. Nothing new leaves the server. The retry fix waits longer, never shorter. |
| **VI. Simplicity** | **Pass** | No dependency, no configuration. The placeholder list is a constant set. `OwnershipMatcher` is unchanged; it receives a longer album list. |

**Technical Constraints**: Held. **Development Workflow**: Spec Kit order is followed. CI gate:
build with zero warnings, `dotnet test`, `node --test`. `CHANGELOG.md` gets an entry at release.

## Project Structure

### Documentation (this feature)

```text
specs/009-fix-refresh-defects/
├── plan.md              # This file
├── spec.md
├── research.md          # Phase 0: R1..R9
├── data-model.md        # Phase 1: source_entry.credit, Interrupted, placeholder reason
├── quickstart.md        # Phase 1: suite gate + real-server pass
├── contracts/
│   ├── release-source.md   # amends 001: joint IDs, credit, retry table
│   └── http-api.md         # amends 001: per-row hint, Interrupted outcome
├── checklists/
└── tasks.md             # Phase 2: NOT created by /speckit-plan
```

### Source code (repository root)

```text
src/Jellyfin.Plugin.NewReleases/
├── Sources/
│   ├── PlaceholderArtists.cs     # NEW: the fixed set (R1)
│   ├── MusicBrainzIds.cs         # NEW: Parse(a;b) (R4)
│   ├── MusicBrainzSource.cs      # joint match, browse inc + filter, credit (R4, R5)
│   └── SourceHttpClient.cs       # max(hint, backoff) (R7)
├── Model/
│   ├── SourceRecords.cs          # CatalogueItem.Credit, CreditedArtist, PlaceholderReason
│   └── StoredRecords.cs          # Release.Credit
├── Storage/
│   ├── Migrations/001_initial.sql              # outcome check + 'Interrupted'
│   ├── Migrations/003_source_entry_credit.sql  # NEW
│   ├── ReleaseRepository.cs      # write/read credit
│   └── SourceStateRepository.cs  # CloseOpenRunsAsync (R8)
├── ScheduledTasks/
│   ├── RefreshNewReleasesTask.cs # placeholder rule (R2), credited albums (R6)
│   └── InterruptedRunCloser.cs   # NEW: hosted service (R8)
├── Api/AdminController.cs        # per-row hint (R3)
└── PluginServiceRegistrator.cs   # AddHostedService<InterruptedRunCloser>

tests/Jellyfin.Plugin.NewReleases.Tests/
├── Sources/                      # MusicBrainzIds, joint browse, credit parse, retry delays
├── ScheduledTasks/               # placeholder rule, credited ownership, run closer
├── Storage/                      # 002 migration, credit round-trip, CloseOpenRunsAsync
├── Api/                          # placeholder hint
└── Acceptance/                   # US1, US2 through the real task and controller

tests/fixtures/musicbrainz/releases_joint_artist.json   # NEW (R9)
```

**Structure Decision**: The existing plugin project and test project. No new project or folder.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
| --- | --- | --- |
| IV: `Interrupted` is allowed by editing `001_initial.sql`, with no migration for existing installs | Maintainer decision (Clarifications 2026-10-10): the only existing install is the maintainer's server, already changed by hand. On any other install, the close logs one warning and the plugin works as before. | A migration step that rebuilds `refresh_run` was offered and declined. Note: `003` now exists for `source_entry.credit`, so adding the rebuild there would cost one more SQL block and remove this deviation. |
