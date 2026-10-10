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

## Cycle 24: U49, U40 and U53 — the reasons shape, in one commit (deviation)

The page fixture `tests/fixtures/pages/admin-status.json` is read by both `ResponseNamingTests`
(against the server records) and `render-status.test.js` (by the page). Changing the shape on one
side alone leaves the other suite red, so these three behaviours went green in one step and one
commit. Each had its own red first:

- U49 test: `Api/ResponseNamingTests.cs::AdminStatusResponse_AsTheAdminStatusEndpointDeclaresIt_CarriesTheNamesTheAdministratorPageReads`
  (existing). The fixture was rewritten to the 008 shape (research R8) with no code change.
  red: `dotnet test --configuration Release --filter "FullyQualifiedName~ResponseNamingTests.AdminStatusResponse" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: Collections differ` Expected `["unmatched[]"] = ["jellyfinId", "name", "reasons"]` Actual `["unmatched[]"] = ["hint", "jellyfinId", "name", "sources"]` (1 failed)
- U40 test: `Api/AdminControllerTests.cs::Status_EachUnmatchedArtist_CarriesReasons_AndNoSourcesOrHint` (new), on the serialized response.
  red: `dotnet test --configuration Release --filter "FullyQualifiedName~AdminControllerTests.Status_EachUnmatchedArtist_CarriesReasons" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: Collections differ` Expected `["jellyfinId", "name", "reasons"]` Actual `["jellyfinId", "name", "sources", "hint"]` (1 failed)
- U53 test: `tests/web/render-status.test.js::each unmatched artist has one Reason line per reasons entry, led by the source display name` (new).
  red: `node --test tests/web/render-status.test.js` (no node single-test command)
  -> `TypeError: Cannot read properties of undefined (reading 'map')` at `admin.html:85` (the page reads `u.sources`), 9 failed
- green: `UnmatchedArtistDto(JellyfinId, Name, Reasons)` with `ReasonDto`, `StepDto`, `CandidateDto`
  per contracts/http-api.md; `UnmatchedSourceDto` and `AdminController.UnmatchedHint` removed. The
  controller fills `Source`, `State` (the stored status name) and `Reason` only; U41–U48 drive the
  rest. The page writes one `<div class="nr-reason">{displayName}: {reason or state}</div>` per
  entry and leaves How to fix empty. Suites -> dotnet 393 passed (A10 and A11 included), node 363 passed
- existing tests changed with this behaviour, because FR-013 removes what they pinned:
  - `ResponseNamingTests.PopulatedAdminStatusResponse` builds the new shape, every field non-null at least once.
  - `AdminControllerTests.Status_ReportsSourceHealthRunTriggerArtistCountsAndUnmatchedArtistsWithHint` asserted the fixed hint; renamed `…WithReasons` and asserts the reason through `Reasons`.
  - `render-status.test.js::every artist no source matched is listed with its reasons and the hint` asserted `musicbrainz: NoConfidentMatch<br>…` and the hint sentence; replaced by the U53 test. U57 pins that the hint is gone.
- refactor: none needed
- commit: `e542fd3`

## Cycle 25: A10 and A11 close — a disabled source's unmatched artists

- tests: `Acceptance/ConfigureAndRunTests.cs::Spec008_A10_WithDeezerDisabled_NoListedArtistHasADeezerReason`
  and `Spec008_A11_AnArtistUnmatchedOnlyAtDisabledDeezer_IsHidden_AndReturnsWhenDeezerIsEnabledAgain`,
  held open (uncommitted) since cycle 18 with the reds recorded there
- green: after U30–U35 and cycle 24 both pass in the full suite with no change to them. Suite -> dotnet 393 passed
- commit: `ebccd8e`

## Cycle 26: U41 `Reasons` has one entry per enabled source, in the `Sources` order

- test: `Api/AdminControllerTests.cs::Status_Reasons_FollowTheSourcesOrder_OnePerEnabledSource` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~AdminControllerTests.Status_Reasons_FollowTheSourcesOrder" -- RunConfiguration.TreatNoTestsAsError=true`
  -> passed on first run (1 passed): the enabled set is a two-entry `HashSet` that enumerates in insertion order, MusicBrainz then Deezer. Deliberate mutant: the controller reverses that order -> `Assert.Equal() Failure: Collections differ` Expected `["musicbrainz", "deezer"]` Actual `["deezer", "musicbrainz"]` (1 failed). Code restored exactly from a copy. Test commit `c4ab07b`
- green: no production change. Suite -> dotnet 394 passed
- refactor: the controller passes the enabled ids of the `sources` list it already built, so the order no longer depends on how a `HashSet` enumerates. Suite re-run: 394 passed (structure commit below)
- commit: `223b34d`

## Cycle 27: U42 an `ambiguous (score 92 vs 90)` MusicBrainz row gives `FailedStep` `Unique`, the four states, and the values on `Unique` only

- test: `Api/AdminControllerTests.cs::Status_AnAmbiguousMusicBrainzReason_GivesTheUniqueTrailWithBothScores` (new), with a `OnlyReasonAsync` helper that enables one source
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~AdminControllerTests.Status_AnAmbiguousMusicBrainzReason" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: Strings differ` Expected `"Unique"` Actual `null` (1 failed)
- green: `AdminController.ToReason` parses an Unmatched entry through `MatchReasons.Parse`, builds `Steps` from `MatchReasons.Steps`, and writes values only where a step has some. Suites -> dotnet 395 passed, node 363 passed
- refactor: none needed
- commit: `aa99222`

## Cycle 28: U43 a `Matched` entry has null `FailedStep`, empty `Steps` and null `Reason`

- test: `Api/AdminControllerTests.cs::Status_AMatchedSource_IsMatched_WithNoFailedStepNoStepsAndNoReason` (new), with a helper that puts Deezer beside a MusicBrainz failure
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~AdminControllerTests.Status_AMatchedSource" -- RunConfiguration.TreatNoTestsAsError=true`
  -> passed on first run (1 passed): U34 and U42 already give a match no reason and no trail. Deliberate mutant: the controller falls back to the state name for a missing reason -> `Assert.Equal() Failure: Values differ` Expected `("Matched", null, 0, null)` Actual `("Matched", null, 0, "Matched")` (1 failed). Code restored exactly from a copy
