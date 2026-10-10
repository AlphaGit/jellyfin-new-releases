# Cycle Log: Explain each unmatched artist step by step

Append only. Newest last. Every entry's `red` block is the evidence that the test
existed and failed before the implementation.

## Baseline

- suite: `dotnet test --configuration Release` -> 361 passed, 0 failed
- suite: `node --test "tests/web/*.test.js"` -> 363 passed, 0 failed
- commit: `09033f8`
- recorded: cycle 0, before any change

Convention for this feature: an acceptance test held open stays uncommitted while its units run,
as in `007`. Cycle-log and test-list updates are committed in `docs:` batches. A C# cycle's
"suite" is `dotnet test`; a page cycle's is `node --test`. Both run before each docs batch.

## Cycle 1: U1 `LowScore(70)` is `low score (70)`

- test: `Matching/MatchReasonsTests.cs::LowScore_WritesTheBestScoreInParentheses` (new file),
  with `MatchReasons.LowScore` declared returning `""` so the test compiles
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~MatchReasonsTests.LowScore_WritesTheBestScoreInParentheses" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: Strings differ` Expected `"low score (70)"` Actual `""` (1 failed)
- green: `LowScore` interpolates the score. Suite -> dotnet 362 passed
- refactor: none needed
- commit: `4bb19b2`

## Cycle 2: U2 `Ambiguous(92, 90)` is `ambiguous (score 92 vs 90)`

- test: `Matching/MatchReasonsTests.cs::Ambiguous_WritesTheBestScoreThenTheRunnerUp` (new), with `Ambiguous` declared returning `""`
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~MatchReasonsTests.Ambiguous_WritesTheBestScoreThenTheRunnerUp" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: Strings differ` Expected `"ambiguous (score 92 vs 90)"` Actual `""` (1 failed)
- green: `Ambiguous` interpolates both scores. Suite -> dotnet 363 passed
- refactor: none needed
- commit: `392f7a0`

## Cycle 3: U3 MusicBrainz `no result` parses to failed step `Search` with no values

- test: `Matching/MatchReasonsTests.cs::Parse_MusicBrainzNoResult_FailsAtSearchWithNoValues` (new), with `FailedStep` and `Parse` declared returning null
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~MatchReasonsTests.Parse_MusicBrainzNoResult" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: Strings differ` Expected `"Search"` Actual `null` (1 failed)
- green: `Parse` returns `Search` with no values for that one text. Suite -> dotnet 364 passed
- refactor: none needed
- notes: first run compared whole `FailedStep` records, which holds arrays by reference: `Expected: FailedStep { Step = Search, Values = System.Int32[] } Actual: null`. Rewritten to compare the step and the values separately before any implementation, so the red stands
- commit: `63a3fa3`

## Cycle 4: U4 MusicBrainz `low score (70)` parses to `Score` with values `[70]`

- test: `Matching/MatchReasonsTests.cs::Parse_MusicBrainzLowScore_FailsAtScoreWithTheScore` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~MatchReasonsTests.Parse_MusicBrainzLowScore" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: Strings differ` Expected `"Score"` Actual `null` (1 failed)
- green: `Parse` matches `^low score \((\d+)\)$` (a generated regex) and returns `Score` with the number. Suite -> dotnet 365 passed. Behaviour commit `e8bc1f8`
- refactor: `Ambiguous` moved beside `LowScore`, ahead of `Parse`. Suite re-run: 365 passed (structure commit below)
- commit: `33206e1`

## Cycle 5: U5 MusicBrainz `ambiguous (score 92 vs 90)` parses to `Unique` with values `[92, 90]`

- test: `Matching/MatchReasonsTests.cs::Parse_MusicBrainzAmbiguous_FailsAtUniqueWithBothScores` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~MatchReasonsTests.Parse_MusicBrainzAmbiguous" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: Strings differ` Expected `"Unique"` Actual `null` (1 failed)
- green: `Parse` matches the ambiguous pattern and returns `Unique` with both numbers. Suite -> dotnet 366 passed. Behaviour commit `2f70e63`
- refactor: the two number reads go through one `Number(match, group)` helper. Suite re-run: 366 passed (structure commit below)
- commit: `59edd5a`

