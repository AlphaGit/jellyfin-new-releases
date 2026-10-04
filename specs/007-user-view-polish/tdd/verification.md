---
feature: 007-user-view-polish
verdict: FAIL
standard: .specify/templates/overrides/tdd-test-quality-rubric.md # project override of the extension rubric (TEST_AFTER_ACCEPTED row)
profile: .specify/memory/tdd-profile.md
verified_at: 78b0f4b
previous_audit: 8e7daf7 (FAIL)
behaviors: 51 # 85 on the list, 34 DROPPED
proven: 35
likely: 3
test_after: 0
test_after_accepted: 13
no_test: 0
dropped: 34
high_smells: 3
criteria_total: 13
criteria_covered: 13 # with gaps in US1-AS5, US2-AS1, US2-AS4, US3 and US4, see Findings 2 and 3
mutation_score: unmeasured # profile records mutation: null
deliberate_mutants: 75 applied; 56 caught; 17 survived inside a behaviour; 2 controls behave as intended (E1 passes, K1 fails U67 only) # scope: user-view.html, ReleasesController.cs, styles.test.js
suite: 318 passed, 0 failed, 10 s (dotnet) + 254 passed, 0 failed, 0.2 s (node; also under LANG=de_DE.UTF-8)
independent: no # this session wrote cycles 64–66; the smell pass and the new mutants came from fresh-context subagents and were re-verified here
---

# TDD Verification: Polish the New Releases view

**Verdict: FAIL.** 17 deliberate mutants outside the `<style>` rules survive inside `DONE` behaviours,
and the U67 list has no recorded review. For example, a second `<style>` element with
`.nr-links a { text-decoration: none }`, or `style="width:50%"` on the "Ignore" button, leaves every
test green.

This is the fifth audit. The fourth (`8e7daf7`) gave FAIL with 22 survivors inside the stylesheet.
The closed-world check U67 closed that class completely:

- All 58 earlier mutants are caught again, including the 22 survivors and the six recorded-ceiling
  mutants.
- K1 now fails U67 only.
- `dimsText` reads `!important`.

Every test-first class still holds.

The new survivors are outside the rules that U67 reads. They fall into two places:

- **Style sources other than the first `<style>` block's plain rules**: a second `<style>` element, an
  `@supports` or `@layer` wrapper, and a nested `@media`.
- **The markup the page writes**: an inline `style`, an added or renamed class, a `disabled`
  attribute, and an attribute repeated in capital letters.

Unlike the predicate tables, both places are finite. The page has one stylesheet and a fixed set of
element templates. A closed world over each one ends this class of survivor.

## Test-first evidence

Cycles 2–63 were checked by the earlier audits. This audit re-read the T069 decision, cycles 64–66,
and the commit map. It also read the full diff `8e7daf7..78b0f4b`. That range changes no file under
`src/`. Each cycle commit (`5ebd87c`, `3ae9a45`, `78b0f4b`) changes only `tests/web/styles.test.js` and
the feature's documents. Each cycle records a red that its diff explains. Cycle 64's red is U67
against an empty list.

| Behavior | Class | Evidence |
| --- | --- | --- |
| A1, A2, A4, A10, A13, A15 | PROVEN | cycles 2, 3, 49, 23/33, 47, 51 |
| A3, A5, A12, A14, A16, A17, A18, U33, U34, U36, U62, U64, U65 | TEST_AFTER_ACCEPTED | each labelled, accepted 2026-10-03, and a recorded mutant caught today (R1, R2, R4, R5, R11, R13a, R13b, N10, N8, P9, P10, Q6, Q7, R9, R10, R11b, R12, R7, R8) |
| A7, A8, A9 | LIKELY | reds recorded; history cannot show the order (second audit) |
| A11, U59, U60, U61, U66 | PROVEN | cycles 42, 44, 39, 40, 41; helpers corrected with reds in later cycles |
| U31, U32, U35, U37–U39, U42, U44–U58, U63 | PROVEN | as in the fourth audit |
| **U67** | **PROVEN** | cycle 64 red (`STYLESHEET = []`, 1 failed); `5ebd87c` adds the test and the list together. The list itself is Finding 1 |
| A6, U1–U30, U40, U41, U43 | DROPPED | removed by the 2026-10-03 clarification |

`A5`, `A10`, `A11`, `A12`, `A13`, `A14`, `A17`, `A18`, `U50`, `U51`, `U59`, `U61` and `U66` each have a
survivor (Findings 2 and 3).

