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

## Cycle 3: U1 the name `build.yaml` declares equals the name the plugin reports

- test: `tests/Jellyfin.Plugin.NewReleases.Tests/PluginSanityTests.cs::Plugin_DisplayName_MatchesTheNameThePackageDeclares` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~PluginSanityTests.Plugin_DisplayName_MatchesTheNameThePackageDeclares" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Expected: "New Releases"` / `Actual: "Jellyfin New Releases"` (1 failed)
- green: `build.yaml` `name` set to `New Releases`. Suite `dotnet test --configuration Release`
  -> 320 passed, 0 failed
- refactor: none
- commit: see the commit that carries this entry
- deviation: `tasks.md` T005 also asked for `package.yml:86` to change in this step, expecting
  `003`'s U39 to go red. It did not: U39 is `Assert.Contains($"{Slug}_${{ … }}.zip", workflow)`,
  and `new-releases_${{ … }}.zip` is a substring of the stale `jellyfin-new-releases_${{ … }}.zip`.
  With no red demanding it, the workflow was left alone; it is wrong until T013 and is covered by
  `U13`, now noted to anchor the name. T005 is ticked for its behaviour, `U1`; its workflow half
  moves to T013. Do not tag a release between this commit and T013.

## Cycle 4: U2 the plugin's displayed name does not contain "Jellyfin" in any letter case

- test: `tests/Jellyfin.Plugin.NewReleases.Tests/PluginSanityTests.cs::Plugin_DisplayName_DoesNotClaimToBeJellyfin` (new)
- red: none on the first run, as planned — `Plugin.Name` is already `New Releases`:
  `dotnet test --configuration Release --filter "FullyQualifiedName~PluginSanityTests.Plugin_DisplayName_DoesNotClaimToBeJellyfin" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Passed!  - Failed:     0, Passed:     1`
- mutant: `src/Jellyfin.Plugin.NewReleases/Plugin.cs:31` `Name` set to `"JELLYFIN New Releases"`
  (upper case, so the "any letter case" half is what fails). Same command ->
  `Assert.DoesNotContain() Failure: Sub-string found` / `Found:  "jellyfin"` (1 failed). Restored
  from a file copy, verified with `cmp -s`; `git status` clean for the file
- green: no production change. Suite `dotnet test --configuration Release` -> 321 passed, 0 failed
- refactor: none
- commit: see the commit that carries this entry
- note: the first attempt at the mutant run printed nothing because of a shell quoting mistake in
  the command, not a test result; it was re-run as recorded above
