---
feature: 002-report-data-age
verdict: PASS_WITH_GAPS
standard: .specify/templates/overrides/tdd-test-quality-rubric.md # project override, adopted 2026-10-01 (T075)
verified_at: 76f6b05
behaviors: 48
proven: 19
likely: 14
test_after: 0
test_after_accepted: 6 # U12, U13, U36, U38, A6, A7; all three override conditions re-checked here
no_test: 0
not_applicable: 9
high_smells: 0
criteria_total: 25 # 16 FR, 9 SC
criteria_covered: 24 # FR-015 verified by inspection, by recorded decision
mutation_score: null # no tool (profile `mutation: null`); 15 deliberate mutants, 15 caught
mutants_survived: 0
suite: 308 passed, 0 failed, 16 s incl. build (dotnet); 79 passed, 0 failed, 0.3 s (node)
suite_non_english_locale: 79 passed, 0 failed (node, LANG=de_DE.UTF-8, TZ=Asia/Tokyo)
suite_shifted_clock: 79 passed, 0 failed (node, process clock at 2026-07-01 and at 2030-01-01)
independent: yes # this session wrote no code, test or log entry of 002; it started from cleared context
audits: 6
---

# TDD Verification: Report the age of the data, not the age of the run

**Verdict: PASS_WITH_GAPS.** Every mutant the fifth audit found surviving now fails, no new mutant
survives, and no `HIGH` smell remains. The gaps are weak evidence, not missing tests.

This is the sixth audit. Phase 12 (`T070`–`T078`) changed tests, test helpers and documents only;
`git diff 43fb6b6..76f6b05 -- src/` is empty. This run re-applied the four survivors of the fifth
audit (X1, X2, N1, N8). Each one now fails. It also re-ran the recorded mutant of each accepted
test-after behaviour, and each one fails in that behaviour's own test.

**The standard changed in the audited range.** `76f6b05` adds the project rubric override. It adds
the class `TEST_AFTER_ACCEPTED`, which `PASS_WITH_GAPS` admits. The fifth audit's Finding 5 named that
slot, and `T075` records the maintainer's dated decision. Under the extension's own rubric,
`.specify/extensions/tdd/templates/tdd-test-quality-rubric.md`, the same evidence is a `FAIL`. The
six behaviours are still test-after. The override changes their grade, not the facts.

The gaps:

- 14 behaviours are `LIKELY`, not `PROVEN`.
- 6 behaviours are `TEST_AFTER_ACCEPTED`.
- Mutation is sampled, with no tool. Coverage is unavailable.
- `FR-015` is verified by inspection.
- `T037`, the manual pass on Jellyfin 12.x, has not run.

**Independence.** This session wrote nothing of `002`. The smell pass was not delegated: this
session started from cleared context and is not the author of Phase 12, which the cycle log names as
the session that ran the fifth audit. Every cited line was opened before inclusion.

## Test-first evidence

Phase 12 added no behaviour. It added tests to `DONE` behaviours on correct production code, so each
new test passed on first run. The proof for each is a mutant that changed from survived to caught.
That does not change any class. The table is the fifth audit's, with the six test-after rows
re-graded against the override.

