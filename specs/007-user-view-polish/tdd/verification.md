---
feature: 007-user-view-polish
verdict: FAIL
standard: .specify/templates/overrides/tdd-test-quality-rubric.md # project override of the extension rubric (TEST_AFTER_ACCEPTED row)
profile: .specify/memory/tdd-profile.md
verified_at: ad2b277
previous_audit: 5846c02 (FAIL)
behaviors: 48 # 82 on the list, 34 DROPPED
proven: 34
likely: 3
test_after: 1
test_after_accepted: 10
no_test: 0
dropped: 34
high_smells: 4
criteria_total: 13
criteria_covered: 13 # US4-AS2 only in part, see Finding 5
mutation_score: unmeasured # profile records mutation: null
deliberate_mutants: 27 applied, 22 caught, 5 survived # scope: user-view.html, ReleasesController.cs, styles.test.js
suite: 318 passed, 0 failed, 10 s (dotnet) + 165 passed, 0 failed, 0.2 s (node; also under LANG=de_DE.UTF-8)
independent: fresh context # the auditing session did not write the tests; same model family
---

# TDD Verification: Polish the New Releases view

**Verdict: FAIL.** The new selector predicate behind `A14` and `A16` misses any rule that reaches a
source link through an ancestor class: `#nr-user-view .nr-row a { text-decoration-line: none }` and
`#nr-user-view .nr-row a:hover { color: #0000ee }` both leave the suite green. Three more blocking
items are open. `ad2b277` weakened `U57`. `A16` is test-after with no maintainer decision. `U60`'s
colour check accepts `background: initial`.

This is the second audit. The first one (`5846c02`) gave FAIL with three `HIGH` findings and twelve
unaccepted test-after behaviours. The remediation closed all of them:

- The `A4` defect is fixed and pinned by a table: `constructor`, `toString`, `hasOwnProperty` and
  `__proto__`.
- `N6` and `N9` are now caught.
- The maintainer accepted eleven test-after behaviours on 2026-10-03. Ten of them grade
  `TEST_AFTER_ACCEPTED`, and `U45` now has its own red.

The new findings are all in the remediation's own test code: two predicates, one loosened
assertion, and one new behaviour.

## Test-first evidence

Commit convention: each cycle's test and source land in one commit. Cycles 2–48 were checked by the
first audit. This audit re-read cycles 49–54, the two refactor entries, and the full diff
`5846c02..ad2b277`. Each "fix:" or "feat:" commit (`64ff918`, `6495435`, `3e4bc76`) adds the test and
the source together, and its cycle records a red that the diff explains. The test-only commits
(`352f5d8`, `baff78c`, `6880ddd`, `7767c12`, `ad2b277`) change no source, as their entries say.

