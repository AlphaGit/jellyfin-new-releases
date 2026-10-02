---
feature: 002-report-data-age
verdict: FAIL
standard: .specify/extensions/tdd/templates/tdd-test-quality-rubric.md
verified_at: 99c805e
behaviors: 48
proven: 19
likely: 14
test_after: 6
no_test: 0
not_applicable: 9
high_smells: 6 # each a real surviving mutant inside a DONE behaviour
criteria_total: 25 # 16 FR, 9 SC
criteria_covered: 24 # FR-015 verified by inspection, by recorded decision
mutation_score: null # no tool (profile `mutation: null`); 25 deliberate mutants, 18 caught
mutants_survived: 7 # 1 equivalent, 6 real
suite: 303 passed, 0 failed, 16 s incl. build (dotnet); 64 passed, 0 failed, 0.4 s (node)
suite_non_english_locale: 64 passed, 0 failed (node, LANG=de_DE.UTF-8, TZ=Asia/Tokyo)
independent: partly # this session wrote none of 002; smell pass delegated to a fresh-context subagent
audits: 4
---

# TDD Verification: Report the age of the data, not the age of the run

**Verdict: FAIL.** Six one-line mutants inside `DONE` behaviours pass the whole suite (303 + 64).
The worst one is `U13`. Replace the stored-releases flag with `visible.Count > 0` and nothing
fails. A viewer whose filter or library access hides every stored release would then see "No data
yet. New Releases is waiting for its first refresh." `FR-008` forbids this.

This is the fourth audit and the first since Phase 10. Phase 10 closed every finding of the third
audit, and this run confirms each of them independently (mutants P1, P3, P4 below). This run finds
new defects. None of them is a regression; earlier audits did not try these mutants. Two
patterns cause most of them:

- **Inputs that coincide.** Every staleness test uses a 24 h interval. Every same-instant test
  has releases stored and every source enabled. Every future-instant test stays inside the
  interval. So a mutant that ignores the varying input cannot be seen.
- **Helpers tested, call sites not.** `esc` is pinned, but its use in `row()` is not.
  `GetReleasesLastCheckedAtAsync` is pinned, but the administrator view's argument to it is not.

Six behaviours are also `TEST_AFTER`, so `PASS` is out of reach under the rubric even after the
mutants are fixed. That history cannot be rewritten. See Finding 7.

**Independence.** This session wrote no code, test or log entry of `002`. Earlier sessions wrote
them, and those sessions ran the first three audits. A fresh-context subagent did the smell pass.
Each `HIGH` claim it returned was run here as a real mutant before inclusion. One claim was
refuted: it said dropping the administrator view's stored-releases gate survives, but mutant S8
shows that `U35` catches it.

## Test-first evidence

The rubric has no class for "passed on first run, deliberate mutant recorded". This audit applies
one rule to every behaviour: a behaviour is `TEST_AFTER` when this feature wrote the production
code that satisfies it before any valid red of its test. A failure that only shows a missing
symbol is not a valid red; the loop itself rejected one at cycle 15. A behaviour satisfied by code
that already existed, or by the smallest change another behaviour's red demanded, is `LIKELY`. Its
test strength rests on its recorded mutant, not on ordering.

