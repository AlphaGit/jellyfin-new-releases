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
- Page tests: `tests/web/*.test.js`, run by `node --test "tests/web/*.test.js"`
- No new folder, project or dependency. See `plan.md` "Structure Decision"

---

## Phase 1: Setup

- [X] T001 Correct the stale note in `.specify/memory/tdd-profile.md` that says "Constitution principle not applied: `.specify/memory/constitution.md` is still the template" — the constitution has been at version 1.2.0 with principle II written since 2026-09-06, and the note misleads the loop that runs next

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

- [X] T004 Add `tests/web/load-page.js`: read a page from `src/Web/`, run its `<script>` body in a `node:vm` sandbox with stubs for the browser globals it touches (`document`, `ApiClient`), and return the object of exposed helpers. Standard library only — no `package.json`, no install (research.md R9)
- [X] T005 Expose the pure helpers on one named global at the end of the script in `src/Web/user-view.html` (`esc`, `groupOf`, `artistLink`, and `stalenessText` once it exists) and in `src/Web/admin.html` (`esc`, `healthText`), then write a failing test in `tests/web/exposure.test.js` that the loader reaches both pages' objects Behaviors: [U34]
- [X] T006 Add a `node` ecosystem entry to `.specify/memory/tdd-profile.md`: `single` (`node --test --test-name-pattern "<name>" "tests/web/*.test.js"`), `suite` (`node --test "tests/web/*.test.js"`), `test_glob`, the exemplar and the `tests/web/load-page.js` helper, so the loop and the audit both find the page side
- [X] T007 Add a `node --test "tests/web/*.test.js"` step to `.github/workflows/build.yml` after `dotnet test`, with `actions/setup-node@v4` pinned to Node 22 (FR-015)

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

- [X] T015 [US1] Write failing tests in `tests/web/staleness.test.js` for the showing rules: no instant yields no sentence; an instant later than now counts as zero and yields none; exactly one refresh interval yields none; one second past it yields a sentence Behaviors: [U19] [U20] [U21] [U22]
- [X] T016 [US1] Write failing tests in `tests/web/staleness.test.js` for every band and boundary of the unit ladder — 47 h, 48 h, 13 d, 14 d, 60 d, 61 d, 364 d, 365 d — asserting both sides of each changeover Behaviors: [U23] [U24] [U25] [U26] [U27]
- [X] T017 [US1] Write a failing test in `tests/web/staleness.test.js` that the sentence reads `Releases last checked <relative time> ago.` and contains none of "refresh", "run", "scan" or "update" Behaviors: [U28]
- [X] T018 [US1] Implement `stalenessText(checkedAt, now, intervalHours)` in `src/Web/user-view.html` returning the sentence or nothing, and have the existing `staleness(data)` call it and touch the DOM, until T015–T017 are green Behaviors: [U19] [U20] [U21] [U22] [U23] [U24] [U25] [U26] [U27] [U28]
- [X] T019 [US1] Point the empty-state and staleness rendering in `src/Web/user-view.html` at the response fields as they stand after this phase, so the page and the API agree before the Phase 7 rename
- [X] T020 [US1] Story gate: the acceptance behaviours for US1 are green Behaviors: [A1] [A2] [A3] [A4] [A5]

**Checkpoint**: US1 is independently shippable.

---

## Phase 5: User Story 2 - A partly successful refresh reports honestly (Priority: P2)

**Goal**: with one source down and the other working, the age reflects the source that worked; with every source switched off, the age keeps growing.

**Independent Test**: put one source in cooldown, leave the other working, refresh, and confirm the stated instant is the completed fetch.

