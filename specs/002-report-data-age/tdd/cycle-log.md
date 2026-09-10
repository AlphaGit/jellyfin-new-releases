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
