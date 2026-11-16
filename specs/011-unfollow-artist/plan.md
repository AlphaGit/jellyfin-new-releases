# Implementation Plan: Unfollow artist

**Branch**: `011-unfollow-artist` | **Date**: 2026-11-15 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/011-unfollow-artist/spec.md`

## Summary

One new table, two artist actions, one new list, and a skip in Refresh. Release decisions are
not touched.

1. **Storage**: migration `005` adds `unfollowed_artist`, keyed by `artist_key`, with no foreign
   key to `library_artist`. A row survives the artist leaving the library, Purge release data,
   Clear Archive and every Refresh (R1).
2. **Refresh**: the task reads the unfollowed keys once and drops those artists from both the
   source rotation and the ownership pass. No source request, no ownership check (R2).
3. **Read**: the List view gains one `NOT EXISTS` condition. The Want-it and Archive views are
   unchanged. Each Release row carries `artistUnfollowed`. `GET Artists/Unfollowed` lists the
   unfollowed library artists the viewer may see (R3, R4).
4. **Write**: `POST Artists/{jellyfinId}/Unfollow` and `.../Follow`. Admin gets
   `POST Admin/ClearUnfollowedArtists` and one count in the status (R5, R6).
5. **Page**: an artist-level button next to the artist name, a fourth tab, and one admin line
   and button (R7). The domain vocabulary gains the new terms (R8).

## Technical Context

**Language/Version**: C# on `net10.0`. Page JavaScript in `Web/user-view.html` and
`Web/admin.html`.

**Primary Dependencies**: `Jellyfin.*` 12.0.0 (`ExcludeAssets=runtime`), `Microsoft.Data.Sqlite`
10.0.11. **No new dependency.**

**Storage**: SQLite. Migration `005_unfollowed_artist.sql` ([data-model.md](./data-model.md)).

**Testing**: xunit 2.9.3 + NSubstitute 5.3.0 and `node --test tests/web` for the page. No
network. The response fixtures in `tests/fixtures/pages/` gain the new field and one
unfollowed-artists sample (R9).

**Target Platform**: Jellyfin 12.0.x server.

**Project Type**: Jellyfin server plugin with embedded web pages.

**Performance Goals**: none new. Refresh does less work. The List query adds one indexed
`NOT EXISTS` on the primary key of `unfollowed_artist`.

**Constraints**: Hermetic tests (III). Routes follow [docs/http-surface.md](../../docs/http-surface.md).
`TreatWarningsAsErrors` stays on. Existing installs migrate forward with all data intact.

**Scale/Scope**: 1 migration, 1 repository, 1 record, changes in `RefreshNewReleasesTask`,
`ReleaseRepository`, `ArtistRepository`, `ReleasesController`, `AdminController`, `Dtos.cs`,
`user-view.html`, `admin.html`, `CONTEXT.md`, `http-surface.md`.

## Constitution Check

*Checked against `.specify/memory/constitution.md` v2.0.0. Re-checked after Phase 1 design; no
verdict changed.*

| Principle | Verdict | Evidence |
| --- | --- | --- |
| **I. Spec-Driven Development** | **Pass** | Planned from a grilled spec (14 answers in the planning interview, 1 in `/speckit-grill-me`). Every research decision traces to an `FR-`. The `001` contract is amended in [contracts/](./contracts/), not changed silently. |
| **III. Hermetic Tests** | **Pass** | Repository tests use a temporary database file. Controller tests substitute Jellyfin services. The Refresh test counts calls on a stubbed source. No live call. |
| **IV. Jellyfin Compatibility** | **Pass** | GUID and configuration unchanged. `005` only adds a table, so it migrates forward from every released version with no row touched. No operator step. |
| **V. Respectful Sources and Privacy** | **Pass** | Fewer source requests, none added. No new data leaves the server. |
| **VI. Simplicity** | **Pass** | One table, one repository, five routes. No interface, factory, setting or dependency added. A flag on the existing library-artist row was rejected (R1). |

**Technical Constraints**: Held. **Development Workflow**: Spec Kit order is followed. CI gate:
build with zero warnings, `dotnet test`, `node --test tests/web`. `CHANGELOG.md` gets an entry at
release.

## Project Structure

### Documentation (this feature)

```text
specs/011-unfollow-artist/
├── plan.md              # This file
├── spec.md
├── research.md          # Phase 0: R1..R9
├── data-model.md        # Phase 1: unfollowed_artist, the List condition, counts
├── quickstart.md        # Phase 1: suite gate + real-server pass
├── contracts/
│   └── http-api.md      # amends 001/010: Artists actions, row flag, admin
├── checklists/
└── tasks.md             # Phase 2: NOT created by /speckit-plan
```

### Source code (repository root)

```text
src/Jellyfin.Plugin.NewReleases/
├── Storage/
│   ├── Migrations/005_unfollowed_artist.sql       # new
│   └── UnfollowedArtistRepository.cs              # new: Set, Remove, Clear, GetKeys, ListFor
├── Model/StoredRecords.cs                         # ListedRelease gains ArtistUnfollowed
├── Storage/ArtistRepository.cs                    # GetByJellyfinIdAsync; counts skip unfollowed
├── Storage/ReleaseRepository.cs                   # List view condition; unfollowed flag column
├── ScheduledTasks/RefreshNewReleasesTask.cs       # skip unfollowed in rotation and ownership
├── Api/Dtos.cs                                    # ReleaseDto.ArtistUnfollowed; admin count
├── Api/ReleasesController.cs                      # Artists/Unfollowed, Artists/{id}/Unfollow|Follow
├── Api/AdminController.cs                         # ClearUnfollowedArtists; status count
├── PluginServiceRegistrator.cs                    # register the repository
└── Web/user-view.html, Web/admin.html             # button, tab, admin line and button

tests/
├── Jellyfin.Plugin.NewReleases.Tests/{Storage,ScheduledTasks,Api,Acceptance}/   # new and changed
├── web/                                           # button, tab, admin markup tests
└── fixtures/pages/                                # new field, unfollowed-artists sample

docs/domain_knowledge/CONTEXT.md                   # FR-021
docs/http-surface.md                               # route examples only if it lists them
```

**Structure Decision**: the existing single plugin project and its test project. No new
project.

## Complexity Tracking

No deviation from the constitution.
