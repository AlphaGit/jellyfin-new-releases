---
feature: 007-user-view-polish
verdict: FAIL
standard: .specify/templates/overrides/tdd-test-quality-rubric.md # project override of the extension rubric (TEST_AFTER_ACCEPTED row)
profile: .specify/memory/tdd-profile.md
verified_at: 8e7daf7
previous_audit: f68901c (FAIL)
behaviors: 50 # 84 on the list, 34 DROPPED
proven: 34
likely: 3
test_after: 0
test_after_accepted: 13
no_test: 0
dropped: 34
high_smells: 4
criteria_total: 13
criteria_covered: 13 # US4-AS2 and US2-AS3 with gaps, see Findings 1–4
mutation_score: unmeasured # profile records mutation: null
deliberate_mutants: 58 applied; 34 caught; 22 survived inside a behaviour (16 outside the recorded ceiling, 6 inside it); 2 controls (E1 passes as it should, K1 fails a correct stylesheet) # scope: user-view.html, ReleasesController.cs, styles.test.js
suite: 318 passed, 0 failed, 11 s (dotnet) + 247 passed, 0 failed, 0.2 s (node; also under LANG=de_DE.UTF-8)
independent: no # this session wrote cycles 59–63; the smell pass and the new mutants came from fresh-context subagents and were re-verified here
---

# TDD Verification: Polish the New Releases view

**Verdict: FAIL.** 16 deliberate mutants that no recorded ceiling covers survive inside `DONE`
behaviours. An example: `#nr-user-view .nr-links a { all: unset; }` removes the source link's
underline and focus outline, and every test stays green.

This is the fourth audit. The third one (`f68901c`) gave FAIL with four `HIGH` findings. The
remediation closed all of them:

- Q1–Q9 now fail their tests.
- `A18` was added for Q6 and Q7.
- The maintainer accepted `A18` as test-after on 2026-10-03.
- CI is green on `main` at `8875456` (T050).

Every test-first class now holds. The verdict fails on test strength only.

The pattern is the one that the third audit's Finding 6 described, and it is stronger now. Each
remediation adds table rows for the previous audit's mutants. A fresh probe then finds new CSS that
the predicates misread:

| Audit | New survivors |
| --- | --- |
| Second | 5 |
| Third | 7 |
| Fourth (this one) | 16 outside the recorded ceiling, and 6 inside it |

This audit sampled wider than the third. It also probed `U60`, `U61`, `U66` and `A11`, which read
one exact selector each.

## Test-first evidence

Commit convention: each cycle's test and source land in one commit. Cycles 2–58 were checked by the
earlier audits. This audit re-read cycles 59–63, the refactor entry, the ceiling entry, the commit
map and the decision on `A18`. It also read the full diff `f68901c..8e7daf7`. That range changes no
file under `src/`. Each cycle commit (`d644d7a`, `8e41243`, `eb816a1`, `719eaa2`, `940663c`) changes
only `tests/web/styles.test.js` and the feature's documents. The refactor commit (`37e24f5`) changes
only the test file. Each cycle records a red on its helper table, and the diff explains each one. The
commit map in the cycle log matches the history.

