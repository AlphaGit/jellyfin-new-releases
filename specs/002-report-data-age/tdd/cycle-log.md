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
