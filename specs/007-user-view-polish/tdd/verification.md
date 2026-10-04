---
feature: 007-user-view-polish
verdict: FAIL
standard: .specify/templates/overrides/tdd-test-quality-rubric.md # project override of the extension rubric (TEST_AFTER_ACCEPTED row)
profile: .specify/memory/tdd-profile.md
verified_at: 093d2a1
previous_audit: 3f9061f (FAIL)
behaviors: 72 # 106 on the list, 34 DROPPED
proven: 37
likely: 3
test_after: 0
test_after_accepted: 13
no_test: 0
not_applicable: 19 # U70–U88, characterization (BASELINE) of behaviour that predates 007
dropped: 34
high_smells: 0
criteria_total: 13
criteria_covered: 13 # US1-AS4 with one gap, see Finding 1
mutation_score: unmeasured # profile records mutation: null
deliberate_mutants: 122 run under the stopping rule (121 recorded + 1 probe survivor); 117 caught; 3 survived inside a 007 behaviour (H5, J3 by the maintainer's scope decisions; P1 new); 2 controls behave as intended # scope: user-view.html, ReleasesController.cs, ReleaseRepository.cs, styles.test.js, fake-dom.js
stopping_rule: applied # recorded mutants re-run + one probe round limited to 007's acceptance criteria (maintainer, 2026-10-04)
suite: 318 passed, 0 failed, 10 s (dotnet) + 354 passed, 0 failed, 0.3 s (node; also under LANG=de_DE.UTF-8)
independent: no # this session wrote cycles 76–81 and the tidying; the smell pass and the probe round came from fresh-context subagents and were re-verified here
---

# TDD Verification: Polish the New Releases view

**Verdict: FAIL.** The stopping rule's one probe round found one gap inside 007. In page terms:
someone picks "ASP", then deletes one letter so the field shows "AS". The list keeps showing only ASP's
releases until they leave the field. FR-002 says text that matches no artist must not filter the list,
and A4 says so "while typing". One test closes it.

Two more breaks still pass the tests, and the list still claims both cases:
- **H5:** lower-casing that depends on the system language (Turkish).
- **J3:** the page restyling itself after an action click.

In the grilling session, the maintainer chose not to test these two (Q2, Q3). The list was never
narrowed to match. That is a one-line record in the list, not a test.

Everything else holds:
- No `HIGH` smell.
- Every test-first class holds.
- The 117 recorded breaks are caught again, including all of groups A, B and C.
- The probe round tried 10 changes a developer might make to the covers, the fallback and the Artist
  filter. The tests caught every one except P1. One C# change was equivalent: Deezer sorts first in
  the alphabet anyway.

## Test-first evidence

This audit re-read the decision and grilling entries, cycles 76–81, the tidy entries, the commit map
and the diff `3f9061f..093d2a1`. No file under `src/` changed. Each commit changes only test files and
the feature's documents, as its entry says.

| Behavior | Class | Evidence |
| --- | --- | --- |
| A1, A2, A4, A10, A11, A13, A15 | PROVEN | as in earlier audits; A1, A2 and A15 gained examples in cycle 76, shown by H1–H3 |
| A3, A5, A12, A14, A16, A17, A18, U33, U34, U36, U62, U64, U65 | TEST_AFTER_ACCEPTED | labelled, accepted, recorded break caught today. A5 is stricter since cycle 77 (H4, R4) |
| A7, A8, A9 | LIKELY | reds recorded; history cannot show the order |
| U31, U32, U35, U37–U39, U42, U44–U61, U63, U66–U69 | PROVEN | as in earlier audits; U69's table gained the missing case in cycle 78 (J5) |
| U70–U88 | NOT_APPLICABLE | characterization (`BASELINE`) of behaviour that predates 007, per T083 and T091. Each is shown to catch one hand-made break: Y4, Y8, Y9, J4, H6–H10, J1, B74a–B88 |
| A6, U1–U30, U40, U41, U43 | DROPPED | removed by the 2026-10-03 clarification |

**Existing tests changed since `3f9061f`.**

| Test | Before | After | Judgment |
| --- | --- | --- | --- |
| `artist-filter.test.js` A5 | pattern allowing any attribute after `list` | the contract's markup, character for character | Stronger |
| `requests.test.js` U72 | one action | one test per action, plus a click outside a button | Stronger |
| `fake-dom.test.js` `closest` | one combined assertion | one row per selector, plus the missing case | Stronger |
| `render.test.js` U83 | found labels through `rel="noopener"` | through `target="_blank"` | Equivalent; decoupled from U88 |
| `cover-fallback.test.js` `capturesEveryError` | its own `once`/`signal` rule | uses the shared `keepsListening` | Equivalent; U57's table intact |
| U71 | in `requests.test.js` | in `view.test.js` | Moved, unchanged |

No test was skipped, renamed out of a filter, or excluded. **`tasks.md`**: every ticked task's ids are
`DONE` or `BASELINE`, and no task is open. T089 is ticked with its "dropped" note, as decided.

## Findings

| # | Severity | Finding | Evidence |
| --- | --- | --- | --- |
| 1 | HIGH | **Editing an applied artist name keeps the old filter until the field is left.** P1 changes the Artist field's typing handler so that it reloads only when the text names an artist or is empty, and it survives. In page terms: pick "ASP", delete a letter, and the list stays filtered to ASP while the field shows "AS". A4 says text that names no artist applies no filter "while typing", and FR-002 says it "MUST NOT filter the list". Today the page reloads the full list at once. No test types a non-matching text after an artist is applied: A4's rows start from no artist, and A3 covers only an empty field. **Should assert**: with ASP applied, typing "AS" asks for the full list right away, with no `change` event | `tests/web/artist-filter.test.js` A3, A4; `user-view.html:259` |
| 2 | HIGH | **The list still claims two cases the maintainer chose not to test.** H5 (lower-casing by system language) survives inside A15. J3 (a style written after an action click) survives inside U68. The grilling session dropped both (Q2, Q3), but A15 and U68 still state them in full, and the rubric fails a survivor inside a `DONE` behaviour. **Should**: record the decisions in the list: A15 is pinned in the suite's English locale, and U68's runtime check covers load, both tabs, a filter change, Clear and a render. The other route is a rubric override row for accepted survivors, as on 2026-10-01 for test-after | `tdd/test-list.md` A15 and U68 rows; `tdd/cycle-log.md` grilling session Q2, Q3 |
| 3 | MED | **U85 reads the "selected on open" state from the source text, not from the page after it loads.** A start-up script that set the List tab to not selected would pass | `tests/web/view.test.js:33-48` |
| 4 | MED | **Two tests check two things each.** U81 checks the Upcoming badge and the Ignored badge together, and a missing Upcoming badge gives a `TypeError`, not a diff. U87 checks label pairing and the date fields together. The maintainer asked for one plain test per behaviour | `tests/web/render.test.js:276-284`; `tests/web/view.test.js:54-62` |
| 5 | MED | **Two tests sit in files about another subject.** U84 (the status line after an action) is in `requests.test.js`, which is about the paths the page sends. The `keepsListening` table is in `view.test.js`, but the helper now lives in `fake-dom.js`, whose contract `fake-dom.test.js` holds | `tests/web/requests.test.js:144-153`; `tests/web/view.test.js:66-81` |
| 6 | LOW | **The 1,000-artist test's name says "every name is suggested", but it checks the count and the last name only** | `tests/web/artist-filter.test.js:138-146` |
| 7 | LOW | **Small repetitions.** The U72 rows set up the page by hand where `loadedView` already does it. U77 repeats U70's link reader. `view.test.js` adds a fourth copy of the page-source read | `tests/web/requests.test.js:66-82`; `tests/web/render.test.js:228-245`; `tests/web/view.test.js:14` |

**Suite properties.** Fast and deterministic: node takes 0.3 s and dotnet takes 10 s. Failures name
what broke, except U81's `TypeError` path. The main refactor risk is that several tests find a link or
a button by attribute order (U70, U77, U83, U88, U80). Reordering the attributes harmlessly would fail
four or five tests at once.

## Mutation results

No mutation tool (profile: `mutation: null`). Stopping rule: every recorded break is re-run, plus one
probe round limited to 007's acceptance criteria. The runner applied each break to a file copy, one at
a time, ran the whole page-side suite (or the C# filter), restored the file and checked it against
`HEAD`. Afterwards `git status` was clean and both suites were green.

| Mutant | Behavior | Survived | Judgment |
| --- | --- | --- | --- |
| 117 recorded breaks (R*, N*, P*, Q*, S*, X*, C*, M*, E2, W*, V*, Y*, Z*, G*, H1–H4, H6–H10, J1, J4, J5, B74a–B88) | 007 and characterization behaviours | No | All caught |
| E1, K1 | controls | as intended | E1 passes; K1 fails U67 only |
| **P1** typing handler reloads only for an artist or an empty field | A4, FR-002 | **Yes** | **Real defect: the old filter stays while the field shows non-matching text. Finding 1** |
| H5 locale-aware lower-casing | A15 | Yes | Dropped by the maintainer (Q2); the list still claims it. Finding 2 |
| J3 a style written on the panel after an action click | U68 | Yes | Dropped by the maintainer (Q3); the list still claims it. Finding 2 |

The probe round's other candidates:

- **Caught (page):**
  - no covers in the Archive tab;
  - the fallback list never shortened;
  - the Cover Art Archive fallback dropped;
  - the image removed while one fallback remains;
  - the last failed image left in place;
  - no `change` listener on the Artist field;
  - the tag compared as `img`.
- **C# (judged, not run):**
  - the cover's source id replaced by the URL, which the exact-URL tests would catch;
  - Deezer ordered first by the alphabet instead of by rule, which is equivalent, because "deezer"
    sorts before "musicbrainz".

## Traceability

| Criterion | Tests | End to end |
| --- | --- | --- |
| US1-AS1 (FR-001) | A1, U44, U68, 1,000-artist test | Yes |
| US1-AS2 (FR-002, FR-005) | A2, A15, U42, U46, U65 | Yes |
| US1-AS3 (FR-003) | A3, U68 | Yes |
| US1-AS4 (FR-002) | A4, U64 | Yes, except editing an applied name (Finding 1) |
| US1-AS5 (FR-004) | A5, U68 | Partly: keyboard use is the browser's |
| US2-AS1 (FR-006, FR-006a) | A7, U31–U37, U48, U52, U66, U67, U68 | Yes |
| US2-AS2 (FR-006a) | A8, U49, U55, U57 | Yes, within the fake DOM's limits |
| US2-AS3 (FR-007) | A9, U53, U56, U60, U67 | Yes |
| US2-AS4 | A10, U68 | Yes |
| US3-AS1 (FR-009, SC-003) | A11, U59, U67, U68 | Declarations; pixels are the real-browser pass |
| US3-AS2 (FR-009) | A12, U68 | Yes |
| US4-AS1 (FR-010, SC-004) | A13, U62, U63, U67, U68 | Declarations; rendered contrast is the real-browser pass |
| US4-AS2 (FR-010) | A14, A16, A17, A18, U67, U68 | Yes |

FR-011 (existing behaviour unchanged) is covered by U36 and U70–U88. Untested criteria: none. No test
traces to nothing.

## What was not audited

- **Independence**: this session wrote cycles 76–81 and the tidying. Fresh-context subagents did the
  smell pass and the probe round. This session re-ran every counted break on the real tree and opened
  every cited line. It corrected the smell pass's line numbers for `view.test.js`. All agents are the
  same model family.
- **Beyond the stopping rule**: no adversarial search outside 007's acceptance criteria, by the
  maintainer's decision. Behaviour outside them is covered only as far as U70–U88 go.
- **C# breaks in the probe round**: judged from the tests, not run.
- **Coverage**: unavailable for the `node:vm` sandbox and for dotnet.
- **Real-browser behaviour**: rendering, layout, contrast on screen, keyboard use and datalist matching
  belong to `quickstart.md` §2.