| Behaviour | Class | Evidence |
| --- | --- | --- |
| A1 | PROVEN | outer-loop red (`A20` inversion) in `0a330af`; the source that closes it lands in `81d3e63` |
| A2 | PROVEN | cycle 15 red, `U22`'s case; Phase 12 adds the call-site tests (N1 caught) |
| A3 | LIKELY | the same case as `U21`; passed against the `null` stub; N1 caught |
| A4 | NOT_APPLICABLE | `001`'s test, inherited; a regression guard |
| A5 | PROVEN | cycle 19 red; test and fix together in `df7f7b5`; X2 caught |
| A6 | TEST_AFTER_ACCEPTED | written at cycle 21, after its code; labelled and accepted 2026-10-01; M2 fails `A6_…` today |
| A7 | TEST_AFTER_ACCEPTED | written at cycle 21, after its code; labelled and accepted 2026-10-01; S10 fails `A7_…` today |
| A8 | NOT_APPLICABLE | the deliberate-mutant procedure by design |
| A9 | NOT_APPLICABLE | characterization; Q1 and Q2 caught by the quoted-title test |
| A10 | NOT_APPLICABLE | verified by inspection, declared as such |
| U1, U2 | LIKELY | assertion reds recorded; cycles 1–3 share `0a330af` and were rewritten mid-cycle; X1, M3 caught |
| U3–U6 | LIKELY | passed on first run against code that predates this feature; recorded mutants |
| U7–U9 | PROVEN | cycle 6 red; `81d3e63` |
| U10, U11 | PROVEN | cycle 7 red; `81d3e63` |
| U12 | TEST_AFTER_ACCEPTED | `81d3e63` changed the code before a test asked; accepted; V4 and P12 fail `GetReleases_AfterAPurge_…` today |
| U13 | TEST_AFTER_ACCEPTED | same commit; accepted; S7 fails `GetReleases_FlagAndAgeHoldWhenTheSelectionHidesEveryRow` today |
| U14, U15 | LIKELY | satisfied by the smallest change for `U10`; recorded mutants; S9 caught by `U15`'s test |
| U16–U18 | PROVEN | cycle 13 red; `20778d0`; N8 caught by the new page test |
| U19–U21 | LIKELY | passed against cycle 15's `null` stub |
| U22 | PROVEN | cycle 15 red |
| U23 | LIKELY | passed against the hours-only implementation it triangulates against |
| U24–U28 | PROVEN | cycle 16 red |
| U29–U33 | NOT_APPLICABLE | characterization (`BASELINE`) |
| U34 | PROVEN | cycle 14 red |
| U35 | PROVEN | cycle 20 red; `df7f7b5` |
| U36 | TEST_AFTER_ACCEPTED | `T029` shipped the ladder before its assertions; accepted; P6 fails `checked.test.js` (60 days, 364 days) today |
| U38 | TEST_AFTER_ACCEPTED | the clamp shipped with `T029`; accepted; P1 fails `checked.test.js::an instant later than now …` today |
| U37 | LIKELY | its only recorded red is a missing function |

The three override conditions, per accepted behaviour:

- **Labelled with evidence:** `tdd/cycle-log.md`, "Test-after admissions", names all six with the
  cycle and commit.
- **Dated decision:** the same entry, "Decision (maintainer, 2026-10-01): accepted".
- **Recorded mutant caught by its own test today:** the table above. Each mutant ran in this audit.

### Existing tests: none weakened

Phase 12 changed six existing tests or helpers:

- `U1`, `ArtistRepositoryTests.cs:139-150`: unchanged except its comment. The new sibling at `:153`
  covers source order. Fifth audit's Finding 1 closed (X1).
- `U13`, `ReleasesControllerTests.cs:190-199`: renamed to
  `GetReleases_FlagAndAgeHoldWhenTheSelectionHidesEveryRow`. It now seeds a fetch and asserts the
  instant as well. Stronger (X2).
- `U12`, `ReleasesControllerTests.cs:161`: a precondition assertion added. Stronger (P12).
- `render.test.js:116`: a pattern replaced by the exact sentence. Stronger.
- `load-page.js:74-81`: `Date.now()` pinned to `NOW`. Determinism fix. See Finding 1 for a side effect.
- **One assertion removed.** `U10`, `ReleasesControllerTests.cs:106` at `43fb6b6`:
  `Assert.Equal((false, null), (…HasStoredReleases, …ReleasesLastCheckedAt))` on an empty database,
  before any setup. After: the line is gone and the test is renamed
  `…NotTheLastRunsStartOrEnd`. The fifth audit's Finding 9 asked for this. The flag on an empty
  store is pinned at `ReleasesControllerTests.cs:178`. Both values on an empty store are pinned by
  `001`'s `BrowseReleasesTests.cs:115-121` (`A5`). The instant with nothing stored is pinned after a
  purge at `:167` (V4 caught). Judged a removed duplicate, not a weakening.

No `Skip`, `.skip`, `todo` or filter exclusion exists. No threshold changed. No old test name is
left in `tests/`, `specs/` or `docs/` outside the historical logs.

### tasks.md against the list

`T070`–`T078` are ticked. Each names only `DONE` behaviours or a document change. Each stated proof
was re-run here and holds, including `T074`'s shifted-clock claim: with the pre-Phase-12
`load-page.js` and the clock at 2026-07-01, 3 tests fail, as the cycle log says. With the committed
harness, 79 pass. `T023`/`T024` tick `BASELINE` behaviours; that is the profile's recorded extension
conflict. One task is open: `T037`, the maintainer's own pass on Jellyfin 12.x.

## Findings

