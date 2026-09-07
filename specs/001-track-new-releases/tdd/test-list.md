---
feature: 001-track-new-releases
loop: outside-in
profile: .specify/memory/tdd-profile.md
spec_criteria: 19
planned_at: 92a272d
updated_at: 91d4802
suite_baseline: green
---

# Test List: Track New Releases

## Trace id key

`spec.md` numbers acceptance scenarios per user story without ids. This list uses:

- `US<n>-AS<m>`: acceptance scenario *m* of user story *n* (19 in total: US1 1–8, US2 1–7,
  US3 1–4).
- `FR-nnn`, `SC-nnn`: functional requirements and success criteria as written in `spec.md`.
- `EC-<n>`: the *n*-th bullet of "Edge Cases" in `spec.md` (1 homonyms … 12 Plugin Pages absent).
- `R<n>`: decision in `research.md` when a behaviour pins a recorded design ceiling.
- `INV-1`: recorded invariant, ownership is recomputed locally every run so a release added to
  the library disappears within one cycle even when no source was reachable (SC-004, SC-007).

## Outer loop: acceptance behaviors

One per acceptance scenario. No host-level acceptance runner exists (`acceptance: null` in the
profile), so each is an integration test that composes the real entry points with substituted
Jellyfin services: `RefreshNewReleasesTask.ExecuteAsync` with `StubHttpMessageHandler` fixtures
and `LibraryFakes`, then `ReleasesController` / `AdminController` actions with a `ClaimsPrincipal`
carrying `Jellyfin-UserId`, on a real SQLite `TestDatabase`. This is weaker than a browser test;
the web pages are checked manually (see Out of scope).

| id  | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| A1  | Library has A with X, Y; sources list X, Y, Z → after a run `GET api/releases` lists Z under A and neither X nor Y | US1-AS1 | example | PENDING | `tests/Jellyfin.Plugin.NewReleases.Tests/Acceptance/BrowseReleasesTests.cs` |
| A2  | Releases from several years come back ordered newest first with undated rows last, each carrying its year in `date` | US1-AS2 | example | PENDING | `Acceptance/BrowseReleasesTests.cs` |
| A3  | With 40 releases, `?artistId=` returns only that artist's rows; the same call without the filter returns all 40 | US1-AS3 | example | PENDING | `Acceptance/BrowseReleasesTests.cs` |
| A4  | Every listed release carries one `sources[]` link per source that lists it, each an `https` URL on `musicbrainz.org` or `www.deezer.com` | US1-AS4 | example | PENDING | `Acceptance/BrowseReleasesTests.cs` |
| A5  | With no completed run, the list response has `hasCompletedRefresh=false` and no items | US1-AS5 | example | PENDING | `Acceptance/BrowseReleasesTests.cs` |
| A6  | Z is listed after run 1; Z appears in the library snapshot; after run 2 Z is absent from the list | US1-AS6, INV-1 | example | PENDING | `Acceptance/BrowseReleasesTests.cs` |
| A7  | A release dated after `serverToday` is returned with `state=Upcoming`; `?state=Upcoming` returns only it | US1-AS7 | example | PENDING | `Acceptance/BrowseReleasesTests.cs` |
| A8  | Library album W holds 8 of the source edition's 10 tracks → W is listed `Incomplete` with exactly the 2 missing titles and `comparedEdition` naming the source and edition | US1-AS8 | example | PENDING | `Acceptance/BrowseReleasesTests.cs` |
| A9 | A new `PluginConfiguration` has both sources enabled and exactly Album and EP included | US2-AS1 | example | DONE | `Configuration/PluginConfigurationTests.cs::Defaults_BothSourcesEnabled_ExactlyAlbumsAndEpsIncluded_NoCutoffNoContact` |
| A10 | A fully changed configuration serialized with `XmlSerializer` and read back is equal field by field | US2-AS2 | example | DONE | `Configuration/PluginConfigurationTests.cs::XmlRoundTrip_FullyChangedConfiguration_IsEqualFieldByField` |
| A11 | `POST api/admin/run-now` queues the task (202); after a completed run `GET api/admin/status.lastRun` has `endedAt`, `artistsProcessed`, `releasesFound` | US2-AS3 | example | PENDING | `tests/Jellyfin.Plugin.NewReleases.Tests/Acceptance/ConfigureAndRunTests.cs` |
| A12 | MusicBrainz answers 503 to every call while Deezer succeeds → admin status shows MusicBrainz `CoolingDown` with `lastError`, and Deezer's releases are listed | US2-AS4 | example | PENDING | `Acceptance/ConfigureAndRunTests.cs` |
| A13 | After `POST api/admin/purge` the list is empty; after the next run a release archived before the purge is still in `?archived=true` | US2-AS5 | example | PENDING | `Acceptance/ConfigureAndRunTests.cs` |
| A14 | A source whose terms are not accepted stays disabled with the reason shown | US2-AS6 | example | DROPPED | no v1 source requires terms acceptance (research R12); becomes testable with the Bandcamp feature |
| A15 | After `POST api/admin/clear-archive`, `?archived=true` is empty, the archived releases are back in the list, and the number of stored releases is unchanged | US2-AS7 | example | PENDING | `Acceptance/ConfigureAndRunTests.cs` |
| A16 | `POST api/releases/{id}/ignore` removes the release from the list at once; it is still absent after a run and present in `?archived=true` with `kind=Ignore` | US3-AS1 | example | PENDING | `tests/Jellyfin.Plugin.NewReleases.Tests/Acceptance/ArchiveTests.cs` |
| A17 | `POST api/releases/{id}/restore` on an archived release puts it back in the list at its date position | US3-AS2 | example | PENDING | `Acceptance/ArchiveTests.cs` |
| A18 | `have-it` on an `Incomplete` release archives it; after a later run that still finds tracks missing it remains archived with `kind=HaveIt` | US3-AS3 | example | PENDING | `Acceptance/ArchiveTests.cs` |
| A19 | A `have-it` release whose library album later gains every track stays in the Archive; the decision is not reopened | US3-AS4 | example | PENDING | `Acceptance/ArchiveTests.cs` |

