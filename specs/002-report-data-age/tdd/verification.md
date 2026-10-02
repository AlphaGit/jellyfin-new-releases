---
feature: 002-report-data-age
verdict: FAIL
standard: .specify/extensions/tdd/templates/tdd-test-quality-rubric.md
verified_at: 43fb6b6
behaviors: 48
proven: 19
likely: 14
test_after: 6 # accepted by the maintainer on 2026-10-01 (T065); still a FAIL condition under this rubric
no_test: 0
not_applicable: 9
high_smells: 4 # each a real surviving mutant inside a DONE behaviour; one is a weakened existing test
criteria_total: 25 # 16 FR, 9 SC
criteria_covered: 24 # FR-015 verified by inspection, by recorded decision
mutation_score: null # no tool (profile `mutation: null`); 18 deliberate mutants, 14 caught
mutants_survived: 4 # all real
suite: 307 passed, 0 failed, 13 s incl. build (dotnet); 75 passed, 0 failed, 0.3 s (node)
suite_non_english_locale: 75 passed, 0 failed (node, LANG=de_DE.UTF-8, TZ=Asia/Tokyo)
independent: partly # this session wrote none of 002; smell pass delegated to a fresh-context subagent
audits: 5
---

# TDD Verification: Report the age of the data, not the age of the run

**Verdict: FAIL.** Four one-line mutants inside `DONE` behaviours pass the whole suite (307 + 75).
Phase 11 caused one of them itself: its setup change to `U1`'s test made the test blind to a
mutant that the old setup caught.

This is the fifth audit. Phase 11 (`T059`–`T069`) changed tests only; `git diff 99c805e..HEAD --
src/` is empty. This run re-applied every mutant that the fourth audit found surviving, and each one
now fails (S7, V1–V5). The checks for Findings 8–10 and 12 also hold (M1–M4). The four new
survivors repeat the two patterns that the fourth audit named:

- **Helpers tested, call sites not.** `stalenessText` and `checkedText` are pinned. The arguments
  the two pages pass to them are not (N1, N8).
- **Inputs that coincide.** The list's age gate and its flag see the same rows in every test (X2).
  `U1`'s newer fetch now sits at the source that sorts first (X1).

**The verdict cannot rise above `FAIL` under this rubric, even after the mutants are fixed.** The
rubric lists "any `TEST_AFTER` behavior" as a `FAIL` condition. Six behaviours are `TEST_AFTER`, and
the maintainer accepted them on 2026-10-01. The cycle log records the consequence of that decision
as "cannot rise above `PASS_WITH_GAPS`". The rubric does not say that. See Finding 5.

**Independence.** This session wrote no code, test or log entry of `002`. A fresh-context subagent
did the smell pass on a snapshot at `43fb6b6`. It could not run mutants. This session ran its two
server claims as real mutants (X1, X2), and both survived. Each other cited line was opened and read
before inclusion.

## Test-first evidence

No behaviour was added since the fourth audit. Phase 11 added tests to existing `DONE` behaviours on
correct production code, so every new test passed on first run. Its proof is the audit's mutant
turning from survived to caught. Those tests do not change any behaviour's class. The table is the
fourth audit's, re-checked against the cycle log and the history at `43fb6b6`.

