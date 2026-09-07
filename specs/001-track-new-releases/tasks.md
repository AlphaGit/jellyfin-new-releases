---

description: "Task list for Track New Releases"
---

# Tasks: Track New Releases

**Input**: Design documents from `specs/001-track-new-releases/`

**Prerequisites**: plan.md, spec.md, research.md (R1–R17), data-model.md, contracts/ (http-api.md,
plugin-configuration.md, release-source.md), quickstart.md

**Tests**: MANDATORY. Constitution II (TDD, non-negotiable): every behaviour has a test task that
precedes its implementation task, and the implementation task is not started until that test is
red. `/speckit-tdd-plan` derives `tdd/test-list.md` from these tasks; `/speckit-tdd-run` drives
the red-green loop and records the red in `tdd/cycle-log.md`. Stack commands:
`.specify/memory/tdd-profile.md`.

**Organization**: grouped by user story. US1 (the list) needs the whole refresh pipeline, so it is
the largest phase and the MVP. US2 and US3 add the admin surface and user decisions on top.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: US1 (browse releases), US2 (configure and run), US3 (ignore / have it / restore)
- Paths are relative to the repository root. `src/` = `src/Jellyfin.Plugin.NewReleases/`,
  `tests/` = `tests/Jellyfin.Plugin.NewReleases.Tests/` unless a full path is given.

## Path Conventions

- Plugin: `src/Jellyfin.Plugin.NewReleases/{Api,Configuration,Library,Matching,Model,Sources,Storage,ScheduledTasks,Web}/`
- Tests mirror the source folders: `tests/Jellyfin.Plugin.NewReleases.Tests/<Folder>/<Class>Tests.cs`
- Shared test helpers: `tests/Jellyfin.Plugin.NewReleases.Tests/Support/`
- Recorded HTTP bodies: `tests/fixtures/musicbrainz/`, `tests/fixtures/deezer/`
- Reference for every Jellyfin pattern: `../jellyfin-concert-radar` (same folder names)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: dependency, packaging, and test scaffolding the whole feature needs

