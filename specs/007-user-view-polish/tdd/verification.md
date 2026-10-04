---
feature: 007-user-view-polish
verdict: FAIL
standard: .specify/templates/overrides/tdd-test-quality-rubric.md # project override of the extension rubric (TEST_AFTER_ACCEPTED row)
profile: .specify/memory/tdd-profile.md
verified_at: f68901c
previous_audit: ad2b277 (FAIL)
behaviors: 49 # 83 on the list, 34 DROPPED
proven: 34
likely: 3
test_after: 0
test_after_accepted: 12
no_test: 0
dropped: 34
high_smells: 4
criteria_total: 13
criteria_covered: 13 # US4-AS2 only in part, see Finding 5
mutation_score: unmeasured # profile records mutation: null
deliberate_mutants: 35 applied, 26 caught, 7 survived inside a behaviour, 2 survived outside, E1 a passing control # scope: user-view.html, ReleasesController.cs, styles.test.js
suite: 318 passed, 0 failed, 10 s (dotnet) + 200 passed, 0 failed, 0.2 s (node; also under LANG=de_DE.UTF-8)
independent: fresh context # the auditing session did not write the tests; same model family
---

# TDD Verification: Polish the New Releases view

**Verdict: FAIL.** Seven deliberate mutants survive inside `DONE` behaviours. The most direct one is
`#nr-user-view .nr-links a:focus-visible { outline: none }`, which removes the source link's focus
outline while `A14`'s focus test stays green.

This is the third audit. The second one (`ad2b277`) gave FAIL with four `HIGH` findings. The
remediation closed all of them:

- P1, P2 and P3 now fail `A14` or `A16`.
- E2 (`once`) now fails `U57`.
- P4 (`background: initial`) now fails `U60`.
- The maintainer accepted `A16` and `A17` as test-after on 2026-10-03.
- P9 now fails the new `A17`.

The new findings are of one kind. Three test predicates read CSS by hand: `reachesSourceLink`,
`removesUnderline` and `isVisibleColour`. Each one passed its own table and still misses CSS that
breaks the behaviour. The `A14` focus test does not use the predicates at all. Finding 6 explains
why more table rows have not been enough, three audits in a row.

## Test-first evidence

Commit convention: each cycle's test and source land in one commit. Cycles 2–54 were checked by
the earlier audits. This audit re-read cycles 55–58, the refactor entry, the commit map and the
maintainer decision. It also read the full diff `ad2b277..f68901c`. That range changes no file under
`src/`. Each test commit (`0cbaa7d`, `ba64eed`, `a1e0b2f`, `739e6df`, `82c8ee9`) changes only test
files and the feature's documents, as its entry says. Each cycle entry records a red on the helper
table that its diff explains.

| Behavior | Class | Evidence |
| --- | --- | --- |
| A1 | PROVEN | cycle 2 red; `cd52744` |
| A2 | PROVEN | cycle 3 red; `3791558` |
| A3 | TEST_AFTER_ACCEPTED | accepted 2026-10-03 (first decision); R1, R2 caught today |
| A4 | PROVEN | cycle 49 red on 4 rows; `64ff918` |
| A5 | TEST_AFTER_ACCEPTED | accepted 2026-10-03; R4, R5 caught today |
| A7 | LIKELY | cycle 14 red; test committed in `9947c90`, after its source |
| A8 | LIKELY | cycle 23 red in an untracked file; committed in `41cec3b`, after `98ae035` |
| A9 | LIKELY | as A8 |
| A10 | PROVEN | cycle 23 red, re-observed in cycle 33; `319d07c` |
| A11 | PROVEN | cycle 42 red; `7df504a` |
| A12 | TEST_AFTER_ACCEPTED | accepted 2026-10-03; R11 caught today |
| A13 | PROVEN | cycle 47 red; `084d299` |
| A14 | TEST_AFTER_ACCEPTED | accepted 2026-10-03; R13a, R13b, N9, P1, P3 caught today. Q1, Q3, Q4, Q5, Q9 survive (Findings 1–3) |
| A15 | PROVEN | cycle 51 red, 4 rows; `3e4bc76` |
| A16 | TEST_AFTER_ACCEPTED | labelled in cycle 54; accepted 2026-10-03 (`cycle-log.md:897-907`, `f68901c`); N10, N8, P2 caught today. Q2 survives (Finding 2) |
| A17 | TEST_AFTER_ACCEPTED | labelled in cycle 58; accepted 2026-10-03 (same entry); P9, P10 caught today |
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
| U45 | PROVEN | cycle 50 red (the `constructor` row); `6495435`; R6 caught today |
| U46 | PROVEN | cycle 11 red; `e57fa95` |
| U47 | PROVEN | cycle 7 red; `5dcac35` |
| U48–U54 | PROVEN | cycles 26–32, one red each |
| U55 | PROVEN | cycle 35 red; `33a7bc0` |
| U56 | PROVEN | cycle 36 red; `d11940b` |
| U57 | PROVEN | cycle 37 red; `98ae035`. Test corrected with a red on its helper table in cycle 56 (`ba64eed`); E2 caught today |
| U58 | PROVEN | cycle 34 red; `d0ef65f` |
| U59 | PROVEN | cycle 44 red; `2962937` |
| U60 | PROVEN | cycle 39 red; helper corrected with reds in cycles 53 and 57 (`a1e0b2f`). Q8 survives (Finding 4) |
| U61 | PROVEN | cycle 40 red; `0d75bc8` |
| U62 | TEST_AFTER_ACCEPTED | accepted 2026-10-03; R12 caught today |
| U63 | PROVEN | cycle 45 red; `d48a008` |
| U64 | TEST_AFTER_ACCEPTED | accepted 2026-10-03; R7 caught today |
| U65 | TEST_AFTER_ACCEPTED | accepted 2026-10-03; R8 caught today |
| U66 | PROVEN | cycle 41 red; `629dd0e` |
| A6, U1–U30, U40, U41, U43 | DROPPED | removed by the 2026-10-03 clarification |

