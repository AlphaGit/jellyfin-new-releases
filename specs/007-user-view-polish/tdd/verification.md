---
feature: 007-user-view-polish
verdict: FAIL
standard: .specify/templates/overrides/tdd-test-quality-rubric.md # project override of the extension rubric (TEST_AFTER_ACCEPTED row)
profile: .specify/memory/tdd-profile.md
verified_at: 5846c02
behaviors: 46 # 80 on the list, 34 DROPPED
proven: 31
likely: 3
test_after: 12
test_after_accepted: 0
no_test: 0
dropped: 34
high_smells: 3
criteria_total: 13
criteria_covered: 13 # US4-AS2 only in part, see Finding 5
mutation_score: unmeasured # profile records mutation: null
deliberate_mutants: 27 applied, 24 caught, 3 survived # scope: the 4 source files and 1 test file the feature changed
suite: 318 passed, 0 failed, 10 s (dotnet) + 119 passed, 0 failed, 0.2 s (node; also under LANG=de_DE.UTF-8)
independent: fresh context # the auditing session did not write the tests in this context; same model family
---

# TDD Verification: Polish the New Releases view

**Verdict: FAIL.** `A4` is marked `DONE`, but the shipped Artist filter breaks it: typing
`constructor`, `toString` or `hasOwnProperty` sends `artistId=function…` to the server, because the
name lookup reads inherited keys of a plain object. Two more `HIGH` survivors sit inside `DONE`
behaviours (`A14`, `U60`), and 12 behaviours are `TEST_AFTER` with no recorded maintainer acceptance.

The loop itself is in good order. The cycle log and the git history agree on every cycle that was
checked. No existing test was weakened. Each test-after behaviour has a recorded deliberate mutant,
and this audit re-ran every one of them: all are caught today. The `TEST_AFTER` gap therefore closes
with one maintainer decision (Finding 4), not with new tests.

## Test-first evidence

Commit convention: each cycle's test and source land in one commit, and the next cycle's entry
records that commit's hash. Every recorded hash was matched to its commit message and file list.
`cd52744`, `6d6d8fa`, `bc52ad7`, `2ed2f69` and `204ed03` were also read in full. Each diff matches
the "green" text of its cycle, including the deliberate fakes (`[]` for `Covers`, unescaped IDs,
unescaped URLs) that later cycles replaced.