- [ ] T001 Add `<PackageReference Include="Microsoft.Data.Sqlite" Version="9.0.19" />` (exact pin, R1) and `<EmbeddedResource Include="Storage/Migrations/*.sql" />` to `src/Jellyfin.Plugin.NewReleases/Jellyfin.Plugin.NewReleases.csproj`; add the same package reference to `tests/Jellyfin.Plugin.NewReleases.Tests/Jellyfin.Plugin.NewReleases.Tests.csproj`; run `dotnet restore` so both `packages.lock.json` files update; build must stay warning-free
- [ ] T002 [P] Extend `build.yaml` `artifacts:` with `Microsoft.Data.Sqlite.dll`, `SQLitePCLRaw.batteries_v2.dll`, `SQLitePCLRaw.core.dll`, `SQLitePCLRaw.provider.e_sqlite3.dll`, `runtimes/linux-x64/native/libe_sqlite3.so` (mirror `../jellyfin-concert-radar/build.yaml`); update `overview`/`description` wording to the canonical terms
- [ ] T003 [P] Port test helpers from `../jellyfin-concert-radar/tests/.../Support/` into `tests/Jellyfin.Plugin.NewReleases.Tests/Support/StubHttpMessageHandler.cs`, `Support/FixtureLoader.cs`, `Support/TimeProviderStub.cs` (namespace `Jellyfin.Plugin.NewReleases.Tests.Support`); list them under `helpers:` in `.specify/memory/tdd-profile.md`
- [ ] T004 [P] Record and trim the fixtures listed in `specs/001-track-new-releases/contracts/release-source.md` (section "Fixtures") into `tests/fixtures/musicbrainz/` and `tests/fixtures/deezer/` using the `curl` commands in `specs/001-track-new-releases/quickstart.md`; keep envelope fields consistent with trimmed content; run the scrub grep from `tests/fixtures/README.md` (must print nothing); update `tests/fixtures/README.md` layout line to name both sources. Developer-only step: live network, never inside a test

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: models, storage, normalization, type mapping, HTTP client with source limits,
configuration, DI. No user story can be tested without these.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [ ] T005 Create shared model types: `src/Jellyfin.Plugin.NewReleases/Model/Enums.cs` (`ReleaseType {Album, EP, Single, Compilation, Live, Remix, Soundtrack, Other}`, `OwnershipState {Missing, Incomplete, Owned}`, `DecisionKind {Ignore, HaveIt}`, `FetchOutcome {Complete, Partial, Failed}`, `MatchStatus {Pending, Matched, Unmatched}`), `Model/SourceRecords.cs` (`ArtistMatch`, `CatalogueItem`, `CataloguePage`, `EditionTrackList` per contracts/release-source.md), `Model/StoredRecords.cs` (`LibraryArtist`, `ArtistSourceState`, `Release`, `SourceEntry`, `Edition`, `Decision`, `SourceState`, `RefreshRun` as records mirroring data-model.md columns), `Model/OwnershipResult.cs`
- [X] T006 [P] Write failing tests `tests/Jellyfin.Plugin.NewReleases.Tests/Matching/TitleNormalizerTests.cs`: every example row in `docs/domain_knowledge/title-normalization.md` for `NormalizeAlbum`, `NormalizeTrack`, `NormalizeName` (NFKC + diacritics, case-fold, `&`→`and`, punctuation removal, whitespace collapse; album trailing qualifier removed once; track trailing `feat.`/`ft.` removed; `The Album` ≠ `Album`; `Vol. 2` ≠ `Volume 2`) Behaviors: [U1] [U2] [U3] [U4] [U5] [U6] [U7] [U8] [U9]
- [X] T007 Implement `src/Jellyfin.Plugin.NewReleases/Matching/TitleNormalizer.cs` (static, pure) until T006 is green Behaviors: [U1] [U2] [U3] [U4] [U5] [U6] [U7] [U8] [U9]
- [X] T008 [P] Write failing tests `tests/Jellyfin.Plugin.NewReleases.Tests/Matching/ReleaseTypeMapperTests.cs`: mapping table and all four examples in `docs/domain_knowledge/release-types.md`; `MapMusicBrainz(primary, secondaries)` → `(ReleaseType primary, IReadOnlyList<ReleaseType> secondaries)` with unknown → `Other`; `MapDeezer(recordType)`; `IsIncluded(primary, secondaries, enabledSet)` (any `Other` → false); `DisplayType(primary, secondaries)` precedence Live, Remix, Soundtrack, Compilation, else primary Behaviors: [U10] [U11] [U12] [U13] [U14] [U15] [U16] [U17]
- [X] T009 Implement `src/Jellyfin.Plugin.NewReleases/Matching/ReleaseTypeMapper.cs` (static, pure) until T008 is green Behaviors: [U10] [U11] [U12] [U13] [U14] [U15] [U16] [U17]
- [X] T010 [P] Write failing tests `tests/Jellyfin.Plugin.NewReleases.Tests/Storage/DatabaseTests.cs`: `Database(IApplicationPaths)` resolves `{DataPath}/newreleases/newreleases.db`; first `OpenAsync` creates the directory and applies `001_initial.sql` (all tables from data-model.md exist, `schema_version` = 1); second `OpenAsync` applies nothing; two concurrent first calls share one migration; uses a temp directory it deletes Behaviors: [U18] [U19] [U20] [U21]
- [X] T011 Write `src/Jellyfin.Plugin.NewReleases/Storage/Migrations/001_initial.sql` with every table, index, unique constraint, `ON DELETE CASCADE`, and CHECK vocabulary from `specs/001-track-new-releases/data-model.md` (library_artist, artist_source, release, source_entry, edition, decision, source_state, refresh_run, schema_version); `PRAGMA foreign_keys` enabled by the connection string or on open Behaviors: [U19]
- [X] T012 Implement `src/Jellyfin.Plugin.NewReleases/Storage/Database.cs` (path + connection string, `Lazy<Task>` migration of embedded `NNN_*.sql` with `version > MAX(schema_version)` each in one transaction, `OpenAsync(ct)`; `// ponytail:` note that errors surface on first use, R13) until T010 is green Behaviors: [U18] [U19] [U20] [U21]
- [ ] T013 Create `tests/Jellyfin.Plugin.NewReleases.Tests/Support/TestDatabase.cs`: temp `IApplicationPaths` substitute → `Database`, exposes the repositories as they are added (T015, T017, T019, T021), `IAsyncDisposable` deletes the temp dir; pattern from `../jellyfin-concert-radar/tests/.../Support/TestDatabase.cs`
- [X] T014 [P] Write failing tests `tests/Jellyfin.Plugin.NewReleases.Tests/Storage/ArtistRepositoryTests.cs`: upsert by `artist_key` keeps `id`; `DeleteMissingAsync(keys)` cascades `artist_source`/`release`; `GetRotationAsync()` orders `last_refreshed_at ASC NULLS FIRST, name`; `artist_source` upsert of status/source id/reason, `resume_offset` set and reset, `last_outcome`, `last_complete_at`; counts for admin (`library artists`, matched per source, unmatched list with reasons) Behaviors: [U22] [U23] [U24] [U25] [U26]
- [X] T015 Implement `src/Jellyfin.Plugin.NewReleases/Storage/ArtistRepository.cs` (parameterized `SqliteCommand`, no ORM) until T014 is green; add to `TestDatabase` Behaviors: [U22] [U23] [U24] [U25] [U26]
- [X] T016 [P] Write failing tests `tests/Jellyfin.Plugin.NewReleases.Tests/Storage/ReleaseRepositoryTests.cs`: `UpsertFromSourceAsync(artistId, source, CatalogueItem, runId)` merges on `(library_artist_id, normalized_title)`; one `source_entry` per source per release, `UNIQUE (source, source_release_id)`; canonical recompute prefers `musicbrainz`, falls back when its entry is removed (R17); `PruneEntriesAsync(artistId, source, runId)` deletes entries with `last_seen_run_id < runId` and then orphan releases; edition upsert `UNIQUE (source, source_edition_id)`; `WriteOwnershipAsync`; `date_sort` padding `2024`→`2024-00-00` and NULL for undated (R16); `ListAsync(filter)` order undated last then `date_sort DESC` then title, filters by artist/type/state/from–to/archived (Archive join on natural key), excludes `Owned`, applies enabled types and `ReleasedSince` at read time (R7), hard cap 5 000; `PurgeAsync` truncates release/source_entry/edition only; SC-005: listing 500 rows measured with `Stopwatch`, written to test output, asserted under 2 000 ms for CI headroom (target 500 ms) Behaviors: [U27] [U28] [U29] [U30] [U31] [U32] [U33] [U34] [U35] [U36] [U37] [U38] [U39] [U40] [U41] [U42] [U43]
- [X] T017 Implement `src/Jellyfin.Plugin.NewReleases/Storage/ReleaseRepository.cs` until T016 is green; add to `TestDatabase` Behaviors: [U27] [U28] [U29] [U30] [U31] [U32] [U33] [U34] [U35] [U36] [U37] [U38] [U39] [U40] [U41] [U42] [U43]
- [X] T018 [P] Write failing tests `tests/Jellyfin.Plugin.NewReleases.Tests/Storage/ArchiveRepositoryTests.cs`: `SetAsync(artistKey, normalizedTitle, kind, userId, now)` inserts or replaces; `RemoveAsync` deletes; `ClearAsync` truncates; `GetAsync` returns kind + decidedAt; decision survives `ReleaseRepository.PurgeAsync` (R8) Behaviors: [U44] [U45] [U46]
- [X] T019 Implement `src/Jellyfin.Plugin.NewReleases/Storage/ArchiveRepository.cs` until T018 is green; add to `TestDatabase` Behaviors: [U44] [U45] [U46]
- [X] T020 [P] Write failing tests `tests/Jellyfin.Plugin.NewReleases.Tests/Storage/SourceStateRepositoryTests.cs` (with `TimeProviderStub`): `RecordCallAsync` increments `calls_today` and resets when `calls_day` ≠ today UTC; `GetRemainingBudgetAsync(source, budget)`; `RecordFailureAsync` increments `consecutive_failures` and sets `cooldown_until = now + cooldown` at the 5th; `RecordSuccessAsync` resets failures and cooldown; `SetNextAllowedAtAsync`; `IsInCooldownAsync`; `StartRunAsync`/`FinishRunAsync(counts, outcome)`; `GetLastCompletedRunAsync`; `GetLatestRunAsync` Behaviors: [U47] [U48] [U49] [U50] [U51] [U52]
- [X] T021 Implement `src/Jellyfin.Plugin.NewReleases/Storage/SourceStateRepository.cs` until T020 is green; add to `TestDatabase` Behaviors: [U47] [U48] [U49] [U50] [U51] [U52]
- [X] T022 [P] Write failing tests `tests/Jellyfin.Plugin.NewReleases.Tests/Configuration/PluginConfigurationTests.cs`: defaults exactly as `specs/001-track-new-releases/contracts/plugin-configuration.md` (US2 scenario 1); `XmlSerializer` serialize→deserialize equality for a fully changed instance and for an XML missing every element (US2 scenario 2, constitution IV); `EnabledReleaseTypes()` returns the enabled set; `ReleasedSinceDate()` parses `yyyy-MM-dd`, returns null for empty or invalid Behaviors: [A9] [A10] [U53] [U54] [U55]
- [X] T023 Implement `src/Jellyfin.Plugin.NewReleases/Configuration/PluginConfiguration.cs` (scalar bool/string properties only, R9) until T022 is green Behaviors: [A9] [A10] [U53] [U54] [U55]
- [X] T024 [P] Write failing tests `tests/Jellyfin.Plugin.NewReleases.Tests/Sources/UserAgentBuilderTests.cs`: `JellyfinNewReleases/<version>` without contact, `JellyfinNewReleases/<version> ( <contact> )` with contact, contact trimmed (FR-018) Behaviors: [U56] [U57]
- [X] T075 Implement `src/Jellyfin.Plugin.NewReleases/Sources/UserAgentBuilder.cs` and the constants file `src/Jellyfin.Plugin.NewReleases/Sources/SourceLimits.cs` (per-source `RequestsPerSecond`, `DailyBudget`, `FailureThreshold = 5`, `Cooldown = 6 h`, `RetryBackoffs = 200 ms, 800 ms, 3 200 ms`, `DefaultRetryAfter = 60 s`, `MaxResponseBytes = 10 MiB`; values from R10) until T024 is green Behaviors: [U56] [U57]
- [X] T025 Write failing tests `tests/Jellyfin.Plugin.NewReleases.Tests/Sources/SourceHttpClientTests.cs` (StubHttpMessageHandler + TestDatabase + TimeProviderStub): `GetStringAsync("musicbrainz", url, ct)` sends the `User-Agent` from T024; records one call in `source_state` per HTTP request; throws `DailyBudgetExhaustedException` when budget is spent; 429/503 with `Retry-After` sets `next_allowed_at`, waits, retries up to `RetryBackoffs.Length`; 5xx retries; 4xx (not 429) throws immediately; failure increments `consecutive_failures`, success resets; `IsAvailableAsync(source)` false during cooldown or when `next_allowed_at` is in the future; unknown source id throws `ArgumentException`; request URLs contain only names/ids (FR-017 is asserted at source level, see T030/T032) Behaviors: [U58] [U59] [U60] [U61] [U62] [U63] [U64] [U65] [U66] [U67]
- [X] T026 Implement `src/Jellyfin.Plugin.NewReleases/Sources/SourceHttpClient.cs` (named `IHttpClientFactory` client `"newreleases"`, one BCL `TokenBucketRateLimiter` per source from `SourceLimits`, `SourceStateRepository` for budget/cooldown/backoff, `TimeProvider`, `DailyBudgetExhaustedException` as a nested or same-file type) until T025 is green Behaviors: [U58] [U59] [U60] [U61] [U62] [U63] [U64] [U65] [U66] [U67]
- [ ] T027 Register foundation services in `src/Jellyfin.Plugin.NewReleases/PluginServiceRegistrator.cs`: `Database`, the four repositories, `TryAddSingleton(TimeProvider.System)`, `AddHttpClient("newreleases", c => c.MaxResponseContentBufferSize = SourceLimits.MaxResponseBytes)`, `SourceHttpClient` (sources and the task are registered in T038); keep the existing `PluginSanityTests` green

