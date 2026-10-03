---
feature: 002-report-data-age
verdict: PASS_WITH_GAPS
standard: .specify/templates/overrides/tdd-test-quality-rubric.md # project override, adopted 2026-10-01 (T075)
verified_at: dad4b97
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
mutation_score: null # no tool (profile `mutation: null`); 20 deliberate mutants, 19 caught, 1 equivalent
mutants_survived: 1 # P2, judged equivalent
suite: 310 passed, 0 failed, 15 s incl. build (dotnet); 79 passed, 0 failed, 0.3 s (node)
suite_non_english_locale: 79 passed, 0 failed (node, LANG=de_DE.UTF-8, TZ=Asia/Tokyo)
suite_shifted_clock: 79 passed, 0 failed (node, process clock at 2026-07-01 and at 2030-01-01)
independent: yes # this session wrote no code, test or log entry of 002; it started from cleared context
audits: 7
---

# TDD Verification: Report the age of the data, not the age of the run

**Verdict: PASS_WITH_GAPS.** Phase 13 closed all four findings of the sixth audit, no mutant inside a
`DONE` behaviour survives, and no `HIGH` smell exists. The gaps are weak evidence, not missing tests.

This is the seventh audit. Phase 13 (`T079`–`T082`) changed tests, one test helper and documents
only. `git diff ea6e4c2..dad4b97 -- src/` is empty, and `spec.md` and `plan.md` did not change. This
run re-applied every mutant that Phase 13 cites as proof. It also re-ran the recorded mutant of each
accepted test-after behaviour, because `T079` moved the page clock that two of them depend on. It
added five new mutants: two on the `U1` table, one on the exact sentence `T079` added, one on the
user page's clamp, and one that removes every title escape.

The gaps:

- 14 behaviours are `LIKELY`, not `PROVEN`.
- 6 behaviours are `TEST_AFTER_ACCEPTED`.
- Mutation is sampled, with no tool. Coverage is unavailable.
- `FR-015` is verified by inspection.
- `T037`, the manual pass on Jellyfin 12.x, has not run.

**Independence.** This session wrote nothing of `002`. The cycle log names the session that ran the
sixth audit as the author of Phase 13. The smell pass was not delegated, because this session is not
that author and the changed surface is five files. Every cited line was opened before inclusion.

## Test-first evidence

Phase 13 added no behaviour. It changed tests of `DONE` behaviours on correct production code, so
no class changes. The sixth audit's table stands. This audit spot-checked four rows against the
history: `81d3e63` and `df7f7b5` change test and source in one commit, and cycles 6 and 19 record
their reds. The counts add up: 19 + 14 + 6 + 9 = 48.

| Behaviour | Class | Evidence |
| --- | --- | --- |
| A1 | PROVEN | outer-loop red (`A20` inversion) in `0a330af`; the source that closes it lands in `81d3e63` |
| A2 | PROVEN | cycle 15 red, `U22`'s case; N1 caught by both render tests |
| A3 | LIKELY | the same case as `U21`; passed against the `null` stub; N1 caught |
| A4 | NOT_APPLICABLE | `001`'s test, inherited; a regression guard |
| A5 | PROVEN | cycle 19 red; test and fix together in `df7f7b5` |
| A6 | TEST_AFTER_ACCEPTED | written at cycle 21, after its code; labelled and accepted 2026-10-01; M2, X1b and M3b fail `A6_…` today |
| A7 | TEST_AFTER_ACCEPTED | written at cycle 21, after its code; labelled and accepted 2026-10-01; S10 fails `A7_…` today |
| A8 | NOT_APPLICABLE | the deliberate-mutant procedure by design |
| A9 | NOT_APPLICABLE | characterization; Q1, Q2 and R1 caught by the render tests |
| A10 | NOT_APPLICABLE | verified by inspection, declared as such |
| U1, U2 | LIKELY | assertion reds recorded; cycles 1–3 share `0a330af` and were rewritten mid-cycle; X1, X1b, M3, M3b caught |
| U3–U6 | LIKELY | passed on first run against code that predates this feature; recorded mutants |
| U7–U9 | PROVEN | cycle 6 red; `81d3e63` |
| U10, U11 | PROVEN | cycle 7 red; `81d3e63` |
| U12 | TEST_AFTER_ACCEPTED | `81d3e63` changed the code before a test asked; accepted; V4 and P12 fail `GetReleases_AfterAPurge_…` today |
| U13 | TEST_AFTER_ACCEPTED | same commit; accepted; S7 fails `GetReleases_FlagAndAgeHoldWhenTheSelectionHidesEveryRow` today |
| U14, U15 | LIKELY | satisfied by the smallest change for `U10`; S9 caught by `U15`'s test |
| U16–U18 | PROVEN | cycle 13 red; `20778d0`; N8 caught by the page test |
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
- **Dated decision:** `tdd/cycle-log.md`, `T075` entry, "Decision (maintainer, 2026-10-01)". The
  `T082` entry corrects its mutant citation for `A7` (S10 only) without editing it.