| Behaviour | Class | Evidence |
| --- | --- | --- |
| A1 | PROVEN | outer-loop red (cycle log, `A20` inversion) in `0a330af`; the source that closes it lands in `81d3e63` |
| A2 | PROVEN | cycle 15 red, `U22`'s case: `Expected "actual" to be strictly unequal to: null` |
| A3 | LIKELY | the same case as `U21`. It passed against the `null` stub, constrains the cycle 15 implementation, and a mutant proves it (P9) |
| A4 | NOT_APPLICABLE | `001`'s test, inherited; green before and after. A regression guard, not driven by this feature |
| A5 | PROVEN | cycle 19, `Assert.Null() Failure … Actual: 2026-09-06T12:00:00Z`; test and fix together in `df7f7b5` |
| A6, A7 | **TEST_AFTER** | written at cycle 21, after every unit and every line of the code they cover; no red. Cycle 21 calls this "the outer loop closing". The playbook's outer loop is an acceptance test written **first** that stays red. The units beneath are test-driven, so behaviour is protected; ordering is not |
| A8 | NOT_APPLICABLE | the deliberate-mutant procedure by design. Three of its four boundaries re-run here (P5, P7; the third audit's N8–N11) |
| A9 | NOT_APPLICABLE | characterization. Its row's claim is only half pinned: Finding 6 |
| A10 | NOT_APPLICABLE | verified by inspection, declared as such |
| U1, U2 | LIKELY | assertion reds recorded. Cycles 1–3 share `0a330af`, and the log records that a `git checkout --` destroyed and rewrote their implementation mid-cycle |
| U3, U4, U5, U6 | LIKELY | passed on first run against code that predates this feature (SQLite `IN ()`, `MAX` over NULL, `COALESCE`); recorded mutants; re-run here (S2, S3) |
| U7, U8, U9 | PROVEN | cycle 6, `Assert.True() Failure`; test and source together in `81d3e63` |
| U10, U11 | PROVEN | cycle 7, `Expected: Tuple (True, …12:00:00Z, 24) / Actual: Tuple (True, …12:05:00Z, 24)`; `81d3e63` |
| U12 | **TEST_AFTER** | `81d3e63` (cycle 7) rewired `GetStatusAsync` as well as the list action. No test then touched the status action. The test arrived at cycle 9 and passed on first run |
| U13 | **TEST_AFTER** | the same commit switched the flag to `HasAnyAsync`. The cycle 7 test expected `true` after seeding a release, which the old `lastRun is not null` also gave. The test arrived at cycle 10. Its mutant does not catch S7 (Finding 1) |
| U14, U15 | LIKELY | satisfied by the smallest change for `U10` (read `MAX` with `EnabledSourceIds()` per request); passed on first run; recorded mutants |
| U16, U17, U18 | PROVEN | cycle 13 assertion red after a `null` stub; test and source together in `20778d0` |
| U19, U20, U21 | LIKELY | passed against cycle 15's `null` stub; they constrain the generalization `U22` forced. `U21` is mutant-proven (P9). `U20` is not: Finding 3 |
| U22 | PROVEN | cycle 15 red |
| U23 | LIKELY | passed against the hours-only implementation it triangulates against; cycle 16's red came from the other rows |
| U24–U27 | PROVEN | cycle 16, `expected: '…2 days ago.' / actual: '…48 hours ago.'` (7 failed) |
| U28 | PROVEN | the wording is in cycle 16's red expected strings, before the code produced it |
| U29–U33 | NOT_APPLICABLE | characterization (`BASELINE`); mutants P3, P4, P10 caught |
| U34 | PROVEN | cycle 14, `user-view.html exposed no NewReleasesInternals` |
| U35 | PROVEN | cycle 20 red; `df7f7b5` |
| U36 | **TEST_AFTER** | `T029` wrote the ladder against a missing-function red only. The log (Phase 9 table) records that three of its four boundaries survived mutation until `T039` added the assertions |
| U37 | LIKELY | its assertion existed before `checkedText`, but the only recorded red is `checkedText is not a function` |
| U38 | **TEST_AFTER** | the clamp shipped with `T029`. The log (Phase 10) records it surviving deletion until `T051` added the test |

Git history adds nothing for `U36`–`U38`. `T029`, the `T030` rename and both remediation phases are
one commit, `36ef43b`, so history cannot order test against code for any of that work.

### Existing tests: nothing weakened

`git diff 36ef43b HEAD` over every test file this feature owns changes only two files, both in
`005`:

- `exposure.test.js` widened the exact key sets to add `render` and `renderStatus`. That is an
  added expectation, not a loosened one.
- `load-page.js` switched the sandbox to the fake DOM.

The C# test files are byte-identical since `002` closed. No `Skip`, `.skip`, `todo` or filter
exclusion exists. The removals Phase 9 made were judged legitimate by the third audit and are not
re-graded.

### tasks.md against the list

Every ticked behavioural task names only `DONE` behaviours, except `T023` and `T024` (`U29`–`U33`
are `BASELINE`). That is the extension's known vocabulary conflict, recorded in the profile, and is
not raised again. One task is open: `T037`, the manual check against Jellyfin **10.11.11**.
Feature `003` dropped that version, so the task cannot be run as written.

## Findings

| # | Sev | Finding | Evidence |
| --- | --- | --- | --- |
| 1 | HIGH | `HasStoredReleases` is never tested with stored releases the caller cannot see; `visible.Count > 0` passes everything | `ReleasesController.cs:95`; `ReleasesControllerTests.cs:156-169`; mutant S7 |
| 2 | HIGH | The staleness threshold is only ever tested at 24 h; hard-coding 24 passes everything | `user-view.html:106`; `staleness.test.js:13,26-31`; mutant V1 |
| 3 | HIGH | The user page's future-instant test stays inside the interval; `Math.abs` for the clamp passes everything | `user-view.html:105`; `staleness.test.js:22-24`; mutant V2 |
| 4 | HIGH | FR-011's "same instant" is tested only where every path agrees trivially; the administrator view may ignore the enabled sources | `AdminController.cs:97`; `AdminControllerTests.cs:107-127`; mutant V3 |
| 5 | HIGH | The same gap on the user status endpoint: it may drop the stored-releases gate | `ReleasesController.cs:162`; `ReleasesControllerTests.cs:139-153`; mutant V4 |
| 6 | HIGH | `A9` claims a title with markup is escaped on the page; only the `esc` helper is tested, and `row()` may drop it | `user-view.html:143`; `esc.test.js`; mutant V5 |
| 7 | MED | Six behaviours are `TEST_AFTER`; the cycle log does not mark them so | `U12`, `U13`, `U36`, `U38`, `A6`, `A7`; table above |
| 8 | MED | The fetch instant equals the run's start, so "the latest run's start" passes `U10`/`U11`; only `A20` stands in the way | `ReleasesControllerTests.cs:108-117` |
| 9 | MED | `A6` cannot tell the data age from the run time: the stub clock does not move, and the cooling-down source never had an earlier fetch | `ConfigureAndRunTests.cs:107-123` |
| 10 | MED | `U1` writes the newer fetch last and at one source, so "the last row written" passes it | `ArtistRepositoryTests.cs:138-149` |
| 11 | MED | `tdd/test-list.md` prints a node single-test command that runs on no match and exits 0 | `test-list.md`, "Verification commands" |
| 12 | MED | `loadPageDom` swallows any error thrown after the helpers are exposed; with the fake DOM a page now initializes fully, so an initialization regression is hidden from every helper test | `tests/web/load-page.js:104-108` |
| 13 | LOW | Text drift: `A7`'s row says "stops moving" (the assertion is `null`); `ConfigureAndRunTests.cs:143` still says "last refreshed"; `exposure.test.js` titles say "pure helpers" and its comment omits the render tests, though `005` added `render`/`renderStatus`; `AdminControllerTests.cs:131`'s name claims a precondition it does not assert; `test-list.md` still prints a `dotnet@9` path | as cited |

### Finding 1 (HIGH): the empty-state flag follows the visible rows in every test

`ReleasesController.cs:95` passes `hasStored` (from `HasAnyAsync`) as the flag. Mutant S7 passes
`visible.Count > 0` instead. Result: 303 passed, 0 failed, and the node suite is unaffected. Every
test of the flag (`:156-169`, `:173-184`, `A5`, `A4`) uses a caller who sees all folders, with no
filter, so the visible rows always equal the stored rows.

The two differ when stored releases exist but none is visible to this request. Two ways that
happens:

- a filter that matches nothing
- a user with access to a library holding no tracked artist

`user-view.html:173` then shows "No data yet. New Releases is waiting for its first refresh." It
should show "Nothing missing for this selection." `FR-008`: the empty state "MUST depend on whether
stored release data exists". `U13`'s row says the same. Fix: a test that stores a release, asks
with a filter (or a caller) that hides it, and asserts `HasStoredReleases` is `true`.

### Finding 2 (HIGH): the refresh interval is never varied

`stalenessText(checkedAt, now, intervalHours)` gates on `ageMs <= intervalHours * 3600000`. Every
test passes `INTERVAL_HOURS = 24`, and every page fixture serves `refreshIntervalHours: 24`. So
mutant V1 (`intervalHours` → `24`) passes all 64 node tests.

An operator who sets a 12 h or a weekly trigger would get a threshold that ignores it. `FR-006`
ties the line to "one refresh interval". The interval is what `001`'s `R15` reads from the
trigger. Fix: assert both sides of the boundary at a second interval.

### Finding 3 (HIGH): the user page's clamp is unobservable as tested

`staleness.test.js:22-24` uses `ahead(5 * HOUR)`. A negative age of 5 h is under the 24 h gate, so
the result is `null` with or without the clamp. The third audit judged dropping the clamp
equivalent here (P2 confirms). `Math.abs` is not equivalent: mutant V2 makes an instant 30 h in
the future read as "Releases last checked 30 hours ago."

That age is invented, and `FR-010` and `U20`'s row ("counts as an age of zero") forbid it. Fix: a
future instant beyond the interval, asserting `null`.

### Findings 4 and 5 (HIGH): one instant everywhere, tested only where it is trivially one

`FR-011` requires the list, the status endpoint and the administrator view to report the same
instant. Its tests (`U12` at `ReleasesControllerTests.cs:139-153`, `U17` at
`AdminControllerTests.cs:107-127`) both have releases stored, MusicBrainz rows only, and every
source enabled. Under those conditions each path's gate and source set make no difference. Two
mutants show it:

- **V3:** `AdminController.cs:97` passes a fixed set of both sources instead of
  `configuration.EnabledSourceIds()`. Result: 303 passed. After an operator disables a source, the
  administrator page would report a different instant from the user page.
- **V4:** `GetStatusAsync` at `ReleasesController.cs:162` drops the stored-releases gate. Result:
  303 passed. After a purge, the status endpoint would report an instant while the list reports
  none.

The administrator copy of the gate is caught (S8, by `U35`). The status copy and the administrator
source set are not. Fix: compare all three responses after a purge and after the newest source is
disabled.

### Finding 6 (HIGH): the escaping test stops at the helper

`A9` reads "A release title containing HTML markup is escaped rather than rendered as markup"
(`US3-AS2`, `SC-008`). `esc.test.js` pins `esc`. Nothing asserts that `row()` uses it. Mutant V5
removes `esc()` around `item.title` at `user-view.html:143`, and all 64 node tests pass. No fixture
title contains markup, and no render test checks escaping.

When `002` closed, rendering was out of scope for want of a DOM. `005` has since added
`loadPageDom` and the fake DOM, and `render.test.js` already drives `render()` with fixtures, so
this test can now be written. Release titles come from MusicBrainz and Deezer. This is the one
place in the feature where a regression is an injection, not a wrong number.

### Finding 7 (MED): test-after work that the log does not label as such

The playbook asks that a behaviour whose code came first be "recorded as test-after in the log". The
cycle log is honest about every fact behind the classification:

- cycle 7's note says the change "also makes `U11`–`U14` true"
- cycle 21 says `A6`/`A7` passed on first run
- the Phase 9 and 10 tables show `U36`'s boundaries and `U38`'s clamp surviving until their tests
  arrived

But it never uses the label. It describes `A6`/`A7` as the expected end of the double loop, which
the playbook does not say. Each of these behaviours is now mutant-proven except for the gaps in
Findings 1, 4 and 5, so the cost is evidence, not protection. It cannot be repaired by new work.
The remedy is to label the entries and record whether the maintainer accepts them.

### Findings 8–10 (MED): coincident inputs, smaller cases

These come from the subagent. Each was vetted by reading the cited lines; none was run as a
mutant.

- **Finding 8, `U10`/`U11`:** the fetch is written at the clock's current instant, and
  `StartRunAsync` follows with no advance, so the run's start equals the fetch.
- **Finding 9, `A6`:** the clock does not move during `RunAsync`, so the Deezer fetch, the run's
  start and its end are all `SourceHarness.Start`.
- **Finding 10, `U1`:** the newer fetch is both the last row written and at the same source as the
  older one.

### Finding 11 (MED): a printed command that passes on no match

`tdd/test-list.md`'s single-test command for the page side is
`node --test --test-name-pattern "<name>" "tests/web/*.test.js"`. Run here with a name that
matches nothing: `# tests 9, # pass 9, # fail 0`, exit 0. The profile sets node `single: null`
for this reason. This is the same hazard as the third audit's Finding 4, in a line that audit's
task did not reach.

### Finding 12 (MED): the harness hides initialization failures

`load-page.js:104-108` rethrows only when the page fails before it exposes
`NewReleasesInternals`. In `002` the catch absorbed the expected failures against a DOM that
answered `null`. Since `005`, the fake DOM lets initialization run to completion, so a throw there
is now a real regression. The helper tests absorb it silently. The render tests would surface it
only if they read what initialization writes.

### Not raised again

The exact key set in `exposure.test.js` was decided and documented in `T056`, and stays decided.
Several smaller observations are below the bar for a task:

- the eager `HasAnyAsync` sequence
- the unlabelled cases in `esc.test.js`
- the sentence test at `staleness.test.js:60-67`, which the ladder template also pins
- `GetReleases_RefreshIntervalFollowsTheTrigger` holding three cases in one `[Fact]`, with no
  weekly case. That rule is `001`'s `R15`.

## Mutation results

There is no tool (`mutation: null`). Each of the 25 deliberate mutants was applied alone from a
file copy and restored with a `cmp` byte check, never with `git checkout`. Both suites were re-run
green after the last restore (303 / 64), and `git status` is clean.

Page mutants ran the full node suite. Server mutants ran the behaviour's tests. S7, V3 and V4
survived, and each was re-run against the full dotnet suite.

| # | Mutant | Behaviour | Survived | Judgment |
| --- | --- | --- | --- | --- |
| P1 | `admin.html` drop `Math.max(0, …)` | U38 | No | The third audit's `HIGH` is closed |
| P2 | `user-view.html` drop `Math.max(0, …)` | U20 | Yes | **Equivalent**: the interval gate hides any negative age |
| P3 | `healthText` `when(s.cooldownUntil)` → `when(null)` | U33 | No | Third audit's Finding 3a closed |
| P4 | `artistLink` stops encoding the server id | U32 | No | Third audit's Finding 3b closed |
| P5 | `user-view.html` floor → round | U26, U27 | No | 2 failed |
| P6 | `admin.html` floor → round | U36 | No | 2 failed |
| P7 | `user-view.html` month length 30.5 → 30 days | U27 | No | 1 failed |
| P8 | `admin.html` drop the no-instant guard | U37 | No | 4 failed |
| P9 | `user-view.html` gate `<=` → `<` | U21 | No | 1 failed |
| P10 | `esc` stops escaping `'` | U29, A9 | No | 1 failed |
| S1 | `ArtistRepository` `MAX` → `MIN` | U1 | No | 1 failed |
| S2 | source filter bypassed (`1=1 OR …`) | U2, U3 | No | 2 failed |
| S3 | `@completeAt` bound on every outcome | U5, U6 | No | 2 failed |
| S4 | `HasAnyAsync` SQL → `SELECT 1` | U7–U9 | No | 1 failed |
| S5 | list/status gate always open | A5 | No | 1 failed |
| S6 | status reports `null` | U12 | No | 1 failed |
| **S7** | list flag `hasStored` → `visible.Count > 0` | U13 | **Yes** | **Real.** Finding 1. 303/303 passed |
| S8 | administrator gate always open | U35 | No | 1 failed. Refutes the subagent's claim |
| S9 | MusicBrainz always enabled | U2, U15, A7 | No | 1 failed |
| S10 | Deezer always enabled | U15, A7 | No | 1 failed |
| **V1** | staleness gate uses `24` for the interval | U21, U22, A2, A3 | **Yes** | **Real.** Finding 2 |
| **V2** | user-page clamp → `Math.abs` | U20 | **Yes** | **Real.** Finding 3 |
| **V3** | administrator view ignores `EnabledSourceIds()` | U17 | **Yes** | **Real.** Finding 4. 303/303 passed |
| **V4** | `GetStatusAsync` drops the stored gate | U12 | **Yes** | **Real.** Finding 5. 303/303 passed |
| **V5** | `row()` writes the title unescaped | A9 | **Yes** | **Real.** Finding 6 |

18 of 25 were caught. Of the 7 survivors, 1 is equivalent and 6 are real, all inside `DONE`
behaviours. The S-series repeats the third audit's server sample with new operators. The V-series
tests the subagent's claims.

## Traceability

Resolved by hand. The list's `traces` column mixes scenario ids and requirement ids. Every
named test was confirmed to exist and run (18 C# methods by name, 6 node files).

| Criterion | Behaviours | Real entry point | Gap |
| --- | --- | --- | --- |
| FR-001, FR-002, FR-004, FR-005 | U1–U3, U10, U11, U15 + A1, A6 | Yes, `ConfigureAndRunTests` | Findings 8–10 |
| FR-003 | U5, U6, U11 + A1 | Yes | |
| FR-006 | U21, U22 + A2, A3; `005`'s `render.test.js` drives the DOM line | Yes | **Finding 2**: the interval is never varied |
| FR-007, SC-004 | U28 | Yes | |
| FR-008 | U4, U7–U9, U13, U14, U19, U35 + A4, A5 | Yes | **Finding 1**: the flag is never tested against hidden rows |
| FR-009, SC-005 | U16–U18, U36–U38; `005`'s `render-status.test.js` drives the row | Controller level (`acceptance: null`) | |
| FR-010 | U20, U38 | Yes, both pages | **Finding 3**: the user-page clamp |
| FR-011 | U12, U17, U35 | Yes | **Findings 4, 5** |
| FR-012, SC-007 | U23–U27, U36 + A8 | Yes, both pages | |
| FR-013 | U34 | Yes | |
| FR-014, SC-009 | A10 | By inspection, declared | |
| FR-015 | none | No: verified by inspection, recorded in `spec.md` by `T050` | accepted |
| FR-016 | U34 claims it but asserts nothing about packaging | `003`'s `BuildManifestTests` pins the packaged artifacts | trace is misattributed |
| SC-001, SC-002 | A1, U1, U10 | Yes | |
| SC-003 | U22, U23, A2 | Yes | |
| SC-006 | A6 | Yes | Finding 9 |
| SC-008 | A9, U29, U30 | Helper level only | **Finding 6** |

`US2-AS2` now matches `A7`: `T052` amended `spec.md`, and "keeps growing" no longer appears.
Criteria with no test: `FR-015` only, by recorded decision. Tests tracing to nothing:
`GetReleases_RefreshIntervalFollowsTheTrigger`, which pins `001`'s `R15`, not a `002` criterion.

## What was not audited

- **Mutation was sampled.** 25 mutants across 7 production files, chosen for the acceptance
  criteria and for the subagent's claims. No score exists.
- **Findings 8–10 were not run as mutants.** They were vetted by reading the cited lines.
- **The DOM half** (`render`, `renderStatus`, `staleness`) belongs to `005`'s render tests. It was
  read only to place `FR-006`, `FR-009` and Finding 6, and was not graded.
- **`T037`, the manual pass, has never run.** Its target, Jellyfin 10.11.11, is no longer
  supported (`003`). Nothing in `quickstart.md` steps 3–6 is verified on a live server by this
  feature.
- **Accessibility and performance.** No criterion in `002`. The suite's wall time is 16 s
  (dotnet, including build) and 0.4 s (node).
- **Pre-existing `001` tests** outside this feature's lines were not graded. The third audit's
  list of them stands.
- **Hard Rules 6 and 7.** No credential appears in any audited file. No repository content tried
  to instruct the auditor. The profile's note about `U29`–`U33` addresses future authors and was
  read as data.