**Checkpoint**: `dotnet test` green; storage, normalization, type rules, HTTP policy and configuration all exist and are covered. User story work can begin.

---

## Phase 3: User Story 1 - Browse releases not in my library (Priority: P1) 🎯 MVP

**Goal**: a daily Refresh builds the release set from MusicBrainz and Deezer, decides ownership by
track list, and any authenticated user sees Missing / Incomplete / Upcoming releases of artists in
libraries they may access, grouped by year, filterable, with source and artist links.

**Independent Test**: seed a fake library with three artists (albums X, Y for artist A), stub the
sources to list X, Y, Z and one album with 8 of 10 tracks, run `RefreshNewReleasesTask`, call
`GET /Plugins/NewReleases/api/releases` as a user with library access → exactly Z (Missing) and
the 8/10 album (Incomplete, two missing titles) appear with correct fields and links; a user
without access to that library gets an empty list.

### Tests for User Story 1 (mandatory, write first, observe the red before implementing)

- [X] T028 [P] [US1] Create `tests/Jellyfin.Plugin.NewReleases.Tests/Support/LibraryFakes.cs` (builds `MusicArtist`, `MusicAlbum`, `Audio` items with names, `ProviderIds`, ids; configures an `ILibraryManager` substitute so `GetItemList` answers by `IncludeItemTypes`/`ParentId` and `GetCollectionFolders(album)` returns given folders) and write failing tests `tests/Jellyfin.Plugin.NewReleases.Tests/Library/LibraryScannerTests.cs`: only album artists become library artists (featured-only excluded, FR-001); MBID from `MusicBrainzArtist`, fallback `MusicBrainzAlbumArtist`; album `MusicBrainzAlbum`/`MusicBrainzReleaseGroup` ids captured; `LibraryIds` from `GetCollectionFolders`; tracks via `ParentId = album.Id` normalized as tracks; two artist items sharing an MBID → one library artist (edge case); `GetArtist(string)` is never called (R6) Behaviors: [U68] [U69] [U70] [U71] [U72] [U73]
- [X] T029 [P] [US1] Write failing tests `tests/Jellyfin.Plugin.NewReleases.Tests/Sources/MusicBrainzSourceTests.cs` with fixtures from T004: `MatchArtistAsync` returns `Matched(mbid)` with zero HTTP calls when the snapshot has an MBID; search confident → Matched; ambiguous (second within 5 points) → `Unmatched` with reason; low score → Unmatched; empty → Unmatched (FR-002); `FetchCataloguePageAsync` groups `releases[]` by `release-group.id`, maps types via `ReleaseTypeMapper`, keeps `first-release-date` as `YYYY`/`YYYY-MM`/`YYYY-MM-DD`/null, builds `https://musicbrainz.org/release-group/<id>` URLs, computes `NextOffset` from `release-count`; `FetchEditionsAsync` returns one `EditionTrackList` per Official release with track titles normalized as tracks; request URLs match the exact endpoints in contracts/release-source.md and contain only the artist name / ids (FR-017); a 503 goes through `SourceHttpClient` backoff Behaviors: [U74] [U75] [U76] [U77] [U78] [U79] [U80] [U81] [U82] [U83] [U84]
- [X] T030 [P] [US1] Write failing tests `tests/Jellyfin.Plugin.NewReleases.Tests/Sources/DeezerSourceTests.cs`: match requires exact normalized name and one first-page album title equal to a library album title (FR-002); homonyms without corroboration → Unmatched; empty → Unmatched; `FetchCataloguePageAsync` follows `next` for `NextOffset`, maps `record_type`, turns `0000-00-00` into null date, uses `link` as URL; `FetchEditionsAsync` reads `album/<id>/tracks` (+`next`) into one edition titled as the album; HTTP 200 body with `error.code == 4` is treated as transient (backoff) and any other `error` as failure (R3) Behaviors: [U85] [U86] [U87] [U88] [U89] [U90] [U91] [U92]
- [X] T031 [P] [US1] Write failing tests `tests/Jellyfin.Plugin.NewReleases.Tests/Matching/OwnershipMatcherTests.cs` for the algorithm in data-model.md "Ownership algorithm": candidate by `MusicBrainzReleaseGroupId`, by `MusicBrainzReleaseId` ∈ edition ids (`Identifier`), by normalized title including `Album (Deluxe Edition)` (`Title`), none → `Missing`; best edition = max matched, then fewest missing, then musicbrainz, then lowest id; all matched → `Owned`; otherwise `Incomplete` with the missing titles and the edition; trackless editions → `Owned` by presence (documented ceiling) Behaviors: [U93] [U94] [U95] [U96] [U97] [U98] [U99] [U100] [U101] [U102]
- [X] T032 [US1] Write failing tests `tests/Jellyfin.Plugin.NewReleases.Tests/ScheduledTasks/RefreshNewReleasesTaskTests.cs` (TestDatabase, LibraryFakes, StubHttpMessageHandler with T004 fixtures, TimeProviderStub, config from T023): metadata `Name == "Refresh new releases"`, `Category == "New Releases"`, `Key == "NewReleases.Refresh"`, one `DailyTrigger` at 03:00 (FR-009); US1 scenario 1: X, Y `Owned`, Z `Missing`; scenario 6: second run with Z in the library → Z `Owned`; scenario 8: 8/10 tracks → `Incomplete`, two missing titles, edition + source recorded; artists processed in rotation order and `last_refreshed_at` advanced only when every enabled source was attempted; Complete fetch prunes entries the source no longer returns and deletes orphan releases; Partial (budget exhausted mid-page) keeps `resume_offset` and removes nothing; Failed removes nothing (FR-014); edition requests are issued only for releases with a library album candidate; artist removed from the library → its releases gone; a `refresh_run` row with counts and `Completed` is written; cancellation writes `Cancelled`; with every source unavailable ownership is still recomputed from stored editions (INV-1); an `Unmatched` artist is matched again on the next run Behaviors: [U103] [U104] [U105] [U106] [U107] [U108] [U109] [U110] [U113] [U114]
- [X] T033 [US1] Create `tests/Jellyfin.Plugin.NewReleases.Tests/Support/ControllerContextFactory.cs` (a `DefaultHttpContext` whose `User` carries claim `"Jellyfin-UserId"`; an `IUserManager` substitute returning an in-memory `User` with `EnableAllFolders` or explicit `EnabledFolders`) and write failing tests `tests/Jellyfin.Plugin.NewReleases.Tests/Api/ReleasesControllerTests.cs` for `GET api/releases`, `GET api/artists`, `GET api/status` per contracts/http-api.md: scenario 1 only Z listed; scenario 2 order newest first, undated last; scenario 3 `?artistId=` filter and no filter; scenario 4 one `sources[]` link per listing source, https, allowed hosts; scenario 5 no completed run → `hasCompletedRefresh=false`, empty items; scenario 7 `date > serverToday` → `state=Upcoming`, `?state=Upcoming`; scenario 8 `Incomplete` carries `missingTracks` and `comparedEdition`; `?type=`, `?state=Missing|Incomplete`, `?from=&to=` (undated excluded when set); `Owned` never returned; type inclusion and `ReleasedSince` from configuration applied at read time (FR-004); user whose `EnabledFolders` exclude the library gets an empty list (FR-007); missing user claim → 401; `lastRefreshedAt` from the last completed run and `refreshIntervalHours` from an `ITaskManager` substitute's triggers (FR-015, R15) Behaviors: [U115] [U116] [U117] [U118] [U119]
- [X] T054 [US1] Write failing outer-loop acceptance tests `tests/Jellyfin.Plugin.NewReleases.Tests/Acceptance/BrowseReleasesTests.cs` composing `RefreshNewReleasesTask.ExecuteAsync` (TestDatabase, LibraryFakes, StubHttpMessageHandler + T004 fixtures, TimeProviderStub) with `ReleasesController` actions via `ControllerContextFactory`: US1 scenarios 1–8 exactly as rows A1–A8 of `specs/001-track-new-releases/tdd/test-list.md` Behaviors: [A1] [A2] [A3] [A4] [A5] [A6] [A7] [A8]

