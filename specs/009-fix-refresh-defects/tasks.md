---

description: "Task list for 009-fix-refresh-defects"
---

# Tasks: Fix four refresh defects found on the live server

**Input**: Design documents from `/specs/009-fix-refresh-defects/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md),
[data-model.md](./data-model.md), [contracts/](./contracts/), [quickstart.md](./quickstart.md)

**Tests**: Included. [quickstart.md](./quickstart.md) §1 maps every acceptance scenario to a
test, and the suite is the gate (constitution III, Development Workflow). No live network call in
a test: stubbed `HttpMessageHandler` and recorded fixtures only.

**Organization**: A small foundational phase adds the two shared types. Then one phase per user
story, in spec priority order. US1 and US2 both edit `RefreshNewReleasesTask.cs` and
`RefreshNewReleasesTaskTests.cs`, so run them one after the other. US3 and US4 touch other files
and can run in parallel with either.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel: different files, and no dependency on an incomplete task.
- **[Story]**: US1–US4 from `spec.md`.

## Commands

```bash
PATH=/opt/homebrew/opt/dotnet/bin:$PATH dotnet test --configuration Release --filter "FullyQualifiedName~{Class}"
PATH=/opt/homebrew/opt/dotnet/bin:$PATH dotnet test --configuration Release
node --test tests/web/{file}
node --test "tests/web/*.test.js"
```

---

## Phase 1: Setup (Shared Infrastructure)

None. No new project, dependency or folder.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The placeholder set and the MusicBrainz ID parser. US1 and US2 both read them.

- [X] T001 [P] Create `src/Jellyfin.Plugin.NewReleases/Sources/PlaceholderArtists.cs`: a static class with the 13 MusicBrainz MBIDs and Deezer `5080` of research R1, and `IsPlaceholder(string sourceId, string sourceArtistId)` (MBIDs compared case-insensitively). Add `tests/Jellyfin.Plugin.NewReleases.Tests/Sources/PlaceholderArtistsTests.cs`: each R1 ID is a placeholder at its own source; `5080` is not a placeholder at MusicBrainz and an MBID is not one at Deezer; "No Artist" `954f3f5b-…` and a real MBID are not placeholders.
- [X] T002 Create `src/Jellyfin.Plugin.NewReleases/Sources/MusicBrainzIds.cs` with `Parse(string?)` per [data-model.md](./data-model.md) "Derived rules": split on `;`, trim, drop empties and `PlaceholderArtists` MBIDs, distinct (case-insensitive), keep order. Add `tests/Jellyfin.Plugin.NewReleases.Tests/Sources/MusicBrainzIdsTests.cs`: null and empty give `[]`; one ID gives `[id]`; `a;b` and ` a ; b ` give `[a, b]`; `a;a` gives `[a]`; `a;<Various Artists MBID>` gives `[a]`; only placeholders give `[]`. Depends on T001.

**Checkpoint**: Both types and their tests green.

---

## Phase 3: User Story 1 - A refresh skips placeholder artists (Priority: P1) 🎯 MVP

**Goal**: No catalogue request for a placeholder at any source; every enabled source records
`Unmatched("placeholder artist")`; the admin page shows the one-step `Placeholder` trail and the
placeholder fix once.

**Independent Test**: Refresh a library holding "Various Artists" with MBID `89ad4ac3-…` and one
real artist. No request names the placeholder, the real artist is fetched and decided, the
release list has no row for the placeholder, and the status shows `failedStep` `Placeholder` at
every enabled source.

### Tests for User Story 1

- [X] T003 [P] [US1] Add cases to `tests/Jellyfin.Plugin.NewReleases.Tests/Matching/MatchReasonsTests.cs` (research R3): `MatchReasons.Placeholder` is `placeholder artist`; `Parse("musicbrainz", …)` and `Parse("deezer", …)` both give `FailedStep("Placeholder", [])`; `Steps(source, that step)` gives exactly one step, `Placeholder` `Failed`, with no values, for both sources.
- [X] T004 [P] [US1] Add cases to `tests/Jellyfin.Plugin.NewReleases.Tests/ScheduledTasks/RefreshNewReleasesTaskTests.cs` (research R2):
  - A snapshot artist with MBID `89ad4ac3-…`: zero requests to either source (`StubHttpMessageHandler.ReceivedRequests`); both sources stored `Unmatched` with reason `placeholder artist` and no `match_detail`; no release stored for it; `last_refreshed_at` set.
  - A joint artist `a;<Various Artists MBID>` is not a placeholder and is fetched with `a` only.
  - An artist with no MBID whose MusicBrainz search returns the Various Artists MBID (build the search with `SourceJson.MusicBrainz.Search`): no catalogue request and no Deezer request; both sources `Unmatched("placeholder artist")`.
  - MusicBrainz disabled, Deezer search resolving to `5080`: no Deezer catalogue request; Deezer `Unmatched("placeholder artist")`.
  - A stored `Matched` row whose source artist ID is a placeholder (as on the live server) becomes `Unmatched("placeholder artist")` with no catalogue request.
  - A real artist beside a placeholder sends the same requests as before (FR-009).
- [X] T005 [P] [US1] Add a case to `tests/Jellyfin.Plugin.NewReleases.Tests/Api/AdminControllerTests.cs`: an artist stored `Unmatched("placeholder artist")` at both sources gives two `Reasons` entries, each with `FailedStep` `Placeholder`, `Steps` = one step `Placeholder` `Failed` with null `Values`, `Reason` `placeholder artist`, and null `Candidates`, `ExactNameCount`, `AlbumsCompared` ([contracts/http-api.md](./contracts/http-api.md)).
- [X] T006 [P] [US1] Add cases to `tests/web/render-status.test.js`, with a placeholder row ("Various Artists", both sources as in the [contracts/http-api.md](./contracts/http-api.md) example) appended to `admin-status.json` in the test, as `withUnrecognizedReason` does (the shared fixture stays as it is; an extra row there breaks `008`'s row count, and the row adds no field name to `ResponseNamingTests`): the row's Reason cell reads `MusicBrainz: Placeholder ✗` and `Deezer: Placeholder ✗`; How to fix holds the placeholder fix text exactly once, then `Not tracked.`; the other fixture rows render exactly as before.
- [X] T007 [US1] Add a case to `tests/Jellyfin.Plugin.NewReleases.Tests/Acceptance/ConfigureAndRunTests.cs`, using `AcceptanceRig`: a library with "Various Artists" (MBID `89ad4ac3-…`) and one real artist. After a refresh, no recorded request names the placeholder, the real artist's releases are stored and decided, `ReleasesController` lists no release for the placeholder, and `AdminStatusAsync` gives `FailedStep` `Placeholder` at every enabled source (US1 scenarios 1, 3, 4; SC-001, SC-002).

### Implementation for User Story 1

- [X] T008 [US1] In `src/Jellyfin.Plugin.NewReleases/Matching/MatchReasons.cs`, add `Placeholder = "placeholder artist"`; make `Parse` return `FailedStep("Placeholder", [])` for it at either source, before the per-source rules; make `Steps` return the single `Placeholder` `Failed` step for it. T003 and T005 pass.
- [X] T009 [US1] In `src/Jellyfin.Plugin.NewReleases/ScheduledTasks/RefreshNewReleasesTask.cs` (research R2):
  - Before the source loop of phase 3: when the snapshot `Mbid` holds at least one ID and `MusicBrainzIds.Parse(artist.Mbid)` is empty, mark the artist a placeholder.
  - In `RefreshArtistAtSourceAsync`, after the match (fresh or stored): when `PlaceholderArtists.IsPlaceholder(source.Id, match.SourceArtistId)`, mark the artist a placeholder and send no catalogue request.
  - A placeholder: `SetMatchAsync(id, s, ArtistMatch.Unmatched(MatchReasons.Placeholder))` for every enabled source, skip the remaining sources, and count every source as attempted so `SetLastRefreshedAsync` runs.
  - Phase 4 skips placeholder artists.
  T004 and T007 pass.
- [X] T010 [US1] In `src/Jellyfin.Plugin.NewReleases/Web/admin.html`, add to `fixFor` the branch `r.failedStep === 'Placeholder'` with the fix text of [contracts/http-api.md](./contracts/http-api.md), and de-duplicate the row's fix paragraphs before the join (`008` FR-007). T006 passes.

**Checkpoint**: US1 complete. Both suites green. A placeholder costs zero catalogue requests.

---

## Phase 4: User Story 2 - A joint artist refreshes (Priority: P2)

**Goal**: A joint artist's browse sends one MBID per request and keeps only releases credited to
every MBID. Ownership compares the albums of every credited library artist.

**Independent Test**: Refresh "Seven Lions + Kill the Noise" (`75c016da-…;a9930a14-…`) against
`releases_joint_artist.json`. Every request carries one ID, only releases credited to both IDs
are stored, and the fetch outcome is complete.

### Tests for User Story 2

- [X] T011 [US2] Record `tests/fixtures/musicbrainz/releases_joint_artist.json` (research R9): one manual request, outside the test suite, to `https://musicbrainz.org/ws/2/release?artist=75c016da-48e4-4992-8a10-b7128189b272&status=official&inc=release-groups+artist-credits&limit=100&offset=0&fmt=json` with the plugin's User-Agent. Trim `releases` to 3 items: one credited to both IDs ("The Blood"), one to both plus Tritonal ("Horizon"), one to Seven Lions alone. Keep `release-count` as served.
- [X] T012 [P] [US2] Add cases to `tests/Jellyfin.Plugin.NewReleases.Tests/Sources/MusicBrainzSourceTests.cs`:
  - `MatchArtistAsync` with Mbid ` a ; b ` returns `Matched("a;b")` with no request.
  - `FetchCataloguePageAsync("a;b", 0)` sends one request, `release?artist=a&…&inc=release-groups+artist-credits…`, with no `;` in the URL.
  - With `releases_joint_artist.json`, a joint ID keeps "The Blood" and "Horizon" and drops the solo release; `NextOffset` and `Total` come from `release-count`, as before.
  - A single ID keeps every release of the page, as before; the URL gains only `+artist-credits`.
  - Each kept item's `Credit` lists the release's `artist-credit` entries as `(artist.id, artist.name)`; `releases_page1.json` (no `artist-credit`) gives empty credits.
