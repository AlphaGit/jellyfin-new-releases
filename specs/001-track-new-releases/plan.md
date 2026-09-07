# Implementation Plan: Track New Releases

**Branch**: `001-track-new-releases` (Orca worktree scratch branch `init-plugin-structure`; lands on `main`) | **Date**: 2026-09-06 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/001-track-new-releases/spec.md`

## Summary

A daily **Refresh** scheduled task reads the Jellyfin music library (album artists, their albums,
track titles, the library folders that hold them), matches each library artist to MusicBrainz and
Deezer, pulls the artist's whole catalogue, merges both sources into one Release per (artist,
normalized title), and decides ownership per Release by comparing the library album's track titles
with the best-overlapping Official edition. Results live in a private SQLite database. A
Plugin Pages fragment lists Missing, Incomplete, and Upcoming releases grouped by year, filtered
by the viewer's library access, with Ignore / Have it / Restore decisions shared server-wide and
stored separately from release data so they survive a purge. The admin page configures sources,
release types, and the "released since" cutoff, shows source health and unmatched artists, and
offers Run now, Purge release data, and Clear Archive.

Reference implementation for every Jellyfin pattern: `../jellyfin-concert-radar`.

## Technical Context

**Language/Version**: C# (`LangVersion` latest) on `net9.0`, Jellyfin 10.11.11 host

**Primary Dependencies**:
- `Jellyfin.Controller` 10.11.11, `Jellyfin.Model` 10.11.11 (`ExcludeAssets=runtime`, existing)
- `Microsoft.Data.Sqlite` **9.0.19** (new; published 2026-08-11, 26 days before adoption; MIT).
  Reason: constitution mandates SQLite for persistent state; this is the only supported .NET
  SQLite provider on the 9.0 line that matches the `net9.0` host. Native `libe_sqlite3` ships in
  `build.yaml` artifacts as Concert Radar does.
- Host-provided: `IHttpClientFactory`, `ILibraryManager`, `IUserManager`, `ITaskManager`,
  `TimeProvider.System`, `System.Threading.RateLimiting` (BCL).
- No assertion library (xunit `Assert` suffices; revisit when limiting, per constitution).

**Storage**: SQLite at `{DataPath}/newreleases/newreleases.db`, schema from embedded numbered SQL
migrations (`Storage/Migrations/001_initial.sql`). See [data-model.md](data-model.md).

**Testing**: xunit 2.9.3 + NSubstitute 5.3.0. Real SQLite in a temp directory per test. HTTP via
`HttpClient` with `StubHttpMessageHandler` and recorded fixtures in `tests/fixtures/musicbrainz/`
and `tests/fixtures/deezer/`. Jellyfin services substituted. Stack commands in
`.specify/memory/tdd-profile.md`.

**Target Platform**: Jellyfin 10.11.x server (Linux x64 primary), plugin packaged by JPRM.

**Project Type**: Jellyfin server plugin (one class library) with embedded web pages; one test
project.

**Performance Goals**: list API < 500 ms and first meaningful render < 2 s on LAN with 500
releases (SC-005); full refresh of 500 library artists < 24 h inside source budgets (SC-006).

**Constraints**: hermetic tests (no network, no server); `TreatWarningsAsErrors`; no web build
step; MusicBrainz ≤ 1 req/s with identifying `User-Agent`; Deezer ≤ 50 req / 5 s (we use 5 req/s);
Plugin Pages optional at runtime; `PluginConfiguration` must round-trip `XmlSerializer`.

**Scale/Scope**: single server; ~500 library artists, ~5 000 library albums, ~500–5 000 stored
releases; two sources; two web pages; one scheduled task; ~8 HTTP endpoints.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | How the plan satisfies it |
| --- | --- | --- |
| I. Spec-Driven Development | PASS | Every artifact traces to `spec.md` FR/SC ids. Nothing outside the spec (no artist-page section, no Bandcamp, no rate-limit config UI). |
| II. Test-Driven Development | PASS | Each acceptance scenario maps to a test at a real entry point (`ReleasesController`, `AdminController`, `RefreshNewReleasesTask`, repositories) in [quickstart.md](quickstart.md). `/speckit-tdd-plan` derives the test list; `/speckit-tdd-run` drives red-green. |
| III. Hermetic Tests | PASS | `StubHttpMessageHandler` + fixtures for MusicBrainz/Deezer; `ILibraryManager`/`IUserManager`/`ITaskManager` substituted; `TestDatabase` uses a temp dir it deletes; `TimeProviderStub` for clocks. |
| IV. Jellyfin Compatibility | PASS | GUID unchanged (guarded). Packages pinned 10.11.11. `PluginConfiguration` uses scalar properties only (no lists to seed). Plugin Pages entry is already optional; API and task work without it. Schema versioned by numbered migrations from day one. |
| V. Respectful Sources and Privacy | PASS | Two open APIs, no scraping. Per-source token bucket + daily budget + cooldown in `source_state`. `User-Agent` = `JellyfinNewReleases/<version> ( <contact> )`, contact from config only. Outbound payload: artist names, MBIDs, source ids. No secrets exist in v1 URLs or headers, so there is nothing to redact; logged errors carry the request URL as-is. |
| VI. Simplicity | PASS | One new dependency, justified above. Only interface: `IReleaseSource` with two implementations. No hosted service / migration gate: `Database` migrates lazily on first open. No repository-per-table: four repositories by aggregate. Rate limits and cooldowns are constants, not config. Deliberate ceilings carry `// ponytail:` comments (see research R11, R13, R16). |

