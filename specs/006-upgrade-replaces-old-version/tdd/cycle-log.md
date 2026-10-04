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

## Cycle 5: A5 changing the plugin's name in `build.yaml` alone fails the suite

- test: the suite; the behaviour is closed by `U1` (cycle 3), evidenced here by a deliberate mutant
- mutant: `build.yaml:3` `name` set to `"New Releases Tracker"`, nothing else changed.
  `dotnet test --configuration Release` -> `Failed!  - Failed:     2, Passed:   319`:
  `PluginSanityTests.Plugin_DisplayName_MatchesTheNameThePackageDeclares [FAIL]` and
  `RepositoryManifestTests.TheDerivedSlug_MatchesTheFilenameTheReleaseWorkflowBuilds [FAIL]`
  (the slug `new-releases-tracker` is no substring of the workflow). Restored from a file copy,
  verified with `cmp -s`; `git status` clean for the file
- green: suite back to 321 passed after the restore
- refactor: none
- commit: see the commit that carries this entry

## Cycle 6: U24 the release root of an entry is its address above the tag directory

- test: `tests/Jellyfin.Plugin.NewReleases.Tests/Packaging/RepositoryManifestTests.cs::SiteRootOf_ReturnsTheAddressAboveTheEntrysTagDirectory`
  (restates `003`'s U41, `SiteRootOf_ReturnsTheDirectoryOfTheEntrysOwnSourceUrl`; behaviour added to
  the list in the loop, see the test list's note under `U12`)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~RepositoryManifestTests.SiteRootOf_ReturnsTheAddressAboveTheEntrysTagDirectory" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Expected: ···"ost.invalid/owner/repo/releases/download/"` /
  `Actual:   ···"alid/owner/repo/releases/download/v3.0.0/"` (1 failed)
- green: `SiteRootOf` drops the file and then the tag directory. Build 0 warnings. Suite
  `dotnet test --configuration Release` -> 321 passed, 0 failed. `003`'s U40 still passes on its
  old-shape literals: their root is now the host, which still differs between its two entries
- refactor: none yet. `SiteRootOf` now returns a release root; renaming it waits until the
  source-address rule is restated, so the rename is one structural commit over the finished rule
- commit: see the commit that carries this entry

## Cycle 7: U7 an entry whose `sourceUrl` is the release asset for its own version is accepted

- test: `tests/Jellyfin.Plugin.NewReleases.Tests/Packaging/RepositoryManifestTests.cs::AWellFormedEntry_SourceUrlIsAccepted`
  (existing, `003`'s U25 accepting side). The rule change is in its fixture: `WellFormedEntry`'s
  default `sourceUrl` became `{ExampleSiteRoot}v{version}/{Slug}.zip`, and `ExampleSiteRoot`
  became `https://example.invalid/owner/repo/releases/download/`
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~RepositoryManifestTests.AWellFormedEntry_SourceUrlIsAccepted" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.EndsWith() Failure: String end does not match` /
  `String: ···"leases/download/v1.2.3.0/new-releases.zip"` / `Expected end: "new-releases_1.2.3.0.zip"` (1 failed)
- green: `AssertSourceUrlNamesItsOwnVersion` now requires `sourceUrl` to equal
  `{root}v{version}/{Slug}.zip` exactly — stricter than the `StartsWith` + `EndsWith` pair it
  replaces. The full suite then failed `EntriesFromTwoDifferentSites_AreRejected` (`003`'s U40) on
  its accepting half: its literals were the old shape. Only its two literals moved to the release
  shape; both assertions are unchanged. Suite -> 321 passed, 0 failed
- refactor: none
- commit: see the commit that carries this entry
- note: the three-part tag (`U8`) is deliberately not handled yet; exact equality with the
  four-part version is the smallest rule this test demands

## Cycle 8: U8 a three-part tag `v1.2.3` is accepted for version `1.2.3.0`

- test: `tests/Jellyfin.Plugin.NewReleases.Tests/Packaging/RepositoryManifestTests.cs::AReleaseTaggedInThreeParts_IsAcceptedForItsFourPartVersion` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~RepositoryManifestTests.AReleaseTaggedInThreeParts_IsAcceptedForItsFourPartVersion" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Expected: ···"leases/download/v1.2.3.0/new-releases.zip"` /
  `Actual:   ···"releases/download/v1.2.3/new-releases.zip"` (1 failed)
- green: `AssertSourceUrlNamesItsOwnVersion` requires `{root}v` before and `/{Slug}.zip` after the
  tag, and the tag padded to four parts by the new `InFourParts` — the same padding the release
  workflow's `version4` step applies — to equal the entry's version. Build 0 warnings. Suite ->
  322 passed, 0 failed
- refactor: none
- commit: see the commit that carries this entry

## Cycle 9: U9 a release address naming another tag is rejected

- test: `tests/Jellyfin.Plugin.NewReleases.Tests/Packaging/RepositoryManifestTests.cs::ASourceUrlOffTheSiteOrNamingAnotherVersion_IsRejected`,
  new case `("1.2.3.1", ExampleSiteRoot + "v1.2.3/new-releases.zip")` — the other side of `U8`'s boundary
- red: none on the first run; cycle 8's rule already rejects it.
  `dotnet test --configuration Release --filter "FullyQualifiedName~RepositoryManifestTests.ASourceUrlOffTheSiteOrNamingAnotherVersion_IsRejected" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Passed!  - Failed:     0, Passed:     4`
- mutant: the tag-to-version line `Assert.Equal(number, InFourParts(…))` deleted from
  `AssertSourceUrlNamesItsOwnVersion`. Same command ->
  `ASourceUrlOffTheSiteOrNamingAnotherVersion_IsRejected(number: "1.2.3.1", …) [FAIL]` (1 failed,
  3 passed): this case alone catches it. Restored from a file copy, verified with `cmp -s`
- green: no rule change. Suite -> 323 passed, 0 failed
- refactor: none
- commit: see the commit that carries this entry

## Cycle 10: U10 a Pages-style `…/<slug>/<slug>_<version>.zip` address is rejected

- test: `ASourceUrlOffTheSiteOrNamingAnotherVersion_IsRejected`, new case
  `("1.0.0.0", ExampleSiteRoot + "new-releases/new-releases_1.0.0.0.zip")` — the Pages layout,
  placed under the release root so that only the address shape can reject it
- red: none on the first run. Same filter as cycle 9 -> `Passed!  - Failed:     0, Passed:     5`
- mutant: `AssertSourceUrlNamesItsOwnVersion` put back to the Pages-era rule,
  `StartsWith(siteRoot)` + `EndsWith($"{Slug}_{number}.zip")`. Same command ->
  `ASourceUrlOffTheSiteOrNamingAnotherVersion_IsRejected(number: "1.0.0.0", sourceUrl: "https://example.invalid/owner/repo/releases/downlo"···) [FAIL]`
  (1 failed, 4 passed). The display is truncated; it is this case, because the only other cases
  with `"1.0.0.0"` and that root end in `_2.0.0.0.zip` and `jellyfin-new-releases.zip`, which the
  old rule also rejects. Restored from a file copy, verified with `cmp -s`
- green: no rule change. Suite -> 324 passed, 0 failed
- refactor: none
- commit: see the commit that carries this entry

## Cycle 11: U11 an asset carrying the version in its file name is rejected under the right tag

- test: `ASourceUrlOffTheSiteOrNamingAnotherVersion_IsRejected`, new case
  `("1.0.0.0", ExampleSiteRoot + "v1.0.0.0/new-releases_1.0.0.0.zip")` — right root, right tag,
  JPRM's own file name
- red: none on the first run. Same filter as cycle 9 -> `Passed!  - Failed:     0, Passed:     6`
- mutant: `asset` in `AssertSourceUrlNamesItsOwnVersion` changed from `$"/{Slug}.zip"` to
  `$"/{Slug}_{number}.zip"`. Same command -> `…IsRejected(number: "1.0.0.0", sourceUrl: "https://example.invalid/owner/repo/releases/downlo"···) [FAIL]`
  (1 failed, 5 passed). It is this case: the Pages-style case still fails the `{root}v` prefix,
  and the old-slug cases end in `jellyfin-new-releases…`. Restored from a file copy, verified with `cmp -s`
- green: no rule change. Suite -> 325 passed, 0 failed
- refactor: none
- commit: see the commit that carries this entry

## Cycle 12: U12 two entries under two different release roots are rejected

- test: `tests/Jellyfin.Plugin.NewReleases.Tests/Packaging/RepositoryManifestTests.cs::EntriesFromTwoDifferentSites_AreRejected`
  (`003`'s U40; its literals moved to release addresses in cycle 7, assertions unchanged)
- red: none on the first run; cycle 7 already made it bind release roots.
  `dotnet test --configuration Release --filter "FullyQualifiedName~RepositoryManifestTests.EntriesFromTwoDifferentSites_AreRejected" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Passed!  - Failed:     0, Passed:     1`
- mutant: `tagStart` built from `SiteRootOf(version)` — the entry's own root — instead of the
  root passed in. Same command -> `EntriesFromTwoDifferentSites_AreRejected [FAIL]` (1 failed).
  Restored from a file copy, verified with `cmp -s`
- green: no rule change. Suite -> 325 passed, 0 failed
- refactor: done as its own structural commit after this one, over the finished rule
- commit: see the commit that carries this entry

## Refactor after cycle 12: the source-address rule speaks of release roots

- structural only, suite unchanged in count: in `RepositoryManifestTests.cs`, `SiteRootOf` ->
  `ReleaseRootOf`, `ExampleSiteRoot` -> `ExampleReleaseRoot`, `siteRoot` -> `releaseRoot`, and
  the doc comments of `003`'s U25, U26 and U40 no longer describe a Pages site. Cycle 6's test is
  renamed with its subject: `SiteRootOf_ReturnsTheAddressAboveTheEntrysTagDirectory` ->
  `ReleaseRootOf_ReturnsTheAddressAboveTheEntrysTagDirectory`; the test list follows. The three
  `003` test method names that say "Site" are kept: other features' documents cite them
- suite: build 0 warnings; `dotnet test --configuration Release` -> 325 passed, 0 failed
- commit: see the commit that carries this entry

## Cycle 13: U13 the workflow uploads `{Slug}.zip` to the version's GitHub Release

- test: `tests/Jellyfin.Plugin.NewReleases.Tests/Packaging/RepositoryManifestTests.cs::TheDerivedSlug_MatchesTheFilenameTheReleaseWorkflowBuilds`
  (restates `003`'s U39: an anchored `Assert.Matches` on `gh release create … {Slug}.zip` over the
  workflow's non-comment lines, replacing the bare `Assert.Contains` cycle 3 found too weak)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~RepositoryManifestTests.TheDerivedSlug_MatchesTheFilenameTheReleaseWorkflowBuilds" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Matches() Failure: Pattern not found in value` (1 failed): no `gh release create` step
- green: `.github/workflows/package.yml` gains "Create the GitHub Release" after the framework
  check: `mv` JPRM's `new-releases_<version4>.zip` to `new-releases.zip`, then
  `gh release create "$GITHUB_REF_NAME" ./artifacts/new-releases.zip --title "$GITHUB_REF_NAME"`
  with `GH_TOKEN`. Suite -> 325 passed, 0 failed
- anchor check: the uploaded file renamed to `jellyfin-new-releases.zip` -> the same test fails,
  `Assert.Matches() Failure: Pattern not found in value`. Restored from a file copy, verified with `cmp -s`
- refactor: deferred. The comment-stripping now exists here and in `ReleaseWorkflowTests.Steps`;
  it is extracted once `U14`–`U19` have settled what both classes read
- commit: see the commit that carries this entry
- note: the workflow is mid-change. "Add the version to the published repository" still names the
  stale `jellyfin-new-releases_<version4>.zip`, which the new step has moved away; `U14` replaces it.
  Do not tag a release before T013 is ticked