**`TEST_AFTER_ACCEPTED` conditions.** The cycle log meets all three conditions for each of the
twelve:

1. A cycle-log entry labels each one test-after and names its evidence.
2. A cycle-log entry records a dated decision to accept it.
3. This audit re-ran a recorded mutant for each one, and each mutant fails its test today.

The audit cannot tell the maintainer's commits from the loop's. Every commit has the same author,
so the attribution of both decisions rests on the log's own statement.

**Existing tests changed since `ad2b277`.**

| Test | Before | After | Judgment |
| --- | --- | --- | --- |
| `cover-fallback.test.js:68-72` U57 | options mapped through `capture` (any object with `capture: true`) | mapped through `capturesEveryError`, which rejects `once` and `signal`; 9-row table | Stronger. E2 caught, E1 still passes |
| `styles.test.js:76-79` U60 | `isVisibleColour` without CSS-wide keywords | rejects `initial`, `inherit`, `unset`, `revert`, `revert-layer` | Stronger, but a gap remains (Finding 4) |
| `styles.test.js:157-162` `reachesSourceLink` (A14, A16) | before the subject, only the class `.nr-links` was checked; ids were ignored | before the subject, every id and class must be in `LINK_ANCESTORS` | Stronger overall: P1–P3 caught. **Narrower in one place**: an id outside the list was accepted before and is rejected now, so Q9 (`#nr-filters ~ #nr-panel a`) passed the old predicate and survives the new one (Finding 2) |
| `styles.test.js:31-33` `declarations()` | its own parsing loop | merges the output of `rules()` | Refactor. Every earlier mutant still fails its test |

No test was skipped, renamed out of a filter, or excluded.

**`tasks.md` against the list.** Every ticked task's ids are `DONE`. Two ticked behavioural tasks
carry no behaviour id (Finding 7). `T026` stays ticked while `T050`, its check, is open (Finding 8).

## Findings

