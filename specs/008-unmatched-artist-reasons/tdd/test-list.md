---
feature: 008-unmatched-artist-reasons
loop: outside-in
profile: .specify/memory/tdd-profile.md
spec_criteria: 12
planned_at: 09033f8
updated_at: 09033f8
suite_baseline: green
---

# Test List: Explain each unmatched artist step by step

Traces use `US1-AS1` … `US2-AS6` for the acceptance scenarios, `FR-001` … `FR-016` for the
functional requirements, `SC-001` … `SC-005` for the success criteria, and `EC-old`,
`EC-unknown`, `EC-notchecked` for the spec's edge cases (a reason recorded before the upgrade, a
reason text the page does not recognize, a source that has not run for an artist yet). `INV-esc`
is the recorded invariant that every value the page writes passes through `esc` (existing
`admin.html` rule, kept by [contracts/admin-page.md](../contracts/admin-page.md) "Unchanged").
`INV-migrate` is constitution IV: a schema change migrates forward from every released version.

## Outer loop: acceptance behaviors

The fix wording and the trail live in the page (research R5), so their acceptance behaviors run
through the page's real entry point, its status rendering, against the committed
`tests/fixtures/pages/admin-status*.json`. `ResponseNamingTests` (U49) holds those fixtures to the
server's records, and U50–U51 run the server end to end through the refresh task and
`AdminController`. The listing behaviors (A10, A11) are observable in the response alone and run
through `AcceptanceRig`.

| id  | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| A1 | A MusicBrainz `ambiguous` row's fix tells the administrator to set the MusicBrainz artist ID and lists its 3 candidates with name, score and ID | US1-AS1 | example | DONE | `tests/web/unmatched-acceptance.test.js::A1 (US1-AS1)` |
| A2 | A Deezer `no corroborating album` row's fix gives both counts and does not mention the MusicBrainz artist ID | US1-AS2 | example | DONE | `tests/web/unmatched-acceptance.test.js::A2 (US1-AS2)` |
| A3 | A Deezer `no result` row with near names lists them and says Deezer matches by name only | US1-AS3 | example | DONE | `tests/web/unmatched-acceptance.test.js::A3 (US1-AS3)` |
| A4 | A row Unmatched at one source and Matched at the other ends with "Tracked through" the matched source | US1-AS4 | example | DONE | `tests/web/unmatched-acceptance.test.js::A4 (US1-AS4)` |
| A5 | A row Unmatched at every enabled source ends with "Not tracked." | US1-AS5 | example | DONE | `tests/web/unmatched-acceptance.test.js::A5 (US1-AS5)` |
| A6 | A row Unmatched at both sources has one fix per failed source and no fix twice | US1-AS6 | example | DONE | `tests/web/unmatched-acceptance.test.js::A6 (US1-AS6)` |
| A7  | A MusicBrainz `low score (70)` row's Reason line reads ID failed, Search passed, Score failed with 70, Unique not reached | US2-AS1 | example | PENDING | |
| A8  | A Deezer `no corroborating album` row's Reason line reads Name passed, Album failed | US2-AS2 | example | PENDING | |
| A9 | A Matched source's Reason line reads "<Source>: Matched" with no trail | US2-AS3 | example | DONE | `tests/web/render-status.test.js::a matched source reads "<Source>: Matched" and nothing else` |
| A10 | With Deezer disabled, no listed artist has a Deezer entry in the admin status | US2-AS4 | example | DONE | `Acceptance/ConfigureAndRunTests.cs::Spec008_A10_WithDeezerDisabled_NoListedArtistHasADeezerReason` |
| A11 | An artist Unmatched only at disabled Deezer is absent from the admin status, and is listed with its stored reason once Deezer is enabled again, with no refresh between | US2-AS5 | example | DONE | `Acceptance/ConfigureAndRunTests.cs::Spec008_A11_AnArtistUnmatchedOnlyAtDisabledDeezer_IsHidden_AndReturnsWhenDeezerIsEnabledAgain` |
| A12 | Every rendered step carries its state in words for a screen reader, separate from its mark and dimming | US2-AS6 | example | PENDING | |

## Inner loop: unit behaviors

### `src/Jellyfin.Plugin.NewReleases/Matching/MatchReasons.cs` (new)