- green: no production change. Suite -> dotnet 396 passed
- refactor: none needed
- commit: `e8484ac`

## Cycle 29: U44 a not-checked entry has `State` `NotChecked`, null `FailedStep`, empty `Steps` and null `Reason`

- test: `Api/AdminControllerTests.cs::Status_ASourceNotYetRun_IsNotChecked_WithNoFailedStepNoStepsAndNoReason` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~AdminControllerTests.Status_ASourceNotYetRun" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: Values differ` Expected `("NotChecked", null, 0, null)` Actual `("Pending", null, 0, null)` (1 failed)
- green: the controller states `Pending` as `NotChecked`. Suite -> dotnet 397 passed
- refactor: none needed
- commit: `82f36d9`

## Cycle 30: U45 an unrecognized reason gives `Unmatched`, null `FailedStep`, empty `Steps`, and the stored text

- test: `Api/AdminControllerTests.cs::Status_AnUnrecognizedReason_IsUnmatched_WithNoFailedStepNoSteps_AndKeepsItsText` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~AdminControllerTests.Status_AnUnrecognizedReason" -- RunConfiguration.TreatNoTestsAsError=true`
  -> passed on first run (1 passed): an unparsed reason already yields no trail. Deliberate mutant: an unparsed Unmatched reason defaults to a `Search` failure -> `Assert.Equal() Failure: Values differ` Actual `("Unmatched", "Search", 4, "rate limited")` (1 failed). Code restored exactly from a copy
- green: no production change. Suite -> dotnet 398 passed
- refactor: none needed
- commit: `7847fe8`

## Cycle 31: U46 with Deezer disabled in the configuration, an artist Unmatched only at Deezer is absent

- test: `Api/AdminControllerTests.cs::Status_WithDeezerDisabled_AnArtistOnlyDeezerFailed_IsNotListed` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~AdminControllerTests.Status_WithDeezerDisabled" -- RunConfiguration.TreatNoTestsAsError=true`
  -> passed on first run (1 passed): U30 and U41 already pass the enabled sources. Deliberate mutant: the controller passes every source, enabled or not -> `Assert.Empty() Failure: Collection was not empty` (1 failed). Code restored exactly from a copy
- green: no production change. Suite -> dotnet 399 passed
- refactor: none needed
- commit: `2e46074`

## Cycle 32: U51 after a refresh whose MusicBrainz search scores below 85, the status gives the Score trail

- test: `Acceptance/ConfigureAndRunTests.cs::Spec008_U51_AfterARefreshWhoseMusicBrainzSearchScoresBelow85_TheStatusGivesTheScoreTrail` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~ConfigureAndRunTests.Spec008_U51" -- RunConfiguration.TreatNoTestsAsError=true`
  -> passed on first run (1 passed). Deliberate mutant: the controller drops every step value -> `Assert.Equal() Failure: Collections differ` Actual `("Score", "Failed", "")` (1 failed). Code restored exactly from a copy
- green: no production change. Suite -> dotnet 400 passed
- refactor: none needed
- notes: test-after: tasks.md placed this test (T027) before the controller work, but it was written after U42, so it never saw the behaviour missing. It composes units each driven by its own red (U4, U12, U16, U42); the mutant shows it can fail
- commit: `d0c020c`

## Cycle 33: U52 the table header reads Artist, Reason, How to fix

- test: `tests/web/render-status.test.js::the unmatched table is headed Artist, Reason, How to fix` (new, reads the page source)
- red: `node --test tests/web/render-status.test.js`
  -> `AssertionError` actual `'Sources'` expected `'Reason'` (1 failed)
- green: `<th>Sources</th>` becomes `<th>Reason</th>`. Suite -> node 364 passed
- refactor: none needed
- commit: `b984856`

## Cycle 34: A9 and U54 a matched source reads "<Source>: Matched", with no trail

- test: `tests/web/render-status.test.js::a matched source reads "<Source>: Matched" and nothing else` (new). One test serves both: the outer behaviour (A9) and the page unit (U54) assert the same line
- red: `node --test tests/web/render-status.test.js`
  -> passed on first run (14 passed): cycle 24 writes the state when there is no reason. Deliberate mutant: the page appends a mark to a state line -> `not ok 11 - a matched source reads "<Source>: Matched" and nothing else` (actual `'MusicBrainz: Matched ✓'`). Page restored exactly from a copy
- green: no production change. Suite -> node 365 passed
- refactor: none needed
- commit: `a74d9b7`

## Cycle 35: U55 a not-checked entry reads `Not checked`

- test: `tests/web/render-status.test.js::a source not yet checked reads "<Source>: Not checked"` (new)
- red: `node --test tests/web/render-status.test.js`
  -> `not ok 12` actual `'Deezer: NotChecked'` expected `'Deezer: Not checked'` (1 failed)
- green: the page writes `Not checked` for the `NotChecked` state. Suite -> node 366 passed
- refactor: none needed
- commit: `09172c5`

## Cycle 36: U56 an entry with no failed step shows its stored reason text, escaped

- test: `tests/web/render-status.test.js::an entry with no failed step shows its stored reason text, escaped` (new, a fixture copy with a fourth artist)
- red: `node --test tests/web/render-status.test.js`
  -> passed on first run (16 passed): cycle 24 writes the reason through `esc`. Deliberate mutant: the reason written without `esc` -> `not ok 13 - an entry with no failed step shows its stored reason text, escaped` (1 failed). Page restored exactly from a copy
- green: no production change. Suite -> node 367 passed
- refactor: none needed
- commit: `3c3f126`

## Cycle 37: U57 the old fixed hint sentence appears nowhere on the page