- [X] T021 [US2] Write failing tests: in `tests/Acceptance/ConfigureAndRunTests.cs`, one source cooling down while the other completes a fetch gives an instant within one refresh interval; in `tests/Api/ReleasesControllerTests.cs`, disabling the newest source falls back to the next enabled one and disabling every source reports none Behaviors: [U15] [A6] [A7]
- [X] T022 [US2] Make T021 green, changing `src/Api/ReleasesController.cs` or `src/Storage/ArtistRepository.cs` only if they demand it. Expect no production change beyond Phase 2 — if either test passes on the first run, apply the deliberate-mutant check from the TDD playbook and record it, because the behaviour is meant to fall out of `FR-002`'s enabled-source filter Behaviors: [U15] [A6] [A7]

**Checkpoint**: both behaviour stories complete and independently verifiable.

---

## Phase 6: User Story 3 - The page's own logic is covered by tests (Priority: P3)

**Goal**: the escaping and the helpers the pages already rely on are pinned, so the Phase 7 rename and any later change cannot break them silently.

**Independent Test**: run `node --test "tests/web/*.test.js"` with no network and nothing installed.

These are characterization behaviours: they capture what the code does today and are green against unchanged code. Per the loop playbook, verify each with a deliberate mutant rather than expecting a red.

- [X] T023 [P] [US3] Write characterization tests in `tests/web/esc.test.js`: a release title containing `<script>`, `&`, `"` and `'` comes back escaped; null and undefined become the empty string Behaviors: [U29] [U30] [A9]
- [X] T024 [P] [US3] Write characterization tests in `tests/web/page-helpers.test.js` for `groupOf` (Upcoming, undated, year slice), `artistLink` (link shape with `ApiClient` stubbed) and `healthText` (each health value the admin page can receive) Behaviors: [U31] [U32] [U33]
- [X] T025 [US3] Deliberate-mutant check for the ladder: move each boundary in `src/Web/user-view.html` by one unit, confirm a test in `tests/web/staleness.test.js` fails, restore exactly, re-run. Record each in `specs/002-report-data-age/tdd/cycle-log.md` — this is the second half of `A8`, which no assertion can express Behaviors: [A8]
- [X] T026 [US3] Run the whole suite with the network down and nothing installed — `dotnet test --configuration Release` then `node --test "tests/web/*.test.js"` — and record the result in `specs/002-report-data-age/tdd/cycle-log.md` Behaviors: [A10]

---

## Phase 7: Cross-cutting, renames, and the `001` amendments

### Administrator view

- [X] T027 Write failing tests in `tests/Api/AdminControllerTests.cs` that the admin status reports the last run including one that reached no source, reports the same instant the user page reports, and that after a run which completed no fetch the two differ Behaviors: [U16] [U17] [U18]
- [X] T028 Add the instant to `AdminStatusResponse` in `src/Api/Dtos.cs` and populate it in `src/Api/AdminController.cs` from the same repository call the user page uses, until T027 is green; leave `sources[].lastSuccessAt` and `lastRun` unchanged Behaviors: [U16] [U17] [U18]
- [X] T029 Show the data age beside the last run in `src/Web/admin.html`, using the wording and unit ladder from `contracts/staleness-line.md` Behaviors: [U36] [U37] [U38]

### The rename, on a green suite

Constitution II: refactoring happens only on a green suite and never in the same commit as a behaviour change.

- [X] T030 Rename `LastRefreshedAt` → `ReleasesLastCheckedAt` and `HasCompletedRefresh` → `HasStoredReleases` across `src/Api/Dtos.cs`, `src/Api/ReleasesController.cs`, `src/Api/AdminController.cs`, `src/Web/user-view.html`, `src/Web/admin.html` and every test that names them; run both suites before and after and confirm the same tests pass both times (research.md R2)

### Bring `001` into line

- [X] T031 [P] Restate `FR-015` in `specs/001-track-new-releases/spec.md` to the new datapoint and the new line, keeping its intent and its `SC-007` unchanged, with a pointer to `002`
- [X] T032 [P] Update `specs/001-track-new-releases/contracts/http-api.md` for the renamed fields in the list, status and admin responses, and the added admin field
- [X] T033 [P] Restate the `U118` and `A20` rows in `specs/001-track-new-releases/tdd/test-list.md` to the behaviour they assert after T008
- [X] T034 [P] Replace the canonical wording at `docs/domain_knowledge/CONTEXT.md` line 64 with `Releases last checked <relative time> ago.`

