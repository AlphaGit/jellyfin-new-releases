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