## Cycle 6: U6 Deezer `no result` parses to `Name`

- test: `Matching/MatchReasonsTests.cs::Parse_DeezerNoResult_FailsAtNameWithNoValues` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~MatchReasonsTests.Parse_DeezerNoResult" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: Strings differ` Expected `"Name"` Actual `null` (1 failed)
- green: `Parse` answers `Name` for Deezer `no result`. Suite -> dotnet 367 passed
- refactor: none needed
- commit: `3a7811b`

## Cycle 7: U7 Deezer `no corroborating album` parses to `Album`

- test: `Matching/MatchReasonsTests.cs::Parse_DeezerNoCorroboratingAlbum_FailsAtAlbumWithNoValues` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~MatchReasonsTests.Parse_DeezerNoCorroboratingAlbum" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: Strings differ` Expected `"Album"` Actual `null` (1 failed)
- green: the Deezer branch becomes a switch over the two texts. Suite -> dotnet 368 passed. Behaviour commit `13b83b2`
- refactor: `NoResult` and `NoCorroboratingAlbum` constants replace the literals in `Parse`. Suite re-run: 368 passed (structure commit below)
- commit: `2dec14d`

## Cycle 8: U8 an unknown text parses to null

- test: `Matching/MatchReasonsTests.cs::Parse_ATextNoReleasedVersionWrites_IsNull` (new theory, both sources)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~MatchReasonsTests.Parse_ATextNoReleasedVersionWrites" -- RunConfiguration.TreatNoTestsAsError=true`
  -> passed on first run (2 passed): `Parse` already falls through to null. Deliberate mutant: the Deezer switch default and the MusicBrainz fall-through return a step -> `Assert.Null() Failure: Value is not null` Actual `FailedStep { Step = Name, … }` and `{ Step = Search, … }` (2 failed). Code restored exactly from a copy
- green: no production change. Suite -> dotnet 370 passed
- refactor: none needed
- commit: `ff4b55f`

## Cycle 9: U9 an empty text parses to null

- test: `Matching/MatchReasonsTests.cs::Parse_AnEmptyText_IsNull` (new theory, both sources)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~MatchReasonsTests.Parse_AnEmptyText" -- RunConfiguration.TreatNoTestsAsError=true`
  -> passed on first run (2 passed). Deliberate mutant: an empty text returns `Search` -> `Assert.Null() Failure: Value is not null` Actual `FailedStep { Step = Search, … }` (2 failed). Code restored exactly from a copy
- green: no production change. Suite -> dotnet 372 passed
- refactor: none needed
- commit: `63154a7`

## Cycle 10: U10 a MusicBrainz-only text under the Deezer source parses to null

- test: `Matching/MatchReasonsTests.cs::Parse_AMusicBrainzOnlyTextUnderDeezer_IsNull` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~MatchReasonsTests.Parse_AMusicBrainzOnlyTextUnderDeezer" -- RunConfiguration.TreatNoTestsAsError=true`
  -> passed on first run (1 passed). First mutant (Deezer branch only for texts starting "no") still passed: it never let a MusicBrainz pattern apply to Deezer, so it was not a mutant of this behaviour. Second mutant: the Deezer branch skipped and the MusicBrainz-only guard removed -> `Assert.Null() Failure: Value is not null` Actual `FailedStep { Step = Score, … }` (1 failed). Code restored exactly from a copy
- green: no production change. Suite -> dotnet 373 passed
- refactor: none needed
- commit: `03a72f7`

## Cycle 11: U11 steps for MusicBrainz failed at `Search`: Id Failed, Search Failed, Score NotReached, Unique NotReached

