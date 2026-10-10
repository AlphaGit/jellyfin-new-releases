---
feature: 008-unmatched-artist-reasons
verdict: FAIL
standard: .specify/templates/overrides/tdd-test-quality-rubric.md # project override of the extension rubric (TEST_AFTER_ACCEPTED row)
profile: .specify/memory/tdd-profile.md
verified_at: dce5b53
behaviors: 95
proven: 48
likely: 11
test_after: 36
test_after_accepted: 0
no_test: 0
not_applicable: 0
high_smells: 1 # finding 1; findings 2–4 are FAIL conditions of other kinds (weakened existing tests, test-after)
weakened_existing_tests: 3 # findings 2 and 3 (two source tests, one page test)
criteria_total: 12
criteria_covered: 12
mutation_score: unmeasured # profile records mutation: null; deliberate mutants only
deliberate_mutants: 19 run by this audit; 16 caught, 3 survived (findings 1–3) # scope: 008's changed source files
suite: 416 passed, 0 failed, 10 s (dotnet) + 403 passed, 0 failed, 0.4 s (node)
independent: no # this session wrote the tests; the smell pass came from a fresh-context subagent and every cited line was re-opened here
---

# TDD Verification: Explain each unmatched artist step by step

**Verdict: FAIL.** A mutant inside a `DONE` behaviour survives the whole suite: the status
response can drop every candidate's MusicBrainz score and ID, and no test notices (U47, finding 1).

Three other conditions also fail the feature on their own:

- **Two existing source tests were loosened.** A stale `SourceArtistId` on an Unmatched result
  now passes; before 008 it failed (finding 2).
- **One deleted page test left the artist cell unasserted.** The artist name is no longer checked
  anywhere; writing it unescaped passes (finding 3).
- **36 behaviours are test-after with no maintainer acceptance on record** (finding 4). Each has
  a recorded deliberate mutant that its test catches, so each meets two of the three
  `TEST_AFTER_ACCEPTED` conditions. Only the maintainer's dated decision is missing.

The discipline otherwise holds. Every acceptance criterion has a test, the history agrees with
the log, and 16 of this audit's 19 deliberate mutants were caught.

## Test-first evidence

The history check over `1553a88..dce5b53` found 102 commits: 46 behaviour commits (`feat`, `fix`),
each changing a test and its source together; 35 test-only commits; 9 source-only `refactor`
commits; 12 documentation commits. That shape matches the cycle log entry for entry. No commit
changes source for a behaviour before its test.

| Behaviors | Class | Evidence |
| --- | --- | --- |
| U1–U7, U11, U12, U14, U17–U19, U24, U26, U27, U30–U32, U34, U36, U39, U40, U42, U44, U47, U49, U52, U53, U55, U58–U61, U63–U66, U68–U70, U72, U73, U75, U76, U79–U81 (48) | PROVEN | red recorded in the cycle; the test and the source change in one commit |
| A1–A8, A10–A12 (11) | LIKELY | red recorded when each was opened (cycles 18, 38, 73); held uncommitted, then committed after the source that made it pass, so history cannot show the order |
| U8–U10, U13, U15, U16, U20–U23, U25, U28, U29, U33, U35, U37, U38, U41, U43, U45, U46, U48, U50, U51, U54, U56, U57, U62, U67, U71, U74, U77, U78, U82, U83, A9 (36) | TEST_AFTER | no red: each passed on its first run (U20, U21, U28 credit tests that predate 008). Each entry records a deliberate mutant its test catches. This audit re-ran three of them (U20, U43, U82) and all three are still caught. No maintainer acceptance is recorded, so none is `TEST_AFTER_ACCEPTED` |

**Existing tests changed by 008.**

| Test | Before | After | Judgment |
| --- | --- | --- | --- |
| `Sources/MusicBrainzSourceTests.cs:107` `MatchArtistAsync_NoResults_IsUnmatchedAsNoResult` | whole record equals `ArtistMatch.Unmatched("no result")` | `(Status, Reason)` only | **Weakened** (finding 2) |
| `Sources/DeezerSourceTests.cs:47` `MatchArtistAsync_ExactNameWithoutACorroboratingAlbum_IsUnmatchedWithReason` | whole record | `(Status, Reason)` only | **Weakened** (finding 2) |
| `tests/web/render-status.test.js` "every artist no source matched is listed with its reasons and the hint" (deleted in `e542fd3`) | row count, `/Various Artists/`, reason markup, hint | row count moved to U53; reasons to U53–U65; hint absence to U57; **artist name to nothing** | **Weakened** (finding 3) |
| `Storage/ArtistRepositoryTests.cs` `GetCountsAsync_ReportsTotalsMatchedPerSourceAndUnmatchedReasons` | `(artist, name, source, reason)` for Unmatched sources | `(artist, name, source, status)` for all sources, plus a flat list of Unmatched reasons | Mixed (finding 9): stronger on order and matches, but a reason is no longer tied to its artist and source |
| `Storage/DatabaseTests.cs` three migration tests | schema version `1` | `LatestSchema = 2` | Not weakened |
| `Api/AdminControllerTests.cs` status test (renamed `…WithReasons`) | asserted the fixed hint | asserts the reason through `Reasons` | Not weakened: FR-013 removes the hint and U40 pins its absence |
| `Api/ResponseNamingTests.cs` `PopulatedAdminStatusResponse` | old shape | 008 shape, every field present at least once | Not weakened |

