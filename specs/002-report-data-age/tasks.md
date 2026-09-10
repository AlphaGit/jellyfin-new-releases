---
description: "Task list for 002-report-data-age"
---

# Tasks: Report the age of the data, not the age of the run

**Input**: Design documents from `/specs/002-report-data-age/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md, tdd/test-list.md — all present

**Tests**: Mandatory, not optional. Constitution II is non-negotiable: a test exists and has been
observed failing, for the right reason, before the code that makes it pass. Every behavioural task
carries its behaviour ids from `tdd/test-list.md` in brackets; `/speckit-tdd-run` ticks a task only
when it can read those ids, and `/speckit-implement` writes anything still unticked.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: US1 (trust the age shown), US2 (a partly successful refresh reports honestly), US3 (the page's own logic is covered by tests)
- **Behaviors: [A#] [U#]**: the `tdd/test-list.md` ids the task covers
- Paths are relative to the repository root. `src/` = `src/Jellyfin.Plugin.NewReleases/`,
  `tests/` = `tests/Jellyfin.Plugin.NewReleases.Tests/` unless a full path is given.

## Path Conventions

- Plugin: `src/Jellyfin.Plugin.NewReleases/{Api,Storage,Web}/`
- Server tests mirror the source folders: `tests/Jellyfin.Plugin.NewReleases.Tests/<Folder>/<Class>Tests.cs`
- Page tests: `tests/web/*.test.js`, run by `node --test tests/web/`
- No new folder, project or dependency. See `plan.md` "Structure Decision"

---

## Phase 1: Setup

- [ ] T001 Correct the stale note in `.specify/memory/tdd-profile.md` that says "Constitution principle not applied: `.specify/memory/constitution.md` is still the template" — the constitution has been at version 1.2.0 with principle II written since 2026-09-06, and the note misleads the loop that runs next

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: the one read every story depends on. Nothing after this can start until it is green.

- [X] T002 Write failing tests in `tests/Storage/ArtistRepositoryTests.cs` for `GetReleasesLastCheckedAtAsync`: newest value across artists; a completed fetch at a source outside the enabled set ignored even when newest; empty enabled set returns nothing; no pair ever completed returns nothing; a `Partial` and a `Failed` outcome each leave the value where the last `Complete` left it Behaviors: [U1] [U2] [U3] [U4] [U5] [U6]
- [X] T003 Implement `ArtistRepository.GetReleasesLastCheckedAtAsync(ISet<string> enabledSources, CancellationToken)` in `src/Storage/ArtistRepository.cs` as `SELECT MAX(last_complete_at) FROM artist_source WHERE source IN (…)` until T002 is green — per `data-model.md` "Read-model additions" Behaviors: [U1] [U2] [U3] [U4] [U5] [U6]

---

## Phase 3: Page-side test harness (Blocking Prerequisites)

**Purpose**: the page owns `FR-006`, `FR-007`, `FR-010` and `FR-012`. Without a runner they can
only be written test-after, which constitution II forbids. This phase exists **before** the page
behaviours for that reason, and it is why `/speckit-tdd-plan` reordered the task file.

- [ ] T004 Add `tests/web/load-page.js`: read a page from `src/Web/`, run its `<script>` body in a `node:vm` sandbox with stubs for the browser globals it touches (`document`, `ApiClient`), and return the object of exposed helpers. Standard library only — no `package.json`, no install (research.md R9)
- [ ] T005 Expose the pure helpers on one named global at the end of the script in `src/Web/user-view.html` (`esc`, `groupOf`, `artistLink`, and `stalenessText` once it exists) and in `src/Web/admin.html` (`esc`, `healthText`), then write a failing test in `tests/web/exposure.test.js` that the loader reaches both pages' objects Behaviors: [U34]
- [ ] T006 Add a `node` ecosystem entry to `.specify/memory/tdd-profile.md`: `single` (`node --test --test-name-pattern "<name>" tests/web/`), `suite` (`node --test tests/web/`), `test_glob`, the exemplar and the `tests/web/load-page.js` helper, so the loop and the audit both find the page side
- [ ] T007 Add a `node --test tests/web/` step to `.github/workflows/build.yml` after `dotnet test`, with `actions/setup-node@v4` pinned to Node 22 (FR-015)

---

## Phase 4: User Story 1 - Trust the age shown on the New Releases page (Priority: P1) 🎯 MVP

**Goal**: the page states when the releases were last checked, and that statement never gets younger unless the plugin actually learned something.

**Independent Test**: complete one refresh that finishes a catalogue fetch, make every source unreachable, refresh again, and confirm the stated instant is still the first refresh's.

### Rewrite the `001` tests that assert the old rule

Both assert the *opposite* of what `002` requires, so both go red before the wiring changes. Neither is deleted or weakened (constitution II).

- [X] T008 [US1] Rewrite `GetReleases_LastRefreshedAtIsTheLastCompletedRunsEnd_RefreshIntervalFollowsTheTrigger` in `tests/Api/ReleasesControllerTests.cs` (`001`'s `U118`) to assert the reported instant is the newest completed catalogue fetch, renaming the method to match; and invert `A20_WithEverySourceInCooldown_TheListStillShowsTheStoredDataAndItsAge` in `tests/Acceptance/ConfigureAndRunTests.cs` (`001`'s `A20`) to assert the **first** run's fetch after a second run that completed none. Record both reds Behaviors: [U10] [U11]
- [X] T009 [US1] Read the aggregate in `src/Api/ReleasesController.cs` and return it from both the list and status actions, replacing the last-completed-run lookup, until T008 is green Behaviors: [U10] [U11]
- [X] T010 [US1] Write a failing test in `tests/Api/ReleasesControllerTests.cs` that the list and status responses report the same instant for one caller at one moment, then make it green Behaviors: [U12]

### The empty state

- [X] T011 [P] [US1] Write failing tests in `tests/Storage/ReleaseRepositoryTests.cs` for `HasAnyAsync`: false on an empty database, true with one release row, false again after `PurgeAsync` Behaviors: [U7] [U8] [U9]
- [X] T012 [US1] Implement `ReleaseRepository.HasAnyAsync(CancellationToken)` in `src/Storage/ReleaseRepository.cs` as `SELECT EXISTS(SELECT 1 FROM release LIMIT 1)` until T011 is green Behaviors: [U7] [U8] [U9]
- [X] T013 [US1] Write failing tests in `tests/Api/ReleasesControllerTests.cs` that the stored-releases flag follows whether release rows exist rather than run history, and that release rows with no completed fetch anywhere are listed with no instant reported Behaviors: [U13] [U14]
- [X] T014 [US1] Gate the stored-releases flag in `src/Api/ReleasesController.cs` on `HasAnyAsync` in both the list and status actions until T013 is green Behaviors: [U13] [U14]

### The page

Test-first, on the harness from Phase 3.

- [ ] T015 [US1] Write failing tests in `tests/web/staleness.test.js` for the showing rules: no instant yields no sentence; an instant later than now counts as zero and yields none; exactly one refresh interval yields none; one second past it yields a sentence Behaviors: [U19] [U20] [U21] [U22]
- [ ] T016 [US1] Write failing tests in `tests/web/staleness.test.js` for every band and boundary of the unit ladder — 47 h, 48 h, 13 d, 14 d, 60 d, 61 d, 364 d, 365 d — asserting both sides of each changeover Behaviors: [U23] [U24] [U25] [U26] [U27]
- [ ] T017 [US1] Write a failing test in `tests/web/staleness.test.js` that the sentence reads `Releases last checked <relative time> ago.` and contains none of "refresh", "run", "scan" or "update" Behaviors: [U28]
- [ ] T018 [US1] Implement `stalenessText(checkedAt, now, intervalHours)` in `src/Web/user-view.html` returning the sentence or nothing, and have the existing `staleness(data)` call it and touch the DOM, until T015–T017 are green Behaviors: [U19] [U20] [U21] [U22] [U23] [U24] [U25] [U26] [U27] [U28]
- [ ] T019 [US1] Point the empty-state and staleness rendering in `src/Web/user-view.html` at the response fields as they stand after this phase, so the page and the API agree before the Phase 7 rename
- [ ] T020 [US1] Story gate: the acceptance behaviours for US1 are green Behaviors: [A1] [A2] [A3] [A4] [A5]

**Checkpoint**: US1 is independently shippable.

---

## Phase 5: User Story 2 - A partly successful refresh reports honestly (Priority: P2)

**Goal**: with one source down and the other working, the age reflects the source that worked; with every source switched off, the age keeps growing.

**Independent Test**: put one source in cooldown, leave the other working, refresh, and confirm the stated instant is the completed fetch.

- [ ] T021 [US2] Write failing tests: in `tests/Acceptance/ConfigureAndRunTests.cs`, one source cooling down while the other completes a fetch gives an instant within one refresh interval; in `tests/Api/ReleasesControllerTests.cs`, disabling the newest source falls back to the next enabled one and disabling every source reports none Behaviors: [U15] [A6] [A7]
- [ ] T022 [US2] Make T021 green, changing `src/Api/ReleasesController.cs` or `src/Storage/ArtistRepository.cs` only if they demand it. Expect no production change beyond Phase 2 — if either test passes on the first run, apply the deliberate-mutant check from the TDD playbook and record it, because the behaviour is meant to fall out of `FR-002`'s enabled-source filter Behaviors: [U15] [A6] [A7]

**Checkpoint**: both behaviour stories complete and independently verifiable.

---

## Phase 6: User Story 3 - The page's own logic is covered by tests (Priority: P3)

**Goal**: the escaping and the helpers the pages already rely on are pinned, so the Phase 7 rename and any later change cannot break them silently.

**Independent Test**: run `node --test tests/web/` with no network and nothing installed.

These are characterization behaviours: they capture what the code does today and are green against unchanged code. Per the loop playbook, verify each with a deliberate mutant rather than expecting a red.

- [ ] T023 [P] [US3] Write characterization tests in `tests/web/esc.test.js`: a release title containing `<script>`, `&`, `"` and `'` comes back escaped; null and undefined become the empty string Behaviors: [U29] [U30] [A9]
- [ ] T024 [P] [US3] Write characterization tests in `tests/web/page-helpers.test.js` for `groupOf` (Upcoming, undated, year slice), `artistLink` (link shape with `ApiClient` stubbed) and `healthText` (each health value the admin page can receive) Behaviors: [U31] [U32] [U33]
- [ ] T025 [US3] Deliberate-mutant check for the ladder: move each boundary in `src/Web/user-view.html` by one unit, confirm a test in `tests/web/staleness.test.js` fails, restore exactly, re-run. Record each in `specs/002-report-data-age/tdd/cycle-log.md` — this is the second half of `A8`, which no assertion can express Behaviors: [A8]
- [ ] T026 [US3] Run the whole suite with the network down and nothing installed — `dotnet test --configuration Release` then `node --test tests/web/` — and record the result in `specs/002-report-data-age/tdd/cycle-log.md` Behaviors: [A10]

---

## Phase 7: Cross-cutting, renames, and the `001` amendments

### Administrator view

- [ ] T027 Write failing tests in `tests/Api/AdminControllerTests.cs` that the admin status reports the last run including one that reached no source, reports the same instant the user page reports, and that after a run which completed no fetch the two differ Behaviors: [U16] [U17] [U18]
- [ ] T028 Add the instant to `AdminStatusResponse` in `src/Api/Dtos.cs` and populate it in `src/Api/AdminController.cs` from the same repository call the user page uses, until T027 is green; leave `sources[].lastSuccessAt` and `lastRun` unchanged Behaviors: [U16] [U17] [U18]
- [ ] T029 Show the data age beside the last run in `src/Web/admin.html`, using the wording and unit ladder from `contracts/staleness-line.md`

### The rename, on a green suite

Constitution II: refactoring happens only on a green suite and never in the same commit as a behaviour change.

- [ ] T030 Rename `LastRefreshedAt` → `ReleasesLastCheckedAt` and `HasCompletedRefresh` → `HasStoredReleases` across `src/Api/Dtos.cs`, `src/Api/ReleasesController.cs`, `src/Api/AdminController.cs`, `src/Web/user-view.html`, `src/Web/admin.html` and every test that names them; run both suites before and after and confirm the same tests pass both times (research.md R2)

### Bring `001` into line

- [ ] T031 [P] Restate `FR-015` in `specs/001-track-new-releases/spec.md` to the new datapoint and the new line, keeping its intent and its `SC-007` unchanged, with a pointer to `002`
- [ ] T032 [P] Update `specs/001-track-new-releases/contracts/http-api.md` for the renamed fields in the list, status and admin responses, and the added admin field
- [ ] T033 [P] Restate the `U118` and `A20` rows in `specs/001-track-new-releases/tdd/test-list.md` to the behaviour they assert after T008
- [ ] T034 [P] Replace the canonical wording at `docs/domain_knowledge/CONTEXT.md` line 64 with `Releases last checked <relative time> ago.`

---

## Phase 8: Polish & Release Gate

- [ ] T035 [P] Add a `CHANGELOG.md` entry under `Unreleased` describing the fix in user terms: the page now reports when the releases were last checked rather than when a refresh last ran
- [ ] T036 Update `specs/002-report-data-age/quickstart.md`: the unit ladder is now covered by `tests/web/`, so remove it from the "does not cover" list and add a step running `node --test tests/web/`
- [ ] T037 Run the manual checks in `specs/002-report-data-age/quickstart.md` steps 3–6 against Jellyfin 10.11.11 with Plugin Pages installed, and record the outcome in that file's "Results" section; open a spec amendment for any deviation before touching code (constitution I)
- [ ] T038 Final gate: `dotnet build --configuration Release` with zero warnings, `dotnet test --configuration Release` green, and `node --test tests/web/` green; commit to `main` with Conventional Commits, push, and verify CI green with `gh run list --branch main` / `gh run watch`

---

## Dependencies & Execution Order

### Phase dependencies

- **Phase 1** (T001) is independent and can run at any time
- **Phase 2** (T002–T003) blocks Phases 4, 5, 6 and 7 — every story reads the aggregate
- **Phase 3** (T004–T007) blocks every page behaviour: T015–T019 in US1, all of US3, and T029. It does not depend on Phase 2, so the two can run in parallel
- **Phase 4** (US1) depends on Phases 2 and 3
- **Phase 5** (US2) depends on Phase 2 only, not on US1
- **Phase 6** (US3) depends on Phase 3; T025 additionally depends on T018
- **Phase 7**: T027–T029 depend on Phase 2 and, for T029, Phase 3. T030 depends on Phases 4, 5 and 6 being green. T031–T034 depend on the behaviour they describe being settled
- **Phase 8** depends on everything

### Within phases

T002 before T003. T008 before T009. T011 before T012. T013 before T014. T015–T017 before T018.
T027 before T028. T004 and T005 before any `tests/web/` test.

### Story independence

US1 delivers the whole user-visible fix and is the MVP. US2 adds no production code beyond Phase 2
in the expected case. US3 delivers no user-visible behaviour, but its harness (Phase 3) is a
prerequisite for US1's page tasks, so it cannot be dropped without dropping the page tests with it.

---

## Parallel Example

Phase 7's `001` amendments touch four different files with no shared state:

```text
T031  specs/001-track-new-releases/spec.md
T032  specs/001-track-new-releases/contracts/http-api.md
T033  specs/001-track-new-releases/tdd/test-list.md
T034  docs/domain_knowledge/CONTEXT.md
```

T011 is `[P]` because it touches `ReleaseRepositoryTests.cs` while T008–T010 are in
`ReleasesControllerTests.cs` and `ConfigureAndRunTests.cs`. T023 and T024 are `[P]` against each
other: two different files under `tests/web/`.

Phases 2 and 3 have no dependency between them and can run at the same time.

---

## Implementation Strategy

**MVP**: Phases 1–4. The whole reported defect: the page stops claiming the data is fresher than
it is, and the empty state follows the data. Shippable alone.

**Increment 2**: Phase 5, pinning the partial-refresh behaviour.

**Increment 3**: Phase 6, pinning the escaping and helpers before anything renames them.

**Increment 4**: Phase 7. The administrator view earns its place by letting an operator see the run
and the data age diverge. The rename and the `001` amendments close the loop.

---

## Notes

- 38 tasks: Setup 1, Foundational 2, Harness 4, US1 13, US2 2, US3 4, Cross-cutting 8, Polish 4
- 44 behaviours on `tdd/test-list.md`: 10 acceptance, 34 unit. Every behavioural task carries its
  ids in brackets
- **What US3 does and does not cover.** It reaches the five functions computable without a page:
  `stalenessText`, `esc`, `groupOf`, `healthText`, `artistLink`. It does **not** reach `row`,
  `render`, `refreshStatus`, `read`, `fill` or `query`, all DOM-coupled. `001`'s `FR-019` and
  `SC-008` — keyboard operation and screen-reader announcement — are **not** unblocked: they need
  a real browser and assistive technology
- Zero new dependencies in either language. `node:test`, `node:assert` and `node:vm` are standard
  library, so there is no `package.json` and no install step
- `A20` was written hours before this feature by `001`'s own TDD audit and pins exactly the
  behaviour being replaced. T008 inverts it deliberately; the cycle log must say so, or the next
  audit will read it as a weakened test
- Commit after each green cycle or logical group; never commit a red suite
