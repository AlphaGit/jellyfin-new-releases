# Cycle Log: Track New Releases

Append only. Newest last. Every entry's `red` block is the evidence that the test existed and
failed before the implementation.

## Baseline

- suite: `dotnet test --configuration Release` -> 1 passed, 0 failed (PluginSanityTests only)
- commit: `92a272d`
- recorded: cycle 0, before any change
- note: run with SDK 9 (`PATH=/opt/homebrew/opt/dotnet@9/bin:$PATH DOTNET_ROOT=/opt/homebrew/opt/dotnet@9/libexec`); the default `dotnet` in non-login shells is SDK 8 and fails with NETSDK1045

## Cycle 1: U1 `Café Bleu` normalizes to `cafe bleu`

- test: `tests/Jellyfin.Plugin.NewReleases.Tests/Matching/TitleNormalizerTests.cs::NormalizeAlbum_StripsDiacritics` (new, Theory: `Café Bleu`, `Mø`)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~TitleNormalizerTests.NormalizeAlbum_StripsDiacritics" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: Strings differ / Expected: "cafe bleu" / Actual: "Café Bleu"` (2 failed, pass-through stub)
- green: `src/Jellyfin.Plugin.NewReleases/Matching/TitleNormalizer.cs` `Base()`: NFKD, drop NonSpacingMark, ligature map (ø, æ, ß, …), lower-case, NFKC. Suite -> 3 passed, 0 failed
- refactor: none needed
- commit: `d777ef5`

## Cycle 2: U2 `Rock & Roll` normalizes to `rock and roll`

- test: `Matching/TitleNormalizerTests.cs::NormalizeAlbum_ReplacesAmpersandWithAnd` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~TitleNormalizerTests.NormalizeAlbum_ReplacesAmpersandWithAnd" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Expected: "rock and roll" / Actual:   "rock & roll"` (1 failed)
- green: `TitleNormalizer.Base()` appends `.Replace("&", "and")`. Suite -> 4 passed, 0 failed
- refactor: none needed
- commit: `f87a0a9`

## Cycle 3: U3 `A.B.  --  C!` normalizes to `ab c`

- test: `Matching/TitleNormalizerTests.cs::NormalizeAlbum_RemovesPunctuationAndCollapsesWhitespace` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~TitleNormalizerTests.NormalizeAlbum_RemovesPunctuationAndCollapsesWhitespace" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Expected: "ab c" / Actual:   "a.b.  --  c!"` (1 failed)
- green: `TitleNormalizer.Finish()` keeps letters/digits, folds whitespace runs to one space, trims. Suite -> 5 passed, 0 failed
- refactor: none needed
- commit: `c44230f`

## Cycle 4: U4 album titles lose their trailing edition qualifier

- test: `Matching/TitleNormalizerTests.cs::NormalizeAlbum_RemovesTrailingEditionQualifier` (new, Theory: `TANZNEID (24-bit HD audio)`, `Random Access Memories [Explicit]`, `Album - 10th Anniversary Edition`)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~TitleNormalizerTests.NormalizeAlbum_RemovesTrailingEditionQualifier" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Expected: "tanzneid" / Actual:   "tanzneid 24bit hd audio"` (3 failed)
- green: `TitleNormalizer.StripEditionQualifier()` — regex for one trailing `(…)`/`[…]`/` - …` segment, removed when it contains a qualifier word (rule 6 list); runs after rules 1–3, before rules 4–5 so brackets are still visible. Suite -> 8 passed, 0 failed
- refactor: none needed
- commit: `b432af4`

## Cycle 5: U5 only the last qualifier is removed; `Deluxe Edition Blues` keeps its words