- test: `Matching/MatchReasonsTests.cs::Steps_MusicBrainzFailedAtSearch_IdAndSearchFailed_TheRestNotReached` (new), with `StepState` (Model/Enums.cs), `MatchStep` and `Steps` declared returning `[]`
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~MatchReasonsTests.Steps_MusicBrainzFailedAtSearch" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: Collections differ` Expected `[("Id", Failed), ("Search", Failed), ("Score", NotReached), ("Unique", NotReached)]` Actual `[]` (1 failed)
- green: `Steps` walks the MusicBrainz order: after the failed step `NotReached`; the failed step and `Id` `Failed`; the rest `Passed`. No values yet (U12 drives them). Suite -> dotnet 374 passed
- refactor: none needed
- commit: `6773ed9`

## Cycle 12: U12 steps for MusicBrainz failed at `Score`: Id Failed, Search Passed, Score Failed with its values, Unique NotReached

- test: `Matching/MatchReasonsTests.cs::Steps_MusicBrainzFailedAtScore_CarriesTheScoreOnTheScoreStepOnly` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~MatchReasonsTests.Steps_MusicBrainzFailedAtScore" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: Collections differ` Expected `[… ("Score", Failed, "70"), …]` Actual `[… ("Score", Failed, ""), …]` (1 failed)
- green: the failed step carries `failed.Values`; every other step none. Suite -> dotnet 375 passed
- refactor: none needed
- commit: `5a86145`

## Cycle 13: U13 steps for MusicBrainz failed at `Unique` (last step)

- test: `Matching/MatchReasonsTests.cs::Steps_MusicBrainzFailedAtUnique_EveryEarlierStepButIdPassed` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~MatchReasonsTests.Steps_MusicBrainzFailedAtUnique" -- RunConfiguration.TreatNoTestsAsError=true`
  -> passed on first run (1 passed): U11–U12 already generalised the walk. Deliberate mutant: the walk drops the last step (`order.Take(order.Length - 1)`) -> `Assert.Equal() Failure: Collections differ` Actual ends at `("Score", Passed, "")` (1 failed). Code restored exactly from a copy
- green: no production change. Suite -> dotnet 376 passed
- refactor: none needed
- commit: `06bedb0`

## Cycle 14: U14 steps for Deezer failed at `Name` (first step)

- test: `Matching/MatchReasonsTests.cs::Steps_DeezerFailedAtName_NameFailed_AlbumNotReached` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~MatchReasonsTests.Steps_DeezerFailedAtName" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: Collections differ` Expected `[("Name", Failed), ("Album", NotReached)]` Actual `[("Id", NotReached), ("Search", NotReached), ("Score", NotReached), ("Unique", NotReached)]` (1 failed)
- green: `Steps` picks the Deezer order for Deezer. Suite -> dotnet 377 passed. Behaviour commit `4e4fc7e`
- refactor: the two orders become `MusicBrainzSteps` and `DeezerSteps` fields. Suite re-run: 377 passed (structure commit below)
- commit: `49ad161`

## Cycle 15: U15 steps for Deezer failed at `Album` (last step): Name Passed, Album Failed

- test: `Matching/MatchReasonsTests.cs::Steps_DeezerFailedAtAlbum_NamePassed_AlbumFailed` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~MatchReasonsTests.Steps_DeezerFailedAtAlbum" -- RunConfiguration.TreatNoTestsAsError=true`
  -> passed on first run (1 passed). Deliberate mutant: the always-failed rule keyed on the first step (`i == 0`) instead of `Id` -> `Assert.Equal() Failure: Collections differ` Actual `[("Name", Failed), ("Album", Failed)]` (1 failed). Code restored exactly from a copy
- green: no production change. Suite -> dotnet 378 passed
- refactor: none needed
- commit: `231ebcf`

## Cycle 16: U16 every Unmatched reason MusicBrainz writes parses to its failed step