## Inner loop: unit behaviors

Grouped by the component from `plan.md` that owns them. Tests mirror the source path under
`tests/Jellyfin.Plugin.NewReleases.Tests/`.

### `src/Jellyfin.Plugin.NewReleases/Matching/TitleNormalizer.cs`

| id  | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U1 | `Café Bleu` normalizes to `cafe bleu` (NFKC, diacritics stripped) | FR-005c | example | DONE | `Matching/TitleNormalizerTests.cs::NormalizeAlbum_StripsDiacritics` |
| U2 | `Rock & Roll` normalizes to `rock and roll` (case-fold, `&` → `and`) | FR-005c | example | DONE | `Matching/TitleNormalizerTests.cs::NormalizeAlbum_ReplacesAmpersandWithAnd` |
| U3 | `A.B.  --  C!` normalizes to `ab c` (punctuation removed, whitespace collapsed, trimmed) | FR-005c | example | DONE | `Matching/TitleNormalizerTests.cs::NormalizeAlbum_RemovesPunctuationAndCollapsesWhitespace` |
| U4 | Album `TANZNEID (24-bit HD audio)`, `Random Access Memories [Explicit]`, `Album - 10th Anniversary Edition` each lose their trailing qualifier | FR-005c, EC-3 | example | DONE | `Matching/TitleNormalizerTests.cs::NormalizeAlbum_RemovesTrailingEditionQualifier` |
| U5 | Album `X (Deluxe) (Remastered)` loses only the last qualifier → `x deluxe`; `Deluxe Edition Blues` keeps its words | FR-005c | example | DONE | `Matching/TitleNormalizerTests.cs::NormalizeAlbum_RemovesAtMostOneQualifierFromTheEndOnly` |
| U6 | Track `Get Lucky (feat. Pharrell Williams)` and `Get Lucky ft. Pharrell` normalize to `get lucky` | FR-005c | example | DONE | `Matching/TitleNormalizerTests.cs::NormalizeTrack_RemovesTrailingFeaturedArtist` |
| U7 | Track `Song (Deluxe)` keeps `deluxe` (album qualifier rule does not apply to tracks) | FR-005c | example | DONE | `Matching/TitleNormalizerTests.cs::NormalizeTrack_KeepsEditionQualifier` |
| U8 | `The Album` ≠ `Album` and `Vol. 2` ≠ `Volume 2` after normalization | FR-005c | example | DONE | `Matching/TitleNormalizerTests.cs::NormalizeAlbum_KeepsArticlesAndAbbreviationsDistinct` |
| U9 | Artist name normalization applies rules 1–5 only (`Sigur Rós` → `sigur ros`, no qualifier or feat. removal) | FR-002 | example | DONE | `Matching/TitleNormalizerTests.cs::NormalizeName_AppliesBaseRulesOnly` |

### `src/Jellyfin.Plugin.NewReleases/Matching/ReleaseTypeMapper.cs`

| id  | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U10 | MusicBrainz primary `Album`/`EP`/`Single` map to Album/EP/Single; `Broadcast` and `Other` map to `Other` | FR-004 | example | DONE | `Matching/ReleaseTypeMapperTests.cs::MapMusicBrainz_MapsPrimaryType` |
| U11 | MusicBrainz secondaries `Compilation`/`Live`/`Remix`/`Soundtrack` map by name; `DJ-mix`, `Mixtape/Street`, `Demo`, `Audiobook` map to `Other` | FR-004 | example | DONE | `Matching/ReleaseTypeMapperTests.cs::MapMusicBrainz_MapsSecondaryTypesByNameAndUnknownToOther` |
| U12 | Deezer `album`/`ep`/`single`/`compile` map to Album/EP/Single/Compilation as primary with no secondaries | FR-004 | example | DONE | `Matching/ReleaseTypeMapperTests.cs::MapDeezer_MapsRecordTypeAsPrimaryWithNoSecondaries` |
| U13 | EP with no secondaries is included under the default selection | FR-004 | example | DONE | `Matching/ReleaseTypeMapperTests.cs::IsIncluded_EpWithNoSecondaries_IsIncludedByDefault` |
| U14 | Album + Live is excluded under the default selection (Live disabled) | FR-004 | example | DONE | `Matching/ReleaseTypeMapperTests.cs::IsIncluded_AlbumPlusLive_IsExcludedByDefault` |
| U15 | Album + Live is included when Live is enabled; Album + Compilation + Live needs both enabled | FR-004 | example | DONE | `Matching/ReleaseTypeMapperTests.cs::IsIncluded_EverySecondaryMustBeEnabled` |
| U16 | Any `Other` primary or secondary excludes the release whatever the selection | FR-004 | example | DONE | `Matching/ReleaseTypeMapperTests.cs::IsIncluded_AnyOtherType_ExcludesWhateverTheSelection` |
| U17 | Display type is the first of Live, Remix, Soundtrack, Compilation present (Album+Compilation+Live → Live); with no secondaries it is the primary | FR-004 | example | DONE | `Matching/ReleaseTypeMapperTests.cs::DisplayType_FirstSecondaryByPrecedence_ElsePrimary` |