**`tasks.md` against the test list.** Every ticked behavioural task names only `DONE` behaviours.
No behavioural task is left unticked. T011 and T029–T032 carry no behaviour id and were ticked by
`/speckit-implement`; T033 is open.

## Findings

| # | Severity | Finding | Evidence |
| --- | --- | --- | --- |
| 1 | HIGH | **Surviving mutant in U47.** The test stores Deezer candidates (null score and ID) and asserts the same nulls back. `new CandidateDto(c.Name, null, null)` in `AdminController.ToReason` passes all 416 C# tests: `ResponseNamingTests` builds its DTOs by hand, U50 and U51 read no candidates, and the page tests read the committed fixture. Should also assert a MusicBrainz candidate's score and ID coming back. | `tests/…/Api/AdminControllerTests.cs:210`; mutant at `src/…/Api/AdminController.cs` `ToReason` |
| 2 | HIGH | **Two existing tests weakened.** Record equality became `(Status, Reason)`, so `SourceArtistId == null` is no longer pinned on an Unmatched result. The new `Detail` justified dropping record equality, not the id check. Mutant `… with { SourceArtistId = "stale" }` on MusicBrainz `no result` passes all 416 tests; before 008 it would have failed. | `Sources/MusicBrainzSourceTests.cs:107`, `Sources/DeezerSourceTests.cs:47` |
| 3 | HIGH | **Existing page test deleted without full replacement.** The deleted test asserted the artist name in the row. No 008 test asserts the Artist cell, so dropping or unescaping `u.name` passes all 403 node tests. Escaping was never pinned before 008 either; name presence was. | `e542fd3` deletion; `src/…/Web/admin.html` artist cell |
| 4 | HIGH (evidence) | **36 test-after behaviours with no maintainer acceptance.** Each passed on its first run, because an earlier cycle had already generalised the code. Each records a mutant its test catches. 007 closed the same gap with a dated maintainer decision (`TEST_AFTER_ACCEPTED`); 008 has none. | Test-first table above |
| 5 | MED | **Redundant tests at one level.** The page acceptance tests call `renderStatus` on the same fixture rows as the unit tests: A6 duplicates U78, A4 duplicates U79, A5 duplicates U80, and A1–A3, A7, A8 and A12 overlap U69, U72, U75, U77 and U58–U64 with weaker regexes. One bug fails both, so this is not double-loop TDD. | `tests/web/unmatched-acceptance.test.js:46,52,58`; `render-status.test.js:254,269,277` |
| 6 | MED | **Self-referential oracle.** U41 takes its expected order from `status.Sources` in the same response; both lists come from one variable, and both sources are enabled, so neither the order nor the "enabled only" half is pinned by this test (A10 and U46 cover the second). | `Api/AdminControllerTests.cs:123` |
| 7 | MED | **Mystery guest.** About 25 page tests pick fixture rows by index (`unmatchedRows(…)[0\|1\|2]`, `withReason(2, 0, …)`). A row's meaning lives in the fixture; only some tests say what it is. | `tests/web/render-status.test.js:99–405` |
| 8 | MED | **Foreign style.** Four page tests read the `<style>` block in `render-status.test.js` with a new `declared()` parser, while the profile puts stylesheet declarations in `styles.test.js`, which already has a parser (`blocks`, `rules`). | `render-status.test.js:92,183,190,368,392` |
| 9 | MED | **Counts test now ties reasons loosely.** See the existing-tests table: a reason is no longer asserted with its artist and source in one tuple. | `Storage/ArtistRepositoryTests.cs:103` |
| 10 | MED | **Migration baseline not frozen.** U39 builds the "released" schema from today's embedded `001_initial.sql`; an in-place edit of 001 would change the baseline and still pass. | `Storage/DatabaseTests.cs:95` |
| 11 | MED | **Weak or eager page and repository tests.** U82 asserts only `fixes(row).length === 1` on rows U71 and U75 already pin exactly. U35 repeats U30's `Assert.Empty` in its arrange step. | `render-status.test.js:299`; `Storage/ArtistRepositoryTests.cs:210` |
| 12 | LOW | **Readability.** Duplicated `LibraryArtistSnapshot("name:blur", …)` setup (6 places) and a duplicated status-to-JSON helper; `visible`, `rows` and `reasonLines` copied between the two page test files; a guarded assertion loop where one `deepEqual` would do; U57 and U77 assert only negatives; `Spec008_U50` and `Spec008_U51` put unit ids in the acceptance class; U53 checks row 0 only. | `AdminControllerTests.cs`, `ConfigureAndRunTests.cs:256,273`, `render-status.test.js:140,247,363`, `unmatched-acceptance.test.js:15,67,76` |

