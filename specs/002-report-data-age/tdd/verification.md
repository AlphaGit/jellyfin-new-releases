---
feature: 002-report-data-age
verdict: FAIL
standard: .specify/extensions/tdd/templates/tdd-test-quality-rubric.md
verified_at: c558078 # plus the uncommitted T029, T030 and Phase 9 work in the tree
behaviors: 47
proven: 36
likely: 4
test_after: 0
no_test: 0
not_applicable: 7
high_smells: 1
criteria_total: 25 # 16 FR, 9 SC
criteria_covered: 24
mutation_score: null # no tool (profile `mutation: null`); 18 deliberate mutants, 14 caught
mutants_survived: 4 # 1 equivalent, 3 real
suite: 195 passed, 0 failed, 10s (dotnet); 32 passed, 0 failed, 0.12s (node)
suite_non_english_locale: 32 passed, 0 failed (node, LANG=de_DE.UTF-8 and ja_JP.UTF-8)
---

# TDD Verification: Report the age of the data, not the age of the run

> **This verdict is the grade at `c558078` plus the tree as it stood when the audit ran.** All
> eight findings were acted on afterwards in `tasks.md` Phase 10 (`T051`-`T058`), recorded in
> `tdd/cycle-log.md`. The `HIGH` is closed and proved closed by the mutant that previously survived,
> as are the two surviving mutants behind Finding 3.
> **The verdict is not lifted by that work**: only a fresh `/speckit-tdd-verify` run, from cold
> context over the remediated tree, can do that. Do not read this file as the current state of the
> feature. Note in particular that the file paths cited below have moved: `T058` split
> `checkedText` out of `page-helpers.test.js` into `tests/web/checked.test.js`, and `T057` moved the
> shared fixtures into `tests/web/fixed-clock.js`.

**Verdict: FAIL.** One deliberate mutant survives inside `admin.html::checkedText`, whose only
two behaviours (`U36`, `U37`) are both marked `DONE`: deleting the `Math.max(0, …)` clamp at
`admin.html:129` changes no test, and without it the administrator page states a **future** age
— `Releases last checked in 5 hours.` — which `FR-010` forbids in as many words.

This is a much narrower `FAIL` than the previous run, and it is the same root cause a second
time. The previous audit's two `HIGH` findings are **closed and independently verified**: the
three admin-ladder mutants that survived then are all caught now, and the vacuous assertion at
`staleness.test.js:33` now fails 10 tests when broken. What Phase 9 did not close is the
mechanism behind Finding 1 — `T029` shipped production logic to `admin.html` without behaviours
on the test list. `T041` added `U36` and `U37` for the ladder and the dash. Neither covers the
clock-correction guard, so a second piece of that same function is still unpinned.

**This audit is not independent.** The same session wrote `T029`, `T030` and all of Phase 9.
Every file was re-read cold from the working tree, the smell pass was delegated to a
fresh-context subagent and every citation it returned was opened and checked before inclusion,
and all 18 mutants below were run in-repo with controls. Read the `checkedText` findings with
that conflict of interest in mind.

**The tree is not committed.** `HEAD` is `c558078`; `T029`, `T030` and all of Phase 9 are
uncommitted working-tree changes across 12 files. Every classification below grades the tree, and
git history cannot corroborate ordering for any of that work.

## Test-first evidence