| id  | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U1 | `LowScore(70)` is `low score (70)` | FR-011 | example | DONE | `Matching/MatchReasonsTests.cs::LowScore_WritesTheBestScoreInParentheses` |
| U2 | `Ambiguous(92, 90)` is `ambiguous (score 92 vs 90)` | FR-011 | example | DONE | `Matching/MatchReasonsTests.cs::Ambiguous_WritesTheBestScoreThenTheRunnerUp` |
| U3 | MusicBrainz `no result` parses to failed step `Search` with no values | FR-003, FR-013 | example | DONE | `Matching/MatchReasonsTests.cs::Parse_MusicBrainzNoResult_FailsAtSearchWithNoValues` |
| U4 | MusicBrainz `low score (70)` parses to `Score` with values `[70]` | FR-003, FR-004 | example | DONE | `Matching/MatchReasonsTests.cs::Parse_MusicBrainzLowScore_FailsAtScoreWithTheScore` |
| U5 | MusicBrainz `ambiguous (score 92 vs 90)` parses to `Unique` with values `[92, 90]` | FR-003, FR-004 | example | DONE | `Matching/MatchReasonsTests.cs::Parse_MusicBrainzAmbiguous_FailsAtUniqueWithBothScores` |
| U6 | Deezer `no result` parses to `Name` | FR-003 | example | DONE | `Matching/MatchReasonsTests.cs::Parse_DeezerNoResult_FailsAtNameWithNoValues` |
| U7 | Deezer `no corroborating album` parses to `Album` | FR-003 | example | DONE | `Matching/MatchReasonsTests.cs::Parse_DeezerNoCorroboratingAlbum_FailsAtAlbumWithNoValues` |
| U8 | An unknown text parses to null | EC-unknown | example | DONE | `Matching/MatchReasonsTests.cs::Parse_ATextNoReleasedVersionWrites_IsNull` |
| U9 | An empty text parses to null | EC-unknown | example | DONE | `Matching/MatchReasonsTests.cs::Parse_AnEmptyText_IsNull` |
| U10 | A MusicBrainz-only text under the Deezer source parses to null | EC-unknown | example | DONE | `Matching/MatchReasonsTests.cs::Parse_AMusicBrainzOnlyTextUnderDeezer_IsNull` |
| U11 | Steps for MusicBrainz failed at `Search`: Id Failed, Search Failed, Score NotReached, Unique NotReached | FR-003 | example | DONE | `Matching/MatchReasonsTests.cs::Steps_MusicBrainzFailedAtSearch_IdAndSearchFailed_TheRestNotReached` |
| U12 | Steps for MusicBrainz failed at `Score`: Id Failed, Search Passed, Score Failed with its values, Unique NotReached | FR-003, FR-004 | example | DONE | `Matching/MatchReasonsTests.cs::Steps_MusicBrainzFailedAtScore_CarriesTheScoreOnTheScoreStepOnly` |
| U13 | Steps for MusicBrainz failed at `Unique` (last step): Id Failed, Search Passed, Score Passed, Unique Failed with its values | FR-003, FR-004 | example | DONE | `Matching/MatchReasonsTests.cs::Steps_MusicBrainzFailedAtUnique_EveryEarlierStepButIdPassed` |
| U14 | Steps for Deezer failed at `Name` (first step): Name Failed, Album NotReached | FR-003 | example | DONE | `Matching/MatchReasonsTests.cs::Steps_DeezerFailedAtName_NameFailed_AlbumNotReached` |
| U15 | Steps for Deezer failed at `Album` (last step): Name Passed, Album Failed | FR-003 | example | DONE | `Matching/MatchReasonsTests.cs::Steps_DeezerFailedAtAlbum_NamePassed_AlbumFailed` |

### `src/Jellyfin.Plugin.NewReleases/Sources/MusicBrainzSource.cs`

