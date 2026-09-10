# Cycle Log: Report the age of the data, not the age of the run

Append only. Newest last. Every entry's `red` block is the evidence that the test existed and
failed before the implementation.

## Baseline

- suite: `dotnet test --configuration Release` -> 178 passed, 0 failed
- page-side suite: none yet; the runner is introduced by this feature (`T029`–`T034`)
- commit: `0fa9999`
- recorded: cycle 0, before any change
- note: run with SDK 9 (`PATH=/opt/homebrew/opt/dotnet@9/bin:$PATH DOTNET_ROOT=/opt/homebrew/opt/dotnet@9/libexec`); the default `dotnet` in non-login shells is SDK 8 and fails with NETSDK1045

## Outer loop opened: A1 a run that completes no fetch does not move the reported instant

- test: `Acceptance/ConfigureAndRunTests.cs::A20_WithEverySourceInCooldown_TheListStillShowsTheStoredDataAndItsAge`
  (existing; its instant assertion inverted to the rule `002` specifies, its data-survival
  assertions left alone). This is the test `001`'s audit wrote hours ago to pin the **old**
  behaviour. Inverting it is a behaviour change decided by `spec.md`, taken as its own step before
  any implementation, not a weakened test.
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~ConfigureAndRunTests.A20_WithEverySourceInCooldown_TheListStillShowsTheStoredDataAndItsAge" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: Values differ / Expected: Tuple (True, 2026-09-06T12:00:00Z) / Actual: Tuple (True, 2026-09-06T13:00:00Z)` (1 failed)
- state: stays RED while the inner loop runs, per the playbook's double loop. Closes when the
  controller reads the new datapoint.
- note: a first attempt failed to compile (`CS0103: SourceHarness does not exist`); a missing
  `using` is not a valid red, so the import was added and the run repeated. The red above is the
  repeated run.

## Cycle 1: U1 the newest completed fetch across artists

- test: `Storage/ArtistRepositoryTests.cs::GetReleasesLastCheckedAtAsync_IsTheNewestCompletedFetchAcrossArtists` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~ArtistRepositoryTests.GetReleasesLastCheckedAtAsync_IsTheNewestCompletedFetchAcrossArtists" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: Values differ / Expected: 2026-09-06T03:00:00Z / Actual: null` (1 failed).
  The first run could not compile (`CS1061: ArtistRepository does not contain a definition for
  GetReleasesLastCheckedAtAsync`); a stub returning null was added so the red could be an
  assertion failure, per the playbook.
- green: `Storage/ArtistRepository.cs` `GetReleasesLastCheckedAtAsync` = `SELECT MAX(last_complete_at) FROM artist_source`, no filter yet. Suite -> 179 passed, 1 failed (A1, expected)
- refactor: none needed
- commit: see below

## Cycle 2: U2 a source outside the enabled set is ignored, even when newest

- test: `Storage/ArtistRepositoryTests.cs::GetReleasesLastCheckedAtAsync_IgnoresASourceThatIsNotEnabled_EvenWhenItIsTheNewest` (new)
- red: same command shape ->
  `Assert.Equal() Failure: Values differ / Expected: 2026-09-01T03:00:00Z / Actual: 2026-09-06T03:00:00Z` (1 failed).
  A first attempt failed to compile (`CS9174: cannot initialize ISet<string> with a collection
  expression`); the fixture uses `new HashSet<string> { … }`. Not a valid red, so it was repeated.
- green: the query gained `WHERE source IN (…)` with one parameter per enabled source. Suite -> 180 passed, 1 failed (A1, expected)
- refactor: none needed
- commit: see below

## Cycle 3: U3 an empty enabled set yields no instant

- test: `Storage/ArtistRepositoryTests.cs::GetReleasesLastCheckedAtAsync_WithNoEnabledSource_IsNothing` (new)
- red: passed on first run — an empty set produces `IN ()`, which SQLite accepts as matching
  nothing, so `MAX` is already NULL.
- deliberate mutant, first attempt: the `enabledSources.Count == 0` early-return guard removed ->
  **test still passed**. The guard was dead code: its comment claimed `IN ()` is a syntax error,
  which is true of PostgreSQL and MySQL but not SQLite (verified directly). The guard was deleted
  rather than kept, per constitution VI.
- deliberate mutant, second attempt: an empty set treated as "no filter"
  (`placeholders.Length == 0 ? "…FROM artist_source" : "…IN (…)"`) ->
  `Assert.Null() Failure: Expected: null / Actual: 2026-09-06T03:00:00Z` (1 failed). Restored exactly.