- test: `Matching/TitleNormalizerTests.cs::NormalizeAlbum_RemovesAtMostOneQualifierFromTheEndOnly` (new, Theory: `X (Deluxe) (Remastered)` -> `x deluxe`, `Deluxe Edition Blues` unchanged)
- red: passed on first run (behaviour already produced by cycle 4's single-match regex). Deliberate mutant: `StripEditionQualifier` made recursive -> `Expected: "x deluxe" / Actual:   "x"` (1 failed). Code restored exactly (`git diff` empty), test green again.
- green: no production change. Suite -> 10 passed, 0 failed
- refactor: none needed
- commit: `6696b03`

## Cycle 6: U6 track titles lose a trailing `feat.`/`ft.` segment

- test: `Matching/TitleNormalizerTests.cs::NormalizeTrack_RemovesTrailingFeaturedArtist` (new, Theory: `Get Lucky (feat. Pharrell Williams)`, `Get Lucky ft. Pharrell`)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~TitleNormalizerTests.NormalizeTrack_RemovesTrailingFeaturedArtist" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Expected: "get lucky" / Actual:   "get lucky feat pharrell williams"` (2 failed; `NormalizeTrack` stub = rules 1–5 only)
- green: `TitleNormalizer.NormalizeTrack()` removes one trailing featured-artist segment (regex `TrailingFeaturedArtist`) between rules 1–3 and 4–5. Suite -> 12 passed, 0 failed
- refactor: none needed
- commit: `a12881c`

## Cycle 7: U7 track `Song (Deluxe)` keeps `deluxe`

- test: `Matching/TitleNormalizerTests.cs::NormalizeTrack_KeepsEditionQualifier` (new)
- red: passed on first run. Deliberate mutant: `NormalizeTrack` routed through `StripEditionQualifier` -> `Expected: "song deluxe" / Actual:   "song"` (1 failed). Code restored exactly (`git diff` empty), test green again.
- green: no production change. Suite -> 13 passed, 0 failed
- refactor: none needed
- commit: `1c8512d`

## Cycle 8: U8 `The Album` ≠ `Album`, `Vol. 2` ≠ `Volume 2`

- test: `Matching/TitleNormalizerTests.cs::NormalizeAlbum_KeepsArticlesAndAbbreviationsDistinct` (new, Theory)
- red: passed on first run. Deliberate mutant: `Finish()` drops a leading `the ` -> `Expected: Not "album" / Actual:       "album"` (1 failed). Code restored exactly (`git diff` empty), test green again.
- green: no production change. Suite -> 15 passed, 0 failed
- refactor: none needed
- commit: `142b4de`

## Cycle 9: U9 artist names get rules 1–5 only

- test: `Matching/TitleNormalizerTests.cs::NormalizeName_AppliesBaseRulesOnly` (new, Theory: `Sigur Rós`, `Artist (Deluxe Edition)`, `Duo feat. Guest`)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~TitleNormalizerTests.NormalizeName_AppliesBaseRulesOnly" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Expected: "artist deluxe edition" / Actual:   "artist"` (1 failed; stub routed names through `NormalizeAlbum`)
- green: `TitleNormalizer.NormalizeName()` = `Finish(Base(name))`. Suite -> 18 passed, 0 failed
- refactor: none needed; `Base`/`Finish` already shared by the three entry points
- commit: `7fda815`

## Cycle 10: U10 MusicBrainz primary types map to Album/EP/Single, unknown to Other

- test: `tests/Jellyfin.Plugin.NewReleases.Tests/Matching/ReleaseTypeMapperTests.cs::MapMusicBrainz_MapsPrimaryType` (new, Theory: Album, EP, Single, Broadcast, Other, null)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~ReleaseTypeMapperTests.MapMusicBrainz_MapsPrimaryType" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Expected: Album / Actual:   Other` (3 failed, 3 passed; stub returned Other for everything). `Model/Enums.cs` `ReleaseType` added as the declaration the test needs to compile.
- green: `ReleaseTypeMapper.MapMusicBrainzPrimary()` switch. Suite -> 24 passed, 0 failed
- refactor: none needed
- commit: `24e7717`

## Cycle 11: U11 MusicBrainz secondaries map by name, the rest to Other

- test: `Matching/ReleaseTypeMapperTests.cs::MapMusicBrainz_MapsSecondaryTypesByNameAndUnknownToOther` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~ReleaseTypeMapperTests.MapMusicBrainz_MapsSecondaryTypesByNameAndUnknownToOther" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Expected: [Compilation, Live, Remix, Soundtrack, Other, ···] / Actual:   ReleaseType[] []` (1 failed)
- green: `ReleaseTypeMapper.MapMusicBrainzSecondary()` switch applied to every secondary. Suite -> 25 passed, 0 failed
- refactor: none needed
- commit: `b8cfcc7`

## Cycle 12: U12 Deezer `album`/`ep`/`single`/`compile` map as primary with no secondaries

- test: `Matching/ReleaseTypeMapperTests.cs::MapDeezer_MapsRecordTypeAsPrimaryWithNoSecondaries` (new, Theory incl. unknown `mixtape` -> Other)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~ReleaseTypeMapperTests.MapDeezer_MapsRecordTypeAsPrimaryWithNoSecondaries" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Expected: Album / Actual:   Other` (4 failed, 1 passed; stub returned Other)
- green: `ReleaseTypeMapper.MapDeezer()` switch. Suite -> 30 passed, 0 failed
- refactor: none needed
- commit: `b07cae8`

## Cycle 13: U13 EP with no secondaries is included under the default selection

- test: `Matching/ReleaseTypeMapperTests.cs::IsIncluded_EpWithNoSecondaries_IsIncludedByDefault` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~ReleaseTypeMapperTests.IsIncluded_EpWithNoSecondaries_IsIncludedByDefault" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.True() Failure / Expected: True / Actual:   False` (1 failed; stub returned false)
- green: `IsIncluded` = `enabled.Contains(primary)` (fake-it step; secondaries come with U14). Suite -> 31 passed, 0 failed
- refactor: none needed
- commit: `2aca436`

## Cycle 14: U14 Album + Live is excluded under the default selection

- test: `Matching/ReleaseTypeMapperTests.cs::IsIncluded_AlbumPlusLive_IsExcludedByDefault` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~ReleaseTypeMapperTests.IsIncluded_AlbumPlusLive_IsExcludedByDefault" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.False() Failure / Expected: False / Actual:   True` (1 failed)
- green: `IsIncluded` adds `secondaries.All(enabled.Contains)`. Suite -> 32 passed, 0 failed
- refactor: none needed
- commit: `808678e`

## Cycle 15: U15 Album + Live included with Live enabled; Album + Compilation + Live needs both

- test: `Matching/ReleaseTypeMapperTests.cs::IsIncluded_EverySecondaryMustBeEnabled` (new)
- red: passed on first run (cycle 14's `All` already covers it). Deliberate mutant: `All` -> `Any` -> `Assert.False() Failure / Expected: False / Actual:   True` (1 failed). Code restored exactly (`git diff` empty), test green again.
- green: no production change. Suite -> 33 passed, 0 failed
- refactor: none needed
- commit: `3387ca9`

## Cycle 16: U16 any `Other` primary or secondary excludes the release whatever the selection

- test: `Matching/ReleaseTypeMapperTests.cs::IsIncluded_AnyOtherType_ExcludesWhateverTheSelection` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~ReleaseTypeMapperTests.IsIncluded_AnyOtherType_ExcludesWhateverTheSelection" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.False() Failure / Expected: False / Actual:   True` (1 failed; a selection containing Other let it through)
- green: `IsIncluded` rejects `Other` explicitly for primary and secondaries. Suite -> 34 passed, 0 failed
- refactor: none needed
- commit: `4d79775`

## Cycle 17: U17 display type is the first of Live, Remix, Soundtrack, Compilation present, else the primary

- test: `Matching/ReleaseTypeMapperTests.cs::DisplayType_FirstSecondaryByPrecedence_ElsePrimary` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~ReleaseTypeMapperTests.DisplayType_FirstSecondaryByPrecedence_ElsePrimary" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: Values differ / Expected: Live / Actual:   Album` (1 failed; stub returned the primary)
- green: `ReleaseTypeMapper.DisplayType()` = first of `DisplayPrecedence` contained in secondaries, else primary. Suite -> 35 passed, 0 failed
- refactor: none needed
- commit: `8ed7a3c`

## Cycle 18: U18 database path is `{DataPath}/newreleases/newreleases.db`, directory created on first open

- test: `tests/Jellyfin.Plugin.NewReleases.Tests/Storage/DatabaseTests.cs::OpenAsync_UsesNewReleasesFileUnderDataPathAndCreatesTheDirectory` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~DatabaseTests.OpenAsync_UsesNewReleasesFileUnderDataPathAndCreatesTheDirectory" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: Strings differ / Expected: ···"…/newreleases/newreleases.db" / Actual:   ···"…/newreleases.db"` (1 failed; stub placed the file directly under DataPath)
- green: `Storage/PluginDatabase.cs` — `DirectoryPath`, `DatabasePath`, `ConnectionString` (`Foreign Keys=True`), `OpenAsync` creates the directory and opens a `SqliteConnection`. Suite -> 36 passed, 0 failed
- refactor: none needed
- deviation: class and file are `PluginDatabase` instead of plan.md's `Database`: the name `Database` resolves to the `Jellyfin.Database` namespace from every `Jellyfin.Plugin.NewReleases.*` namespace except `Storage` (CS0118 in the test project). Setup tasks T001–T003 (SQLite package pin, build.yaml artifacts, ported helpers) were done before this cycle as non-behavioural scaffolding, commit `e16967c`.
- commit: `f36f2ba`

## Cycle 19: U19 first open applies `001_initial.sql`; every table exists and `schema_version` is 1

- test: `Storage/DatabaseTests.cs::OpenAsync_FirstOpenAppliesTheInitialMigration` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~DatabaseTests.OpenAsync_FirstOpenAppliesTheInitialMigration" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Superset() Failure: Value is not a superset / Expected: ["library_artist", "artist_source", "release", …] / Actual:   []` (1 failed)
- green: `Storage/Migrations/001_initial.sql` (all nine tables, indexes, UNIQUE/CHECK/CASCADE from data-model.md; `schema_version` is created by code so it exists before the version query) and `PluginDatabase.MigrateAsync` behind a `Lazy<Task>`: applies embedded `NNN_*.sql` with version > MAX(schema_version), one transaction each. Suite -> 37 passed, 0 failed
- refactor: none needed
- commit: `574744b`

## Cycle 20: U20 second open (fresh instance) applies no migration; `schema_version` keeps one row

- test: `Storage/DatabaseTests.cs::OpenAsync_SecondOpenFromAFreshInstanceAppliesNothing` (new)
- red: passed on first run. Deliberate mutant: pending filter `Version > applied` -> `Version >= 0` -> `SqliteException : SQLite Error 1: 'table library_artist already exists'` (1 failed). Code restored exactly (`git diff` empty), test green again.
- green: no production change. Suite -> 38 passed, 0 failed
- refactor: none needed
- commit: `650b01b`

## Cycle 21: U21 two concurrent first opens run the migration once

- test: `Storage/DatabaseTests.cs::OpenAsync_TwoConcurrentFirstOpensRunTheMigrationOnce` (new; four `Task.Run` opens on one instance)
- red: first version of the test (two plain awaited `OpenAsync`, asserting one `schema_version` row) passed on first run **and** survived the mutant (three runs): the SQLite driver is synchronous, so the awaits never overlapped and the second open simply saw version 1. Rewritten per the playbook: `Task.Run` ×4 for real parallelism, and an `internal int MigrationRuns` counter on `PluginDatabase` as the observable for "once". Deliberate mutant: `OpenAsync` calls `MigrateAsync()` directly instead of awaiting the shared `Lazy<Task>` -> `Assert.Equal() Failure / Expected: 1 / Actual:   4` (1 failed). Code restored exactly, test green again.
- green: production change limited to the counter seam (`Interlocked.Increment` in `MigrateAsync`); the `Lazy<Task>` from cycle 19 already provides the behaviour. Suite -> 39 passed, 0 failed
- refactor: none needed
- commit: `023dbbc`

## Cycle 22: U22 upserting an artist by `artist_key` keeps its `id` and updates name, mbid, `library_ids`, `album_count`

- test: `tests/Jellyfin.Plugin.NewReleases.Tests/Storage/ArtistRepositoryTests.cs::UpsertAsync_SameKeyKeepsIdAndUpdatesFields` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~ArtistRepositoryTests.UpsertAsync_SameKeyKeepsIdAndUpdatesFields" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `System.NotImplementedException : The method or operation is not implemented.` (1 failed; stub repository). Declarations added for compilation: `Model/Enums.cs` (OwnershipState, DecisionKind, FetchOutcome, MatchStatus), `Library/LibrarySnapshot.cs` records, `Model/StoredRecords.cs` `LibraryArtist`, `tests/Support/TestDatabase.cs` (T013 helper, temp SQLite + repositories).
- green: `ArtistRepository.UpsertAsync` = `INSERT … ON CONFLICT (artist_key) DO UPDATE … RETURNING id`; `GetAllAsync` reads rows back. Suite -> 40 passed, 0 failed
- refactor: none needed
- commit: `2e08b54`

## Cycle 23: U23 deleting artists absent from the snapshot removes their `artist_source`, `release`, `source_entry`, `edition` rows

- test: `Storage/ArtistRepositoryTests.cs::DeleteMissingAsync_RemovesArtistsAbsentFromTheSnapshotWithTheirRows` (new; dependent rows seeded with raw SQL via `TestDatabase.ExecuteAsync`)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~ArtistRepositoryTests.DeleteMissingAsync_RemovesArtistsAbsentFromTheSnapshotWithTheirRows" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Single() Failure: The collection contained 2 items` (1 failed; no-op stub)
- green: `ArtistRepository.DeleteMissingAsync` = `DELETE FROM library_artist WHERE artist_key NOT IN (json_each(@keys))`; cascades come from the schema (`Foreign Keys=True` in the connection string). Suite -> 41 passed, 0 failed
- refactor: none needed
- commit: `86e2522`

## Cycle 24: U24 rotation returns NULL `last_refreshed_at` first, then oldest first, ties by name

- test: `Storage/ArtistRepositoryTests.cs::GetRotationAsync_NeverRefreshedFirstThenOldestThenName` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~ArtistRepositoryTests.GetRotationAsync_NeverRefreshedFirstThenOldestThenName" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: Collections differ / Expected: ["Amy Never", "Zed Never", "Old", "Recent"] / Actual: ["Amy Never", "Old", "Recent", "Zed Never"]` (1 failed; stub delegated to name order)
- green: `GetRotationAsync` = `ORDER BY last_refreshed_at IS NOT NULL, last_refreshed_at, name`. Suite -> 42 passed, 0 failed
- refactor: `GetAllAsync`/`GetRotationAsync` share `QueryArtistsAsync(orderBy)`; suite re-run green
- commit: `b302949`

## Cycle 25: U25 `artist_source` upsert stores status, source artist id, reason, outcome, `resume_offset`; Complete resets the offset

- test: `Storage/ArtistRepositoryTests.cs::ArtistSource_UpsertStoresMatchAndOutcome_CompleteResetsOffset` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~ArtistRepositoryTests.ArtistSource_UpsertStoresMatchAndOutcome_CompleteResetsOffset" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `System.NotImplementedException : The method or operation is not implemented.` (1 failed; stubs). Declarations added: `Model/SourceRecords.cs` `ArtistMatch`, `ArtistSourceState` record.
- green: `ArtistRepository.SetMatchAsync` / `SetFetchOutcomeAsync` (upserts on the pair PK; Complete -> offset 0 and `last_complete_at = now`) / `GetSourceStateAsync`. Suite -> 43 passed, 0 failed
- refactor: none needed
- commit: `7102e7b`

## Cycle 26: U26 admin counts return total library artists, matched per source, and the unmatched list with reasons

- test: `Storage/ArtistRepositoryTests.cs::GetCountsAsync_ReportsTotalsMatchedPerSourceAndUnmatchedReasons` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~ArtistRepositoryTests.GetCountsAsync_ReportsTotalsMatchedPerSourceAndUnmatchedReasons" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `System.NotImplementedException : The method or operation is not implemented.` (1 failed; stub). Declarations added: `ArtistCounts`, `UnmatchedArtist`, `UnmatchedAt` records.
- green: `ArtistRepository.GetCountsAsync` — COUNT(*), GROUP BY source on Matched, and a joined Unmatched query folded per artist. First suite run after the implementation still failed on this test: the assertion compared `UnmatchedArtist` records whose `Sources` list property compares by reference (test defect, same expected values). Test fixed to compare flattened (artist, source, reason) tuples; no assertion dropped. Suite -> 44 passed, 0 failed
- refactor: none needed
- commit: `a17bdca`

## Cycle 27: U27 the same normalized title from two sources yields one `release` with two `source_entry` rows

- test: `tests/Jellyfin.Plugin.NewReleases.Tests/Storage/ReleaseRepositoryTests.cs::UpsertFromSourceAsync_SameNormalizedTitleFromTwoSources_OneReleaseTwoEntries` (new; `Discovery` vs `DISCOVERY (Deluxe Edition)`)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~ReleaseRepositoryTests.UpsertFromSourceAsync_SameNormalizedTitleFromTwoSources_OneReleaseTwoEntries" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `System.NotImplementedException : The method or operation is not implemented.` (1 failed; stub). Declarations added: `CatalogueItem`, `CataloguePage`, `EditionTrackList`, `Release` records; `TestDatabase.Releases`.
- green: `ReleaseRepository.UpsertFromSourceAsync` — release upsert on `(library_artist_id, normalized_title)` with `RETURNING id`, then `source_entry` upsert on `(release_id, source)` inside one transaction; `GetAsync` reads a release row. Suite -> 45 passed, 0 failed
- refactor: none needed
- commit: `d02ceba`

## Cycle 28: U28 when a MusicBrainz entry exists, canonical source, id, types and date come from it

- test: `Storage/ReleaseRepositoryTests.cs::UpsertFromSourceAsync_MusicBrainzEntryIsCanonicalForSourceIdTypesAndDate` (new; Deezer upserted first, MusicBrainz second)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~ReleaseRepositoryTests.UpsertFromSourceAsync_MusicBrainzEntryIsCanonicalForSourceIdTypesAndDate" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Expected: Tuple ("musicbrainz", "rg-9", Album, "2001-10-02") / Actual:   Tuple ("deezer", "dz-9", Album, "2001-10-01")` (1 failed)
- green: `ReleaseRepository.RecomputeCanonicalAsync` (`UPDATE … FROM` the preferred entry, MusicBrainz first) called after every entry upsert. Suite -> 46 passed, 0 failed
- refactor: none needed
- commit: `5c9f260`

## Cycle 29: U29 removing the MusicBrainz entry makes the Deezer entry canonical

- test: `Storage/ReleaseRepositoryTests.cs::PruneEntriesAsync_RemovingTheMusicBrainzEntryMakesDeezerCanonical` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~ReleaseRepositoryTests.PruneEntriesAsync_RemovingTheMusicBrainzEntryMakesDeezerCanonical" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Expected: Tuple ("deezer", "dz-2", "1997-01-17") / Actual:   Tuple ("musicbrainz", "rg-2", "1997-01-20")` (1 failed; no-op stub)
- green: `ReleaseRepository.PruneEntriesAsync` deletes the pair's entries with `last_seen_run_id < @runId` (`RETURNING release_id`) and recomputes the canonical entry of each touched release. Suite -> 47 passed, 0 failed
- refactor: none needed
- commit: `b4e8a6a`

## Cycle 30: U30 a MusicBrainz entry without a date takes the Deezer entry's date

- test: `Storage/ReleaseRepositoryTests.cs::UpsertFromSourceAsync_MusicBrainzWithoutDateTakesDeezerDate` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~ReleaseRepositoryTests.UpsertFromSourceAsync_MusicBrainzWithoutDateTakesDeezerDate" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Expected: Tuple ("musicbrainz", "2005-03-14") / Actual:   Tuple ("musicbrainz", null)` (1 failed)
- green: `RecomputeCanonicalAsync` sets `release_date = COALESCE(canonical.source_date, any dated entry)`. Suite -> 48 passed, 0 failed
- refactor: none needed
- commit: `4c427ce`

## Cycle 31: U31 pruning (artist, source, run) deletes only that pair's stale entries

- test: `Storage/ReleaseRepositoryTests.cs::PruneEntriesAsync_DeletesOnlyThePairsStaleEntries` (new; other artist and other source untouched)
- red: passed on first run (cycle 29's query already scoped both). Deliberate mutants: (A) artist scope removed -> `Actual: ["deezer:dz-s", "musicbrainz:rg-f"]` (other artist's entry lost); (B) source scope removed -> `Actual: ["musicbrainz:rg-e", "musicbrainz:rg-f"]` (Deezer entry lost). Both 1 failed. Code restored exactly (`git diff` empty), test green again.
- green: no production change. Suite -> 49 passed, 0 failed
- refactor: none needed
- commit: `c298a15`

## Cycle 32: U32 a release left with zero entries after pruning is deleted; one with a remaining entry stays

- test: `Storage/ReleaseRepositoryTests.cs::PruneEntriesAsync_DeletesReleasesLeftWithoutEntriesAndKeepsTheOthers` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~ReleaseRepositoryTests.PruneEntriesAsync_DeletesReleasesLeftWithoutEntriesAndKeepsTheOthers" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Null() Failure: Value is not null / Expected: null / Actual:   Release { Id = 1, … Title = Orphan …}` (1 failed)
- green: `PruneEntriesAsync` deletes the artist's releases with no remaining `source_entry` before recomputing canonicals. Suite -> 50 passed, 0 failed
- refactor: none needed
- commit: `cf7f3b2`

## Cycle 33: U33 `date_sort` is `2024-00-00` for `2024`, `2024-05-00` for `2024-05`, the full date for a full date, NULL for none

- test: `Storage/ReleaseRepositoryTests.cs::UpsertFromSourceAsync_PadsDateSortAndLeavesUndatedNull` (new, Theory ×4)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~ReleaseRepositoryTests.UpsertFromSourceAsync_PadsDateSortAndLeavesUndatedNull" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: Strings differ / Expected: "2024-00-00" / Actual:   null` (3 failed, 1 passed)
- green: `RecomputeCanonicalAsync` pads `date_sort` by `length(release_date)` (4 -> `-00-00`, 7 -> `-00`, else as is; NULL stays NULL). Suite -> 54 passed, 0 failed
- refactor: none needed
- commit: `ff14519`

## Cycle 34: U34 list order is `date_sort` descending, undated last, title tiebreak; year-only 2024 after `2024-01-01`

- test: `Storage/ReleaseRepositoryTests.cs::ListAsync_OrdersByDateDescendingUndatedLastTitleTiebreak_YearOnlyAfterDated` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~ReleaseRepositoryTests.ListAsync_OrdersByDateDescendingUndatedLastTitleTiebreak_YearOnlyAfterDated" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `System.NotImplementedException : The method or operation is not implemented.` (1 failed; stub). Declarations added: `ListState`, `Decision`, `ReleaseFilter`, `ListedRelease`, `SourceLink`, `ComparedEdition`.
- green: `ReleaseRepository.ListAsync` joined query (`artist`, compared `edition`, `decision`, `json_group_array` of source links) with `ORDER BY date_sort IS NULL, date_sort DESC, title`; `ReadListed` maps a row incl. display type. Suite -> 55 passed, 0 failed
- refactor: none needed
- commit: `e49b16d`

## Cycle 35: U35 releases with ownership `Owned` are never returned by the list

- test: `Storage/ReleaseRepositoryTests.cs::ListAsync_NeverReturnsOwnedReleases` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~ReleaseRepositoryTests.ListAsync_NeverReturnsOwnedReleases" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: Collections differ / Expected: ["Missing One"] / Actual: ["Missing One", "In Library"]` (1 failed)
- green: `ListAsync` adds `WHERE r.ownership_state <> 'Owned'`. Suite -> 56 passed, 0 failed
- refactor: none needed
- commit: `3383f83`

## Cycle 36: U36 the list applies the enabled type set at read time (Album+Live absent by default, present as `Live` when enabled)

- test: `Storage/ReleaseRepositoryTests.cs::ListAsync_AppliesTheEnabledTypeSetAtReadTime` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~ReleaseRepositoryTests.ListAsync_AppliesTheEnabledTypeSetAtReadTime" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: Collections differ / Expected: ["Studio"] / Actual: ["Live Set", "Studio"]` (1 failed)
- green: `ListAsync` skips rows where `ReleaseTypeMapper.IsIncluded(primary, secondaries, filter.EnabledTypes)` is false. Suite -> 57 passed, 0 failed
- refactor: `ReadListed` now receives the parsed types instead of re-parsing them; suite re-run green
- commit: `2c4fe4c`

## Cycle 37: U37 with `ReleasedSince=2020-01-01`, `2020-01-01` is returned, `2019-12-31` is not, undated is returned

- test: `Storage/ReleaseRepositoryTests.cs::ListAsync_ReleasedSinceIsInclusiveAndKeepsUndatedRows` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~ReleaseRepositoryTests.ListAsync_ReleasedSinceIsInclusiveAndKeepsUndatedRows" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Expected: ["On The Day", "Undated"] / Actual: ["On The Day", "Day Before", "Undated"]` (1 failed)
- green: `ListAsync` drops dated rows whose `date_sort` sorts before the cutoff (ordinal string compare on `yyyy-MM-dd`); undated rows pass. Suite -> 58 passed, 0 failed
- refactor: none needed
- commit: `702c689`

## Cycle 38: U38 filters — `artistId`, `type` (display type), `state` (Upcoming = `date_sort > today`), inclusive `from`/`to` excluding undated rows

- test: `Storage/ReleaseRepositoryTests.cs::ListAsync_FiltersByArtistTypeStateAndInclusiveDateRange` (new; seven assertions on one seeded set, one per filter)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~ReleaseRepositoryTests.ListAsync_FiltersByArtistTypeStateAndInclusiveDateRange" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: Collections differ / Expected: [···, "Missing Album", "Incomplete Album", "Undated"] / Actual: [···, "Other Artist Album", "Missing Album", "Incomplete Album", ···]` (1 failed; artist filter ignored)
- green: `ListAsync` — `@artist` parameter in SQL; in C#: from/to on `date_sort` (undated skipped when a bound is set), `State` = Upcoming when `date_sort > today` else ownership, then `Type`/`State` equality filters. Suite -> 59 passed, 0 failed
- refactor: `ReadListed` takes `today` and computes the state once; suite re-run green
- commit: `54f5cbc`

## Cycle 39: U39 `archived=false` omits rows with a decision; `archived=true` returns only those, with kind and decided-at

- test: `Storage/ReleaseRepositoryTests.cs::ListAsync_ArchivedFlagSplitsDecidedRowsFromTheList` (new; decision seeded by SQL)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~ReleaseRepositoryTests.ListAsync_ArchivedFlagSplitsDecidedRowsFromTheList" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Expected: ["Kept"] / Actual: ["Ignored Album", "Kept"]` (1 failed)
- green: `ListAsync` adds `AND ((d.kind IS NOT NULL) = @archived)` on the LEFT JOIN to `decision`. Suite -> 60 passed, 0 failed
- refactor: none needed
- commit: `6df737f`

## Cycle 40: U40 the list returns at most 5 000 rows

- test: `Storage/ReleaseRepositoryTests.cs::ListAsync_ReturnsAtMostFiveThousandRows` (new; 5 001 rows seeded with a recursive CTE)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~ReleaseRepositoryTests.ListAsync_ReturnsAtMostFiveThousandRows" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: Values differ / Expected: 5000 / Actual:   5001` (1 failed)
- green: `ReleaseRepository.MaxListRows = 5_000`; `ListAsync` stops reading once the cap is reached (after the read-time filters, so the cap counts listed rows). Suite -> 61 passed, 0 failed
- refactor: none needed
- commit: `c380143`

## Cycle 41: U41 edition upsert is unique on (source, source edition id); ownership write stores state, method, album, edition, missing tracks

- test: `Storage/ReleaseRepositoryTests.cs::Editions_UpsertIsUniquePerSourceEditionId_AndOwnershipColumnsAreStored` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~ReleaseRepositoryTests.Editions_UpsertIsUniquePerSourceEditionId_AndOwnershipColumnsAreStored" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `System.NotImplementedException : The method or operation is not implemented.` (1 failed; stubs). Declarations added: `Edition` record, `Model/OwnershipResult.cs`.
- green: `ReleaseRepository.UpsertEditionAsync` (`ON CONFLICT (source, source_edition_id) … RETURNING id`), `GetEditionsAsync`, `WriteOwnershipAsync`. Suite -> 62 passed, 0 failed
- refactor: none needed
- commit: `0264e18`

## Cycle 42: U42 purge empties `release`, `source_entry`, `edition` and leaves `decision`, `library_artist`, `artist_source`

- test: `Storage/ReleaseRepositoryTests.cs::PurgeAsync_EmptiesReleaseDataAndKeepsDecisionsArtistsAndSourceState` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~ReleaseRepositoryTests.PurgeAsync_EmptiesReleaseDataAndKeepsDecisionsArtistsAndSourceState" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: Values differ / Expected: 0 / Actual:   1` (1 failed; no-op stub)
- green: `ReleaseRepository.PurgeAsync` = `DELETE FROM edition; DELETE FROM source_entry; DELETE FROM release;`. Suite -> 63 passed, 0 failed
- refactor: none needed
- commit: `cac1786`

## Cycle 43: U43 listing 500 stored releases completes within budget (target 500 ms, asserted 2 000 ms)

- test: `Storage/ReleaseRepositoryTests.cs::ListAsync_FiveHundredStoredReleases_ListsWithinBudget` (new; writes the measured time to the test output)
- red: passed on first run (a performance ceiling on existing code). Deliberate mutant: `await Task.Delay(2_500)` at the top of `ListAsync` -> `Assert.InRange() Failure: Value not in range / Range:  (0 - 2000) / Actual: 2526` (1 failed). Code restored exactly (`git diff` empty). Measured on this machine after restore: `ListAsync with 500 releases: 3 ms`.
- green: no production change. Suite -> 64 passed, 0 failed
- refactor: none needed
- commit: `e83e8cd`

## Cycle 44: U44 setting a decision inserts it; setting again for the same key replaces kind, user and time

- test: `tests/Jellyfin.Plugin.NewReleases.Tests/Storage/ArchiveRepositoryTests.cs::SetAsync_InsertsThenReplacesKindUserAndTime` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~ArchiveRepositoryTests.SetAsync_InsertsThenReplacesKindUserAndTime" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `System.NotImplementedException : The method or operation is not implemented.` (1 failed; stub). `TestDatabase.Archive` added.
- green: `ArchiveRepository.SetAsync` (upsert on the natural-key PK) and `GetAsync`. Suite -> 65 passed, 0 failed
- refactor: none needed
- commit: `b7411ca`