### Implementation for User Story 1

- [X] T034 [P] [US1] Implement `src/Jellyfin.Plugin.NewReleases/Library/LibrarySnapshot.cs` (records `LibrarySnapshot`, `LibraryArtistSnapshot`, `LibraryAlbumSnapshot` per data-model.md) and `src/Jellyfin.Plugin.NewReleases/Library/LibraryScanner.cs` (`Scan()` → snapshot; two `GetItemList` calls plus per-album `GetCollectionFolders` and `Audio` children query; `artist_key` = MBID else `name:<normalized>`) until T028 is green Behaviors: [U68] [U69] [U70] [U71] [U72] [U73]
- [X] T035 [P] [US1] Implement `src/Jellyfin.Plugin.NewReleases/Sources/IReleaseSource.cs` (exact signature in contracts/release-source.md) and `src/Jellyfin.Plugin.NewReleases/Sources/MusicBrainzSource.cs` (`Id = "musicbrainz"`, `DisplayName = "MusicBrainz"`, `System.Text.Json` parsing, all HTTP via `SourceHttpClient`) until T029 is green Behaviors: [U74] [U75] [U76] [U77] [U78] [U79] [U80] [U81] [U82] [U83] [U84]
- [X] T036 [P] [US1] Implement `src/Jellyfin.Plugin.NewReleases/Sources/DeezerSource.cs` (`Id = "deezer"`, `DisplayName = "Deezer"`, error-envelope check before `data`) until T030 is green Behaviors: [U85] [U86] [U87] [U88] [U89] [U90] [U91] [U92]
- [X] T037 [P] [US1] Implement `src/Jellyfin.Plugin.NewReleases/Matching/OwnershipMatcher.cs` (static, pure; `Decide(release, editions, albums)` → `OwnershipResult`; needs-editions signal when a candidate exists and no editions are stored) until T031 is green Behaviors: [U93] [U94] [U95] [U96] [U97] [U98] [U99] [U100] [U101] [U102]
- [X] T038 [US1] Implement `src/Jellyfin.Plugin.NewReleases/ScheduledTasks/RefreshNewReleasesTask.cs` following plan.md "Refresh run outline" (scan → sync artists → rotate artists × enabled sources: match, page from `resume_offset`, upsert, prune on Complete → ownership for candidates with on-demand edition fetch (R11) → `refresh_run`), reading `Plugin.Instance.Configuration` for enabled sources/types, reporting `IProgress<double>`, honouring cancellation; register `IReleaseSource` (both) and `IScheduledTask` in `src/Jellyfin.Plugin.NewReleases/PluginServiceRegistrator.cs`; iterate until T032 is green Behaviors: [U103] [U104] [U105] [U106] [U107] [U108] [U109] [U110] [U113] [U114]
- [X] T039 [US1] Implement `src/Jellyfin.Plugin.NewReleases/Api/Dtos.cs` (`ReleaseDto`, `SourceLinkDto`, `ArchivedDto`, `ComparedEditionDto`, `ArtistDto`, `ListResponse`, `StatusResponse` per contracts/http-api.md) and `src/Jellyfin.Plugin.NewReleases/Api/ReleasesController.cs` (`[ApiController] [Authorize] [Route("Plugins/NewReleases/api")]`; `GET releases`, `GET artists`, `GET status`; user id from claim `"Jellyfin-UserId"` (R5); allowed libraries via `IUserManager` + `HasPermission(EnableAllFolders)` / `GetPreferenceValues<Guid>(EnabledFolders)` (R4); `refreshIntervalHours` from `ITaskManager.ScheduledTasks` triggers, default 24) until T033 is green Behaviors: [U115] [U116] [U117] [U118] [U119]
- [ ] T040 [US1] Replace the scaffold `src/Jellyfin.Plugin.NewReleases/Web/user-view.html` with the List view per contracts/http-api.md "user-view.html": header, staleness line (`Intl.RelativeTimeFormat`, shown only when `now − lastRefreshedAt > refreshIntervalHours`), empty-state sentence verbatim, filters (Artist/Type/State selects, From/To dates, Clear) with `role="search"`, groups `Upcoming` → years → `Undated`, rows with artist link `#/details?id=<jellyfinId>&serverId=<ApiClient.serverId()>`, title, type badge, date, state badge, `<details>` for missing tracks + "compared with <edition> from <source>", one source link per source (`target="_blank" rel="noopener"`); all controls native and keyboard-operable with visible focus (FR-019); `ApiClient.ajax`/`getUrl` pattern from `../jellyfin-concert-radar/src/.../Web/user-view.html`; manual check per quickstart.md steps 4–5

