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

## Cycle 45: U45 removing a decision deletes only that key; clearing deletes all

- test: `Storage/ArchiveRepositoryTests.cs::RemoveAsync_DeletesOnlyThatKey_ClearAsync_DeletesAll` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~ArchiveRepositoryTests.RemoveAsync_DeletesOnlyThatKey_ClearAsync_DeletesAll" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Null() Failure: Value is not null / Expected: null / Actual:   Decision { ArtistKey = name:a, NormalizedTitle = one, … }` (1 failed; no-op stubs)
- green: `ArchiveRepository.RemoveAsync` (keyed DELETE) and `ClearAsync` (DELETE all). Suite -> 66 passed, 0 failed
- refactor: none needed
- commit: `51eb3cd`

## Cycle 46: U46 a decision written before a purge still joins the release once the same title is upserted again

- test: `Storage/ArchiveRepositoryTests.cs::Decision_SurvivesPurgeAndJoinsTheReleaseWhenItIsFetchedAgain` (new; purge, refetch with a new surrogate id, list vs Archive)
- red: passed on first run (natural-key join from cycle 39 plus purge scope from cycle 42). Deliberate mutant: `PurgeAsync` also `DELETE FROM decision` -> `Assert.Empty() Failure: Collection was not empty` (1 failed). Code restored exactly (`git diff` empty), test green again.
- green: no production change. Suite -> 67 passed, 0 failed
- refactor: none needed
- commit: `534f5a4`

## Cycle 47: U47 recording a call increments `calls_today`; when the stored day is not today (UTC) the counter restarts at 1

- test: `tests/Jellyfin.Plugin.NewReleases.Tests/Storage/SourceStateRepositoryTests.cs::RecordCallAsync_IncrementsCallsToday_AndRestartsAtOneOnANewUtcDay` (new; `TimeProviderStub` advanced across midnight)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~SourceStateRepositoryTests.RecordCallAsync_IncrementsCallsToday_AndRestartsAtOneOnANewUtcDay" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `System.NotImplementedException : The method or operation is not implemented.` (1 failed; stub). Declarations added: `SourceState`, `RefreshRun` records; `TestDatabase.SourceState` (clock-aware).
- green: `SourceStateRepository.RecordCallAsync` upsert with `CASE WHEN calls_day = today THEN calls_today + 1 ELSE 1`; `GetAsync` reads the row. Suite -> 68 passed, 0 failed
- refactor: none needed
- commit: `4c65ed3`

## Cycle 48: U48 remaining budget is budget minus calls today, never below 0

- test: `Storage/SourceStateRepositoryTests.cs::GetRemainingBudgetAsync_IsBudgetMinusCallsToday_NeverBelowZero` (new; includes the day rollover)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~SourceStateRepositoryTests.GetRemainingBudgetAsync_IsBudgetMinusCallsToday_NeverBelowZero" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: Values differ / Expected: 0 / Actual:   3` (1 failed; stub returned the full budget)
- green: `SourceStateRepository.GetRemainingBudgetAsync` = `max(0, budget − (calls_day == today ? calls_today : 0))`. Suite -> 69 passed, 0 failed
- refactor: none needed
- commit: `18ec5f4`

## Cycle 49: U49 four consecutive failures set no cooldown; the fifth sets `cooldown_until = now + 6 h`

- test: `Storage/SourceStateRepositoryTests.cs::RecordFailureAsync_FourFailuresNoCooldown_FifthSetsCooldownSixHoursFromNow` (new)
- red: first run against the no-op stub died with `System.NullReferenceException` (no row to read); test tightened with `Assert.NotNull` before dereferencing (same expectations), re-run:
  `dotnet test --configuration Release --filter "FullyQualifiedName~SourceStateRepositoryTests.RecordFailureAsync_FourFailuresNoCooldown_FifthSetsCooldownSixHoursFromNow" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.NotNull() Failure: Value is null` (1 failed)
- green: `SourceStateRepository.RecordFailureAsync` upsert incrementing `consecutive_failures`, setting `cooldown_until` when the new count reaches the threshold, storing `last_error`. Suite -> 70 passed, 0 failed
- refactor: none needed
- commit: `2f2bffd`

## Cycle 50: U50 a success resets consecutive failures to 0 and clears the cooldown

- test: `Storage/SourceStateRepositoryTests.cs::RecordSuccessAsync_ResetsFailuresClearsCooldownAndStampsLastSuccess` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~SourceStateRepositoryTests.RecordSuccessAsync_ResetsFailuresClearsCooldownAndStampsLastSuccess" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Expected: Tuple (0, null, 2026-09-06T23:59:00…) / Actual:   Tuple (5, 2026-09-07T05:59:00…, null)` (1 failed; no-op stub)
- green: `SourceStateRepository.RecordSuccessAsync` upsert resetting failures, clearing `cooldown_until`, stamping `last_success_at`. Suite -> 71 passed, 0 failed
- refactor: none needed
- commit: `b1ff768`

## Cycle 51: U51 in-cooldown is true one second before `cooldown_until` and false at it