- test: `tests/web/render-status.test.js::the old fixed hint sentence appears nowhere on the page` (new, rendered rows and page source)
- red: `node --test tests/web/render-status.test.js`
  -> passed on first run (17 passed): cycle 24 removed the hint. Deliberate mutant: the page writes a fixed "…picks it up on the next refresh." in How to fix -> `not ok 14 - the old fixed hint sentence appears nowhere on the page` (1 failed). Page restored exactly from a copy
- green: no production change. Suite -> node 368 passed
- refactor: none needed
- commit: `4e82078`

## Cycle 38: U39 a `001` database with an `artist_source` row migrates to `002`, keeping the row, with `match_detail` null

- test: `Storage/DatabaseTests.cs::OpenAsync_ADatabaseAtSchema001_MigratesTo002_KeepingItsArtistSourceRows` (new): builds the file from the embedded `001_initial.sql` with one row, then opens it
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~DatabaseTests.OpenAsync_ADatabaseAtSchema001" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Microsoft.Data.Sqlite.SqliteException : SQLite Error 1: 'no such column: match_detail'.` (1 failed)
- green: `Storage/Migrations/002_match_detail.sql` adds the nullable column. Suite -> dotnet 401 passed; node 368 passed, 6 failed (A1–A6, held open, see below)
- refactor: none needed
- notes: existing tests changed with this behaviour: `OpenAsync_FirstOpenAppliesTheInitialMigration`, `OpenAsync_SecondOpenFromAFreshInstanceAppliesNothing` and `OpenAsync_TwoConcurrentFirstOpensRunTheMigrationOnce` pinned the schema at version 1 / one `schema_version` row; plan R1 adds migration 002, so they failed on the new file (3 failed) and now assert a `LatestSchema = 2` constant. A1–A6 (`tests/web/unmatched-acceptance.test.js`, new, uncommitted) opened US1’s outer loop before this cycle: red `node --test tests/web/unmatched-acceptance.test.js` -> 6 failed, `actual: ''` (the How to fix cell is empty)
- commit: `61efe6b`

## Cycle 39: U17 `artist_search_ambiguous.json` gives 3 candidates in score order with name, score and ID

- test: `Sources/MusicBrainzSourceTests.cs::MatchArtistAsync_Ambiguous_KeepsTheCandidatesInScoreOrderWithScoreAndId` (new), with `MatchDetail`, `MatchCandidate` and an optional `ArtistMatch.Detail` declared so it compiles
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~MusicBrainzSourceTests.MatchArtistAsync_Ambiguous_KeepsTheCandidates" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: Collections differ` Expected the three `MatchCandidate`s Actual `MatchCandidate[] []` (1 failed)
- green: the search rows also read `name`, and the ambiguous result carries every sorted candidate. No cap yet (U18). Suite -> dotnet 402 passed
- refactor: none needed
- commit: `731e283`

## Cycle 40: U18 a search returning 4 or more results keeps only the first 3 candidates

- test: `Sources/MusicBrainzSourceTests.cs::MatchArtistAsync_FiveResultsScoringBelow85_KeepsOnlyTheFirstThree` (new, five rows through `SourceJson.MusicBrainz.Search`)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~MusicBrainzSourceTests.MatchArtistAsync_FiveResultsScoringBelow85" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: Collections differ` Expected `["mbid-1", "mbid-2", "mbid-3"]` Actual `[]` (1 failed)
- green: one `detail` of the first 3 sorted candidates serves the low-score and the ambiguous results. Suite -> dotnet 403 passed
- refactor: none needed
- commit: `8ac89d3`

## Cycle 41: U19 `artist_search_empty.json` gives an empty candidate list

- test: `Sources/MusicBrainzSourceTests.cs::MatchArtistAsync_NoResults_RecordsAnEmptyCandidateList` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~MusicBrainzSourceTests.MatchArtistAsync_NoResults_RecordsAnEmptyCandidateList" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.NotNull() Failure: Value is null` (1 failed)
- green: a `no result` carries `new MatchDetail([])`. Suite -> dotnet 404 passed
- refactor: none needed
- notes: existing test changed with this behaviour: `MatchArtistAsync_NoResults_IsUnmatchedAsNoResult` compared the whole record with `ArtistMatch.Unmatched("no result")`, which has no detail; it failed after the change (1 failed) and now compares status and reason, which is what it names
- commit: `422c4c6`

## Cycle 42: U20 and U21 — a MusicBrainz match carries no detail (existing tests credited)

- tests: `Sources/MusicBrainzSourceTests.cs::MatchArtistAsync_SnapshotWithMbid_IsMatchedWithoutAnyRequest` (U20)
  and `::MatchArtistAsync_ConfidentTopResult_IsMatched` (U21), both existing. Each compares the whole
  result with `ArtistMatch.Matched(id)`, whose `Detail` is null, so each already asserts no detail.
- check: deliberate mutant, a match by stored ID and a confident match both carry a `MatchDetail`
  -> `dotnet test … --filter "…SnapshotWithMbid_IsMatchedWithoutAnyRequest|…ConfidentTopResult_IsMatched"`
  -> `Assert.Equal() Failure: Values differ` Actual `… Detail = MatchDetail { … } }` (2 failed). Source restored exactly from a copy
- no new test, no production change, no commit

## Cycle 43: U22 an Unmatched MusicBrainz attempt sends the same requests as before

- test: `Sources/MusicBrainzSourceTests.cs::MatchArtistAsync_Ambiguous_SendsOnlyTheSearch` (new); a lookup stub answers any extra artist request so a change that adds one fails on the assertion, not on a missing stub
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~MusicBrainzSourceTests.MatchArtistAsync_Ambiguous_SendsOnlyTheSearch" -- RunConfiguration.TreatNoTestsAsError=true`
  -> passed on first run (1 passed). Deliberate mutant: the match looks up its top candidate before deciding -> first without the lookup stub it failed on the unstubbed request (not the assertion), so the stub was added; then `Assert.Equal() Failure: Collections differ` Actual adds `https://musicbrainz.org/ws/2/artist/5b11f4ce-…` (1 failed). Source restored exactly from a copy
- green: no production change. Suite -> dotnet 405 passed
- refactor: none needed
- commit: `f8d01f2`