| id  | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U16 | Every Unmatched reason the source writes (empty, low-score and ambiguous fixtures) parses to its expected failed step | FR-011, FR-013 | example | DONE | `Sources/MusicBrainzSourceTests.cs::MatchArtistAsync_EveryUnmatchedReason_ParsesToItsFailedStep` |
| U17 | `artist_search_ambiguous.json` gives 3 candidates in score order, each with name, score and MusicBrainz ID | FR-015 | example | DONE | `Sources/MusicBrainzSourceTests.cs::MatchArtistAsync_Ambiguous_KeepsTheCandidatesInScoreOrderWithScoreAndId` |
| U18 | A search returning 4 or more results keeps only the first 3 candidates | FR-016 | example | DONE | `Sources/MusicBrainzSourceTests.cs::MatchArtistAsync_FiveResultsScoringBelow85_KeepsOnlyTheFirstThree` |
| U19 | `artist_search_empty.json` gives an empty candidate list | FR-015 | example | DONE | `Sources/MusicBrainzSourceTests.cs::MatchArtistAsync_NoResults_RecordsAnEmptyCandidateList` |
| U20 | A match by the stored MusicBrainz ID carries no detail | FR-015 | example | DONE | `Sources/MusicBrainzSourceTests.cs::MatchArtistAsync_SnapshotWithMbid_IsMatchedWithoutAnyRequest` |
| U21 | A confident name match carries no detail | FR-015 | example | DONE | `Sources/MusicBrainzSourceTests.cs::MatchArtistAsync_ConfidentTopResult_IsMatched` |
| U22 | An Unmatched attempt sends the same requests as before the feature | FR-015, FR-012 | example | DONE | `Sources/MusicBrainzSourceTests.cs::MatchArtistAsync_Ambiguous_SendsOnlyTheSearch` |

### `src/Jellyfin.Plugin.NewReleases/Sources/DeezerSource.cs`

| id  | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U23 | Every Unmatched reason the source writes (empty, near-name and homonym fixtures) parses to its expected failed step | FR-011, FR-013 | example | DONE | `Sources/DeezerSourceTests.cs::MatchArtistAsync_EveryUnmatchedReason_ParsesToItsFailedStep` |
| U24 | `search_artist_exact.json` for library artist `Daft Punk Live` gives `no result` and the first 3 of the 5 names in response order, with no score or ID | FR-015, FR-016 | example | DONE | `Sources/DeezerSourceTests.cs::MatchArtistAsync_NoExactName_KeepsTheFirstThreeNamesInResponseOrder` |
| U25 | `search_artist_empty.json` gives `no result` and an empty candidate list | FR-015 | example | DONE | `Sources/DeezerSourceTests.cs::MatchArtistAsync_NoSearchResults_RecordsAnEmptyCandidateList` |
| U26 | `search_artist_homonyms.json` with no album in common gives `ExactNameCount` 2 | FR-015 | example | DONE | `Sources/DeezerSourceTests.cs::MatchArtistAsync_NoCorroboratingAlbum_RecordsHowManyExactNameArtistsWereChecked` |
| U27 | The same case gives `AlbumsCompared` equal to the first-page items served for both candidates | FR-015 | example | DONE | `Sources/DeezerSourceTests.cs::MatchArtistAsync_NoCorroboratingAlbum_RecordsTheAlbumsComparedAcrossBothArtists` |
| U28 | A corroborated match carries no detail | FR-015 | example | DONE | `Sources/DeezerSourceTests.cs::MatchArtistAsync_ExactNameWhoseFirstAlbumsPageContainsALibraryAlbum_IsMatched` |
| U29 | An Unmatched attempt sends the same requests as before the feature | FR-015, FR-012 | example | DONE | `Sources/DeezerSourceTests.cs::MatchArtistAsync_NoCorroboratingAlbum_SendsOnlyTheSearchAndEachFirstAlbumPage` |

### `src/Jellyfin.Plugin.NewReleases/Storage/ArtistRepository.cs`

| id  | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U30 | An artist Unmatched only at a source outside the enabled set is not listed | FR-014 | example | DONE | `Storage/ArtistRepositoryTests.cs::GetCountsAsync_AnArtistUnmatchedOnlyAtADisabledSource_IsNotListed` |
| U31 | A listed artist has one entry per enabled source, in the order given | FR-002, FR-001 | example | DONE | `Storage/ArtistRepositoryTests.cs::GetCountsAsync_AListedArtistHasOneEntryPerEnabledSource_InTheOrderGiven` |
| U32 | An enabled source with no row is `NotChecked` | FR-002, EC-notchecked | example | DONE | `Storage/ArtistRepositoryTests.cs::GetCountsAsync_AnEnabledSourceWithNoRow_IsListedAsPending` |
| U33 | An enabled source with a `Pending` row is `NotChecked` | FR-002, EC-notchecked | example | DONE | `Storage/ArtistRepositoryTests.cs::GetCountsAsync_AnEnabledSourceWithAPendingRow_IsListedAsPending` |
| U34 | `Reason` is null for `Matched` and `NotChecked` entries | FR-002 | example | DONE | `Storage/ArtistRepositoryTests.cs::GetCountsAsync_MatchedAndNotCheckedEntries_HaveNoReason` |
| U35 | A disabled source's row stays stored, and enabling it again lists the artist with that reason | FR-014 | example | DONE | `Storage/ArtistRepositoryTests.cs::GetCountsAsync_ADisabledSourcesResultIsKept_AndListsAgainOnceEnabled` |
| U36 | An Unmatched result's detail is read back with the same candidates and counts | FR-015 | example | DONE | `Storage/ArtistRepositoryTests.cs::GetCountsAsync_ReadsBackTheDetailStoredWithAnUnmatchedResult` |
| U37 | A later `Matched` result for the same pair leaves no detail | FR-015 | example | DONE | `Storage/ArtistRepositoryTests.cs::SetMatchAsync_AMatchAfterAFailedAttempt_LeavesNoDetail` |
| U38 | A row written without detail reads back with null detail | EC-old | example | DONE | `Storage/ArtistRepositoryTests.cs::GetCountsAsync_AResultWrittenWithoutDetail_ReadsBackWithNone` |