| Behavior | Class | Evidence |
| --- | --- | --- |
| A1 | PROVEN | cycle 2 red; `cd52744` |
| A2 | PROVEN | cycle 3 red; `3791558` |
| A3 | TEST_AFTER_ACCEPTED | labelled and accepted (cycle-log "Maintainer decisions", 2026-10-03); R1, R2 caught today |
| A4 | PROVEN | restated by the case decision; cycle 49 red on 4 rows (`constructor`, `toString`, `hasOwnProperty`, `__proto__`); `64ff918` adds test and source together |
| A5 | TEST_AFTER_ACCEPTED | accepted 2026-10-03; R4, R5 caught today |
| A7 | LIKELY | cycle 14 red; test committed in `9947c90`, after its source. History cannot show the order |
| A8 | LIKELY | cycle 23 red in an untracked file; committed in `41cec3b`, after `98ae035` |
| A9 | LIKELY | as A8 |
| A10 | PROVEN | cycle 23 red, re-observed in cycle 33; `319d07c` |
| A11 | PROVEN | cycle 42 red; `7df504a` |
| A12 | TEST_AFTER_ACCEPTED | accepted 2026-10-03; R11 caught today |
| A13 | PROVEN | cycle 47 red; `084d299` |
| A14 | TEST_AFTER_ACCEPTED | accepted 2026-10-03; R13a, R13b, N9 caught today. P1 and P3 survive (Finding 1) |
| A15 | PROVEN | cycle 51 red, 4 rows; `3e4bc76` |
| **A16** | **TEST_AFTER** | cycle 54: passed on first run, labelled test-after with N10 and N8 as evidence. **No maintainer decision**: the entry ends "open: needs the maintainer's decision" (`cycle-log.md:777`). Finding 3 |
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
| U45 | PROVEN | also in the accepted list; its rewritten test has its own red (cycle 50, the `constructor` row); `6495435` |
| U46 | PROVEN | cycle 11 red; `e57fa95` |
| U47 | PROVEN | cycle 7 red; `5dcac35` |
| U48–U54 | PROVEN | cycles 26–32, one red each |
| U55 | PROVEN | cycle 35 red; `33a7bc0` |
| U56 | PROVEN | cycle 36 red; `d11940b` |
| U57 | PROVEN | cycle 37 red; `98ae035`. Test loosened by `ad2b277`; E2 survives (Finding 2) |
| U58 | PROVEN | cycle 34 red; `d0ef65f` |
| U59 | PROVEN | cycle 44 red; `2962937` |
| U60 | PROVEN | cycle 39 red; assertion corrected with a red on its helper table in cycle 53. P4 survives (Finding 4) |
| U61 | PROVEN | cycle 40 red; `0d75bc8` |
| U62 | TEST_AFTER_ACCEPTED | accepted 2026-10-03; R12 caught today |
| U63 | PROVEN | cycle 45 red; `d48a008` |
| U64 | TEST_AFTER_ACCEPTED | accepted 2026-10-03; R7 caught today |
| U65 | TEST_AFTER_ACCEPTED | accepted 2026-10-03; R8 caught today |
| U66 | PROVEN | cycle 41 red; `629dd0e` |
| A6, U1–U30, U40, U41, U43 | DROPPED | removed by the 2026-10-03 clarification |

**`TEST_AFTER_ACCEPTED` conditions.** The cycle log meets all three conditions for each of the ten.

1. The "Maintainer decisions" entry labels each one test-after and names its evidence.
2. The same entry records a dated decision to accept them.
3. This audit re-ran each recorded mutant, and each one fails its test today.

The audit cannot tell the maintainer's commits from the loop's. Every commit has the same author,
so the attribution of the decision rests on the log's own statement.

**Existing tests changed since `5846c02`.**

| Test | Before | After | Judgment |
| --- | --- | --- | --- |
| `artist-filter.test.js` A4 | `asp` → unfiltered | 6-row table; `asp` now applies ASP (A15) | Reversal driven by the amended FR-002 (spec Clarifications 2026-10-03). Not a weakening |
| `artist-filter.test.js` U42 | keys `ASP`, `Aspen`, `Wasp` | keys in lower case | Baseline change with a red (cycle 51) |
| `artist-filter.test.js` U45 | `releases.length > 0 && every unfiltered` | exact `requests.slice(before)` | Stronger |
| `styles.test.js` A14 underline | shorthand on 4 exact selectors | every rule through `rules()` and two predicates | Stronger, but the predicate has a gap (Finding 1) |
| `styles.test.js` U60 | `Boolean(cover.background)` | `isVisibleColour(...)` | Stronger, but the predicate has a gap (Finding 4) |
| `cover-fallback.test.js:44-51` U57 | `listenerOptions.error` deep-equals `[true]` | each option mapped through `capture`, then equals `[true]` | **Weakened** (Finding 2) |
| `cover-fallback.test.js` A8, A9 | own `imgAttribute` (decodes `&amp;` only) | shared `cover-markup.js`, decodes every entity `esc` writes | Stronger; N3, N4 and M12 still caught (cycle log) |

No test was skipped, renamed out of a filter, or excluded.

**`tasks.md` against the list.** Every ticked task's ids are `DONE`. `T044` is ticked with `A16`
`DONE`, but `A16` is not accepted as test-after (Finding 3). `T026` stays ticked while `T050`, its
check, is open.

## Findings

