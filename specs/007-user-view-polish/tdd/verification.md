---
feature: 007-user-view-polish
verdict: FAIL
standard: .specify/templates/overrides/tdd-test-quality-rubric.md # project override of the extension rubric (TEST_AFTER_ACCEPTED row)
profile: .specify/memory/tdd-profile.md
verified_at: 3f9061f
previous_audit: dbab71c (FAIL)
behaviors: 56 # 90 on the list, 34 DROPPED
proven: 37
likely: 3
test_after: 0
test_after_accepted: 13
no_test: 0
not_applicable: 3 # U70–U72, characterization (BASELINE)
dropped: 34
high_smells: 4
criteria_total: 13
criteria_covered: 13 # with gaps in US1-AS1, US1-AS2, US1-AS5 and US4-AS2, see Findings 2–5
mutation_score: unmeasured # profile records mutation: null
deliberate_mutants: 103 applied; 88 caught; 8 survived inside 007's behaviours or its test infrastructure; 5 survived in behaviours that predate 007; 2 controls behave as intended # scope: user-view.html, ReleasesController.cs, styles.test.js, fake-dom.js
suite: 318 passed, 0 failed, 10 s (dotnet) + 320 passed, 0 failed, 0.2 s (node; also under LANG=de_DE.UTF-8)
independent: no # this session wrote cycles 69–75 and both refactors; the smell pass and the new mutants came from fresh-context subagents and were re-verified here
---

# TDD Verification: Polish the New Releases view

**Verdict: FAIL.** The lists that cycles 69 and 70 added have no recorded review. Also, eight deliberate
mutants survive inside 007's behaviours or its test infrastructure. For example, `autocomplete="on"` on
the Artist field breaks the contract (`contracts/user-view.md:12`) and lets the browser mix earlier
typed text into the suggestions. Every test stays green.

This is the seventh audit. The sixth (`dbab71c`) gave FAIL with ten survivors inside 007. The
remediation closed all of them: Y1–Y10, Z1–Z2 and G1–G3 fail today, and Y4, Y8 and Y9 are pinned by
characterization tests. Every test-first class holds.

The probes have moved to new ground. No survivor is in the stylesheet or in the shape of the markup
any more. The new ones are:

- **Attribute values**: U68 checks attribute names, not values.
- **Inputs the tests never use**: a non-ASCII name, a list of more than 100 artists, and a locale
  other than English.
- **Writes outside U68's five steps**: for example, after an action click.
- **The stand-in's new `closest`**: one combined assertion leaves its attribute check unpinned.

Five more survivors lie in behaviours that predate 007. The probe listed about twenty more of that
kind (Finding 6).

## Test-first evidence

Cycles 2–68 were checked by the earlier audits. This audit re-read the T079/T083 decision entry,
cycles 69–75, both refactors, the commit map and the diff `dbab71c..3f9061f`. No file under `src/`
changed. Each cycle commit changes only test files and the feature's documents, as its entry says.
Each cycle records a red that its diff explains, except the three characterization cycles. Those
record no red, which is correct for `BASELINE`.

| Behavior | Class | Evidence |
| --- | --- | --- |
| A1, A2, A4, A10, A11, A13, A15 | PROVEN | as in earlier audits |
| A3, A5, A12, A14, A16, A17, A18, U33, U34, U36, U62, U64, U65 | TEST_AFTER_ACCEPTED | labelled, accepted, recorded mutant caught today |
| A7, A8, A9 | LIKELY | reds recorded; history cannot show the order |
| U31, U32, U35, U37–U39, U42, U44–U61, U63, U66, U67 | PROVEN | as in earlier audits |
| U68 | PROVEN | cycle 68 reds; corrected with reds in cycles 69 (empty template lists), 70 (`writesOn` rows) and 71 (`signatures` rows). The runtime test passed on its first run, with Y3, Y6, Y7, Y10, V4 and Y11 as evidence |
| **U69** | **PROVEN** | cycle 72 red (`closest is not a function`, 2 failed); `578e818`. J5 survives (Finding 5) |
| **U70, U71, U72** | **NOT_APPLICABLE** | characterization (`BASELINE`) of code that predates 007, per T083. Each catches its audit mutant (Y9, Y4, Y8). U71's helper table had a red. J4 survives U72 (Finding 5) |
| A6, U1–U30, U40, U41, U43 | DROPPED | removed by the 2026-10-03 clarification |