### `src/Jellyfin.Plugin.NewReleases/Storage/Migrations/002_match_detail.sql`

| id  | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U39 | A `001` database holding an `artist_source` row migrates to `002`: the row survives, its `match_detail` is null, and `schema_version` records 2 | INV-migrate, EC-old | example | DONE | `Storage/DatabaseTests.cs::OpenAsync_ADatabaseAtSchema001_MigratesTo002_KeepingItsArtistSourceRows` |

### `src/Jellyfin.Plugin.NewReleases/Api/AdminController.cs` and `Api/Dtos.cs`

| id  | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U40 | Each `unmatched[]` entry carries `Reasons` and no `Sources` or `Hint` | FR-013 | example | DONE | `Api/AdminControllerTests.cs::Status_EachUnmatchedArtist_CarriesReasons_AndNoSourcesOrHint` |
| U41 | `Reasons` has one entry per enabled source, in the `Sources` order | FR-002, FR-001 | example | DONE | `Api/AdminControllerTests.cs::Status_Reasons_FollowTheSourcesOrder_OnePerEnabledSource` |
| U42 | An `ambiguous (score 92 vs 90)` MusicBrainz row gives `FailedStep` `Unique`, the four step states, and `Values` `[92, 90]` on `Unique` only | FR-003, FR-004, FR-013 | example | DONE | `Api/AdminControllerTests.cs::Status_AnAmbiguousMusicBrainzReason_GivesTheUniqueTrailWithBothScores` |
| U43 | A `Matched` entry has null `FailedStep`, empty `Steps` and null `Reason` | US2-AS3 | example | DONE | `Api/AdminControllerTests.cs::Status_AMatchedSource_IsMatched_WithNoFailedStepNoStepsAndNoReason` |
| U44 | A `NotChecked` entry has null `FailedStep`, empty `Steps` and null `Reason` | EC-notchecked | example | DONE | `Api/AdminControllerTests.cs::Status_ASourceNotYetRun_IsNotChecked_WithNoFailedStepNoStepsAndNoReason` |
| U45 | An unrecognized reason gives `Unmatched`, null `FailedStep`, empty `Steps`, and the stored text in `Reason` | EC-unknown | example | DONE | `Api/AdminControllerTests.cs::Status_AnUnrecognizedReason_IsUnmatched_WithNoFailedStepNoSteps_AndKeepsItsText` |
| U46 | With Deezer disabled in the configuration, an artist Unmatched only at Deezer is absent | FR-014 | example | DONE | `Api/AdminControllerTests.cs::Status_WithDeezerDisabled_AnArtistOnlyDeezerFailed_IsNotListed` |
| U47 | Stored detail appears as `Candidates` (Deezer with null `Score` and `Id`), `ExactNameCount` and `AlbumsCompared` | FR-015, FR-013 | example | DONE | `Api/AdminControllerTests.cs::Status_StoredDetail_AppearsAsCandidatesAndCounts` |
| U48 | A row without detail gives null `Candidates`, `ExactNameCount` and `AlbumsCompared` | EC-old, FR-016 | example | DONE | `Api/AdminControllerTests.cs::Status_ARowWithoutDetail_HasNoCandidatesAndNoCounts` |

### `tests/fixtures/pages/admin-status.json` against the records (`ResponseNamingTests`)