### Outer-loop gate for User Story 1

- [X] T057 [US1] Outer loop green: acceptance behavior [A1] passes in the full suite (`dotnet test --configuration Release`) and its red is recorded in `specs/001-track-new-releases/tdd/cycle-log.md` before US1 is called complete
- [X] T058 [US1] Outer loop green: acceptance behavior [A2] passes in the full suite (`dotnet test --configuration Release`) and its red is recorded in `specs/001-track-new-releases/tdd/cycle-log.md` before US1 is called complete
- [X] T059 [US1] Outer loop green: acceptance behavior [A3] passes in the full suite (`dotnet test --configuration Release`) and its red is recorded in `specs/001-track-new-releases/tdd/cycle-log.md` before US1 is called complete
- [X] T060 [US1] Outer loop green: acceptance behavior [A4] passes in the full suite (`dotnet test --configuration Release`) and its red is recorded in `specs/001-track-new-releases/tdd/cycle-log.md` before US1 is called complete
- [X] T061 [US1] Outer loop green: acceptance behavior [A5] passes in the full suite (`dotnet test --configuration Release`) and its red is recorded in `specs/001-track-new-releases/tdd/cycle-log.md` before US1 is called complete
- [X] T062 [US1] Outer loop green: acceptance behavior [A6] passes in the full suite (`dotnet test --configuration Release`) and its red is recorded in `specs/001-track-new-releases/tdd/cycle-log.md` before US1 is called complete
- [X] T063 [US1] Outer loop green: acceptance behavior [A7] passes in the full suite (`dotnet test --configuration Release`) and its red is recorded in `specs/001-track-new-releases/tdd/cycle-log.md` before US1 is called complete
- [X] T064 [US1] Outer loop green: acceptance behavior [A8] passes in the full suite (`dotnet test --configuration Release`) and its red is recorded in `specs/001-track-new-releases/tdd/cycle-log.md` before US1 is called complete