**Existing tests changed since `dbab71c`.**

| Test | Before | After | Judgment |
| --- | --- | --- | --- |
| `markup.test.js` written shapes | one pooled sorted set | one ordered list per template, 9 templates | Stronger |
| `markup.test.js` runtime | `style` on declared elements after load | every attribute, `hidden` and unmodelled property written, through five steps, against an allow-list | Stronger. Includes everything the old test checked |
| `markup.test.js` `signatures` | quoted values only; skipped other tags | every tag or a failure | Stronger |
| `imgAttribute` rows | in `markup.test.js` | in `cover-markup.test.js` | Moved, unchanged |
| `render.test.js` `rendered`, `settled` ×3, row tree ×2 | copies | one copy each in `load-page.js` and `fake-dom.js` | Refactor; same behaviour |

No test was skipped, renamed out of a filter, or excluded.

**`tasks.md` against the list.** Every ticked task's ids are `DONE` or `BASELINE`, and no task is open.

## Findings

| # | Severity | Finding | Evidence |
| --- | --- | --- | --- |
| 1 | HIGH | **The lists that cycles 69 and 70 added have no recorded review.** `TEMPLATES` (9 ordered lists) and `RUNTIME` (the allow-list of runtime writes) were written from the page's code and passed on their first run. Cycle 69 says "They need their own review". `RUNTIME`'s comment calls it "reviewed". T079 covered `STATIC` and the old `WRITTEN` only. **Should**: a dated cycle-log entry recording the maintainer's review of both at a named commit | `tests/web/markup.test.js:129-145`, `:176-182`; `tdd/cycle-log.md` cycles 69, 70 |
| 2 | HIGH | **The artist filter is tested only with a few ASCII names in English.** Survivors: H1 normalizes the lookup to NFD, so "Björk" never applies (A2, A15, FR-002). H2 suggests only the first 100 artists (A1, FR-001, SC-001). H3 applies only the first 100 (FR-002). H5 lower-cases by locale, so "ASPIRIN" misses under a Turkish locale (A15). **Should assert**: an A15 row with a precomposed non-ASCII name; a list of 1,000 artists whose last one is suggested and applies; and A15's rows under `LANG=tr_TR.UTF-8`, with that run added to the profile's locale check | `tests/web/artist-filter.test.js` A1, A2, A15; `user-view.html:101, 107, 242` |
| 3 | HIGH | **The Artist field's attribute values are not pinned.** H4, `autocomplete="on"`, survives. The contract states `autocomplete="off"`, and A5's pattern accepts any attribute after `list`. U68 checks names only. **Should assert**: the Artist input carries exactly the contract's attributes and values (`id`, `type`, `list`, `autocomplete`, `placeholder`) | `tests/web/artist-filter.test.js:136-138`; `contracts/user-view.md:12` |
| 4 | HIGH | **U68's runtime check stops at its five steps.** J3 writes `style` on the panel when an action button is clicked, and survives. U68 states that the view writes only reviewed attributes, and an action click is a write path it never drives. Writes in the `input`, `error` and load-failure paths are also unseen. **Should assert**: the same allow-list after an action click (with `actionRow`), an Artist `input`, an image `error` and a failed load | `tests/web/markup.test.js:184-206` |
| 5 | HIGH | **The cycle 72 and 75 tests leave the code they claim partly unpinned.** J5, a `closest` whose attribute check accepts everything, survives the stand-in's tests (U69, `DONE`), because one combined assertion has no element that lacks the attribute. J4, a handler that always posts `/Ignore`, survives U72, because U72 has one example, and `render.test.js:119-121` says U72 pins "the join itself". **Should assert**: one `closest` row per case (attribute absent, several classes, a digit in a tag, `*`, the empty selector), and U72 with a second action (`HaveIt` or `Restore`) plus a click outside any button that posts nothing | `tests/web/fake-dom.test.js:70-77`; `tests/web/requests.test.js:97-107`; `tests/web/render.test.js:119-121` |
| 6 | MED | **Survivors in behaviours that predate 007.** H6 (the Archive request sends `archived=false`), H7 (an action in the Archive tab jumps to the List tab), H9 (no tab selected on open), H10 (a failed load says "waiting for its first refresh") and J1 (`aria-selected` set to any value) all survive. The probe also lists about twenty more: query keys, empty-state texts, labels, `aria-live`, button texts and badge texts. T083 brought three such survivors into 007. These and the rest are the page's untested pre-007 surface, which no 007 requirement states. **Decision for the maintainer**: a separate spec that covers the view's existing behaviour, or more characterization here | probe report; `user-view.html` |
| 7 | MED | **Mystery guest.** `LINK, LINK`, `'li[]', 'li[]'` and the two `option[value]` hold because item 101 has two sources, item 102 two missing tracks, and `artists.json` two artists. The test does not say so | `tests/web/markup.test.js:131-145` |
| 8 | MED | **Foreign style.** U71 (listener options) sits in `requests.test.js`, which the profile describes as "the paths each page sends" | `tests/web/requests.test.js:61-94`; profile page-side conventions |
| 9 | LOW | **Small inconsistencies.** `keepsListening` repeats the `once`/`signal` rule of U57's `capturesEveryError`, with its own table, and throws on `null` where the other does not. `rendered` in `load-page.js` shares its name with `render-status.test.js:16`'s admin helper. `DETAILS` is not the `<details>` element. The U68 row of the test list still names `imgAttribute` as a "U68 helper". `cover-markup.js`'s header describes only the cover box, but `rowOf` now serves U70 | as cited; `tdd/test-list.md` U68 row |