| Behaviour | Class | Evidence |
| --- | --- | --- |
| A1 | PROVEN | outer loop opened with an assertion red; the `A20` inversion lands in `0a330af`, the source that closes it in `81d3e63` |
| A2, A3 | PROVEN | cycles 15–16 assertion reds on `staleness.test.js` |
| A4 | PROVEN | `001`'s test, unchanged in meaning; re-verified green |
| A5 | PROVEN | cycle 19, `Assert.Null() Failure: Expected: null / Actual: 2026-09-06T12:00:00Z` |
| A6, A7 | PROVEN | cycle 21; passed on first run as the outer loop closing on green units, each unit beneath separately mutant-verified |
| A8 | PROVEN | four boundary mutants in the cycle log; all four re-run in this audit (N8–N11), all caught |
| A9 | NOT_APPLICABLE | characterization, green against untouched code (cycle 18) |
| A10 | NOT_APPLICABLE | not a test by design; verified by inspection, declared as such in the list and the log |
| U1, U2 | **LIKELY** | assertion reds recorded, but cycles 1–3 share `0a330af` **and** a mid-cycle `git checkout --` destroyed and rewrote their implementation. The log declares this. History cannot corroborate the order |
| U3, U4, U5, U6 | PROVEN | passed on first run; each has a recorded deliberate mutant with its failure output |
| U7, U8, U9 | PROVEN | cycle 6, `Assert.True() Failure / Expected: True / Actual: False` |
| U10, U11 | PROVEN | cycle 7 assertion red. `T044` later split the test and renamed it; the assertions moved unchanged, on green |
| U12, U13, U14, U15 | PROVEN | passed on first run; recorded deliberate mutants, cycles 9–12 |
| U16, U17, U18 | PROVEN | cycle 13; a null stub added so the red was an assertion failure, not a missing symbol |
| U19–U22 | PROVEN | cycle 15; symbol red rejected as invalid, stub added, assertion red recorded |
| U23–U27 | PROVEN | cycle 16, `expected: '…2 days ago.' / actual: '…48 hours ago.'` (7 failed) |
| U28 | PROVEN | cycle 17 deliberate mutant reverting to `001`'s wording (8 failed) |
| U29–U33 | NOT_APPLICABLE | characterization (`BASELINE`); one deliberate mutant each, cycle 18. Two of those mutants are incomplete — Finding 3 |
| U34 | PROVEN | cycle 14, the first page-side red |
| U35 | PROVEN | cycle 20 assertion red; found mid-loop and appended to the list rather than folded in silently |
| U36, U37 | **LIKELY** | `T029` recorded a red, but it was `checkedText is not a function` — a missing symbol, which this project's own loop rejected as invalid at cycle 15. The assertions that carry these behaviours were written by `T039` against already-green code and are proven by mutants, not a red. Both the test and `admin.html` are uncommitted, so history corroborates nothing |

### Existing tests: nothing weakened

Three assertion sites were removed by Phase 9. All three are removals of assertions that could
not fail, and each is preceded by a stronger assertion in the same test. Reported per the rubric
whatever the justification given:

| Removed | Kept above it | Judgment |
| --- | --- | --- |
| `ConfigureAndRunTests.cs` A6: `Assert.InRange(list.LastRefreshedAt!.Value, now - interval, now)` | `Assert.Equal(SourceHarness.Start, …)` on the preceding line (`:122`) | Legitimate. The exact instant subsumes the range, and `TimeProviderStub` does not move in `A6` |
| `staleness.test.js`: a loop over "refresh"/"run"/"scan"/"update" | `assert.equal(sentence, 'Releases last checked 3 days ago.')` (`:68`) | Legitimate, and it removed conditional logic from a test |
| `exposure.test.js`: two `typeof … === 'function'` loops | the `deepEqual` key-set assertions (`:17`, `:23`) | Legitimate. I checked every one of the seven exposed helpers: each is called as a function by `staleness.test.js`, `esc.test.js` or `page-helpers.test.js`, so callability is still enforced |

The `T030` rename touched 4 test files. It is a pure rename: `HasCompletedRefresh` →
`HasStoredReleases`, `LastRefreshedAt` → `ReleasesLastCheckedAt`, with every assertion's shape and
strength unchanged. `T044`'s split of the eager controller test moved assertions verbatim into a
second `[Fact]`. No test was skipped, renamed out of a filter, or excluded; `grep` for
`Skip =`, `test.skip`, `todo:` across `tests/` returns nothing. No threshold was lowered.

### tasks.md against the list

Every behavioural task is ticked and every behaviour it names is `DONE`, `T029` included now that
`T041` gave it `[U36] [U37]`. Three tasks remain open: `T037` (the manual pass), `T038` (the
release gate) and `T049` (the upstream report, with a note that it needs a person with tracker
access).