- [X] T013 [P] [US2] Add cases to `tests/Jellyfin.Plugin.NewReleases.Tests/Storage/DatabaseTests.cs`: a database at schema `002` with a `source_entry` row migrates to `003`; the row reads `credit` `[]`; `schema_version` records 3.
- [X] T014 [P] [US2] Add cases to `tests/Jellyfin.Plugin.NewReleases.Tests/Storage/ReleaseRepositoryTests.cs`: `UpsertFromSourceAsync` with a credited item stores the credit as JSON; a second upsert rewrites it; the read model's `Release.Credit` is the union of its entries' credits; an item with an empty credit stores `[]`.
- [X] T015 [US2] Add cases to `tests/Jellyfin.Plugin.NewReleases.Tests/ScheduledTasks/RefreshNewReleasesTaskTests.cs` (research R6), after T004:
  - (US2 scenarios 1–3 need the real MusicBrainz source: covered by T012 and T016.)
  - A release found for artist X, credited to X and Y, where Y is a library artist (matched by MBID) holding the album: the release is `Owned` (US2 scenario 5).
  - The same with no MBID on Y's library side or on the credit entry: matched by normalized name.
  - Library artist Z, not credited, holds a same-title album: the album is not compared and the release stays missing (US2 scenario 6).
  - A Deezer release (empty credit) is decided against the found-for artist's albums only, as before.