The smell pass judged `signatures`' fail-on-unread check sound, and this audit agrees. Every `<` +
letter must start a tag it read. A `<` + letter in text or in a quoted value gives a false failure,
never a silent miss, and `esc` keeps written markup free of both.

**Suite properties.** Fast and deterministic: node takes 0.2 s and dotnet takes 10 s. Failure output
is specific: one test per template, and a per-step map in the runtime test. Refactor-insensitivity is
weak, as a closed world must be. `TEMPLATES` fails on any reorder, U70 depends on the attribute order
`href` then `target`, and U72 depends on a hand-built row. The allow-lists (`RUNTIME`, U71) do not
fail when a reviewed write is removed.

## Mutation results

No mutation tool (profile: `mutation: null`). The scratch runner applied each mutant to a file copy,
one at a time. Each literal replacement had to match exactly once. It ran the whole node suite for
page mutants and checked each restore against `HEAD`.

The 90 mutants of the sixth audit ran at `531493b`, which has the same source and test files as
`3f9061f`. All of them failed their tests except the control E1, which passes as it should. K1 fails
U67 only. The 13 new mutants ran at `3f9061f`. Afterwards, `git status` was clean and both suites
were green.

| Mutant | Behavior | Survived | Judgment |
| --- | --- | --- | --- |
| R*, N*, P*, Q*, S*, X*, C*, M1–M4, E2, W*, V*, Y1–Y11, Z1–Z2, G1–G3 (88) | 007 and T083 behaviours | No | All caught |
| E1, K1 | controls | as intended | E1 passes; K1 fails U67 only |
| **H1** lookup normalized to NFD | A2, A15 | **Yes** | **Real defect for accented names. Finding 2** |
| **H2** first 100 artists suggested | A1, SC-001 | **Yes** | **Real defect for large libraries. Finding 2** |
| **H3** first 100 artists indexed | A2 | **Yes** | **Real defect. Finding 2** |
| **H5** `toLocaleLowerCase` | A15 | **Yes** | **Real defect under a Turkish locale. Finding 2** |
| **H4** `autocomplete="on"` | A5, US1-AS1 | **Yes** | **Breaks the contract. Finding 3** |
| **J3** `style` written on the panel on an action click | U68 | **Yes** | **Real defect: dims the list. Finding 4** |
| **J5** `closest` attribute check accepts everything | U69 | **Yes** | **Stand-in defect. Finding 5** |
| **J4** the handler always posts `/Ignore` | U72 (`BASELINE`) | **Yes** | **Real defect: Have it and Restore post Ignore. Finding 5** |
| H6 `archived=false` in the Archive query | pre-007 | Yes | Finding 6 |
| H7 jump to the List tab after an action | pre-007 | Yes | Finding 6 |
| H9 `aria-selected="false"` on the List tab at load | pre-007 | Yes | Finding 6 |
| H10 failure text replaced by the waiting text | pre-007 | Yes | Finding 6 |
| J1 `aria-selected` set to `'banana'` | pre-007 (U68 checks names) | Yes | Finding 6 |

