---

description: "Task list for 011-unfollow-artist"
---

# Tasks: Unfollow artist

**Input**: Design documents from `/specs/011-unfollow-artist/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md),
[data-model.md](./data-model.md), [contracts/](./contracts/), [quickstart.md](./quickstart.md)

**Tests**: Included. [quickstart.md](./quickstart.md) maps every acceptance scenario to a test,
and the suite is the gate (constitution III, Development Workflow). No live network call.

**Organization**: A foundational phase adds the table, its repository and the artist lookup.
Then one phase per user story in spec priority order. US1 and US2 both edit `user-view.html`,
`ReleasesController.cs` and `ReleasesControllerTests.cs`, so run them one after the other. US4
edits other files and can run in parallel with US1. US3 adds acceptance tests that need US1 and
US2.

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

**Purpose**: The Unfollow decision must be storable and findable before any story can use it.

- [X] T001 Create `src/Jellyfin.Plugin.NewReleases/Storage/Migrations/005_unfollowed_artist.sql` per [data-model.md](./data-model.md): `CREATE TABLE unfollowed_artist (artist_key TEXT PRIMARY KEY, user_id TEXT NOT NULL, decided_at TEXT NOT NULL)`, no foreign key, no other statement. Confirm the `.csproj` embeds `Storage/Migrations/*.sql` (no change expected). Add a test in `tests/Jellyfin.Plugin.NewReleases.Tests/Storage/DatabaseTests.cs`: a database migrated only to version 4 with `decision` rows of all three kinds, a `library_artist` row and a `release` row keeps every row unchanged after `005`, and then accepts an `unfollowed_artist` row (FR-020, SC-006).
- [X] T002 In `src/Jellyfin.Plugin.NewReleases/Storage/UnfollowedArtistRepository.cs` add `UnfollowedArtistRepository` (parameterized SQL, same style as `ArchiveRepository`): `SetAsync(artistKey, userId, now, ct)` as an upsert, `RemoveAsync(artistKey, ct)`, `ClearAsync(ct)` (delete every row), and `GetKeysAsync(ct)` returning the set of keys. Register it as a singleton in `src/Jellyfin.Plugin.NewReleases/PluginServiceRegistrator.cs` next to `ArchiveRepository`. Add `tests/Jellyfin.Plugin.NewReleases.Tests/Storage/UnfollowedArtistRepositoryTests.cs` on a temporary database: set, set again (last write wins), remove, clear, get keys; a row survives `ArtistRepository.DeleteMissingAsync`, `ReleaseRepository.PurgeAsync` and `ArchiveRepository.ClearAsync`; the registrator test (`PluginServiceRegistratorTests.cs`) resolves the repository if it lists the others. Depends on T001.
- [X] T003 [P] In `src/Jellyfin.Plugin.NewReleases/Storage/ArtistRepository.cs` add `GetByJellyfinIdAsync(Guid jellyfinId, CancellationToken ct)` returning the `LibraryArtist` or null, using a parameterized query (not string interpolation). Add a test in `tests/Jellyfin.Plugin.NewReleases.Tests/Storage/ArtistRepositoryTests.cs`: finds an upserted artist, returns null for an unknown id.

**Checkpoint**: Migration test green; existing storage tests still green.

---

## Phase 3: User Story 1 - Unfollow an artist I do not want to track (Priority: P1) 🎯 MVP

**Goal**: Unfollow hides the artist's undecided Releases from the List for every user, and
Refresh skips the artist fully. The button sits next to the artist name on every row.

**Independent Test**: Unfollow one artist who has several listed Releases, confirm all leave the
List and a Refresh makes no source request for the artist (quickstart steps 1 and 2).

- [X] T004 [US1] In `src/Jellyfin.Plugin.NewReleases/Storage/ReleaseRepository.cs` change the List condition in `ListAsync` per [research.md](./research.md) R3: add `AND NOT EXISTS (SELECT 1 FROM unfollowed_artist u WHERE u.artist_key = a.artist_key)` to the List view only, and select `EXISTS (SELECT 1 FROM unfollowed_artist u WHERE u.artist_key = a.artist_key)` as an extra column. Add `bool ArtistUnfollowed` as the last member of `ListedRelease` in `src/Jellyfin.Plugin.NewReleases/Model/StoredRecords.cs` and read it in `ReadListed`. Add repository tests next to the existing list tests in `tests/Jellyfin.Plugin.NewReleases.Tests/Storage/ReleaseRepositoryTests.cs`: the List omits an unfollowed artist's undecided Releases and keeps a followed artist's; the Want-it view and the Archive keep an unfollowed artist's rows; rows carry `ArtistUnfollowed`; an Ignored Release of an unfollowed artist, after its decision is removed, is not in the List (US3 scenario 4). Depends on T001.
- [X] T005 [US1] In `src/Jellyfin.Plugin.NewReleases/ScheduledTasks/RefreshNewReleasesTask.cs` add an `UnfollowedArtistRepository` constructor parameter (and the matching field), read `GetKeysAsync` once after `DeleteMissingAsync`, and drop those artists from `rotation` before step 3 so both the source loop and the ownership loop skip them (research R2); progress uses the filtered count. Keep the library snapshot unfiltered. Update every caller of the constructor: `tests/Jellyfin.Plugin.NewReleases.Tests/Acceptance/AcceptanceRig.cs`, `Api/AdminControllerTests.cs`, `Api/ReleasesControllerTests.cs`. Add tests in `tests/Jellyfin.Plugin.NewReleases.Tests/ScheduledTasks/RefreshNewReleasesTaskTests.cs` with a stubbed source: an unfollowed artist gets zero source calls, no ownership write, no `last_refreshed_at` change, and its Releases, Editions and decisions are unchanged (SC-003, FR-008); a followed artist credited with an album of an unfollowed artist still gets its ownership check (FR-019). Depends on T002.
- [X] T006 [US1] In `src/Jellyfin.Plugin.NewReleases/Api/Dtos.cs` add `bool ArtistUnfollowed` to `ReleaseDto` and set it in `ToDto` in `src/Jellyfin.Plugin.NewReleases/Api/ReleasesController.cs`. In the same controller add `[HttpPost("Artists/{jellyfinId:guid}/Unfollow")]` `UnfollowAsync` per [contracts/http-api.md](./contracts/http-api.md): `401` without a user, `404` for an unknown id (via `GetByJellyfinIdAsync`), `403` when `access.CanSee(artist.LibraryIds)` is false, otherwise `UnfollowedArtistRepository.SetAsync(artist.ArtistKey, userId, now)` and `204`; idempotent. Add the repository to both constructors. Update the response fixtures: `tests/fixtures/pages/releases.json` gains `artistUnfollowed` on every item (one `true` sample); extend `tests/Jellyfin.Plugin.NewReleases.Tests/Api/ResponseNamingTests.cs` so the name is pinned. Add controller tests in `tests/Jellyfin.Plugin.NewReleases.Tests/Api/ReleasesControllerTests.cs`: Unfollow writes the caller's id and answers `204`; again answers `204` with one row; `401`, `404`, `403` write nothing; the List hides the artist's undecided Releases and the Want-it list and Archive keep theirs. Confirm `HttpSurfaceTests` still passes (PascalCase route, camelCase profile). Depends on T002, T003, T004.
- [X] T007 [US1] In `src/Jellyfin.Plugin.NewReleases/Web/user-view.html` add the artist button in `row()` inside the `.nr-artist` line, after the name link: `<button type="button" class="nr-artist-action" data-artist-action="Unfollow" aria-label="Unfollow <artist>">Unfollow</button>` styled as a small text-style button (a few lines of CSS next to the existing rules, visibly different from `.nr-actions` buttons). It is not inside `.nr-actions` and does not use `data-action`. Add a click handler on the panel for `button[data-artist-action]`: `api('POST', 'Artists/' + item.artistJellyfinId + '/' + action)`, then reload and announce `Unfollowed <artist>`. Carry `data-artist-id` and `data-artist-name` on the row. No confirmation (FR-002). The page is wired for the Follow label in T011. Depends on T006.
- [X] T008 [US1] Add page tests in `tests/web/view.test.js` (and `tests/web/render.test.js` where it renders rows): every row in the List, Want it and Archive views has the artist button in the artist line, outside `.nr-actions`, without `data-action`, with an `aria-label` naming the artist; clicking it posts to `Artists/{artistJellyfinId}/Unfollow` with no confirm call, reloads, and sets the announcement; the Release buttons per view are unchanged. Depends on T007.

**Checkpoint**: US1 works end to end: Unfollow from any row, the List drops the artist, Refresh skips it.

---

## Phase 4: User Story 2 - Follow an artist again (Priority: P2)

**Goal**: The Unfollowed artists tab lists unfollowed library artists with a Follow button, and
rows of unfollowed artists in the Want-it list and the Archive read "Follow".

**Independent Test**: Unfollow an artist, open the Unfollowed artists tab, choose Follow, confirm
its undecided Releases are in the List again (quickstart steps 1 and 4).

- [X] T009 [US2] In `src/Jellyfin.Plugin.NewReleases/Api/ReleasesController.cs` add `[HttpPost("Artists/{jellyfinId:guid}/Follow")]` `FollowAsync` (same statuses as Unfollow, calls `RemoveAsync`, idempotent) and `[HttpGet("Artists/Unfollowed")]` `GetUnfollowedArtistsAsync` returning `ArtistsResponse`: the `library_artist` rows whose key is in `GetKeysAsync`, filtered by `access.CanSee(a.LibraryIds)`, ordered by name, only artists currently in the library (research R5). Use the existing `ArtistsResponse` and `ArtistDto`; no new DTO. Add `tests/fixtures/pages/unfollowed-artists.json` and pin it in `ResponseNamingTests.cs`. Add controller tests in `tests/Jellyfin.Plugin.NewReleases.Tests/Api/ReleasesControllerTests.cs`: Follow removes the row and answers `204`, again answers `204`, `401`/`404`/`403` write nothing; Follow leaves every Release decision unchanged (FR-007); the list is sorted by name, hides artists of libraries the caller may not see, omits an unfollowed key with no library artist (FR-011, FR-014), and `GET Artists` still returns every library artist (FR-016); the List shows the artist's undecided Releases again after Follow. Depends on T006.
- [X] T010 [US2] In `src/Jellyfin.Plugin.NewReleases/Web/user-view.html` add the fourth tab `nr-tab-unfollowed` "Unfollowed artists" after Archive (`role="tab"`, `aria-selected`, `aria-controls="nr-panel"`) and a `view` value `unfollowed` in `selectTab`. For this view call `GET Artists/Unfollowed` and render one row per artist: the name as a link (`artistLink`-style URL to the Jellyfin artist page) and a **Follow** button (`data-artist-action="Follow"`, `aria-label="Follow <artist>"`), sorted as returned, no counts. Hide the filter form (`#nr-filters`) on this tab and show it again on the others. Empty state: `No unfollowed artists. Choose Unfollow next to an artist name to add one.` The "No data yet" message stays List-only. Reuse the T007 click handler for `Follow`, announcing `Followed <artist>`. Depends on T007, T009.
- [X] T011 [US2] In the same file, make the row button in `row()` read **Follow** with `data-artist-action="Follow"` and `aria-label="Follow <artist>"` when `item.artistUnfollowed` is true, else **Unfollow** (FR-013). Add page tests in `tests/web/view.test.js`: four tabs with the right roles and `aria-selected`; the Unfollowed artists tab requests `Artists/Unfollowed`, shows name link and Follow, hides the filters and shows them again on another tab, shows the empty state; Follow posts to `Artists/{id}/Follow` and the row leaves; a Want-it or Archive row with `artistUnfollowed: true` shows Follow and posts to Follow; the Artist filter datalist still lists every artist from `GET Artists` (FR-016). Depends on T010.

**Checkpoint**: US1 and US2 work together: unfollow and follow from the page.

---

## Phase 5: User Story 3 - Keep decisions I already made (Priority: P3)

**Goal**: Unfollow and Follow never change a Have it, Want it or Ignore decision, and those
Releases stay in their tabs. The Unfollow survives Purge release data, Clear Archive and an
absence.

**Independent Test**: Mark one Release Want it and another Ignore, Unfollow the artist, confirm
both stay; Follow, confirm both are still there (quickstart steps 3, 5, 6 and 7).

- [X] T012 [P] [US3] Add acceptance tests to `tests/Jellyfin.Plugin.NewReleases.Tests/Acceptance/ArchiveTests.cs` through the real repositories and controller: Unfollow then Follow leaves `WantIt`, `Ignore` and `HaveIt` decisions identical (key, kind, user, time); the Want-it Release and the Archive Releases of an unfollowed artist stay in their views and can still be acted on (Have it, Ignore, Restore); Restore of an Ignored Release of an unfollowed artist leaves it out of the List until Follow; Purge release data and Clear Archive leave the artist unfollowed; a Want-it Release of an unfollowed artist stays listed after the library gains the album, because no ownership check runs (US3 scenario 5); remove the artist from the library snapshot, run a Refresh, add it back, run a Refresh: it is unfollowed again and absent from the Unfollowed artists list while gone (FR-014). Depends on T005, T009.

**Checkpoint**: Every decision-safety rule of the spec has a test.

---

## Phase 6: User Story 4 - See and manage unfollowed artists as admin (Priority: P4)

**Goal**: The admin page counts unfollowed artists on their own line, leaves them out of the
matched and unmatched sections, and offers Clear unfollowed artists.

**Independent Test**: Unfollow two artists, one Unmatched; confirm "Unfollowed artists: 2", the
Unmatched artist is gone from its section, the library count is unchanged; run Clear unfollowed
artists and confirm both are followed (quickstart step 8).

- [X] T013 [US4] In `src/Jellyfin.Plugin.NewReleases/Storage/ArtistRepository.cs` change `GetCountsAsync` per research R6: `LibraryArtists` stays `COUNT(*)` of `library_artist`; the per-source matched counts and the unmatched list exclude artists whose key is in `unfollowed_artist`; add `Unfollowed`, the count of `library_artist` rows with such a key, to `ArtistCounts`. Add `int UnfollowedArtists` to `AdminStatusResponse` in `src/Jellyfin.Plugin.NewReleases/Api/Dtos.cs` and set it in `GetStatusAsync` in `src/Jellyfin.Plugin.NewReleases/Api/AdminController.cs`. Update `tests/fixtures/pages/admin-status.json` and `admin-status-quiet.json` (new field) and `ResponseNamingTests.cs`. Add tests in `tests/Jellyfin.Plugin.NewReleases.Tests/Storage/ArtistRepositoryTests.cs` and `Api/AdminControllerTests.cs`: total equals followed plus unfollowed with no artist counted twice (SC-007); an unfollowed Unmatched artist is not in `unmatched` and not in `matchedArtists`; an unfollowed key with no library artist is not counted. Depends on T001.
- [X] T014 [US4] In `src/Jellyfin.Plugin.NewReleases/Api/AdminController.cs` add `[HttpPost("ClearUnfollowedArtists")]` `ClearUnfollowedArtistsAsync` calling `UnfollowedArtistRepository.ClearAsync`, answering `204`, logging a warning like `ClearArchiveAsync`; add the repository to both constructors and update `tests/Jellyfin.Plugin.NewReleases.Tests/Api/AdminControllerTests.cs` for them. Tests: it removes every row (absent artists too), leaves Releases, `WantIt`, `Ignore` and `HaveIt` rows untouched, and `Purge` and `ClearArchive` leave the rows (FR-015, FR-018). Confirm `HttpSurfaceTests` and the elevation attribute on the controller cover the new action. Depends on T002, T013.
- [X] T015 [US4] In `src/Jellyfin.Plugin.NewReleases/Web/admin.html` show `Unfollowed artists: N` beside the library artist count (from `status.unfollowedArtists`) and add a button **Clear unfollowed artists** next to Clear Archive (`id="nr-clear-unfollowed"`), with the same confirm step and request helper: `Follow every unfollowed artist again? Their Releases show in the list again.` → `ClearUnfollowedArtists`, then `Unfollowed artists cleared.` Add page tests in `tests/web/render-status.test.js` (and the matching admin test file): the line renders from the fixture, the button posts to `Admin/ClearUnfollowedArtists` only after the confirm, and the page literal for the API prefix is unchanged. Depends on T014.

**Checkpoint**: All four stories work.

---

## Phase 7: Polish & Cross-Cutting Concerns

- [X] T016 [P] Update `docs/domain_knowledge/CONTEXT.md` per research R8 (FR-021): add rows **Followed artist**, **Unfollowed artist**, **Unfollow** (user action next to the artist name; server-wide; hides the artist's undecided Releases; Refresh skips the artist; button "Unfollow") and **Follow** (undoes it; button "Follow"; tab **Unfollowed artists**); remove "tracked artist" and "followed artist" from the _Avoid_ cell of **Library artist**; add one "Domain boundaries" bullet (artist-level, server-wide, never changes a Release decision, kept while the artist is out of the library); add `UnfollowedArtist` to the code-identifier note; link `011-unfollow-artist` under Related documents. Do not change other rows.
- [X] T017 [P] Update `specs/001-track-new-releases/contracts/http-api.md` only with a one-line pointer to `specs/011-unfollow-artist/contracts/http-api.md`, matching how 008, 009 and 010 point to their amendments. In `docs/http-surface.md` change nothing unless it lists routes by name; if it does, add the new ones.
- [X] T018 Run the suite gate from [quickstart.md](./quickstart.md): `dotnet build --configuration Release` with zero warnings, `dotnet test --configuration Release`, `node --test "tests/web/*.test.js"`. Fix any failure in the task that caused it. `CHANGELOG.md` and the version get their entry at release, not here.

---

## Dependencies & Execution Order

- **Foundational** (T001 → T002; T003) blocks the stories. T003 is independent of T001 and T002.
- **US1** (T004, T005 in parallel after the foundation; T006 after T002, T003, T004; T007 → T008) is the MVP.
- **US2** needs US1: T009 after T006; T010 after T007 and T009; T011 after T010.
- **US3**: T012 after T005 and T009.
- **US4**: T013 after T001, so it can run in parallel with US1. T014 after T002 and T013; T015 after T014. T015 edits `admin.html` only, so it never clashes with US1 and US2.
- **Polish** after all stories; T016 and T017 can run any time after the plan.

### Parallel opportunities

- T003 with T001 and T002.
- T004 with T005 (different files).
- US4 (T013–T015) with US1 and US2.
- T012 with US4.
- T016 and T017 with anything.

## Implementation Strategy

1. T001–T003, then US1 (T004–T008). Stop and check: Unfollow hides the artist in the List and
   Refresh skips it. This is the MVP; with no way back except the database, ship nothing yet.
2. US2 to complete the round trip (Follow, the tab, the row label).
3. US3 tests lock the decision-safety rules; US4 gives the admin view and reset.
4. Polish, then the suite gate. The maintainer runs the real-server pass after release.
