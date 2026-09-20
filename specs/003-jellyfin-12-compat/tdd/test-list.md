---
feature: 003-jellyfin-12-compat
loop: outside-in
profile: .specify/memory/tdd-profile.md
spec_criteria: 10
planned_at: a9f1ba4
updated_at: a9f1ba4
suite_baseline: green
---

# Test List: Run on Jellyfin 12

## Trace id key

`spec.md` numbers acceptance scenarios per user story without ids. This list uses:

- `US<n>-AS<m>`: acceptance scenario *m* of user story *n* (10 in total: US1 1–6, US2 1–4).
- `FR-nnn`, `SC-nnn`: functional requirements and success criteria as written in `spec.md`.
- `EC-<n>`: the *n*-th bullet of "Edge Cases" in `spec.md`.
- `CR-<n>`: testable statement *n* of `contracts/plugin-pages-registration.md`.
- `CM-<n>`: testable statement *n* of `contracts/plugin-repository-manifest.md`.

## Gate: the retarget comes first

**No behaviour on this list closes before `T007` is green.** This feature changes what the plugin
runs on, so a behaviour proved under `net9.0` and Jellyfin 10.11.11 proves nothing here. Every row
below is `PENDING`, including the ones whose test already exists and already passes today, because
the evidence this feature needs is a run against the Jellyfin 12 libraries.

Two consequences for the loop:

- A cycle that closes `A3`, `A4`, `A5` or `U1` MUST record, in its `cycle-log.md` entry, that the
  run was `net10.0` against Jellyfin 12.0.0. Without that line the entry proves the old target.
- The `before_implement` hook fires `/speckit-tdd-run` before `/speckit-implement` writes anything,
  which is **before** `T003`–`T007` exist. On that first firing the loop must report this gate and
  stop: the retarget tasks carry no behaviour marker and belong to `/speckit-implement`.

The stack profile's commands still name SDK 9. After `T007` the profile is stale and
`/speckit-tdd-setup refresh` is due before the remaining cycles run.

## Outer loop: acceptance behaviors

One per acceptance scenario, except `US1-AS6`, whose two clauses are two observable results and so
two rows. No host-level acceptance runner exists (`acceptance: null` in the profile), so each
server-side behaviour is an integration test composing the real entry points with substituted
Jellyfin services, as `001`'s `AcceptanceRig` already does.

| id  | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| A1 | The entry point, constructed with the host's application paths and serializer, reports the frozen identity and offers its configuration page | US1-AS1, FR-007 | example | DONE | `PluginSanityTests.cs::Plugin_ConstructedWithHostServices_ReportsItsIdentityAndOffersAConfigurationPage` |
| A2 | Every service the plugin registers resolves from a container holding the Jellyfin 12 host services, the scheduled task and both sources included | US1-AS2, FR-001, SC-001 | example | DONE | `PluginServiceRegistratorTests.cs::RegisterServices_EveryServiceThePluginRegisters_ResolvesFromTheHostContainer` |
| A3 | A music library read through the Jellyfin 12 library reader produces the same Missing, Incomplete and Upcoming results `001` specifies | US1-AS3, FR-002, FR-003, SC-003, EC-4, EC-5 | example | DONE | existing: `Acceptance/BrowseReleasesTests.cs` A1/A7/A8, `Library/LibraryScannerTests.cs` |
| A4 | An authenticated request answers for the list, for the Archive and for a decision, with per-user library access still enforced | US1-AS4, FR-002, SC-002 | example | DONE | existing: `Api/ReleasesControllerTests.cs`, `Api/AdminControllerTests.cs`, `Acceptance/ArchiveTests.cs` |
| A5 | A configuration with every field set round-trips through the host serializer unchanged, and no collection gains a duplicate | US1-AS5, FR-002, SC-002 | example | DONE | existing: `Configuration/PluginConfigurationTests.cs::XmlRoundTrip_FullyChangedConfiguration_IsEqualFieldByField` + `U32` |
| A6 | With the page integration present, the plugin registers its page on start and withdraws it on stop through the integration's own interface | US1-AS6, FR-015, FR-016 | example | DONE | `Integration/PluginPagesRegistrationTests.cs` (U8–U10); closed by running the suite |
| A7 | With the page integration absent, the plugin still starts and everything that does not depend on it still works | US1-AS6, EC-1, FR-008 | example | DONE | `Integration/PluginPagesRegistrationTests.cs` (U11–U13) + the 001/002 suite |
| A8 | A tagged version publishes the repository document and the package it points at, with no manual editing step | US2-AS1, FR-014 | example | DONE | `Packaging/ReleaseWorkflowTests.cs` (U27–U28); proxy, see the list note |
| A9 | Every version the published repository lists carries a download location, a checksum and Jellyfin 12 as its server version | US2-AS2, FR-013, SC-004 | example | DONE | `Packaging/RepositoryManifestTests.cs` (U23–U26) |
| A10 | The built package's own compatibility declaration names Jellyfin 12, matching its entry in the repository document | US2-AS3, FR-005, SC-004 | example | DONE | `Packaging/BuildManifestTests.cs` (U17, U19) |
| A11 | The documentation states the supported Jellyfin version and the repository address an operator adds | US2-AS4, FR-006, SC-006 | example | DONE | `Packaging/DocumentationTests.cs` (U29–U31); proxy, see the list note |