**Checkpoint**: `dotnet test` green; the manual smoke run (quickstart.md steps 1, 3, 4) shows the populated list in the web client. MVP done.

---

## Phase 4: User Story 2 - Configure and run release tracking (Priority: P2)

**Goal**: the administrator configures sources, release types, the "released since" cutoff and
the User-Agent contact, sees source health, run status and unmatched artists with fix-it links,
and can Run now, Purge release data, and Clear Archive.

**Independent Test**: open the admin page on a fresh install → defaults shown (both sources,
Album + EP); change a type, save, reload → persisted; click Run now → `202` and the status area
shows the completed run's time, artists processed and releases found; make MusicBrainz fail →
shown as failing while Deezer data keeps arriving.

Defaults and persistence (scenarios 1–2) are already covered by T022/T023 in Phase 2.

### Tests for User Story 2 (mandatory, write first, observe the red before implementing)

- [X] T041 [P] [US2] Write failing tests `tests/Jellyfin.Plugin.NewReleases.Tests/Api/AdminControllerTests.cs` per contracts/http-api.md admin section: controller carries `[Authorize(Policy = Policies.RequiresElevation)]`; `POST api/admin/run-now` calls `ITaskManager.QueueScheduledTask<RefreshNewReleasesTask>()` and returns 202, returns 409 when the task worker reports running (scenario 3); `GET api/admin/status` returns per-source `health` (`Ok`/`Failing`/`CoolingDown`/`Disabled`), `callsToday`, `dailyBudget`, `lastError`, `lastRun` from `refresh_run`, `nextRunAt` from triggers, `libraryArtists`, `matchedArtists`, `unmatched[]` with `jellyfinId`, per-source reasons and the exact hint sentence (FR-012); `POST api/admin/purge` empties release/source_entry/edition, keeps decision, library_artist and artist_source status, resets `resume_offset` (scenario 5, FR-013); `POST api/admin/clear-archive` empties decision only (scenario 7) Behaviors: [U123] [U124] [U125] [U126] [U127]
- [X] T042 [US2] Add failing tests to `tests/Jellyfin.Plugin.NewReleases.Tests/ScheduledTasks/RefreshNewReleasesTaskTests.cs`: scenario 4 — MusicBrainz stub returns 503 for every call while Deezer succeeds → Deezer releases stored, `source_state.musicbrainz` reaches cooldown after 5 failures and is skipped for the rest of the run, run outcome `Completed` with `errors > 0`; a source disabled in configuration is never called; scenario 5 — an existing `decision` still matches the release after purge + refetch (natural key, R8) Behaviors: [U111] [U112]
- [X] T055 [US2] Write failing outer-loop acceptance tests `tests/Jellyfin.Plugin.NewReleases.Tests/Acceptance/ConfigureAndRunTests.cs` composing the task with `AdminController` and `ReleasesController`: run-now then status shows the completed run (A11); MusicBrainz 503s isolated while Deezer data is listed (A12); purge empties the list and a prior decision survives the next run (A13); clear-archive restores archived rows without touching release rows (A15), per `specs/001-track-new-releases/tdd/test-list.md` Behaviors: [A11] [A12] [A13] [A15]

### Implementation for User Story 2

- [X] T043 [US2] Implement `src/Jellyfin.Plugin.NewReleases/Api/AdminController.cs` (`[Route("Plugins/NewReleases/api/admin")]`, `RequiresElevation`; status assembly from `ArtistRepository`, `SourceStateRepository`, `ITaskManager`, `SourceLimits`, configuration; run-now / purge / clear-archive) and the admin DTOs in `src/Jellyfin.Plugin.NewReleases/Api/Dtos.cs` until T041 is green Behaviors: [U123] [U124] [U125] [U126] [U127]
- [X] T044 [US2] Adjust `src/Jellyfin.Plugin.NewReleases/ScheduledTasks/RefreshNewReleasesTask.cs` (skip disabled sources; check `SourceHttpClient.IsAvailableAsync` before each source; count errors) until T042 is green without weakening any T032 test Behaviors: [U111] [U112]
- [ ] T045 [US2] Replace the scaffold `src/Jellyfin.Plugin.NewReleases/Web/admin.html` per contracts/http-api.md "admin.html": Sources (two checkboxes + health text from `api/admin/status`), Release types (seven checkboxes), Released since (`<input type="date">`), Contact for User-Agent, Save via `ApiClient.getPluginConfiguration`/`updatePluginConfiguration` with `PLUGIN_ID`; Status block (last/next refresh, artists processed, releases found); **Run now**, **Purge release data**, **Clear Archive** each behind `Dashboard.confirm` and posting to the admin endpoints; Unmatched artists table (name linking to `#/details?id=…&serverId=…`, sources + reasons, hint sentence); labels use the canonical terms verbatim; manual check per quickstart.md steps 2–3

### Outer-loop gate for User Story 2