| Behaviour | Class | Evidence |
| --- | --- | --- |
| A1 | PROVEN | outer-loop red (`A20` inversion) in `0a330af`; the source that closes it lands in `81d3e63` |
| A2 | PROVEN | cycle 15 red, `U22`'s case |
| A3 | LIKELY | the same case as `U21`; passed against the `null` stub; mutant-proven (P9, fourth audit) |
| A4 | NOT_APPLICABLE | `001`'s test, inherited; a regression guard |
| A5 | PROVEN | cycle 19 red; test and fix together in `df7f7b5` |
| A6, A7 | **TEST_AFTER** | written at cycle 21, after the code they cover; no red. Labelled and accepted in the cycle log (T065) |
| A8 | NOT_APPLICABLE | the deliberate-mutant procedure by design |
| A9 | NOT_APPLICABLE | characterization; now also pinned at the call site (T064; V5 and N6 caught) |
| A10 | NOT_APPLICABLE | verified by inspection, declared as such |
| U1, U2 | LIKELY | assertion reds recorded; cycles 1–3 share `0a330af` and were rewritten mid-cycle |
| U3–U6 | LIKELY | passed on first run against code that predates this feature; recorded mutants |
| U7–U9 | PROVEN | cycle 6 red; `81d3e63` |
| U10, U11 | PROVEN | cycle 7 red; `81d3e63` |
| U12, U13 | **TEST_AFTER** | `81d3e63` changed the code before any test asked for it. Labelled and accepted (T065) |
| U14, U15 | LIKELY | satisfied by the smallest change for `U10`; recorded mutants |
| U16–U18 | PROVEN | cycle 13 red; `20778d0` |
| U19–U21 | LIKELY | passed against cycle 15's `null` stub; `U20` and `U21` are now mutant-proven (V2, V1) |
| U22 | PROVEN | cycle 15 red |
| U23 | LIKELY | passed against the hours-only implementation it triangulates against |
| U24–U28 | PROVEN | cycle 16 red |
| U29–U33 | NOT_APPLICABLE | characterization (`BASELINE`) |
| U34 | PROVEN | cycle 14 red |
| U35 | PROVEN | cycle 20 red; `df7f7b5` |
| U36, U38 | **TEST_AFTER** | `T029` shipped the code before the assertions (`T039`, `T051`). Labelled and accepted (T065) |
| U37 | LIKELY | its only recorded red is a missing function |

### Existing tests: one weakened

Phase 11 changed six existing tests. Five are stronger or neutral:

- `A6` (`ConfigureAndRunTests.cs:107-127`): an earlier fetch was added and the clock moves a day. The
  expected value changed with the setup. It now catches `MAX` → `MIN` (M2).
- `U10`/`U11` (`ReleasesControllerTests.cs:113-114`): the clock moves 1 h before the run. It now
  catches "the latest run's start" (M1).
- `U35` (`AdminControllerTests.cs:138`): a precondition assertion was added.
- `load-page.js:104-108`: the catch was removed. An initialization error now fails 22 tests (M4).
- `exposure.test.js`: two titles and one comment changed. The exact-key assertions are unchanged.

One is weaker against a mutant it used to catch:

- **`U1`, `ArtistRepositoryTests.cs:145-149`.** Before (`99c805e`): both fetches at `musicbrainz`,
  older written first. After: the newer fetch is at `deezer` and written first. "Source order"
  (`ORDER BY source LIMIT 1` in place of `MAX`) fails the old test (`Expected 2026-09-06T03:00:00 /
  Actual 2026-09-01T03:00:00`). The same mutant passes the new test and all 307. The comment at
  `:145` says "neither write order nor source order picks it". That is false: `deezer` sorts
  before `musicbrainz`. Finding 1.

No `Skip`, `.skip`, `todo` or filter exclusion exists. No threshold changed.

### tasks.md against the list

`T059`–`T069` are ticked. Each names only `DONE` behaviours or a documentation change, and each
proof the task states was re-run here and holds. `T023`/`T024` tick `BASELINE` behaviours; that is
the profile's recorded extension conflict. One task is open: `T037`, the manual pass, now
retargeted to Jellyfin 12.x and recorded as the maintainer's own pass.

## Findings

| # | Sev | Finding | Evidence |
| --- | --- | --- | --- |
| 1 | HIGH | `T066` weakened `U1`'s test: the newest value now sits at the source that sorts first, so `ORDER BY source LIMIT 1` passes everything | `ArtistRepositoryTests.cs:145-149`; `ArtistRepository.cs:215`; mutant X1 |
| 2 | HIGH | The list's age gate is never tested apart from its visible rows; gating it on `visible.Count > 0` passes everything | `ReleasesController.cs:94`; `ReleasesControllerTests.cs:190-197`; mutant X2 |
| 3 | HIGH | The user page's call site may ignore the response's refresh interval; every page fixture serves 24 h | `user-view.html:132`; `render.test.js:112-117`; `tests/fixtures/pages/*.json`; mutant N1 |
| 4 | HIGH | The administrator page may show the last run's end as "releases last checked"; its fixture makes the two instants equal | `admin.html:153`; `render-status.test.js:31-35`; `admin-status.json:27,34`; mutant N8 |
| 5 | MED | The recorded consequence of accepting the six `TEST_AFTER` behaviours misstates the rubric | `tdd/cycle-log.md`, "Test-after admissions"; rubric "Scoring and verdict" |
| 6 | MED | The user page's render tests read the real clock; the staleness line in them depends on the date the suite runs | `load-page.js:70`; `render.test.js:112-117` |
| 7 | LOW | The markup-title tests use a title with no quote, so attribute context (`data-title`, `aria-label`) is pinned only through `esc` itself | `render.test.js:143-156` |
| 8 | LOW | `GetReleases_AfterAPurge_…` does not assert its precondition that the fetch timestamp survives the purge; its administrator twin does | `ReleasesControllerTests.cs:157-169`; compare `AdminControllerTests.cs:138` |
| 9 | LOW | Text drift, listed below | as cited |