**A8 and A11 are proxies, and the list says so rather than pretending otherwise.** Neither a tag
push nor a reader of the README can be exercised hermetically. `A8` asserts that
`.github/workflows/package.yml` holds the whole chain with no manual step in it; `A11` asserts the
README states the facts `FR-006` requires. Both bite when a step or a sentence is deleted, and both
can stay green while a real release fails. The real run is the maintainer's, and `T035`'s local
JPRM dry run is what checks it by hand.

`FR-010` and `SC-005` — the suite hermetic and warning-free against the Jellyfin 12 libraries — are
evidenced by running the suite, not by an assertion. `T007` records it in the cycle log and `T036`
re-checks it in CI.

## Inner loop: unit behaviors

Grouped by the component from `plan.md` that owns them.

### `src/Jellyfin.Plugin.NewReleases/Plugin.cs`

| id  | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U1 | The plugin reports the frozen GUID `b8a15db8-e368-42c4-9048-390faf0094db` | FR-007, US1-AS1 | example | DONE | `PluginSanityTests.cs::Plugin_Guid_IsStable` |
| U2 | `GetPages` offers exactly one page, the embedded `Web.admin.html` | US1-AS1, FR-001 | example | DONE | `PluginSanityTests.cs::GetPages_OffersExactlyOnePage_TheEmbeddedAdminPage` |
| U3 | Constructing the plugin writes nothing into the plugin-configurations tree | FR-015, US1-AS1 | example | DONE | `PluginSanityTests.cs::Constructing_WritesNothingIntoThePluginConfigurationsTree` |
| U32 | `PluginConfiguration` exposes no collection property, so the `XmlSerializer` round-trip has nothing that could gain a duplicate entry | US1-AS5, FR-002 | example | DONE | `Configuration/PluginConfigurationTests.cs::Configuration_ExposesNoCollectionProperty_SoNothingCanGainADuplicateOnRoundTrip` |

### `src/Jellyfin.Plugin.NewReleases/PluginServiceRegistrator.cs`