**`TEST_AFTER_ACCEPTED` conditions.** All three conditions still hold for each of the thirteen. The
attribution of each decision to the maintainer rests on the log's own statement.

**Existing tests changed since `8e7daf7`.**

| Test | Before | After | Judgment |
| --- | --- | --- | --- |
| `styles.test.js:254-265` `removesUnderline` | its own whitespace split | calls `hidesLine`, which reads a colour function as one token and sets `!` aside | Stronger: K1's false failure gone. M2 now caught by A14 too |
| `styles.test.js:416-418` `removesOutline` | its own split; missed `none!important` | calls `hidesLine` | Stronger |
| `styles.test.js:331-338` `dimsText` | missed `!important` | sets `!important` aside; ceiling comment added | Stronger. M4 caught by A17 |

No test was skipped, renamed out of a filter, or excluded. No row of an earlier table was removed or
inverted.

**`tasks.md` against the list.** Every ticked task's ids are `DONE`. `T063` now carries `[A18]`. No
task carries `[U67]`. T065–T068 name the behaviours whose mutants U67 catches, which is accurate.

## Findings

| # | Severity | Finding | Evidence |
| --- | --- | --- | --- |
| 1 | HIGH | **The U67 list has no recorded review.** It meets the catalogue's "self-approving snapshot": `STYLESHEET` was copied from the page in the same commit that accepted it, and it "matched on the first run". 7 of its 28 entries were read against `contracts/user-view.md`, and the predicate tests pin their meaning. The other 21 predate 007 and have no requirement and no reviewer. Cycle 64 says "The maintainer should read it once". No dated entry records that review. **Should**: a dated cycle-log entry that records the maintainer's review at a named commit, and a mark on the 21 entries that predate 007 | `tests/web/styles.test.js:53-86`; `tdd/cycle-log.md` cycle 64 "review" |
| 2 | HIGH | **U67 reads only the plain rules of the first `<style>` element.** `STYLE` takes the first block. `stylesheet()` skips any at-rule but `@media`, and it keeps only the innermost `@media` condition. Survivors: W1, a second `<style>` element that removes the underline (A14); V1, the link colour rule inside `@supports not (color: red)`, so it never applies (A13, A16); V2, `:focus-visible` inside `@layer x`, so the host theme wins (A14); V3, the narrow-screen rule inside `@media print` (U59). **Should assert**: the page has exactly one `<style>` element; its only at-rule is the pinned `@media`; and `stylesheet()` accounts for every non-whitespace character of the block | `tests/web/styles.test.js:13`, `:39-50` |
| 3 | HIGH | **The markup the page writes is open.** The render tests check that the expected attributes and classes are present. They do not check that nothing else is. Survivors, each with the whole node suite green: W2, `nr-status` added to the links box, so `opacity: .75` dims the links (A17); W3, the links box renamed `nr-sources`, so the links show the default blue (A13, A14); W4, an inline `style` with no underline and no outline on the link (A14); W5, `style="order:-1"` on the details, so the cover leaves the first column (U66); W8, `style="width:50%"` on Ignore (A11); W9, `class="nr-badge"` on Restore (A12); W10, `disabled` on the Artist field (A5); W12, an inline `object-fit:fill` on the cover (U61); W13, `class="nr-empty"` on `#nr-panel`, which dims the panel (A18); V4, `panel.setAttribute('style', 'opacity:.3')` (A18). An attribute repeated in capital letters before the tested one also survives, because `imgAttribute` matches case-sensitively and the browser keeps the first of two duplicates: W6 `LOADING="eager"` (U50), W7 `REFERRERPOLICY="unsafe-url"` (U51), W11 `ALT="Cover"` (A10). **Should assert**: for each element template that `row()` and `cover()` write, and for the static controls, the exact set of attribute names (lower case, no duplicates) and the exact class list; and no `style` attribute anywhere, written or static | `tests/web/render.test.js:205-242`; `tests/web/cover-markup.js`; `tests/web/artist-filter.test.js` A5 |
| 4 | MED | **Maintainer decision: extend the closed world to the markup, or record the fake DOM's limit.** The profile already says that an assertion on `innerHTML` proves what the page wrote, not what a browser renders. Findings 2 and 3 are of that kind. One option extends the T069 principle: exact attribute sets, exact class lists and one stylesheet, all read through the loader and the text reads the profile permits. The other option records these forms as out of the hermetic suite's reach and sends them to `quickstart.md` §2. The rubric fails surviving mutants inside `DONE` behaviours either way, so the second option needs a rubric override row as well | Findings 2, 3; `.specify/memory/tdd-profile.md` "What it does not" |
| 5 | LOW | **Four CSS readers treat at-rules four ways.** `rules()` keeps `@media` rules without their condition. `declarations()` removes `@media` blocks with a regex. `stylesheet()` keeps the innermost condition. `narrowScreen()` finds the block by text. None of them reads `@supports` or `@layer`. T057 had made one parser. `dimsText`'s new ceiling comment also omits `calc()` and `var()`, which read as "does not dim" | `tests/web/styles.test.js:17, 31, 39, 168`, `:331-338` |
| 6 | LOW | **The commit map leaves cycle 66 unnamed.** It reads "this entry's commit". The commit is `78b0f4b` | `tdd/cycle-log.md` "Task link and commits of the fourth remediation" |
| 7 | LOW | **T069's done-check was reinterpreted by the loop.** The task said K1 passes `styles.test.js`. The decision entry says K1 is "read as" failing no predicate-based test. That sentence sits under "Decision (maintainer)", but the maintainer's words were only "go ahead with the closed-world check". The reinterpretation is sound, because a closed world must fail a stylesheet change. The maintainer has not confirmed it | `tdd/cycle-log.md` "Maintainer decision on the fourth TDD audit"; `tasks.md` T069 |