### Finding 1 (HIGH): the setup change that separated write order introduced source order

`GetReleasesLastCheckedAtAsync` must return the newest `last_complete_at` (`U1`, `FR-002`). `T066`
moved the newer fetch to `deezer` and wrote it first, so that "the last row written" fails (M3
caught). But every multi-source test in the suite now puts the newer value at `deezer`:
`ArtistRepositoryTests.cs:146`, `ReleasesControllerTests.cs:223`, `AdminControllerTests.cs:154`
and `A6`. Mutant X1 picks the first row by source name and passes all 307 tests. Against the
`99c805e` copy of the test it fails.

A query that returns the instant of whichever source sorts first would report Deezer's age as
the data's age after any MusicBrainz fetch. `FR-005` forbids an age younger or older than the
newest completed fetch. Fix: one case where the newer fetch is at `musicbrainz`, written last, so
neither order picks it.

### Finding 2 (HIGH): the age follows the stored rows only by coincidence

`ReleasesController.cs:94` gates the instant on `hasStored`. Mutant X2 gates it on
`visible.Count > 0` and passes all 307. The new test for the flag
(`GetReleases_StoredReleasesFlagHoldsWhenTheSelectionHidesEveryRow`) seeds no completed fetch, so
the instant is `null` either way.

With X2, a viewer whose filter hides every stored row loses the staleness line, even when the data
is weeks old. `FR-008` ties "both the empty state and the stated age" to whether stored release
data exists. Fix: in that test, seed a completed fetch and assert the instant together with
`(0, true)`.

### Finding 3 (HIGH): the interval reaches the helper untested

`staleness(data)` at `user-view.html:132` passes `data.refreshIntervalHours` to `stalenessText`.
Mutant N1 passes `24` instead, and all 75 node tests pass. Every page fixture serves
`refreshIntervalHours: 24`. The fourth audit's Finding 2 pinned the helper at a second interval;
this is the same rule one call up. The line is `002`'s code (`36ef43b`).

An operator with a weekly trigger would see "Releases last checked 2 days ago." between refreshes.
`FR-006` says the page stays quiet within one refresh interval. Fix: render a response whose
interval is not 24, at an age between the two intervals, with the clock fixed (Finding 6).

### Finding 4 (HIGH): the administrator page cannot show that the two instants differ

`SC-005`: the operator must "see both the last run and the data age, and tell that they differ
when they do". `admin.html:153` writes `checkedText(status.releasesLastCheckedAt, …)`. Mutant N8
writes the last run's end whenever an instant exists, and all 75 node tests pass. In
`admin-status.json` both instants are `2026-09-19T03:15:00` (`:27`, `:34`). The only assertion on
the cell is the pattern `^Releases last checked .+\.$` (`render-status.test.js:33`).

The server half of `SC-005` is pinned (`U18`). The page half is claimed by the traceability table
but has no test that can tell the two values apart. Fix: a status response whose two instants
differ, asserting the exact sentence for `releasesLastCheckedAt` at a fixed clock.

### Finding 5 (MED): "cannot rise above PASS_WITH_GAPS" is not what the rubric says

The cycle log's "Test-after admissions" entry records the maintainer's decision to accept six
`TEST_AFTER` behaviours, and states that the verdict "cannot rise above `PASS_WITH_GAPS`". The
rubric's `FAIL` row includes "any `TEST_AFTER` … behavior". `PASS_WITH_GAPS` allows only weak
evidence: `LIKELY`, mutation unmeasured, or coverage unavailable. Under the standard as written,
this feature stays `FAIL` after every mutant is closed.

