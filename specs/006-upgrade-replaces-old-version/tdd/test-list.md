---
feature: 006-upgrade-replaces-old-version
loop: outside-in
profile: .specify/memory/tdd-profile.md
spec_criteria: 11
planned_at: c0e20a2
updated_at: c0e20a2
suite_baseline: green
---

# Test List: An upgrade leaves exactly one version of the plugin running

`spec_criteria` counts the five acceptance scenarios (`US1-AS1`–`AS4`, `US2-AS1`) and the six
success criteria (`SC-001`–`SC-006`). Four success criteria restate scenarios and are closed with
them; `SC-004` is `US2-AS1`; `SC-006` has no scenario and gets its own outer behaviour, `A6`.

## Behaviour ids here are this feature's own

The packaging tests' doc comments carry `003`'s ids — `U21`, `U25`, `U26`, `U27`, `U39`, `U40`.
Those are **not** the ids below. `tasks.md` names them as "`003`'s U25" and so on.

## A note on the acceptance level available here

`.specify/memory/tdd-profile.md` records `acceptance: null`, and the spec's Assumptions state the
condition itself — two copies of the plugin in one host — cannot be built by the suite. So:

- `A1`–`A4` are **host-level and outside the suite**. They are `BLOCKED` on purpose, not by a
  defect: each is a manual test on the maintainer's own Jellyfin server, run and verified by the
  maintainer in the real-server pass, `tasks.md` T026. The loop never drives them. Their suite proxy
  is `U1` — the precondition the host's own grouping needs.
- `A5` and `A6` are behaviours **of the suite**, evidenced by recorded deliberate mutants
  (`quickstart.md` pass 1, scenarios 1 and 2), not by a test file of their own. `005`'s `A9` and
  `A10` set the precedent.

## Outer loop: acceptance behaviors

| id | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| A1 | A server upgraded from the previous release and restarted runs only the new release | US1-AS1, SC-001, FR-001 | example | DONE | manual, maintainer's pass, `docs/real-server-upgrade-0.2.0.md` |
| A2 | An upgraded server restarted five times runs the same single release every time | US1-AS2, SC-002, FR-002 | example | DONE | manual, maintainer's pass, `docs/real-server-upgrade-0.2.0.md` |
| A3 | On an upgraded server the New Releases view renders, on every restart | US1-AS3 | example | DONE | manual, maintainer's pass, `docs/real-server-upgrade-0.2.0.md` |
| A4 | An upgrade after the renaming release needs no operator step beyond the restart | US1-AS4, SC-003, FR-003, FR-007a | example | DONE | manual, maintainer's pass, `docs/real-server-upgrade-0.2.0.md` |
| A5 | Changing the plugin's name in `build.yaml` alone fails the suite | US2-AS1, SC-004, FR-005 | example | DONE | deliberate mutant, cycle 5 of `tdd/cycle-log.md` |
| A6 | Renaming the release asset, or creating the release after the catalogue entry, fails the suite | SC-006 | example | DONE | deliberate mutants, cycle 20 of `tdd/cycle-log.md` |

## Inner loop: unit behaviors

### `build.yaml` and `src/Jellyfin.Plugin.NewReleases/Plugin.cs` — the displayed name

Hosted by `tests/Jellyfin.Plugin.NewReleases.Tests/PluginSanityTests.cs`: constructing `Plugin` sets
the static `Plugin.Instance`, so these sit in its `ProcessGlobalStateCollection`.

| id | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U1 | The name `build.yaml` declares equals, ordinally, the name the constructed plugin reports | FR-004, FR-005 | example | DONE | `PluginSanityTests.cs::Plugin_DisplayName_MatchesTheNameThePackageDeclares` |
| U2 | The plugin's displayed name does not contain "Jellyfin" in any letter case | FR-004 | example | DONE | `PluginSanityTests.cs::Plugin_DisplayName_DoesNotClaimToBeJellyfin` |
| U3 | The assembly identity `Jellyfin.Plugin.NewReleases` still names the Plugin Pages entry id, the admin page's embedded resource, the shipped assembly and the migration resources | FR-004a | example | DONE | `Integration/PluginPagesRegistrationTests.cs::StartAsync_SendsThePageEntryFromTheDataModel_AndNoIsEnabledFields`, `PluginSanityTests.cs::GetPages_OffersExactlyOnePage_TheEmbeddedAdminPage`, `Packaging/BuildManifestTests.cs::BuildManifest_ShipsThePluginItsSqliteAssembliesAndTheNativeLibrary`; migrations by every `Storage/` test, which runs them through `PluginDatabase.MigrationPrefix` |