| # | Severity | Finding | Evidence |
| --- | --- | --- | --- |
| 1 | HIGH | **`reachesSourceLink` misses rules that reach a source link through an ancestor class or an attribute.** The predicate rejects any compound that holds a class other than `.nr-links`. A source link sits inside `article.nr-row`, so `.nr-row a` reaches it. The helper table has no ancestor-class row, so the table pins the wrong rule. Three deliberate mutants survive inside `DONE` behaviours: P1 `#nr-user-view .nr-row a { text-decoration-line: none }` (A14), P2 `#nr-user-view .nr-row a:hover { color: #0000ee }` (A16, 1.8:1), P3 `#nr-user-view .nr-links a[href] { text-decoration: none }` (A14). **Should assert**: an accepting row for each ancestor class in the card markup (`.nr-row`, `.nr-list`, and a `<div>` with no class), plus `a[href]`, `.nr-links :any-link` and `.nr-links *`. Rejecting rows must still include `.nr-artist a` | `tests/web/styles.test.js:152-157`, table `:163-179`; used at `:196-201`, `:207-209` |
| 2 | HIGH | **`ad2b277` weakened the existing `U57` test.** The `capture` mapping accepts any object with `capture: true`. Mutant E2, `}, { capture: true, once: true });` on the panel's `error` listener, survives `cover-fallback.test.js`. The old exact `[true]` check rejected it. With `once`, the listener stops after the first failed cover, so every later failed cover shows a broken-image icon (SC-002, FR-007). The fake DOM ignores `once`, so `A8` and `A9` cannot catch it either. The first audit's Finding 10 asked for this loosening and did not name the risk. **Should assert**: either `true`, or an object whose only effective option is `capture: true`. Reject `once`. A table must hold `{ capture: true, once: true }` as a rejecting row | `tests/web/cover-fallback.test.js:44-51` |
| 3 | HIGH (blocking) | **`A16` is `TEST_AFTER` with no maintainer decision.** Cycle 54 labels it and records N10 and N8 as evidence, and both are caught today. The entry ends "open: needs the maintainer's decision", and no later entry records one. `T044` is ticked and `A16` is `DONE` regardless. New tests cannot fix this | `tdd/cycle-log.md:764-777`; `tasks.md` T044 |
| 4 | HIGH | **`isVisibleColour` accepts CSS-wide keywords.** P4 `background: initial` on `.nr-cover` survives `U60`. `initial` makes the background transparent, so no placeholder is visible (FR-007, US2-AS3). `inherit`, `unset`, `revert` and `revert-layer` go through the same branch. The helper table has no keyword row. **Should assert**: rejecting rows for `initial`, `unset`, `revert`, `revert-layer` and `inherit`, or else accept only the colour notations the table lists | `tests/web/styles.test.js:30-40`, table `:42-62`, `U60` `:65-68` |
| 5 | MED | **US4-AS2 "keeps the same contrast" is still covered only in part.** `A16` reads `color` only. P9 `#nr-user-view .nr-links a:hover { opacity: .3 }` cuts the rendered contrast below 4.5:1, and every test stays green. No behaviour on the list states it, so this is a missing behaviour, not a survivor inside one. A `background` or `filter` on hover is the same class | `tests/web/styles.test.js:196-201`; `tdd/test-list.md:49` |
| 6 | LOW | **Cycles 49–54 and the two refactor entries do not name their commits.** Cycles 2–48 name each commit in the next entry, and the history check depends on that link. This audit matched the cycles to commits by message and file list | `tdd/cycle-log.md:687-802` |
| 7 | LOW | **Two CSS readers in one file.** `declarations()` drops `@media` blocks and matches a scoped selector exactly. `rules()` keeps `@media` and splits every selector list. They parse the same text in two ways, so a fix to one does not reach the other | `tests/web/styles.test.js:17-27`, `:138-149` |
| 8 | LOW | **`T026` is ticked, but its CI check is not done.** This is the first audit's Finding 11. `T050` already tracks it, so this audit adds no task | `tasks.md:187, 254` |

**Suite properties.** Both suites are fast: node takes 0.2 s and dotnet takes 10 s. Both are
deterministic. The new tests use one `setImmediate` tick and a pinned locale. They use no clock, no
network and no shared state. Each table row is its own `test()`, so a failure names the input that
broke. The two predicates follow the profile's "a predicate needs a table" rule. Findings 1 and 4
show that a table is only as strong as its rows: neither table holds a row from the real card markup.