| # | Severity | Finding | Evidence |
| --- | --- | --- | --- |
| 1 | HIGH | **`A14`'s focus test reads one exact selector.** It checks only the rule `#nr-user-view :focus-visible`. Mutant Q4, `#nr-user-view .nr-links a:focus-visible { outline: none }`, removes the source link's focus outline, and every test stays green. This is the gap that N9 showed for the underline in the first audit. The underline test was then moved to `rules()` and `reachesSourceLink`, but the focus test was not. **Should assert**: no rule that reaches a source link removes its outline (`outline: none`, `outline: 0`, `outline-style: none`, `outline-width: 0`), through a predicate with its own table. Keep the present check on `:focus-visible` | `tests/web/styles.test.js:255-257` |
| 2 | HIGH | **`reachesSourceLink` treats every compound before the subject as an ancestor.** `~` and `+` select a sibling, not an ancestor. The source link's `<div>` has the siblings `.nr-title`, `.nr-artist`, `.nr-meta` and `.nr-cover`. Three mutants survive: Q1 `.nr-meta ~ .nr-links a { text-decoration: none }` (A14), Q2 `.nr-title ~ .nr-links a:hover { color: #0000ee }` (A16, 1.8:1), Q3 `.nr-cover + div a { text-decoration: none }` (A14). The closed `LINK_ANCESTORS` list also rejects an id that the old predicate accepted, so Q9 `#nr-filters ~ #nr-panel a { text-decoration: none }` passed the old predicate and survives the new one. The code comment names only `:is()` and quoted spaces as limits | `tests/web/styles.test.js:149-162`, table `:170-193` |
| 3 | HIGH | **`removesUnderline` accepts only `none`.** Q5, `#nr-user-view .nr-links a { text-decoration-color: transparent }`, keeps the underline but makes it invisible. `A14` stays green. A thickness of `0` is the same case | `tests/web/styles.test.js:166-168`, table `:199-208` |
| 4 | HIGH | **`isVisibleColour` accepts any value of more than one token.** Q8, `background: transparent none` on `.nr-cover`, matches no rejecting rule, so it returns `true` and `U60` stays green. The placeholder is transparent (FR-007, US2-AS3). The table holds only single-token values | `tests/web/styles.test.js:36-46`, table `:48-70` |
| 5 | MED | **US4-AS2 "keeps the same contrast" is still covered only in part.** Q6, `#nr-user-view .nr-links a:hover { background: #00a4dc }`, puts the link text on a background of its own colour (1:1). Q7, `#nr-user-view .nr-row:hover .nr-links { opacity: .3 }`, dims the link through an ancestor. Both survive. No behaviour on the list states them, so they are not survivors inside a behaviour. Cycle 58 records the ancestor-opacity limit, but only in the cycle log | `tests/web/styles.test.js:214-245`; `tdd/cycle-log.md:860-862` |
| 6 | MED | **The predicate approach has not converged.** Three audits each found CSS that the predicates miss, and each remediation added rows for the mutants of the audit before it: N9 → P1–P3 → Q1–Q3, Q9; N6 → P4 → Q8. The tables come from the mutants, not from the CSS that can reach the link. The profile's rule "a predicate needs a table" holds, but a table cannot list an open set. **Should assert** (maintainer's choice): either a closed-world check (the exact list of selectors in the stylesheet whose subject can be an anchor or a focus state, so that any new such rule fails until someone reviews it and adds it), or a recorded ceiling for the predicates with the remaining risk sent to the real-browser pass (`quickstart.md` §2). The second option does not change this verdict, because the rubric fails a surviving mutant inside a `DONE` behaviour | Findings 1–4; second audit Findings 1 and 4 |
| 7 | LOW | **`T044` and `T055` are ticked with no behaviour id.** The task format says that each behavioural task carries its ids, and `/speckit-tdd-run` ticks only through them. `T044` drove `A16` and `T055` drove `A17` | `tasks.md:248`, `:270` |
| 8 | LOW | **`T026` ("Commit to `main`") is ticked, but `main` does not hold the feature.** `main` is at `5846c02`. The 13 commits since then are on the branch `AlphaGit/tdd-run-red-green`. `T050` already tracks the CI check, so this audit adds no task. It reports the branch state for the maintainer | `tasks.md:187`, `:254`; `git branch --contains f68901c` |

**Suite properties.** Both suites are fast: node takes 0.2 s and dotnet takes 10 s. Both are
deterministic: the new tests use no clock, no network and no shared state, and the locale is pinned.
Each table row is its own `test()`, so a failure names the input that broke. `A16` guards against an
empty selection. `A14`'s underline test and `A17` do not, but the `reachesSourceLink` table pins the
predicate they depend on.

## Mutation results

No mutation tool (profile: `mutation: null`). A scratch runner applied 35 deliberate mutants, one at
a time. It copied the file aside, applied literal replacements that each had to match exactly once,
ran the behaviour's test file or filter, and copied the file back. It then checked each restore
against `HEAD` with `git diff --quiet`. After the run, `git status` was clean, and both full suites
were green again (dotnet 318, node 200).

Sample:

- R*: the recorded mutant of each accepted test-after behaviour, re-run for condition 3.
- N*, P*, E2: the earlier audits' mutants and survivors, re-run.
- E1: a control that must pass (`{ capture: true }` is equivalent to `true`).
- Q*: new probes on the remediation's predicates and on the tests that do not use them.

