---
feature: 007-user-view-polish
verdict: PASS_WITH_GAPS
standard: .specify/templates/overrides/tdd-test-quality-rubric.md # project override of the extension rubric (TEST_AFTER_ACCEPTED row)
profile: .specify/memory/tdd-profile.md
verified_at: 457bd78
previous_audit: 093d2a1 (FAIL)
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
criteria_covered: 13
mutation_score: unmeasured # profile records mutation: null; deliberate breaks only
deliberate_mutants: 133 run under the stopping rule (123 recorded + 10 probe-round); 128 caught; 0 survived inside a 007 behaviour; 2 outside the behaviours as scoped (H5, J3); 2 controls behave as intended; 1 probe candidate equivalent # scope: user-view.html, ReleasesController.cs, ReleaseRepository.cs, styles.test.js, fake-dom.js
stopping_rule: applied, condition met # recorded breaks re-run + one probe round limited to 007's acceptance criteria found no survivor inside 007
suite: 318 passed, 0 failed, 10 s (dotnet) + 358 passed, 0 failed, 0.2 s (node; also under LANG=de_DE.UTF-8)
independent: no # this session wrote the tests since cycle 64; the smell pass and the probe round came from fresh-context subagents and were re-verified here
---

# TDD Verification: Polish the New Releases view

**Verdict: PASS_WITH_GAPS.** Every acceptance criterion has a test that a hand-made break of its
behaviour fails. The maintainer's stopping rule is met: the recorded breaks were re-run, and one probe
round limited to 007's acceptance criteria found nothing. There is no `HIGH` or `MED` smell. The gaps
are in the evidence, not in the tests:

- 13 behaviours are test-after, each with the maintainer's acceptance on record.
- 3 behaviours have reds that history cannot order.
- Test strength rests on 133 deliberate breaks, not on a mutation tool.
- This session wrote the tests it audits.

In page terms, every 007 behaviour now has a test that fails if the behaviour breaks:

- the Artist type-ahead: suggestions; case-blind, accented and big-library matching; editing and
  clearing a pick; and the field as the contract writes it;
- the cover images: their order, the fallback to the next image, the placeholder, lazy loading, and
  no referrer;
- the equal "Ignore" and "Have it" buttons;
- the readable, underlined source link.

The view's older behaviour, which 007 must leave unchanged (FR-011), is pinned by 19
characterization tests:

- the requests each tab and filter sends;
- the sentences the page shows;
- what a screen reader announces.

## Test-first evidence

This audit re-read cycles 82 and 83, the T099 decision, the tidy entries, the commit maps and the diff
`093d2a1..457bd78`. No file under `src/` changed. Each commit changes only test files and the
feature's documents, as its entry says.

| Behavior | Class | Evidence |
| --- | --- | --- |
| A1, A2, A4, A10, A11, A13, A15 | PROVEN | reds recorded in their cycles; later examples (cycles 76, 82) shown by H1–H3 and P1 |
| A3, A5, A12, A14, A16, A17, A18, U33, U34, U36, U62, U64, U65 | TEST_AFTER_ACCEPTED | labelled test-after, the maintainer's dated acceptance recorded, a recorded break caught today |
| A7, A8, A9 | LIKELY | reds recorded; history cannot show the order |
| U31, U32, U35, U37–U39, U42, U44–U61, U63, U66–U69 | PROVEN | reds recorded in their cycles |
| U70–U88 | NOT_APPLICABLE | characterization (`BASELINE`) of behaviour that predates 007; each shown to catch at least one hand-made break |
| A6, U1–U30, U40, U41, U43 | DROPPED | removed by the 2026-10-03 clarification |

A15 and U68 are graded as the list states them since T099: A15 in the suite's English locale, and U68's
runtime check through load, both tabs, a filter change, Clear and a render. The narrowing records the
maintainer's grilling decisions (Q2, Q3), made before the eighth audit.

**Existing tests changed since `093d2a1`.**

| Test | Before | After | Judgment |
| --- | --- | --- | --- |
| A4 | started from no applied artist | also edits an applied artist (`AS`, `ASPx`) | Stronger |
| U85 | opened state read from the markup | read from the loaded page, markup as fallback | Stronger |
| 1,000-artist test | count and last name | all 1,000 names in order | Stronger |
| U81, U87 | two checks each | one check per test | Same checks; U81 now gives a diff, not a `TypeError` |
| U84, `keepsListening` table | in `requests.test.js` and `view.test.js` | in `view.test.js` and `fake-dom.test.js` | Moved, unchanged |
| page-source read, source-link reader, U72 setup | copies | one shared helper each | Same reads and the same setup |

No test was skipped, renamed out of a filter, or excluded. **`tasks.md`**: every ticked task's ids are
`DONE` or `BASELINE`, and no task is open.

## Findings