- test: `Sources/MusicBrainzSourceTests.cs::MatchArtistAsync_EveryUnmatchedReason_ParsesToItsFailedStep` (new theory over the empty, low-score and ambiguous fixtures)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~MusicBrainzSourceTests.MatchArtistAsync_EveryUnmatchedReason" -- RunConfiguration.TreatNoTestsAsError=true`
  -> passed on first run (3 passed): the source already writes the texts `MatchReasons` reads. Deliberate mutant: the source rewords its ambiguous reason to `ambiguous (100 vs 96)` -> `Assert.Equal() Failure: Strings differ` Expected `"Unique"` Actual `null` (1 failed, 2 passed). Source restored exactly from a copy. Test commit `572334f`
- green: no production change. Suite -> dotnet 381 passed
- refactor: the source builds its three reasons through `MatchReasons.NoResult`, `LowScore` and `Ambiguous`, so the writer and the parser share one definition. Suite re-run: 381 passed (structure commit below)
- commit: `cdd2f0a`

## Cycle 17: U23 every Unmatched reason Deezer writes parses to its failed step

- test: `Sources/DeezerSourceTests.cs::MatchArtistAsync_EveryUnmatchedReason_ParsesToItsFailedStep` (new theory: empty search, near names only, exact names with no album in common)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~DeezerSourceTests.MatchArtistAsync_EveryUnmatchedReason" -- RunConfiguration.TreatNoTestsAsError=true`
  -> passed on first run (3 passed). Deliberate mutant: the source rewords its album reason to `no album in common` -> `Assert.Equal() Failure: Strings differ` Expected `"Album"` Actual `null` (1 failed, 2 passed). Source restored exactly from a copy. Test commit `e6ba3ef`
- green: no production change. Suite -> dotnet 384 passed
- refactor: the source builds both reasons through `MatchReasons.NoResult` and `NoCorroboratingAlbum`. Suite re-run: 384 passed (structure commit below)
- commit: `9cf5d2a`

## Cycle 18: U30 an artist Unmatched only at a source outside the enabled set is not listed

- test: `Storage/ArtistRepositoryTests.cs::GetCountsAsync_AnArtistUnmatchedOnlyAtADisabledSource_IsNotListed` (new). `GetCountsAsync` gained an `IReadOnlyList<string> enabledSources` parameter so the test compiles; the parameter was ignored for the red. Its two callers pass the sources mechanically: `AdminController` the configured enabled set, the existing counts test both sources
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~ArtistRepositoryTests.GetCountsAsync_AnArtistUnmatchedOnlyAtADisabledSource" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Empty() Failure: Collection was not empty` (1 failed)
- green: the unmatched query keeps only rows whose source is in `json_each(@enabled)`. Suite -> dotnet 385 passed, 2 failed (A10, A11, held open)
- refactor: none needed
- notes: held open since before this cycle, uncommitted: `Acceptance/ConfigureAndRunTests.cs::Spec008_A11_…` red `Assert.Empty() Failure: Collection was not empty` (the disabled-Deezer artist still listed); `Spec008_A10_…` red `KeyNotFoundException` (no `reasons` field). After this cycle A11 gets past the hiding step and fails on the missing `reasons` field
- commit: `e27abd4`

## Cycle 19: U31 a listed artist has one entry per enabled source, in the order given

- test: `Storage/ArtistRepositoryTests.cs::GetCountsAsync_AListedArtistHasOneEntryPerEnabledSource_InTheOrderGiven` (new), with `UnmatchedAt.Status` declared (default `Unmatched`) so the test compiles
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~ArtistRepositoryTests.GetCountsAsync_AListedArtistHasOneEntryPerEnabledSource" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: Collections differ` Expected `[("deezer", Unmatched), ("musicbrainz", Matched)]` Actual `[("deezer", Unmatched)]` (1 failed)
- green: the query lists every row at an enabled source for any artist with an Unmatched row at one, ordered by the source's position in `@enabled` (`json_each` key), and reads the status. Suite -> dotnet 386 passed, 2 failed (A10, A11 held open). Behaviour commit `04d1cbd`
- refactor: `UnmatchedAt` becomes `(Source, Status, Reason)` with no default. Suite re-run: same counts (structure commit below)
- notes: existing test changed as part of this behaviour, before the implementation: `GetCountsAsync_ReportsTotalsMatchedPerSourceAndUnmatchedReasons` asserted that a listed artist carries only its Unmatched sources; FR-002 replaces that with every enabled source. Rewritten to the FR-002 rows (MB Only now also carries its MusicBrainz match) plus the three Unmatched reasons; observed red `Assert.Equal() Failure: Collections differ` (Actual lacks the MusicBrainz Matched row) before the query changed
- commit: `d4aadd6`