| id  | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U49 | The admin status record serializes with exactly the field names of `admin-status.json`, at every level | FR-013 | contract | DONE | `Api/ResponseNamingTests.cs::AdminStatusResponse_AsTheAdminStatusEndpointDeclaresIt_CarriesTheNamesTheAdministratorPageReads` |

### End to end through the refresh task and `AdminController` (`Acceptance/ConfigureAndRunTests.cs`)

| id  | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U50 | After a refresh where MusicBrainz matches and Deezer finds exact-name artists with no album in common, the status gives Deezer `Album` with both counts and MusicBrainz `Matched` | US1-AS2, US1-AS4, FR-015 | example | DONE | `Acceptance/ConfigureAndRunTests.cs::Spec008_U50_AfterARefreshWhereDeezerFindsNoAlbumInCommon_TheStatusGivesTheAlbumFailureWithItsCounts` |
| U51 | After a refresh where the MusicBrainz search scores below 85, the status gives steps Id Failed, Search Passed, Score Failed with the score, Unique NotReached | US2-AS1, FR-004 | example | DONE | `Acceptance/ConfigureAndRunTests.cs::Spec008_U51_AfterARefreshWhoseMusicBrainzSearchScoresBelow85_TheStatusGivesTheScoreTrail` |

### `src/Jellyfin.Plugin.NewReleases/Web/admin.html`: Reason column

| id  | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U52 | The table header reads Artist, Reason, How to fix | FR-001 | example | DONE | `tests/web/render-status.test.js::the unmatched table is headed Artist, Reason, How to fix` |
| U53 | The Reason cell has one line per `reasons` entry, led by the source's `displayName` | FR-001, FR-002 | example | DONE | `tests/web/render-status.test.js::each unmatched artist has one Reason line per reasons entry, led by the source display name` |
| U54 | A `Matched` entry's line reads `Matched` | US2-AS3 | example | DONE | `tests/web/render-status.test.js::a matched source reads "<Source>: Matched" and nothing else` |
| U55 | A `NotChecked` entry's line reads `Not checked` | EC-notchecked | example | DONE | `tests/web/render-status.test.js::a source not yet checked reads "<Source>: Not checked"` |
| U56 | An entry with no failed step shows its stored reason text, escaped | EC-unknown, INV-esc | example | DONE | `tests/web/render-status.test.js::an entry with no failed step shows its stored reason text, escaped` |
| U57 | The old fixed hint sentence appears nowhere on the page | SC-001 | example | DONE | `tests/web/render-status.test.js::the old fixed hint sentence appears nowhere on the page` |
| U58 | A trail shows every step as short name and mark, joined by ` · `, with `Id` written `ID` | FR-003 | example | PENDING | |
| U59 | A passed step shows `✓` and the words `passed` | FR-003, FR-010 | example | PENDING | |
| U60 | A failed step shows `✗` and the words `failed` | FR-003, FR-010 | example | PENDING | |
| U61 | A not-reached step shows `–`, the words `not reached`, and the dimming class | FR-003, FR-010 | example | PENDING | |
| U62 | Each mark is `aria-hidden`, and the words sit in a visually hidden `nr-sr` element | FR-010 | example | PENDING | |
| U63 | The stylesheet gives the dimming class an `opacity` below 1 and makes `.nr-sr` visually hidden | FR-010 | example | PENDING | |
| U64 | A failed `Score` step shows its value after the mark: `Score ✗ 70` | FR-004 | example | PENDING | |
| U65 | A failed `Unique` step shows both values: `Unique ✗ 92 vs 90` | FR-004 | example | PENDING | |
| U66 | The trail line declares `white-space: nowrap` | FR-005, SC-004 | example | PENDING | |
| U67 | The longest possible trail text is at most 64 characters | FR-005, SC-004 | example | PENDING | |

### `src/Jellyfin.Plugin.NewReleases/Web/admin.html`: How to fix column