## Cycle 44: U24 `search_artist_exact.json` for `Daft Punk Live` gives `no result` and the first 3 of the 5 names, with no score or ID

- test: `Sources/DeezerSourceTests.cs::MatchArtistAsync_NoExactName_KeepsTheFirstThreeNamesInResponseOrder` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~DeezerSourceTests.MatchArtistAsync_NoExactName_KeepsTheFirstThreeNames" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: Collections differ` Expected `[MatchCandidate { Name = Daft Punk, … }, { Name = Daft Punk - Stardust, … }, { Name = Daft Punk, … }]` Actual `[]` (1 failed)
- green: the match keeps the first 3 search rows by name and attaches them to `no result`. Suite -> dotnet 406 passed
- refactor: none needed
- commit: `e9b3a75`

## Cycle 45: U25 `search_artist_empty.json` gives `no result` and an empty candidate list

- test: `Sources/DeezerSourceTests.cs::MatchArtistAsync_NoSearchResults_RecordsAnEmptyCandidateList` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~DeezerSourceTests.MatchArtistAsync_NoSearchResults_RecordsAnEmptyCandidateList" -- RunConfiguration.TreatNoTestsAsError=true`
  -> passed on first run (1 passed): U24 attaches whatever the search returned, nothing included. Deliberate mutant: no detail when the search returned nobody -> `Assert.NotNull() Failure: Value is null` (1 failed). Source restored exactly from a copy
- green: no production change. Suite -> dotnet 407 passed
- refactor: none needed
- commit: `8569591`

## Cycle 46: U26 `search_artist_homonyms.json` with no album in common gives `ExactNameCount` 2

- test: `Sources/DeezerSourceTests.cs::MatchArtistAsync_NoCorroboratingAlbum_RecordsHowManyExactNameArtistsWereChecked` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~DeezerSourceTests.MatchArtistAsync_NoCorroboratingAlbum_RecordsHowMany" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: Values differ` Expected `("no corroborating album", 2)` Actual `("no corroborating album", null)` (1 failed)
- green: `no corroborating album` carries the returned names and the exact-name count. Suite -> dotnet 408 passed
- refactor: none needed
- notes: existing test changed with this behaviour: `MatchArtistAsync_ExactNameWithoutACorroboratingAlbum_IsUnmatchedWithReason` compared the whole record with a bare `Unmatched`; it failed after the change (1 failed) and now compares status and reason, which is what it names
- commit: `7bce8b3`

## Cycle 47: U27 the album check records `AlbumsCompared` across both same-name artists

- test: `Sources/DeezerSourceTests.cs::MatchArtistAsync_NoCorroboratingAlbum_RecordsTheAlbumsComparedAcrossBothArtists` (new; 2 and 3 first-page albums served)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~DeezerSourceTests.MatchArtistAsync_NoCorroboratingAlbum_RecordsTheAlbumsCompared" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: Values differ` Expected `5` Actual `null` (1 failed)
- green: the match sums the first-page items it already reads for each candidate. Suite -> dotnet 409 passed
- refactor: none needed
- commit: `de1e98a`

## Cycle 48: U28 — a corroborated Deezer match carries no detail (existing tests credited)

- tests: `Sources/DeezerSourceTests.cs::MatchArtistAsync_ExactNameWhoseFirstAlbumsPageContainsALibraryAlbum_IsMatched`
  and `::MatchArtistAsync_TwoExactNameHomonyms_OnlyTheSecondCorroborated_MatchesTheSecond`, both existing;
  each compares the whole result with `ArtistMatch.Matched(id)`, whose `Detail` is null
- check: deliberate mutant, a corroborated match carries the returned names -> `Assert.Equal() Failure: Values differ`
  Actual `… Detail = MatchDetail { … } }` (2 failed). Source restored exactly from a copy
- no new test, no production change, no commit

## Cycle 49: U29 an Unmatched Deezer attempt sends the same requests as before

- test: `Sources/DeezerSourceTests.cs::MatchArtistAsync_NoCorroboratingAlbum_SendsOnlyTheSearchAndEachFirstAlbumPage` (new; a stub answers any added artist request)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~DeezerSourceTests.MatchArtistAsync_NoCorroboratingAlbum_SendsOnlyTheSearch" -- RunConfiguration.TreatNoTestsAsError=true`
  -> passed on first run (1 passed). Deliberate mutant: the check also looks up each candidate -> `Assert.Equal() Failure: Collections differ` Actual adds `https://api.deezer.com/artist/415?fmt=json` and `…/278793911?fmt=json` (1 failed). Source restored exactly from a copy
- green: no production change. Suite -> dotnet 410 passed
- refactor: none needed
- commit: `42a864d`

## Cycle 50: U36 an Unmatched result's detail is read back with the same candidates and counts

- test: `Storage/ArtistRepositoryTests.cs::GetCountsAsync_ReadsBackTheDetailStoredWithAnUnmatchedResult` (new), with `UnmatchedAt.Detail` declared (default null)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~ArtistRepositoryTests.GetCountsAsync_ReadsBackTheDetail" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: Collections differ` Expected the two `MatchCandidate`s Actual `[]` (1 failed)
- green: `SetMatchAsync` writes `match_detail` as camelCase JSON with nulls left out; `GetCountsAsync` reads it into `UnmatchedAt.Detail`. Suite -> dotnet 411 passed
- refactor: none needed
- commit: `56298ae`

## Cycle 51: U37 a later `Matched` result for the same pair leaves no detail

- test: `Storage/ArtistRepositoryTests.cs::SetMatchAsync_AMatchAfterAFailedAttempt_LeavesNoDetail` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~ArtistRepositoryTests.SetMatchAsync_AMatchAfterAFailedAttempt" -- RunConfiguration.TreatNoTestsAsError=true`
  -> passed on first run (1 passed): U36 writes every result’s detail, null for a match. Deliberate mutant: the upsert keeps an existing detail when the new one is null (`COALESCE`) -> `Assert.Null() Failure: Value is not null` (1 failed). Code restored exactly from a copy
