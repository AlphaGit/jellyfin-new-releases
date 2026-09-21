---

description: "Task list for 005-page-json-casing"
---

# Tasks: Make the pages read what the server actually sends

**Input**: Design documents from `/specs/005-page-json-casing/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md),
[data-model.md](./data-model.md), [contracts/](./contracts/),
[tdd/test-list.md](./tdd/test-list.md)

**Tests**: **Mandatory.** Constitution II is non-negotiable, and `spec.md` makes "the mismatch
cannot return unnoticed" a P1 user story of its own. Every behavioural task carries the behaviour
ids it covers in brackets, and `/speckit-tdd-run` drives each behaviour red before its
implementation task starts.

**Organization**: grouped by user story. All three stories are P1 — `spec.md` states US3 is "equal
to the other two" — so the order below is a dependency order, not a priority order.

## Format: `[ID] [P?] [Story] Description [Behaviors]`

- **[P]**: can run in parallel (different files, no dependency on an incomplete task)
- **[Story]**: US1, US2, US3
- **[Uxx] / [Axx]**: behaviour ids from [`tdd/test-list.md`](./tdd/test-list.md). A task with no
  id is infrastructure or documentation, not a behaviour change.
- Exact file paths in every description

## Commands

```bash
dotnet test --configuration Release --filter "FullyQualifiedName~{Class}.{Method}" -- RunConfiguration.TreatNoTestsAsError=true
dotnet test --configuration Release
node --test tests/web/{file}
node --test "tests/web/*.test.js"
LANG=de_DE.UTF-8 node --test "tests/web/*.test.js"
```

The `--` argument is mandatory: without it a dotnet filter matching nothing exits 0. The node
stack has no safe single-test invocation for the same reason, so a **file** is the unit of work
there. A shell started before 2026-09-20 may resolve `dotnet@9` and fail with `NETSDK1045`;
prefix `PATH=/opt/homebrew/opt/dotnet/bin:$PATH DOTNET_ROOT=/opt/homebrew/opt/dotnet/libexec`.

## The red this feature starts from

Measured before planning and recorded in [`tdd/cycle-log.md`](./tdd/cycle-log.md):
`JsonDefaults.Options` — what an endpoint gets when it declares nothing — has **no naming
policy**, so it writes `Items`, not `items`.

**Every naming test below resolves the serializer options from the endpoint's own `[Produces]`
declaration**, falling back to `JsonDefaults.Options` when it declares no JSON type. A test that
reaches for `CamelCaseOptions` directly is green today and proves nothing — it repeats the mistake
the 257 existing tests made.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: the committed fixtures every later assertion reads. They are the physical form of the
contract, so nothing can be written before them.

Built from `src/Jellyfin.Plugin.NewReleases/Api/Dtos.cs` and the name tables in
[`data-model.md` §1.1](./data-model.md). Synthetic data only; no keys, no personal data
(constitution III).

- [ ] T001 [P] Create `tests/fixtures/pages/releases.json` — a `ListResponse` with one row of each `state` (`Missing`, `Incomplete`, `Upcoming`), the `Incomplete` row carrying `missingTracks` and `comparedEdition`, one row with `date: null`, one row carrying `archived.kind`, and every row carrying `sources[]`
- [ ] T002 [P] Create `tests/fixtures/pages/releases-empty.json` — `hasStoredReleases: false`, `items: []`, `releasesLastCheckedAt: null`
- [ ] T003 [P] Create `tests/fixtures/pages/releases-filtered.json` — `hasStoredReleases: true` with a single `items` entry, the shape a narrowed filter returns
- [ ] T004 [P] Create `tests/fixtures/pages/releases-stale.json` — `hasStoredReleases: true` with a `releasesLastCheckedAt` older than its `refreshIntervalHours`
- [ ] T005 [P] Create `tests/fixtures/pages/artists.json` — an `ArtistsResponse` with two `ArtistDto` entries
- [ ] T006 [P] Create `tests/fixtures/pages/admin-status.json` — both sources (one `Ok` carrying a `lastError`, one `CoolingDown` with `cooldownUntil` and `lastError`), a completed `lastRun`, non-null `nextRunAt` and `releasesLastCheckedAt`, and two `unmatched[]` entries with per-source reasons and the hint
- [ ] T007 [P] Create `tests/fixtures/pages/admin-status-quiet.json` — `unmatched: []`, `releasesLastCheckedAt: null`, `isRunning: true`
- [ ] T008 [P] Create `tests/fixtures/pages/status.json` — a `StatusResponse`, read by no page but covered by `FR-010`
- [ ] T009 Add `tests/fixtures/pages/` to the copy-to-output item group in `tests/Jellyfin.Plugin.NewReleases.Tests/Jellyfin.Plugin.NewReleases.Tests.csproj`, following the existing `tests/fixtures/<source>/` entry
- [ ] T010 Record the new fixture folder and its purpose in `tests/fixtures/README.md`

**Checkpoint**: eight fixtures exist and reach the test output.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: the single route source and the page stand-in. Every user story phase depends on both.

**⚠️ CRITICAL**: no user story work begins until this phase is complete.

- [ ] T011 Create `src/Jellyfin.Plugin.NewReleases/Api/PluginRoutes.cs` — `const string` members `Base = "Plugins/NewReleases"`, `Admin = Base + "/Admin"`, `UserView = Base + "/UserView"`, `UserViewAbsolute = "/" + UserView`, with an XML comment stating it is the single authoritative source (`FR-013`)
- [X] T012 Write failing `tests/web/fake-dom.test.js` for the stand-in's own contract, before the stand-in exists [U52] [U53] [U54]
- [X] T013 Create `tests/web/fake-dom.js` until T012 is green — the string-capturing stand-in specified in [`contracts/page-sandbox.md`](./contracts/page-sandbox.md). **No HTML parser.** The file header records what it does not cover, verbatim from that contract (`FR-017`, `FR-018`) [U52] [U53] [U54]
- [X] T014 Extend `tests/web/load-page.js` to install the fake DOM in place of the null-returning `document` stub, keeping the existing `Intl` and `Date` pinning and the `overrides` merge behaviour unchanged; `node --test "tests/web/*.test.js"` stays green [U55]

**Checkpoint**: `PluginRoutes` exists, the sandbox has a DOM, the existing page suite is untouched.

---

## Phase 3: User Story 1 — A person sees their releases (Priority: P1) 🎯 MVP

**Goal**: `user-view.html` lists the releases the server holds, with every field populated, and
still shows the empty state when there is genuinely nothing.

**Independent Test**: with releases stored, open the view and confirm the list matches what the API
returns (`quickstart.md` pass 2 steps 1–4).

### Tests for User Story 1 ⚠️

> Write these FIRST. Observe each failing, for the right reason, and record the failure in
> [`tdd/cycle-log.md`](./tdd/cycle-log.md). `U29`–`U38` are **characterization** behaviours:
> `render` is correct as written and has never had a test, so they go green against untouched
> code and terminate at `BASELINE`. Their value is the guard, not a red — confirm each is green
> for the right reason and that it fails under the T060 mutant.

- [X] T015 [P] [US1] Write failing `tests/Jellyfin.Plugin.NewReleases.Tests/Api/ResponseNamingTests.cs` with the options-resolution helper (effective `[Produces]` → `CamelCaseOptions` / `PascalCaseOptions` / `JsonDefaults.Options`) and its first case: `ListResponse` serialized through `ReleasesController`'s declaration carries the names in `tests/fixtures/pages/releases.json`, at every nesting level [U1]
- [X] T016 [P] [US1] Add failing `ResponseNamingTests.cs` case for `ArtistsResponse` against `tests/fixtures/pages/artists.json` [U2]
- [X] T017 [US1] Write failing `tests/web/render.test.js` — `render` with `tests/fixtures/pages/releases.json` writes a row per item, and writes no "waiting for its first refresh" message [U29] [U30]
- [X] T018 [US1] Add failing `render.test.js` cases for `tests/fixtures/pages/releases-empty.json` — the message appears and no row does [U31] [U32]
- [X] T019 [US1] Add a failing `render.test.js` case for stored releases with an empty `items` — "Nothing missing for this selection." [U33]
- [X] T020 [US1] Add a failing `render.test.js` case for `tests/fixtures/pages/releases-filtered.json` — the narrowed response renders only its single row [A3]
- [X] T021 [US1] Add failing `render.test.js` cases for a rendered row's artist name, title, type, date and state; for an `Incomplete` row's missing track titles and compared edition; and for one link per `sources` entry [U34] [U35] [U36]
- [X] T022 [US1] Add a failing `render.test.js` case for a row whose `date` is null — grouped as `Undated` and printed as "Undated" [U34]
- [X] T023 [US1] Add a failing `render.test.js` case for the Archive tab — the archived badge and the kind of the decision [U37]
- [X] T024 [US1] Add failing `render.test.js` cases for the staleness line: `releasesLastCheckedAt: null` writes no sentence and does not hide the list; `tests/fixtures/pages/releases-stale.json` writes `002`'s sentence [U38] [A8]
- [X] T025 [US1] Write failing `HttpSurfaceTests.cs` cases for `ReleasesController`'s registered routes — `Releases`, `Artists`, `Status`, and `Releases/{id}/Ignore|HaveIt|Restore` under prefix `PluginRoutes.Base` [U4] [U5] [U6] [U7] [U8]
- [X] T026 [US1] Write failing `render.test.js` cases for the paths the page sends — `Releases`, `Artists`, and `Releases/{id}/Ignore|HaveIt|Restore` — driving them through a recording `ApiClient` override [U39]

### Implementation for User Story 1

- [X] T027 [US1] Expose `render` on `globalThis.NewReleasesInternals` in `src/Jellyfin.Plugin.NewReleases/Web/user-view.html`, keeping the assignment as the first statement of the IIFE, and add `render` to the exact set asserted in `tests/web/exposure.test.js` [U40]
- [X] T028 [US1] Add `[Produces(JsonDefaults.CamelCaseMediaType)]` at class level on `src/Jellyfin.Plugin.NewReleases/Api/ReleasesController.cs`, with `using Jellyfin.Extensions.Json;`, until T015, T016 are green [U1] [U2]
- [X] T029 [US1] Rename `ReleasesController` routes to `[Route(PluginRoutes.Base)]` with `[HttpGet("Releases")]`, `[HttpGet("Artists")]`, `[HttpGet("Status")]`, `[HttpPost("Releases/{id:long}/Ignore")]`, `[HttpPost("Releases/{id:long}/HaveIt")]`, `[HttpPost("Releases/{id:long}/Restore")]` until T025 is green [U4] [U5] [U6] [U7] [U8]
- [X] T030 [US1] Update `src/Jellyfin.Plugin.NewReleases/Web/user-view.html` until T026 is green: the `API` literal becomes `'Plugins/NewReleases/'`, the request paths become `'Releases' + query()` and `'Artists'`, and the three `data-action` values become `Ignore`, `HaveIt`, `Restore`. Paths still go through `ApiClient.getUrl` (`FR-014`) [U39]
- [ ] T031 [US1] Replace the stale `// Jellyfin serializes camelCase` comment in `src/Jellyfin.Plugin.NewReleases/Api/Dtos.cs` with a reference to `contracts/http-surface.md` — the naming is now stated to the host, not assumed in a comment
- [ ] T032 [US1] Outer loop green before the story is complete: `A1`, `A2`, `A3`, `A4` and `A8` all pass against their fixtures, and `tdd/test-list.md` records each with its test [A1] [A2] [A3] [A4] [A8]

