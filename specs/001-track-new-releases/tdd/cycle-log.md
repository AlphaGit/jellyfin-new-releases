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