| # | Sev | Finding | Evidence |
| --- | --- | --- | --- |
| 1 | MED | The pinned clock is earlier than every "fresh" page fixture, so each render of those fixtures takes the clock-correction path | `fixed-clock.js:10`; `load-page.js:75-77`; `admin-status.json:34`; `releases.json:61`; `status.json:3`; `render-status.test.js:31-35` |
| 2 | LOW | The quoted-title test fails with a `TypeError`, not a count, when no escaped copy is found | `render.test.js:180` |
| 3 | LOW | `U1`'s two cases copy the same setup with the two orders swapped | `ArtistRepositoryTests.cs:139-164` |
| 4 | LOW | Text drift, listed below | as cited |

### Finding 1 (MED): the fixed clock runs ten days behind the fixtures

`T074` pinned the sandbox's `Date.now()` to `NOW`, `2026-09-09T12:00Z`. The page fixtures
`releases.json`, `releases-filtered.json`, `admin-status.json` and `status.json` give
`releasesLastCheckedAt` as `2026-09-19T03:15Z`, 9.6 days later. So every render of those fixtures
is a clock-correction case. Today the administrator page renders `admin-status.json` as
"Releases last checked 0 hours ago.", and the user page hides the staleness line through the clamp,
not through the interval rule.

`render-status.test.js:31-35` ("after a completed refresh … is a sentence") still passes, but only on
that path. No `002` mutant follows from it today: the clamp (P1) is pinned by `checked.test.js`, and
the interval rule by the new render tests. The risk is the next test. An author who renders
`releases.json` to show "fresh data, no line" will prove the clamp, not the interval.

Fix: put `NOW` after the fixture instants, or move the fixture instants before `NOW`. Then assert the
exact sentence at `render-status.test.js:34`. Moving `NOW` changes the `releases-stale.json`
expectation at `render.test.js:116` (at `2026-09-19T12:00Z`, for example, 49 days is "7 weeks"), so that line changes in the same step.

### Finding 2 (LOW): failure message

`render.test.js:180` calls `.match(…).length`. When no escaped copy is in the row, `match` returns
`null` and the test fails with `Cannot read properties of null`. It still fails, so Q1 and Q2 were
caught, but the output does not say "expected 4, found 0". `(… .match(…) ?? []).length` keeps the
count in the message.

### Finding 3 (LOW): duplicated setup

`GetReleasesLastCheckedAtAsync_IsTheNewestCompletedFetchAcrossArtists` and
`…_EvenAtTheSourceThatSortsLast` build the same two artists and instants, with source and write
order swapped. One `[Theory]` with the newer fetch's source and write position as its two columns
states the rule ("neither order picks it") as a table.

### Finding 4 (LOW): text drift

- `tdd/cycle-log.md`, `T075` entry: it cites "S9 and S10" as `A7`'s recorded mutants. S9 does not fail
  `A7`'s test; only `U15`'s test catches it. S10 fails `A7`. The condition holds through S10.
- `tdd/test-list.md`, `A2` and `A3`: the trace says "the weekly and six-hour interval tests". The
  other node traces in the list use `file::name`.
- `load-page.js:78-79`: two blank lines inside the `Date` class.

### Not raised

- The quoted-title test pins `esc`'s entity spelling (`&#39;`). `esc.test.js` pins the same output,
  so an `esc` change already fails a test. This adds no coupling a reader would not expect.
- `S9` (MusicBrainz always enabled) is caught by `U15`'s test, not by `A7`'s. The acceptance test is
  not the only guard, and the gap is not inside a `002` rule that lacks one.

## Mutation results

There is no tool (`mutation: null`). Each mutant was applied alone from a file copy by a script that
required exactly one match, ran the full suite of its side, and was restored with a `cmp` byte check,
never `git checkout`. Both suites were green after the last restore, and `git status` is clean.