**Checkpoint**: the user view renders real data against a real response shape, and the empty state
is still reachable.

---

## Phase 4: User Story 2 — An administrator sees the plugin's true state (Priority: P1)

**Goal**: every status value on `admin.html` shows a real value after a completed refresh.

**Independent Test**: with a completed refresh on record, open the administrator page and confirm
each status value matches the API (`quickstart.md` pass 2 steps 5–8).

### Tests for User Story 2 ⚠️

- [X] T033 [P] [US2] Add a failing `ResponseNamingTests.cs` case — `AdminStatusResponse` serialized through `AdminController`'s declaration carries the names in `tests/fixtures/pages/admin-status.json`, including `sources[]`, `lastRun`, `unmatched[]` and `unmatched[].sources[]` [U9]
- [X] T034 [US2] Write failing `tests/web/render-status.test.js` — `renderStatus` with `tests/fixtures/pages/admin-status.json` writes the last refresh instant and outcome, the releases-last-checked sentence, the next run, the artists processed and the releases found, **none a dash** [U41] [U42] [U43] [U44]
- [X] T035 [US2] Add failing `render-status.test.js` cases for each source's health, calls today and daily budget, and for a cooling-down source's `cooldownUntil` and last error [U45] [U46]
- [X] T036 [US2] Add failing `render-status.test.js` cases for the unmatched table — one row per artist with its per-source reasons and the hint; and with `tests/fixtures/pages/admin-status-quiet.json` the table is hidden and the empty line shown [U47] [U48]
- [X] T037 [US2] Add failing `render-status.test.js` cases for `tests/fixtures/pages/admin-status-quiet.json` — `releasesLastCheckedAt: null` writes a dash, and `isRunning: true` writes "Running now" for the next run [U49] [U43]
- [X] T038 [US2] Add a failing `render-status.test.js` case for a source whose `lastError` is present while its health is `Ok` — the error is suppressed, driven from a real response shape [U45]
- [X] T039 [US2] Write failing `HttpSurfaceTests.cs` cases for `AdminController`'s registered routes — `Status`, `RunNow`, `Purge`, `ClearArchive` under prefix `PluginRoutes.Admin` [U10] [U11] [U12]
- [X] T040 [US2] Write failing `render-status.test.js` cases for the paths the page sends — `Status`, `RunNow`, `Purge`, `ClearArchive` — through a recording `ApiClient` override [U50]