- green: no production change beyond deleting the dead guard. Suite -> 180 passed, 1 failed (A1, expected)
- refactor: the dead guard removed, as above
- commit: see below
- note: cycles 1-3 share one commit. They were driven as three separate red-green cycles, but the
  working change was not committed between them, so the history cannot show the order. A mid-cycle
  `git checkout --` intended to revert a mutant reverted the whole file and destroyed cycles 1 and
  2's implementation; it was rewritten identically and the suite re-run green. Recorded here rather
  than hidden: the evidence for those two cycles is this log, not the commit history.

## Cycle 4: U4 no fetch ever completed yields no instant

- test: `Storage/ArtistRepositoryTests.cs::GetReleasesLastCheckedAtAsync_WithNoFetchEverCompleted_IsNothing` (new)
- red: passed on first run — an artist matched but never fetched has a NULL `last_complete_at`,
  so `MAX` is NULL.
- deliberate mutant: a null scalar mapped to `DateTimeOffset.MinValue` instead of null ->
  `Assert.Null() Failure: Expected: null / Actual: 0001-01-01T00:00:00Z` (1 failed). Code restored
  exactly (`git diff` empty), test green again.
- green: no production change. Suite -> 181 passed, 1 failed (A1, expected)
- refactor: none needed

## Cycle 5: U5 and U6 an outcome other than Complete leaves the value alone

- test: `Storage/ArtistRepositoryTests.cs::GetReleasesLastCheckedAtAsync_AnOutcomeOtherThanComplete_LeavesTheValueWhereTheLastCompleteLeftIt`
  (new, Theory: `Partial`, `Failed`)
- red: passed on first run — `SetFetchOutcomeAsync` writes `@completeAt` as NULL unless the
  outcome is `Complete`, and the upsert's `COALESCE` keeps the previous value.
- deliberate mutant: `@completeAt` bound to `now` regardless of outcome ->
  `Assert.Equal() Failure: Expected: 2026-09-01T03:00:00Z / Actual: 2026-09-06T03:00:00Z`
  (both theory rows failed). Code restored exactly (`git diff` empty), tests green again.
- green: no production change. `FR-003` holds because of a property the schema already had, not
  because of new code — which is why it needed pinning. Suite -> 183 passed, 1 failed (A1, expected)
