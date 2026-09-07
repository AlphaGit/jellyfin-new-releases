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