### `src/Jellyfin.Plugin.NewReleases/Storage/Database.cs`

| id  | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U18 | Database path is `{DataPath}/newreleases/newreleases.db` and the directory is created on first open | FR-006 | example | DONE | `Storage/DatabaseTests.cs::OpenAsync_UsesNewReleasesFileUnderDataPathAndCreatesTheDirectory` |
| U19 | First open applies `001_initial.sql`: every table from data-model.md exists and `schema_version` is 1 | FR-006 | example | DONE | `Storage/DatabaseTests.cs::OpenAsync_FirstOpenAppliesTheInitialMigration` |
| U20 | Second open applies no migration and `schema_version` still has one row | FR-006 | example | DONE | `Storage/DatabaseTests.cs::OpenAsync_SecondOpenFromAFreshInstanceAppliesNothing` |
| U21 | Two concurrent first opens run the migration once | R13 | example | DONE | `Storage/DatabaseTests.cs::OpenAsync_TwoConcurrentFirstOpensRunTheMigrationOnce` |

### `src/Jellyfin.Plugin.NewReleases/Storage/ArtistRepository.cs`

| id  | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U22 | Upserting an artist by `artist_key` keeps its `id` and updates name, mbid, `library_ids`, `album_count` | FR-001 | example | DONE | `Storage/ArtistRepositoryTests.cs::UpsertAsync_SameKeyKeepsIdAndUpdatesFields` |
| U23 | Deleting artists absent from the snapshot removes their `artist_source`, `release`, `source_entry`, `edition` rows | FR-014, EC-9 | example | DONE | `Storage/ArtistRepositoryTests.cs::DeleteMissingAsync_RemovesArtistsAbsentFromTheSnapshotWithTheirRows` |
| U24 | Rotation returns artists with NULL `last_refreshed_at` first, then oldest first, ties by name | EC-6 | example | DONE | `Storage/ArtistRepositoryTests.cs::GetRotationAsync_NeverRefreshedFirstThenOldestThenName` |
| U25 | `artist_source` upsert stores status, source artist id, unmatched reason, outcome, `resume_offset`; a Complete outcome resets the offset to 0 | FR-002, FR-014 | example | DONE | `Storage/ArtistRepositoryTests.cs::ArtistSource_UpsertStoresMatchAndOutcome_CompleteResetsOffset` |
| U26 | Admin counts return total library artists, matched per source, and the unmatched list with each source's reason | FR-012 | example | DONE | `Storage/ArtistRepositoryTests.cs::GetCountsAsync_ReportsTotalsMatchedPerSourceAndUnmatchedReasons` |

### `src/Jellyfin.Plugin.NewReleases/Storage/ReleaseRepository.cs`