- test: `Storage/SourceStateRepositoryTests.cs::IsInCooldownAsync_TrueOneSecondBeforeCooldownUntil_FalseAtIt` (new; both sides of the boundary via `TimeProviderStub.Set`)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~SourceStateRepositoryTests.IsInCooldownAsync_TrueOneSecondBeforeCooldownUntil_FalseAtIt" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.True() Failure / Expected: True / Actual:   False` (1 failed; stub returned false)
- green: `SourceStateRepository.IsInCooldownAsync` = `cooldown_until > now`. Suite -> 72 passed, 0 failed
- refactor: none needed
- commit: `8e40b90`

## Cycle 52: U52 runs are started and finished with counts and outcome; latest completed run and latest run are read back

- test: `Storage/SourceStateRepositoryTests.cs::Runs_StartAndFinishWithCounts_LatestCompletedAndLatestAreReadBack` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~SourceStateRepositoryTests.Runs_StartAndFinishWithCounts_LatestCompletedAndLatestAreReadBack" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `System.NotImplementedException : The method or operation is not implemented.` (1 failed; stubs)
- green: `SourceStateRepository.StartRunAsync` (`RETURNING id`), `FinishRunAsync`, `GetLastCompletedRunAsync` (`outcome = 'Completed'`, latest `ended_at`), `GetLatestRunAsync` via one `QueryRunAsync(tail)`. Suite -> 73 passed, 0 failed
- refactor: none needed
- commit: `182d498`

## Cycle 53: A9 a new `PluginConfiguration` has both sources enabled and exactly Album and EP included

- test: `tests/Jellyfin.Plugin.NewReleases.Tests/Configuration/PluginConfigurationTests.cs::Defaults_BothSourcesEnabled_ExactlyAlbumsAndEpsIncluded_NoCutoffNoContact` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~PluginConfigurationTests.Defaults_BothSourcesEnabled_ExactlyAlbumsAndEpsIncluded_NoCutoffNoContact" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.True() Failure / Expected: True / Actual:   False` (1 failed; the eleven scalar properties were declared without initializers)
- green: `PluginConfiguration` initializers `= true` on `MusicBrainzEnabled`, `DeezerEnabled`, `IncludeAlbums`, `IncludeEps`. Suite -> 74 passed, 0 failed
- refactor: none needed
- commit: `205293b`

## Cycle 54: A10 a fully changed configuration serialized with `XmlSerializer` and read back is equal field by field

- test: `Configuration/PluginConfigurationTests.cs::XmlRoundTrip_FullyChangedConfiguration_IsEqualFieldByField` (new)
- red: passed on first run (scalar auto-properties round-trip by construction). Deliberate mutant: `[XmlIgnore]` on `ReleasedSince` -> `Expected: Tuple (…, "2020-01-01", "admin@example.org") / Actual:   Tuple (…, "", "admin@example.org")` (1 failed). Code restored exactly (`git diff` empty), test green again.
- green: no production change. Suite -> 75 passed, 0 failed
- refactor: none needed
- commit: `056c563`

## Cycle 55: U53 `EnabledReleaseTypes()` is {Album, EP} by default and reflects each toggled flag

- test: `Configuration/PluginConfigurationTests.cs::EnabledReleaseTypes_AlbumAndEpByDefault_ReflectsEachToggle` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~PluginConfigurationTests.EnabledReleaseTypes_AlbumAndEpByDefault_ReflectsEachToggle" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: HashSets differ / Expected: [Album, EP] / Actual:   []` (1 failed; stub returned an empty set)
- green: `PluginConfiguration.EnabledReleaseTypes()` maps the seven flags. Suite -> 76 passed, 0 failed
- refactor: none needed
- commit: `d29dbc7`

## Cycle 56: U54 `ReleasedSinceDate()` parses `2020-01-01`; empty and `yesterday` give null

- test: `Configuration/PluginConfigurationTests.cs::ReleasedSinceDate_ParsesIsoDate_EmptyOrInvalidIsNull` (new, Theory ×3)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~PluginConfigurationTests.ReleasedSinceDate_ParsesIsoDate_EmptyOrInvalidIsNull" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: Values differ / Expected: 1/1/2020 / Actual:   null` (1 failed, 2 passed; stub returned null)
- green: `PluginConfiguration.ReleasedSinceDate()` = `DateOnly.TryParseExact("yyyy-MM-dd")`. Suite -> 79 passed, 0 failed
- refactor: none needed
- commit: `2a997d9`

## Cycle 57: U55 deserializing XML that lacks every element yields the defaults (forward migration)

- test: `Configuration/PluginConfigurationTests.cs::Deserialize_XmlWithoutAnyElement_YieldsTheDefaults` (new)
- red: passed on first run. First mutant check (constructor sets `IncludeEps = false`) **survived** because the test compared against `new PluginConfiguration()`, which carried the same mutation — a tautology. Test rewritten to assert the contract's literal defaults; mutant re-run -> `Expected: Tuple (True, True, True, True, False, …) / Actual:   Tuple (True, True, True, False, False, …)` (1 failed). Code restored exactly (`git diff` empty), test green again.
- green: no production change. Suite -> 80 passed, 0 failed
- refactor: none needed
- commit: `1980c13`