| id  | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U4 | Every service the registrator registers resolves from a collection holding substituted Jellyfin 12 host services | US1-AS2, SC-001 | example | DROPPED | duplicate of A2 — same observable, see cycle log |
| U5 | `IEnumerable<IReleaseSource>` yields both MusicBrainz and Deezer, not one of them | US1-AS2 | example | DONE | `PluginServiceRegistratorTests.cs::RegisterServices_BothReleaseSourcesAreRegistered_NotOneOfThemTwice` |
| U6 | `IScheduledTask` resolves as the refresh task | US1-AS2, FR-001 | example | DONE | `PluginServiceRegistratorTests.cs::RegisterServices_TheScheduledTaskResolvesAsTheRefreshTask` |
| U7 | The page-registration hosted service is registered and resolves as an `IHostedService` | FR-015, US1-AS6 | example | DONE | `PluginServiceRegistratorTests.cs::RegisterServices_ThePageRegistrationRunsAsAHostedService` |
| U34 | The gateway the registrator builds can reach a type in a genuinely loaded assembly, not only an injected stand-in source | US1-AS6, FR-015, CR-1 | example | DONE | `Integration/PluginPagesRegistrationTests.cs::TheGatewayTheRegistratorBuilds_CanReachATypeInALoadedAssembly` |

### `src/Jellyfin.Plugin.NewReleases/Integration/PluginPagesRegistrationService.cs`

Reaches the integration through `Integration/PluginPagesGateway.cs`, whose assembly source is an
injected seam so a test supplies the stand-in without touching load contexts.

| id  | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U8 | Starting calls the integration's `RegisterPage` exactly once | CR-1, US1-AS6 | example | DONE | `Integration/PluginPagesRegistrationTests.cs::StartAsync_WithTheIntegrationPresent_CallsRegisterPageExactlyOnce` |
| U9 | The payload carries `Id`, `Url`, `DisplayText` and `Icon` as `data-model.md` states them, and none of the three `IsEnabled*` fields | CR-1, FR-015 | example | DONE | `Integration/PluginPagesRegistrationTests.cs::StartAsync_SendsThePageEntryFromTheDataModel_AndNoIsEnabledFields` |
| U10 | Stopping calls `RemovePage` exactly once with `Jellyfin.Plugin.NewReleases` | CR-2, US1-AS6 | example | DONE | `Integration/PluginPagesRegistrationTests.cs::StopAsync_WithTheIntegrationPresent_CallsRemovePageOnceWithThePluginId` |
| U11 | With no integration assembly loaded, starting and stopping neither throw | CR-3, EC-1, FR-008 | example | DONE | `Integration/PluginPagesRegistrationTests.cs::WithNoIntegrationAssembly_StartingAndStoppingBothSucceed` |
| U12 | With no integration assembly loaded, exactly one message is logged, below error level | CR-3, EC-1 | example | DONE | `Integration/PluginPagesRegistrationTests.cs::WithNoIntegrationAssembly_LogsExactlyOnce_BelowErrorLevel` |
| U13 | When `RegisterPage` throws, starting does not throw and logs exactly one message | CR-4, FR-008 | example | DONE | `Integration/PluginPagesRegistrationTests.cs::WhenRegisterPageThrows_StartingDoesNotThrow_AndLogsOnce` |
| U14 | Starting twice in one process logs at most one message | CR-5, EC-1 | example | DONE | `Integration/PluginPagesRegistrationTests.cs::StartingTwiceWithNoIntegration_LogsAtMostOnce` |
| U15 | After a registration that failed, stopping neither throws nor logs a second message | CR-4 | example | DONE | `Integration/PluginPagesRegistrationTests.cs::AfterAFailedRegistration_StoppingDoesNotThrow_AndLogsNothingFurther` |

### `src/Jellyfin.Plugin.NewReleases/Jellyfin.Plugin.NewReleases.csproj`

| id  | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U16 | The plugin assembly references neither `Jellyfin.Plugin.PluginPages` nor `Newtonsoft.Json` | CR-6, FR-008 | example | DONE | `PluginSanityTests.cs::ThePluginAssembly_ReferencesNeitherPluginPagesNorNewtonsoft` |

### `build.yaml`