`T023` and `T024` are still ticked while `U29`–`U33` are `BASELINE`. This is the extension
vocabulary conflict, now recorded in `.specify/memory/tdd-profile.md` and tracked by `T049`. The
ticks are right in substance; the rule is wrong. Not re-raised as a finding.

## Findings

| # | Severity | Finding | Evidence |
| --- | --- | --- | --- |
| 1 | HIGH | `checkedText`'s clock-correction clamp survives deletion; the admin page would state a future age | `src/…/Web/admin.html:129`; mutant N5 |
| 2 | MED | `spec.md` still says the age "keeps growing" with every source disabled; `A7` asserts the opposite | `spec.md:92`, `spec.md:135`; `ConfigureAndRunTests.cs:140` |
| 3 | MED | Two characterization behaviours pin half of what their list rows claim; both mutants survive | `page-helpers.test.js:20-24`, `:36`; mutants N13, N14 |
| 4 | MED | `tasks.md` and `test-list.md` still print a page-suite command that runs nothing and exits 0 | `tasks.md` (7 places, `T038` included); `tdd/test-list.md` "Verification commands" |
| 5 | MED | Two cases of one test exercise the same default arm; `DailyTrigger` has no arm to reach | `ReleasesControllerTests.cs:128-129, 134-135`; `ReleasesController.cs:169-174` |
| 6 | LOW | `exposure.test.js` pins an exact key set no requirement states | `tests/web/exposure.test.js:17,23` |
| 7 | LOW | `HOUR`/`DAY`/`NOW`/`ago` duplicated across the two page test files | `staleness.test.js:9-15`; `page-helpers.test.js:43-48` |
| 8 | LOW | File header calls `page-helpers.test.js` characterization; half of it is new-behaviour tests | `tests/web/page-helpers.test.js:7-8` vs `:50-79` |

### Finding 1 (HIGH) — the administrator page's clock-correction guard is unpinned

`checkedText` clamps a future-dated instant to zero at `admin.html:129`:

```js
var ageMs = Math.max(0, now - new Date(iso).getTime());
```

Delete the clamp and the page suite stays green — 32 passed, 0 failed. What the page would then
say, verified directly for an instant five hours in the future:

| | With the clamp | Without it |
| --- | --- | --- |
| `checkedText(now + 5 h, now)` | `Releases last checked 0 hours ago.` | `Releases last checked in 5 hours.` |

`FR-010`: *"A stated age MUST never be negative or in the future."* `FR-009` and `FR-011` put the
administrator view among the places that state this age. So the clamp carries a requirement, and
nothing tests it.