`U1` is ordinal although the host groups case-insensitively: `FR-004` says "identically", and the
stricter rule costs nothing. `U2` passes on its first run — `Plugin.Name` is already `New Releases` —
so its cycle needs the deliberate-mutant check. `U3` is credited, not rewritten: each listed test
asserts the literal string, and a renamed root namespace would turn every `Storage/` test red.

### `repo/manifest.json` — the catalogue

Hosted by `tests/Jellyfin.Plugin.NewReleases.Tests/Packaging/RepositoryManifestTests.cs`, except
`U5`, which constructs `Plugin` and so sits in `PluginSanityTests.cs` beside `U1`.

| id | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U4 | The catalogue lists no published version | FR-009, INV-1 | example | DONE | `Packaging/RepositoryManifestTests.cs::Manifest_EveryVersionCarriesItsDownloadChecksumTimestampAndJellyfin12` (`PublishedVersionsToday` = 0) |
| U5 | The catalogue's plugin entry carries the name the plugin reports | FR-004 | example | DONE | `PluginSanityTests.cs::Plugin_DisplayName_MatchesTheNameTheCatalogueLists` |
| U6 | The catalogue is an array of exactly one plugin carrying the frozen GUID | INV-1 | example | DONE | `Packaging/RepositoryManifestTests.cs::Manifest_IsAnArrayOfOnePlugin_CarryingTheFrozenGuid` |

`U4` is `003`'s `PublishedVersionsToday` restated from 2 to 0. `U6` must stay green through the hand
edit in T003; it is credited because it already asserts exactly this.

### The catalogue's source-address rule