- [X] T065 [US2] Outer loop green: acceptance behavior [A9] passes in the full suite (`dotnet test --configuration Release`) and its red is recorded in `specs/001-track-new-releases/tdd/cycle-log.md` before US2 is called complete
- [X] T066 [US2] Outer loop green: acceptance behavior [A10] passes in the full suite (`dotnet test --configuration Release`) and its red is recorded in `specs/001-track-new-releases/tdd/cycle-log.md` before US2 is called complete
- [X] T067 [US2] Outer loop green: acceptance behavior [A11] passes in the full suite (`dotnet test --configuration Release`) and its red is recorded in `specs/001-track-new-releases/tdd/cycle-log.md` before US2 is called complete
- [X] T068 [US2] Outer loop green: acceptance behavior [A12] passes in the full suite (`dotnet test --configuration Release`) and its red is recorded in `specs/001-track-new-releases/tdd/cycle-log.md` before US2 is called complete
- [X] T069 [US2] Outer loop green: acceptance behavior [A13] passes in the full suite (`dotnet test --configuration Release`) and its red is recorded in `specs/001-track-new-releases/tdd/cycle-log.md` before US2 is called complete
- [X] T070 [US2] Outer loop green: acceptance behavior [A15] passes in the full suite (`dotnet test --configuration Release`) and its red is recorded in `specs/001-track-new-releases/tdd/cycle-log.md` before US2 is called complete

**Checkpoint**: admin page configures, runs, purges and clears; source failure is visible and isolated.

---

## Phase 5: User Story 3 - Ignore a release, or say I have it (Priority: P3)

**Goal**: any authenticated user can Ignore a release or mark it Have it; it moves to the shared
Archive, stays there across refreshes, and can be Restored from the same page.

**Independent Test**: Ignore one listed release and mark another Have it → both gone from the
list, both in `?archived=true` with their `kind`; run a refresh → still archived; Restore both →
back in the list in date position.

### Tests for User Story 3 (mandatory, write first, observe the red before implementing)

- [ ] T046 [P] [US3] Add failing tests to `tests/Jellyfin.Plugin.NewReleases.Tests/Api/ReleasesControllerTests.cs` for `POST api/releases/{id}/ignore|have-it|restore`: scenario 1 ignore → 204, absent from the list, present in `?archived=true` with `archived.kind == "Ignore"` and `decidedAt`; have-it → `"HaveIt"`; scenario 2 restore → 204 and the release returns in date order; unknown id → 404; release of an artist outside the caller's libraries → 403 (FR-007); missing claim → 401; a second user sees the same Archive (FR-016) Behaviors: [U120] [U121] [U122]
- [ ] T056 [P] [US3] Write failing outer-loop acceptance tests `tests/Jellyfin.Plugin.NewReleases.Tests/Acceptance/ArchiveTests.cs` through `ReleasesController`: ignore removes the release from the list at once and shows it in `?archived=true` with `kind=Ignore` (A16, first half); restore puts it back at its date position (A17), per `specs/001-track-new-releases/tdd/test-list.md` Behaviors: [A16] [A17]
- [ ] T047 [P] [US3] Add failing run-spanning acceptance tests to `tests/Jellyfin.Plugin.NewReleases.Tests/Acceptance/ArchiveTests.cs`: an ignored release is still absent from the list and present in the Archive after a refresh (US3 scenario 1, second half); Have it on an `Incomplete` release keeps it archived after a run that still finds tracks missing (scenario 3); the library later gains the full track list → ownership becomes `Owned`, the decision remains, `?archived=true` still lists it (scenario 4, FR-005b) Behaviors: [A16] [A18] [A19]

### Implementation for User Story 3