## Mutation results

No mutation tool (profile: `mutation: null`). A scratch runner applied 27 deliberate mutants, one at
a time. It copied the file aside, applied a literal replacement that had to match exactly once, ran
the behaviour's test file or filter, and copied the file back. It then checked each restore
byte-equal to `HEAD` with `cmp -s`. After the run, `git status` was clean, and both full suites were
green again (dotnet 318, node 165).

Sample:

- R*: the recorded mutant of each accepted test-after behaviour, re-run for condition 3.
- N6, N9, N10: the first audit's survivors, re-run.
- P*, E2: new mutants on the remediation's predicates, lookup and listener.

| Mutant | File | Behavior | Survived | Judgment |
| --- | --- | --- | --- | --- |
| R1 `query()` keeps the last applied id on no match | `user-view.html:89` | A3 | No | Both A3 tests fail |
| R2 Clear skips the Artist field | `user-view.html:262` | A3 | No | A3 Clear test fails |
| R4 `list` attribute removed | `user-view.html:44` | A5 | No | Markup test fails |
| R5 `keydown` listener added | `user-view.html:258` | A5 | No | Key-handling test fails |
| R11 Restore wrapped in `<span class="nr-restore">` | `user-view.html:202` | A12 | No | A12 fails |
| R13a `text-decoration: none` on `.nr-links a` | `user-view.html:24` | A14 | No | Underline test fails |
| R13b `:focus-visible` `outline: none` | `user-view.html:29` | A14 | No | Focus test fails |
| R6 `.catch` sets the map to `null` | `user-view.html:244` | U45 | No | Both U45 rows fail |
| R7 reload whenever nothing is applied | `user-view.html:259` | U64 | No | U64, every A4 row and both U45 rows fail |
| R8 applied artist recorded in the handler | `user-view.html:89, 259` | U65 | No | U65 fails |
| R12 flare terms dropped from `contrast()` | `styles.test.js:117` | U62 | No | U62, U63 fail |
| R9 URL-format branch inverted | `ReleasesController.cs:228` | U33 | No | U33 fails |
| R10 `front-250` → `front-500` | `ReleasesController.cs:230` | U34 | No | U34 fails |
| R11b `Sources` ordered descending | `ReleasesController.cs:221` | U36 | No | U36 fails |
| N6 `.nr-cover` `background: none` | `user-view.html:15` | U60 | No | First audit's survivor, now caught |
| N9 `text-decoration-line: none` on `.nr-links a` | `user-view.html:24` | A14 | No | First audit's survivor, now caught |
| N10 `.nr-links a:hover { color: #0000ee }` | `user-view.html:24` | A16 | No | First audit's survivor, now caught |
| P5 `artistIndex` on `{}` | `user-view.html:106` | A4, A15 | No | A4 `constructor`, `__proto__`; A15 `__PROTO__`; U45 `constructor` fail |
| P6 `artistOf` without `toLowerCase` | `user-view.html:101` | A15 | No | A15, A2, A3, U65 fail |
| P7 `artistIndex` keeps the name's case | `user-view.html:107` | U42, A15 | No | U42, A15, A2, A3, U65 fail |
| P8 initial `artistIds = {}` | `user-view.html:75` | U45 | No | U45 `constructor` row fails |
| **P1 `#nr-user-view .nr-row a { text-decoration-line: none }`** | `user-view.html` (added rule) | A14 | **Yes** | **Real defect: underline gone. Finding 1** |
| **P2 `#nr-user-view .nr-row a:hover { color: #0000ee }`** | `user-view.html` (added rule) | A16 | **Yes** | **Real defect: hover contrast 1.8:1. Finding 1** |
| **P3 `#nr-user-view .nr-links a[href] { text-decoration: none }`** | `user-view.html` (added rule) | A14 | **Yes** | **Real defect: underline gone. Finding 1** |
| **P4 `.nr-cover` `background: initial`** | `user-view.html:15` | U60 | **Yes** | **Real defect: transparent placeholder. Finding 4** |
| **E2 error listener `{ capture: true, once: true }`** | `user-view.html:270` | U57 (A8, A9) | **Yes** | **Real defect: only the first failed cover falls back. Finding 2** |
| P9 `#nr-user-view .nr-links a:hover { opacity: .3 }` | `user-view.html` (added rule) | none (US4-AS2) | Yes | Not equivalent. No behaviour states it. Finding 5. Not counted among the 5 survivors inside a behaviour |