Hosted by `RepositoryManifestTests.cs`'s `AssertSourceUrlNamesItsOwnVersion` and the tests over it
(`003`'s U25, U26, U40). With no published version, the rule binds synthetic entries only until
`0.2.0` publishes; it must therefore be pinned on both sides by its own cases.

| id | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U7 | An entry whose `sourceUrl` is `<root>releases/download/v<tag>/new-releases.zip` for its own version is accepted | FR-010 | example | DONE | `Packaging/RepositoryManifestTests.cs::AWellFormedEntry_SourceUrlIsAccepted` |
| U8 | A three-part tag `v1.2.3` is accepted for version `1.2.3.0` | FR-010 | example | DONE | `Packaging/RepositoryManifestTests.cs::AReleaseTaggedInThreeParts_IsAcceptedForItsFourPartVersion` |
| U9 | A release address naming another tag is rejected | FR-010, FR-009 | example | DONE | `Packaging/RepositoryManifestTests.cs::ASourceUrlOffTheSiteOrNamingAnotherVersion_IsRejected("1.2.3.1", …v1.2.3/new-releases.zip)` |
| U10 | A Pages-style `…/<slug>/<slug>_<version>.zip` address is rejected | FR-010, FR-012 | example | DONE | `Packaging/RepositoryManifestTests.cs::ASourceUrlOffTheSiteOrNamingAnotherVersion_IsRejected("1.0.0.0", …new-releases/new-releases_1.0.0.0.zip)` |
| U11 | An asset carrying the version in its file name, `new-releases_<version>.zip`, is rejected under the right tag | FR-010 | example | DONE | `Packaging/RepositoryManifestTests.cs::ASourceUrlOffTheSiteOrNamingAnotherVersion_IsRejected("1.0.0.0", …v1.0.0.0/new-releases_1.0.0.0.zip)` |
| U12 | Two entries under two different release roots are rejected | FR-010 | example | DONE | `Packaging/RepositoryManifestTests.cs::EntriesFromTwoDifferentSites_AreRejected` |
| U24 | The release root of an entry is its address above the tag directory | FR-010 | example | DONE | `Packaging/RepositoryManifestTests.cs::ReleaseRootOf_ReturnsTheAddressAboveTheEntrysTagDirectory` |
| U27 | A release address with the right tag and a wrong asset name is rejected | FR-010 | example | DONE | `Packaging/RepositoryManifestTests.cs::ASourceUrlOffTheSiteOrNamingAnotherVersion_IsRejected("1.0.0.0", …v1.0.0.0/new_releases.zip)` |

`U8` and `U9` are the two sides of the tag-to-version boundary: `v1.2.3` against `1.2.3.0` passes,
against `1.2.3.1` fails. `U12` restates `003`'s U40 to release roots. `U24` was added in the loop, before `U7`: with `003`'s U41 the root
is the directory holding the file, which under release addresses includes the tag, so no two
versions could ever share one root and `003`'s U25 would fail the day a second version publishes.
It restates `003`'s U41.

### `.github/workflows/package.yml` — the release workflow

Hosted by `Packaging/ReleaseWorkflowTests.cs`, except `U13`, which restates `003`'s U39 in
`RepositoryManifestTests.cs` beside the slug it derives. Comment lines are excluded from every
ordering and presence check, as `ReleaseWorkflowTests.Steps` already does.

| id | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U13 | The workflow uploads `{Slug}.zip` to the version's GitHub Release with `gh release create` | FR-010 | example | DONE | `Packaging/RepositoryManifestTests.cs::TheDerivedSlug_MatchesTheFilenameTheReleaseWorkflowBuilds` |
| U14 | `jprm repo add` is given `--plugin-url` naming `releases/download/${GITHUB_REF_NAME}/{Slug}.zip` | FR-010 | example | DONE | `Packaging/RepositoryManifestTests.cs::TheReleaseWorkflow_PointsTheCatalogueAtTheReleaseAsset` |
| U15 | `jprm plugin build` runs before `gh release create`, which runs before `jprm repo add`, which runs before `git commit` | FR-010, FR-011 | example | DONE | `Packaging/ReleaseWorkflowTests.cs::ReleaseWorkflow_BuildsReleasesAddsToTheManifestAndCommits_InThatOrder` |
| U16 | No step uses `actions/configure-pages`, `actions/upload-pages-artifact` or `actions/deploy-pages` | FR-012 | example | DONE | `Packaging/ReleaseWorkflowTests.cs::ReleaseWorkflow_DeploysNoPagesSite` (3 cases) |
| U17 | The workflow's permissions grant neither `pages` nor `id-token` | FR-012 | example | DONE | `Packaging/ReleaseWorkflowTests.cs::ReleaseWorkflow_GrantsNoPagesPermission` (2 cases) |
| U18 | `gh release create` takes `--notes-file` written by `entryFor` for `${{ steps.ver.outputs.version }}` | FR-011 | example | DONE | `Packaging/ReleaseWorkflowTests.cs::ReleaseWorkflow_GivesTheReleaseTheTaggedVersionsChangelogSection` |
| U19 | The release notes are written before `gh release create` runs | FR-011 | example | DONE | `Packaging/ReleaseWorkflowTests.cs::ReleaseWorkflow_WritesTheReleaseNotesBeforeCreatingTheRelease` |
| U25 | The workflow moves the file JPRM writes, `{Slug}_${{ steps.ver.outputs.version4 }}.zip`, and no other | FR-010 | example | DONE | `Packaging/RepositoryManifestTests.cs::TheReleaseWorkflow_MovesTheFileJprmWrites` |
| U26 | The file the workflow moves the package to is the file `gh release create` uploads | FR-010 | example | DONE | `Packaging/RepositoryManifestTests.cs::TheReleaseWorkflow_UploadsTheFileItMovedThePackageTo` |
| U28 | The workflow moves the package after `jprm plugin build` and before `gh release create` | FR-010 | example | DONE | `Packaging/ReleaseWorkflowTests.cs::ReleaseWorkflow_MovesThePackageBetweenBuildingAndReleasingIt` |
| U29 | The folder the workflow moves the package from is the folder `jprm plugin build --output` names | FR-010 | example | DONE | `Packaging/RepositoryManifestTests.cs::TheReleaseWorkflow_MovesThePackageFromTheFolderJprmWritesTo` |
| U30 | The `mv` that produces the uploaded file is the one that moves JPRM's package | FR-010 | example | DONE | `Packaging/RepositoryManifestTests.cs::TheReleaseWorkflow_UploadsJprmsPackageItself` |
| U31 | A flow-form `permissions` mapping that names neither `pages` nor `id-token` grants neither | FR-012 | example | DONE | `Packaging/ReleaseWorkflowTests.cs::GrantsPagesOrIdToken_ReadsEveryWayOfGrantingThem("permissions: { contents: write }\n", false)` |
| U32 | A quoted `permissions: "write-all"` grants `pages` and `id-token` | FR-012 | example | DONE | `Packaging/ReleaseWorkflowTests.cs::GrantsPagesOrIdToken_ReadsEveryWayOfGrantingThem("permissions: \"write-all\"\n", true)` |

`U13` must anchor the file name at a path or quote boundary. Cycle 3 found `003`'s U39 is a bare
substring check: after the rename, `new-releases_<v>.zip` is found inside the stale
`jellyfin-new-releases_<v>.zip`, so the wrong workflow path passes it.

`U15` restates `003`'s U27, which ends at `actions/deploy-pages` today.

`U30`–`U34` were added after the audit of `2b97421` (findings 27–29, survivors F1, F2).
`U28`–`U29` were added after the audit of `c210cd1` (findings 19–20, survivors S1, S2).
`U25`–`U27` were added after the audit of `74313fc` (`tdd/verification.md` findings 1–2, mutants
M5, M14, M12 survived). `U25` restores the half of `003`'s U39 that tied the slug to the file JPRM
writes; `U26` ties the moved file to the uploaded one; `U27` pins the asset-name check in
`AssertSourceUrlNamesItsOwnVersion`. `003`'s U28, U33, U51 and the
tag-trigger test are untouched by this feature and must stay green; they trace to `003`, not here.

### `README.md` — the install step

Hosted by `Packaging/DocumentationTests.cs`, beside `003`'s U30.

| id | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U20 | The `## Install` section names `https://raw.githubusercontent.com/AlphaGit/jellyfin-new-releases/main/repo/manifest.json` | FR-012 | example | DONE | `Packaging/DocumentationTests.cs::Readme_InstallNamesTheRawCatalogueAddress` |
| U21 | The `## Install` section names no `github.io` address | FR-012 | example | DONE | `Packaging/DocumentationTests.cs::Readme_InstallNamesNoPagesAddress` |

### `CHANGELOG.md` — the renaming release's notes

Hosted by `Packaging/DocumentationTests.cs`. Read through the same `## <version>` section rule
`entryFor` uses, so what is tested is what the release and the catalogue will publish.

| id | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U22 | The `0.2.0` section names the `Jellyfin New Releases_` directory as the one to remove once | FR-007 | example | DONE | `tests/web/changelog-entry.test.js::the 0.2.0 notes name the old-name folder, and say to remove it once and nothing else` |
| U23 | The `0.2.0` section names the raw catalogue address as the repository address that replaces the old one | FR-007 | example | DONE | `tests/web/changelog-entry.test.js::the 0.2.0 notes give the raw catalogue address as the one that replaces the old address` |
| U33 | The `0.2.0` removal instruction itself says to remove the old-name folder and nothing else, once | FR-007 | example | DONE | `tests/web/changelog-entry.test.js::the 0.2.0 removal instruction deletes the old-name folder and nothing else, and only this once` |
| U34 | The `0.2.0` replacement instruction itself names the raw catalogue address as the replacement | FR-007 | example | DONE | `tests/web/changelog-entry.test.js::the 0.2.0 replacement instruction swaps the old github.io address for the raw catalogue address` |

## Recorded invariants

- **INV-1** — the catalogue keeps the plugin's entry, with its GUID, after its versions are
  cleared. From `spec.md` session 2026-10-04, first answer. Rationale: JPRM's `repo add` merges a new
  version into the entry whose GUID matches; with no entry it would append a fresh one, and `003`'s
  U21 would fail on the empty array until the first release.

## Invariants and edge cases still to place

None.

## Out of scope

- **Two copies in one host**, and everything that follows from it: `FR-001`, `FR-002`, `FR-003`,
  `SC-001`–`SC-003`, `SC-005`. The suite cannot load a second copy (spec Assumptions). Closed by T026.
- **`FR-009`'s reinstall.** The suite pins that every entry names its own immutable address
  (`U7`, `U9`); that a server reinstalls from it is T026 step 8.
- **`FR-008`'s recorded rule** in `docs/http-surface.md` and `CLAUDE.md` (T016, T017). Prose for a
  future author, with no observable result a test could assert beyond its presence.
- **`FR-006`, no change to what the plugin serves.** No behaviour here touches `src/`; the existing
  318 and 358 tests are the guard and must stay green.
- **The `007` changes in the `0.2.0` changelog section** (T031). Release prose, not a requirement of
  this feature.
- **The past `0.1.0` changelog line** (T020). An address correction in prose.
- **Turning GitHub Pages off** — the maintainer's manual step, after `0.2.0`.
- **The host's own behaviour** — grouping by name, deleting superseded copies. Measured in
  `research.md` R1–R2, relied on, not re-tested.
- **A real run of the release workflow.** A tag push cannot run hermetically; T025 checks the
  published release by hand (`quickstart.md` scenario 3).

## Verification commands

Copied verbatim from `.specify/memory/tdd-profile.md`:

- Single test (dotnet): `dotnet test --configuration Release --filter "FullyQualifiedName~{name}" -- RunConfiguration.TreatNoTestsAsError=true`
- Full suite (dotnet): `dotnet test --configuration Release`
- One file (node): `node --test tests/web/{file}`
- Full suite (node): `node --test "tests/web/*.test.js"`
- Coverage: none. `coverlet.collector` is not referenced; the audit falls back to trace checking.
- Mutation: none. Stryker.NET is not installed; the audit uses deliberate mutants, restored from
  a file copy and verified with `cmp -s`. **Never `git checkout --`.**

`--` before `RunConfiguration.TreatNoTestsAsError=true` is mandatory. Without it a filter that
matches nothing exits 0, which turns every red into a false green. A shell started before
2026-09-20 may resolve `dotnet@9`; prefix
`PATH=/opt/homebrew/opt/dotnet/bin:$PATH DOTNET_ROOT=/opt/homebrew/opt/dotnet/libexec`.