**Suite properties.** Both suites are fast: node takes 0.2 s and dotnet takes 10 s. Both are
deterministic and isolated. U67 is deliberately sensitive to syntax. Reordering declarations or
writing `#fff` for `#ffffff` fails it, and its `deepEqual` diff names the rule. That cost is
acceptable for a closed world, but only once the review in Finding 1 is recorded. U67 and the
predicate tests complement each other. U67 catches any change. The predicates catch a reviewed but
wrong update to the list. The smell pass found no redundant, foreign-style or bypassed-helper test.

## Mutation results

No mutation tool (profile: `mutation: null`). The scratch runner applied each mutant to a file copy,
one at a time. Each literal replacement had to match exactly once. It ran the behaviour's test file,
or the whole node suite for W* and V*, and copied the file back. It checked each restore against
`HEAD` with `git diff --quiet`. After the run, `git status` was clean, and both full suites were green
(dotnet 318, node 254).

Sample:

- The 58 mutants of the fourth audit, re-run at `78b0f4b`.
- W1–W13, designed by a fresh-context subagent that edited only scratch copies outside the
  repository.
- V1–V4, from the fresh-context smell pass.

Each one was re-run here on the real tree.

| Mutant | Behavior | Survived | Judgment |
| --- | --- | --- | --- |
| R*, N*, P*, Q*, S*, X*, C*, M*, E2 (56 mutants) | A3–A18, U33–U36, U45, U57, U59–U66 | No | All caught; every stylesheet mutant fails U67 |
| E1 `{ capture: true }` | U57 (control) | Passes | Equivalent registration |
| K1 `outline: 2px solid rgb(82 181 0)` | A14 (control) | Fails U67 only | Closed world as decided; no predicate fails correct CSS |
| **W1** second `<style>` with `.nr-links a { text-decoration: none }` | A14 | **Yes** | **Real defect. Finding 2** |
| **V1** link colour inside `@supports not (color: red)` | A13, A16 | **Yes** | **Real defect: default link blue. Finding 2** |
| **V2** `:focus-visible` inside `@layer x` | A14 | **Yes** | **Real defect: the theme's outline wins. Finding 2** |
| **V3** the `600px` rule inside `@media print` | U59 | **Yes** | **Real defect: no narrow-screen layout. Finding 2** |
| **W2** `nr-status` added to the links box | A17 | **Yes** | **Real defect: links at `opacity: .75`. Finding 3** |
| **W3** links box renamed `nr-sources` | A13, A14 | **Yes** | **Real defect: default link blue. Finding 3** |
| **W4** inline `text-decoration:none;outline:none` on the link | A14 | **Yes** | **Real defect. Finding 3** |
| **W5** `style="order:-1"` on the details | U66 | **Yes** | **Real defect: the cover is not first. Finding 3** |
| **W8** `style="width:50%"` on Ignore | A11 | **Yes** | **Real defect: unequal buttons. Finding 3** |
| **W9** `class="nr-badge"` on Restore | A12 | **Yes** | **Real defect: Restore styled as a badge. Finding 3** |
| **W10** `disabled` on the Artist field | A5 | **Yes** | **Real defect: no typing. Finding 3** |
| **W12** inline `object-fit:fill` on the cover | U61 | **Yes** | **Real defect: stretched cover. Finding 3** |
| **W13** `class="nr-empty"` on `#nr-panel` | A18 | **Yes** | **Real defect: dimmed panel. Finding 3** |
| **V4** `panel.setAttribute('style', 'opacity:.3')` | A18 | **Yes** | **Real defect: dimmed panel. Finding 3** |
| **W6** `LOADING="eager"` before `loading="lazy"` | U50 | **Yes** | **Real defect: eager load. Finding 3** |
| **W7** `REFERRERPOLICY="unsafe-url"` before the tested one | U51 | **Yes** | **Real defect: referrer sent. Finding 3** |
| **W11** `ALT="Cover"` before `alt=""` | A10 | **Yes** | **Real defect: cover announced. Finding 3** |