| Behavior | Class | Evidence |
| --- | --- | --- |
| A1 | PROVEN | cycle 2 red; `cd52744` |
| A2 | PROVEN | cycle 3 red; `3791558` |
| A3 | TEST_AFTER_ACCEPTED | accepted 2026-10-03; R1, R2 caught today |
| A4 | PROVEN | cycle 49 red on 4 rows; `64ff918` |
| A5 | TEST_AFTER_ACCEPTED | accepted 2026-10-03; R4, R5 caught today |
| A7 | LIKELY | cycle 14 red; test committed in `9947c90`, after its source |
| A8 | LIKELY | cycle 23 red in an untracked file; committed in `41cec3b`, after `98ae035` |
| A9 | LIKELY | as A8 |
| A10 | PROVEN | cycle 23 red, re-observed in cycle 33; `319d07c` |
| A11 | PROVEN | cycle 42 red; `7df504a`. X3 survives (Finding 1) |
| A12 | TEST_AFTER_ACCEPTED | accepted 2026-10-03; R11 caught today |
| A13 | PROVEN | cycle 47 red; `084d299` |
| A14 | TEST_AFTER_ACCEPTED | accepted 2026-10-03; R13a, R13b, N9, P1, P3, Q1, Q3, Q4, Q5, Q9 caught today. S2–S6, M1, M2 survive (Findings 1–3); C2, C3, C5 survive inside the ceiling (Finding 4) |
| A15 | PROVEN | cycle 51 red, 4 rows; `3e4bc76` |
| A16 | TEST_AFTER_ACCEPTED | accepted 2026-10-03; N10, N8, P2, Q2 caught today. S1 survives (Finding 2) |
| A17 | TEST_AFTER_ACCEPTED | accepted 2026-10-03; P9, P10 caught today. M4, S9 survive (Findings 2, 3) |
| A18 | TEST_AFTER_ACCEPTED | labelled in cycle 63; accepted 2026-10-03 (`8875456`); Q6, Q7 caught today. S8, S9 survive (Findings 2, 3); C1, C4, S7 survive inside the ceiling (Finding 4) |
| U31 | PROVEN | cycle 15 red; `7da7bce` |
| U32 | PROVEN | cycle 17 red; `bc52ad7` |
| U33 | TEST_AFTER_ACCEPTED | accepted 2026-10-03; R9 caught today |
| U34 | TEST_AFTER_ACCEPTED | accepted 2026-10-03; R10 caught today |
| U35 | PROVEN | cycle 20 red; `2ed2f69` |
| U36 | TEST_AFTER_ACCEPTED | accepted 2026-10-03; R11b caught today |
| U37 | PROVEN | cycle 16 red; `6d6d8fa` |
| U38 | PROVEN | cycle 24 red; `8a26cf0` |
| U39 | PROVEN | cycle 25 red; `14a92eb` |
| U42 | PROVEN | cycle 8 red; baseline changed with a red in cycle 51; `3e4bc76` |
| U44 | PROVEN | cycle 9 red; `f146cb6` |
| U45 | PROVEN | cycle 50 red; `6495435`; R6 caught today |
| U46 | PROVEN | cycle 11 red; `e57fa95` |
| U47 | PROVEN | cycle 7 red; `5dcac35` |
| U48–U54 | PROVEN | cycles 26–32, one red each |
| U55 | PROVEN | cycle 35 red; `33a7bc0` |
| U56 | PROVEN | cycle 36 red; `d11940b` |
| U57 | PROVEN | cycle 37 red; corrected with a red in cycle 56; E2 caught, E1 passes |
| U58 | PROVEN | cycle 34 red; `d0ef65f` |
| U59 | PROVEN | cycle 44 red; `2962937` |
| U60 | PROVEN | cycle 39 red; helper corrected with reds in cycles 53, 57 and 62. S10, M3 survive (Findings 1, 2) |
| U61 | PROVEN | cycle 40 red; `0d75bc8`. X2 survives (Finding 1) |
| U62 | TEST_AFTER_ACCEPTED | accepted 2026-10-03; R12 caught today |
| U63 | PROVEN | cycle 45 red; `d48a008` |
| U64 | TEST_AFTER_ACCEPTED | accepted 2026-10-03; R7 caught today |
| U65 | TEST_AFTER_ACCEPTED | accepted 2026-10-03; R8 caught today |
| U66 | PROVEN | cycle 41 red; `629dd0e`. X1 survives (Finding 1) |
| A6, U1–U30, U40, U41, U43 | DROPPED | removed by the 2026-10-03 clarification |

