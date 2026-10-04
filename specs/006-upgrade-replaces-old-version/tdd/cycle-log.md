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

## Cycle 14: U14 `jprm repo add` is given `--plugin-url` naming the release asset

- test: `tests/Jellyfin.Plugin.NewReleases.Tests/Packaging/RepositoryManifestTests.cs::TheReleaseWorkflow_PointsTheCatalogueAtTheReleaseAsset` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~RepositoryManifestTests.TheReleaseWorkflow_PointsTheCatalogueAtTheReleaseAsset" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Matches() Failure: Pattern not found in value` (1 failed): the step passed `--url`
- green: "Add the version to the published repository" now runs
  `jprm repo add --plugin-url "https://github.com/${{ github.repository }}/releases/download/${GITHUB_REF_NAME}/new-releases.zip" ./repo/manifest.json ./artifacts/new-releases.zip`.
  With `--plugin-url`, JPRM 1.1.0 copies nothing into `repo/` (`research.md` R7). The stale
  `jellyfin-new-releases_…zip` path cycle 13 left behind is gone. Suite -> 326 passed, 0 failed
- refactor: the two workflow tests in this class now build the same comment-free text; extracted
  in its own structural commit after this one
- commit: see the commit that carries this entry

## Refactor after cycle 14: one reader for a workflow's steps

- structural only: `Support/RepositoryFiles.WorkflowSteps(path)` reads a workflow with its comment
  lines removed. `ReleaseWorkflowTests.Steps` and the new `RepositoryManifestTests.ReleaseWorkflowSteps`
  both use it; the two inline copies cycles 13 and 14 wrote, and `ReleaseWorkflowTests.Workflow`,
  are gone
- suite: build 0 warnings; `dotnet test --configuration Release` -> 326 passed, 0 failed
- commit: see the commit that carries this entry

## Cycle 15: U15 build, then release, then catalogue entry, then commit