- [X] T016 [US2] Add a case to `tests/Jellyfin.Plugin.NewReleases.Tests/Acceptance/ConfigureAndRunTests.cs`, using `AcceptanceRig`: a library with the joint artist and a library album "The Blood" under artist "Kill the Noise" (MBID `a9930a14-…`). After a refresh, every recorded MusicBrainz request carries one ID, and "The Blood" is owned (SC-003).

### Implementation for User Story 2

- [X] T017 [US2] In `src/Jellyfin.Plugin.NewReleases/Model/SourceRecords.cs`, add `CreditedArtist(string? Mbid, string Name)` and `CatalogueItem.Credit` (`IReadOnlyList<CreditedArtist>`, default empty so `DeezerSource` needs no change). In `src/Jellyfin.Plugin.NewReleases/Model/StoredRecords.cs`, add `Release.Credit`.
- [X] T018 [US2] In `src/Jellyfin.Plugin.NewReleases/Sources/MusicBrainzSource.cs` (research R4, R5, [contracts/release-source.md](./contracts/release-source.md)): `MatchArtistAsync` returns `Matched(string.Join(';', MusicBrainzIds.Parse(artist.Mbid)))` when that list is not empty; `FetchCataloguePageAsync` parses the stored ID, browses the first one with `inc=release-groups+artist-credits`, filters to releases whose credit IDs contain every ID when there are two or more, and fills `Credit`. T012 passes.
- [X] T019 [US2] Add `src/Jellyfin.Plugin.NewReleases/Storage/Migrations/003_source_entry_credit.sql` with `ALTER TABLE source_entry ADD COLUMN credit TEXT NOT NULL DEFAULT '[]';` and the header comment style of `002_match_detail.sql`. T013 passes.
- [X] T020 [US2] In `src/Jellyfin.Plugin.NewReleases/Storage/ReleaseRepository.cs`, write `credit` as JSON in the `source_entry` insert and update of `UpsertFromSourceAsync`, and read the union of a release's entry credits into `Release.Credit` where releases are read for ownership. T014 passes.
- [X] T021 [US2] In `src/Jellyfin.Plugin.NewReleases/ScheduledTasks/RefreshNewReleasesTask.cs` (research R6): after phase 3, build the index of snapshot artists by each parsed MBID and by `TitleNormalizer`-normalized name; in `DecideOwnershipAsync`, pass `OwnershipMatcher.Decide` the found-for artist's albums first, then the albums of every other credited library artist. `OwnershipMatcher.cs` does not change. T015 and T016 pass.