### Implementation for User Story 2

- [X] T041 [US2] Expose `renderStatus` on `globalThis.NewReleasesInternals` in `src/Jellyfin.Plugin.NewReleases/Web/admin.html` as the first statement of the IIFE, and add `renderStatus` to the exact set asserted in `tests/web/exposure.test.js` [U51]
- [X] T042 [US2] Add `[Produces(JsonDefaults.CamelCaseMediaType)]` at class level on `src/Jellyfin.Plugin.NewReleases/Api/AdminController.cs`, with `using Jellyfin.Extensions.Json;`, until T033 is green [U9]
- [X] T043 [US2] Rename `AdminController` routes to `[Route(PluginRoutes.Admin)]` with `[HttpGet("Status")]`, `[HttpPost("RunNow")]`, `[HttpPost("Purge")]`, `[HttpPost("ClearArchive")]` until T039 is green [U10] [U11] [U12]
- [X] T044 [US2] Update `src/Jellyfin.Plugin.NewReleases/Web/admin.html` until T040 is green: the `API` literal becomes `'Plugins/NewReleases/Admin/'` and the three `confirmed(...)` call sites pass `RunNow`, `Purge`, `ClearArchive` [U50]
- [ ] T045 [US2] Outer loop green before the story is complete: `A5`, `A6`, `A7` and `A8`'s administrator half all pass against their fixtures [A5] [A6] [A7]