## Cycle 20: U32 an enabled source with no row is listed as not checked

- test: `Storage/ArtistRepositoryTests.cs::GetCountsAsync_AnEnabledSourceWithNoRow_IsListedAsPending` (new). "Not checked" is carried as `MatchStatus.Pending` in the read model; the response maps it to `NotChecked` (U44)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~ArtistRepositoryTests.GetCountsAsync_AnEnabledSourceWithNoRow" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: Collections differ` Expected `[("musicbrainz", Unmatched), ("deezer", Pending)]` Actual `[("musicbrainz", Unmatched)]` (1 failed)
- green: rows are collected per artist by source, then each listed artist gets one entry per enabled source in the given order, a missing one as `Pending`. Suite -> dotnet 387 passed, 2 failed (A10, A11 held open). Behaviour commit `22dad4f`
- refactor: the SQL no longer joins `json_each` to order sources, since the C# step orders them from the enabled list. Suite re-run: same counts (structure commit below)
- commit: `8db89a2`

## Cycle 21: U33 an enabled source with a `Pending` row is listed as not checked

- test: `Storage/ArtistRepositoryTests.cs::GetCountsAsync_AnEnabledSourceWithAPendingRow_IsListedAsPending` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~ArtistRepositoryTests.GetCountsAsync_AnEnabledSourceWithAPendingRow" -- RunConfiguration.TreatNoTestsAsError=true`
  -> passed on first run (1 passed): the row status is read as stored. Deliberate mutant: a stored `Pending` read as `Matched` -> `Assert.Equal() Failure: Values differ` Expected `Pending` Actual `Matched` (1 failed). Code restored exactly from a copy
- green: no production change. Suite -> dotnet 388 passed, 2 failed (A10, A11 held open)
- refactor: none needed
- commit: `5bcc90e`

## Cycle 22: U34 `Reason` is null for `Matched` and not-checked entries

- test: `Storage/ArtistRepositoryTests.cs::GetCountsAsync_MatchedAndNotCheckedEntries_HaveNoReason` (new), with `UnmatchedAt.Reason` made `string?`; `AdminController` passes `?? string.Empty` to the old DTO until U40 reshapes it
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~ArtistRepositoryTests.GetCountsAsync_MatchedAndNotCheckedEntries" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: Collections differ` Expected `[("musicbrainz", null), ("deezer", "no result"), ("other", null)]` Actual `[("musicbrainz", ""), …, ("other", "")]` (1 failed)
- green: the repository keeps a stored null reason null and fills a not-checked entry with null. Suite -> dotnet 389 passed, 2 failed (A10, A11 held open)
- refactor: none needed
- commit: `29d3375`

## Cycle 23: U35 a disabled source keeps its row, and enabling it lists the artist with that reason

- test: `Storage/ArtistRepositoryTests.cs::GetCountsAsync_ADisabledSourcesResultIsKept_AndListsAgainOnceEnabled` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~ArtistRepositoryTests.GetCountsAsync_ADisabledSourcesResultIsKept" -- RunConfiguration.TreatNoTestsAsError=true`
  -> passed on first run (1 passed): U30 filters at read time and deletes nothing. Deliberate mutant: `GetCountsAsync` first deletes the rows of sources outside the enabled set -> `Assert.Single() Failure: The collection was empty` (1 failed). Code restored exactly from a copy
- green: no production change. Suite -> dotnet 390 passed, 2 failed (A10, A11 held open)
- refactor: none needed
- commit: `7839d90`