The 27 mutants are 26 that ran in the main pass plus E2. Of those, 22 were caught and 5 survived
inside a behaviour. P9 survived outside every behaviour.

## Traceability

| Criterion | Tests | End to end |
| --- | --- | --- |
| US1-AS1 (FR-001) | A1, U44 | Yes: real page script through `load-page.js` |
| US1-AS2 (FR-002, FR-005) | A2, A15, U42, U46, U65 | Yes |
| US1-AS3 (FR-003) | A3 (two tests) | Yes |
| US1-AS4 (FR-002) | A4 (6 rows, typed and left), U64 | Yes; `change` now fired |
| US1-AS5 (FR-004) | A5 (two tests) | Partly: keyboard operation is the browser's; the markup test reads source text, which the profile now permits |
| US2-AS1 (FR-006, FR-006a) | A7, U31–U37, U48, U52, U66 | Yes: `AcceptanceRig` refresh → `GET Releases`, joined to the page by `releases.json` |
| US2-AS2 (FR-006a) | A8, U49, U55, U57 | Yes, within the fake DOM's limits; listener options are weak (Finding 2) |
| US2-AS3 (FR-007) | A9, U53, U56, U60 | Yes; placeholder visibility has a gap (Finding 4) |
| US2-AS4 | A10 | Yes |
| US3-AS1 (FR-009, SC-003) | A11, U59 | Declarations only; pixel equality is the real-browser pass |
| US3-AS2 (FR-009) | A12 | Yes |
| US4-AS1 (FR-010, SC-004) | A13, U62, U63 | Declarations only; rendered contrast is the real-browser pass |
| US4-AS2 (FR-010) | A14, A16 | Partial: underline, visited colour, focus outline, state colours. Ancestor-class rules escape both (Finding 1); opacity and background are untested (Finding 5) |

Functional requirements without an acceptance scenario:

- FR-007a: covered by U50 (`loading="lazy"`).
- FR-008: covered by U51 (`no-referrer`). U32–U35 assert URL strings only.
- FR-011: covered by U36 and the unchanged existing suite.
- FR-002's "MUST NOT show as an applied filter": the page has no applied-filter indicator, so there
  is nothing to assert.

Untested criteria: none. No test traces to nothing:

- The `A14 helper` and `U60 helper` table rows pin the predicates that `A14` and `U60` use.
- `U38` and `U39` test the fake DOM.
- `U62` and `U63` test the contrast formula.

## What was not audited

- **Mutation score**: the profile has no mutation tool. 27 deliberate mutants are a sample. They were
  chosen to re-check the accepted test-after behaviours and to probe the remediation's new code.
  They are not exhaustive.
- **Coverage**: the page runs in a `node:vm` sandbox, which `--experimental-test-coverage` does not
  instrument. Dotnet coverage is unavailable (`coverage: null`).
- **Cycles 2–48**: the first audit checked these against history. This audit did not repeat that
  check. It re-ran their recorded mutants only where condition 3 of `TEST_AFTER_ACCEPTED` needs them.
- **C# mutants N11 and N12** (cover order, repository read): the code has not changed since the first
  audit, so they were not re-run.
- **Real-browser behaviour**: datalist matching, keyboard operation, lazy-load timing, pixel widths,
  rendered contrast, the `error` capture phase, and `once`. These belong to the maintainer's pass in
  `quickstart.md` §2.
- **Case folding beyond ASCII**: `toLowerCase` against the server's `OrdinalIgnoreCase` grouping (for
  example `ß`, dotted `İ`). No criterion names it.
- **The card background `#1c1c1c`** comes from research R9. It was not measured against a running
  theme.
- **Performance** for more than 1,000 artists: no measurable requirement.
- **Independence**: this audit ran in a fresh context with no memory of the loop. It was not run by a
  different model family. The smell pass was done in this context rather than by a subagent, because
  the context was already fresh.