| # | Mutant | Behaviour | Survived | Judgment |
| --- | --- | --- | --- | --- |
| X1 | `MAX` → first non-null row `ORDER BY source LIMIT 1` | U1 | No | Fifth audit's Finding 1 closed (T070); fails the new source-order case |
| M3 | `MAX` → first non-null row `ORDER BY rowid DESC LIMIT 1` | U1 | No | Write order still pinned by the first case |
| M2 | `MAX` → `MIN` | U1, U15, A6 | No | 4 failed, including `A6_…` (third override condition) |
| X2 | list's age gated on `visible.Count > 0` | A5, U13 | No | Fifth audit's Finding 2 closed (T071) |
| S7 | list flag from `visible.Count > 0` | U13 | No | Fails `U13`'s test (third override condition) |
| V4 | `GetStatusAsync` gate always open | U12 | No | Fails `U12`'s purge test (third override condition) |
| P12 | `PurgeAsync` also clears `last_complete_at` | U12 | No | T077's precondition holds |
| S9 | MusicBrainz always enabled | U15 | No | Fails `U15`'s test only |
| S10 | Deezer always enabled | U15, U17, A7 | No | 3 failed, including `A7_…` (third override condition) |
| N1 | `staleness(data)` passes `24` | A2, A3 | No | Fifth audit's Finding 3 closed (T072); 2 failed |
| N8 | `nr-last-checked` shows `lastRun.endedAt` when an instant exists | U18 (page half of SC-005) | No | Fifth audit's Finding 4 closed (T073) |
| Q1 | `data-title` escapes `<` and `>` only | A9 | No | T076 holds |
| Q2 | the Have-it `aria-label` escapes `<` and `>` only | A9 | No | T076 covers a second attribute, not only the one it was proven on |
| P1 | `admin.html` drops `Math.max(0, …)` | U38 | No | Fails `U38`'s test (third override condition) |
| P6 | `admin.html` floor → round | U36 | No | 2 failed in `checked.test.js` (third override condition) |

15 of 15 were caught. Two controls ran for `T074`. The process clock was shifted to 2026-07-01 and to
2030-01-01 through `--require`. The preload reached all 11 node processes, and the suite stayed at
79 passed. With the `43fb6b6` copy of `load-page.js`, the 2026-07-01 run fails 3 tests.

## Traceability

Resolved by hand. Every renamed or added test in the list's `test` column was confirmed to exist and
run: each appears by name in a mutant's failure output above.

| Criterion | Behaviours | Real entry point | Gap |
| --- | --- | --- | --- |
| FR-001, FR-002, FR-004, FR-005 | U1–U3, U10, U11, U15 + A1, A6 | Yes, `ConfigureAndRunTests` | |
| FR-003 | U5, U6, U11 + A1 | Yes | |
| FR-006 | U21, U22 + A2, A3; `render.test.js` drives the call site | Yes | Finding 1 |
| FR-007, SC-004 | U28 | Yes | |
| FR-008 | U4, U7–U9, U13, U14, U19, U35 + A4, A5 | Yes | |
| FR-009, SC-005 | U16–U18, U36–U38; `render-status.test.js` | Server: controller level. Page: yes | |
| FR-010 | U20, U38 | Yes, both pages | |
| FR-011 | U12, U17, U35 | Yes | |
| FR-012, SC-007 | U23–U27, U36 + A8 | Yes, both pages | |
| FR-013 | U34 | Yes | |
| FR-014, SC-009 | A10 | By inspection, declared; node suite re-run in two locales and two clocks | |
| FR-015 | none | No: verified by inspection, recorded in `spec.md` by `T050`; `.github/workflows/build.yml:37` runs `node --test "tests/web/*.test.js"` | accepted |
| FR-016 | none in `002`'s list | `003`'s `BuildManifestTests` pins the packaged artifacts | |
| SC-001, SC-002 | A1, U1, U10 | Yes | |
| SC-003 | U22, U23, A2 | Yes | |
| SC-006 | A6 | Yes | |
| SC-008 | A9, U29, U30; `render.test.js` markup and quoted-title tests | Yes, through `render` | |

Criteria with no test: `FR-015` only, by recorded decision. Tests tracing to nothing:
`GetReleases_RefreshIntervalFollowsTheTrigger`, which pins `001`'s `R15`.

## What was not audited

- **Mutation was sampled.** 15 mutants across 6 production files. They re-checked the fifth audit's
  survivors, Phase 12's claims, and one recorded mutant per accepted test-after behaviour. No score
  exists.
- **The smell pass covered Phase 12's changes only.** The tests that earlier audits graded and that
  did not change were not re-graded.
- **The DOM half** (`render`, `renderStatus`) is graded only where a `002` rule passes through it.
  The rest belongs to `005`. Finding 1 touches `005`'s characterization tests too.
- **`T037`, the manual pass, has never run.** It is the maintainer's own pass on Jellyfin 12.x.
  Nothing in `quickstart.md` steps 3–6 is verified on a live server.
- **The override's own soundness.** This audit graded against the resolved override. It checked that
  the override differs from the extension's rubric only in the `TEST_AFTER_ACCEPTED` rule. It did not
  judge whether that rule should exist.
- **Accessibility and performance.** No criterion in `002`. Wall time is 16 s (dotnet, including
  build) and 0.3 s (node).
- **Pre-existing `001` tests** outside this feature's lines were not graded.
- **Hard Rules 6 and 7.** No credential appears in any audited file. No repository content tried to
  instruct the auditor.