| id  | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U17 | Declares `targetAbi: "12.0.0.0"` | CM-5, FR-005, FR-012, SC-004 | example | DONE | `Packaging/BuildManifestTests.cs::BuildManifest_DeclaresJellyfin12AsTheTargetAbi` |
| U18 | Declares `framework: "net10.0"` | CM-5 | example | DONE | `Packaging/BuildManifestTests.cs::BuildManifest_DeclaresNet10AsTheFramework` |
| U19 | Its `guid` equals the GUID compiled into `Plugin.PluginGuid` | CM-5, FR-007 | example | DONE | `Packaging/BuildManifestTests.cs::BuildManifest_GuidMatchesTheOneCompiledIntoThePlugin` |
| U20 | Its `artifacts` names the plugin DLL, the four SQLite assemblies and the native library | FR-009 | example | DONE | `Packaging/BuildManifestTests.cs::BuildManifest_ShipsThePluginItsSqliteAssembliesAndTheNativeLibrary` |

### `repo/manifest.json`

`U22` is the low side of the boundary and `U24`/`U26` the high side: the entry checks pass
vacuously while `versions` is empty, so without a rejecting case they would pin nothing until the
first tag.

| id  | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U21 | Parses as a JSON array of exactly one object whose `guid` is the frozen plugin GUID | CM-1, FR-013 | example | DONE | `Packaging/RepositoryManifestTests.cs::Manifest_IsAnArrayOfOnePlugin_CarryingTheFrozenGuid` |
| U22 | An empty `versions` array is accepted — the state before the first tag | CM-1, FR-014 | example | DONE | `Packaging/RepositoryManifestTests.cs::Manifest_MayListNoVersionsAtAll` |
| U23 | Every entry carries a non-empty `version`, `sourceUrl`, `checksum` and `timestamp`, and a `targetAbi` of `12.0.0.0` | CM-2, FR-013, SC-004 | example | DONE | `Packaging/RepositoryManifestTests.cs::Manifest_EveryVersionCarriesItsDownloadChecksumTimestampAndJellyfin12` |
| U24 | An entry missing one of those fields, or carrying any other `targetAbi`, is rejected by that same check | CM-2, SC-004 | example | DONE | `Packaging/RepositoryManifestTests.cs::AnEntryMissingAFieldOrDeclaringAnotherAbi_IsRejected` |
| U25 | Every `sourceUrl` sits under the published site root and its filename is `jellyfin-new-releases_<version>.zip` for that entry's version | CM-3, FR-013 | example | DONE | `Packaging/RepositoryManifestTests.cs::Manifest_EverySourceUrlSharesOneSiteRoot_AndNamesItsOwnVersion` |
| U26 | A `sourceUrl` whose filename names a different version is rejected | CM-3 | example | DONE | `Packaging/RepositoryManifestTests.cs::ASourceUrlOffTheSiteOrNamingAnotherVersion_IsRejected` |

### `.github/workflows/package.yml`

| id  | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U27 | The workflow builds with JPRM, adds the version with `jprm repo add`, commits `repo/` and deploys it to Pages, in that order and with no manual step between them | US2-AS1, FR-014 | example | DONE | `Packaging/ReleaseWorkflowTests.cs::ReleaseWorkflow_BuildsAddsToTheManifestCommitsAndDeploys_InThatOrder` |
| U28 | The workflow fails the run when the JPRM build leaves the project file modified | CM-6 | example | DONE | `Packaging/ReleaseWorkflowTests.cs::ReleaseWorkflow_FailsTheRunIfPackagingDidNotRestoreTheTargetFramework` |
| U33 | The workflow names the package by the four-part version JPRM writes, not the tag's three-part one | CM-3, FR-014 | example | DONE | `Packaging/ReleaseWorkflowTests.cs::ReleaseWorkflow_NamesThePackageByTheFourPartVersionJprmWrites` |

### `README.md`