- refactor: none needed
- note: `U5` and `U6` were driven as **one** cycle, not two. They are one rule ("an outcome other
  than Complete does not move the value") with two inputs, so staging them as separate cycles
  would have been theatre. Both ids are marked DONE against the one Theory.

## Cycle 6: U7, U8, U9 the stored-releases check follows the release rows

- test: `Storage/ReleaseRepositoryTests.cs::HasAnyAsync_FollowsWhetherReleaseRowsExist` (new; walks
  empty -> one row -> purged, which is the three list rows in one sequence)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~ReleaseRepositoryTests.HasAnyAsync_FollowsWhetherReleaseRowsExist" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.True() Failure / Expected: True / Actual: False` (1 failed; stub returned false)
- green: `ReleaseRepository.HasAnyAsync` = `SELECT EXISTS(SELECT 1 FROM release LIMIT 1)`.
  Suite -> 184 passed, 1 failed (A1, expected)
- refactor: none needed
- note: `U7`, `U8` and `U9` are one rule over three states, driven as one cycle.

## Cycle 7: U10 the list reports the newest completed fetch, not a run's end — and A1 closes

- test: `Api/ReleasesControllerTests.cs::GetReleases_ReportsTheNewestCompletedFetch_RefreshIntervalFollowsTheTrigger`
  (`001`'s `U118`, rewritten and renamed: it asserted the last completed run's end, which `002`
  replaces. A behaviour change decided by `spec.md`, taken before the implementation change.)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~ReleasesControllerTests.GetReleases_ReportsTheNewestCompletedFetch_RefreshIntervalFollowsTheTrigger" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: Values differ / Expected: Tuple (True, 2026-09-06T12:00:00Z, 24) / Actual: Tuple (True, 2026-09-06T12:05:00Z, 24)` (1 failed)
- green: `PluginConfiguration.EnabledSourceIds()` added beside `EnabledReleaseTypes()`;
  `ReleasesController` list and status actions now read
  `ArtistRepository.GetReleasesLastCheckedAtAsync` and `ReleaseRepository.HasAnyAsync` instead of
  the last completed run. **Suite -> 185 passed, 0 failed: the outer loop A1 closed on this change.**
- refactor: none needed. The enabled-source mapping is still duplicated in
  `ScheduledTasks/RefreshNewReleasesTask.cs:268` and `Api/AdminController.cs:68`; neither is in
  this cycle's scope and `RefreshNewReleasesTask` is not in `plan.md`'s file list, so both are
  reported rather than changed.
- note: this change also makes `U11`, `U12`, `U13` and `U14` true, but none of them has a test yet,
  so they stay PENDING. Each is pinned in its own cycle below, with a deliberate mutant, rather
  than credited to an implementation that arrived first.

## Cycle 8: U11 credited to an existing test, not re-driven

`U11` ("a refresh that completed no fetch leaves the reported instant unchanged") is already
asserted by cycle 7's test, which seeds a completed fetch and then finishes a run five minutes
later, expecting the fetch instant. Per the loop's Phase 1, a behaviour already covered by a
passing test that really asserts it is marked DONE against that test rather than given a staged
cycle. No new test, no new code.

## Cycle 9: U12 the list and the status report the same instant

- test: `Api/ReleasesControllerTests.cs::GetReleases_ListAndStatusReportTheSameInstant` (new)
- red: passed on first run — cycle 7 wired both actions to the same call.
- deliberate mutant: the status action reverted to `GetLastCompletedRunAsync()?.EndedAt` ->
  `Assert.Equal() Failure: Expected: Tuple (True, 2026-09-06T12:00:00Z) / Actual: Tuple (True, null)`
  (1 failed). Code restored exactly (`git diff` empty), test green again.
- green: no production change. Suite -> 186 passed, 0 failed
- refactor: none needed

## Cycle 10: U13 the empty state follows the rows, not the run history

- test: `Api/ReleasesControllerTests.cs::GetReleases_StoredReleasesFlagFollowsTheRows_NotWhetherARunCompleted`
  (new; a completed run with no rows, then rows, then a purge)
- red: passed on first run.
- deliberate mutant: the flag reverted to `GetLastCompletedRunAsync() is not null`, which is what
  `001` shipped -> `Assert.False() Failure / Expected: False / Actual: True` (1 failed). Code
  restored exactly, test green again. This mutant is the `001` behaviour, so the test pins the
  change rather than merely describing it.
- green: no production change. Suite -> 187 passed, 0 failed
- refactor: none needed

## Cycle 11: U14 releases with nothing that confirmed them are listed with no age

- test: `Api/ReleasesControllerTests.cs::GetReleases_ReleasesStoredButNoFetchEverCompleted_AreListedWithNoInstant` (new)
- red: passed on first run.
- deliberate mutant: the instant falling back to the last completed run when no fetch has
  completed -> `Assert.Null() Failure: Expected: null / Actual: 2026-09-06T12:00:00Z` (1 failed).
  Code restored exactly, test green again.
- green: no production change. Suite -> 188 passed, 0 failed
- refactor: none needed

## Cycle 12: U15 disabling the newest source falls back to the newest enabled one

- test: `Api/ReleasesControllerTests.cs::GetReleases_DisablingTheNewestSource_FallsBackToTheNewestEnabledOne`
  (new; both sources, then Deezer off, then both off)
- red: passed on first run — `EnabledSourceIds()` is read on every request.
- deliberate mutant: the enabled set replaced with a hard-coded pair of both sources ->
  `Assert.Equal() Failure: Expected: 2026-09-01T03:00:00Z / Actual: 2026-09-06T03:00:00Z`
  (1 failed). Code restored exactly, test green again.
- green: no production change. Suite -> 189 passed, 0 failed
- refactor: none needed

## Cycle 13: U16, U17, U18 the administrator sees the run and the data age diverge

- test: `Api/AdminControllerTests.cs::Status_ReportsTheLastRunAndTheDataAge_WhichDivergeAfterARunThatCompletedNoFetch`
  (new; a fetch, then six hours later a run that completed none)
- red: `ReleasesLastCheckedAt` added to `AdminStatusResponse` and wired to a `null` stub so the red
  could be an assertion rather than a compile error ->
  `Assert.Equal() Failure: Expected: 2026-09-06T12:00:00Z / Actual: null` (1 failed)
- green: `AdminController.GetStatusAsync` reads the same `GetReleasesLastCheckedAtAsync` call the
  user page uses, satisfying FR-011's "one instant everywhere". Suite -> 190 passed, 0 failed
- refactor: none needed
- note: one test for three list rows — they are three assertions about one response.

## Cycle 14: U34 both pages expose their pure helpers

- test: `tests/web/exposure.test.js` (new, the first page-side test)
- red: `node --test "tests/web/*.test.js"`
  -> `user-view.html exposed no NewReleasesInternals; see tests/web/load-page.js` (2 failed)
- green: each page assigns its helpers to `globalThis.NewReleasesInternals` as the first statement
  of its IIFE, before any DOM access; function declarations hoist, so they are all defined.
  Page suite -> 2 passed, 0 failed
- refactor: none needed
- note: **the profile and CI had the wrong command.** `node --test tests/web` and
  `node --test tests/web/` both resolve the path as a module (`MODULE_NOT_FOUND`); a directory is
  only scanned when no path is given. Corrected to the glob `node --test "tests/web/*.test.js"`
  in `.specify/memory/tdd-profile.md` and `.github/workflows/build.yml`, and verified by running
  the CI command exactly as written. Caught because this was the first cycle to run it.

## Cycle 15: U19, U20, U21, U22 when there is a sentence at all

- test: `tests/web/staleness.test.js` (new): no instant, an instant in the future, exactly one
  refresh interval, one second past it
- red: `node --test "tests/web/*.test.js"` -> `stalenessText is not a function` (4 failed). A
  missing symbol is not a valid red, so a stub returning null was added; re-run ->
  `not ok - an age one second past the interval yields a sentence / Expected "actual" to be strictly unequal to: null` (1 failed)
- green: `stalenessText(checkedAt, now, intervalHours)` in `Web/user-view.html` — the gating only,
  with hours as the single unit. Page suite -> 6 passed, 0 failed
- refactor: none needed
- note: `tests/web/exposure.test.js` gained `stalenessText` in its expected key list. An added
  expectation, not a weakened one.

## Cycle 16: U23, U24, U25, U26, U27 the unit ladder

- test: `tests/web/staleness.test.js` extended: 47 h, 48 h, 13 d, 14 d, 60 d, 61 d, 364 d, 365 d
  and 700 d — both sides of every changeover
- red: `node --test "tests/web/*.test.js"` ->
  `not ok - 48 hours renders in days / expected: 'Releases last checked 2 days ago.' / actual: 'Releases last checked 48 hours ago.'` (7 failed)
- green: `relativeAge(ageMs)` extracted, stepping hours -> days -> weeks -> months -> `over a year`.
  Page suite -> 14 passed, 0 failed
- refactor: `relativeAge` extracted from `stalenessText` as part of the green step, so the sentence
  and the unit choice are separate concerns
- note: **the first implementation rounded and two tests failed** — 60 days gave "9 weeks" and 364
  days "12 months", the latter sitting absurdly next to "over a year" at 365. The tests were right
  and the implementation was wrong, so the implementation changed to floor: "checked 8 weeks ago"
  should mean at least eight weeks have passed. The tests were not touched.

## Cycle 17: U28 the sentence is about the releases, not the job

- test: `tests/web/staleness.test.js` extended: the exact sentence, and none of "refresh", "run",
  "scan" or "update" in it
- red: passed on first run — cycle 16 already produced this wording.
- deliberate mutant: the sentence reverted to `001`'s `Last refreshed … ago.` -> 8 failed,
  including this one. Restored exactly, tests green again.
- green: no production change. Page suite -> 15 passed, 0 failed
- refactor: `staleness(data)` reduced to showing what `stalenessText` decides, so every rule now
  lives where a test can reach it. The DOM half has no test — no runner reaches it — and is
  checked by hand in `quickstart.md`.

## Cycle 18: U29, U30, U31, U32, U33 characterization of the pages' existing helpers

- tests: `tests/web/esc.test.js` and `tests/web/page-helpers.test.js` (new). These capture what the
  code already does; this feature does not change any of them. They exist so the rename in Phase 7,
  and anything later, cannot break them silently.
- red: none expected, and none occurred — characterization tests are green against untouched code.
  Page suite -> 22 passed, 0 failed.
- deliberate mutants, one per behaviour, each applied alone and restored exactly:
  - `U29` `esc` escaping only `&<>`, not quotes -> 1 failed
  - `U30` `esc` dropping the null guard -> 1 failed
  - `U31` `groupOf` no longer returning `Undated` -> 1 failed
  - `U32` `artistLink` not URL-encoding the artist id -> 1 failed
  - `U33` `healthText` dropping the error reason -> 1 failed
- green: no production change. Page suite -> 22 passed, 0 failed after every restore
- refactor: none needed
- note: `esc` is the one worth having regardless of this feature. Release titles and artist names
  arrive from MusicBrainz and Deezer and are concatenated into HTML by `row()` in about ten places;
  `esc` is the only thing between them and the DOM, and until now nothing tested it.

## Cycle 19: A5 a purge returns the page to the empty state — and exposes a missing rule

- test: `Acceptance/ConfigureAndRunTests.cs::A5_AfterAPurge_TheListReportsNoStoredReleasesAndNoInstant` (new)
- red: `Assert.Null() Failure: Expected: null / Actual: 2026-09-06T12:00:00Z` (1 failed). **A real
  gap, not a wrong expectation.** After a purge the empty-state flag went false correctly, but the
  instant survived: `artist_source.last_complete_at` outlives the release rows, exactly as
  `research.md` R3 said it would. R3 concluded the page would hide the line anyway, which is true
  of the line but not of the value: the response still carried an age for a list that no longer
  existed, and the administrator page would have shown it.
- green: `ReleasesController.LastCheckedAtAsync` reports the instant only when releases are stored,
  in both the list and status actions. `FR-008` ties both the empty state and the age to whether
  releases exist; only the first half had been built. Suite -> 191 passed, 0 failed
- refactor: the gate extracted to one private helper rather than repeated at both call sites

## Cycle 20: U35 the administrator view reports no instant either — a behaviour found mid-loop

`U35` was not on the list. Cycle 19's fix applied to the user-facing responses only, and `FR-011`
requires every place reporting the age to report the same instant, so the administrator view had to
follow. Appended to the test list rather than folded silently into cycle 19.

- test: `Api/AdminControllerTests.cs::Status_WithNothingStored_ReportsNoInstantEitherThoughTheFetchTimestampSurvives` (new)
- red: `Assert.Null() Failure: Expected: null / Actual: 2026-09-06T12:00:00Z` (1 failed)
- green: `AdminController.GetStatusAsync` gates the instant on stored releases too.
  Suite -> 192 passed, 0 failed
- refactor: none needed
- note: this broke cycle 13's test, which seeded a fetch but no release rows — a setup that the new
  rule makes impossible. **The assertion was right and the setup was wrong**, so the setup gained a
  stored release. No assertion was loosened.

## Cycle 21: A6 and A7 the outer loop for user story 2

- tests: `Acceptance/ConfigureAndRunTests.cs::A6_OneSourceCoolingDownWhileTheOtherCompletesAFetch_TheAgeCountsFromThatFetch`
  and `::A7_WithEverySourceDisabled_NoAgeIsReportedWhileTheListStillShowsWhatIsStored` (new)
- red: both passed on first run. This is the outer loop closing on units that are already green,
  which the playbook describes as the expected end of the double loop, not a suspicious pass. Every
  unit beneath them (`U1`-`U6`, `U15`) is separately mutant-verified.
- green: no production change. Suite -> 194 passed, 0 failed
- refactor: none needed
- **conflict found in the specification, reported not resolved:** `spec.md`'s `US2-AS2` and its
  matching edge case both say that with every source disabled "the stated age keeps growing".
  `FR-002`, the Clarifications answer behind it, and `U3`/`U15` all say a disabled source stops
  counting, so with none enabled there is no instant at all and the line disappears. The prose
  predates the disabled-source decision taken during grilling and was not swept. `A7` asserts the
  `FR-002` behaviour, which is what is built and what the later decision requires. **`spec.md` needs
  the amendment; this command may not make it.**

## A8: every boundary of the ladder moved by one unit

The half of `US3-AS1` no assertion can express. Each boundary moved alone, then restored exactly:

| Boundary moved | Result |
| -------------- | ------ |
| hours/days, 2 days -> 3 days | 1 failed |
| days/weeks, 14 days -> 15 days | 1 failed |
| weeks/months, 61 days -> 62 days | 1 failed |
| months/over a year, 365 days -> 366 days | 1 failed |

Page suite green again after every restore: 22 passed, 0 failed.

## A10: the suite runs with no network and no installation step

Verified rather than asserted — there is nothing for a test to claim about itself:

- No `package.json`, no `package-lock.json`, no `node_modules`. Nothing to install.
- Every `require` in `tests/web` resolves to a Node builtin (`node:test`, `node:assert/strict`,
  `node:fs`, `node:path`, `node:vm`) or to the local `./load-page.js`.
- No network-capable module is reached for: `node:http`, `node:https`, `node:net`, `node:dns`,
  `node:tls`, `fetch(` and `XMLHttpRequest` all appear nowhere under `tests/web`.
- Both suites, back to back: page 22 passed, 0 failed; server 194 passed, 0 failed.

Ceiling on this evidence: the machine's network was not physically severed for the run. What is
proven is that no install step exists and no network API is referenced, which is what `FR-014`
asks for.

## T029: the administrator view states the data age beside the last run

Written during `/speckit-implement`, not by the loop: `T029` carries no behaviour id, because the
test list treats the administrator page's copy as page wiring. It still added real logic — a second
copy of the unit ladder — so it was driven test-first rather than written test-after.

- test: `tests/web/page-helpers.test.js::checkedText states the data age in the ladder's units, or a dash when nothing is known` (new)
- red: `node --test --test-name-pattern "checkedText" "tests/web/*.test.js"`
  -> `error: 'checkedText is not a function'` (1 failed)
- green: `admin.html` gained `checkedText(iso, now)` and a `Releases last checked` row in
  `#nr-status`, rendered from `status.releasesLastCheckedAt`. Page suite -> 23 passed, 0 failed
- refactor: none needed
- note: `exposure.test.js` pins the exact set of helpers each page exposes, so it went red on the
  new key and was updated to `['checkedText', 'esc', 'healthText']`. The assertion was not
  loosened; it still demands an exact set.
- note: the ladder is stated twice. The two pages are separate embedded resources with no way to
  share code, and a third resource plus a route to serve it costs more than twelve duplicated
  lines. `admin.html` uses `numeric: 'always'` where `user-view.html` uses `'auto'`: this view
  shows the age even when it is under an hour, and `'auto'` renders that as "this hour", not
  "0 hours". Every other value the ladder can produce is identical in both modes (verified: the
  bands never emit 1 day or 1 week).

## T030: the rename, on a green suite

Structural only, no behaviour change. `LastRefreshedAt` -> `ReleasesLastCheckedAt` and
`HasCompletedRefresh` -> `HasStoredReleases` in `Api/Dtos.cs`, `Web/user-view.html` and the three
test files that name them.

- before: 194 passed, 0 failed (server); 23 passed, 0 failed (page)
- after: 194 passed, 0 failed (server); 23 passed, 0 failed (page)
- the two lists of test names differ by exactly one entry, the deliberate method rename
  `A5_NoCompletedRun_HasCompletedRefreshFalseAndNoItems` ->
  `A5_NoCompletedRun_ReportsNoStoredReleasesAndNoInstant`. Both `001`'s and `002`'s test lists
  were repointed at the new name.
- `Model/StoredRecords.cs`'s `ArtistRecord.LastRefreshedAt` is a different datapoint — when that
  artist was last refreshed — and was deliberately left alone, with its tests.
- `SourceStateRepository.GetLastCompletedRunAsync` has no production caller left. Its doc comment
  claimed to be "the run behind `lastRefreshedAt`", which is no longer true; the comment was
  corrected. **The method itself was kept**: two tests still exercise it, and deleting it is
  outside this feature's scope. Reported, not fixed.

## Phase 9: remediation of the TDD audit findings

Driven from `tdd/verification.md` (verdict FAIL). These are test changes on green production code,
so the proof of each is a deliberate mutant, not a red.

**T039 + T041 — Finding 1, the administrator ladder.** `page-helpers.test.js`'s single
`checkedText` test became one test per rung in the shape of the node exemplar, asserting both sides
of every changeover. The three mutants that survived the audit are now caught:

| Boundary moved down one unit | before T039 | after T039 |
| --- | --- | --- |
| day → week, 14 d → 13 d | SURVIVED | 1 failed |
| week → month, 61 d → 60 d | SURVIVED | 1 failed |
| over a year, 365 d → 364 d | SURVIVED | 1 failed |
| hour → day, 2 d → 3 d | 1 failed | 1 failed |
| drop the no-instant guard | 1 failed | 1 failed |

`U36` and `U37` were added to the test list and their ids to `T029`. The root cause was not the
test: it was that `T029` shipped production logic with no behaviour on the list, so it never
entered the per-behaviour evidence at all.

**T040 — Finding 2, the vacuous assertion.** `staleness.test.js:33` asserted `notEqual(…, null)`,
which `''` would pass while hiding the line exactly as `null` does. Now asserts the exact sentence.
Mutant: `stalenessText` returning `''` past the interval -> **10 failed** (was 0). Restored exactly.

**T042 — Finding 3, the locale.** The suite was green on an English machine and red on any other:
`LANG=de_DE.UTF-8` gave 14 passed, 9 failed. `load-page.js` now pins the sandbox's
`Intl.RelativeTimeFormat` default to `en`. Production is untouched and still passes `undefined`, so
a Jellyfin user keeps reading the sentence in their own language. Verified green under the default
locale, `de_DE.UTF-8` and `ja_JP.UTF-8`.

**T043 to T047 — the MED and LOW findings.** Three assertions that could not fail were removed
(`ConfigureAndRunTests.cs`'s `InRange` behind an exact-instant equality; `staleness.test.js`'s
job-word loop behind a whole-string equality; `exposure.test.js`'s `typeof` loops). The exact-key-set
assertions in `exposure.test.js` were **kept** against the subagent's advice to delete the file:
they are the one thing no other test does, and they caught `T029`'s new helper. The eager
`GetReleases_ReportsTheNewestCompletedFetch_RefreshIntervalFollowsTheTrigger` split into
`…_NotTheLastRunsEnd` and `GetReleases_RefreshIntervalFollowsTheTrigger`; both test lists repointed.
`sandboxGlobals` now merges one level down, so a test pins one `ApiClient` member without restating
the rest.

**T048 to T050 — process.** The `git checkout` hazard, the locale rule and the extension's
`BASELINE` tick conflict are recorded in `.specify/memory/tdd-profile.md`, which both the loop and
the audit read at preflight. `FR-015` is recorded in `spec.md` as verified by inspection: it governs
the gate that would have to run any test of it.

- suite after remediation: 195 passed, 0 failed (dotnet, one test became two); 32 passed, 0 failed
  (node, one test became eleven)
- no production code changed in this phase

## Phase 10: remediation of the second TDD audit's findings

Driven from `tdd/verification.md` (verdict FAIL, second run). The production code was already
correct in every case except `T055`, so the proof of each is a deliberate mutant, not a red. Every
mutant was applied from a file copy and restored with a `cmp` check, never `git checkout`.

**T051 — Finding 1 (HIGH), the administrator page's clock-correction guard.** `checkedText`'s
`Math.max(0, …)` clamp at `admin.html:129` survived deletion: 32 passed, 0 failed. `user-view.html`
does not need a test for the same clamp because `FR-006` gates its line below one refresh interval
and a clamped zero never renders; the administrator view has no gate and shows the age whenever the
instant is known, so there the clamp is what holds `FR-010`. Without it the page states
`Releases last checked in 5 hours.` — verified directly. `U38` added to the test list, its id added
to `T029`'s brackets, and one test added beside the `checkedText` ladder.

| Mutant | before T051 | after T051 |
| --- | --- | --- |
| `admin.html` drop `Math.max(0, …)` | SURVIVED | 1 failed |
| `user-view.html` drop `Math.max(0, …)` | SURVIVED | SURVIVED — equivalent, and the control: the interval gate makes it unreachable |

**T052 — Finding 2 (MED), the specification's own contradiction.** `spec.md`'s `US2-AS2` and its
matching edge case still said the stated age "keeps growing" when every source is disabled, which
`FR-002` and `A7` contradict. Reported by the loop at cycle 21 and left unamended by Phase 9. Both
passages now state the `FR-002` behaviour: no enabled source means no instant, so the page states no
age while the list still shows what is stored. No test changed — `A7` already asserted this.

**T053 — Finding 3 (MED), two characterizations pinned on one half.** Both mutants survived because
the test could not see the half they broke:

| Mutant | before T053 | after T053 |
| --- | --- | --- |
| `healthText` drops the cooldown instant (`when(s.cooldownUntil)` → `when(null)`) | SURVIVED | 1 failed |
| `artistLink` stops encoding the server id | SURVIVED | 1 failed |

`U33`: the assertion used `/^CoolingDown until .+ · …/` because `when()` calls
`Date.prototype.toLocaleString()`, whose output follows the machine's locale **and timezone**.
`load-page.js` already pinned `Intl.RelativeTimeFormat` for exactly this reason; it now pins `Date`'s
`toLocaleString` the same way, to `en-US` and UTC, so the exact sentence can be asserted. Production
is untouched and still passes nothing, so a Jellyfin operator keeps their own format. `U32`: the
sandbox served `serverId: () => 'srv-42'`, which needs no escaping, so only the artist-id half could
fail; the fixture now serves `'srv 42&x'`. Verified green under `de_DE`, `ja_JP`, `Asia/Tokyo` and
`America/Sao_Paulo`.

**T054 — Finding 4 (MED), a documented command that runs nothing.** `node --test tests/web/`
resolves the path as a module, runs no test and **exits 0**. Cycle 14 fixed the profile and CI but
not this feature's own files. Nine occurrences replaced with the glob form across `tasks.md` and
`tdd/test-list.md`, and that file's two stale claims — that the profile has no `node` entry yet, and
that `T033` adds it — corrected to `T006`, which did. The profile's `file:` command is unaffected:
`node --test tests/web/{file}` resolves to a real file once substituted.

**T055 — Finding 5 (MED), two cases of one test on one arm.** `RefreshIntervalHours` had no
`DailyTrigger` arm, so the daily case and the no-task case both landed on `_ => 24` and the daily
case proved nothing about a daily trigger. **I departed from the task text**, which offered only
"drop the daily case" or "add an arm if daily means something other than 24". Daily does mean 24, so
neither branch fitted. Dropping the case would have deleted the only test of a rule `001`'s `U118`
states explicitly ("24 for a daily trigger"), so instead the arm was made explicit —
`TaskTriggerInfoType.DailyTrigger => 24` — which changes no behaviour and keeps the rule pinned
against a future change to the default. This is the one production change in this phase. Proven by
two mutants that now fail on different arms:

| Mutant | Result |
| --- | --- |
| `DailyTrigger => 24` → `48` | 1 failed, at `ReleasesControllerTests.cs:129` (the daily case) |
| `_ => 24` → `48` | 1 failed, at the no-task case |

- suite after remediation: 195 passed, 0 failed (dotnet, unchanged); 33 passed, 0 failed (node, one
  test added by `T051`). `dotnet build --configuration Release` clean with `TreatWarningsAsErrors`
- `T056`–`T058` (the three LOW findings) are left open

## Phase 10, second part: the three LOW findings

No production code changed. Every mutant that the moved and rewritten tests are meant to catch was
re-run afterwards and still fails; each was applied from a file copy and restored with `cmp`.

**T056 — Finding 6, the exact key set in `exposure.test.js`.** Kept, and the rule it enforces is
now written where it is enforced. The objection was fair — no requirement states the set, so a
refactor exposing one more pure helper fails the test. That failure is the point: `T029` added
`checkedText` to `admin.html` with no behaviour on the test list, and both audits found the same
consequence, first three of four boundaries pinned on one side only, then the clock-correction
clamp with no test at all. The assertion is the gate that would have caught it, so it stays and the
comment says so. The removed `typeof` loops are not restored: every name in both key sets is called
as a function by one of the four test files, so callability is still enforced.

**T057 — Finding 7, duplicated fixtures.** `HOUR`, `DAY`, `NOW` and `ago` moved to
`tests/web/fixed-clock.js`, which also gained `ahead` for the clock-correction cases that both
pages now have. The two `NOW` values differed by a day for no reason and are now one. Added to the
profile's `helpers`. The two ladder tables stay separate on purpose: they pin two implementations
in two pages.

**T058 — Finding 8, a file that claimed to be one thing and was two.** Split rather than
re-labelled. `checkedText` moved out of `page-helpers.test.js` into `tests/web/checked.test.js`,
beside `staleness.test.js`, so the two copies of the unit ladder sit side by side — `user-view.html`
in one file, `admin.html` in the other — and an author changing one ladder can see the other.
`page-helpers.test.js` is characterization again, as its header always claimed. `esc.test.js` set
the precedent. `tdd/test-list.md` repointed `U36`, `U37` and `U38` at the new file, and the
profile's page-side conventions now state the one-file-per-subject rule.

Mutants re-run after the split, all caught:

| Mutant | Behaviour | Result |
| --- | --- | --- |
| `admin.html` drop `Math.max(0, …)` | U38 | 1 failed |
| `admin.html` day→week 14 d → 13 d | U36 | 1 failed |
| `admin.html` week→month 61 d → 60 d | U36 | 1 failed |
| `admin.html` over-a-year 365 d → 364 d | U36 | 1 failed |
| `healthText` drops the cooldown instant | U33 | 1 failed |
| `artistLink` stops encoding the server id | U32 | 1 failed |
| `user-view.html` day→week 14 d → 13 d | U25, A8 | 1 failed |
| `user-view.html` interval gate `<=` → `<` | U21 | 1 failed |

`A10` re-verified after adding two files: no `package.json`, every `require` resolves to a Node
builtin or a local file, and no network API is referenced anywhere under `tests/web`.

- suite: 195 passed, 0 failed (dotnet); 33 passed, 0 failed (node) under `en_US`, `de_DE`, `ja_JP`,
  `Asia/Tokyo` and `America/Sao_Paulo`. `dotnet build --configuration Release` clean with
  `TreatWarningsAsErrors`
- every finding of the second audit is now closed. `T037`, `T038` and `T049` remain open and none
  of them is a code change