| Behavior | Class | Evidence |
| --- | --- | --- |
| A1 | PROVEN | cycle 2 red; `cd52744` adds test and datalist together |
| A2 | PROVEN | cycle 3 red; `3791558` |
| A3 | TEST_AFTER | cycle 4: both tests passed on first run (A2's code satisfied them); log says "test-after in the strict sense"; R1, R2 caught today |
| A4 | TEST_AFTER | cycle 5: passed on first run; R3 caught today. See Finding 1: the behaviour is violated by the shipped code |
| A5 | TEST_AFTER | cycle 6: both tests passed on first run; R4, R5 caught today |
| A7 | LIKELY | cycle 14 red recorded (`Actual []`); test held uncommitted by design and committed in `9947c90`, after its source (`bc52ad7`). History cannot show the order; it does not contradict the log |
| A8 | LIKELY | cycle 23 red in an untracked file; committed in `41cec3b`, after `98ae035`. Same reason as A7 |
| A9 | LIKELY | as A8 |
| A10 | PROVEN | cycle 23 red, re-observed at the new location in cycle 33; `319d07c` adds test and `alt=""` together |
| A11 | PROVEN | cycle 42 red; `7df504a` |
| A12 | TEST_AFTER | cycle 43: passed on first run; R11 caught today |
| A13 | PROVEN | cycle 47 red; `084d299` |
| A14 | TEST_AFTER | cycle 48: the visited test had a red; the underline and focus tests passed on first run. Graded by its weakest part. R13a, R13b caught today; N9 survives (Finding 2) |
| U31 | PROVEN | cycle 15 red; `7da7bce` |
| U32 | PROVEN | cycle 17 red; `bc52ad7` |
| U33 | TEST_AFTER | cycle 18: passed on first run; R9 caught today |
| U34 | TEST_AFTER | cycle 19: passed on first run; R10 caught today |
| U35 | PROVEN | cycle 20 red, widened to a `[Theory]` before the change; `2ed2f69` |
| U36 | TEST_AFTER | cycle 21: passed on first run; R11b caught today |
| U37 | PROVEN | cycle 16 red on the existing naming test; `6d6d8fa` |
| U38 | PROVEN | cycle 24 red; `8a26cf0` |
| U39 | PROVEN | cycle 25 red; `14a92eb` |
| U42 | PROVEN | cycle 8 red; `a0d0208` |
| U44 | PROVEN | cycle 9 red; `f146cb6` |
| U45 | TEST_AFTER | cycle 10: passed on first run; test narrowed in `b2720ed` (logged); R6 caught today |
| U46 | PROVEN | cycle 11 red; `e57fa95` |
| U47 | PROVEN | cycle 7 red on the existing exposure test; `5dcac35` |
| U48–U54 | PROVEN | cycles 26–32, one red each; `f3efd40` … `ee69fc1`, test and source together |
| U55 | PROVEN | cycle 35 red; `33a7bc0` |
| U56 | PROVEN | cycle 36 red; `d11940b` |
| U57 | PROVEN | cycle 37 red; `98ae035` |
| U58 | PROVEN | cycle 34 red; `d0ef65f` |
| U59 | PROVEN | cycle 44 red; `2962937` |
| U60 | PROVEN | cycle 39 red; `db3c3f4`. N6 survives (Finding 3) |
| U61 | PROVEN | cycle 40 red; `0d75bc8` |
| U62 | TEST_AFTER | cycle 46: passed on first run; R12 (on the test file) caught today |
| U63 | PROVEN | cycle 45 red; `d48a008` |
| U64 | TEST_AFTER | cycle 12: passed on first run; R7 caught today |
| U65 | TEST_AFTER | cycle 13: passed on first run; R8 caught today |
| U66 | PROVEN | cycle 41 red; `629dd0e` |
| A6, U1–U30, U40, U41, U43 | DROPPED | removed by the 2026-10-03 clarification; A6's red and the reason are in cycle 1 |

**None of the 12 `TEST_AFTER` behaviours is `TEST_AFTER_ACCEPTED`.** The override needs three
conditions. The third holds for all 12: this audit re-ran each recorded mutant and each one is
caught today. The second fails for all 12: the cycle log of `007` has no maintainer decision to
accept them. The first holds explicitly only for `A3` ("test-after in the strict sense"); the others
are described as "passed on the first run", which is the evidence but not the label.

**Existing tests.** The feature changed three existing tests:

- `exposure.test.js:26` widened its exact set by `artistIndex` and `nextCover`. This is a stronger
  assertion, not a weaker one.
- `ResponseNamingTests.cs:100-110` gained a `covers` argument.
- The three `tests/fixtures/pages/releases*.json` gained `covers`.

Nothing was removed, loosened, skipped or excluded. `fake-dom.js` gained `listenerOptions`,
`parentNode` and `remove()`; its existing contract tests still pass.

**`tasks.md` against the list.** Every ticked task's ids are `DONE`. No behavioural task is left
unticked. Two ticked tasks claim more than was built (Finding 7).

## Findings

| # | Severity | Finding | Evidence |
| --- | --- | --- | --- |
| 1 | HIGH | **A `DONE` behaviour is false in the shipped code.** `A4` states "text that equals no artist name requests releases with no `artistId`". `artistIds` is a plain object, so `artistIds['constructor']` returns `Object`. The `input` handler sees a change and reloads, and `query()` sends it. Probe (scratch file, not in the tree): `constructor` → `Releases?artistId=function%20Object()%20%7B%20%5Bnative%20code%5D%20%7D`; same for `toString` and `hasOwnProperty`. An artist literally named `__proto__` cannot be indexed at all. The test pins the predicate with one example, `asp`, against the profile's own rule "a predicate needs a table, not an example". **Should assert**: a `[table]` of rejecting texts (`asp`, `constructor`, `toString`, `hasOwnProperty`, `__proto__`) and an accepting case for an artist named `constructor`, each through `type()` | `src/.../Web/user-view.html:75, 89, 101, 254`; `tests/web/artist-filter.test.js:92-98` |
| 2 | HIGH | **Surviving mutant inside `A14`.** Adding `text-decoration-line: none` to `.nr-links a` (N9) removes the underline and every test stays green. The test reads only the `text-decoration` shorthand, and only on four exact selectors, so a longhand or a broader rule (`#nr-user-view a`) escapes it. **Should assert**: no rule that reaches a source link sets `text-decoration` or `text-decoration-line` to `none`, with the longhand in the rejecting cases | `tests/web/styles.test.js:102-106` |
| 3 | HIGH | **Vacuous assertion with a survivor inside `U60`.** `Boolean(cover.background)` accepts `background: none` (N6, survived) and `transparent`. Either value leaves no visible placeholder, against FR-007 and US2-AS3. **Should assert**: the declared background is a visible colour, rejecting `none`, `transparent` and zero alpha | `tests/web/styles.test.js:32` |
| 4 | HIGH (blocking) | **12 behaviours are `TEST_AFTER` with no maintainer acceptance**: A3, A4, A5, A12, A14, U33, U34, U36, U45, U62, U64, U65. All 12 have a mutant caught today. Only the decision and the label are missing. New tests cannot fix this | `tdd/cycle-log.md` cycles 4–6, 10, 12, 13, 18, 19, 21, 43, 46, 48 |
| 5 | MED | **US4-AS2 is covered only in part.** The criterion says the link "keeps the same contrast" on hover and focus. A hover rule `color: #0000ee` (N10, 1.8:1) survives. `A14`'s statement leaves hover and focus colour out, so this is a missing behaviour, not a survivor inside one | `tests/web/styles.test.js:98-110`; `tdd/test-list.md:45` |
| 6 | MED | **Foreign style, against an explicit profile rule.** The profile says "never read a page's source as text to assert on it". `A5`'s markup test does exactly that, with a regex coupled to attribute order, and all of `styles.test.js` reads the `<style>` block as text. The fake DOM models neither markup attributes nor CSS, so no compliant way exists today. Either the convention or the harness must change | `tests/web/artist-filter.test.js:101-105`; `tests/web/styles.test.js:12-27` |
| 7 | MED | **Ticked tasks claim work that is not in the page.** `T009` lists `decoding="async"` and `T016` lists a "centred note glyph". Neither is in `user-view.html`. `contracts/user-view.md:34` still specifies `decoding="async"`. The cycle log's session close records both as "left for a decision" | `tasks.md:112, 127`; `contracts/user-view.md:34`; `tdd/cycle-log.md:647-649` |
| 8 | LOW | **Assertions read the wrong request.** In `A4` and `U45`, typing sends no request, so `requests.at(-1)` and the "every request is unfiltered" check read the initial page load. The tests still catch a wrong match, which would add a request. But they cannot tell "no request" from "an unfiltered request", and the names claim the latter. US1-AS4's "when they leave the field" (`change`) is never fired | `tests/web/artist-filter.test.js:95-97, 133-136` |
| 9 | LOW | **Duplicated setup.** `cover-fallback.test.js` re-implements `render.test.js`'s `rendered`, `coverBox` and `imgAttribute`. The two `imgAttribute` copies differ: one decodes `&amp;`, one does not | `tests/web/cover-fallback.test.js:54-69`; `tests/web/render.test.js:186-200` |
| 10 | LOW | **Refactoring sensitivity.** `U57` asserts the options are exactly `[true]`. The equivalent `{ capture: true }` would fail it | `tests/web/cover-fallback.test.js:46` |
| 11 | LOW | **`T026` is ticked, but its last step is not done.** The CI run after the push is not verified, because the push waits for the maintainer. HEAD is on `main` and on `AlphaGit/tdd-run-red-green` | `tasks.md:187` |

**Suite properties.** The node suite is fast (0.2 s) and deterministic: one `setImmediate` tick,
a pinned locale, and no clock in the new tests. The dotnet additions use the recorded
`TestDatabase` and `AcceptanceRig`. No network, no sleeps, no shared state was found. Failure
messages name the behaviour id in every new test.

## Mutation results

No mutation tool (profile: `mutation: null`). There were 27 deliberate mutants, applied one at a
time by a scratch runner. Each was restored from a file copy and verified byte-equal. The full
suites were re-run green afterwards, and `git status` was clean.

Sample:

- the recorded mutant of every `TEST_AFTER` behaviour, re-run (R*)
- new mutants on the high-risk paths: the filter predicate, the cover order and escaping, the
  fallback chain, contrast, underline, placeholder and the repository read (N*)

| Mutant | File | Behavior | Survived | Judgment |
| --- | --- | --- | --- | --- |
| R1 `query()` keeps the last applied id on no match | `user-view.html:89` | A3 | No | Both A3 tests fail |
| R2 Clear skips the Artist field | `user-view.html` Clear handler | A3 | No | A3 Clear test fails |
| R3 case-insensitive index | `user-view.html:102` | A4 | No | A4 and U42 fail |
| R4 `list` attribute removed | `user-view.html:44` | A5 | No | Markup test fails |
| R5 `keydown` listener added | `user-view.html:253` | A5 | No | Key-handling test fails |
| R6 `.catch` sets the map to `null` | `user-view.html` `loadArtists` | U45 | No | `TypeError` fails U45 |
| R7 reload whenever nothing is applied | `user-view.html:254` | U64 | No | U64 fails |
| R8 applied artist recorded in the handler | `user-view.html:89, 254` | U65 | No | U65 fails |
| N1 `input` always reloads | `user-view.html:254` | U46 | No | U46 and U64 fail |
| R11 Restore wrapped in its own span | `user-view.html:197` | A12 | No | A12 fails |
| N2 `loading="eager"` | `user-view.html` `cover()` | U50 | No | U50 fails |
| N3 fallbacks not escaped | `user-view.html` `cover()` | U54 | No | U54 fails |
| N4 `img.src = ''` instead of `remove()` | `user-view.html` `nextCover` | U56, A9 | No | Both fail |
| N5 capture flag `false` | `user-view.html:265` | U57 | No | U57 fails |
| **N6 `.nr-cover` `background: none`** | `user-view.html:15` | U60 | **Yes** | **Real defect class: no visible placeholder. Finding 3** |
| N7 `grid-auto-columns: auto` | `user-view.html:31` | U59 | No | U59 fails |
| N8 link colour `#3a6ea5` (3.2:1) | `user-view.html:26` | A13 | No | A13 fails |
| R13a `text-decoration: none` on `.nr-links a` | `user-view.html:25` | A14 | No | Underline test fails |
| **N9 `text-decoration-line: none` on `.nr-links a`** | `user-view.html:25` | A14 | **Yes** | **Real defect: underline gone, suite green. Finding 2** |
| R13b `:focus-visible` `outline: none` | `user-view.html:30` | A14 | No | Focus test fails |
| **N10 `.nr-links a:hover { color: #0000ee }`** | `user-view.html:26` | none (US4-AS2) | **Yes** | **Not equivalent: hover contrast drops to 1.8:1. No behaviour states it. Finding 5** |
| R12 flare terms dropped from `contrast()` | `styles.test.js:81` | U62 | No | U62 and U63 fail |
| R9 URL-format branch inverted | `ReleasesController.cs` `CoversOf` | U33 | No | U33 fails |
| R10 `front-250` → `front-500` | `ReleasesController.cs` `CoversOf` | U34 | No | U34 fails |
| R11b `Sources` ordered descending | `ReleasesController.cs` `ToDto` | U36 | No | U36 fails |
| N11 Deezer ordered last | `ReleasesController.cs` `CoversOf` | U32, A7 | No | Both fail |
| N12 `sourceReleaseId` reads `url` | `ReleaseRepository.cs:188` | U31, A7 | No | Both fail |

Not a mutant, but recorded here: the probe for Finding 1 is a real defect in the shipped code, found
by input, not by a code change.

## Traceability

| Criterion | Tests | End to end |
| --- | --- | --- |
| US1-AS1 (FR-001) | A1, U44 | Yes: real page script through `load-page.js` |
| US1-AS2 (FR-002, FR-005) | A2, U42, U46, U65 | Yes |
| US1-AS3 (FR-003) | A3 (two tests) | Yes |
| US1-AS4 (FR-002) | A4, U64 | Yes, but the predicate is broken for inherited keys (Finding 1). `change` is not exercised (Finding 8) |
| US1-AS5 (FR-004) | A5 (two tests) | Partly: keyboard operation is the browser's; the markup test reads source text (Finding 6) |
| US2-AS1 (FR-006, FR-006a) | A7, U31–U37, U48, U52, U66 | Yes: `AcceptanceRig` refresh → `GET Releases`, joined to the page by the `releases.json` fixture |
| US2-AS2 (FR-006a) | A8, U49, U55, U57 | Yes, with the fake DOM's limits (no event dispatch) |
| US2-AS3 (FR-007) | A9, U53, U56, U60 | Yes; placeholder visibility unpinned (Finding 3) |
| US2-AS4 | A10 | Yes |
| US3-AS1 (FR-009, SC-003) | A11, U59 | Declarations only; pixel equality is the maintainer's real-browser pass |
| US3-AS2 (FR-009) | A12 | Yes |
| US4-AS1 (FR-010, SC-004) | A13, U62, U63 | Declarations only; rendered contrast is the real-browser pass |
| US4-AS2 (FR-010) | A14 | Partial: underline (weak, Finding 2), visited colour, focus outline. Hover and focus colour untested (Finding 5) |

Functional requirements without an acceptance scenario:

- FR-007a: covered by U50 (`loading="lazy"`).
- FR-008: covered by U51 (`no-referrer`). The server side holds by construction: U32–U35 assert URL
  strings only.
- FR-011: covered by U36 and the unchanged existing suite.
- FR-002's "MUST NOT show as an applied filter": no test. The page has no applied-filter indicator,
  so nothing exists to assert.

Untested criteria: none. Tests tracing to nothing: none. U38 and U39 test the fake DOM, and U62 and
U63 test the test's own contrast formula. All four are traced to the invariants that need them.

## What was not audited

- **Mutation score**: no tool in the profile. 27 deliberate mutants are a sample. They were chosen
  for risk and to re-check the test-after behaviours; they are not exhaustive.
- **Coverage of the page**: `--experimental-test-coverage` instruments only the test modules. The
  page runs in a `node:vm` sandbox and is not instrumented. Dotnet coverage is unavailable
  (`coverage: null`, no `coverlet.collector`).
- **Real-browser behaviour**: datalist matching, keyboard operation, lazy-load timing, pixel widths,
  rendered contrast and the `error` capture phase. These belong to the maintainer's own pass in
  `quickstart.md` §2. A fake DOM with no layout and no dispatch cannot observe them.
- **The card background value** `#1c1c1c` is taken from research R9. It was not measured against a
  running Jellyfin theme.
- **Performance** for more than 1,000 artists: there is no measurable requirement, and it was not
  assessed.
- **History was spot-checked, not read in full**: every recorded commit hash was matched to its
  message and file list, and five source diffs were read in full.
- **Independence**: this audit ran in a fresh context, with no memory of the loop. It was not run by
  a different model family.