**`TEST_AFTER_ACCEPTED` conditions.** All three conditions hold for each of the thirteen. A cycle-log
entry labels each one test-after with its evidence. A cycle-log entry records a dated decision to
accept it. A recorded mutant fails its test today. The attribution of each decision to the
maintainer rests on the log's own statement, because every commit has the same author.

**Existing tests changed since `f68901c`.**

| Test | Before | After | Judgment |
| --- | --- | --- | --- |
| `styles.test.js:373-377` A14 focus | `:focus-visible` outline matches `solid` | the same check, plus no rule that reaches a source link removes the outline | Stronger. Q4 caught |
| `styles.test.js:178-195` `reachesSourceLink` | every compound before the subject read as an ancestor | compounds before `~` or `+` read as siblings | Wider acceptance; no row lost. Q1–Q3, Q9 caught |
| `styles.test.js:201-205` `removesUnderline` | `none` on the shorthand or the line longhand | also `transparent` and a zero length, on four properties | Stronger. Q5 caught. False failure on a colour function with a zero channel (Finding 5) |
| `styles.test.js:43-58` `isVisibleColour` | any multi-token value accepted | exactly one colour token must remain | Stronger. Q8 caught |

No test was skipped, renamed out of a filter, or excluded. No row of an earlier table was removed or
inverted.

**`tasks.md` against the list.** Every ticked task's ids are `DONE`. `T063` is ticked and drove `A18`,
but it carries no `[A18]` (Finding 7), which is the gap that `T064` fixed for `T044` and `T055`.
`T026` and `T050` now hold: `main` is at `8e7daf7` and CI run `37168054919` passed.

## Findings