`MatchReasonsTests.cs` matches its exemplar with no smell. The source tests for U16–U29 are sound.
No fixture contains a credential, and no repository content tried to give instructions.

## Mutation results

The profile records no mutation tool, so this is a sample of deliberate mutants, each run against
its behaviour's tests, restored from a copy, and checked with `git diff` before the next. The
full suites were green afterwards (dotnet 416, node 403). The sample is 19 mutants, not
exhaustive.

| Mutant | Behavior | Survived | Judgment |
| --- | --- | --- | --- |
| `ArtistRepository` subquery drops the enabled-source filter | U30, A11 | No | caught |
| `MatchReasons.Parse` swaps the two ambiguous scores | U5 | No | caught |
| `MatchReasons.Steps` drops the "Id always failed" rule | U11–U13 | No | caught (3 tests) |
| `DeezerSource` keeps 4 names, not 3 | U24 | No | caught |
| `MusicBrainzSource` keeps 2 candidates, not 3 | U17, U18 | No | caught (2 tests) |
| `SetMatchAsync` never writes `match_detail` | U36 | No | caught |
| controller states `Pending` as `Pending` | U44 | No | caught |
| page tracking line lists unmatched sources | U79, U80 | No | caught |
| page joins values with ` / ` | U65 | No | caught |
| page shows the source id, not its display name | U53 | No | caught (6 tests) |
| page gives the Album sentence to a Name failure | U72–U75 | No | caught (3 tests) |
| `DeezerSource` exact-name count off by one | U26 | No | caught |
| migration 002 defaults `match_detail` to `{}` | U39 | No | caught |
| recorded U43 mutant: reason falls back to state name | U43 | No | caught today |
| recorded U82 mutant: a paragraph for every source | U82 | No | caught today |
| recorded U20 mutant: match by ID carries detail | U20 | No | caught today |
| controller drops every candidate's score and ID | U47 | **Yes** | **HIGH**, finding 1 |
| MusicBrainz `no result` carries a stale `SourceArtistId` | none in 008 (existing test, loosened) | **Yes** | **HIGH**, finding 2 |
| page writes the artist name unescaped | none (existing test, deleted) | **Yes** | **HIGH**, finding 3 |

## Traceability

| Criterion | Tests | End to end |
| --- | --- | --- |
| US1-AS1 | A1, U68–U71 | Page entry point on the committed fixture; server side through U42, U47 |
| US1-AS2 | A2, U75–U77, U50 | Yes: U50 runs the refresh and `Admin/Status`; A2 renders the page |
| US1-AS3 | A3, U72–U74 | Page entry point; server side through U24, U47 |
| US1-AS4 | A4, U79, U50 | Yes |
| US1-AS5 | A5, U80 | Page entry point |
| US1-AS6 | A6, U78 | Page entry point |
| US2-AS1 | A7, U58–U64, U51 | Yes: U51 runs the refresh and `Admin/Status`; A7 renders the page |
| US2-AS2 | A8, U59, U60 | Page entry point |
| US2-AS3 | A9, U43, U54 | Page entry point and controller |
| US2-AS4 | A10, U46 | Yes, through `AcceptanceRig` |
| US2-AS5 | A11, U30, U35 | Yes, through `AcceptanceRig` |
| US2-AS6 | A12, U62, U63 | Page entry point |

Every functional requirement FR-001 to FR-016 reaches at least one behaviour (see the `traces`
column of `test-list.md`). Untested criteria: none. Tests tracing to nothing: none.

The seam between server and page is the committed `admin-status.json`. `ResponseNamingTests`
holds its field names to the records, but not its values. That is why finding 1 survives: no test
feeds a server-built response into the page.

## What was not audited

- **Mutation tool.** None exists in the profile. Test strength rests on 19 deliberate mutants
  here plus the ones the cycle log records. It is a sample.
- **Pixel width (SC-004) and reading time (SC-002, SC-003).** These are not measurable in the
  fake DOM. They belong to the maintainer's real-server pass.
- **Coverage.** Not run. The node coverage command does not attribute lines inside `admin.html`'s
  script.
- **Independence.** This session wrote the tests. The smell pass was delegated to a
  fresh-context subagent, and each cited line was re-opened here.
- **Code outside 008's changed files** was not mutated.