| id  | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U27 | Upserting the same normalized title from two sources yields one `release` with two `source_entry` rows | FR-006a | example | DONE | `Storage/ReleaseRepositoryTests.cs::UpsertFromSourceAsync_SameNormalizedTitleFromTwoSources_OneReleaseTwoEntries` |
| U28 | When a MusicBrainz entry exists, canonical source, id, types and date come from it | FR-006a, R17 | example | DONE | `Storage/ReleaseRepositoryTests.cs::UpsertFromSourceAsync_MusicBrainzEntryIsCanonicalForSourceIdTypesAndDate` |
| U29 | Removing the MusicBrainz entry makes the Deezer entry canonical | FR-006a, R17 | example | DONE | `Storage/ReleaseRepositoryTests.cs::PruneEntriesAsync_RemovingTheMusicBrainzEntryMakesDeezerCanonical` |
| U30 | A MusicBrainz entry without a date takes the Deezer entry's date | FR-006a | example | DONE | `Storage/ReleaseRepositoryTests.cs::UpsertFromSourceAsync_MusicBrainzWithoutDateTakesDeezerDate` |
| U31 | Pruning for (artist, source, run) deletes only that pair's entries with `last_seen_run_id < run`; other artists and the other source are untouched | FR-014 | example | DONE | `Storage/ReleaseRepositoryTests.cs::PruneEntriesAsync_DeletesOnlyThePairsStaleEntries` |
| U32 | A release left with zero entries after pruning is deleted; one with a remaining entry stays | FR-014 | example | DONE | `Storage/ReleaseRepositoryTests.cs::PruneEntriesAsync_DeletesReleasesLeftWithoutEntriesAndKeepsTheOthers` |
| U33 | `date_sort` is `2024-00-00` for `2024`, `2024-05-00` for `2024-05`, `2024-05-17` for a full date, NULL for no date | R16, EC-4 | example | DONE | `Storage/ReleaseRepositoryTests.cs::UpsertFromSourceAsync_PadsDateSortAndLeavesUndatedNull` |
| U34 | List order is `date_sort` descending, undated last, title as tiebreak; a year-only 2024 release sorts after `2024-01-01` | US1-AS2, EC-4 | example | DONE | `Storage/ReleaseRepositoryTests.cs::ListAsync_OrdersByDateDescendingUndatedLastTitleTiebreak_YearOnlyAfterDated` |
| U35 | Releases with ownership `Owned` are never returned by the list | FR-007 | example | DONE | `Storage/ReleaseRepositoryTests.cs::ListAsync_NeverReturnsOwnedReleases` |
| U36 | The list applies the enabled type set at read time: Album+Live is absent by default and present when Live is enabled, with `type=Live` | FR-004, R7 | example | DONE | `Storage/ReleaseRepositoryTests.cs::ListAsync_AppliesTheEnabledTypeSetAtReadTime` |
| U37 | With `ReleasedSince=2020-01-01`, a release dated `2020-01-01` is returned, `2019-12-31` is not, and an undated release is returned | FR-003, R7 | example | DONE | `Storage/ReleaseRepositoryTests.cs::ListAsync_ReleasedSinceIsInclusiveAndKeepsUndatedRows` |
| U38 | Filters: `artistId` narrows to one artist; `type` matches the display type; `state=Upcoming` returns rows with `date_sort > today` and not a row dated today; `Missing`/`Incomplete` exclude upcoming rows; `from`/`to` are inclusive and exclude undated rows | FR-008 | example | DONE | `Storage/ReleaseRepositoryTests.cs::ListAsync_FiltersByArtistTypeStateAndInclusiveDateRange` |
| U39 | `archived=false` omits rows that have a decision; `archived=true` returns only those, with kind and decided-at | FR-008, FR-016 | example | DONE | `Storage/ReleaseRepositoryTests.cs::ListAsync_ArchivedFlagSplitsDecidedRowsFromTheList` |
| U40 | The list returns at most 5 000 rows | R16 | example | DONE | `Storage/ReleaseRepositoryTests.cs::ListAsync_ReturnsAtMostFiveThousandRows` |
| U41 | Edition upsert is unique on (source, source edition id); writing ownership stores state, method, library album id, compared edition id, missing tracks | FR-005, FR-005a | example | DONE | `Storage/ReleaseRepositoryTests.cs::Editions_UpsertIsUniquePerSourceEditionId_AndOwnershipColumnsAreStored` |
| U42 | Purge empties `release`, `source_entry`, `edition` and leaves `decision`, `library_artist`, `artist_source` rows in place | FR-013 | example | DONE | `Storage/ReleaseRepositoryTests.cs::PurgeAsync_EmptiesReleaseDataAndKeepsDecisionsArtistsAndSourceState` |
| U43 | Listing 500 stored releases completes within 500 ms on the development machine (asserted at 2 000 ms in the suite for CI headroom; the measured time is written to the test output) | SC-005 | example | DONE | `Storage/ReleaseRepositoryTests.cs::ListAsync_FiveHundredStoredReleases_ListsWithinBudget` |

### `src/Jellyfin.Plugin.NewReleases/Storage/ArchiveRepository.cs`

| id  | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U44 | Setting a decision inserts it; setting again for the same key replaces kind, user and time | FR-016 | example | DONE | `Storage/ArchiveRepositoryTests.cs::SetAsync_InsertsThenReplacesKindUserAndTime` |
| U45 | Removing a decision deletes only that key; clearing deletes all | FR-013, FR-016 | example | DONE | `Storage/ArchiveRepositoryTests.cs::RemoveAsync_DeletesOnlyThatKey_ClearAsync_DeletesAll` |
| U46 | A decision written before a purge still joins the release once the same title is upserted again | FR-013, R8 | example | DONE | `Storage/ArchiveRepositoryTests.cs::Decision_SurvivesPurgeAndJoinsTheReleaseWhenItIsFetchedAgain` |

### `src/Jellyfin.Plugin.NewReleases/Storage/SourceStateRepository.cs`

| id  | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U47 | Recording a call increments `calls_today`; when the stored day is not today (UTC) the counter restarts at 1 | FR-011 | example | DONE | `Storage/SourceStateRepositoryTests.cs::RecordCallAsync_IncrementsCallsToday_AndRestartsAtOneOnANewUtcDay` |
| U48 | Remaining budget is budget minus calls today, never below 0 | FR-011 | example | DONE | `Storage/SourceStateRepositoryTests.cs::GetRemainingBudgetAsync_IsBudgetMinusCallsToday_NeverBelowZero` |
| U49 | Four consecutive failures set no cooldown; the fifth sets `cooldown_until = now + 6 h` | FR-011 | example | DONE | `Storage/SourceStateRepositoryTests.cs::RecordFailureAsync_FourFailuresNoCooldown_FifthSetsCooldownSixHoursFromNow` |
| U50 | A success resets consecutive failures to 0 and clears the cooldown | FR-011 | example | DONE | `Storage/SourceStateRepositoryTests.cs::RecordSuccessAsync_ResetsFailuresClearsCooldownAndStampsLastSuccess` |
| U51 | In-cooldown is true one second before `cooldown_until` and false at it | FR-011 | example | DONE | `Storage/SourceStateRepositoryTests.cs::IsInCooldownAsync_TrueOneSecondBeforeCooldownUntil_FalseAtIt` |
| U52 | Runs are started and finished with counts and outcome; latest completed run and latest run are read back | FR-012, FR-015 | example | DONE | `Storage/SourceStateRepositoryTests.cs::Runs_StartAndFinishWithCounts_LatestCompletedAndLatestAreReadBack` |

### `src/Jellyfin.Plugin.NewReleases/Configuration/PluginConfiguration.cs`