| Mutant | File | Behavior | Survived | Judgment |
| --- | --- | --- | --- | --- |
| R1 `query()` keeps the last applied id on no match | `user-view.html:89` | A3 | No | Both A3 tests fail |
| R2 Clear skips the Artist field | `user-view.html:262` | A3 | No | A3 Clear test fails |
| R4 `list` attribute removed | `user-view.html:44` | A5 | No | Markup test fails |
| R5 `keydown` listener added | `user-view.html:258` | A5 | No | Key-handling test fails |
| R11 Restore wrapped in `<span class="nr-restore">` | `user-view.html:202` | A12 | No | A12 fails |
| R13a `text-decoration: none` on `.nr-links a` | `user-view.html` (added rule) | A14 | No | Underline test fails |
| R13b `:focus-visible` `outline: none` | `user-view.html:29` | A14 | No | Focus test fails |
| R6 `.catch` sets the map to `null` | `user-view.html:244` | U45 | No | Both U45 rows fail |
| R7 reload whenever nothing is applied | `user-view.html:259` | U64 | No | 9 tests fail, A4 rows among them |
| R8 applied artist recorded in the handler | `user-view.html:89, 259` | U65 | No | U65 fails |
| R12 flare terms dropped from `contrast()` | `styles.test.js:128` | U62 | No | U62, U63 fail |
| R9 URL-format branch inverted | `ReleasesController.cs:228` | U33 | No | 3 cover tests fail |
| R10 `front-250` → `front-500` | `ReleasesController.cs:230` | U34 | No | 2 cover tests fail |
| R11b `Sources` ordered descending | `ReleasesController.cs:221` | U36 | No | U36 fails |
| N6 `.nr-cover` `background: none` | `user-view.html:15` | U60 | No | U60 fails |
| N8 link colour `#3a6ea5` | `user-view.html:25` | A13, A16 | No | A13, A16 fail |
| N9 `text-decoration-line: none` on `.nr-links a` | `user-view.html` (added rule) | A14 | No | Underline test fails |
| N10 `.nr-links a:hover { color: #0000ee }` | `user-view.html` (added rule) | A16 | No | A16 fails |
| P1 `.nr-row a { text-decoration-line: none }` | `user-view.html` (added rule) | A14 | No | Second audit's survivor, now caught |
| P2 `.nr-row a:hover { color: #0000ee }` | `user-view.html` (added rule) | A16 | No | Second audit's survivor, now caught |
| P3 `.nr-links a[href] { text-decoration: none }` | `user-view.html` (added rule) | A14 | No | Second audit's survivor, now caught |
| P4 `.nr-cover` `background: initial` | `user-view.html:15` | U60 | No | Second audit's survivor, now caught |
| P9 `.nr-links a:hover { opacity: .3 }` | `user-view.html` (added rule) | A17 | No | A17 fails |
| P10 `.nr-row a:focus { filter: brightness(.4) }` | `user-view.html` (added rule) | A17 | No | A17 fails |
| E2 error listener `{ capture: true, once: true }` | `user-view.html:270` | U57 | No | Second audit's survivor, now caught |
| E1 error listener `{ capture: true }` | `user-view.html:270` | U57 | Passes | Control. Equivalent registration, so passing is correct |
| **Q1 `.nr-meta ~ .nr-links a { text-decoration: none }`** | `user-view.html` (added rule) | A14 | **Yes** | **Real defect: underline gone. Finding 2** |
| **Q2 `.nr-title ~ .nr-links a:hover { color: #0000ee }`** | `user-view.html` (added rule) | A16 | **Yes** | **Real defect: hover contrast 1.8:1. Finding 2** |
| **Q3 `.nr-cover + div a { text-decoration: none }`** | `user-view.html` (added rule) | A14 | **Yes** | **Real defect: underline gone. Finding 2** |
| **Q9 `#nr-filters ~ #nr-panel a { text-decoration: none }`** | `user-view.html` (added rule) | A14 | **Yes** | **Real defect. Caught by the predicate at `ad2b277`. Finding 2** |
| **Q4 `.nr-links a:focus-visible { outline: none }`** | `user-view.html` (added rule) | A14 | **Yes** | **Real defect: no focus outline on the link. Finding 1** |
| **Q5 `.nr-links a { text-decoration-color: transparent }`** | `user-view.html` (added rule) | A14 | **Yes** | **Real defect: invisible underline. Finding 3** |
| **Q8 `.nr-cover` `background: transparent none`** | `user-view.html:15` | U60 | **Yes** | **Real defect: transparent placeholder. Finding 4** |
| Q6 `.nr-links a:hover { background: #00a4dc }` | `user-view.html` (added rule) | none (US4-AS2) | Yes | Not equivalent: 1:1 on hover. No behaviour states it. Finding 5 |
| Q7 `.nr-row:hover .nr-links { opacity: .3 }` | `user-view.html` (added rule) | none (A17's recorded limit) | Yes | Not equivalent. Outside A17 as stated. Finding 5 |

Every added rule carries the `#nr-user-view ` prefix, as the page's own rules do. Of the 35 mutants,
26 were caught, 7 survived inside a behaviour, 2 survived outside every behaviour, and E1 passed as
it should.

## Traceability

| Criterion | Tests | End to end |
| --- | --- | --- |
| US1-AS1 (FR-001) | A1, U44 | Yes: real page script through `load-page.js` |
| US1-AS2 (FR-002, FR-005) | A2, A15, U42, U46, U65 | Yes |
| US1-AS3 (FR-003) | A3 (two tests) | Yes |
| US1-AS4 (FR-002) | A4 (6 rows, typed and left), U64 | Yes |
| US1-AS5 (FR-004) | A5 (two tests) | Partly: keyboard operation is the browser's; the markup test reads source text, which the profile permits |
| US2-AS1 (FR-006, FR-006a) | A7, U31–U37, U48, U52, U66 | Yes: `AcceptanceRig` refresh → `GET Releases`, joined to the page by `releases.json` |
| US2-AS2 (FR-006a) | A8, U49, U55, U57 | Yes, within the fake DOM's limits |
| US2-AS3 (FR-007) | A9, U53, U56, U60 | Yes; placeholder visibility has a gap (Finding 4) |
| US2-AS4 | A10 | Yes |
| US3-AS1 (FR-009, SC-003) | A11, U59 | Declarations only; pixel equality is the real-browser pass |
| US3-AS2 (FR-009) | A12 | Yes |
| US4-AS1 (FR-010, SC-004) | A13, U62, U63 | Declarations only; rendered contrast is the real-browser pass |
| US4-AS2 (FR-010) | A14, A16, A17 | Partial: sibling-combinator rules escape (Finding 2), the link's own focus rule is not read (Finding 1), an invisible underline passes (Finding 3), and background and ancestor opacity are untested (Finding 5) |

Functional requirements without an acceptance scenario:

- FR-007a: covered by U50 (`loading="lazy"`).
- FR-008: covered by U51 (`no-referrer`). U32–U35 assert URL strings only.
- FR-011: covered by U36 and the unchanged existing suite.
- FR-002's "MUST NOT show as an applied filter": the page has no applied-filter indicator, so there
  is nothing to assert.

Untested criteria: none. No test traces to nothing:

- The `A14 helper`, `A17 helper`, `U57 helper` and `U60 helper` table rows pin the predicates that
  their behaviour tests use.
- `U38` and `U39` test the fake DOM.
- `U62` and `U63` test the contrast formula.

## What was not audited

- **Mutation score**: the profile has no mutation tool. 35 deliberate mutants are a sample. They were
  chosen to re-check the accepted test-after behaviours and the earlier survivors, and to probe the
  remediation's predicates. They are not exhaustive.
- **Mutants P5–P8 and the C# mutants N11 and N12**: neither the code nor the tests they cover changed
  since the second audit, so they were not re-run.
- **Coverage**: the page runs in a `node:vm` sandbox, which `--experimental-test-coverage` does not
  instrument. Dotnet coverage is unavailable (`coverage: null`).
- **Cycles 2–54**: the earlier audits checked these against history. This audit did not repeat that
  check. It re-ran their recorded mutants where condition 3 of `TEST_AFTER_ACCEPTED` needs them.
- **Real-browser behaviour**: datalist matching, keyboard operation, lazy-load timing, pixel widths,
  rendered contrast, the `error` capture phase, and `once`. These belong to the maintainer's pass in
  `quickstart.md` §2.
- **Case folding beyond ASCII**: `toLowerCase` against the server's `OrdinalIgnoreCase` grouping. No
  criterion names it.
- **The card background `#1c1c1c`** comes from research R9. It was not measured against a running
  theme.
- **Performance** for more than 1,000 artists: no measurable requirement.
- **Independence**: this audit ran in a fresh context with no memory of the loop. It was not run by a
  different model family. The smell pass was done in this context rather than by a subagent, because
  the context was already fresh and the changed test code is about 100 lines.
