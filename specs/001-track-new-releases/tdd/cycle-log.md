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