| id  | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U53 | `EnabledReleaseTypes()` is {Album, EP} by default and reflects each toggled flag | FR-004 | example | DONE | `Configuration/PluginConfigurationTests.cs::EnabledReleaseTypes_AlbumAndEpByDefault_ReflectsEachToggle` |
| U54 | `ReleasedSinceDate()` parses `2020-01-01`; empty and `yesterday` give null | FR-003 | example | DONE | `Configuration/PluginConfigurationTests.cs::ReleasedSinceDate_ParsesIsoDate_EmptyOrInvalidIsNull` |
| U55 | Deserializing XML that lacks every element yields the defaults (forward migration of older configs) | US2-AS2 | example | DONE | `Configuration/PluginConfigurationTests.cs::Deserialize_XmlWithoutAnyElement_YieldsTheDefaults` |

### `src/Jellyfin.Plugin.NewReleases/Sources/UserAgentBuilder.cs`

| id  | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U56 | Without a contact the User-Agent is `JellyfinNewReleases/<version>` | FR-018 | example | DONE | `Sources/UserAgentBuilderTests.cs::Build_WithoutContact_IsProductAndVersion` |
| U57 | With contact ` me@example.org ` the User-Agent is `JellyfinNewReleases/<version> ( me@example.org )` | FR-018 | example | DONE | `Sources/UserAgentBuilderTests.cs::Build_WithContact_AppendsTrimmedContactInParentheses` |

### `src/Jellyfin.Plugin.NewReleases/Sources/SourceHttpClient.cs`

| id  | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U58 | Every request carries the User-Agent from `UserAgentBuilder` | FR-018 | example | DONE | `Sources/SourceHttpClientTests.cs::GetStringAsync_EveryRequestCarriesThePluginUserAgent` |
| U59 | Each HTTP request records one call in `source_state` for that source | FR-011 | example | DONE | `Sources/SourceHttpClientTests.cs::GetStringAsync_RecordsOneCallPerHttpRequestForThatSource` |
| U60 | With 1 call of budget left the request is sent; with 0 left `DailyBudgetExhaustedException` is thrown and nothing is sent | FR-011 | example | DONE | `Sources/SourceHttpClientTests.cs::GetStringAsync_LastUnitOfBudgetIsSent_ExhaustedBudgetThrowsWithoutSending` |
| U61 | A 503 with `Retry-After: 2` sets `next_allowed_at = now + 2 s`, waits, retries, and returns the later 200 body | FR-011 | example | DONE | `Sources/SourceHttpClientTests.cs::GetStringAsync_503WithRetryAfter_SetsNextAllowedAtWaitsRetriesAndReturnsTheLaterBody` |
| U62 | Persistent 500s are retried three times (200 ms, 800 ms, 3 200 ms) then surface as `HttpRequestException` | FR-011 | example | DONE | `Sources/SourceHttpClientTests.cs::GetStringAsync_Persistent500_RetriesThreeTimesWithBackoffThenThrows` |
| U63 | A 404 or 400 throws immediately with no retry | FR-011 | example | DONE | `Sources/SourceHttpClientTests.cs::GetStringAsync_ClientError_ThrowsImmediatelyWithoutRetry` |
| U64 | A failed request increments consecutive failures; a later success resets them | FR-011 | example | DONE | `Sources/SourceHttpClientTests.cs::GetStringAsync_FailureIncrementsConsecutiveFailures_LaterSuccessResetsThem` |
| U65 | `IsAvailableAsync` is false during cooldown or before `next_allowed_at`, true otherwise | FR-011 | example | DONE | `Sources/SourceHttpClientTests.cs::IsAvailableAsync_FalseDuringCooldownOrBeforeNextAllowedAt_TrueOtherwise` |
| U66 | An unknown source id throws `ArgumentException` | FR-011 | example | DONE | `Sources/SourceHttpClientTests.cs::GetStringAsync_UnknownSourceId_ThrowsArgumentException` |
| U67 | Two back-to-back MusicBrainz requests are at least ~1 s apart (token bucket at 1 request/s) | FR-011 | example | DONE | `Sources/SourceHttpClientTests.cs::GetStringAsync_TwoMusicBrainzRequests_AreAtLeastOneSecondApart` |

### `src/Jellyfin.Plugin.NewReleases/Library/LibraryScanner.cs`

| id  | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U68 | Album artists become library artists; an artist credited only as a featured track artist does not | FR-001 | example | DONE | `Library/LibraryScannerTests.cs::Scan_AlbumArtistsBecomeLibraryArtists_FeaturedOnlyArtistsDoNot` |
| U69 | MBID is read from the artist's `MusicBrainzArtist`, else the album's `MusicBrainzAlbumArtist`; with neither the key is `name:<normalized name>` | FR-002 | example | DONE | `Library/LibraryScannerTests.cs::Scan_MbidFromArtistThenAlbumArtistProviderId_ElseNameKey` |
| U70 | Each album snapshot carries `MusicBrainzAlbum`, `MusicBrainzReleaseGroup` and the normalized titles of its `Audio` children (queried by `ParentId`) | FR-005 | example | DONE | `Library/LibraryScannerTests.cs::Scan_AlbumSnapshotsCarryIdentifiersAndNormalizedTrackTitles` |
| U71 | `LibraryIds` are the distinct collection folder ids of the artist's albums | FR-007 | example | DONE | `Library/LibraryScannerTests.cs::Scan_LibraryIdsAreTheDistinctCollectionFoldersOfTheArtistsAlbums` |
| U72 | Two `MusicArtist` items sharing an MBID become one library artist holding both albums | EC-10 | example | DONE | `Library/LibraryScannerTests.cs::Scan_TwoArtistItemsSharingAnMbid_BecomeOneLibraryArtistWithBothAlbums` |
| U73 | The scanner never calls `ILibraryManager.GetArtist(string)` | R6 | example | DONE | `Library/LibraryScannerTests.cs::Scan_NeverCallsGetArtistByName` |