- [ ] T048 [US3] Add the three `POST` actions to `src/Jellyfin.Plugin.NewReleases/Api/ReleasesController.cs` (resolve release → natural key → `ArchiveRepository.SetAsync`/`RemoveAsync` with the caller's user id and `TimeProvider`; visibility check before writing) until T046 and T047 are green Behaviors: [U120] [U121] [U122]
- [ ] T049 [US3] Extend `src/Jellyfin.Plugin.NewReleases/Web/user-view.html`: tabs **List** / **Archive** (`role="tablist"`), row buttons **Ignore** and **Have it** in the list, **Restore** in the Archive (Archive rows show the decision kind and "In library" explanation for Have it), `aria-live="polite"` announcements "Ignored <title>", "Marked <title> as Have it", "Restored <title>" (FR-019); optimistic row removal then reload; keyboard-only walkthrough per quickstart.md step 5

### Outer-loop gate for User Story 3

- [ ] T071 [US3] Outer loop green: acceptance behavior [A16] passes in the full suite (`dotnet test --configuration Release`) and its red is recorded in `specs/001-track-new-releases/tdd/cycle-log.md` before US3 is called complete
- [ ] T072 [US3] Outer loop green: acceptance behavior [A17] passes in the full suite (`dotnet test --configuration Release`) and its red is recorded in `specs/001-track-new-releases/tdd/cycle-log.md` before US3 is called complete
- [ ] T073 [US3] Outer loop green: acceptance behavior [A18] passes in the full suite (`dotnet test --configuration Release`) and its red is recorded in `specs/001-track-new-releases/tdd/cycle-log.md` before US3 is called complete
- [ ] T074 [US3] Outer loop green: acceptance behavior [A19] passes in the full suite (`dotnet test --configuration Release`) and its red is recorded in `specs/001-track-new-releases/tdd/cycle-log.md` before US3 is called complete

**Checkpoint**: all three stories independently functional; every acceptance scenario in spec.md has a green test except US2 scenario 6 (not applicable in v1, research R12).

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: documentation, packaging, release gate

- [ ] T050 [P] Rewrite `README.md`: what New Releases does (canonical terms), sources and what leaves the server (FR-017), configuration fields, the scheduled task name and how to change its interval, Plugin Pages optionality, privacy statement, build/test commands
- [ ] T051 [P] Add an `Unreleased` section to a new `CHANGELOG.md` describing this feature; keep `build.yaml` `version` at `0.1.0` (bump happens in the release commit with the tag, constitution)
- [ ] T052 Run the manual smoke run in `specs/001-track-new-releases/quickstart.md` (steps 1–6) against Jellyfin 10.11.11 with Plugin Pages installed and record the outcome as a "Results" section at the end of `specs/001-track-new-releases/quickstart.md`; open a spec amendment for any deviation before touching code (constitution I)
- [ ] T053 Final gate: `dotnet build --configuration Release` with zero warnings and `dotnet test --configuration Release` green (the CI workflow in `.github/workflows/` runs the same); commit to `main` with Conventional Commits, push, and verify CI green with `gh run list --branch main` / `gh run watch` (constitution: a feature is done when CI on `main` is green)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: T001 first (package + embedded SQL); T002, T003, T004 in parallel after it
- **Foundational (Phase 2)**: depends on Phase 1; BLOCKS all user stories. Test tasks marked [P]
  (T006, T008, T010, T014, T016, T018, T020, T022, T024) can be written in parallel; each
  implementation task (T007, T009, T012, T015, T017, T019, T021, T023, T075) follows its own test. T013 (TestDatabase) grows as T015/T017/T019/T021
  land. T025→T026 need T021 and T024. T027 last.
- **US1 (Phase 3)**: depends on Phase 2. Tests T028–T031 and T033 parallel; T032 and T054 after
  T028–T031 exist (they compose them). Implementations T034–T037 parallel; T038 needs T034–T037;
  T039 needs T017, T019, T021, T023; T040 needs T039. Gates T057–T064 close the story.
- **US2 (Phase 4)**: depends on Phase 2 and on T038 (task) and T039 (DTO file). T041 ∥ T042 ∥ T055;
  T043 after T041; T044 after T042; T045 after T043. Gates T065–T070 close the story.
- **US3 (Phase 5)**: depends on Phase 2, T039 (controller) and T040 (page). T046 ∥ T047 ∥ T056;
  T048 after all three; T049 after T048. Gates T071–T074 close the story.
- **Polish (Phase 6)**: after the stories you ship. T050 ∥ T051; T052 after all stories; T053 last.

### User Story Dependencies

- **US1 (P1)**: only Phase 2. Delivers the MVP on its own.
- **US2 (P2)**: shares `RefreshNewReleasesTask` and `Dtos.cs` with US1; its tests are
  independent (admin endpoints, source-failure isolation, purge/clear).
- **US3 (P3)**: adds endpoints to `ReleasesController` and controls to `user-view.html`; its
  tests are independent (decisions, Archive listing).

### Within Each User Story

- Every test task is written and observed failing for the right reason before its
  implementation task starts; the red is recorded in `specs/001-track-new-releases/tdd/cycle-log.md`
- Pure functions (normalizer, mapper, matcher) → storage → sources → task → controller → page
- Tests are never weakened to reach green; when a test and code disagree, `spec.md` decides

### Parallel Opportunities

- Phase 2: nine test files written concurrently; five implementations (T007, T009, T015, T017,
  T019, T021, T023) touch different files
- Phase 3: five test files (T028–T031, T033) concurrently; four implementations (T034–T037)
  concurrently
- Phases 4 and 5 can proceed in parallel once T038–T040 exist (they touch disjoint files except
  `Dtos.cs`, which US2 appends to)

---

## Parallel Example: User Story 1

```bash
# Tests first, in parallel (each must fail before its implementation starts):
Task: "T028 LibraryScannerTests + LibraryFakes"        → tests/.../Library/LibraryScannerTests.cs
Task: "T029 MusicBrainzSourceTests"                    → tests/.../Sources/MusicBrainzSourceTests.cs
Task: "T030 DeezerSourceTests"                         → tests/.../Sources/DeezerSourceTests.cs
Task: "T031 OwnershipMatcherTests"                     → tests/.../Matching/OwnershipMatcherTests.cs
Task: "T033 ReleasesControllerTests + ControllerContextFactory" → tests/.../Api/ReleasesControllerTests.cs

# Then implementations, in parallel:
Task: "T034 LibraryScanner"      → src/.../Library/LibraryScanner.cs
Task: "T035 MusicBrainzSource"   → src/.../Sources/MusicBrainzSource.cs
Task: "T036 DeezerSource"        → src/.../Sources/DeezerSource.cs
Task: "T037 OwnershipMatcher"    → src/.../Matching/OwnershipMatcher.cs

# Then sequentially: T032 (task tests) → T038 (task) → T039 (controller) → T040 (page)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Phase 1: Setup (T001–T004)
2. Phase 2: Foundational (T005–T027) — blocks everything
3. Phase 3: User Story 1 (T028–T040)
4. **STOP and VALIDATE**: `dotnet test` green; quickstart.md steps 1, 3, 4 on a real server (Run
   now via Jellyfin's Scheduled Tasks page until US2 exists)
5. Commit to `main`, CI green → usable plugin

### Incremental Delivery

1. Setup + Foundational → storage and rules exist, nothing visible yet
2. US1 → the list works with default configuration (MVP)
3. US2 → admin control, health, purge, clear
4. US3 → shared decisions and the Archive
5. Polish → docs, changelog, smoke run, CI

### Parallel Team Strategy

Single maintainer with agents: after Phase 2, run US1 first; once T038–T040 exist, US2 and US3
can run in two worktrees and fast-forward onto `main` one after the other.

---

## Notes

- 75 tasks: Setup 4, Foundational 24, US1 22, US2 12, US3 9, Polish 4
- `Behaviors: [A#] [U#]` markers tie each task to `tdd/test-list.md`; `/speckit-tdd-run` ticks a task only through these ids. Ids T054–T075 were inserted by `/speckit-tdd-plan` and keep the sequence; they sit where the loop needs them, not in numeric order
- Every `[P]` task touches files no other in-flight task touches
- `// ponytail:` comments mark the known ceilings from research.md (R8 merge key, R11 edition
  refetch, R13 lazy migration, R16 no paging)
- US2 acceptance scenario 6 (terms acceptance) has no task: no v1 source requires terms (R12)
- Commit after each green cycle or logical group; never commit a red suite