| id  | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U29 | States Jellyfin 12 as the supported server version, and no longer claims 10.11.x | FR-006, FR-012, SC-006, EC-2 | example | DONE | `Packaging/DocumentationTests.cs::Readme_StatesJellyfin12_AndNoLongerClaims1011` |
| U30 | Gives the plugin repository URL and where an operator adds it | FR-006, US2-AS4 | example | DONE | `Packaging/DocumentationTests.cs::Readme_GivesTheRepositoryUrlAndWhereToAddIt` |
| U31 | States the minimum Plugin Pages version the new registration needs | FR-006, FR-015 | example | DONE | `Packaging/DocumentationTests.cs::Readme_StatesTheMinimumPluginPagesVersion` |

## Invariants and edge cases still to place

None. Every edge case in `spec.md` is placed or explicitly out of scope: `EC-1` on `A7`, `U11`,
`U12` and `U14`; `EC-4` and `EC-5` on `A3`; `EC-2` on `U17` and `U29`; `EC-3` below.

## Out of scope

- **Verification on a running Jellyfin 12 server.** `spec.md` puts it outside this feature. The
  feature is complete when the suite is green against the Jellyfin 12 libraries.
- **`CM-4` (the recorded checksum equals the MD5 of the built zip) and `CM-7` (a second version
  merges into a manifest that already holds one).** Both need a real JPRM run. `T035`'s local dry
  run checks them by hand; neither can be hermetic.
- **`EC-3`, a later Jellyfin 12 point release.** `targetAbi` is a floor in Jellyfin's own catalogue
  logic, not in this plugin. `U17` pins the floor; there is nothing else here to assert.
- **`EC-2`, a sideload onto a server older than Jellyfin 12.** The plugin is not required to work.
  `U17` keeps the catalogue from offering it and `U29` makes the cause findable.
- **Characterization tests for `Plugin.TryRegisterPluginPagesEntry` and `IsPluginPagesInstalled`.**
  Untested code, but `T023` deletes it rather than changing it, so a captured baseline would be
  thrown away in the same feature. `U3` pins what replaces it: the plugin writes nothing into
  another plugin's configuration.
- **The embedded web pages.** Unchanged by this feature (`spec.md` Assumptions, research `R7`).
  `tests/web/` runs as it stands; its 33 tests are part of the green the gate needs.
- **`FR-004`, no forward migration.** A requirement that nothing happens. The plugin has never been
  released, so there is no prior state for a test to start from.
- **`FR-011`, CI verifying against the Jellyfin 12 libraries on every push.** A property of the
  workflow run, not of the plugin. `T009` retargets it and `T036` verifies the run is green.
- **The constitution amendment (`T010`), `CLAUDE.md` (`T011`) and `CHANGELOG.md` (`T034`).**
  Governing documents, not behaviour. No test.
- **Every behaviour `001` and `002` specify, individually.** `FR-002` requires them all to hold,
  and `A3`, `A4` and `A5` carry that requirement through their existing tests. Re-listing 195
  behaviours here would duplicate two test lists that already exist.

## Verification commands

Copied verbatim from `.specify/memory/tdd-profile.md` at planning time. **They name SDK 9 and will
be stale from `T007` onward** — see the gate above.

Server side:

- Single test: `dotnet test --configuration Release --filter "FullyQualifiedName~{name}" -- RunConfiguration.TreatNoTestsAsError=true`
- Full suite: `dotnet test --configuration Release`
- Coverage: not available (`coverage: null`)
- Mutation: not available (`mutation: null`); the loop uses deliberate mutants

Page side:

- Single test: `node --test --test-name-pattern "<name>" "tests/web/*.test.js"`
- Full suite: `node --test "tests/web/*.test.js"`

`{name}` is `Class.Method` for xunit. In a shell that did not source `~/.zshenv`, prefix the dotnet
commands with `PATH=/opt/homebrew/opt/dotnet@9/bin:$PATH DOTNET_ROOT=/opt/homebrew/opt/dotnet@9/libexec`
— and with the `dotnet@10` equivalents once `T003`–`T007` have landed.