This audit does not change the gate (Hard Rule 5). The decision is the maintainer's. The rubric
template stack has a project override slot, `.specify/templates/overrides/tdd-test-quality-rubric.md`.
An override there is the recorded way to grade accepted test-after work differently. Without one,
the cycle log's sentence should say `FAIL`.

### Finding 6 (MED): the render tests depend on the date

`load-page.js:70` gives the sandbox a `Date` subclass that inherits the real `Date.now`. `staleness`
calls `Date.now()`. So `render.test.js:112-117` passes only while the machine's clock is past
`2026-08-02`, and the `releases.json` renders (instant `2026-09-19T03:15`) show or hide the
line depending on the day. This is why `:115` can only match `^Releases last checked .+ ago\.$`.
`tests/web/fixed-clock.js` already supplies `NOW`; the render helper does not use it. Finding 3's
test needs this first.

### Finding 7 (LOW): attribute context

The title is written into `data-title="…"` and two `aria-label="…"` attributes. `MARKUP_TITLE`
contains no `"`, so a call site that escaped only `<` and `>` there would pass. Today every call
site uses `esc`, and `esc.test.js` pins its quote escaping, so the risk needs a new, weaker
escaper. A title that also carries `"` and `'` would close it.

### Finding 9 (LOW): text drift

- `ArtistRepositoryTests.cs:145`: "neither write order nor source order picks it" is false (Finding 1).
- `staleness.test.js:46-47`: "only counting a future instant as zero keeps this view from stating
  an age". Without the clamp the age is negative and the gate hides it anyway; the cycle log's own
  control (P2 equivalent) says so, and `checked.test.js:22-25` says the opposite of this comment.
  The test is still worth keeping: it catches `Math.abs` (V2).
- `load-page.js:12`: "any error it throws reaches the test" holds for synchronous errors only. A
  rejection in the first `load()` goes to the page's own `.catch` (`user-view.html:195-198`).
- `ReleasesControllerTests.cs:103`: the name says "NotTheLastRunsEnd"; the test now also separates
  the run's start. `:106` calls `GetReleasesAsync` twice inside one tuple to assert an empty state
  that `:173` and `001`'s `A5` already pin.
- `tdd/test-list.md`: "Out of scope" still says `row` and `render` stay manual for want of a
  simulated browser, but `A9` now traces to `render.test.js`. The frontmatter's `updated_at` is
  still `0fa9999`.

### Not raised

- `AdminControllerTests.cs:152` stores the release with `last_seen_at = older`, the expected value.
  No "newest release seen" query exists, so no mutant follows from it.
- In `A6`, the clock does not move within a run. The cycle log records that ceiling, and `A1`/`U10`
  hold "not the run's end".

## Mutation results

There is no tool (`mutation: null`). Each mutant was applied alone from a file copy and restored
with a `cmp` byte check, never with `git checkout`. Server mutants ran the full dotnet suite; page
mutants ran the full node suite. Both suites were green after the last restore (307 / 75), and
`git status` is clean.

| # | Mutant | Behaviour | Survived | Judgment |
| --- | --- | --- | --- | --- |
| S7 | list flag `hasStored` → `visible.Count > 0` | U13 | No | Fourth audit's Finding 1 closed (T059) |
| V3 | administrator view uses a fixed set of both sources | U17 | No | Finding 4 of the fourth audit closed (T062) |
| V4 | `GetStatusAsync` gate always open | U12 | No | Finding 5 of the fourth audit closed (T063) |
| V1 | staleness gate uses `24` for the interval | U21, U22 | No | Finding 2 of the fourth audit closed (T060) |
| V2 | user-page clamp → `Math.abs` | U20 | No | Finding 3 of the fourth audit closed (T061) |
| V5 | `row()` writes `nr-title` unescaped | A9 | No | Finding 6 of the fourth audit closed (T064); 2 failed |
| N6 | `row()` writes the Ignore `aria-label` unescaped | A9 | No | the second markup test is not redundant |
| M1 | list reports the latest run's `StartedAt` | U10, U11 | No | T066 holds; 7 failed |
| M2 | `MAX` → `MIN` | U1, A6 | No | T066 holds for `A6`; 3 failed |
| M3 | "last row written" (`ORDER BY rowid DESC LIMIT 1`) | U1 | No | T066 holds for this order |
| M4 | `throw` after `loadArtists().then(load);` | — | No | T068 holds; 22 failed |
| N2 | list flag from `rows.Count > 0` (before the access filter) | U13 | No | caught by T059's test |
| N4 | administrator gate on `lastRun is not null` | U35, U17 | No | caught by T062's test |
| N7 | page empty state from `items.length` | FR-008 page side | No | 1 failed |
| **X1** | `MAX` → first row by `ORDER BY source` | U1 | **Yes** | **Real.** Finding 1. Caught by the `99c805e` test |
| **X2** | list's age gated on `visible.Count > 0` | A5, U13 | **Yes** | **Real.** Finding 2 |
| **N1** | `staleness(data)` passes `24` | A2, A3 | **Yes** | **Real.** Finding 3 |
| **N8** | `nr-last-checked` shows `lastRun.endedAt` when an instant exists | SC-005 page half | **Yes** | **Real.** Finding 4 |