### `src/Jellyfin.Plugin.NewReleases/Sources/MusicBrainzSource.cs`

| id  | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U74 | An artist snapshot with an MBID is `Matched(mbid)` with zero HTTP requests | FR-002 | example | DONE | `Sources/MusicBrainzSourceTests.cs::MatchArtistAsync_SnapshotWithMbid_IsMatchedWithoutAnyRequest` |
| U75 | Top score 85 with the runner-up at 79 → `Matched` | FR-002 | example | DONE | `Sources/MusicBrainzSourceTests.cs::MatchArtistAsync_ConfidentTopResult_IsMatched` |
| U76 | Top score 90 with the runner-up at 85 (5 points) → `Unmatched` with an "ambiguous" reason | FR-002, EC-1 | example | DONE | `Sources/MusicBrainzSourceTests.cs::MatchArtistAsync_RunnerUpWithinFivePoints_IsUnmatchedAsAmbiguous` |
| U77 | Top score 90 with the runner-up at 84 (6 points) → `Matched` | FR-002 | example | DONE | `Sources/MusicBrainzSourceTests.cs::MatchArtistAsync_RunnerUpSixPointsBehind_IsMatched` |
| U78 | Top score 84 → `Unmatched` with a "low score" reason | FR-002 | example | DONE | `Sources/MusicBrainzSourceTests.cs::MatchArtistAsync_TopScoreBelow85_IsUnmatchedAsLowScore` |
| U79 | An empty `artists` array → `Unmatched` with a "no result" reason | FR-002, EC-2 | example | DONE | `Sources/MusicBrainzSourceTests.cs::MatchArtistAsync_NoResults_IsUnmatchedAsNoResult` |
| U80 | A catalogue page groups releases by release group, maps primary and secondary types, keeps `first-release-date` as `2013`, `2013-05` or `2013-05-17`, and links `https://musicbrainz.org/release-group/<id>` | FR-003, FR-004 | example | DONE | `Sources/MusicBrainzSourceTests.cs::FetchCataloguePageAsync_GroupsByReleaseGroupMapsTypesKeepsPartialDatesAndLinksTheReleaseGroup` |
| U81 | `NextOffset` is `offset + 100` while `release-count` exceeds it and null on the last page | FR-003 | example | DONE | `Sources/MusicBrainzSourceTests.cs::FetchCataloguePageAsync_NextOffsetAdvancesBy100WhileTheCountExceedsIt_NullOnTheLastPage` |
| U82 | Editions come back one per Official release with all `media[].tracks[].title` normalized as tracks | FR-005 | example | DONE | `Sources/MusicBrainzSourceTests.cs::FetchEditionsAsync_OneEditionPerOfficialReleaseWithAllMediaTracksNormalized` |
| U83 | Request URLs are exactly the endpoints in `contracts/release-source.md` and their query strings contain only the artist name or ids | FR-017 | example | DONE | `Sources/MusicBrainzSourceTests.cs::Requests_UseExactlyTheContractEndpointsWithOnlyNamesAndIdsInTheQuery` |
| U84 | A persistent 503 surfaces as an exception, not as `Unmatched` or an empty page | FR-014 | example | DONE | `Sources/MusicBrainzSourceTests.cs::PersistentServiceUnavailable_SurfacesAsAnException` |

### `src/Jellyfin.Plugin.NewReleases/Sources/DeezerSource.cs`

| id  | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U85 | A candidate with the exact normalized name whose first albums page contains a library album title → `Matched(id)` | FR-002 | example | DONE | `Sources/DeezerSourceTests.cs::MatchArtistAsync_ExactNameWhoseFirstAlbumsPageContainsALibraryAlbum_IsMatched` |
| U86 | A candidate with the exact name but no album matching the library → `Unmatched` with a "no corroborating album" reason | FR-002, EC-1 | example | DONE | `Sources/DeezerSourceTests.cs::MatchArtistAsync_ExactNameWithoutACorroboratingAlbum_IsUnmatchedWithReason` |
| U87 | Two exact-name homonyms where only the second is corroborated → `Matched(second)` | FR-002, EC-1 | example | DONE | `Sources/DeezerSourceTests.cs::MatchArtistAsync_TwoExactNameHomonyms_OnlyTheSecondCorroborated_MatchesTheSecond` |
| U88 | No search results → `Unmatched` | FR-002, EC-2 | example | DONE | `Sources/DeezerSourceTests.cs::MatchArtistAsync_NoSearchResults_IsUnmatched` |
| U89 | An albums page maps `record_type`, sets `NextOffset` from `next` when present and null when absent | FR-003, FR-004 | example | DONE | `Sources/DeezerSourceTests.cs::FetchCataloguePageAsync_MapsRecordTypeAndTakesNextOffsetFromNext` |
| U90 | `release_date` `0000-00-00` becomes a null date; `link` becomes the source URL | FR-003, EC-4 | example | DONE | `Sources/DeezerSourceTests.cs::FetchCataloguePageAsync_UnknownDateBecomesNull_LinkBecomesTheUrl` |
| U91 | Album tracks spread over two `next` pages come back as one edition titled as the album | FR-005 | example | DONE | `Sources/DeezerSourceTests.cs::FetchEditionsAsync_TracksAcrossTwoPages_ComeBackAsOneEditionTitledAsTheAlbum` |
| U92 | An HTTP 200 body with `error.code = 4` is retried as transient; any other `error` code throws | FR-011, R3 | example | DONE | `Sources/DeezerSourceTests.cs::ErrorEnvelope_QuotaCode4IsRetriedAsTransient_OtherCodesThrow` |