**Technical Constraints check**: C# `net9.0` ✓; xunit + NSubstitute ✓; no HTML parser ✓; SQLite via
`Microsoft.Data.Sqlite` with embedded numbered SQL ✓; web as embedded resources, no build step ✓;
JPRM `build.yaml` gains the SQLite artifacts and a `CHANGELOG.md` entry at release ✓.

**Post-design re-check (after Phase 1)**: unchanged, all PASS. No entries in Complexity Tracking.

## Project Structure

### Documentation (this feature)

```text
specs/001-track-new-releases/
├── plan.md              # This file
├── research.md          # Phase 0: decisions R1–R17
├── data-model.md        # Phase 1: entities, SQLite schema, state rules
├── quickstart.md        # Phase 1: validation scenarios per acceptance criterion
├── contracts/
│   ├── http-api.md              # Plugin REST endpoints and DTOs
│   ├── plugin-configuration.md  # PluginConfiguration fields and defaults
│   └── release-source.md        # IReleaseSource contract + per-source endpoint mapping
├── checklists/requirements.md
└── tasks.md             # Phase 2 (/speckit-tasks), not created here
```

### Source Code (repository root)

```text
src/Jellyfin.Plugin.NewReleases/
├── Plugin.cs                          # exists; Plugin Pages entry already registered
├── PluginServiceRegistrator.cs        # register Database, repositories, sources, task, HttpClient
├── AssemblyInfo.cs                    # exists (InternalsVisibleTo tests)
├── Configuration/
│   └── PluginConfiguration.cs         # scalar settings, see contracts/plugin-configuration.md
├── Api/
│   ├── UserViewController.cs          # exists; serves Web/user-view.html
│   ├── ReleasesController.cs          # [Authorize]  GET releases/artists/status, POST ignore|have-it|restore
│   ├── AdminController.cs             # [RequiresElevation] GET status, POST run-now|purge|clear-archive
│   └── Dtos.cs                        # ReleaseDto, ArtistDto, ListResponse, AdminStatusResponse, … (one file)
├── Library/
│   ├── LibraryScanner.cs              # ILibraryManager → LibrarySnapshot (read-only)
│   └── LibrarySnapshot.cs             # records: LibraryArtistSnapshot, LibraryAlbumSnapshot
├── Matching/
│   ├── TitleNormalizer.cs             # docs/domain_knowledge/title-normalization.md
│   ├── ReleaseTypeMapper.cs           # docs/domain_knowledge/release-types.md
│   └── OwnershipMatcher.cs            # album candidate → best edition → Owned/Incomplete/Missing
├── Model/                             # enums + records shared by storage, sources, API
├── Sources/
│   ├── IReleaseSource.cs
│   ├── MusicBrainzSource.cs
│   ├── DeezerSource.cs
│   ├── SourceHttpClient.cs            # rate limit, budget, UA, retry/Retry-After, redaction
│   ├── SourceLimits.cs                # per-source constants (rps, daily budget, cooldown)
│   └── UserAgentBuilder.cs
├── Storage/
│   ├── PluginDatabase.cs              # path, connection string, lazy one-time migration (`Database` collides with the Jellyfin.Database namespace)
│   ├── Migrations/001_initial.sql
│   ├── ArtistRepository.cs            # library_artist, artist_source
│   ├── ReleaseRepository.cs           # release, source_entry, edition, ownership columns
│   ├── ArchiveRepository.cs           # decision
│   └── SourceStateRepository.cs       # source_state, refresh_run
├── ScheduledTasks/
│   └── RefreshNewReleasesTask.cs      # "Refresh new releases", category "New Releases"
└── Web/
    ├── admin.html                     # Jellyfin config page
    └── user-view.html                 # Plugin Pages fragment

tests/Jellyfin.Plugin.NewReleases.Tests/
├── PluginSanityTests.cs               # exists
├── Api/ReleasesControllerTests.cs, AdminControllerTests.cs
├── Library/LibraryScannerTests.cs
├── Matching/TitleNormalizerTests.cs, ReleaseTypeMapperTests.cs, OwnershipMatcherTests.cs
├── Sources/MusicBrainzSourceTests.cs, DeezerSourceTests.cs, SourceHttpClientTests.cs
├── Storage/*RepositoryTests.cs, DatabaseTests.cs
├── ScheduledTasks/RefreshNewReleasesTaskTests.cs
├── Configuration/PluginConfigurationTests.cs   # XmlSerializer round-trip
└── Support/StubHttpMessageHandler.cs, FixtureLoader.cs, TestDatabase.cs, TimeProviderStub.cs,
            LibraryFakes.cs, ControllerContextFactory.cs
tests/fixtures/musicbrainz/*.json, tests/fixtures/deezer/*.json
```

