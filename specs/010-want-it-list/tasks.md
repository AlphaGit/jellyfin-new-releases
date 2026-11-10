---

description: "Task list for 010-want-it-list"
---

# Tasks: Want it list

**Input**: Design documents from `/specs/010-want-it-list/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md),
[data-model.md](./data-model.md), [contracts/](./contracts/), [quickstart.md](./quickstart.md)

**Tests**: Included. [quickstart.md](./quickstart.md) maps every acceptance scenario to a test,
and the suite is the gate (constitution III, Development Workflow). No live network call.

**Organization**: A small foundational phase adds the enum member and the migration. Then one
phase per user story in spec priority order. US1 and US2 both edit `user-view.html` and
`ReleasesControllerTests.cs`, so run them one after the other. US3 touches other files and can
run in parallel with US1.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel: different files, and no dependency on an incomplete task.
- **[Story]**: US1–US3 from `spec.md`.

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

**Purpose**: The new decision kind must be storable before any story can use it.

- [X] T001 Add `WantIt` to `DecisionKind` in `src/Jellyfin.Plugin.NewReleases/Model/Enums.cs` and update its summary ("Ignore and Have it send a release to the Archive; Want it sends it to the Want-it list").
- [X] T002 Create `src/Jellyfin.Plugin.NewReleases/Storage/Migrations/004_decision_want_it.sql` per research R1: create `decision_new` with `CHECK (kind IN ('Ignore', 'HaveIt', 'WantIt'))` and the same columns and primary key, `INSERT … SELECT` every row, `DROP TABLE decision`, rename `decision_new` to `decision`. Confirm the `.csproj` embeds `Storage/Migrations/*.sql` (no change expected). Add a test in `tests/Jellyfin.Plugin.NewReleases.Tests/Storage/DatabaseTests.cs`: a database migrated only to version 3 with one `Ignore` and one `HaveIt` row keeps both rows (key, kind, user, time) after `004`, and then accepts a `WantIt` row. Depends on T001.

**Checkpoint**: Migration test green; existing storage tests still green.

---

## Phase 3: User Story 1 - Put a release on my Want-it list (Priority: P1) 🎯 MVP

**Goal**: Want it moves a Release from the List to a Want-it list that the user can open, with
the List's grouping, order and filters.

**Independent Test**: Mark one Release Want it, confirm it is gone from the List, open the
Want-it list, confirm it is there (quickstart steps 1 and 5).

- [X] T003 [US1] In `src/Jellyfin.Plugin.NewReleases/Storage/ReleaseRepository.cs` add `bool Wanted = false` as the last parameter of `ReleaseFilter` and change `ListAsync` to choose one fixed condition per view as in research R3 (List: not Owned and no decision; Want-it list: not Owned and `d.kind = 'WantIt'`; Archive: `d.kind IN ('Ignore','HaveIt')`). In `ReadListed`, leave `Archived` null when the kind is `WantIt`. Add repository tests next to the existing list tests in `tests/Jellyfin.Plugin.NewReleases.Tests/Storage/` (find the file with `ListAsync`): a `WantIt` release is only in the Want-it view; an Owned `WantIt` release is in none of the three views; `Ignore` and `HaveIt` releases keep their old behaviour; type, state, date and artist filters apply to the Want-it view; `Archived` is null in it. Depends on T001.
- [X] T004 [US1] In `src/Jellyfin.Plugin.NewReleases/Api/ReleasesController.cs` add `[FromQuery] bool wanted = false` to `GetReleasesAsync`, pass it into `ReleaseFilter`, and return `400 Bad Request` when `archived` and `wanted` are both true. Add `[HttpPost("Releases/{id:long}/WantIt")]` `WantItAsync` calling `DecideAsync(id, DecisionKind.WantIt, …)`, with the same `ProducesResponseType` as `HaveItAsync`; update the class summary and the `DecideAsync` summary. Depends on T003.
- [X] T005 [US1] Add tests to `tests/Jellyfin.Plugin.NewReleases.Tests/Api/ReleasesControllerTests.cs`: `WantIt` on a visible release writes a `WantIt` decision by the caller and answers 204; unknown id answers 404; a release in a library the caller may not see answers 403 and writes nothing; no user answers 401; `wanted=true` lists the release and the List does not; a library-restricted caller sees only releases of their libraries in the Want-it view; `archived=true&wanted=true` answers 400. The `wanted` response has the same shape as the List's, so no new fixture or naming case was added (`ResponseNamingTests` already pins it). Confirm `HttpSurfaceTests` still passes with the new route (PascalCase, no `api` segment). Depends on T004.
- [X] T006 [US1] In `src/Jellyfin.Plugin.NewReleases/Web/user-view.html` replace the boolean `archived` with `view` (`list` | `wantit` | `archive`): add the **Want it** tab between List and Archive (`id="nr-tab-wantit"`, `role="tab"`, `aria-selected`, `aria-controls="nr-panel"`), send `wanted=true` for it (`query()`), set `aria-labelledby` per view in `selectTab`, add the **Want it** button to List rows (`data-action="WantIt"`, `aria-label="Mark <title> as Want it"`), the announcement `Marked <title> as Want it`, and the empty state `Nothing on the Want it list. Choose Want it on a release to add it.` The "No data yet" message stays List-only, as for the Archive. Keep the Archive badge reading only `item.archived`. Update the header comment of the script if it names the two views.
- [X] T007 [US1] Add page tests in `tests/web/view.test.js` (and `tests/web/render.test.js` where it renders rows): three tabs exist with the right roles and `aria-selected`; the Want it tab requests `Releases?wanted=true` with the active filters; List rows have Want it, Ignore, Have it; clicking Want it posts to `Releases/{id}/WantIt`, removes the row and sets the announcement; the empty state text. Depends on T006.

**Checkpoint**: US1 works end to end: Want it from the List, view the Want-it list.

---

## Phase 4: User Story 2 - Close a Want-it release: bought or dropped (Priority: P2)

**Goal**: From the Want-it list: Have it and Ignore go to the Archive, Restore returns to the
List. From the Archive: Want it.

**Independent Test**: Put two releases on the Want-it list, mark one Have it and one Ignore,
confirm both are in the Archive; restore a third; mark an archived one Want it (quickstart steps
2 to 4).

- [X] T008 [US2] In `src/Jellyfin.Plugin.NewReleases/Web/user-view.html` set the buttons per view as in FR-002b: Want-it list rows get **Have it**, **Ignore**, **Restore**; Archive rows get **Restore** and **Want it**; List rows are unchanged. Reuse the existing `data-action` handler; no new endpoint. Depends on T006.
- [X] T009 [US2] Add page tests to `tests/web/view.test.js`: the exact button set per view (FR-002b); Have it, Ignore and Restore on a Want-it row post to `HaveIt`, `Ignore`, `Restore` and remove the row; Want it on an Archive row posts to `WantIt`. Depends on T008.
- [X] T010 [P] [US2] Add acceptance tests to `tests/Jellyfin.Plugin.NewReleases.Tests/Acceptance/ArchiveTests.cs` through the real repository and controller: Want it then Have it puts the release in the Archive as `HaveIt`; Want it then Ignore puts it there as `Ignore`; Want it then Restore returns it to the List; Archive then Want it moves it to the Want-it list (a new decision replaces the old, FR-007); an Owned release with a `WantIt` decision shows in no view and returns to the Want-it list when its ownership becomes Missing. Depends on T004.

**Checkpoint**: US1 and US2 work together; every transition in [data-model.md](./data-model.md) is tested.

---

## Phase 5: User Story 3 - Keep the Want-it list through refreshes and admin actions (Priority: P3)

**Goal**: Want-it decisions survive Purge release data and Clear Archive.

**Independent Test**: Mark a Release Want it, run Purge release data, refresh, confirm it is in
the Want-it list; run Clear Archive, confirm it is still there (quickstart steps 5 and 6).

- [X] T011 [US3] In `src/Jellyfin.Plugin.NewReleases/Storage/ArchiveRepository.cs` change `ClearAsync` to `DELETE FROM decision WHERE kind <> 'WantIt'` and update its summary and the class summary (Archive = Ignore and Have it; Want it lives beside it). Depends on T001.
- [X] T012 [P] [US3] Add tests: in `tests/Jellyfin.Plugin.NewReleases.Tests/Storage/ArchiveRepositoryTests.cs`, `ClearAsync` removes `Ignore` and `HaveIt` rows and keeps a `WantIt` row, and `SetAsync` accepts `WantIt`; in `tests/Jellyfin.Plugin.NewReleases.Tests/Api/AdminControllerTests.cs`, `ClearArchive` leaves a `WantIt` decision; in the acceptance tests of T010's file, purge then re-fetch the same release and the `WantIt` decision applies again (FR-005). Depends on T011.
- [X] T013 [US3] Check the Clear Archive confirmation in `src/Jellyfin.Plugin.NewReleases/Web/admin.html` (line 285: "Remove every Ignore and Have it decision? Archived releases reappear in the list."). It is already correct for FR-006: leave it unchanged unless a test or the spec says otherwise. Depends on T011.

**Checkpoint**: All three stories work.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [X] T014 [P] Update `docs/domain_knowledge/CONTEXT.md` per research R6: add rows **Want it** (user action: this Release is wanted but not yet in the library; moves it to the Want-it list; row button "Want it"; avoid "wishlist", "bookmark") and **Want-it list** (the set of Releases with a Want-it decision; shared server-wide, filtered by library access; not part of the Archive; tab "Want it"); reword **Restore** ("undo a decision; the Release returns to the list") and **Clear Archive** ("Want-it decisions stay"); add `WantIt` and `Wanted` to the code-identifier note; mention `010-want-it-list` under Related documents. Do not change other rows (FR-010).
- [X] T015 [P] Update `specs/001-track-new-releases/contracts/http-api.md` only with a one-line pointer under its decisions section to `specs/010-want-it-list/contracts/http-api.md`, as 008 and 009 do for their amendments (check how they did it and match).
- [X] T016 Run the suite gate from [quickstart.md](./quickstart.md): `dotnet build --configuration Release` with zero warnings, `dotnet test --configuration Release`, `node --test "tests/web/*.test.js"`. Fix any failure in the task that caused it. `CHANGELOG.md` gets its entry at release, not here.

---

## Dependencies & Execution Order

- **Foundational** (T001, T002) blocks everything.
- **US1** (T003 → T004 → T005; T006 → T007) is the MVP. T006 can start after T001 using the contract.
- **US2** needs US1: T008 after T006; T010 after T004.
- **US3**: T011 after T001, so it can run in parallel with US1. T012 and T013 follow T011.
- **Polish** after all stories; T014 and T015 can run any time after the plan.

### Parallel opportunities

- T003/T004/T005 (C#) and T006/T007 (page) after T001.
- T010 with T008/T009.
- T011/T012/T013 with US1.
- T014 and T015.

## Implementation Strategy

1. T001, T002, then US1 (T003–T007). Stop and check: Want it moves a release to a visible list.
2. US2 for the full shopping-list life cycle.
3. US3 so admin actions never lose the list.
4. Polish and the suite gate. The real-server pass is the maintainer's own, after release.