---

## Phase 8: Polish & Release Gate

- [X] T035 [P] Add a `CHANGELOG.md` entry under `Unreleased` describing the fix in user terms: the page now reports when the releases were last checked rather than when a refresh last ran
- [X] T036 Update `specs/002-report-data-age/quickstart.md`: the unit ladder is now covered by `tests/web/`, so remove it from the "does not cover" list and add a step running `node --test "tests/web/*.test.js"`
- [ ] T037 Run the manual checks in `specs/002-report-data-age/quickstart.md` steps 3–6 against Jellyfin 12.x with Plugin Pages installed (retargeted 2026-10-01: `003` dropped 10.11.x; JD's own pass), and record the outcome in that file's "Results" section; open a spec amendment for any deviation before touching code (constitution I)
- [X] T038 Final gate: `dotnet build --configuration Release` with zero warnings, `dotnet test --configuration Release` green, and `node --test "tests/web/*.test.js"` green; commit to `main` with Conventional Commits, push, and verify CI green with `gh run list --branch main` / `gh run watch`

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

---

## Phase 9: TDD remediation

From `specs/002-report-data-age/tdd/verification.md` (verdict **FAIL**, audited at `c558078`).

**The feature is not done until T039 and T040 are cleared.** Both are `HIGH`. Every task below is
test-first: the assertion changes and is observed failing before any production code moves.

- [X] T039 Finding 1 (HIGH): `tests/web/page-helpers.test.js:45-56` pins three of the four
  `checkedText` boundaries on one side only, and mutants at 14 d → 13 d, 61 d → 60 d and
  365 d → 364 d all survive. Split it into one test per rung, following the exemplar
  `tests/web/staleness.test.js:36-58`, and assert both sides of every changeover: 13 d and 14 d,
  60 d and 61 d, 364 d and 365 d, plus the existing 47 h / 48 h. Express the ages in days with
  named constants, not `24 * 61`. Proven done when each of the three mutants above makes
  `node --test "tests/web/*.test.js"` fail, and the code is restored exactly afterwards, with each
  result recorded in `specs/002-report-data-age/tdd/cycle-log.md`
- [X] T040 Finding 2 (HIGH): `tests/web/staleness.test.js:33` asserts only `notEqual(…, null)`,
  which an empty string would pass while hiding the line exactly as `null` does. Replace it with
  `assert.equal(…, 'Releases last checked 24 hours ago.')`. Proven done when a mutant returning
  `''` from `stalenessText` past the interval makes `node --test "tests/web/*.test.js"` fail
- [X] T041 Finding 1, second half: `checkedText` in `src/Web/admin.html` has no behaviour on
  `tdd/test-list.md`, so it never entered the per-behaviour evidence. Add its behaviours to the
  list (the ladder and the no-instant case), traced to `FR-009` and `contracts/staleness-line.md`,
  and add the behaviour ids to `T029` in brackets so `/speckit-tdd-run` can see them
- [X] T042 Finding 3 (MED): `node --test "tests/web/*.test.js"` fails 9 of 23 under
  `LANG=de_DE.UTF-8`, and the page suite is a CI gate. Pin the locale in the tests, or give
  `Intl.RelativeTimeFormat` an explicit locale argument the tests can set — do not change what a
  Jellyfin user sees, which is deliberately their own language. Proven done when
  `LANG=de_DE.UTF-8 node --test "tests/web/*.test.js"` is green
- [X] T043 [P] Finding 4 (MED): remove three assertions that cannot fail —
  `tests/Acceptance/ConfigureAndRunTests.cs:123` (`InRange` after line 122 pins the exact instant),
  `tests/web/staleness.test.js:66-68` (the job-word loop after line 65 asserts the whole string),
  and the `typeof … === 'function'` loops at `tests/web/exposure.test.js:14-16,23-25`. **Keep the
  exact-key-set assertions at `exposure.test.js:13,22`**: they are the only thing that file
  uniquely pins, and they caught `T029`'s new helper. Proven done when both suites stay green and
  the assertion count drops
- [X] T044 Finding 5 (MED): split
  `tests/Api/ReleasesControllerTests.cs::GetReleases_ReportsTheNewestCompletedFetch_RefreshIntervalFollowsTheTrigger`
  (lines 103-127), which asserts five behaviours under one name. Leave the refresh-interval rules
  in it, and move the reported-instant behaviour to its own test beside
  `GetReleases_ListAndStatusReportTheSameInstant`. Refactor on green; the assertions move
  unchanged. Update the `U10`/`U11` rows in `tdd/test-list.md` to the new test names
- [X] T045 [P] Finding 6 (LOW): `tests/Acceptance/ConfigureAndRunTests.cs:94` asserts `NotNull`
  where the value is known. Assert `SourceHarness.Start`, matching line 132 in the same file
- [X] T046 [P] Finding 7 (LOW): the doc comment at `tests/Api/AdminControllerTests.cs:128` claims
  the behaviour holds "on either view", but the body asserts the administrator view only. Name the
  admin view alone; the list side is covered at `ReleasesControllerTests.cs:147-160`
- [X] T047 [P] Finding 8 (LOW): `sandboxGlobals` in `tests/web/load-page.js:41` merges overrides at
  the top level, so a test overriding one `ApiClient` member must restate the rest (see
  `page-helpers.test.js:10-12`). Merge per global object, then simplify that call site
- [X] T048 Process, not code: `git checkout -- <file>` has now twice destroyed uncommitted work on
  this feature — cycle 3 of `tdd/cycle-log.md`, and mutant M1 of `tdd/verification.md`. Record in
  `.specify/extensions/tdd/templates/tdd-loop-playbook.md`, or in the project constitution, that a
  deliberate mutant is restored from a file copy verified with `cmp`, never with `git checkout`
- [X] T049 *(**upstream report dropped by decision.** The conflict is in the third-party `tdd` extension (`d0whc3r/spec-kit-tdd`), not in this plugin, and chasing it is not this project's work. The workaround stands and is recorded in `.specify/memory/tdd-profile.md`, which both the loop and the audit read at preflight.)* The loop command forbids ticking a task whose behaviour is `BASELINE`, but
  characterization behaviours terminate at `BASELINE` and can never reach `DONE`, so `T023`/`T024`
  could never be ticked under that rule. Report it upstream against the `tdd` extension; do not
  work around it by changing `U29`–`U33` to `DONE`
- [X] T050 `FR-015` (the build gate runs the page suite) has no test and is verified only by
  reading `.github/workflows/build.yml`. Either accept it explicitly in `spec.md` as verified by
  inspection, or drop it to an assumption. Decide, and record the decision

---

## Phase 10: TDD remediation (second audit)

From `specs/002-report-data-age/tdd/verification.md` (verdict **FAIL**, audited at `c558078`
plus the uncommitted tree). Phase 9 closed both of the first audit's `HIGH` findings; this phase
closes what the re-audit found.

**The feature is not done until T051 is cleared.** It is the only `HIGH`, and it is the same root
cause as Phase 9's Finding 1 — production logic in `admin.html` with no behaviour on the test list
to pin it. Every task below is test-first: the assertion is written and observed failing, by a red
or by a deliberate mutant, before any production code moves.

- [X] T051 Finding 1 (HIGH): `checkedText`'s clock-correction clamp at
  `src/Web/admin.html:129` (`Math.max(0, now - …)`) survives deletion — the page suite stays green
  at 32 passed. Without it the administrator page states `Releases last checked in 5 hours.`, which
  `FR-010` forbids. Unlike `user-view.html`, this view has no refresh-interval gate to hide a
  negative age. Add a behaviour to `tdd/test-list.md` for it, traced to `FR-010` and `FR-009`, add
  its id to `T029`'s brackets, and add a test in `tests/web/page-helpers.test.js` beside the
  `checkedText` ladder asserting `checkedText(<an instant later than now>, NOW)` is
  `'Releases last checked 0 hours ago.'`. Proven done when deleting the clamp makes
  `node --test "tests/web/*.test.js"` fail, and the file is restored from a copy verified with
  `cmp`, with the result recorded in `specs/002-report-data-age/tdd/cycle-log.md`
- [X] T052 Finding 2 (MED): `spec.md:92` (`US2-AS2`) and `spec.md:135` (Edge Cases) still say the
  stated age "keeps growing" when every source is disabled. `FR-002` and the built behaviour say
  the opposite, and `tests/Acceptance/ConfigureAndRunTests.cs:140` asserts
  `Assert.Null(list.ReleasesLastCheckedAt)`. The loop reported this at cycle 21 and correctly did
  not amend the spec itself. Amend both passages to the `FR-002` behaviour — no enabled source
  means no instant and no line — keeping the scenario's intent. Proven done when no passage in
  `spec.md` contradicts `A7`
- [X] T053 Finding 3 (MED): two characterization behaviours pin half of what they claim, and both
  mutants survive. (a) `U33`: `tests/web/page-helpers.test.js:36` matches
  `/^CoolingDown until .+ · …/`, so `when(s.cooldownUntil)` → `when(null)` passes. Pin the sandbox
  timezone in `tests/web/load-page.js` the way `Intl.RelativeTimeFormat` is already pinned, then
  assert the exact sentence. (b) `U32`: the test at `:20-24` is named "escaping both ids" but the
  sandbox serves `serverId: () => 'srv-42'`, which needs no escaping, so dropping
  `encodeURIComponent` around the server id passes. Use a server id that needs escaping. Proven
  done when each of those two mutants makes `node --test "tests/web/*.test.js"` fail, restored
  from a copy verified with `cmp`
- [X] T054 Finding 4 (MED): `node --test tests/web/` (the bare directory) resolves the path as a module, runs no test
  and **exits 0**. Cycle 14 corrected `.specify/memory/tdd-profile.md` and
  `.github/workflows/build.yml` to the glob form but not this feature's own files. Replace all 7
  occurrences in `tasks.md` — `Path Conventions`, `T006`, `T007`, Phase 6's Independent Test,
  `T026`, `T036`, `T038` — and the one in `tdd/test-list.md`'s "Verification commands" with
  `node --test "tests/web/*.test.js"`. In the same pass fix that file's "`T033` adds them", which
  should read `T006`. Proven done when every command printed in these two files runs the 32 tests
- [X] T055 [P] Finding 5 (MED): `tests/Api/ReleasesControllerTests.cs:128-129` and `:134-135` both
  assert `RefreshIntervalHours == 24`, and `src/Api/ReleasesController.cs:169-174` has no
  `DailyTrigger` arm — daily falls to `_ => 24`, the same arm as no task at all. Either drop the
  daily case and rename the test to say the interval falls back to 24 unless an `IntervalTrigger`
  gives one, or add the missing arm if a daily trigger is meant to mean something other than 24
  (a spec amendment first, per constitution I). Proven done when no two cases of that test reach
  the same arm
- [X] T056 [P] Finding 6 (LOW): `tests/web/exposure.test.js:17,23` pins the exact key set of
  `NewReleasesInternals`, a rule no requirement states, so a refactor exposing one more pure
  helper fails it. It was kept deliberately in Phase 9 and it did catch `T029`'s new helper.
  Decide and record: keep it with a comment naming the rule it enforces, or relax it to
  "contains at least". Do not simply delete the file
- [X] T057 [P] Finding 7 (LOW): `HOUR`, `DAY`, `NOW` and `ago` are copied between
  `tests/web/staleness.test.js:9-15` and `tests/web/page-helpers.test.js:43-48`. Move them to one
  shared place. The two ladder tables stay separate — they pin two implementations in two pages
- [X] T058 [P] Finding 8 (LOW): `tests/web/page-helpers.test.js:7-8` calls the file
  characterization of "helpers this feature does not change", but `:50-79` are new-behaviour tests
  for `checkedText`, which this feature added. State both purposes in the header, or move
  `checkedText` to its own file beside `staleness.test.js` as `esc.test.js` does

---

## Phase 11: TDD remediation (fourth audit)

From `specs/002-report-data-age/tdd/verification.md` (verdict **FAIL**, audited at `99c805e`).
Phase 10 closed every finding of the third audit; this phase closes what the fourth found.

**The feature is not done until T059–T064 are cleared.** Each is a real surviving mutant inside a
`DONE` behaviour. Every task is a test change on correct production code, so each is proven by its
mutant failing, applied from a file copy and restored with `cmp`, never `git checkout`. Record each
in `tdd/cycle-log.md`. Even with all of them closed, the verdict cannot reach `PASS`: T065 records
six `TEST_AFTER` behaviours that no new work can turn into test-first ones.

- [X] T059 Finding 1 (HIGH): add a test to `tests/Jellyfin.Plugin.NewReleases.Tests/Api/ReleasesControllerTests.cs` that stores a release, then calls `GetReleasesAsync` with a filter (or a caller) that hides it, and asserts `HasStoredReleases` is `true` with no items. Every current test of the flag (`:156-169`) uses a caller who sees every row, so `src/Jellyfin.Plugin.NewReleases/Api/ReleasesController.cs:95` passing `visible.Count > 0` instead of `hasStored` passes all 303 tests. Proven done when that mutant fails `dotnet test --configuration Release` [U13]
- [X] T060 Finding 2 (HIGH): add both sides of the boundary at a second refresh interval (for example 12 h) to `tests/web/staleness.test.js`. Every test uses `INTERVAL_HOURS = 24`, so replacing `intervalHours` with `24` at `src/Jellyfin.Plugin.NewReleases/Web/user-view.html:106` passes. Proven done when that mutant fails `node --test "tests/web/*.test.js"` [U21] [U22]
- [X] T061 Finding 3 (HIGH): add a case to `tests/web/staleness.test.js` with an instant further in the future than one refresh interval (for example `ahead(30 * HOUR)`), expecting `null`. The case at `:22-24` stays inside the interval, so `Math.max(0, …)` → `Math.abs(…)` at `user-view.html:105` passes and states "checked 30 hours ago" for a future instant. Proven done when that mutant fails `node --test "tests/web/*.test.js"` [U20]
- [X] T062 Finding 4 (HIGH): extend `tests/Jellyfin.Plugin.NewReleases.Tests/Api/AdminControllerTests.cs` so the administrator view and the list report the same instant after the newest source is disabled. Today every `FR-011` test has every source enabled, so `src/Jellyfin.Plugin.NewReleases/Api/AdminController.cs:97` passing a fixed set of both sources instead of `configuration.EnabledSourceIds()` passes all 303 tests. Proven done when that mutant fails `dotnet test --configuration Release` [U17]
- [X] T063 Finding 5 (HIGH): extend `GetReleases_ListAndStatusReportTheSameInstant` (or add a sibling) in `ReleasesControllerTests.cs` so list and status are compared after a purge. Today `GetStatusAsync` at `ReleasesController.cs:162` can drop the stored-releases gate and all 303 tests pass. Proven done when that mutant fails `dotnet test --configuration Release` [U12]
- [X] T064 Finding 6 (HIGH): add a render test (in `tests/web/render.test.js`, through `loadPageDom`) that renders a release whose title contains markup, and asserts that the panel's HTML carries it escaped. Only `esc` itself is tested, so removing `esc()` around `item.title` at `user-view.html:143` passes all 64 node tests. Proven done when that mutant fails `node --test "tests/web/*.test.js"` [A9]
- [X] T065 Finding 7 (MED): append a cycle-log entry labelling `U12`, `U13`, `U36`, `U38`, `A6` and `A7` as test-after, with the evidence the audit cites, and record the maintainer's decision to accept them or not. Do not edit past entries. This cannot be fixed by new tests. Proven done when the entry exists and names all six
- [X] T066 [P] Findings 8–10 (MED): separate the coincident inputs. In `ReleasesControllerTests.cs:108-117`, advance the clock between the fetch and `StartRunAsync`. In `ConfigureAndRunTests.cs:107-123` (`A6`), give MusicBrainz an earlier completed fetch and advance the clock before the run. In `ArtistRepositoryTests.cs:138-149` (`U1`), write the newer fetch first and at the second source. Proven done when "the latest run's start" (`U10`), "the run's end" (`A6`) and "the last row written" (`U1`) each fail `dotnet test --configuration Release` as mutants [U1] [U10] [U11] [A6]
- [X] T067 [P] Finding 11 (MED): in `tdd/test-list.md` "Verification commands", replace the node `--test-name-pattern` single-test line with the profile's `file:` command (`node --test tests/web/{file}`), and the `dotnet@9` path with the profile's shell note. Proven done when every command printed there fails on a test that does not exist, or runs one file
- [X] T068 [P] Finding 12 (MED): in `tests/web/load-page.js:104-108`, rethrow every error from the page script now that the fake DOM lets initialization complete, or narrow the catch to a named, expected case. Proven done when a `throw` added at the end of `user-view.html`'s initialization fails `node --test "tests/web/*.test.js"`
- [X] T069 [P] Finding 13 (LOW): fix the text drift. `A7`'s row ("stops moving" → no instant), the summary at `ConfigureAndRunTests.cs:143` ("last refreshed"), the `exposure.test.js` titles and comment (now `render`/`renderStatus` too), the name of `AdminControllerTests.cs:131`, and `U34`'s `FR-016` trace (packaging is pinned by `003`'s `BuildManifestTests`)

---

## Phase 12: TDD remediation (fifth audit)

From `specs/002-report-data-age/tdd/verification.md` (verdict **FAIL**, audited at `43fb6b6`).
Phase 11 closed every mutant the fourth audit found; this phase closes what the fifth found.

**The feature is not done until T070–T073 are cleared.** Each is a real surviving mutant inside a
`DONE` behaviour, and `T070` repairs a test that Phase 11 weakened. Every task is a test change on
correct production code, so each is proven by its mutant failing, applied from a file copy and
restored with `cmp`, never `git checkout`. Record each in `tdd/cycle-log.md`. Even with all of them
closed, the verdict stays `FAIL` under the current rubric while six `TEST_AFTER` behaviours stand:
see T075.

- [X] T070 Finding 1 (HIGH): in `tests/Jellyfin.Plugin.NewReleases.Tests/Storage/ArtistRepositoryTests.cs:145-149`, add a case where the newer fetch is at `musicbrainz` and written last, with the older one at `deezer`, so neither write order nor source order picks it. `T066` put the newer value at `deezer` (which sorts first), so `ORDER BY source LIMIT 1` in place of `MAX` at `src/Jellyfin.Plugin.NewReleases/Storage/ArtistRepository.cs:215` passes all 307 tests; the `99c805e` copy of the test caught it. Proven done when that mutant and "last row written" (`ORDER BY rowid DESC LIMIT 1`) both fail `dotnet test --configuration Release` [U1]
- [X] T071 Finding 2 (HIGH): in `GetReleases_StoredReleasesFlagHoldsWhenTheSelectionHidesEveryRow` (`tests/Jellyfin.Plugin.NewReleases.Tests/Api/ReleasesControllerTests.cs:190-197`), seed a completed fetch and assert the reported instant together with `(0, true)`. Today no fetch is seeded, so gating the age on `visible.Count > 0` at `src/Jellyfin.Plugin.NewReleases/Api/ReleasesController.cs:94` passes all 307 tests. Proven done when that mutant fails `dotnet test --configuration Release` [A5] [U13]
- [X] T072 Finding 3 (HIGH), after T074: add a render test to `tests/web/render.test.js` with a response whose `refreshIntervalHours` is not 24 (for example 168), at an age between 24 h and that interval, at a fixed clock, asserting no staleness sentence; and its mirror with a shorter interval asserting the exact sentence. Every page fixture serves 24, so `staleness(data)` passing `24` at `src/Jellyfin.Plugin.NewReleases/Web/user-view.html:132` passes all 75 node tests. Proven done when that mutant fails `node --test "tests/web/*.test.js"` [A2] [A3]
- [X] T073 Finding 4 (HIGH): add a test to `tests/web/render-status.test.js` with a status response whose `releasesLastCheckedAt` differs from `lastRun.endedAt`, asserting the exact `nr-last-checked` sentence for `releasesLastCheckedAt` at a fixed clock. In `tests/fixtures/pages/admin-status.json:27,34` the two instants are equal and `:33` matches only a pattern, so writing `lastRun.endedAt` there at `src/Jellyfin.Plugin.NewReleases/Web/admin.html:153` passes all 75 node tests. Proven done when that mutant fails `node --test "tests/web/*.test.js"` [U16] [U18] [U36]
- [X] T074 Finding 6 (MED): make the user page's render tests use the fixed clock from `tests/web/fixed-clock.js` (through `loadPageDom` overrides, or `load-page.js:70`'s `Date`), and assert the exact sentence at `tests/web/render.test.js:112-117` instead of `/^Releases last checked .+ ago\.$/`. Proven done when the node suite passes with the machine clock set before `2026-08-02` (for example via a `Date.now` override in a scratch run) and the exact sentence is asserted
- [X] T075 Finding 5 (MED): maintainer decision. Either add `.specify/templates/overrides/tdd-test-quality-rubric.md` stating how accepted `TEST_AFTER` behaviours are graded, or correct the "Test-after admissions" consequence in `tdd/cycle-log.md` with a new entry saying the verdict stays `FAIL` under the current rubric. Do not edit the past entry. Proven done when one of the two exists
- [X] T076 [P] Finding 7 (LOW): add a title carrying `"` and `'` as well as markup to the `tests/web/render.test.js:143-156` markup tests, asserting no raw quote from it appears in `data-title` or `aria-label`. Proven done when a call site using an escaper that skips quotes fails `node --test "tests/web/*.test.js"`
- [X] T077 [P] Finding 8 (LOW): in `GetReleases_AfterAPurge_ListAndStatusBothReportNoInstant` (`ReleasesControllerTests.cs:157-169`), assert before acting that `GetReleasesLastCheckedAtAsync` still returns an instant after the purge, as `AdminControllerTests.cs:138` does. Proven done when the test fails if `PurgeAsync` is changed to clear `last_complete_at`
- [X] T078 [P] Finding 9 (LOW): fix the text drift. The comment at `ArtistRepositoryTests.cs:145` (after T070); `staleness.test.js:46-47` (the gate also hides a negative age; `Math.abs` is what the test catches); `load-page.js:12` (synchronous errors only); the name of `ReleasesControllerTests.cs:103` and the duplicated empty-state call at `:106`; and `tdd/test-list.md`'s "Out of scope" `row`/`render` line and its `updated_at`