- green: no production change. Suite -> dotnet 412 passed
- refactor: none needed
- commit: `96a912d`

## Cycle 52: U38 a row written without detail reads back with null detail

- test: `Storage/ArtistRepositoryTests.cs::GetCountsAsync_AResultWrittenWithoutDetail_ReadsBackWithNone` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~ArtistRepositoryTests.GetCountsAsync_AResultWrittenWithoutDetail" -- RunConfiguration.TreatNoTestsAsError=true`
  -> passed on first run (1 passed). Deliberate mutant: a null column reads as an empty detail -> `Assert.Null() Failure: Value is not null` (1 failed). Code restored exactly from a copy
- green: no production change. Suite -> dotnet 413 passed
- refactor: none needed
- commit: `c8dc8e3`

## Cycle 53: U47 stored detail appears as `Candidates` (Deezer with null `Score` and `Id`), `ExactNameCount` and `AlbumsCompared`

- test: `Api/AdminControllerTests.cs::Status_StoredDetail_AppearsAsCandidatesAndCounts` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~AdminControllerTests.Status_StoredDetail" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: Collections differ` Expected the two `CandidateDto`s Actual `[]` (1 failed)
- green: `ToReason` maps `Detail` to `Candidates`, `ExactNameCount` and `AlbumsCompared`. Suite -> dotnet 414 passed
- refactor: none needed
- commit: `d799791`

## Cycle 54: U48 a row without detail gives null `Candidates`, `ExactNameCount` and `AlbumsCompared`

- test: `Api/AdminControllerTests.cs::Status_ARowWithoutDetail_HasNoCandidatesAndNoCounts` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~AdminControllerTests.Status_ARowWithoutDetail" -- RunConfiguration.TreatNoTestsAsError=true`
  -> passed on first run (1 passed). Deliberate mutant: a missing detail sends an empty candidate list -> `Assert.Equal() Failure: Values differ` Actual `([], null, null)` (1 failed). Code restored exactly from a copy
- green: no production change. Suite -> dotnet 415 passed
- refactor: none needed
- commit: `4b87bd3`

## Cycle 55: U50 after a refresh where MusicBrainz matches and Deezer finds no album in common, the status gives Deezer `Album` with both counts and MusicBrainz `Matched`

- test: `Acceptance/ConfigureAndRunTests.cs::Spec008_U50_AfterARefreshWhereDeezerFindsNoAlbumInCommon_TheStatusGivesTheAlbumFailureWithItsCounts` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~ConfigureAndRunTests.Spec008_U50" -- RunConfiguration.TreatNoTestsAsError=true`
  -> passed on first run (1 passed). Deliberate mutant: the Deezer match counts no albums -> `Assert.Equal() Failure: Values differ` Expected `("Album", 1, 2)` Actual `("Album", 1, 0)` (1 failed). Source restored exactly from a copy
- green: no production change. Suite -> dotnet 416 passed
- refactor: none needed
- notes: test-after, as U51: it composes units each driven by its own red (U26, U27, U36, U42, U47); the mutant shows it can fail
- commit: `4647380`

## Cycle 56: U68 a MusicBrainz `Search` failure gives the artist-ID sentence and no candidates

- test: `tests/web/render-status.test.js::a MusicBrainz Search failure gives the artist-ID sentence and no candidates` (new, with `withReason` and `fixes` helpers)
- red: `node --test tests/web/render-status.test.js`
  -> `not ok 15` actual `[]` expected `["Set the MusicBrainz artist ID in Jellyfin's metadata editor or artist.nfo; the plugin uses it on the next refresh."]` (1 failed)
- green: the page writes one `<p class="nr-fix">` per reasons entry through `fixFor`, which answers the artist-ID sentence for any MusicBrainz failed step. Suite -> node 369 passed, 6 failed (A1–A6 held open)
- refactor: none needed
- commit: `6e9d2da`

## Cycle 57: U69 a MusicBrainz `Unique` failure with candidates gives the sentence, then each `{name} ({score})` with its ID in a `<code>`

- test: `tests/web/render-status.test.js::a MusicBrainz Unique failure with candidates gives the sentence, then each name (score) with its ID in a <code>` (new)
- red: `node --test tests/web/render-status.test.js`
  -> `not ok 16` actual the sentence alone, expected the sentence plus ` Candidates: Nirvana (100) <code class="nr-id">5b11f4ce-…</code>, …` (1 failed)
- green: after the sentence, `fixFor` writes ` Candidates: ` and each candidate as `name (score) <code class="nr-id">id</code>`, comma-separated; not escaped yet (U81 drives that). Suite -> node 371 passed, 5 failed (A2–A6 held open; A1 now passes)
- refactor: none needed
- notes: first run of the new test expected the apostrophe as `&#39;`; the page writes that sentence as a literal, so the expectation was wrong. Corrected before any implementation, and the red above is the corrected test's
- commit: `59f08f0`

## Cycle 58: U70 the candidate ID `<code>` has `user-select: all`

