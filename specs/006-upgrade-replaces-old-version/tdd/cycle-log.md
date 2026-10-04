# Cycle Log: An upgrade leaves exactly one version of the plugin running

Append only. Newest last. Every entry's `red` block is the evidence that the test existed and
failed before the implementation.

## Baseline

- build: `dotnet build --configuration Release` -> succeeded, 0 warnings
- suite: `dotnet test --configuration Release` -> 318 passed, 0 failed, 0 skipped
- suite: `node --test "tests/web/*.test.js"` -> 358 passed, 0 failed
- commit: `c0e20a2`, with the 006 planning documents uncommitted and no source or test changed
- recorded: cycle 0, before any change

## Cycle 1: U4 the catalogue lists no published version

- test: `tests/Jellyfin.Plugin.NewReleases.Tests/Packaging/RepositoryManifestTests.cs::Manifest_EveryVersionCarriesItsDownloadChecksumTimestampAndJellyfin12`
  (existing; `PublishedVersionsToday` restated from 2 to 0, the rule change `spec.md` session
  2026-10-04 decided, taken before the catalogue changed)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~RepositoryManifestTests.Manifest_EveryVersionCarriesItsDownloadChecksumTimestampAndJellyfin12" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `repo/manifest.json lists 2 version(s), expected 0.` (1 failed)
- green: `repo/manifest.json` `versions` emptied; GUID, name and every other field untouched.
  Suite `dotnet test --configuration Release` -> 318 passed, 0 failed
- refactor: the class comment said the catalogue is "never hand-edited"; it now records the one
  hand edit. Suite re-run -> 318 passed
- commit: see the commit that carries this entry

## Cycle 2: U5 the catalogue's plugin entry carries the name the plugin reports

- test: `tests/Jellyfin.Plugin.NewReleases.Tests/PluginSanityTests.cs::Plugin_DisplayName_MatchesTheNameTheCatalogueLists` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~PluginSanityTests.Plugin_DisplayName_MatchesTheNameTheCatalogueLists" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Expected: "New Releases"` / `Actual: "Jellyfin New Releases"` (1 failed)
- green: `repo/manifest.json` entry `name` set to `New Releases`. Build 0 warnings. Suite
  `dotnet test --configuration Release` -> 319 passed, 0 failed
- refactor: none. Every test in the class builds its own `Plugin` from two substitutes; the new one
  follows the file rather than extracting a helper the other tests would then need to adopt
- commit: see the commit that carries this entry