**Checkpoint**: US1 and US2 complete. Both suites green.

---

## Phase 5: User Story 3 - A busy source is given time before a retry (Priority: P2)

**Goal**: Each retry waits `max(Retry-After, backoff)`. The next-allowed floor is set only for a
hint longer than the backoff.

**Independent Test**: Serve `503` with `Retry-After: 0`, then `200`. The retry waits the
attempt's backoff on the fake clock and the fetch succeeds.

### Tests for User Story 3

- [X] T022 [P] [US3] Add cases to `tests/Jellyfin.Plugin.NewReleases.Tests/Sources/SourceHttpClientTests.cs`, with `TimeProviderStub` (research R7, [contracts/release-source.md](./contracts/release-source.md) "Retry"):
  - `Retry-After: 0` → the delay is `SourceLimits.RetryBackoffs[attempt]`; no next-allowed floor is set.
  - A hint longer than the backoff → the delay is the hint; the floor is now + hint.
  - A `Retry-After` date in the past → the backoff; no floor; no exception.
  - No `Retry-After` → the backoff, as before.

### Implementation for User Story 3

- [X] T023 [US3] In `src/Jellyfin.Plugin.NewReleases/Sources/SourceHttpClient.cs` `GetStringAsync`, set `delay` to the larger of the hint and `SourceLimits.RetryBackoffs[attempt]`, and call `SetNextAllowedAtAsync` only when the hint is the larger. Leave `SourceLimits.DefaultRetryAfter` as it is (research R7 note). T022 passes.

**Checkpoint**: US3 complete.

---

## Phase 6: User Story 4 - An interrupted run is closed (Priority: P3)

**Goal**: At host start, every run with no end time gets an end time and the outcome
`Interrupted`. An install whose check rejects the value logs one warning and starts.

**Independent Test**: Store an open run and a finished run, start the hosted service, and confirm
the open run is `Interrupted` with the start time and the finished run is unchanged.

### Tests for User Story 4

- [X] T024 [P] [US4] Add cases to `tests/Jellyfin.Plugin.NewReleases.Tests/Storage/SourceStateRepositoryTests.cs` for `CloseOpenRunsAsync(before)` (research R8):
  - An open run that started before `before` gets `ended_at` = now and outcome `Interrupted`; the method returns 1.
  - A finished run is unchanged; a run that started at or after `before` stays open.
  - A second call closes nothing (spec edge case "interrupted twice").
- [X] T025 [P] [US4] Create `tests/Jellyfin.Plugin.NewReleases.Tests/ScheduledTasks/InterruptedRunCloserTests.cs`: `StartAsync` closes an open run with the clock's time; on a database whose `refresh_run` check allows only `Completed`, `Cancelled`, `Failed` (built by hand in the test), it logs one warning through `RecordingLogger` and does not throw (FR-008); `StopAsync` does nothing. Add a case to `tests/Jellyfin.Plugin.NewReleases.Tests/PluginServiceRegistratorTests.cs` asserting `InterruptedRunCloser` is registered as an `IHostedService`.
- [X] T026 [P] [US4] Add a case to `tests/web/render-status.test.js`: a `lastRun` with outcome `Interrupted` and 0 errors renders `<time> (Interrupted)`.

### Implementation for User Story 4

- [X] T027 [US4] In `src/Jellyfin.Plugin.NewReleases/Storage/Migrations/001_initial.sql`, widen the `refresh_run.outcome` check to `('Completed','Cancelled','Failed','Interrupted')`. No new migration (Clarifications; plan Complexity Tracking).
- [X] T028 [US4] In `src/Jellyfin.Plugin.NewReleases/Storage/SourceStateRepository.cs`, add `CloseOpenRunsAsync(DateTimeOffset before, CancellationToken ct)` with the SQL of research R8. A check-constraint failure surfaces as `SqliteException`; the caller handles it. T024 passes.
- [X] T029 [US4] Create `src/Jellyfin.Plugin.NewReleases/ScheduledTasks/InterruptedRunCloser.cs`, an `IHostedService` whose `StartAsync` calls `CloseOpenRunsAsync(clock.GetUtcNow())` and turns a constraint `SqliteException` into one warning, and register it with `AddHostedService<InterruptedRunCloser>()` in `src/Jellyfin.Plugin.NewReleases/PluginServiceRegistrator.cs`. T025 and T026 pass.

