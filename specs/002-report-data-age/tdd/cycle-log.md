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