Two candidates were judged equivalent and are not counted. A `dataset` write on the panel changes
nothing visible, because nothing reads it. `rest.join(',')` in `nextCover` matters only with three or
more covers, and the server sends two at most.

Of the 103 mutants, 88 were caught. 8 survived inside 007's behaviours or its test infrastructure,
and 5 survived in behaviours that predate 007. Both controls behaved as intended.

## Traceability

| Criterion | Tests | End to end |
| --- | --- | --- |
| US1-AS1 (FR-001) | A1, U44, U68 | Yes; large lists and the `autocomplete` value are untested (Findings 2, 3) |
| US1-AS2 (FR-002, FR-005) | A2, A15, U42, U46, U65 | Yes; non-ASCII names and non-English locales are untested (Finding 2) |
| US1-AS3 (FR-003) | A3, U68 | Yes |
| US1-AS4 (FR-002) | A4, U64 | Yes |
| US1-AS5 (FR-004) | A5, U68 | Partly: keyboard use is the browser's; Finding 3 |
| US2-AS1 (FR-006, FR-006a) | A7, U31–U37, U48, U52, U66, U67, U68 | Yes |
| US2-AS2 (FR-006a) | A8, U49, U55, U57 | Yes, within the fake DOM's limits |
| US2-AS3 (FR-007) | A9, U53, U56, U60, U67 | Yes |
| US2-AS4 | A10, U68 | Yes |
| US3-AS1 (FR-009, SC-003) | A11, U59, U67, U68 | Declarations; pixels are the real-browser pass |
| US3-AS2 (FR-009) | A12, U68 | Yes |
| US4-AS1 (FR-010, SC-004) | A13, U62, U63, U67, U68 | Declarations |
| US4-AS2 (FR-010) | A14, A16, A17, A18, U67, U68 | Yes, except a write after an action click (Finding 4) |

FR-011 is covered by U36, U70–U72 and the unchanged suite; Finding 6 lists what it does not reach.
Untested criteria: none. No test traces to nothing.

## What was not audited

- **Independence**: this session wrote cycles 69–75, both refactors and the T079/T083 entry. Fresh-context
  subagents did the smell pass and designed H1–H10. This session re-ran every counted mutant on the
  real tree and opened every cited line. The verdict, the severities and the two equivalence
  judgments were decided here. All agents are the same model family.
- **Mutation score**: no mutation tool. 103 deliberate mutants are a sample, aimed at the page and its
  test infrastructure. The C# code was sampled through its recorded mutants only.
- **The probe's other ~20 pre-007 survivors** were not re-run here (Finding 6).
- **Coverage**: unavailable for the `node:vm` sandbox and for dotnet.
- **Cycles 2–68** against history: done by earlier audits.
- **Real-browser behaviour**: rendering, layout, contrast on screen, keyboard use and datalist
  matching belong to `quickstart.md` §2.