**Checkpoint**: All four stories complete. Both suites green.

---

## Phase 7: Polish & Cross-Cutting Concerns

- [X] T030 [P] Amend `specs/001-track-new-releases/contracts/release-source.md` per [contracts/release-source.md](./contracts/release-source.md), and the admin status section of `specs/001-track-new-releases/contracts/http-api.md` per [contracts/http-api.md](./contracts/http-api.md) (the `Placeholder` step and the `Interrupted` outcome).
- [X] T031 [P] Add an Unreleased entry to `CHANGELOG.md`: placeholder artists such as Various Artists are skipped and shown with their own fix; joint artists refresh and keep only joint releases; ownership counts every credited library artist; retries wait at least the backoff; runs left open by a restart close as Interrupted.
- [X] T032 Run `PATH=/opt/homebrew/opt/dotnet/bin:$PATH dotnet build --configuration Release`. The build has zero warnings (`TreatWarningsAsErrors`).
- [X] T033 Run the full `dotnet test --configuration Release` and `node --test "tests/web/*.test.js"`. Both are green, including every `001`–`008` acceptance test (SC-006).
- [X] T034 [P] Confirm that `HttpSurfaceTests.NoContractDocument_NamesARouteThePluginDoesNotServe` in `tests/Jellyfin.Plugin.NewReleases.Tests/Api/HttpSurfaceTests.cs` passes with the amended `001` contracts and this feature's contracts.
- [X] T035 Commit. Push only when the maintainer asks, then verify that the CI run for the push is green (`gh run list --branch main`).

The real-server pass in [quickstart.md](./quickstart.md) §2 is the maintainer's own pass after
release.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Foundational (Phase 2)**: no dependency. Blocks US1 and US2.
- **US1 (Phase 3)**: after Phase 2.
- **US2 (Phase 4)**: after Phase 2, and after US1 for the shared files `RefreshNewReleasesTask.cs` and `RefreshNewReleasesTaskTests.cs`.
- **US3 (Phase 5)**: no dependency on Phase 2. Can run at any time.
- **US4 (Phase 6)**: no dependency on Phase 2. Can run at any time. T026 edits `render-status.test.js`, as T006 does; do not run them at the same time.
- **Polish (Phase 7)**: after every story.

### Within Each Story

- Tests before the implementation task that makes them pass.
- US1: T008 before T009 (T009 writes `MatchReasons.Placeholder`).
- US2: T011 before T012 and T015; T017 before T018, T020 and T021; T019 before T020.
- US4: T027 before T028; T028 before T029.

### Parallel Opportunities

- T001 alone, then T002.
- US1: T003, T004, T005, T006 together.
- US2: T012, T013, T014 together (after T011).
- US3 and US4 in parallel with US1 or US2.
- Polish: T030, T031, T034 together.

---

## Parallel Example: User Story 1

```bash
Task: "MatchReasons Placeholder cases in tests/Jellyfin.Plugin.NewReleases.Tests/Matching/MatchReasonsTests.cs"
Task: "Placeholder rule cases in tests/Jellyfin.Plugin.NewReleases.Tests/ScheduledTasks/RefreshNewReleasesTaskTests.cs"
Task: "Placeholder status case in tests/Jellyfin.Plugin.NewReleases.Tests/Api/AdminControllerTests.cs"
Task: "Placeholder row in tests/fixtures/pages/admin-status.json and tests/web/render-status.test.js"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Phase 2: Foundational.
2. Phase 3: US1. This alone lets a run reach the ownership step on the live library.
3. Validate: both suites green.

### Incremental Delivery

1. Foundational + US1 → the run finishes.
2. US3 → fewer artists fail on "busy".
3. US2 → joint artists refresh; cross-artist ownership.
4. US4 → run history is correct after a restart.
5. Polish → one release carries all four.

---

## Notes

- [P] tasks touch different files and depend on no incomplete task.
- `ArtistKey` does not change for joint artists (research R4); stored rows and decisions keep their keys.
- `SourceLimits.DefaultRetryAfter` stays unused; it is out of scope (research R7).