**Structure Decision**: single plugin project plus one test project, mirroring Concert Radar's
folder layout so agents can look up any pattern in the sibling repo by the same path. Folders
group by responsibility (`Library`, `Matching`, `Sources`, `Storage`, `Api`), not by layer
ceremony; there is no `Services/` bucket.

## Refresh run outline (for tasks and tests)

1. **Scan library** (`LibraryScanner`): one `GetItemList(MusicArtist)` and one
   `GetItemList(MusicAlbum, Recursive)`; per album, `GetCollectionFolders(album)` and its `Audio`
   children (`ParentId = album.Id`). Yields library artists (FR-001) with their libraries, MBIDs,
   albums (title, MusicBrainz release / release-group ids, normalized track titles).
2. **Sync artists** (`ArtistRepository`): upsert by `artist_key`; delete artists no longer in the
   library (cascade removes their releases, FR-014).
3. **Rotate** artists by `last_refreshed_at ASC NULLS FIRST`. For each artist × enabled source
   not in cooldown and with budget:
   - match (`IReleaseSource.MatchArtistAsync`, FR-002) → `artist_source.status`;
   - fetch catalogue pages from `resume_offset` (FR-003); upsert releases + source entries
     (merge on `(library_artist_id, normalized_title)`, FR-006a);
   - outcome **Complete** → prune this source's entries for this artist not seen in the run, drop
     releases with no entry, reset offset; **Partial/Failed** → keep offset, remove nothing (FR-014).
4. **Ownership** (`OwnershipMatcher`, FR-005/5a): for every release with a library album
   candidate, ensure Official editions with track lists are stored (fetch once per source
   release id), pick the best-overlapping edition, write state, matched album, edition, missing
   tracks. Runs locally for all artists each run so SC-004 holds even when budgets are exhausted.
5. **Record run** (`refresh_run`): counts and per-source outcomes for the admin page (FR-012).

Read-time rules (list API): type inclusion and displayed type (FR-004), "released since", Upcoming
vs Missing/Incomplete, Archive join, and per-user library filter (FR-007) are all evaluated when
listing, so admin changes apply without a refresh.

## Complexity Tracking

No constitution violations. Nothing to justify.