**Checkpoint**: the administrator page shows real values, and both JSON controllers state their
naming.

---

## Phase 5: User Story 3 — The mismatch cannot return unnoticed (Priority: P1)

**Goal**: a rename on either side, or an endpoint added without the declaration, fails the suite.

**Independent Test**: rename the server's responses the way Jellyfin 12 renamed them and confirm
the suite fails (`quickstart.md` pass 1 scenarios 1–6).

### Tests for User Story 3 ⚠️

> `U15`–`U22` and `U25`–`U27` are **predicates**. The profile's standing rule applies: write the
> accepting and rejecting cases as a `[Theory]` table from the requirement **before** the
> predicate exists. Three consecutive remediations on this project each fixed one defect and
> introduced another, every time because the fix was demonstrated with a single example chosen
> after the implementation was written.

- [X] T046 [P] [US3] Write the failing `[Theory]` table for the naming-declaration rule in `tests/Jellyfin.Plugin.NewReleases.Tests/Api/HttpSurfaceTests.cs` — a JSON action without the profile fails; an action producing no `application/json` type passes; an action inheriting the profile from its controller passes. Table first, no scanning code [U15] [U16] [U17]
- [X] T047 [US3] Write failing `HttpSurfaceTests.cs::EveryJsonAction_DeclaresTheCamelCaseProfile` — reflect over every public action on every `ControllerBase` in the plugin assembly and apply the rule from T046 (`FR-010`, `SC-004`) [U18]
- [X] T048 [P] [US3] Write the failing `[Theory]` table for the route-casing rule in `HttpSurfaceTests.cs` — a lower-case segment fails, an `api` segment fails, a PascalCase multi-word segment with no separator passes. Table first [U19] [U20] [U21]
- [X] T049 [US3] Write failing `HttpSurfaceTests.cs::EveryRoute_UsesPascalCaseSegmentsWithNoApiSegment` applying the rule from T048 to every route the plugin registers [U22]
- [X] T050 [P] [US3] Write the failing `[Theory]` table for the contract-document rule in `HttpSurfaceTests.cs` — a path the plugin does not serve fails, a path it does serve passes. Table first [U25] [U26]
- [X] T051 [US3] Write failing `HttpSurfaceTests.cs::NoContractDocument_NamesARouteThePluginDoesNotServe` — scan `specs/**/contracts/*.md` through `Support/RepositoryFiles.cs` and apply the rule from T050 (`SC-008`) [U27]
- [X] T052 [US3] Write failing `HttpSurfaceTests.cs::BothEmbeddedPages_BuildTheirPathsFromPluginRoutes` — each page's `API` literal begins with `PluginRoutes.Base`, and the failure message names the page that has not followed [U23]
- [X] T053 [US3] ~~Write failing `HttpSurfaceTests.cs::EveryPageRequestPath_ResolvesToARegisteredRoute`~~ — dropped in cycle 16 with `U24`; see `tdd/cycle-log.md`. `U39` and `U50` capture the real requests instead [U24]
- [X] T054 [P] [US3] Add a failing `ResponseNamingTests.cs` case for `StatusResponse` against `tests/fixtures/pages/status.json` — read by no page, covered by `FR-010` [U3]
- [X] T055 [US3] Write failing `HttpSurfaceTests.cs::UserViewController_DeclaresTextHtmlAndNoJsonProfile` — the one endpoint the naming rule exempts, and the only entry in the exceptions table [U14]
- [X] T056 [US3] Write failing `HttpSurfaceTests.cs::UserViewController_ServesPluginRoutesUserView` — the route is `PluginRoutes.UserView` and the served path is still `Plugins/NewReleases/UserView` [U13]
- [X] T057 [US3] Write failing `Integration/PluginPagesRegistrationTests.cs::PageEntryUrl_EqualsPluginRoutesUserViewAbsolute` alongside the existing literal assertion, which stays [U28]