One more candidate was judged equivalent and is not counted. Removing `e.preventDefault()` from the
form's `submit` handler changes nothing, because the form has no submit button and several fields
block implicit submission.

Of the 75 mutants, 56 were caught and 17 survived inside a behaviour. Both controls behaved as
intended.

## Traceability

| Criterion | Tests | End to end |
| --- | --- | --- |
| US1-AS1 (FR-001) | A1, U44 | Yes: real page script through `load-page.js` |
| US1-AS2 (FR-002, FR-005) | A2, A15, U42, U46, U65 | Yes |
| US1-AS3 (FR-003) | A3 (two tests) | Yes |
| US1-AS4 (FR-002) | A4 (6 rows, typed and left), U64 | Yes |
| US1-AS5 (FR-004) | A5 (two tests) | Partly: keyboard operation is the browser's; extra attributes escape the markup test (Finding 3) |
| US2-AS1 (FR-006, FR-006a) | A7, U31–U37, U48, U52, U66, U67 | Yes for the data; the card's column order has a markup gap (Finding 3) |
| US2-AS2 (FR-006a) | A8, U49, U55, U57 | Yes, within the fake DOM's limits |
| US2-AS3 (FR-007) | A9, U53, U56, U60, U67 | Yes |
| US2-AS4 | A10 | Yes, but a duplicate attribute escapes (Finding 3) |
| US3-AS1 (FR-009, SC-003) | A11, U59, U67 | Declarations only; inline styles and at-rule wrappers escape (Findings 2, 3) |
| US3-AS2 (FR-009) | A12 | Partial: an extra class escapes (Finding 3) |
| US4-AS1 (FR-010, SC-004) | A13, U62, U63, U67 | Declarations only; a renamed class or `@supports` escapes (Findings 2, 3) |
| US4-AS2 (FR-010) | A14, A16, A17, A18, U67 | Partial: Findings 2, 3 |

FR-007a (U50), FR-008 (U51) and FR-011 (U36 and the unchanged suite) are covered as before. U50 and
U51 have the duplicate-attribute gap (Finding 3).

Untested criteria: none. No test traces to nothing. U67 traces to FR-007, FR-009 and FR-010.

## What was not audited

- **Independence**: this session wrote cycles 64–66 and the T069 decision entry. A fresh-context
  subagent did the smell pass, and another designed W1–W13 on scratch copies. This session re-ran
  every mutant on the real tree and opened every cited line. The verdict and severities were decided
  here. All agents are the same model family.
- **Mutation score**: the profile has no mutation tool. 75 deliberate mutants are a sample. They aim
  at the page and its stylesheet. The artist-filter logic, the render data paths and the C# code were
  sampled only through their recorded mutants.
- **Mutants P5–P8, N11 and N12**: their code and tests have not changed since the second audit.
- **Coverage**: unavailable for the `node:vm` sandbox and for dotnet (`coverage: null`).
- **Cycles 2–63** against history: done by earlier audits, not repeated.
- **Real-browser behaviour**: rendering, layout, contrast on screen, keyboard use, lazy-load timing.
  These belong to the maintainer's pass in `quickstart.md` §2.
- **Case folding beyond ASCII**, **the card background `#1c1c1c`** against a running theme, and
  **performance** for more than 1,000 artists: no criterion or measurement.