| # | Severity | Finding | Evidence |
| --- | --- | --- | --- |
| 1 | HIGH | **Five tests read one exact selector or one exact property.** `declarations(selector)[property]` sees only rules whose selector is exactly `#nr-user-view <selector>`. A more specific rule, a shorthand over the longhand, or a longhand over the shorthand changes the computed value unseen. Survivors: S2 `.nr-links a[href]:visited { color: #c58af9 }` (A14 visited colour), S10 `.nr-cover { background-color: transparent }` (U60 reads only `background`), X1 `.nr-row { grid-template: auto / 1fr 64px auto }` (U66), X2 `.nr-row .nr-cover img { object-fit: fill }` (U61), X3 `.nr-row .nr-actions button { width: auto }` (A11). U59 has the same form. These tests date from cycles 39–48. The third audit did not probe them. **Should assert**: every rule that reaches the element, each property through its shorthand and longhands. Or a closed-world check (Finding 5) | `tests/web/styles.test.js:96-113`, `:336-338` |
| 2 | HIGH | **The token predicates misread values that a reasonable author writes.** Survivors: S1 `color: #00a4dc66` (A16: `luminance` reads 6 hex digits and ignores the alpha; the real contrast is 1.98), S3 `all: unset` (A14: no predicate reads `all`), S5 `outline-color: #1c1c1c` and S6 `text-decoration-color: #1c1c1c` (A14: a colour equal to the card hides the line, but only `transparent` is read), M2 `outline: none!important` (A14: `removesOutline` tests `token === 'none'`, while `removesUnderline` accepts `none!`), M3 `background: transparent!important` (U60), M4 `opacity: 50% !important` (A17: `dimsText` has no table row for it and no ceiling comment), S8 `box-shadow: inset 0 0 0 2em #3a3a3a` (A18 reads only `background` and `background-color`) | `tests/web/styles.test.js:43-58`, `:138-148`, `:201-205`, `:268-272`, `:328-334`, `:348-352` |
| 3 | HIGH | **The selector reader misreads valid selectors.** Survivors: S4 `.nr-links a:not([download])` (an attribute inside `:not()` is read as one the link must have), M1 `.nr-links a[rel~="noopener"]` (`compounds` splits on `~` inside brackets), S9 `ARTICLE:hover { opacity: .6 }` (type selectors are compared with case). Checked in isolation: `.nr-links a:nth-child(2n+1)` and `.nr-row div:not(.nr-meta)` are also read as reaching nothing. The ceiling comment names only whitespace, commas and `:is()` | `tests/web/styles.test.js:174-195`, `:299-304` |
| 4 | HIGH | **Six mutants survive inside the recorded ceiling, and the rubric has no class for that.** C1 (a gradient with nested parentheses), C2 (`:is()`), C3 and C5 (a zero-alpha colour function), C4 (an attribute on the subject), and S7 (a background on the row, recorded in cycle 63). The maintainer's T062 decision sends this risk to `quickstart.md` §2 step 6. The rubric still fails a surviving mutant inside a `DONE` behaviour, as the third audit's Finding 6 said. **Should assert**: either the closed-world check of Finding 5, which catches added rules of any form, or a rubric override row for recorded-ceiling survivors, like the 2026-10-01 `TEST_AFTER_ACCEPTED` row | `tdd/cycle-log.md` T062 entry and cycle 63; `quickstart.md` §2 step 6 |
| 5 | MED | **The predicate approach does not converge.** The survivor count grows from audit to audit: 5, then 7, then 22. Each table row closes one input form, and CSS has more forms than the tables can list. The predicates also fail correct CSS: K1 (`outline: 2px solid rgb(82 181 0)` on `:focus-visible`) fails A14, because a whitespace token `0` inside a colour function reads as a zero width. `removesUnderline` does the same for `rgb(255 0 0)`. **Should assert** (maintainer's choice): a closed-world check. That is a reviewed, hand-written list of every rule in the `<style>` block (selector and declarations) that the test compares with the page. Any added or changed rule then fails until someone reviews it and updates the list. The existing predicates keep checking the listed values (contrast, visible colour). This catches every "add a rule" mutant of Findings 1–4 in one step. It needs no new tool | Findings 1–4; third audit Finding 6 |
| 6 | MED | **`dimsText` has no ceiling comment.** The T062 entry says that each predicate names the CSS it does not read. `dimsText` (used by A17 and A18) names none, and it misreads `opacity: 50% !important` (M4) and `filter: none!important` | `tests/web/styles.test.js:268-272`; `tdd/cycle-log.md` "Recorded ceiling and task links" |
| 7 | LOW | **`T063` is ticked with no behaviour id.** It drove `A18`. `T064` fixed this gap for `T044` and `T055` in the same remediation | `tasks.md:292` |

The smell pass reported one more item: assertion roulette in the A14 focus test (`:376`), which puts
two checks in one tuple. This audit did not take it. The tuple `deepEqual` is the house style
(`U60` `:99`, `A11` `:111`, `U59` `:129`, `A16` `:265`), and its diff shows which element broke.

**Suite properties.** Both suites are fast: node takes 0.2 s and dotnet takes 11 s. Both are
deterministic and isolated: the tests use no clock and no network, the locale is pinned, and
`STYLE` is read once. Each table row is its own named test. Refactor-insensitivity is weak, and
Findings 1–5 are the cause. The page tests read the stylesheet as text, not as computed style, so a
change in syntax alone can turn a test red (K1) or let a defect through (Findings 1–3).

## Mutation results

No mutation tool (profile: `mutation: null`). The scratch runner of the third audit applied 58
deliberate mutants, one at a time. It copied the file aside, applied literal replacements that each
had to match exactly once, ran the behaviour's test file or filter, and copied the file back. It then
checked each restore against `HEAD` with `git diff --quiet`. After the run, `git status` was clean,
and both full suites were green again (dotnet 318, node 247).

Sample:

- The 35 mutants of the third audit, re-run.
- S1–S10, X1–X3 and C1–C5, designed by a fresh-context subagent from the test file and the page.
- M1–M4, from the fresh-context smell pass.
- K1, a correct stylesheet as a control.

Each one was re-run here. Each added rule carries the `#nr-user-view ` prefix and sits before the
`@media` block.

| Mutant | Behavior | Survived | Judgment |
| --- | --- | --- | --- |
| R1, R2, R4, R5, R6, R7, R8, R11, R12, R13a, R13b, R9, R10, R11b | accepted test-after behaviours (condition 3) | No | Each fails its test |
| N6, N8, N9, N10, P1–P4, P9, P10, E2, Q1–Q9 | A13, A14, A16, A17, A18, U57, U60 | No | Earlier audits' mutants and survivors, all caught |
| E1 `{ capture: true }` | U57 (control) | Passes | Equivalent registration, so passing is correct |
| **K1** `:focus-visible { outline: 2px solid rgb(82 181 0) }` | A14 (control) | **Fails** | **False failure on correct CSS. Finding 5** |
| **S2** `.nr-links a[href]:visited { color: #c58af9 }` | A14 | **Yes** | **Real defect: visited colour differs. Finding 1** |
| **S10** `.nr-cover { background-color: transparent }` | U60 | **Yes** | **Real defect: transparent placeholder. Finding 1** |
| **X1** `.nr-row { grid-template: auto / 1fr 64px auto }` | U66 | **Yes** | **Real defect: the cover is not the first column. Finding 1** |
| **X2** `.nr-row .nr-cover img { object-fit: fill }` | U61 | **Yes** | **Real defect: the cover stretches. Finding 1** |
| **X3** `.nr-row .nr-actions button { width: auto }` | A11 | **Yes** | **Real defect: the buttons differ in width. Finding 1** |
| **S1** `.nr-links a:hover { color: #00a4dc66 }` | A16 | **Yes** | **Real defect: hover contrast 1.98. Finding 2** |
| **S3** `.nr-links a { all: unset }` | A14 | **Yes** | **Real defect: no underline, no outline. Finding 2** |
| **S5** `.nr-links a:focus-visible { outline-color: #1c1c1c }` | A14 | **Yes** | **Real defect: invisible outline. Finding 2** |
| **S6** `.nr-links a:hover { text-decoration-color: #1c1c1c }` | A14 | **Yes** | **Real defect: invisible underline. Finding 2** |
| **M2** `.nr-links a:focus-visible { outline: none!important }` | A14 | **Yes** | **Real defect: no outline. Finding 2** |
| **M3** `.nr-cover` `background: transparent!important` | U60 | **Yes** | **Real defect: transparent placeholder. Finding 2** |
| **M4** `.nr-links a:hover { opacity: 50% !important }` | A17 | **Yes** | **Real defect: dimmed link. Finding 2** |
| **S8** `.nr-links a:hover { box-shadow: inset 0 0 0 2em #3a3a3a }` | A18 | **Yes** | **Real defect: a background in effect. Finding 2** |
| **S4** `.nr-links a:not([download]) { text-decoration-line: none }` | A14 | **Yes** | **Real defect: no underline. Finding 3** |
| **M1** `.nr-links a[rel~="noopener"] { text-decoration: none }` | A14 | **Yes** | **Real defect: no underline. Finding 3** |
| **S9** `ARTICLE:hover { opacity: .6 }` | A18 | **Yes** | **Real defect: dimmed row. Finding 3** |
| C1 gradient background on the link | A18 | Yes | Inside the recorded ceiling. Finding 4 |
| C2 `:is(.nr-links, .nr-artist) a { text-decoration: none }` | A14 | Yes | Inside the recorded ceiling. Finding 4 |
| C3 `text-decoration-color: rgba(0,0,0,0)` | A14 | Yes | Inside the recorded ceiling. Finding 4 |
| C4 `.nr-row[data-id]:hover { opacity: .6 }` | A18 | Yes | Inside the recorded ceiling. Finding 4 |
| C5 `outline-color: rgba(0,0,0,0)` | A14 | Yes | Inside the recorded ceiling. Finding 4 |
| S7 `.nr-row:hover { background: #0e7fa8 }` | A16, A18 | Yes | Inside the ceiling recorded in cycle 63 (contrast 1.59). Finding 4 |

Of the 58 mutants, 34 were caught and 22 survived inside a behaviour: 16 outside the recorded
ceiling, and 6 inside it. E1 passed, as it should. K1 failed, which it should not.

## Traceability

| Criterion | Tests | End to end |
| --- | --- | --- |
| US1-AS1 (FR-001) | A1, U44 | Yes: real page script through `load-page.js` |
| US1-AS2 (FR-002, FR-005) | A2, A15, U42, U46, U65 | Yes |
| US1-AS3 (FR-003) | A3 (two tests) | Yes |
| US1-AS4 (FR-002) | A4 (6 rows, typed and left), U64 | Yes |
| US1-AS5 (FR-004) | A5 (two tests) | Partly: keyboard operation is the browser's; the markup test reads source text, which the profile permits |
| US2-AS1 (FR-006, FR-006a) | A7, U31–U37, U48, U52, U66 | Yes: `AcceptanceRig` refresh → `GET Releases`, joined to the page by `releases.json`. U66's layout check has a gap (Finding 1) |
| US2-AS2 (FR-006a) | A8, U49, U55, U57 | Yes, within the fake DOM's limits |
| US2-AS3 (FR-007) | A9, U53, U56, U60 | Yes; placeholder visibility has gaps (Findings 1, 2) |
| US2-AS4 | A10 | Yes |
| US3-AS1 (FR-009, SC-003) | A11, U59 | Declarations only; a more specific rule escapes (Finding 1); pixel equality is the real-browser pass |
| US3-AS2 (FR-009) | A12 | Yes |
| US4-AS1 (FR-010, SC-004) | A13, U62, U63 | Declarations only; rendered contrast is the real-browser pass |
| US4-AS2 (FR-010) | A14, A16, A17, A18 | Partial: Findings 1–4 |

Functional requirements without an acceptance scenario:

- FR-007a: covered by U50 (`loading="lazy"`).
- FR-008: covered by U51 (`no-referrer`). U32–U35 assert URL strings only.
- FR-011: covered by U36 and the unchanged existing suite.
- FR-002's "MUST NOT show as an applied filter": the page has no applied-filter indicator.

Untested criteria: none. No test traces to nothing. The `A14`, `A17`, `A18`, `U57` and `U60` helper
rows pin the predicates that their behaviour tests use.

## What was not audited

- **Independence**: this session wrote cycles 59–63 and the refactor. A fresh-context subagent did
  the smell pass, and another one designed S1–S10, X1–X3 and C1–C5. This session re-checked every
  cited line and re-ran every mutant on the real tree. The verdict, the severities and the rejection
  of one smell-pass item were decided here. Both subagents are the same model family.
- **Mutation score**: the profile has no mutation tool. 58 deliberate mutants are a sample, aimed at
  the stylesheet tests. The artist-filter, render and cover-fallback tests were sampled only through
  their recorded mutants.
- **Mutants P5–P8 and the C# mutants N11 and N12**: their code and tests have not changed since the
  second audit, so they were not re-run.
- **Coverage**: the page runs in a `node:vm` sandbox, which `--experimental-test-coverage` does not
  instrument. Dotnet coverage is unavailable (`coverage: null`).
- **Cycles 2–58**: the earlier audits checked these against history. This audit did not repeat that
  check.
- **Real-browser behaviour**: datalist matching, keyboard operation, lazy-load timing, pixel widths,
  rendered contrast, the `error` capture phase, and every CSS form in the recorded ceiling. These
  belong to the maintainer's pass in `quickstart.md` §2.
- **Case folding beyond ASCII**, **the card background `#1c1c1c`** against a running theme, and
  **performance** for more than 1,000 artists: no criterion or measurement, as in earlier audits.