| id  | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U68 | A MusicBrainz `Search` failure gives the artist-ID sentence and no candidates | FR-008 | example | DONE | `tests/web/render-status.test.js::a MusicBrainz Search failure gives the artist-ID sentence and no candidates` |
| U69 | A MusicBrainz `Unique` failure with candidates gives the sentence, then each `{name} ({score})` with its ID in a `<code>` | FR-008, FR-016 | example | DONE | `tests/web/render-status.test.js::a MusicBrainz Unique failure with candidates gives the sentence, then each name (score) with its ID in a <code>` |
| U70 | The candidate ID `<code>` has `user-select: all` | FR-016 | example | DONE | `tests/web/render-status.test.js::a candidate ID selects as a whole` |
| U71 | A MusicBrainz `Score` failure with null candidates gives the sentence only | FR-016, EC-old | example | DONE | `tests/web/render-status.test.js::a MusicBrainz Score failure without stored candidates gives the sentence only` |
| U72 | A Deezer `Name` failure with names lists them comma-separated and says Deezer matches by name only | FR-008 | example | DONE | `tests/web/render-status.test.js::a Deezer Name failure with names lists them comma-separated and says Deezer matches by name only` |
| U73 | A Deezer `Name` failure with an empty candidate list says Deezer may not list this artist | FR-008 | example | DONE | `tests/web/render-status.test.js::a Deezer Name failure with an empty candidate list says Deezer may not list this artist` |
| U74 | A Deezer `Name` failure with null candidates says Deezer may not list this artist | FR-008, EC-old | example | DONE | `tests/web/render-status.test.js::a Deezer Name failure with no stored candidates says Deezer may not list this artist` |
| U75 | A Deezer `Album` failure with detail gives both counts | FR-008 | example | DONE | `tests/web/render-status.test.js::a Deezer Album failure with detail gives both counts` |
| U76 | A Deezer `Album` failure without detail starts at "No Deezer album has the same title" | FR-008, EC-old | example | DONE | `tests/web/render-status.test.js::a Deezer Album failure without detail starts at "No Deezer album has the same title"` |
| U77 | A row whose only failure is at Deezer never contains "MusicBrainz artist ID" | SC-001, FR-006 | example | DONE | `tests/web/render-status.test.js::a row whose only failure is at Deezer never mentions the MusicBrainz artist ID` |
| U78 | A row failing at both sources gives two different paragraphs | FR-007 | example | DONE | `tests/web/render-status.test.js::a row failing at both sources gives two different fix paragraphs` |
| U79 | A row with a `Matched` entry ends with `Tracked through {displayName}.` | FR-009 | example | DONE | `tests/web/render-status.test.js::a row with a matched source ends with "Tracked through" that source` |
| U80 | A row with no `Matched` entry ends with `Not tracked.` | FR-009 | example | DONE | `tests/web/render-status.test.js::a row with no matched source ends with "Not tracked."` |
| U81 | Candidate names and IDs are escaped | INV-esc | example | DONE | `tests/web/render-status.test.js::candidate names and IDs are written escaped` |
| U82 | A `Matched` or `NotChecked` entry gives no fix paragraph | FR-006 | example | DONE | `tests/web/render-status.test.js::a matched or not-checked source gives no fix paragraph` |
| U83 | An entry with no failed step gives no fix paragraph | EC-unknown, FR-006 | example | DONE | `tests/web/render-status.test.js::an entry with no failed step gives no fix paragraph` |

## Invariants and edge cases still to place

None. EC-old is placed on U38, U39, U48, U71, U74 and U76; EC-unknown on U8–U10, U45, U56 and
U83; EC-notchecked on U32, U33, U44 and U55.

## Out of scope

- Ownership matching (whether the library holds a release): clarification 1, a separate feature.
- How a Matched source matched (by ID or by name): clarification 8, not stored.
- Links to a source's site: clarification 5, FR-016 forbids them.
- A MusicBrainz artist ID that is set but wrong, and an artist with no Jellyfin page: spec edge
  cases, out of scope.
- Pixel width at 1280 pixels (SC-004): the fake DOM measures no pixels; U66–U67 assert the
  declarations and a character bound, and the maintainer's real-server pass checks the width
  (research R7).
- SC-002 and SC-003 (reading time, naming the failed step without documentation): human
  measures, checked in the real-server pass; U58–U61 and U79–U80 pin what they read.

## Verification commands

Copied verbatim from `.specify/memory/tdd-profile.md` at planning time:

- Single test (dotnet): `dotnet test --configuration Release --filter "FullyQualifiedName~{name}" -- RunConfiguration.TreatNoTestsAsError=true`
- Full suite (dotnet): `dotnet test --configuration Release`
- Single file (node): `node --test tests/web/{file}`
- Full suite (node): `node --test "tests/web/*.test.js"`
- Coverage (node): `node --test --experimental-test-coverage "tests/web/*.test.js"`
- Coverage, mutation (dotnet): none in the profile; the loop uses a deliberate-mutant spot check
  (constitution II).