### Implementation for User Story 3

- [X] T058 [US3] Set `src/Jellyfin.Plugin.NewReleases/Api/UserViewController.cs`'s route to `[Route(PluginRoutes.UserView)]`, leaving the served path and its `[Produces("text/html")]` unchanged, until T055, T056 are green [U13] [U14]
- [X] T059 [US3] Build the Plugin Pages payload in `src/Jellyfin.Plugin.NewReleases/Integration/PluginPagesRegistrationService.cs` from `PluginRoutes.UserViewAbsolute` instead of the raw literal, keeping the payload byte-identical, until T057 is green [U28]
- [X] T060 [US3] Record `UserViewController` in the exceptions table of [`contracts/http-surface.md`](./contracts/http-surface.md) — the one deliberate exception (`FR-012`, `SC-006`)
- [X] T061 [US3] Outer loop green before the story is complete: run the `A9` and `A10` mutants from `quickstart.md` pass 1 scenarios 2 and 3, confirm each fails the suite, restore from a file copy verified with `cmp -s`, and record both in `tdd/cycle-log.md`. **Never restore with `git checkout --`** [A9] [A10] [A11]

**Checkpoint**: every guard `spec.md` asks for is in the suite, and the two mutants prove it.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [ ] T062 [P] Create `docs/http-surface.md` from [`contracts/http-surface.md`](./contracts/http-surface.md) — the convention a future author reads before adding an endpoint, with the route table and the exceptions list (`FR-011`)
- [ ] T063 [P] Add a one-line link to `docs/http-surface.md` in the Conventions section of `CLAUDE.md`
- [X] T064 [P] Amend `specs/001-track-new-releases/contracts/http-api.md` to the renamed routes, leaving field names, status codes, query parameters and ownership rules untouched (`FR-015`)
- [X] T065 [P] Amend `specs/002-report-data-age/contracts/http-api.md` to the renamed routes, same constraint (`FR-015`)
- [ ] T066 Record in `.specify/memory/tdd-profile.md`: `tests/web/fake-dom.js` joins the node `helpers` list, `tests/fixtures/pages/` joins the fixture conventions, and the page-side note that "anything that reads or writes elements needs a simulated browser this project does not have" is amended to name what the stand-in now covers and what it still does not (`FR-018`)
- [ ] T067 [P] Add a `## Unreleased` entry to `CHANGELOG.md` covering the renamed HTTP surface and the stated response naming
- [ ] T068 Run `dotnet build --configuration Release` (zero warnings), `dotnet test --configuration Release`, `node --test "tests/web/*.test.js"` and `LANG=de_DE.UTF-8 node --test "tests/web/*.test.js"`; all four green
- [ ] T069 Run the remaining `quickstart.md` pass 1 mutants — scenarios 1, 4, 5 and 6 — each failing the suite and restored from a file copy verified with `cmp -s`
- [ ] T070 Push to `main` and verify the CI run green (`gh run watch`). Constitution: a feature is done when the CI run for that push is verified green, not when it is pushed
- [ ] T071 `quickstart.md` pass 2 — the real-server pass on a running Jellyfin 12, all ten steps, recorded in `docs/` as `003`'s was. **JD's own pass.** `SC-001`, `SC-002`, `SC-003` and `SC-005` are met here, not by the suite. Anything it finds becomes its own specification

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: no dependencies. The fixtures gate everything.
- **Foundational (Phase 2)**: depends on Phase 1. Blocks all three stories.
- **US1 (Phase 3)**: depends on Phase 2.
- **US2 (Phase 4)**: depends on Phase 2. Independent of US1 — different controller, different page, different fixtures.
- **US3 (Phase 5)**: depends on Phase 2. T047, T049, T053 pass only once T028/T029 and T042/T043 have landed, so US3 completes after US1 and US2 even though its tests are written before them.
- **Polish (Phase 6)**: depends on all three stories. T069 depends on T068; T070 on T069; T071 on T070.