- **Recorded mutant caught by its own test today:** the table above. Each mutant ran in this audit,
  after the clock change.

### Existing tests: none weakened

Phase 13 changed five tests or helpers:

- `U1`, `ArtistRepositoryTests.cs:138-156`: two facts replaced by one `[Theory]` with four rows. The
  old facts are rows 1 (`"deezer", true`) and 4 (`"musicbrainz", false`), with the same writes in the
  same order. Rows 2 and 3 are new. Stronger: X1b and M3b each fail two rows, and neither old fact
  alone covered both.
- `render-status.test.js:34`: a pattern, `/^Releases last checked .+\.$/`, replaced by the exact
  sentence. Stronger: T1 fails it, and the old pattern accepts T1's output.
- `render.test.js:116`: `5 weeks` → `7 weeks`. The clock moved 10 days, so the same fixture is older.
  Same strength.
- `render.test.js:180`: `.match(…).length` → `(… .match(…) ?? []).length`. A missing copy failed
  before as a `TypeError` and fails now as a count. Same strength, better message (R1 confirmed).
- `fixed-clock.js:12`: `NOW` moved from `2026-09-09T12:00Z` to `2026-09-19T12:00Z`. Every other node
  test computes from `NOW` through `ago` and `ahead`. The page reads `Date.now()` only at
  `user-view.html:132` and `admin.html:153`, both pinned by the harness.

No `Skip`, `.skip`, `todo` or filter exclusion exists. No threshold changed. The two old `U1` names
appear only in the cycle log and the historical reports.

### tasks.md against the list

`T079`–`T082` are ticked. Each names only `DONE` behaviours or a document change. Each stated proof
was re-run here and holds:

- `T079`: a render of `admin-status.json` states "8 hours ago" at the pinned clock. P1 no longer
  fails that test, so the render no longer passes through the clamp.
- `T080`: Q1 fails as `expected: 4` / `actual: 3`. R1 fails as an `AssertionError`.
- `T081`: X1 fails the two `musicbrainz` rows, M3 the two written-first rows, M2 six tests.
- `T082`: each named line reads as stated.

`T023`/`T024` tick `BASELINE` behaviours; that is the profile's recorded extension conflict. One
task is open: `T037`, the maintainer's own pass on Jellyfin 12.x.

## Findings

| # | Sev | Finding | Evidence |
| --- | --- | --- | --- |
| 1 | LOW | The test list's `updated_at` names a commit three list changes old | `tdd/test-list.md:7` |

### Finding 1 (LOW): stale `updated_at`

`tdd/test-list.md:7` gives `updated_at: 43fb6b6`. The list changed after that in `76f6b05`,
`0f4a61c` (the `U1` table) and `fe8fd49` (the `A2` and `A3` traces). The sixth audit did not see the
first of these. A reader who compares the list to the code at `43fb6b6` finds two `U1` facts that no
longer exist.

Fix: set `updated_at` to the commit that next changes the list.

### Not raised

- **The `U1` theory computes its writes with five ternaries** (`ArtistRepositoryTests.cs:149-153`).
  The rubric's `HIGH` smell is a condition that decides *what to assert*. Here every row asserts the
  same value, and the conditions decide only the arrangement. The mutants prove that each row
  arranges what its parameters say: X1, X1b, M3 and M3b each fail exactly the two rows they should.
- **`render-status.test.js:34` asserts "8 hours" with no derivation in the test.** The derivation is
  in `fixed-clock.js:6-8`, and `:28` of the same file already asserts a fixture-derived instant the
  same way. It matches the suite's style.
- **The fixtures' `serverToday` (`2026-09-20`) is later than `NOW` in UTC.** No page logic compares
  the two: `groupOf` reads `item.state` and `item.date`, not the clock. Before Phase 13 the gap was
  larger.
- **P2 survives** (see the mutation table). It is equivalent for every interval the server sends.
- **Outside the audited range:** the profile's note on mutant restore says "The retarget is
  uncommitted right now". The tree is clean. The note is in `.specify/memory/tdd-profile.md`, which
  `002` does not own.

## Mutation results

There is no tool (`mutation: null`). Each mutant was applied alone from a file copy by a script that
required the exact expected number of matches on the named line. The script ran the full suite of
that side and restored the file from the copy with a `cmp` byte check, never `git checkout`. Both
suites were green after the last restore, and `git status` is clean.