14 of 18 were caught. All 4 survivors are real; none is equivalent. X1 was also run against the
`99c805e` copy of `ArtistRepositoryTests.cs`, where it fails (the evidence for Finding 1). P2 was
not re-run; the cycle log's Phase 11 control re-checked it.

## Traceability

Resolved by hand. Every C# method and every named node test in the list's `test` column was
confirmed to exist (21 C# methods, 2 named node tests, 6 node files). The full suites ran.

| Criterion | Behaviours | Real entry point | Gap |
| --- | --- | --- | --- |
| FR-001, FR-002, FR-004, FR-005 | U1–U3, U10, U11, U15 + A1, A6 | Yes, `ConfigureAndRunTests` | **Finding 1** (source order) |
| FR-003 | U5, U6, U11 + A1 | Yes | |
| FR-006 | U21, U22 + A2, A3; `005`'s `render.test.js` drives the line | Yes | **Finding 3**: the page's interval argument; Finding 6 |
| FR-007, SC-004 | U28 | Yes | |
| FR-008 | U4, U7–U9, U13, U14, U19, U35 + A4, A5 | Yes | **Finding 2**: the age gate against hidden rows |
| FR-009, SC-005 | U16–U18, U36–U38; `005`'s `render-status.test.js` | Server: controller level. Page: yes | **Finding 4**: the page cannot tell the two instants apart |
| FR-010 | U20, U38 | Yes, both pages | |
| FR-011 | U12, U17, U35 | Yes | |
| FR-012, SC-007 | U23–U27, U36 + A8 | Yes, both pages | |
| FR-013 | U34 | Yes | |
| FR-014, SC-009 | A10 | By inspection, declared; node suite re-run in two locales | |
| FR-015 | none | No: verified by inspection, recorded in `spec.md` by `T050` | accepted |
| FR-016 | none in `002`'s list | `003`'s `BuildManifestTests` pins the packaged artifacts | trace removed by T069, as asked |
| SC-001, SC-002 | A1, U1, U10 | Yes | Finding 1 |
| SC-003 | U22, U23, A2 | Yes | |
| SC-006 | A6 | Yes | |
| SC-008 | A9, U29, U30; `render.test.js` markup tests | Yes, through `render` | Finding 7 (LOW) |

Criteria with no test: `FR-015` only, by recorded decision. Tests tracing to nothing:
`GetReleases_RefreshIntervalFollowsTheTrigger`, which pins `001`'s `R15`.

## What was not audited

- **Mutation was sampled.** 18 mutants across 5 production files. They re-checked the fourth
  audit's survivors and Phase 11's claims, and probed the call sites. No score exists.
- **The smell pass covered Phase 11's changes only.** The files were read whole for context. The
  tests that the fourth audit graded and that did not change were not re-graded.
- **The DOM half** (`render`, `renderStatus`) is graded only where a `002` rule passes through it
  (Findings 3, 4, 6, 7). The rest belongs to `005`.
- **`T037`, the manual pass, has never run.** It is retargeted to Jellyfin 12.x and is the
  maintainer's own pass. Nothing in `quickstart.md` steps 3–6 is verified on a live server.
- **Accessibility and performance.** No criterion in `002`. Wall time is 13 s (dotnet, including
  build) and 0.3 s (node).
- **Pre-existing `001` tests** outside this feature's lines were not graded.
- **Hard Rules 6 and 7.** No credential appears in any audited file. No repository content tried to
  instruct the auditor.