### `src/Jellyfin.Plugin.NewReleases/Matching/OwnershipMatcher.cs`

| id  | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U93 | An album whose `MusicBrainzReleaseGroup` equals the canonical id is the candidate with method `Identifier` | FR-005 | example | DONE | `Matching/OwnershipMatcherTests.cs::Decide_AlbumWithTheCanonicalReleaseGroupId_IsTheCandidateByIdentifier` |
| U94 | An album whose `MusicBrainzAlbum` equals a stored MusicBrainz edition id is the candidate with method `Identifier` | FR-005 | example | DONE | `Matching/OwnershipMatcherTests.cs::Decide_AlbumWhoseReleaseIdIsAStoredMusicBrainzEdition_IsTheCandidateByIdentifier` |
| U95 | With no identifier match, an album titled `Album (Deluxe Edition)` is the candidate for release `Album` with method `Title` | FR-005, EC-3 | example | DONE | `Matching/OwnershipMatcherTests.cs::Decide_WithoutIdentifierMatch_AlbumTitledWithAnEditionQualifier_IsTheCandidateByTitle` |
| U96 | With no candidate the result is `Missing` and no edition fetch is requested | FR-005, EC-7 | example | DONE | `Matching/OwnershipMatcherTests.cs::Decide_NoCandidate_IsMissingAndAsksForNoEditions` |
| U97 | With a candidate and no stored editions the result asks for editions instead of deciding | FR-005, EC-7 | example | DONE | `Matching/OwnershipMatcherTests.cs::Decide_CandidateWithoutStoredEditions_AsksForEditionsInsteadOfDeciding` |
| U98 | Every edition track matched by a library track → `Owned` | FR-005 | example | DONE | `Matching/OwnershipMatcherTests.cs::Decide_EveryEditionTrackMatchedByALibraryTrack_IsOwned` |
| U99 | 8 of 10 matched → `Incomplete` listing the 2 missing titles, the edition and its source | FR-005a, US1-AS8 | example | DONE | `Matching/OwnershipMatcherTests.cs::Decide_EightOfTenTracksMatched_IsIncompleteNamingTheTwoMissingTitlesAndTheEdition` |
| U100 | Edition choice: more matched tracks wins; equal matched → fewer missing; still equal → MusicBrainz over Deezer; still equal → lowest edition id | FR-005 | example | DONE | `Matching/OwnershipMatcherTests.cs::Decide_EditionChoice_MostMatchedThenFewestMissingThenMusicBrainzThenLowestId` |
| U101 | Extra library tracks beyond the edition do not prevent `Owned` | FR-005 | example | DONE | `Matching/OwnershipMatcherTests.cs::Decide_ExtraLibraryTracksBeyondTheEdition_DoNotPreventOwned` |
| U102 | Editions with no tracks at all yield `Owned` by album presence | FR-005 | example | DONE | `Matching/OwnershipMatcherTests.cs::Decide_EditionsWithoutAnyTracks_AreOwnedByAlbumPresence` |

### `src/Jellyfin.Plugin.NewReleases/ScheduledTasks/RefreshNewReleasesTask.cs`

| id  | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U103 | Task `Name` is `Refresh new releases`, `Category` is `New Releases`, `Key` is `NewReleases.Refresh`, default trigger is daily at 03:00 | FR-009 | example | DONE | `ScheduledTasks/RefreshNewReleasesTaskTests.cs::Metadata_NameCategoryKeyAndDailyTriggerAtThree` |
| U104 | Artists are processed in rotation order and `last_refreshed_at` advances only when every enabled source was attempted for the artist | EC-6 | example | PENDING | `ScheduledTasks/RefreshNewReleasesTaskTests.cs` |
| U105 | A Complete fetch removes the source's entries the page set no longer contains and deletes the releases left without entries | FR-014 | example | PENDING | `ScheduledTasks/RefreshNewReleasesTaskTests.cs` |
| U106 | Budget exhausted after page 1 of 2 → outcome `Partial`, `resume_offset = 100`, nothing removed; the next run fetches from offset 100 | FR-014, EC-5, EC-8 | example | PENDING | `ScheduledTasks/RefreshNewReleasesTaskTests.cs` |
| U107 | A fetch that throws → outcome `Failed`, `last_error` recorded, nothing removed | FR-014, EC-5 | example | PENDING | `ScheduledTasks/RefreshNewReleasesTaskTests.cs` |
| U108 | Edition requests are issued only for releases with a library album candidate (request count equals candidates) | EC-7 | example | PENDING | `ScheduledTasks/RefreshNewReleasesTaskTests.cs` |
| U109 | An artist missing from the snapshot loses its releases after the run | FR-014, EC-9 | example | PENDING | `ScheduledTasks/RefreshNewReleasesTaskTests.cs` |
| U110 | A completed run writes one `refresh_run` row with counts and `Completed`; a cancelled run writes `Cancelled` | FR-012 | example | PENDING | `ScheduledTasks/RefreshNewReleasesTaskTests.cs` |
| U111 | A source disabled in configuration receives no request | FR-010 | example | PENDING | `ScheduledTasks/RefreshNewReleasesTaskTests.cs` |
| U112 | A source in cooldown is skipped for every artist while the other source is processed | FR-011, EC-5 | example | PENDING | `ScheduledTasks/RefreshNewReleasesTaskTests.cs` |
| U113 | With every source unavailable, ownership is still recomputed from stored editions so a newly complete library album becomes `Owned` | INV-1 | example | PENDING | `ScheduledTasks/RefreshNewReleasesTaskTests.cs` |
| U114 | An `Unmatched` artist is matched again on the next run | EC-2 | example | PENDING | `ScheduledTasks/RefreshNewReleasesTaskTests.cs` |

