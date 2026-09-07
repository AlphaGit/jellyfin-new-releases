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