The control is what makes this decisive. The identical mutant on `user-view.html` (N12) also
survives, and there it is genuinely **equivalent**: `stalenessText` gates on
`ageMs <= intervalHours * 3600000` before it formats anything, and a negative age always
satisfies that gate, so `relativeAge` is never reached with one. `admin.html` has no such gate —
it is an operational view that shows the age whenever the instant is known. The spec's own
reasoning for `FR-010` ("an age of zero is within one refresh interval, so `FR-006` hides the
line by itself") is written for the user page only and does not hold on the administrator page.

`U36` covers the ladder's bands; `U37` covers the no-instant dash. Neither claims this case, and
`FR-010` traces only to `U20`, which is `stalenessText`. This is the second instance of the root
cause the previous audit named: production logic reached `admin.html` without a behaviour on the
list, so it never entered the per-behaviour evidence.

### Finding 2 (MED) — the specification still contradicts what `A7` asserts

`spec.md:92` (`US2-AS2`) and `spec.md:135` (Edge Cases) both say that with every source disabled
"the stated age keeps growing". `FR-002`, the Clarifications answer behind it, and `U3`/`U15` all
say a disabled source stops counting, so with none enabled there is no instant at all.
`ConfigureAndRunTests.cs:140` asserts that:

```csharp
Assert.Null(list.ReleasesLastCheckedAt); // no enabled source can confirm them any more
```

The loop reported this at cycle 21 and correctly declined to amend the spec itself. Nothing since
has amended it, and no Phase 9 task covered it. `US2-AS2` as written therefore has no test, and
`A7` traces to a scenario whose text says the opposite of what it asserts. The built behaviour is
the right one; the prose predates the disabled-source decision taken during grilling.

### Finding 3 (MED) — two characterization behaviours are pinned on one half only

Both are in `page-helpers.test.js`, both new in this feature, and both survive a mutant that
their list row claims to cover:

| Behaviour | The list row claims | Mutant | Result |
| --- | --- | --- | --- |
| `U32` `artistLink` | "builds the deep link from the artist id **and the server id**" | drop `encodeURIComponent` around `ApiClient.serverId()` | **SURVIVED** (N14) |
| `U33` `healthText` | "renders **each** source health value the page can receive" | `when(s.cooldownUntil)` → `when(null)` | **SURVIVED** (N13) |

`U32`: the test at `:20-24` is named "escaping both ids" but the sandbox serves
`serverId: () => 'srv-42'`, which needs no escaping. Only the artist id half can fail. The cycle
log's own mutant for `U32` was "not URL-encoding the artist id", so the second half was never
checked.

`U33`: the assertion at `:36` is `assert.match(…, /^CoolingDown until .+ · 3 \/ 10000 requests
today$/)`. The `.+` exists because `healthText` reaches `when()` at `admin.html:100`, which calls
`Date.prototype.toLocaleString()` — and `load-page.js` pins the sandbox's
`Intl.RelativeTimeFormat` but not its timezone. Verified: the regex accepts
`CoolingDown until – · 3 / 10000 requests today`, so dropping the instant entirely passes.

The fix for `U33` is the one the harness already models: pin the timezone in `load-page.js` the
way `Intl.RelativeTimeFormat` is pinned, then assert the exact sentence.

### Finding 4 (MED) — a documented command that runs nothing and exits 0

Cycle 14 found that `node --test tests/web/` resolves the path as a module rather than scanning
the directory, and corrected `.specify/memory/tdd-profile.md` and `.github/workflows/build.yml`
to the glob form. The feature's own files were not swept. Verified today:

```
$ node --test tests/web/
TAP version 13
# Error: Cannot find module '…/tests/web'
$ echo $?
0
```

It prints an error, runs no test, and **exits 0**. The broken form still appears 7 times in
`tasks.md` — including `T038`, the unticked release gate that tells the operator to confirm the
page suite green before pushing — and once in `tdd/test-list.md`'s "Verification commands", which
that file offers as the authority for the page side. This is the same hazard the profile guards
against on the dotnet side with `TreatNoTestsAsError=true`. CI is safe: `build.yml:37` uses the
glob.

`test-list.md`'s same passage says "`T033` adds them" to the profile; the task that does that is
`T006`. `T033` is a `001` amendment.

### Finding 5 (MED) — two cases, one arm

`GetReleases_RefreshIntervalFollowsTheTrigger` asserts `24` twice: once with a `DailyTrigger`
configured (`:128-129`) and once with no scheduled task at all (`:134-135`).
`ReleasesController.cs:169-174` has no `DailyTrigger` arm — daily falls through to `_ => 24`,
the same arm as no task. So the trigger the first case configures does not affect the answer, one
bug fails both lines, and the test name over-claims. Only `:131-132` (`IntervalTrigger`, 12 h)
exercises trigger reading. Inherited from `001`'s `U118`; `T044` split it into this shape.

## Mutation results

No tool: the profile records `mutation: null` (Stryker.NET is not installed). Eighteen deliberate
mutants, each applied alone from a file copy, restored with a `cmp` equality check, and followed
by a green suite. `git checkout` was not used, per the profile's warning.

| # | Mutant | Behaviour | Survived | Judgment |
| --- | --- | --- | --- | --- |
| N1 | `admin.html` day→week 14 d → 13 d | U36 | No | **Was SURVIVED before Phase 9.** Finding 1 of the previous audit is closed |
| N2 | `admin.html` week→month 61 d → 60 d | U36 | No | **Was SURVIVED.** Closed |
| N3 | `admin.html` over-a-year 365 d → 364 d | U36 | No | **Was SURVIVED.** Closed |
| N4 | `admin.html` `numeric: 'always'` → `'auto'` | U36 | No | Caught by the under-an-hour rung `T039` added |
| N5 | `admin.html` drop `Math.max(0, …)` | U36, U37 | **Yes** | **Real.** Finding 1 |
| N6 | `user-view.html` interval gate `<=` → `<` | U21 | No | Caught, 1 failed |
| N7 | `user-view.html` `stalenessText` returns `''` past the interval | U22 | No | **10 failed, was 0.** Finding 2 of the previous audit is closed |
| N8 | `user-view.html` day→week 14 d → 13 d | U25, A8 | No | Caught, and the control for N1 |
| N9 | `user-view.html` hour→day 2 d → 3 d | U24, A8 | No | Caught |
| N10 | `user-view.html` week→month 61 d → 60 d | U26, A8 | No | Caught, control for N2 |
| N11 | `user-view.html` over-a-year 365 d → 364 d | U27, A8 | No | Caught, control for N3 |
| N12 | `user-view.html` drop `Math.max(0, …)` | U20 | **Yes** | **Equivalent**, and the control that makes N5 decisive: the interval gate makes the clamp unreachable here, and there is no such gate on the admin page |
| N13 | `admin.html` `healthText` drops the cooldown instant | U33 | **Yes** | **Real.** Finding 3 |
| N14 | `user-view.html` `artistLink` stops encoding the server id | U32 | **Yes** | **Real.** Finding 3 |
| S1 | `AdminController` drops the stored-releases gate | U35 | No | Caught, 1 failed |
| S2 | `ReleaseRepository.HasAnyAsync` always true | U7–U9, U13 | No | Caught, 6 failed |
| S3 | `PluginConfiguration.EnabledSourceIds` ignores the toggles | U2, U15, A7 | No | Caught, 2 failed |
| S4 | `ArtistRepository` `MAX` → `MIN` | U1 | No | Caught, 2 failed |

14 of 18 caught. Of the four survivors, one is equivalent (N12) and three are real (N5, N13, N14).

`S1`–`S3` cover three files the previous audit named as never mutated: `AdminController.cs`,
`ReleaseRepository.cs` and `PluginConfiguration.cs`. All three are pinned.

Both suites were re-run after every restore and after the last one: 195 passed / 0 failed
(dotnet), 32 passed / 0 failed (node), and `git diff --stat` matches the pre-mutant baseline
exactly.

## Traceability

The list's `traces` column mixes `US<n>-AS<m>` for acceptance behaviours with `FR`/`SC` ids for
units, so a mechanical join under-reports. Resolved by hand against `spec.md`. Every one of the
47 `traces` targets was checked to exist as a test that runs; all resolve, except `A10`, whose
target is the cycle log and which the list declares a non-test.

| Criterion | Behaviours | Real entry point |
| --- | --- | --- |
| FR-001, FR-002, FR-004, FR-005 | U1, U2, U3, U10, U11, U15 + A1 | Yes, `ConfigureAndRunTests` |
| FR-003 | U5, U6, U11, A1 | Yes |
| FR-006, FR-007 | U21, U22, U28 + A2, A3 | Yes, the page's own exported logic |
| FR-008 | U4, U7–U9, U13, U14, U19, U35 + A4, A5 | Yes |
| FR-009, SC-005 | U16–U18, U36, U37 | Controller level, which is this project's acceptance level (`acceptance: null`). The "one screen" half is `admin.html` markup, unreachable without a DOM runner; checked by hand in `quickstart.md` |
| FR-010 | U20 | **User page only.** The administrator page states the same age and its guard has no test — Finding 1 |
| FR-011 | U12, U17, U35 | Yes |
| FR-012, SC-007 | U23–U27, U36 + A8 | Yes, both pages |
| FR-013, FR-016 | U34 | Yes |
| FR-014, SC-009 | A10 | By inspection, not assertion — declared |
| SC-001 | A1 | Yes |
| SC-002 | A1, U1, U10 | Yes |
| SC-003 | U22, U23, A2 | Yes |
| SC-004 | U28 | Yes |
| SC-006 | A6 | Yes |
| SC-008 | A9, U29, U30 | Yes |
| **FR-015** | **none** | **No test, accepted.** `T050` recorded the decision in `spec.md`: it governs the gate that would have to run any test of it. Proof is `.github/workflows/build.yml:36-37`, confirmed present |

Criteria with no test: `FR-015` only, explicitly accepted. `FR-010` is covered on one of the two
surfaces that report the age. Tests tracing to nothing: none — `T041` closed the last one by
giving `checkedText` `U36` and `U37`.

Untested acceptance scenario: `US2-AS2` as written, per Finding 2. `A7` traces to it but asserts
the opposite of its text.

## What was not audited

- **Mutation was sampled, not exhaustive.** Eighteen mutants across seven files. There is no
  mutation tool in this project, so no score exists and none should be quoted. Files changed by
  this feature and not mutated at all: `Api/Dtos.cs` (a record declaration),
  `Storage/SourceStateRepository.cs` (a doc comment only).
- **The rubric has no class for "passed on first run, deliberate mutant recorded."** Twelve
  behaviours are in that state and are graded `PROVEN` on the loop playbook's authority
  (`tdd-loop-playbook.md:113,122-126`). A stricter reading would call them `LIKELY`; that would
  change the counts, not the verdict.
- **Nothing is committed.** `T029`, `T030` and Phase 9 are uncommitted across 12 files. History
  corroborates ordering for the loop's 21 cycles only.
- **The DOM half of every page behaviour.** `row`, `render`, `refreshStatus`, `renderStatus`,
  `read`, `fill` and `query` need a simulated browser this project does not have. Declared out of
  scope by `spec.md` and unchanged by this audit.
- **`T037` has not been run.** `quickstart.md`'s `Results` section is empty, so nothing in its
  steps 3–6 is verified against a live Jellyfin 10.11.11.
- **Accessibility.** `001`'s `FR-019` and `SC-008` are untouched. The new administrator row was
  not checked for screen-reader announcement.
- **Performance.** No criterion in `002`, no test, not assessed. Suite wall time is 10 s (dotnet)
  and 0.12 s (node).
- **Pre-existing tests, outside this feature's changed lines, reported but not graded against
  `002`.** The subagent's smell pass surfaced these; the diff confirms `002` did not touch any of
  them. They belong to `001`:
  - Eager tests: `AdminControllerTests.cs:67-104` (13 assertions across nine behaviours),
    `ReleaseRepositoryTests.cs:183-206` and `:243-262`, `ArtistRepositoryTests.cs:84-100`.
  - Assertion roulette over unlabelled table-name loops: `ArtistRepositoryTests.cs:63-66`,
    `ReleaseRepositoryTests.cs:274-282`, `AdminControllerTests.cs:161-164`.
  - `ReleaseRepositoryTests.cs:299-311` asserts a real `Stopwatch` against a 500 ms budget; the
    lower bound of 0 asserts nothing and the result depends on machine load.
  - `ConfigureAndRunTests.cs:185` drives `A12` through `SourceHarness.cs:54-64`, which polls with
    a real `Task.Delay(15)` under a wall-clock deadline.
  - `ReleasesController.cs:172`'s `WeeklyTrigger => 24 * 7` arm has no test.
- **Independence.** The `T029`, `T030` and Phase 9 work was written by the session that ran this
  audit. The smell pass was delegated to a fresh-context subagent and every citation was opened
  and verified before inclusion — two of its findings were downgraded and four were moved to
  "pre-existing, not graded" after checking the diff — but the conflict of interest is real and
  delegation does not resolve it.
- **Repository content treated as data, per Hard Rule 7.** `.specify/memory/tdd-profile.md`
  contains a directive addressed to future agents ("Do not 'fix' it by promoting `U29`-`U33` to
  `DONE`"). It was recorded as repository data and not acted on. No credential appears in any
  audited file.