| # | Severity | Finding | Evidence |
| --- | --- | --- | --- |
| 1 | LOW | **Duplicated setup.** U84, moved by T102, builds the page by hand with the same `loadPageDom(…)` and `settled()` as U85, and `view.test.js` has no file-local setup like `requests.test.js`'s `loadedView` | `tests/web/view.test.js:32`, `:80` |

**Observation outside the tests.** The probe round noted a possible real-world defect, which is not a
test gap. The page does not discard an out-of-date response. If someone types "ASP" and then clears
the field quickly, a slow "ASP" response could arrive last, and the list would show ASP's releases while
the field is empty (US1-AS3). Only real network timing shows this. The tests answer in order. By the
project rule, a defect found outside the suite becomes its own spec.

**Suite properties.** Fast and deterministic: node takes 0.2 s and dotnet takes 10 s. The clock and the
locale are pinned, and each test gets its own sandbox. Failures name what broke: one check per test
where it matters, and one test per template or per action. The remaining refactor risk is known and
accepted: a few tests find a link or a button by attribute order, and the closed-world lists fail on any
reviewed change until they are updated, which is their purpose.

## Mutation results

No mutation tool (profile: `mutation: null`). Stopping rule (maintainer, 2026-10-04): every recorded
break is re-run, plus one probe round limited to 007's acceptance criteria. The runner applied each
break to a file copy, one at a time, ran the whole page-side suite (or the C# filter), restored the file
and checked it against `HEAD`. Afterwards `git status` was clean and both suites were green.

| Group | Breaks | Result |
| --- | --- | --- |
| Recorded breaks (all audits) | 123 | 118 caught. E1 and K1 are controls: E1 passes and K1 fails U67 only, both as intended. H5 and J3 pass, outside A15 and U68 as scoped by T099. P1 is now caught |
| Probe round (this audit) | 10 on the page | 10 caught: Artists failure handling, fallback list update, last-image removal, the no-cover box, recording the applied artist, the typing reload, the tag comparison, Clear, lower-cased names, and the capture-phase listener |
| Probe round, C# (judged from the tests, not run) | 3 | exact cover URLs and source order are pinned; removing `CoversOf`'s `OrderBy` is equivalent, because the repository already returns sources in alphabetical order, so Deezer stays first |

The probe round also judged two candidates equivalent, as earlier audits did. `rest.pop()` and
`rest.shift()` give the same result, because a card has at most one fallback. The form's
`preventDefault` cannot matter, because Enter never submits that form.

## Traceability

| Criterion | Tests | End to end |
| --- | --- | --- |
| US1-AS1 (FR-001) | A1, U44, U68, 1,000-artist test | Yes |
| US1-AS2 (FR-002, FR-005) | A2, A15, U42, U46, U65 | Yes (English locale) |
| US1-AS3 (FR-003) | A3, U68 | Yes |
| US1-AS4 (FR-002) | A4 (typed, left, and edited after a pick), U64 | Yes |
| US1-AS5 (FR-004) | A5, U68 | Partly: keyboard use is the browser's |
| US2-AS1 (FR-006, FR-006a) | A7, U31–U37, U48, U52, U66, U67, U68 | Yes |
| US2-AS2 (FR-006a) | A8, U49, U55, U57 | Yes, within the fake DOM's limits |
| US2-AS3 (FR-007) | A9, U53, U56, U60, U67 | Yes |
| US2-AS4 | A10, U68 | Yes |
| US3-AS1 (FR-009, SC-003) | A11, U59, U67, U68 | Declarations; pixels are the real-browser pass |
| US3-AS2 (FR-009) | A12, U68 | Yes |
| US4-AS1 (FR-010, SC-004) | A13, U62, U63, U67, U68 | Declarations; rendered contrast is the real-browser pass |
| US4-AS2 (FR-010) | A14, A16, A17, A18, U67, U68 | Yes |

FR-007a (U50), FR-008 (U51) and FR-011 (U36, U70–U88) are covered. Untested criteria: none. No test
traces to nothing.

## What was not audited

- **Independence**: this session wrote every test since cycle 64. Fresh-context subagents did the smell
  pass and the probe round. This session re-ran every counted break on the real tree and checked the
  cited lines. All agents are the same model family.
- **Mutation score**: no mutation tool. 133 deliberate breaks are a sample, and by the maintainer's
  stopping rule the probe was limited to 007's acceptance criteria.
- **C# breaks in the probe round**: judged from the tests, not run.
- **Coverage**: unavailable for the `node:vm` sandbox and for dotnet.
- **Real-browser behaviour**: rendering, layout, contrast on screen, keyboard use, datalist matching,
  lazy-load timing, and response timing (the observation above) belong to `quickstart.md` §2.
- **Scope the maintainer excluded**: locale-aware lower-casing (A15) and writes after an action click
  (U68).