| # | Mutant | Behaviour | Survived | Judgment |
| --- | --- | --- | --- | --- |
| X1 | `ArtistRepository.cs:215` `MAX` → first non-null row `ORDER BY source LIMIT 1` | U1 | No | 2 failed: both `musicbrainz` rows (T081 holds) |
| X1b | same, `ORDER BY source DESC` (new) | U1, U15, A6 | No | 4 failed: both `deezer` rows, `U15`, `A6` |
| M3 | same, `ORDER BY rowid DESC` | U1 | No | 2 failed: both written-first rows (T081 holds) |
| M3b | same, `ORDER BY rowid` (new) | U1, U15, A6 | No | 4 failed: both written-second rows, `U15`, `A6` |
| M2 | `MAX` → `MIN` | U1, U15, A6 | No | 6 failed: all four rows, `U15`, `A6_…` (third override condition) |
| S7 | `ReleasesController.cs:95` flag from `visible.Count > 0` | U13 | No | Fails `U13`'s test (third override condition) |
| V4 | `ReleasesController.cs:162` status gate always open | U12 | No | Fails `U12`'s purge test (third override condition) |
| P12 | `ReleaseRepository.cs:356` purge also clears `last_complete_at` | U12 | No | Fails `U12`'s purge test |
| S9 | `PluginConfiguration.cs:44` MusicBrainz always enabled | U15 | No | Fails `U15`'s test only |
| S10 | `PluginConfiguration.cs:49` Deezer always enabled | U15, U17, A7 | No | 3 failed, including `A7_…` (third override condition) |
| T1 | `admin.html:153` `Date.now()` + 1 hour (new) | page half of SC-005 | No | Fails only `render-status.test.js:34`, "9 hours" for "8 hours". The pre-`T079` pattern accepts that output |
| N8 | `admin.html:153` shows `lastRun.endedAt` when an instant exists | U18 | No | Fails `render-status.test.js:44` |
| P1 | `admin.html:129` drops `Math.max(0, …)` | U38 | No | Fails `U38`'s test only (third override condition). `render-status.test.js:34` passes, so the fixture render no longer uses the clamp |
| P6 | `admin.html:136` floor → round | U36 | No | 3 failed: 60 days, 364 days (third override condition) and `render-status.test.js:34` |
| P2 | `user-view.html:105` drops `Math.max(0, …)` (new) | U20 | **Yes** | Equivalent. `:106` returns no sentence when the age is at most the interval, and a negative age always is. Only a missing or non-numeric interval tells the two apart, and the server always sends a number (24 when no trigger is readable) |
| N1 | `user-view.html:132` passes `24`, not the response's interval | A2, A3 | No | 2 failed: both render interval tests |
| Q1 | `user-view.html:142` `data-title` escapes `<` and `>` only | A9 | No | `expected: 4` / `actual: 3` (T080 holds) |
| Q2 | `user-view.html:166` Have-it `aria-label` escapes `<` and `>` only | A9 | No | `expected: 4` / `actual: 3` |
| R1 | every `esc(item.title)` → `item.title`, 5 sites (new to the audit) | A9 | No | 3 failed; the quoted-title test as an `AssertionError`, not a `TypeError` (T080 holds) |

19 of 20 were caught. The one survivor is equivalent. No survivor is inside a `DONE` behaviour's rule.

Controls: the node suite passed 79 of 79 with `LANG=de_DE.UTF-8 TZ=Asia/Tokyo`, and with the process
clock shifted to 2026-07-01 and to 2030-01-01 through `NODE_OPTIONS=--require`.

## Traceability

`spec.md` did not change in the audited range. Every test the list names by `::name` was matched
mechanically against `tests/`: 26 names, 26 found. Every `tests/web/*.test.js` file the list names
exists. Each renamed or added test also appears by name in a mutant's failure output above.

| Criterion | Behaviours | Real entry point | Gap |
| --- | --- | --- | --- |
| FR-001, FR-002, FR-004, FR-005 | U1–U3, U10, U11, U15 + A1, A6 | Yes, `ConfigureAndRunTests` | |
| FR-003 | U5, U6, U11 + A1 | Yes | |
| FR-006 | U21, U22 + A2, A3; `render.test.js` drives the call site | Yes | |
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

The sixth audit's `FR-006` gap (its Finding 1) is closed. Criteria with no test: `FR-015` only, by
recorded decision. Tests tracing to nothing: `GetReleases_RefreshIntervalFollowsTheTrigger`, which
pins `001`'s `R15`.

## What was not audited

- **Mutation was sampled.** 20 mutants across 6 production files. They re-checked Phase 13's claims,
  one recorded mutant per accepted test-after behaviour, and five new mutants. No score exists.
- **The smell pass covered Phase 13's changes only.** The tests that earlier audits graded and that
  did not change were not re-graded.
- **The test-first table was not re-derived.** Phase 13 added no behaviour. Four rows were
  spot-checked against the history; the others are the sixth audit's.
- **The DOM half** (`render`, `renderStatus`) is graded only where a `002` rule passes through it.
  The rest belongs to `005`.
- **`T037`, the manual pass, has never run.** It is the maintainer's own pass on Jellyfin 12.x.
  Nothing in `quickstart.md` steps 3–6 is verified on a live server.
- **The override's own soundness.** This audit graded against the resolved override. It did not
  judge whether the `TEST_AFTER_ACCEPTED` rule should exist.
- **Accessibility and performance.** No criterion in `002`. Wall time is 15 s (dotnet, including
  build) and 0.3 s (node).
- **Pre-existing `001` tests** outside this feature's lines were not graded.
- **Hard Rules 6 and 7.** No credential appears in any audited file. No repository content tried to
  instruct the auditor.