- test: `tests/web/render-status.test.js::a candidate ID selects as a whole` (new, reads the page's `<style>`)
- red: `node --test tests/web/render-status.test.js`
  -> `not ok 17 - a candidate ID selects as a whole` (actual `undefined`: the page had no `<style>`) (1 failed)
- green: a `<style>` block inside `#newreleasesConfigPage` declares `#newreleasesConfigPage .nr-id { user-select: all; }`. Suite -> node 372 passed, 5 failed (A2–A6 held open)
- refactor: none needed
- commit: `64f3db0`

## Cycle 59: U71 a MusicBrainz `Score` failure with null candidates gives the sentence only

- test: `tests/web/render-status.test.js::a MusicBrainz Score failure without stored candidates gives the sentence only` (new, fixture row 2)
- red: `node --test tests/web/render-status.test.js`
  -> passed on first run (21 passed): U69 adds the candidate list only when there is one. Deliberate mutant: ` Candidates: ` written unconditionally -> `not ok 18` (and U68's `not ok 15`) (2 failed). Page restored exactly from a copy
- green: no production change. Suite -> node 373 passed, 5 failed (A2–A6 held open)
- refactor: none needed
- commit: `2a2850b`

## Cycle 60: U72 a Deezer `Name` failure with names lists them comma-separated and says Deezer matches by name only

- test: `tests/web/render-status.test.js::a Deezer Name failure with names lists them comma-separated and says Deezer matches by name only` (new)
- red: `node --test tests/web/render-status.test.js`
  -> `not ok 19` actual `undefined` (no second fix paragraph) expected `"Deezer lists Nirvana (UK), Approaching Nirvana. If one is this artist, …"` (1 failed)
- green: `fixFor` answers a Deezer `Name` failure with the names it carries. Not escaped yet (U81). Suite -> node 376 passed, 3 failed (A2, A4, A5 held open; A3 and A6 now pass)
- refactor: none needed
- commit: `d5b944d`

## Cycle 61: U73 a Deezer `Name` failure with an empty candidate list says Deezer may not list this artist

- test: `tests/web/render-status.test.js::a Deezer Name failure with an empty candidate list says Deezer may not list this artist` (new)
- red: `node --test tests/web/render-status.test.js`
  -> `not ok 20` actual `'Deezer lists . If one is this artist, …'` expected `'Deezer may not list this artist.'` (1 failed)
- green: with no names, `fixFor` says Deezer may not list the artist. Suite -> node 377 passed, 3 failed (A2, A4, A5 held open)
- refactor: none needed
- commit: `a543aab`

## Cycle 62: U74 a Deezer `Name` failure with null candidates says Deezer may not list this artist

- test: `tests/web/render-status.test.js::a Deezer Name failure with no stored candidates says Deezer may not list this artist` (new)
- red: `node --test tests/web/render-status.test.js`
  -> passed on first run (24 passed): U73 treats no candidates like none. Deliberate mutant: a missing candidate list is written as a placeholder name -> `not ok 21` (1 failed). Page restored exactly from a copy
- green: no production change. Suite -> node 378 passed, 3 failed (A2, A4, A5 held open)
- refactor: none needed
- commit: `a0da171`

## Cycle 63: U75 a Deezer `Album` failure with detail gives both counts

- test: `tests/web/render-status.test.js::a Deezer Album failure with detail gives both counts` (new)
- red: `node --test tests/web/render-status.test.js`
  -> `not ok 22` actual `[]` expected `["Checked 2 Deezer artists with this name and 37 of their albums. None has the same title …"]` (1 failed)
- green: `fixFor` answers a Deezer `Album` failure with both counts and the title rule. Suite -> node 380 passed, 2 failed (A4, A5 held open; A2 now passes)
- refactor: none needed
- commit: `d49ad9c`

## Cycle 64: U76 a Deezer `Album` failure without detail starts at "No Deezer album has the same title"

- test: `tests/web/render-status.test.js::a Deezer Album failure without detail starts at "No Deezer album has the same title"` (new)
- red: `node --test tests/web/render-status.test.js`
  -> `not ok 23` actual `'Checked undefined Deezer artists with this name and undefined of their albums. …'` expected `'No Deezer album has the same title …'` (1 failed)
- green: with no counts, `fixFor` starts the sentence at the rule. Suite -> node 381 passed, 2 failed (A4, A5 held open)
- refactor: none needed
- commit: `8f2f477`

## Cycle 65: U77 a row whose only failure is at Deezer never contains "MusicBrainz artist ID"

- test: `tests/web/render-status.test.js::a row whose only failure is at Deezer never mentions the MusicBrainz artist ID` (new, fixture row 0)
- red: `node --test tests/web/render-status.test.js`
  -> passed on first run (27 passed). Deliberate mutant: the artist-ID sentence for any failed step, whatever the source -> `not ok 24` and five Deezer fix tests (6 failed). Page restored exactly from a copy
- green: no production change. Suite -> node 382 passed, 2 failed (A4, A5 held open)
- refactor: none needed
- commit: `313ef06`

## Cycle 66: U78 a row failing at both sources gives two different paragraphs

- test: `tests/web/render-status.test.js::a row failing at both sources gives two different fix paragraphs` (new, fixture row 1)
- red: `node --test tests/web/render-status.test.js`
  -> passed on first run (28 passed): each source answers with its own sentence. Deliberate mutant: every fix written twice -> `not ok 25` and four single-fix tests (5 failed). Page restored exactly from a copy
- green: no production change. Suite -> node 383 passed, 2 failed (A4, A5 held open)
- refactor: none needed
- commit: `721bc1e`

## Cycle 67: U79 a row with a `Matched` entry ends with `Tracked through {displayName}.`

- test: `tests/web/render-status.test.js::a row with a matched source ends with "Tracked through" that source` (new)
- red: `node --test tests/web/render-status.test.js`
  -> `not ok 26` actual `undefined` expected `"Tracked through MusicBrainz."` (1 failed)
- green: How to fix ends with `<p class="nr-tracking">Tracked through {matched display names}.</p>`. Suite -> node 385 passed, 1 failed (A5 held open; A4 now passes)
- refactor: none needed
- notes: the test's second assertion expected `</td></tr>` at the end of a row string that `unmatchedRows` already splits at `</tr>`; corrected to `</td>` before the implementation. The red above came from the first assertion, which the correction did not touch
- commit: `e78a7b6`

## Cycle 68: U80 a row with no `Matched` entry ends with `Not tracked.`

- test: `tests/web/render-status.test.js::a row with no matched source ends with "Not tracked."` (new)
- red: `node --test tests/web/render-status.test.js`
  -> `not ok 27` actual `'Tracked through .'` expected `'Not tracked.'` (1 failed)
- green: with no matched source the line reads `Not tracked.`. Suite -> node 387 passed (A5 now passes; A1–A6 all green)
- refactor: none needed
- commit: `ffe1908`

## Cycle 69: U81 candidate names and IDs are escaped

- test: `tests/web/render-status.test.js::candidate names and IDs are written escaped` (new)
- red: `node --test tests/web/render-status.test.js`
  -> `not ok 28 - candidate names and IDs are written escaped`: the row holds `Candidates: <b>Blur</b> (90) <code class="nr-id">id<1></code>` and `Deezer lists Tom & <i>Jerry</i>.` (1 failed)
- green: candidate names, scores and IDs, and Deezer names, go through `esc`. Suite -> node 388 passed
- refactor: none needed
- notes: the first draft of the test built its status in an unreadable inline function; rewritten to two plain statements before the implementation, re-run red with the output above
- commit: `e12d54e`

## Cycle 70: U82 a `Matched` or `NotChecked` entry gives no fix paragraph

- test: `tests/web/render-status.test.js::a matched or not-checked source gives no fix paragraph` (new, fixture rows 0 and 2)
- red: `node --test tests/web/render-status.test.js`
  -> passed on first run (32 passed): `fixFor` answers only failed steps. Deliberate mutant: a source with no failed step gets a paragraph naming its state -> `not ok 29` and four single-fix tests (5 failed). Page restored exactly from a copy
- green: no production change. Suite -> node 389 passed
- refactor: none needed
- commit: `faa249b`

## Cycle 71: U83 an entry with no failed step gives no fix paragraph

- test: `tests/web/render-status.test.js::an entry with no failed step gives no fix paragraph` (new)
- red: `node --test tests/web/render-status.test.js`
  -> passed on first run (33 passed). Deliberate mutant: the MusicBrainz fix keyed on the Unmatched state instead of a failed step -> `not ok 30` (1 failed). Page restored exactly from a copy
- green: no production change. Suite -> node 390 passed
- refactor: none needed
- commit: `e7e7455`

## Cycle 72: A1–A6 close — a fix that applies to each failure (US1)

- tests: `tests/web/unmatched-acceptance.test.js` (new file), one test per US1 acceptance scenario
  through the page's rendering of the committed admin status; held open (uncommitted) since cycle 38
  with the red recorded there (6 failed, How to fix empty)
- green: they closed one by one as the units landed: A1 with U69, A3 and A6 with U72, A2 with U75,
  A4 with U79, A5 with U80. Each unit cycle's suite reports which held-open test it closed. Suite
  -> node 390 passed, dotnet 416 passed
- refactor: none needed
- commit: `144393a`

## Cycle 73: U58 a trail shows every step as short name, joined by ` · `, with `Id` written `ID`

- test: `tests/web/render-status.test.js::a trail shows every step in order by short name, ID for Id, joined by " · "` (new)
- red: `node --test tests/web/render-status.test.js`
  -> `not ok 31` actual `[]` expected `["ID", "Search", "Score", "Unique"]` (1 failed)
- green: a line with a failed step writes each step as `<span class="nr-step nr-step-{state}">{short name}</span>`, joined by ` · `; marks come next. Suite -> node 391 passed, 3 failed (A7, A8, A12 held open)
- refactor: none needed
- notes: A7, A8 and A12 (`tests/web/unmatched-acceptance.test.js`, uncommitted additions) opened US2’s outer loop before this cycle: red `node --test tests/web/unmatched-acceptance.test.js` -> A7 actual `MusicBrainz: low score (70)`, A8 actual `Deezer: no corroborating album`, A12 actual `[]` (3 failed)
- commit: `8105609`

## Cycle 74: U59 a passed step shows `✓` and the words `passed`

- test: `tests/web/render-status.test.js::a passed step shows ✓ and the words "passed"` (new)
- red: `node --test tests/web/render-status.test.js`
  -> `not ok 32` actual `undefined` expected `["✓", "passed"]` (1 failed)
- green: a `STEP_MARKS` table gives `Passed` its mark and words; the step writes ` <span aria-hidden="true">✓</span><span class="nr-sr">passed</span>`. Suite -> node 392 passed, 3 failed (A7, A8, A12 held open)
- refactor: none needed
- commit: `d2bc3d9`

## Cycle 75: U60 a failed step shows `✗` and the words `failed`

- test: `tests/web/render-status.test.js::a failed step shows ✗ and the words "failed"` (new)
- red: `node --test tests/web/render-status.test.js`
  -> `not ok 33` actual `undefined` expected `["✗", "failed"]` (1 failed)
- green: `STEP_MARKS` gains `Failed`. Suite -> node 394 passed, 2 failed (A7, A12 held open; A8 now passes)
- refactor: none needed
- commit: `a57dc2c`

## Cycle 76: U61 a not-reached step shows `–`, the words `not reached`, and the dimming class

- test: `tests/web/render-status.test.js::a not-reached step shows –, the words "not reached", and the dimming class` (new)
- red: `node --test tests/web/render-status.test.js`
  -> `not ok 34` actual `undefined` expected `["–", "not reached"]` (1 failed)
- green: `STEP_MARKS` gains `NotReached`; the step class `nr-step-notreached` (from U58) is the one the stylesheet dims (U63). Suite -> node 396 passed, 1 failed (A7 held open; A12 now passes)
- refactor: none needed
- commit: `6b19b87`

## Cycle 77: U62 each mark is `aria-hidden`, and the words sit in a visually hidden `nr-sr` element

- test: `tests/web/render-status.test.js::every step hides its mark from screen readers and states itself in visually hidden words` (new)
- red: `node --test tests/web/render-status.test.js`
  -> passed on first run (38 passed): U59–U61 already write that pair. Deliberate mutant: the mark span loses `aria-hidden` -> `not ok 35` and U59–U61 (4 failed). Page restored exactly from a copy
- green: no production change. Suite -> node 397 passed, 1 failed (A7 held open)
- refactor: none needed
- commit: `5970285`

## Cycle 78: U63 the stylesheet dims a not-reached step and makes `.nr-sr` visually hidden

- test: `tests/web/render-status.test.js::the stylesheet dims a not-reached step and hides the screen-reader words visually` (new)
- red: `node --test tests/web/render-status.test.js`
  -> `not ok 36` — `assert.ok` falsy: no `.nr-step-notreached` rule, so its opacity reads as `NaN` (1 failed)
- green: `.nr-step-notreached { opacity: 0.5; }` and the usual visually-hidden rule for `.nr-sr` (absolute, 1px × 1px, overflow hidden, clipped, no wrap). Suite -> node 398 passed, 1 failed (A7 held open)
- refactor: none needed
- commit: `a9ec639`

## Cycle 79: U64 a failed `Score` step shows its value after the mark: `Score ✗ 70`

- test: `tests/web/render-status.test.js::a failed Score step shows its score after the mark` (new, with a `visible` helper)
- red: `node --test tests/web/render-status.test.js`
  -> `not ok 37` actual `'MusicBrainz: ID ✗ · Search ✓ · Score ✗ · Unique –'` (1 failed)
- green: a step with values writes the first one after its mark. Suite -> node 400 passed (A7 now passes; A7, A8, A12 all green)
- refactor: none needed
- commit: `00f1ce5`

## Cycle 80: U65 a failed `Unique` step shows both values: `Unique ✗ 100 vs 96`

- test: `tests/web/render-status.test.js::a failed Unique step shows both scores: "Unique ✗ 100 vs 96"` (new)
- red: `node --test tests/web/render-status.test.js`
  -> `not ok 38` actual `'MusicBrainz: ID ✗ · Search ✓ · Score ✓ · Unique ✗ 100'` (1 failed)
- green: the step writes every value, joined by ` vs `. Suite -> node 401 passed
- refactor: none needed
- commit: `c254754`

## Cycle 81: U66 the trail line declares `white-space: nowrap`

- test: `tests/web/render-status.test.js::a Reason line does not wrap` (new)
- red: `node --test tests/web/render-status.test.js`
  -> `not ok 39` actual `undefined` expected `"nowrap"` (1 failed)
- green: `#newreleasesConfigPage .nr-reason { white-space: nowrap; }`. Suite -> node 402 passed
- refactor: none needed
- commit: `7f0f6e2`

## Cycle 82: U67 the longest possible trail text is at most 64 characters

- test: `tests/web/render-status.test.js::the longest possible trail is at most 64 characters as a reader sees it` (new: MusicBrainz failed at Unique with 100 vs 95)
- red: `node --test tests/web/render-status.test.js`
  -> passed on first run (43 passed): the line is 59 characters. Deliberate mutant: the trail also appends the stored reason text -> `not ok 40` and U65's `not ok 38` (2 failed). Page restored exactly from a copy
- green: no production change. Suite -> node 403 passed
- refactor: none needed
- notes: the test pins the exact text as well as the bound, so any mutant that lengthens the line fails the equality first; the bound states the budget research R7 sets. Pixel width at 1280 px stays with the maintainer’s real-server pass
- commit: `2324e7a`

## Cycle 83: A7, A8 and A12 close — which steps passed and which failed (US2)

- tests: `tests/web/unmatched-acceptance.test.js` A7, A8, A12 (new), held open (uncommitted) since
  cycle 73 with the reds recorded there
- green: A8 closed with U60, A12 with U61, A7 with U64; each unit cycle's suite names it. A9, A10
  and A11 closed earlier (cycles 34 and 25). Suite -> node 403 passed, dotnet 416 passed
- refactor: none needed
- commit: `91346a1`

## Cycle 84: remediation of audit finding 1 — U47 also pins a MusicBrainz candidate's score and ID (T046)

- test: `Api/AdminControllerTests.cs::Status_StoredMusicBrainzCandidates_KeepTheirScoreAndId` (new).
  The existing U47 test kept its Deezer case, where score and ID are null by rule
- red: none possible, the behaviour exists; passed on first run (1 passed). The audit's surviving
  mutant, `new CandidateDto(c.Name, null, null)` in `AdminController.ToReason`, now fails it:
  `dotnet test --configuration Release --filter "FullyQualifiedName~AdminControllerTests.Status_StoredMusicBrainzCandidates" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: Collections differ` Actual `[CandidateDto { Name = Blur, Score = , Id =  }, …]` (1 failed). Code restored exactly from a copy
- green: no production change. Suite -> dotnet 417 passed
- refactor: none needed
- commit: `e116064`

## Cycle 85: remediation of audit finding 2 — the two source tests pin the whole record again (T047)

- tests: `Sources/MusicBrainzSourceTests.cs::MatchArtistAsync_NoResults_IsUnmatchedAsNoResult` and
  `Sources/DeezerSourceTests.cs::MatchArtistAsync_ExactNameWithoutACorroboratingAlbum_IsUnmatchedWithReason`
  (existing, loosened in cycles 41 and 46). Each now compares the whole record with
  `ArtistMatch.Unmatched(reason) with { Detail = match.Detail }`: every field but the detail,
  which U19 and U26 pin on their own. This restores the `SourceArtistId == null` check
- red: none possible, the behaviour exists; both passed (2 passed). The audit's surviving mutant,
  `… with { SourceArtistId = "stale" }` on each source's Unmatched result, now fails each test:
  `Assert.Equal() Failure: Values differ` Actual `ArtistMatch { …, SourceArtistId = stale, … }` (1 failed each). Sources restored exactly from a copy
- green: no production change. Suite -> dotnet 417 passed
- refactor: none needed
- commit: `cf5526e`

## Cycle 86: remediation of audit finding 3 — the Artist cell is pinned again (T048)

- test: `tests/web/render-status.test.js::each unmatched row's Artist cell links the artist's page with the name escaped`
  (new): an artist named `Tom & <Jerry>` renders as `<a … href="#/details?id=…&serverId=…">Tom &amp; &lt;Jerry&gt;</a>`
- first run: `not ok 1 - tests/web/render-status.test.js`, a file-level failure: the test name's
  apostrophes closed its single-quoted string. Not a red; the name was re-quoted
- red: none possible, the behaviour exists; passed (44 passed). The audit's surviving mutant (the
  name written unescaped) now fails it: `not ok 41` (1 failed). A second mutant (the name dropped)
  also fails it: `not ok 41` (1 failed). Page restored exactly from a copy each time
- green: no production change. Suite -> node 404 passed
- refactor: none needed
- commit: `5ab2c94`