### Within each story

- Test task red, for the right reason, recorded in `tdd/cycle-log.md`, before its implementation task.
- Characterization behaviours (`U29`–`U38`, `U41`–`U49`) go green against untouched code and terminate at `BASELINE`. Confirm each is green for the right reason and fails under the T061 mutant; do not promote them to `DONE`.
- Node tests need T013 and T014. C# naming tests need the Phase 1 fixtures.
- Exposure updates (T027, T041) land with the tests that justify the newly exposed function.

### Parallel Opportunities

- **Phase 1**: T001–T008 are eight separate files — all parallel. T009 and T010 follow.
- **Phase 2**: T011 is independent of T012–T014, which are sequential among themselves.
- **Phase 3 / Phase 4**: the two stories touch disjoint files and can run fully in parallel. The shared files are `tests/web/exposure.test.js` (T027, T041), `ResponseNamingTests.cs` (T015/T016 vs T033) and `HttpSurfaceTests.cs` (T025 vs T039) — serialize each pair.
- **Phase 5**: T046, T048, T050 and T054 are parallel. Everything else writes `HttpSurfaceTests.cs` — serialize.
- **Phase 6**: T062–T065 and T067 are separate files — parallel. T066, T068–T071 are sequential.

---

## Implementation Strategy

### MVP (User Story 1 only)

1. Phase 1 — fixtures.
2. Phase 2 — `PluginRoutes` and the fake DOM.
3. Phase 3 — the user view reads what the server sends.
4. **Stop and validate**: `quickstart.md` pass 2 steps 1–4 on a real server.

That alone turns the plugin from unusable to usable for the person it exists for.

### What "done" means here

A green suite is **not** sufficient evidence. A green suite is exactly what shipped this defect.
`spec.md` puts verification on a running Jellyfin 12 inside the feature, and T071 is that
verification.

---

## Notes

- `[P]` = different files, no dependency on an incomplete task.
- Commit after each task or logical group. Conventional Commits; no AI co-author trailer.
- Deliberate mutants are restored from a file copy and verified with `cmp -s`, never with `git checkout --`.
- `Jellyfin.Extensions` is transitive through `Jellyfin.Controller` 12.0.0. No `PackageReference` is added by any task above; if one appears to be needed, stop — that is a constitution VI decision, not an implementation detail.
