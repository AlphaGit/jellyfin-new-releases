---
feature: 007-user-view-polish
verdict: FAIL
standard: .specify/templates/overrides/tdd-test-quality-rubric.md # project override of the extension rubric (TEST_AFTER_ACCEPTED row)
profile: .specify/memory/tdd-profile.md
verified_at: dbab71c
previous_audit: 78b0f4b (FAIL)
behaviors: 52 # 86 on the list, 34 DROPPED
proven: 36
likely: 3
test_after: 0
test_after_accepted: 13
no_test: 0
dropped: 34
high_smells: 3
criteria_total: 13
criteria_covered: 13 # with gaps in US1-AS3, US1-AS5, US2-AS1, US3-AS1 and US4, see Findings 2 and 3
mutation_score: unmeasured # profile records mutation: null
deliberate_mutants: 89 applied; 74 caught; 10 survived inside a 007 behaviour; 3 survived in pre-007 behaviours; 2 controls behave as intended # scope: user-view.html, ReleasesController.cs, styles.test.js
suite: 318 passed, 0 failed, 10 s (dotnet) + 286 passed, 0 failed, 0.3 s (node; also under LANG=de_DE.UTF-8)
independent: no # this session wrote cycles 67–68 and the refactor; the smell pass and the new mutants came from fresh-context subagents and were re-verified here
---

# TDD Verification: Polish the New Releases view

**Verdict: FAIL.** Ten deliberate mutants survive inside 007 behaviours, and U68's two lists have no
recorded review. Two examples: wrapping the actions in one more `<div>` disables the narrow-screen
layout (U59), and `panel.setAttribute('class', 'nr-status')` dims every source link (A17). Every test
stays green for both.

This is the sixth audit. The fifth (`78b0f4b`) gave FAIL with 17 survivors. The remediation closed all
of them: W1–W13 and V1–V4 fail today. Every test-first class holds.

The ten survivors come from two approximations inside U68, not from a new kind of CSS or markup:

- **`WRITTEN` is a pooled set of distinct shapes.** A change that keeps the set unchanged escapes:
  - reordered elements;
  - a duplicated element;
  - an extra wrapper of a shape already allowed;
  - a template that no fixture renders.
- **The runtime check reads only `style`, on declared elements, after the first load.** A class,
  `list` or `hidden` written at runtime escapes.

Both gaps sit inside what the fake DOM models: the markup string, `setAttribute`, and `hidden`. So the
hermetic suite can close them. Three more survivors lie in behaviours that predate 007, where the fake
DOM does not model the event (Finding 5).

## Test-first evidence

Cycles 2–66 were checked by the earlier audits. This audit re-read the decision entry, cycles 67 and
68, the refactor, the commit map and the full diff `78b0f4b..dbab71c`. No file under `src/` changed.
The docs commit `4a68406` touched `styles.test.js` only to add `// 007` comments. Each cycle records a
red that its diff explains. Cycle 68's reds are on the `signatures` and `imgAttribute` table rows, then
on the empty `STATIC` and `WRITTEN` lists.

| Behavior | Class | Evidence |
| --- | --- | --- |
| A1, A2, A4, A10, A11, A13, A15 | PROVEN | cycles 2, 3, 49, 23/33, 42, 47, 51 |
| A3, A5, A12, A14, A16, A17, A18, U33, U34, U36, U62, U64, U65 | TEST_AFTER_ACCEPTED | each labelled, accepted 2026-10-03, and a recorded mutant caught today |
| A7, A8, A9 | LIKELY | reds recorded; history cannot show the order |
| U31, U32, U35, U37–U39, U42, U44–U61, U63, U66 | PROVEN | as in the fifth audit |
| U67 | PROVEN | cycle 64 red; corrected with reds in cycle 67 (`8edb9c0`); its list review is recorded (T072) |
| **U68** | **PROVEN** | cycle 68 reds on the helper tables and on the empty lists; `3af8788`. Its style-attribute test passed on its first run, with V4 as evidence. The lists are Finding 1 |
| A6, U1–U30, U40, U41, U43 | DROPPED | removed by the 2026-10-03 clarification |

**Existing tests changed since `78b0f4b`.**

| Test | Before | After | Judgment |
| --- | --- | --- | --- |
| `cover-markup.js:15-19` `imgAttribute` | case-sensitive name; first match | names without case; first match | Stronger. It only finds more matches, and no test asserted `undefined` against page output. W6, W7 and W11 now also fail U50, U51 and A10 |
| `styles.test.js:16-55` readers | four readers | `blocks()` behind `rules()`, `declarations(selector, media)` and `stylesheet()`; `narrowScreen()` removed | Refactor. The current stylesheet gives the same results. `rules()` now collapses whitespace inside a selector, so it matches more rules, not fewer |
| `styles.test.js` U59 | `declarations('.nr-actions', narrowScreen())` | `declarations('.nr-actions', '@media (max-width: 600px)')` | Equivalent |