## Cycle 58: U56 without a contact the User-Agent is `JellyfinNewReleases/<version>`

- test: `tests/Jellyfin.Plugin.NewReleases.Tests/Sources/UserAgentBuilderTests.cs::Build_WithoutContact_IsProductAndVersion` (new, Theory: null, empty, blank contact)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~UserAgentBuilderTests.Build_WithoutContact_IsProductAndVersion" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: Strings differ / Expected: "JellyfinNewReleases/0.1.0" / Actual:   "JellyfinNewReleases"` (3 failed)
- green: `UserAgentBuilder.Build` = `$"{ProductName}/{version}"`. Suite -> 83 passed, 0 failed
- refactor: none needed
- commit: `e56d37b`

## Cycle 59: U57 with contact ` me@example.org ` the User-Agent is `JellyfinNewReleases/<version> ( me@example.org )`

- test: `Sources/UserAgentBuilderTests.cs::Build_WithContact_AppendsTrimmedContactInParentheses` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~UserAgentBuilderTests.Build_WithContact_AppendsTrimmedContactInParentheses" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Expected: ···"lyfinNewReleases/0.1.0 ( me@example.org )" / Actual:   "JellyfinNewReleases/0.1.0"` (1 failed)
- green: `UserAgentBuilder.Build` appends ` ( <trimmed contact> )` when non-blank. `Sources/SourceLimits.cs` constants (R10) added in the same commit: the non-behavioural half of T075 that the HTTP client cycles need next. Suite -> 84 passed, 0 failed
- refactor: none needed
- commit: `4c9ec03`

## Cycle 60: U58 every request carries the User-Agent from `UserAgentBuilder`