### `src/Jellyfin.Plugin.NewReleases/Api/ReleasesController.cs`

| id  | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U115 | A request without the `Jellyfin-UserId` claim gets 401 | FR-007 | example | PENDING | `Api/ReleasesControllerTests.cs` |
| U116 | A user with `EnableAllFolders` sees every release; a user whose `EnabledFolders` exclude the library sees none; including it sees them | FR-007, EC-11 | example | PENDING | `Api/ReleasesControllerTests.cs` |
| U117 | `from=2020-01-01` returns a release dated `2020-01-01` and not one dated `2019-12-31`; `type`, `state`, `artistId` are passed to the repository filter | FR-008 | example | PENDING | `Api/ReleasesControllerTests.cs` |
| U118 | `lastRefreshedAt` is the last completed run's end; `refreshIntervalHours` is 24 for a daily trigger, 12 for a 12-hour interval trigger, 24 when no trigger is readable | FR-015 | example | PENDING | `Api/ReleasesControllerTests.cs` |
| U119 | `GET api/artists` returns only artists in libraries the caller may access | FR-007 | example | PENDING | `Api/ReleasesControllerTests.cs` |
| U120 | `ignore` and `have-it` store a decision with the caller's user id and the injected clock; `restore` deletes it; each returns 204 | FR-016 | example | PENDING | `Api/ReleasesControllerTests.cs` |
| U121 | A decision on an unknown release id → 404; on a release outside the caller's libraries → 403 and no decision written | FR-007, FR-016 | example | PENDING | `Api/ReleasesControllerTests.cs` |
| U122 | A second user's `?archived=true` shows the first user's decision with kind and decided-at | FR-016 | example | PENDING | `Api/ReleasesControllerTests.cs` |

### `src/Jellyfin.Plugin.NewReleases/Api/AdminController.cs`

| id  | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U123 | The controller carries `[Authorize(Policy = Policies.RequiresElevation)]` | FR-013 | example | PENDING | `Api/AdminControllerTests.cs` |
| U124 | `run-now` calls `QueueScheduledTask<RefreshNewReleasesTask>()` and returns 202; when the worker reports running it returns 409 without queueing | FR-009 | example | PENDING | `Api/AdminControllerTests.cs` |
| U125 | Status reports per source `Ok`/`Failing`/`CoolingDown`/`Disabled`, `callsToday`, `dailyBudget`, `lastError`, plus `lastRun`, `nextRunAt` from triggers, artist counts, and each unmatched artist with `jellyfinId`, per-source reasons and the hint sentence | FR-012 | example | PENDING | `Api/AdminControllerTests.cs` |
| U126 | `purge` empties release data, keeps decisions and artists, resets every `resume_offset` to 0 | FR-013 | example | PENDING | `Api/AdminControllerTests.cs` |
| U127 | `clear-archive` empties decisions and leaves release rows untouched | FR-013 | example | PENDING | `Api/AdminControllerTests.cs` |

## Invariants and edge cases still to place

None. INV-1 is placed on U113 and A6.

## Out of scope

- Web pages (`Web/user-view.html`, `Web/admin.html`): no JavaScript test runner in the stack
  profile; year grouping, the empty-state and staleness sentences, new-tab links, keyboard
  operation and announcements (FR-019, SC-001, SC-008, US1-AS2 grouping, US1-AS4 new tab) are
  checked manually per `quickstart.md` steps 2–6.
- US2-AS6 terms acceptance: dropped as A14, no v1 source requires it (research R12).
- Plugin Pages entry registration in `Plugin.cs`: pre-existing, not changed by this feature; no
  characterization test needed.
- Two distinct releases by one artist with identical normalized titles merging into one row:
  accepted ceiling (research R8), no test.
- Re-fetching an edition's track list after a source corrects it: accepted ceiling (R11).
- SC-002, SC-003, SC-006: measured on a real library after release, not in the suite.

## Verification commands

Copied verbatim from `.specify/memory/tdd-profile.md` at planning time:

- Single test: `dotnet test --configuration Release --filter "FullyQualifiedName~{name}" -- RunConfiguration.TreatNoTestsAsError=true`
- Full suite: `dotnet test --configuration Release`
- Coverage: not available (`coverage: null`)
- Mutation: not available (`mutation: null`); audit uses deliberate mutants

`{name}` is `Class.Method`. In a shell that did not source `~/.zshenv`, prefix commands with
`PATH=/opt/homebrew/opt/dotnet@9/bin:$PATH DOTNET_ROOT=/opt/homebrew/opt/dotnet@9/libexec`
so `dotnet` is SDK 9 (the default `dotnet` on this Mac is SDK 8 and cannot target `net9.0`).