No test was skipped, renamed out of a filter, or excluded. **`tasks.md` against the list**: every
ticked task's ids are `DONE`, and no task is open.

## Findings

| # | Severity | Finding | Evidence |
| --- | --- | --- | --- |
| 1 | HIGH | **U68's lists have no recorded review.** `STATIC` (39 elements) and `WRITTEN` (24 shapes) were written and accepted in one step: "Both matched on the first run". The cycle log asks for one review ("The maintainer should read them once"). T072 recorded a review of U67's list only. **Should**: a dated cycle-log entry that records the maintainer's review of both lists at a named commit | `tests/web/markup.test.js:57`, `:92`; `tdd/cycle-log.md` cycle 68 "review" |
| 2 | HIGH | **`WRITTEN` pools every template into one set of distinct shapes.** Survivors (whole node suite green): Y1, the cover moved inside the details box, so it is no longer the first grid column (U66, US2-AS1); Z1, a second cover box at the end of each row (US2-AS1); Y2, the actions wrapped in an extra `<div>`, so the `600px` rule's `grid-column` no longer applies (U59); Z2, the links wrapped in a `span.nr-badge`, so they sit on `#3a3a3a`, which gives 3.98:1 (A16, A18); G1, `style="opacity:.3"` on the "Could not load" message; G2, a styled `<span>` in "The Archive is empty". G1 and G2 lie in templates that no fixture renders, which contradicts the file's header ("An inline style … fails here"). **Should assert**: the ordered list of shapes for each template, with every `panel.innerHTML` branch rendered, the error branch included | `tests/web/markup.test.js:12-14`, `:124-144` |
| 3 | HIGH | **Runtime writes to the declared elements escape, except `style`.** The test reads `getAttribute('style')` once, after the first load. Survivors: Y3, `filters.artist.setAttribute('list', '')`, which unbinds the suggestion list (A5, FR-001); Y6, `#nr-f-clear.hidden = true` (A3); Y7, `panel.setAttribute('class', 'nr-status')`, which dims every link (A17, A18); Y10, `panel.hidden = true` after each render, so no card is ever visible. The test name, "no element of the view carries a style attribute", claims more than the test checks. **Should assert**: after the first load, a tab switch, a filter change, Clear, and a render, each declared element's attributes and `hidden` equal a reviewed state. Name the test for that scope | `tests/web/markup.test.js:146-153` |
| 4 | MED | **`signatures()` skips some tags without a failure.** An unquoted or single-quoted attribute value makes the outer pattern fail, so the tag gets no shape. The doc comment says "Every opening tag". G3 (`<span class=nr-badge style=opacity:.3>`) escaped U68 and was caught by an unrelated render test only. The table has no row for unquoted or single-quoted values, comments, `<template>`, self-closing tags, or a duplicate `class` | `tests/web/markup.test.js:20-41` |
| 5 | MED | **Three survivors lie in behaviours that predate 007.** Y4, `{ once: true }` on the filters' `change` listeners. Y8, the action click handler reads `data-id` from `.nr-actions`. Y9, each source link's `href` set to the source name. The fake DOM ignores listener options other than on `error`, has no `closest`, and U68 ignores attribute values. These are not on 007's test list. The project rule sends a defect found outside a feature's scope to a new spec | `src/Jellyfin.Plugin.NewReleases/Web/user-view.html:256, 275, 198`; profile "What it does not" |
| 6 | MED | **The `imgAttribute` table sits in `markup.test.js` under "U68 helper", but U68 does not use `imgAttribute`.** The profile asks for one file per subject. Its users are `render.test.js` and `cover-fallback.test.js` | `tests/web/markup.test.js:43-52` |
| 7 | LOW | **Duplicated setup.** `settled` is now in three files (`markup.test.js:18`, `artist-filter.test.js:22`, `requests.test.js:28`). `panelFor` copies `render.test.js:19` `rendered`. The two-route `ApiClient` stub appears twice in `markup.test.js` | as cited |

One probe candidate was judged equivalent: removing `e.preventDefault()` from the form's `submit`
handler (Y5). The form has no submit button and three fields that block implicit submission (one text,
two date), so Enter never submits it. The fifth audit reached the same judgment.

**Suite properties.** Both suites are fast and deterministic. The node suite takes 0.3 s and dotnet
takes 10 s. Specificity is weaker in U68. A set difference does not name the template that changed,
and pooling hides where a shape came from. `STATIC` is ordered and exact, so it catches a reordered,
added or removed static element. It cannot see attribute values or runtime writes.

## Mutation results

No mutation tool (profile: `mutation: null`). The scratch runner applied each mutant to a file copy,
one at a time. Each literal replacement had to match exactly once. It ran the behaviour's test file,
or the whole node suite for page mutants, then restored the file and checked it against `HEAD`.