- test: `tests/Jellyfin.Plugin.NewReleases.Tests/Sources/SourceHttpClientTests.cs::GetStringAsync_EveryRequestCarriesThePluginUserAgent` (new; `StubHttpMessageHandler`, `TestDatabase`, `TimeProviderStub`, configuration delegate)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~SourceHttpClientTests.GetStringAsync_EveryRequestCarriesThePluginUserAgent" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: Strings differ / Expected: "JellyfinNewReleases/0.1.0 ( ops@example.o"··· / Actual:   ""` (1 failed; skeleton sent a bare `GetAsync`)
- green: `Sources/SourceHttpClient.cs` builds an `HttpRequestMessage` with `User-Agent = UserAgentBuilder.Build(Version, configuration.UserAgentContact)`; version read from the assembly; configuration through an injectable delegate defaulting to `Plugin.Instance`. Suite -> 85 passed, 0 failed
- refactor: none needed
- commit: `8e35979`

## Cycle 61: U59 each HTTP request records one call in `source_state` for that source

- test: `Sources/SourceHttpClientTests.cs::GetStringAsync_RecordsOneCallPerHttpRequestForThatSource` (new)
- red: first run died with `System.NullReferenceException` (no `source_state` row); test tightened with `Assert.NotNull` (same expectations), re-run:
  `dotnet test --configuration Release --filter "FullyQualifiedName~SourceHttpClientTests.GetStringAsync_RecordsOneCallPerHttpRequestForThatSource" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.NotNull() Failure: Value is null` (1 failed)
- green: `SourceHttpClient.GetStringAsync` calls `SourceStateRepository.RecordCallAsync(source)` before each send. Suite -> 86 passed, 0 failed
- refactor: none needed
- commit: `24482c0`

## Cycle 62: U60 with 1 call of budget left the request is sent; with 0 left `DailyBudgetExhaustedException` is thrown and nothing is sent

- test: `Sources/SourceHttpClientTests.cs::GetStringAsync_LastUnitOfBudgetIsSent_ExhaustedBudgetThrowsWithoutSending` (new; budget seeded with one SQL row after a first version spent it with 19 999 repository calls in 6 s)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~SourceHttpClientTests.GetStringAsync_LastUnitOfBudgetIsSent_ExhaustedBudgetThrowsWithoutSending" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Throws() Failure: No exception was thrown / Expected: typeof(…DailyBudgetExhaustedException)` (1 failed). `DailyBudgetExhaustedException` declared for compilation.
- green: `GetStringAsync` checks `GetRemainingBudgetAsync(source, SourceLimits.BySource[source].DailyBudget) <= 0` before building the request. Suite -> 87 passed, 0 failed
- refactor: none needed
- commit: `4b3760c`

## Cycle 63: U61 a 503 with `Retry-After: 2` sets `next_allowed_at = now + 2 s`, waits, retries, and returns the later 200 body

- test: `Sources/SourceHttpClientTests.cs::GetStringAsync_503WithRetryAfter_SetsNextAllowedAtWaitsRetriesAndReturnsTheLaterBody` (new; the pending task is observed before the stub clock is advanced)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~SourceHttpClientTests.GetStringAsync_503WithRetryAfter_SetsNextAllowedAtWaitsRetriesAndReturnsTheLaterBody" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.False() Failure / Expected: False / Actual:   True` (1 failed; the client returned the 503 body immediately)
- green: `GetStringAsync` retry loop — non-success: `Retry-After` (delta or date) becomes the delay and is persisted via new `SourceStateRepository.SetNextAllowedAtAsync`; `Task.Delay(delay, clock)`; each attempt first waits for a stored `next_allowed_at` floor. Suite -> 88 passed, 0 failed
- refactor: none needed
- commit: `7a7073b`

## Cycle 64: U62 persistent 500s are retried three times (200 ms, 800 ms, 3 200 ms) then surface as `HttpRequestException`

- test: `Sources/SourceHttpClientTests.cs::GetStringAsync_Persistent500_RetriesThreeTimesWithBackoffThenThrows` (new; asserts the four send instants on the stub clock)
- red: the first run hung — cycle 63's loop had no attempt bound, so the client retried for as long as the clock advanced and then waited forever; the run was killed. The clock helper now fails with `WaitAsync(2 s)` instead of hanging. Re-run:
  `dotnet test --configuration Release --filter "FullyQualifiedName~SourceHttpClientTests.GetStringAsync_Persistent500_RetriesThreeTimesWithBackoffThenThrows" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Throws() Failure: Exception type was not an exact match / Expected: typeof(System.Net.Http.HttpRequestException) / Actual:   typeof(System.TimeoutException)` (1 failed)
- green: `GetStringAsync` throws `HttpRequestException` (with the status code) once `attempt >= RetryBackoffs.Length`; backoff for attempt *n* is `RetryBackoffs[n]`. Suite -> 89 passed, 0 failed
- refactor: none needed
- commit: `536c702`

## Cycle 65: U63 a 404 or 400 throws immediately with no retry

- test: `Sources/SourceHttpClientTests.cs::GetStringAsync_ClientError_ThrowsImmediatelyWithoutRetry` (new, Theory: 404, 400)
- red: first run failed with `TimeoutException` because the clock helper raced the thread pool (`Task.Yield` did not let the continuation register its next timer); helper changed to a 15 ms poll and cycle 64's test re-verified green. Re-run:
  `dotnet test --configuration Release --filter "FullyQualifiedName~SourceHttpClientTests.GetStringAsync_ClientError_ThrowsImmediatelyWithoutRetry" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Single() Failure: The collection contained 4 items` (2 failed; every non-success was retried)
- green: `GetStringAsync` treats only 429 and 5xx as transient; any other non-success throws `HttpRequestException` with the status before the retry bound. Suite -> 91 passed, 0 failed
- refactor: none needed
- commit: `4206a1b`

## Cycle 66: U64 a failed request increments consecutive failures; a later success resets them

- test: `Sources/SourceHttpClientTests.cs::GetStringAsync_FailureIncrementsConsecutiveFailures_LaterSuccessResetsThem` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~SourceHttpClientTests.GetStringAsync_FailureIncrementsConsecutiveFailures_LaterSuccessResetsThem" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: Values differ / Expected: 2 / Actual:   0` (1 failed)
- green: `GetStringAsync` calls `RecordSuccessAsync` on 2xx and `RecordFailureAsync(threshold 5, cooldown 6 h)` before throwing on a final failure. Suite -> 92 passed, 0 failed
- refactor: the two throw sites merged into one failure branch with a transient-aware message; suite re-run green
- commit: `c2f8c6e`

## Cycle 67: U65 `IsAvailableAsync` is false during cooldown or before `next_allowed_at`, true otherwise

- test: `Sources/SourceHttpClientTests.cs::IsAvailableAsync_FalseDuringCooldownOrBeforeNextAllowedAt_TrueOtherwise` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~SourceHttpClientTests.IsAvailableAsync_FalseDuringCooldownOrBeforeNextAllowedAt_TrueOtherwise" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.False() Failure / Expected: False / Actual:   True` (1 failed; stub returned true)
- green: `SourceHttpClient.IsAvailableAsync` = not (`cooldown_until > now`) and not (`next_allowed_at > now`). Suite -> 93 passed, 0 failed
- refactor: none needed
- commit: `acecf77`

## Cycle 68: U66 an unknown source id throws `ArgumentException`

- test: `Sources/SourceHttpClientTests.cs::GetStringAsync_UnknownSourceId_ThrowsArgumentException` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~SourceHttpClientTests.GetStringAsync_UnknownSourceId_ThrowsArgumentException" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Throws() Failure: Exception type was not an exact match / Expected: typeof(System.ArgumentException) / Actual:   typeof(System.Collections.Generic.KeyNotFoundException)` (1 failed; the dictionary indexer leaked)
- green: `GetStringAsync` uses `SourceLimits.BySource.TryGetValue` and throws `ArgumentException` for an unknown id before any I/O. Suite -> 94 passed, 0 failed
- refactor: none needed
- commit: `75eaeb0`

## Cycle 69: U67 two back-to-back MusicBrainz requests are at least ~1 s apart

- test: `Sources/SourceHttpClientTests.cs::GetStringAsync_TwoMusicBrainzRequests_AreAtLeastOneSecondApart` (new; real wall clock, ~1 s, because `TokenBucketRateLimiter` has no `TimeProvider` seam)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~SourceHttpClientTests.GetStringAsync_TwoMusicBrainzRequests_AreAtLeastOneSecondApart" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.InRange() Failure: Value not in range / Range:  (00:00:00.9000000 - 00:00:05) / Actual: 00:00:00.0086772` (1 failed)
- green: one BCL `TokenBucketRateLimiter` per source (`RequestsPerSecond` tokens per 1 s period, oldest-first queue); a lease is acquired before every attempt; `IAsyncDisposable` disposes the limiters. Suite -> 95 passed, 0 failed
- refactor: none needed
- commit: `79959ef`

## Cycle 70: U68 album artists become library artists; an artist credited only as a featured track artist does not

- test: `tests/Jellyfin.Plugin.NewReleases.Tests/Library/LibraryScannerTests.cs::Scan_AlbumArtistsBecomeLibraryArtists_FeaturedOnlyArtistsDoNot` (new; `tests/Support/LibraryFakes.cs` (T028 helper) builds `MusicArtist`/`MusicAlbum`/`Audio` items behind a substituted `ILibraryManager`)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~LibraryScannerTests.Scan_AlbumArtistsBecomeLibraryArtists_FeaturedOnlyArtistsDoNot" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: Collections differ / Expected: ["Daft Punk"] / Actual: string[] []` (1 failed; stub returned an empty snapshot)
- green: `LibraryScanner.Scan()` lists `MusicArtist` and `MusicAlbum` items once each, groups albums by `AlbumArtists` name and yields one artist per credited name with a `name:<normalized>` key. Suite -> 96 passed, 0 failed
- refactor: none needed
- commit: `abf8954`

## Cycle 71: U69 MBID is read from the artist's `MusicBrainzArtist`, else the album's `MusicBrainzAlbumArtist`; with neither the key is `name:<normalized name>`

- test: `Library/LibraryScannerTests.cs::Scan_MbidFromArtistThenAlbumArtistProviderId_ElseNameKey` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~LibraryScannerTests.Scan_MbidFromArtistThenAlbumArtistProviderId_ElseNameKey" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Expected: [("Daft Punk", "056e4f3e-…", "056e4f3e-…"), ("Justice", "f6ccbf37-…", "f6ccbf37-…"), …] / Actual: [("Daft Punk", null, "name:daft punk"), ("Justice", null, "name:justice"), …]` (1 failed)
- green: `LibraryScanner.ProviderId()` (first comma-separated value) applied to the artist item, then to the artist's albums' `MusicBrainzAlbumArtist`; MBID becomes both `Mbid` and `ArtistKey`. Suite -> 97 passed, 0 failed
- refactor: none needed
- commit: `4e1abd1`

## Cycle 72: U70 each album snapshot carries `MusicBrainzAlbum`, `MusicBrainzReleaseGroup` and the normalized titles of its `Audio` children

- test: `Library/LibraryScannerTests.cs::Scan_AlbumSnapshotsCarryIdentifiersAndNormalizedTrackTitles` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~LibraryScannerTests.Scan_AlbumSnapshotsCarryIdentifiersAndNormalizedTrackTitles" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: Values differ / Expected: 2 / Actual:   0` (1 failed; artists had no albums)
- green: `LibraryScanner.SnapshotAlbum()` — `Audio` children via `GetItemList(ParentId = album.Id)`, track titles through `NormalizeTrack`, album title through `NormalizeAlbum`, both MusicBrainz provider ids. Suite -> 98 passed, 0 failed
- refactor: none needed
- commit: `8c1b0a8`

## Cycle 73: U71 `LibraryIds` are the distinct collection folder ids of the artist's albums

- test: `Library/LibraryScannerTests.cs::Scan_LibraryIdsAreTheDistinctCollectionFoldersOfTheArtistsAlbums` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~LibraryScannerTests.Scan_LibraryIdsAreTheDistinctCollectionFoldersOfTheArtistsAlbums" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: Collections differ / Expected: [aaaaaaaa-…, bbbbbbbb-…] / Actual: []` (1 failed)
- green: `LibraryScanner` fills `LibraryIds` from `GetCollectionFolders(album)` over the artist's albums, distinct. Suite -> 99 passed, 0 failed
- refactor: none needed
- commit: `e5af959`

## Cycle 74: U72 two `MusicArtist` items sharing an MBID become one library artist holding both albums

- test: `Library/LibraryScannerTests.cs::Scan_TwoArtistItemsSharingAnMbid_BecomeOneLibraryArtistWithBothAlbums` (new)
- red: a first version used names differing only by case and passed at once because grouping by name is already case-insensitive — it did not exercise the identifier merge. Rewritten with distinct names (`Daft Punk`, `Daft Punk (duo)`) sharing one MBID:
  `dotnet test --configuration Release --filter "FullyQualifiedName~LibraryScannerTests.Scan_TwoArtistItemsSharingAnMbid_BecomeOneLibraryArtistWithBothAlbums" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Single() Failure: The collection contained 2 items` (1 failed)
- green: `LibraryScanner.Scan()` groups the per-name artists by `ArtistKey` and merges albums and library ids. Suite -> 100 passed, 0 failed
- refactor: none needed
- commit: `d5a1250`

## Cycle 75: U73 the scanner never calls `ILibraryManager.GetArtist(string)`

- test: `Library/LibraryScannerTests.cs::Scan_NeverCallsGetArtistByName` (new; interaction assertion at the boundary — the call itself is the forbidden side effect, R6)
- red: passed on first run. Deliberate mutant: fall back to `_library.GetArtist(name)` for an album artist without an item -> `NSubstitute.Exceptions.ReceivedCallsException : Expected to receive no calls matching: … Actually received 1 matching call` (1 failed). Code restored exactly (`git diff` empty), test green again.
- green: no production change. Suite -> 101 passed, 0 failed
- refactor: none needed
- commit: `1b10343`

## Cycle 76: U93 an album whose `MusicBrainzReleaseGroup` equals the canonical id is the candidate with method `Identifier`

- test: `tests/Jellyfin.Plugin.NewReleases.Tests/Matching/OwnershipMatcherTests.cs::Decide_AlbumWithTheCanonicalReleaseGroupId_IsTheCandidateByIdentifier` (new; a same-titled decoy album is listed first)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~OwnershipMatcherTests.Decide_AlbumWithTheCanonicalReleaseGroupId_IsTheCandidateByIdentifier" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: Values differ / Expected: Tuple (853cfe6b-…, "Identifier") / Actual:   Tuple (null, null)` (1 failed; stub returned Missing)
- green: `OwnershipMatcher.FindCandidate()` matches `MusicBrainzReleaseGroupId` against the canonical MusicBrainz id; `Decide` compares the first edition's tracks (fake-it for the edition choice, generalized by U100). Suite -> 102 passed, 0 failed
- refactor: none needed
- commit: `842068e`

## Cycle 77: U94 an album whose `MusicBrainzAlbum` equals a stored MusicBrainz edition id is the candidate with method `Identifier`

- test: `Matching/OwnershipMatcherTests.cs::Decide_AlbumWhoseReleaseIdIsAStoredMusicBrainzEdition_IsTheCandidateByIdentifier` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~OwnershipMatcherTests.Decide_AlbumWhoseReleaseIdIsAStoredMusicBrainzEdition_IsTheCandidateByIdentifier" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: Values differ / Expected: Tuple (fb7619d8-…, "Identifier") / Actual:   Tuple (null, null)` (1 failed)
- green: `FindCandidate` also accepts an album whose `MusicBrainzReleaseId` is among the stored MusicBrainz editions' ids. Suite -> 103 passed, 0 failed
- refactor: none needed
- commit: `e886884`

## Cycle 78: U95 with no identifier match, an album titled `Album (Deluxe Edition)` is the candidate for release `Album` with method `Title`

- test: `Matching/OwnershipMatcherTests.cs::Decide_WithoutIdentifierMatch_AlbumTitledWithAnEditionQualifier_IsTheCandidateByTitle` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~OwnershipMatcherTests.Decide_WithoutIdentifierMatch_AlbumTitledWithAnEditionQualifier_IsTheCandidateByTitle" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: Values differ / Expected: Tuple (0c9f1340-…, "Title") / Actual:   Tuple (null, null)` (1 failed)
- green: `FindCandidate` falls back to equal `NormalizedTitle` with method `Title`. Suite -> 104 passed, 0 failed
- refactor: none needed
- commit: `bf4737e`

## Cycle 79: U96 with no candidate the result is `Missing` and no edition fetch is requested

- test: `Matching/OwnershipMatcherTests.cs::Decide_NoCandidate_IsMissingAndAsksForNoEditions` (new)
- red: passed on first run (`Missing` was the stub default from cycle 76). Deliberate mutant: the no-candidate branch returns `NeedsEditions = true` -> `Expected: Tuple (Missing, False, null, null) / Actual:   Tuple (Missing, True, null, null)` (1 failed). Code restored exactly (`git diff` empty), test green again.
- green: no production change. This test was written in the same edit as cycle 80's and is committed with it (below); the suite could not be green in between.
- refactor: none needed

## Cycle 80: U97 with a candidate and no stored editions the result asks for editions instead of deciding

- test: `Matching/OwnershipMatcherTests.cs::Decide_CandidateWithoutStoredEditions_AsksForEditionsInsteadOfDeciding` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~OwnershipMatcherTests.Decide_CandidateWithoutStoredEditions_AsksForEditionsInsteadOfDeciding" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `System.ArgumentOutOfRangeException : Index was out of range.` (1 failed; `editions[0]` on an empty list — the branch did not exist)
- green: `Decide` returns `NeedsEditions: true` with the candidate and method when `editions.Count == 0`. Suite -> 106 passed, 0 failed
- refactor: none needed
- commit: `f4101ef`

## Cycle 81: U98 every edition track matched by a library track → `Owned`

- test: `Matching/OwnershipMatcherTests.cs::Decide_EveryEditionTrackMatchedByALibraryTrack_IsOwned` (new)
- red: passed on first run (cycle 76's comparison). Deliberate mutant: verdict inverted -> `Expected: Tuple (Owned, 7, 0, False) / Actual:   Tuple (Incomplete, 7, 0, False)` (1 failed). Code restored exactly (`git diff` empty), test green again.
- green: no production change. Suite -> 107 passed, 0 failed
- refactor: none needed
- commit: `70fb672`

## Cycle 82: U99 8 of 10 matched → `Incomplete` listing the 2 missing titles, the edition and its source

- test: `Matching/OwnershipMatcherTests.cs::Decide_EightOfTenTracksMatched_IsIncompleteNamingTheTwoMissingTitlesAndTheEdition` (new; the edition's source travels with `EditionId` through the `edition` row)
- red: passed on first run. Deliberate mutant: missing titles replaced by `[]` -> `Assert.Equal() Failure: Collections differ / Expected: ["track 9", "track 10"] / Actual: string[] []` (1 failed). Code restored exactly (`git diff` empty), test green again.
- green: no production change. Suite -> 108 passed, 0 failed
- refactor: none needed
- commit: `8aace0b`

## Cycle 83: U100 edition choice: more matched wins; equal → fewer missing; then MusicBrainz over Deezer; then lowest edition id

- test: `Matching/OwnershipMatcherTests.cs::Decide_EditionChoice_MostMatchedThenFewestMissingThenMusicBrainzThenLowestId` (new; one assertion per tie-break level)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~OwnershipMatcherTests.Decide_EditionChoice_MostMatchedThenFewestMissingThenMusicBrainzThenLowestId" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: Values differ / Expected: 2 / Actual:   1` (1 failed; the first edition was always used)
- green: `Decide` orders editions by matched count desc, missing count asc, MusicBrainz first, `SourceEditionId` ordinal. Suite -> 109 passed, 0 failed
- refactor: none needed
- commit: `c29a1dc`

## Cycle 84: U101 extra library tracks beyond the edition do not prevent `Owned`

- test: `Matching/OwnershipMatcherTests.cs::Decide_ExtraLibraryTracksBeyondTheEdition_DoNotPreventOwned` (new)
- red: passed on first run (missing is edition-minus-library by construction). Deliberate mutant: missing computed as the symmetric difference -> `Expected: Owned / Actual:   Incomplete` (1 failed). Code restored exactly (`git diff` empty), test green again.
- green: no production change. Suite -> 110 passed, 0 failed
- refactor: none needed
- commit: `b530d24`

## Cycle 85: U102 editions with no tracks at all yield `Owned` by album presence

- test: `Matching/OwnershipMatcherTests.cs::Decide_EditionsWithoutAnyTracks_AreOwnedByAlbumPresence` (new)
- red: passed on first run (empty edition ⇒ nothing missing). Deliberate mutant: trackless edition forced to `Incomplete` -> `Expected: Tuple (Owned, 0) / Actual:   Tuple (Incomplete, 0)` (1 failed). Code restored exactly, test green again.
- green: no behavioural change; a `// ponytail:` comment names the ceiling (data-model step 5). Suite -> 111 passed, 0 failed
- refactor: none needed
- commit: `09199a4`

## Cycle 86: U74 an artist snapshot with an MBID is `Matched(mbid)` with zero HTTP requests

- test: `tests/Jellyfin.Plugin.NewReleases.Tests/Sources/MusicBrainzSourceTests.cs::MatchArtistAsync_SnapshotWithMbid_IsMatchedWithoutAnyRequest` (new; `tests/Support/SourceHarness.cs` helper wires stub handler, temp database, stub clock and the real `SourceHttpClient`)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~MusicBrainzSourceTests.MatchArtistAsync_SnapshotWithMbid_IsMatchedWithoutAnyRequest" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `System.NotImplementedException : The method or operation is not implemented.` (1 failed; stub). Declarations added: `Sources/IReleaseSource.cs` (contract), `Sources/MusicBrainzSource.cs` skeleton.
- green: `MusicBrainzSource.MatchArtistAsync` returns `Matched(artist.Mbid)` when the snapshot has one. Suite -> 112 passed, 0 failed
- refactor: none needed
- commit: `ace71f7`

## Cycle 87: U75 top score 85+ with the runner-up far behind → `Matched`

- test: `Sources/MusicBrainzSourceTests.cs::MatchArtistAsync_ConfidentTopResult_IsMatched` (new; recorded `artist_search_confident.json`, 100 vs 66)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~MusicBrainzSourceTests.MatchArtistAsync_ConfidentTopResult_IsMatched" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `System.NotImplementedException : The method or operation is not implemented.` (1 failed)
- green: `MatchArtistAsync` searches `artist?query=artist:"<name>"&limit=5&fmt=json` through `SourceHttpClient` and returns the top result's id (fake-it: the score rules come with U76–U79). Suite -> 113 passed, 0 failed
- refactor: none needed
- commit: `a15687e`

## Cycle 88: U76 top score 90 with the runner-up at 85 (5 points) → `Unmatched` with an "ambiguous" reason

- test: `Sources/MusicBrainzSourceTests.cs::MatchArtistAsync_RunnerUpWithinFivePoints_IsUnmatchedAsAmbiguous` (new; `artist_search_ambiguous.json`, 100 vs 96)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~MusicBrainzSourceTests.MatchArtistAsync_RunnerUpWithinFivePoints_IsUnmatchedAsAmbiguous" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: Values differ / Expected: Unmatched / Actual:   Matched` (1 failed)
- green: `MatchArtistAsync` orders candidates by score and returns `Unmatched("ambiguous (score A vs B)")` when the gap is 5 or less. Suite -> 114 passed, 0 failed
- refactor: none needed
- commit: `3619873`

## Cycle 89: U77 top score 90 with the runner-up at 84 (6 points) → `Matched`

- test: `Sources/MusicBrainzSourceTests.cs::MatchArtistAsync_RunnerUpSixPointsBehind_IsMatched` (new; synthetic search body 90/84)
- red: passed on first run (cycle 88's `<= 5`). Deliberate mutant: gap threshold `<= 6` -> `Expected: Matched / Actual: Unmatched` (1 failed). Code restored exactly (`git diff` empty), test green again.
- green: no production change. Committed together with cycles 90–91 (the three tests were added in one edit).
- refactor: none needed

## Cycle 90: U78 top score 84 → `Unmatched` with a "low score" reason

- test: `Sources/MusicBrainzSourceTests.cs::MatchArtistAsync_TopScoreBelow85_IsUnmatchedAsLowScore` (new; `artist_search_low_score.json`)
- red: `dotnet test … --filter "FullyQualifiedName~MusicBrainzSourceTests.MatchArtistAsync_TopScoreBelow85_IsUnmatchedAsLowScore"` -> `Assert.Equal() Failure: Values differ / Expected: Unmatched / Actual:   Matched` (1 failed)
- green: `MatchArtistAsync` returns `Unmatched("low score (N)")` when the top score is below 85. Single test green; suite in cycle 91.
- refactor: none needed

## Cycle 91: U79 an empty `artists` array → `Unmatched` with a "no result" reason

- test: `Sources/MusicBrainzSourceTests.cs::MatchArtistAsync_NoResults_IsUnmatchedAsNoResult` (new; `artist_search_empty.json`)
- red: `dotnet test … --filter "FullyQualifiedName~MusicBrainzSourceTests.MatchArtistAsync_NoResults_IsUnmatchedAsNoResult"` -> `System.ArgumentOutOfRangeException : Index was out of range.` (1 failed; `candidates[0]` on an empty list)
- green: `MatchArtistAsync` returns `Unmatched("no result")` for an empty result set. Suite -> 117 passed, 0 failed
- refactor: none needed
- commit: `98426f6`

## Cycle 92: U80 a catalogue page groups releases by release group, maps types, keeps `first-release-date` as given, links the release group

- test: `Sources/MusicBrainzSourceTests.cs::FetchCataloguePageAsync_GroupsByReleaseGroupMapsTypesKeepsPartialDatesAndLinksTheReleaseGroup` (new; `releases_page1.json`: 8 releases / 7 groups incl. Live, Compilation, Remix, Interview→Other, year-only date)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~MusicBrainzSourceTests.FetchCataloguePageAsync_GroupsByReleaseGroupMapsTypesKeepsPartialDatesAndLinksTheReleaseGroup" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `System.NotImplementedException : The method or operation is not implemented.` (1 failed)
- green: `MusicBrainzSource.FetchCataloguePageAsync` — `release?artist=…&status=official&inc=release-groups&limit=100&offset=N&fmt=json`, first release per `release-group.id`, `ReleaseTypeMapper.MapMusicBrainz`, `first-release-date` (empty → null), release-group URL. `NextOffset` is still null (U81). Suite -> 118 passed, 0 failed
- refactor: none needed
- commit: `30ca229`

## Cycle 93: U81 `NextOffset` is `offset + 100` while `release-count` exceeds it and null on the last page

- test: `Sources/MusicBrainzSourceTests.cs::FetchCataloguePageAsync_NextOffsetAdvancesBy100WhileTheCountExceedsIt_NullOnTheLastPage` (new; pages 1 and 2 of the 110-release fixture)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~MusicBrainzSourceTests.FetchCataloguePageAsync_NextOffsetAdvancesBy100WhileTheCountExceedsIt_NullOnTheLastPage" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: Values differ / Expected: Tuple (100, 110) / Actual:   Tuple (null, 110)` (1 failed)
- green: `FetchCataloguePageAsync` sets `NextOffset = offset + 100 < release-count ? offset + 100 : null`. Suite -> 119 passed, 0 failed
- refactor: none needed
- commit: `5daf18f`

## Cycle 94: U82 editions come back one per Official release with all `media[].tracks[].title` normalized as tracks

- test: `Sources/MusicBrainzSourceTests.cs::FetchEditionsAsync_OneEditionPerOfficialReleaseWithAllMediaTracksNormalized` (new; `editions_two_official.json`, 14 and 15 tracks)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~MusicBrainzSourceTests.FetchEditionsAsync_OneEditionPerOfficialReleaseWithAllMediaTracksNormalized" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `System.NotImplementedException : The method or operation is not implemented.` (1 failed)
- green: `MusicBrainzSource.FetchEditionsAsync` pages `release?release-group=…&status=official&inc=recordings+media&limit=25&offset=N&fmt=json` until `release-count`, flattening media tracks through `NormalizeTrack`. Suite -> 120 passed, 0 failed
- refactor: none needed
- commit: `869af8d`