- test: `tests/Jellyfin.Plugin.NewReleases.Tests/Packaging/ReleaseWorkflowTests.cs::ReleaseWorkflow_BuildsReleasesAddsToTheManifestAndCommits_InThatOrder`
  (restates `003`'s U27, `ReleaseWorkflow_BuildsAddsToTheManifestCommitsAndDeploys_InThatOrder`:
  `gh release create` joins the chain between build and `repo add`; the `deploy-pages` link leaves
  it, because `FR-012` removes Pages — its absence is `U16`'s to pin, not dropped)
- red: none on the first run; cycles 13 and 14 already placed the steps in this order.
  `dotnet test --configuration Release --filter "FullyQualifiedName~ReleaseWorkflowTests.ReleaseWorkflow_BuildsReleasesAddsToTheManifestAndCommits_InThatOrder" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Passed!  - Failed:     0, Passed:     1`
- mutant: the "Create the GitHub Release" step moved after "Add the version to the published
  repository". Same command -> `the catalogue names the release asset before the release exists`
  (1 failed). Restored from a file copy, verified with `cmp -s`. This is also `quickstart.md`
  scenario 2's second mutant, recorded again under `A6`
- green: no workflow change. Suite -> 326 passed, 0 failed
- refactor: none
- commit: see the commit that carries this entry

## Cycle 16: U16 no step uses a GitHub Pages action

- test: `tests/Jellyfin.Plugin.NewReleases.Tests/Packaging/ReleaseWorkflowTests.cs::ReleaseWorkflow_DeploysNoPagesSite`
  (new theory: `actions/configure-pages`, `actions/upload-pages-artifact`, `actions/deploy-pages`)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~ReleaseWorkflowTests.ReleaseWorkflow_DeploysNoPagesSite" -- RunConfiguration.TreatNoTestsAsError=true`
  -> three `[FAIL]`, e.g. `Found:  "actions/deploy-pages"` (3 failed)
- green: `.github/workflows/package.yml` loses "Configure Pages", "Upload the site", "Deploy to
  Pages", and the job's `environment: github-pages` block, whose `url` read the deploy step's
  output and would otherwise dangle. Permissions are `U17`'s. Suite -> 329 passed, 0 failed
- refactor: none in this cycle; the Pages wording in the workflow's comments is cleared once the
  permissions are settled
- commit: see the commit that carries this entry

## Cycle 17: U17 the workflow's permissions grant neither `pages` nor `id-token`

- test: `tests/Jellyfin.Plugin.NewReleases.Tests/Packaging/ReleaseWorkflowTests.cs::ReleaseWorkflow_GrantsNoPagesPermission`
  (new theory: `pages`, `id-token`; a key at any indent over the non-comment lines)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~ReleaseWorkflowTests.ReleaseWorkflow_GrantsNoPagesPermission" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.DoesNotMatch() Failure: Match found`, twice (2 failed)
- green: the `permissions` block keeps only `contents: write`. Suite -> 331 passed, 0 failed
- refactor: the workflow's comments still describe Pages; rewritten in its own structural commit
  after this one
- commit: see the commit that carries this entry

## Refactor after cycle 17: the workflow's comments describe what it now does

- structural only, comments: the header no longer says the catalogue is served by Pages; the
  version comment names `new-releases_1.0.0.0.zip`; the staging comment no longer says
  `jprm repo add` copies the package into `repo/`, which `--plugin-url` stopped
- suite: `dotnet test --configuration Release` -> 331 passed, 0 failed
- commit: see the commit that carries this entry

## Cycle 18: U18 the release text is the tagged version's changelog section

- test: `tests/Jellyfin.Plugin.NewReleases.Tests/Packaging/ReleaseWorkflowTests.cs::ReleaseWorkflow_GivesTheReleaseTheTaggedVersionsChangelogSection`
  (new: reads the path `gh release create --notes-file` names, then requires an `entryFor(` line
  that writes that path for `"${{ steps.ver.outputs.version }}"`)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~ReleaseWorkflowTests.ReleaseWorkflow_GivesTheReleaseTheTaggedVersionsChangelogSection" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `gh release create is given no --notes-file` (1 failed)
- green: "Create the GitHub Release" first runs a `node -e` line that writes
  `entryFor(CHANGELOG.md, "${{ steps.ver.outputs.version }}")` to `./artifacts/release-notes.md`,
  and `gh release create` gains `--notes-file ./artifacts/release-notes.md`. Checked by hand first:
  for `0.1.1` it writes the section starting `### Fixed`; for `9.9.9` it exits 1 with
  `CHANGELOG.md has no section for 9.9.9`, so a missing section still fails the run. Suite ->
  332 passed, 0 failed; node -> 358 passed
- mutant: the notes line fed `"${{ steps.ver.outputs.version4 }}"` instead. Same command ->
  `Assert.Matches() Failure: Pattern not found in value` (1 failed). Restored from a file copy,
  verified with `cmp -s`; the test then passes again. A first attempt used `sed`, whose result
  could not be confirmed; it was restored and repeated with a checked replacement, recorded here
- refactor: none. The `node -e` line is long, but moving it into `changelog-entry.js` would add a
  second entry point to a script no task here changes
- commit: see the commit that carries this entry

## Cycle 19: U19 the release notes are written before `gh release create` runs

- test: `tests/Jellyfin.Plugin.NewReleases.Tests/Packaging/ReleaseWorkflowTests.cs::ReleaseWorkflow_WritesTheReleaseNotesBeforeCreatingTheRelease` (new)
- red: none on the first run; cycle 18 wrote the notes line first.
  `dotnet test --configuration Release --filter "FullyQualifiedName~ReleaseWorkflowTests.ReleaseWorkflow_WritesTheReleaseNotesBeforeCreatingTheRelease" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Passed!  - Failed:     0, Passed:     1`
- mutant: the `entryFor(` line moved below the `gh release create` line. Same command ->
  `the release is created before its notes are written` (1 failed). Restored from a file copy,
  verified with `cmp -s`
- green: no workflow change. Suite -> 333 passed, 0 failed
- refactor: none
- commit: see the commit that carries this entry

## Cycle 20: A6 renaming the release asset, or releasing after the catalogue entry, fails the suite

- test: the suite; closed by `U7`–`U19` and `U24`, evidenced by `quickstart.md` pass 1, scenario 2
- mutant 1: the uploaded file in `gh release create` renamed from `./artifacts/new-releases.zip`
  to `./artifacts/plugin.zip`.
  `dotnet test --configuration Release --filter "FullyQualifiedName~RepositoryManifestTests" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `RepositoryManifestTests.TheDerivedSlug_MatchesTheFilenameTheReleaseWorkflowBuilds [FAIL]`
  (1 failed, 21 passed). Restored from a file copy, verified with `cmp -s`
- mutant 2: "Create the GitHub Release" moved after "Add the version to the published repository".
  `dotnet test --configuration Release --filter "FullyQualifiedName~ReleaseWorkflowTests" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `ReleaseWorkflowTests.ReleaseWorkflow_BuildsReleasesAddsToTheManifestAndCommits_InThatOrder [FAIL]`
  (1 failed, 11 passed). Restored from a file copy, verified with `cmp -s`; `git status` clean
- green: suite unchanged at 333 passed
- refactor: none
- commit: see the commit that carries this entry

## Cycle 21: U20 the `## Install` section names the raw catalogue address

- test: `tests/Jellyfin.Plugin.NewReleases.Tests/Packaging/DocumentationTests.cs::Install_NamesTheRawCatalogueAddress` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~DocumentationTests.Install_NamesTheRawCatalogueAddress" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Contains() Failure: Sub-string not found` / `Not found: "https://raw.githubusercontent.com/AlphaGi"···` (1 failed)
- green: `README.md` install step 2 names
  `https://raw.githubusercontent.com/AlphaGit/jellyfin-new-releases/main/repo/manifest.json`, and
  the raw-address pattern for a fork. The Pages wording went with the sentence it lived in.
  `003`'s U30 still passes. Suite -> 334 passed, 0 failed
- refactor: none
- commit: see the commit that carries this entry

## Cycle 22: U21 the `## Install` section names no `github.io` address

- test: `tests/Jellyfin.Plugin.NewReleases.Tests/Packaging/DocumentationTests.cs::Install_NamesNoPagesAddress` (new)
- red: none on the first run; cycle 21 rewrote the sentence that held the Pages address.
  `dotnet test --configuration Release --filter "FullyQualifiedName~DocumentationTests.Install_NamesNoPagesAddress" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Passed!  - Failed:     0, Passed:     1`
- mutant: install step 2 also says the old `https://alphagit.github.io/jellyfin-new-releases/manifest.json`
  "also works", beside the raw address — which `U20` alone accepts. Same command ->
  `Assert.DoesNotContain() Failure: Sub-string found` / `Found:  "github.io"` (1 failed). Restored
  from a file copy, verified with `cmp -s`
- green: no README change. Suite -> 335 passed, 0 failed
- refactor: none
- commit: see the commit that carries this entry

## Cycle 23: U22 the `0.2.0` section names the old-name directory to remove once

- test: `tests/Jellyfin.Plugin.NewReleases.Tests/Packaging/DocumentationTests.cs::Changelog_020_NamesTheOldNameDirectoryToRemove`
  (new, with a `ChangelogSection` helper that applies `entryFor`'s heading rule)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~DocumentationTests.Changelog_020_NamesTheOldNameDirectoryToRemove" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `CHANGELOG.md has no section for 0.2.0` (1 failed)
- green: `CHANGELOG.md` gains `## 0.2.0 — unreleased` with "Upgrading from 0.1.x — once": the
  `Jellyfin New Releases_<version>` folder, why it is left behind, how to remove it and nothing
  else, and that no later upgrade needs it. Suite -> 336 passed, 0 failed
- refactor: none
- commit: see the commit that carries this entry
- note: the heading says `unreleased`; the release date replaces it when `0.2.0` is tagged.
  `entryFor` matches `## 0.2.0 ` followed by anything, so the release step reads it either way

## Cycle 24: U23 the `0.2.0` section names the address that replaces the old repository address

- test: `tests/Jellyfin.Plugin.NewReleases.Tests/Packaging/DocumentationTests.cs::Changelog_020_NamesTheRepositoryAddressThatReplacesTheOldOne` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~DocumentationTests.Changelog_020_NamesTheRepositoryAddressThatReplacesTheOldOne" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Contains() Failure: Sub-string not found` / `Not found: "https://raw.githubusercontent.com/AlphaGi"···` (1 failed)
- green: the "Upgrading from 0.1.x — once" list gains "Replace the repository address", naming
  the raw catalogue address, where to change it, and that a server left on the old address sees no
  later version. Suite -> 337 passed, 0 failed. (A first edit attempt did not apply — its anchor
  text did not match the wrapped lines — so the suite ran once more on red before the real edit;
  that run was not a green.)
- refactor: the catalogue address is now written twice in this class; extracted in its own
  structural commit after this one
- commit: see the commit that carries this entry

## Refactor after cycle 24: one constant for the catalogue address

- structural only: `DocumentationTests.CatalogueAddress` holds the raw catalogue address `U20`
  and `U23` both assert
- suite: build 0 warnings; `dotnet test --configuration Release` -> 337 passed, 0 failed
- commit: see the commit that carries this entry

## Notes and deviations: `/speckit-implement`, the tasks without a behaviour

- T001: the baseline is the one recorded at the top of this log, at `c0e20a2`
- T006: no change. `build.yaml`'s `overview` and `description` never name the plugin; "Jellyfin"
  appears there only as the server it runs on
- T016: the rule went to a sibling note, `docs/plugin-name.md`, not into `docs/http-surface.md`,
  which is about routes
- T022: `build.yaml` `0.2.0` and the project `<Version>0.2.0.0</Version>`, committed with the
  changelog's `0.2.0` content (T020, T031). The `0.2.0` heading says `unreleased` until tagged
- T023: build 0 warnings; `dotnet test --configuration Release` -> 337 passed; node -> 358 passed.
  Scenario 1 re-run on the final tree: `build.yaml` `name` set to `"Another Name"`,
  `--filter "FullyQualifiedName~PluginSanityTests"` -> `Plugin_DisplayName_MatchesTheNameThePackageDeclares [FAIL]`
  (1 failed, 7 passed), restored and verified with `cmp -s`. Scenario 2 is cycle 20; the workflow
  has not changed since
- T024, T025, T026 stay open: pushing to `main`, tagging `v0.2.0` and the real-server pass are the
  maintainer's

## Cycle 25: U25 the workflow moves the file JPRM writes

- test: `tests/Jellyfin.Plugin.NewReleases.Tests/Packaging/RepositoryManifestTests.cs::TheReleaseWorkflow_MovesTheFileJprmWrites` (new;
  remediation of `tdd/verification.md` finding 1, task T032)
- red: none on the first run, and none was possible: the workflow line this pins was written in
  cycle 13, before the audit found it unpinned. **Test-after**, stated as such.
  `dotnet test --configuration Release --filter "FullyQualifiedName~RepositoryManifestTests.TheReleaseWorkflow_MovesTheFileJprmWrites" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Passed!  - Failed:     0, Passed:     1`
- mutant: the audit's M5, `mv` source `./artifacts/jellyfin-new-releases_${{ steps.ver.outputs.version4 }}.zip`.
  Same command -> `Assert.Matches() Failure: Pattern not found in value` (1 failed). M5 survived
  the audit; it is caught now. Restored from a file copy, verified with `cmp -s`
- green: no workflow change. Suite -> 338 passed, 0 failed
- refactor: none
- commit: see the commit that carries this entry

## Cycle 26: U26 the release uploads the file the package was moved to

- test: `tests/Jellyfin.Plugin.NewReleases.Tests/Packaging/RepositoryManifestTests.cs::TheReleaseWorkflow_UploadsTheFileItMovedThePackageTo` (new;
  finding 1, task T032)
- red: none valid. The first run failed with `the workflow does not move JPRM's package` — the
  **test** was broken: its `\S+` could not cross the spaces inside `${{ … }}`. Not recorded as a
  red; the pattern was corrected to the same literal `U25` uses. The corrected test passed on its
  first run, because the workflow was already right (cycle 13). **Test-after**, stated as such.
  `dotnet test --configuration Release --filter "FullyQualifiedName~RepositoryManifestTests.TheReleaseWorkflow_UploadsTheFileItMovedThePackageTo" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Passed!  - Failed:     0, Passed:     1`
- mutant: the audit's M14, `mv` target `./artifacts/new-release.zip`. Same command ->
  `Assert.Equal() Failure: Strings differ` / `Expected: "./artifacts/new-release.zip"` /
  `Actual:   "./artifacts/new-releases.zip"` (1 failed). M14 survived the audit; it is caught now.
  Restored from a file copy, verified with `cmp -s`
- green: no workflow change. Suite -> 339 passed, 0 failed
- refactor: the `mv` pattern is now written in `U25` and `U26`; extracted in its own structural
  commit after this one
- commit: see the commit that carries this entry

## Refactor after cycle 26: one pattern for the `mv` line

- structural only: `RepositoryManifestTests.MovesJprmsPackage` holds the `mv` source pattern
  `U25` and `U26` both use; `U25` keeps its trailing `\s`. Two mistakes were caught before the
  first run and corrected: the field was first placed above `Slug`, which static initialisation
  order would have left null, and `U25`'s `\s` had been dropped
- suite: build 0 warnings; `dotnet test --configuration Release` -> 339 passed, 0 failed
- mutants re-run on the refactored tests, `--filter "FullyQualifiedName~RepositoryManifestTests"`:
  M5 -> `TheReleaseWorkflow_MovesTheFileJprmWrites [FAIL]` and
  `TheReleaseWorkflow_UploadsTheFileItMovedThePackageTo [FAIL]` (2 failed); M14 ->
  `TheReleaseWorkflow_UploadsTheFileItMovedThePackageTo [FAIL]` (1 failed). Both restored, `cmp -s`
- commit: see the commit that carries this entry

## Cycle 27: U27 a release address with the right tag and a wrong asset name is rejected

- test: `ASourceUrlOffTheSiteOrNamingAnotherVersion_IsRejected`, new case
  `("1.0.0.0", ExampleReleaseRoot + "v1.0.0.0/new_releases.zip")` — the same length as the real
  asset, so the slice and the tag comparison cannot reject it; only the asset-name check can
  (finding 2, task T033)
- red: none on the first run; the check exists since cycle 8. **Test-after**, stated as such.
  `dotnet test --configuration Release --filter "FullyQualifiedName~RepositoryManifestTests.ASourceUrlOffTheSiteOrNamingAnotherVersion_IsRejected" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Passed!  - Failed:     0, Passed:     7`
- mutant: the audit's M12, `Assert.EndsWith(asset, …)` deleted from `AssertSourceUrlNamesItsOwnVersion`.
  Same command -> `…IsRejected(number: "1.0.0.0", sourceUrl: "https://example.invalid/owner/repo/releases/downlo"···) [FAIL]`
  (1 failed, 6 passed). The audit ran M12 against the six other rows and all passed, so the
  failing row is this one. M12 survived the audit; it is caught now. Restored from a file copy,
  verified with `cmp -s`
- green: no rule change. Suite -> 340 passed, 0 failed
- refactor: none
- commit: see the commit that carries this entry

## Refactor after cycle 27: no branch in the catalogue-wide address check (T034)

- structural, test only: `Manifest_EverySourceUrlSharesOneSiteRoot_AndNamesItsOwnVersion` loses
  `003`'s `if (versions.Count > 0)` (finding 3). `Assert.All` reads the root from `versions[0]`
  inside its lambda, so an empty catalogue asserts the count only and the first entry is bound the
  moment it appears. Same result as before for every catalogue; the smell, not a behaviour, changed
- check: `grep -n "if (versions.Count" RepositoryManifestTests.cs` -> no match. Build 0 warnings;
  suite -> 340 passed
- evidence the check binds (temporary, both files restored and verified with `cmp -s`):
  - one Pages-style entry with `PublishedVersionsToday` still 0 -> the count fails first:
    `repo/manifest.json lists 1 version(s), expected 0.`
  - `PublishedVersionsToday = 1` and the Pages-style entry
    `https://AlphaGit.github.io/jellyfin-new-releases/new-releases/new-releases_0.2.0.0.zip` ->
    `Assert.All() Failure: 1 out of 1 items in the collection did not pass.` /
    `Assert.StartsWith() Failure: String start does not match`
  - `PublishedVersionsToday = 1` and the address the `0.2.0` release will write,
    `https://github.com/AlphaGit/jellyfin-new-releases/releases/download/v0.2.0/new-releases.zip`
    -> `Passed!`, so the rule accepts the first real entry
- T034 carries no behaviour marker, so this loop leaves its checkbox for `/speckit-implement`
- commit: see the commit that carries this entry

## Maintainer decision, 2026-10-04: test-after behaviours accepted

The maintainer accepted, on 2026-10-04, the following behaviours as **test-after** (task T035,
`tdd/verification.md` finding 4). Each had no red before its code: its test passed on the first run
against code an earlier cycle had written. Each is labelled here with that evidence and the
recorded mutant its test catches:

| Behaviour | Cycle | Why no red was possible | Recorded mutant caught |
| --- | --- | --- | --- |
| U2 | 4 | `Plugin.Name` was already `New Releases` | `Name` = `"JELLYFIN New Releases"` |
| A5 | 5 | closed after `U1` made the names agree | `build.yaml` `name` = `"New Releases Tracker"` |
| U9 | 9 | cycle 8's tag rule already rejected it | tag-to-version line deleted |
| U10 | 10 | cycle 7's exact rule already rejected it | Pages-era rule put back |
| U11 | 11 | cycle 7's exact rule already rejected it | asset `"/{Slug}_{number}.zip"` |
| U12 | 12 | cycle 7 moved its literals to release roots | root taken from the entry itself |
| U15 | 15 | cycles 13–14 already ordered the steps | release step moved after `repo add` |
| U19 | 19 | cycle 18 wrote the notes line first | notes line moved below `gh release create` |
| A6 | 20 | closed after `U7`–`U19` | uploaded file renamed; release after `repo add` |
| U21 | 22 | cycle 21 removed the Pages sentence | Pages address added beside the raw one |
| U25 | 25 | the `mv` line dates from cycle 13 | audit M5 |
| U26 | 26 | the `mv` target dates from cycle 13 | audit M14 |
| U27 | 27 | the asset check dates from cycle 8 | audit M12 |

## Cycle 28: U28 the package is moved between building and releasing it

- test: `tests/Jellyfin.Plugin.NewReleases.Tests/Packaging/ReleaseWorkflowTests.cs::ReleaseWorkflow_MovesThePackageBetweenBuildingAndReleasingIt` (new;
  second audit finding 19, task T048)
- red: none on the first run; the order dates from cycle 13. **Test-after**, stated as such: the
  code existed when the second audit found the order unpinned.
  `dotnet test --configuration Release --filter "FullyQualifiedName~ReleaseWorkflowTests.ReleaseWorkflow_MovesThePackageBetweenBuildingAndReleasingIt" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Passed!  - Failed:     0, Passed:     1`
- mutant: survivor S1, the `mv` and `gh release create` lines swapped. Same command ->
  `the release uploads the package before it is moved` (1 failed). Restored from a file copy,
  verified with `cmp -s`
- green: no workflow change. Suite -> 341 passed, 0 failed
- refactor: none
- commit: see the commit that carries this entry

## Cycle 29: U29 the package is moved from the folder `jprm plugin build --output` names

- test: `tests/Jellyfin.Plugin.NewReleases.Tests/Packaging/RepositoryManifestTests.cs::TheReleaseWorkflow_MovesThePackageFromTheFolderJprmWritesTo` (new;
  second audit finding 20, task T049)
- red: none on the first run; both folders are `./artifacts` since before 006. **Test-after**,
  stated as such.
  `dotnet test --configuration Release --filter "FullyQualifiedName~RepositoryManifestTests.TheReleaseWorkflow_MovesThePackageFromTheFolderJprmWritesTo" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Passed!  - Failed:     0, Passed:     1`
- mutant: survivor S2, `--output ./out`. Same command -> `Assert.Equal() Failure: Strings differ` /
  `Expected: "./out"` / `Actual:   "./artifacts"` (1 failed). Restored from a file copy, verified with `cmp -s`
- green: no workflow change. Suite -> 342 passed, 0 failed
- refactor: none
- commit: see the commit that carries this entry

## `/speckit-implement`, after the second audit: T037 and T044 (and finding 26)

- `ASourceUrlOffTheSiteOrNamingAnotherVersion_IsRejected` reads its rows from
  `RejectedSourceUrls`, built from `Slug`. `003`'s rows move to the release shape, so each differs
  from a valid address in its stated reason only. The off-site row uses `AnotherReleaseRoot`, the
  same length as `ExampleReleaseRoot`. The same-length asset row is derived from `Slug`, not the
  literal `new_releases`. Seven rows before, seven after; suite 342 passed
- evidence, each check deleted in turn (row names read from TRX, which truncates them as the
  console does, so the rows are named by count and by construction):
  - `StartsWith(tagStart…)` deleted -> 2 rows fail: `https://another.invalid/…` (the other root)
    and the row with no tag directory
  - `EndsWith(asset…)` deleted -> 1 row fails: the same-length asset
  - `Equal(number, InFourParts(…))` deleted -> 2 rows fail: another version, and `1.2.3.1` against `v1.2.3`
  - the Pages-layout and versioned-asset rows are rejected by two checks each, as their comments say
  - each restored from a file copy, `cmp -s`; suite back to 342 passed

## `/speckit-implement`: T038, the permissions rule as a predicate with a table

- `ReleaseWorkflow_GrantsNoPagesPermission` becomes one `[Fact]` over the predicate
  `GrantsPagesOrIdToken`; the new `[Theory]` `GrantsPagesOrIdToken_ReadsEveryWayOfGrantingThem`
  holds seven rows written before the predicate changed: two that grant nothing
  (`contents: write`, `read-all`) and five that grant (`pages:`, `id-token:`, `write-all`, and the
  flow form with each key)
- red, with the predicate still today's regex:
  `dotnet test --configuration Release --filter "FullyQualifiedName~ReleaseWorkflowTests.GrantsPagesOrIdToken_ReadsEveryWayOfGrantingThem|FullyQualifiedName~ReleaseWorkflowTests.ReleaseWorkflow_GrantsNoPagesPermission" -- RunConfiguration.TreatNoTestsAsError=true`
  -> 3 `[FAIL]`: `permissions: { id-token: write }`, `permissions: { contents: write, pages: write }`,
  `permissions: write-all` (3 failed, 5 passed)
- green: the predicate also matches `permissions: write-all` and a flow mapping naming either key.
  Suite -> 348 passed (the two-case theory became one fact; seven rows added)
- against the real workflow: `pages: write` added -> `the release job can still publish a site or
  mint an identity token`; `permissions: write-all` -> the same failure. Both restored, `cmp -s`

## `/speckit-implement`: T039, the release notes' dataflow is tested

- red (node): `node --test tests/web/changelog-entry.test.js` -> `not ok 7 - with --notes, the
  tagged version's section is written to the named file and build.yaml is not read` /
  `Error: ENOENT: no such file or directory, open 'build.yaml'` / `expected: 0` `actual: 1`
- green: `.github/scripts/changelog-entry.js` gains `<version> --notes <file>`, which writes the
  section to the file and leaves `build.yaml` alone. Node -> 359 passed
- mutant: the script's arguments read as `[notes, flag, version]` -> `not ok 7` (1 failed);
  restored, `cmp -s`. This is the argument-order swap the second audit said the old regex missed
- red (C#), U18 and U19 restated to the script call before the workflow changed:
  `ReleaseWorkflow_GivesTheReleaseTheTaggedVersionsChangelogSection [FAIL]`
  (`Assert.Matches() Failure: Pattern not found in value`) and
  `ReleaseWorkflow_WritesTheReleaseNotesBeforeCreatingTheRelease [FAIL]` (`the release workflow has
  no step containing: changelog-entry.js "${{ steps.ver.outputs.version }}" --notes`)
- green: `package.yml:81` runs `node .github/scripts/changelog-entry.js "${{ steps.ver.outputs.version }}" --notes ./artifacts/release-notes.md`.
  dotnet -> 348 passed; node -> 359 passed. Run by hand for `0.2.0`, it writes the section that
  begins `### Upgrading from 0.1.x — once`

## `/speckit-implement`: T040 and T042, the 0.2.0 notes tested through the real `entryFor`

- `U22` and `U23` move from C# to `tests/web/changelog-entry.test.js`, reading `CHANGELOG.md`
  through the exported `entryFor` — the function the release and the catalogue publish with —
  instead of `DocumentationTests.ChangelogSection`, a C# copy without its trim and empty-section
  rule. `ChangelogSection`, `Changelog_020_NamesTheOldNameDirectoryToRemove` and
  `Changelog_020_NamesTheRepositoryAddressThatReplacesTheOldOne` are removed; the behaviours stay
  tested, and `U22` now asserts more (T042: "once" and "nothing else", which its comment claimed)
- first run: node `# pass 9`, the content already being there; each mutant then fails the right test:
  - M10, the folder name removed from the 0.2.0 section -> `not ok 8 - the 0.2.0 notes name the old-name folder…`
  - M11, the address's `main` -> `master` -> `not ok 9 - the 0.2.0 notes give the raw catalogue address…`
  - "once" removed from the 0.2.0 section -> `not ok 8`
  - each restored, `cmp -s`
- suite: dotnet -> 346 passed (the two C# tests removed); node -> 361 passed

## `/speckit-implement`: T041, the canonical name pinned

- `PluginSanityTests.Plugin_DisplayName_IsStable` asserts `Plugin.Name` is `New Releases`, beside
  `Plugin_Guid_IsStable`. It passes on its first run; the name has been `New Releases` throughout
- mutant: `Plugin.cs`, `build.yaml` and the catalogue all renamed to `Release Radar` together ->
  `Plugin_DisplayName_IsStable [FAIL]`. The five workflow-slug tests fail too, because the
  workflow still names `new-releases`; with the workflow renamed as well, this test is the one
  left to catch it. All three files restored, `cmp -s`; suite -> 347 passed

## `/speckit-implement`: T043, the workflow read on use

- `RepositoryManifestTests.ReleaseWorkflowSteps` becomes a property that reads the workflow when a
  test asks for it, instead of a static field read in the type initializer. Suite -> 347 passed
- check: `.github/workflows/package.yml` moved aside, then
  `--filter "FullyQualifiedName~RepositoryManifestTests"` -> exactly the 5 workflow tests fail and
  the other 21, the predicate units `U7`–`U12`, `U24`, `U27` among them, pass. Before, a missing
  workflow failed the whole class through `TypeInitializationException`. File moved back, `git diff` clean

## `/speckit-implement`: T045, the missing sides of the copied predicates

- `ReleaseRootOf_RejectsAnAddressWithNoTagDirectory` (new). Red:
  `--filter "FullyQualifiedName~RepositoryManifestTests"` -> `ReleaseRootOf_RejectsAnAddressWithNoTagDirectory [FAIL]` /
  `Assert.ThrowsAny() Failure: No exception was thrown`. Green: `ReleaseRootOf` asserts its tag
  directory matches `/v[^/]+$`. Suite -> 350 passed
- `AReleaseTaggedInTwoParts_IsAcceptedForItsFourPartVersion` (new, `v1.2` for `1.2.0.0`) and the
  rejecting row `("1.2.3.4", …v1.2.3.4.0/{Slug}.zip)` pass on their first run, the padding loop
  already handling both. Mutants:
  - `InFourParts` with `if` instead of `while` (pads once) -> `AReleaseTaggedInTwoParts_… [FAIL]`
  - `InFourParts` truncating to four parts -> `…IsRejected(number: "1.2.3.4", …) [FAIL]`
  - each restored, `cmp -s`; suite -> 350 passed

## `/speckit-implement`: T050 and T046, one reason to fail each, and the spelling decision

- `U26` now reads the uploaded file from `gh release create` and requires an `mv` line whose
  destination is that file; it no longer goes through `MovesJprmsPackage`. `U29` reads the `mv`
  source folder for any package file name. So the four workflow tests fail one at a time:
  - M5 (`mv` source `jellyfin-new-releases_…`) -> `TheReleaseWorkflow_MovesTheFileJprmWrites` only
  - M14 (`mv` target `new-release.zip`) -> `TheReleaseWorkflow_UploadsTheFileItMovedThePackageTo` only
  - S1 (`mv` and `gh release create` swapped) -> `ReleaseWorkflow_MovesThePackageBetweenBuildingAndReleasingIt` only
  - S2 (`--output ./out`) -> `TheReleaseWorkflow_MovesThePackageFromTheFolderJprmWritesTo` only
  - each restored, `cmp -s`; suite -> 350 passed
- **Decision on spelling (T046, T050; findings 15 and 22), taken during `/speckit-implement` on
  2026-10-04 and open to the maintainer's override:** the workflow tests keep pinning the exact
  spelling the workflow uses — `${{ github.repository }}`, `${{ steps.ver.outputs.version4 }}` with
  its inner spaces, plain `mv`, `./artifacts`, and the one-line script call. The workflow is this
  project's own file and changes rarely and on purpose; a test that turns red on a deliberate
  rewrite names the line to update, while one widened to accept every equivalent spelling also
  accepts more wrong ones. No test was loosened