The 75 mutants of the fifth audit ran at `2a7c0c0`, which has the same source and test files as
`dbab71c`. All of them failed their tests, except the control E1, which passes as it should. K1 fails
U67 only. The 14 new mutants ran at `dbab71c`. After the run, `git status` was clean and both suites
were green.

| Mutant | Behavior | Survived | Judgment |
| --- | --- | --- | --- |
| R*, N*, P*, Q*, S*, X*, C*, M1–M4, E2, W1–W13, V1–V4 (73) | 007 behaviours | No | All caught |
| E1, K1 | controls | as intended | E1 passes; K1 fails U67 only |
| G3 unquoted `class=nr-badge style=opacity:.3` on the type badge | U68 | No | Caught by "a rendered row carries…", not by U68 (Finding 4) |
| **Y1** cover moved inside the details box | U66 | **Yes** | **Real defect. Finding 2** |
| **Z1** a second cover box per row | US2-AS1 | **Yes** | **Real defect. Finding 2** |
| **Y2** extra wrapper around the actions | U59 | **Yes** | **Real defect. Finding 2** |
| **Z2** links inside `span.nr-badge` | A16, A18 | **Yes** | **Real defect: 3.98:1. Finding 2** |
| **G1** `style="opacity:.3"` on "Could not load" | U68 | **Yes** | **Real defect in a template no fixture renders. Finding 2** |
| **G2** styled `<span>` in "The Archive is empty" | U68 | **Yes** | **Same. Finding 2** |
| **Y3** `setAttribute('list', '')` on the Artist field | A5 | **Yes** | **Real defect: no suggestions. Finding 3** |
| **Y6** Clear button hidden at runtime | A3 | **Yes** | **Real defect. Finding 3** |
| **Y7** `panel.setAttribute('class', 'nr-status')` | A17, A18 | **Yes** | **Real defect: dimmed links. Finding 3** |
| **Y10** `panel.hidden = true` after render | US1–US4 | **Yes** | **Real defect: nothing visible. Finding 3** |
| Y4 `{ once: true }` on `change` | pre-007 filters | Yes | Outside 007's list. Finding 5 |
| Y8 click handler reads `.nr-actions` | pre-007 actions | Yes | Outside 007's list. Finding 5 |
| Y9 source `href` set to the source name | pre-007 links | Yes | Outside 007's list. Finding 5 |

Of the 89 mutants, 74 were caught, 10 survived inside a 007 behaviour, and 3 survived in behaviours
that predate 007. Both controls behaved as intended.

## Traceability

| Criterion | Tests | End to end |
| --- | --- | --- |
| US1-AS1 (FR-001) | A1, U44, U68 | Yes; a runtime `list` change escapes (Finding 3) |
| US1-AS2 (FR-002, FR-005) | A2, A15, U42, U46, U65 | Yes |
| US1-AS3 (FR-003) | A3 | Yes; a hidden Clear escapes (Finding 3) |
| US1-AS4 (FR-002) | A4, U64 | Yes |
| US1-AS5 (FR-004) | A5, U68 | Partly: keyboard use is the browser's; Finding 3 |
| US2-AS1 (FR-006, FR-006a) | A7, U31–U37, U48, U52, U66, U67, U68 | Yes for the data; card structure has gaps (Finding 2) |
| US2-AS2 (FR-006a) | A8, U49, U55, U57 | Yes, within the fake DOM's limits |
| US2-AS3 (FR-007) | A9, U53, U56, U60, U67 | Yes |
| US2-AS4 | A10, U68 | Yes |
| US3-AS1 (FR-009, SC-003) | A11, U59, U67, U68 | Declarations; a wrapper escapes (Finding 2) |
| US3-AS2 (FR-009) | A12, U68 | Yes |
| US4-AS1 (FR-010, SC-004) | A13, U62, U63, U67 | Declarations; Finding 2 (Z2) |
| US4-AS2 (FR-010) | A14, A16, A17, A18, U67, U68 | Partial: Findings 2, 3 |

Untested criteria: none. No test traces to nothing.

## What was not audited

- **Independence**: this session wrote cycles 67–68, the refactor and the decision entry. A
  fresh-context subagent did the smell pass, and another designed Y1–Y10 and Z1–Z2 on scratch copies.
  This session re-ran every mutant on the real tree and opened every cited line. The verdict, the
  severities and the Y5 judgment were decided here. All agents are the same model family.
- **Mutation score**: no mutation tool. 89 deliberate mutants are a sample, aimed at the page. The
  artist-filter logic, the render data paths and the C# code were sampled through their recorded
  mutants only.
- **Mutants P5–P8, N11 and N12**: their code and tests have not changed since the second audit.
- **Coverage**: unavailable for the `node:vm` sandbox and for dotnet.
- **Behaviours that predate 007**: probed only where a probe found them (Finding 5), not audited.
- **Cycles 2–66** against history: done by earlier audits.
- **Real-browser behaviour**: rendering, layout, contrast on screen, keyboard use and lazy-load
  timing belong to `quickstart.md` §2.
