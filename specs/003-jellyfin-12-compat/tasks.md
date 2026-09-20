---

description: "Task list for 003-jellyfin-12-compat"
---

# Tasks: Run on Jellyfin 12

**Input**: Design documents from `/specs/003-jellyfin-12-compat/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/](./contracts/)

**Tests**: Required. Constitution principle II is non-negotiable, and the `tdd` extension's
`before_implement` hook drives `/speckit-tdd-run` over the behaviour changes first.

**Organization**: Grouped by user story so each is independently completable and testable.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: `[US1]` plugin runs on Jellyfin 12, `[US2]` publishing chain
- Exact file paths in every task

## Path Conventions

Single project: `src/Jellyfin.Plugin.NewReleases/`, `tests/Jellyfin.Plugin.NewReleases.Tests/`,
`tests/web/` at repository root. New: `repo/` for the published plugin repository.

---

## Phase 1: Setup

**Purpose**: Get the toolchain in place and record a known-good starting point.

- [X] T001 Install the .NET 10 SDK with `brew install dotnet` and confirm `dotnet --list-sdks` lists a `10.0.x` entry alongside the existing 8 and 9
- [X] T002 Record the green baseline before anything changes: run `dotnet build --configuration Release`, `dotnet test --configuration Release` and `node --test "tests/web/*.test.js"` on the current `net9.0` target and note the passing test count

**Checkpoint**: .NET 10 available, current suite known green.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The retarget. Nothing compiles against Jellyfin 12 until this phase is done, so no
user story work can start.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T003 Retarget the plugin project in `src/Jellyfin.Plugin.NewReleases/Jellyfin.Plugin.NewReleases.csproj`: `<TargetFramework>net10.0</TargetFramework>`; `Jellyfin.Controller` and `Jellyfin.Model` to `12.0.0`; add direct `Jellyfin.Data` and `Jellyfin.Database.Implementations` at `12.0.0`, all four with `ExcludeAssets=runtime` and `PrivateAssets=all`; `Microsoft.Data.Sqlite` to `10.0.11` (pins and dates in [research.md](./research.md) R4)
- [X] T004 [P] Retarget the test project in `tests/Jellyfin.Plugin.NewReleases.Tests/Jellyfin.Plugin.NewReleases.Tests.csproj`: `net10.0`; `Jellyfin.Controller`, `Jellyfin.Model`, `Jellyfin.Data` and `Jellyfin.Database.Implementations` at `12.0.0` with `PrivateAssets=all` and no `ExcludeAssets` (the test host needs the runtime assets); `Microsoft.NET.Test.Sdk` to `18.9.0`; `xunit.runner.visualstudio` to `3.1.5`; leave `xunit` at `2.9.3` and `NSubstitute` at `5.3.0`
- [X] T005 Regenerate both lock files with `dotnet restore --force-evaluate`, then confirm `src/Jellyfin.Plugin.NewReleases/packages.lock.json` and `tests/Jellyfin.Plugin.NewReleases.Tests/packages.lock.json` resolve `Jellyfin.Data` and `Jellyfin.Database.Implementations` as Direct at `12.0.0`
- [X] T006 Build `dotnet build --configuration Release` and fix every error and warning it reports. `TreatWarningsAsErrors` stays on and new `net10.0` analyzer warnings get fixed, never suppressed (constitution VI)
- [X] T007 Run `dotnet test --configuration Release` and `node --test "tests/web/*.test.js"` and get back to the T002 passing count with zero warnings — this is the evidence for User Story 1 scenarios 3, 4 and 5, which existing tests already cover Behaviors: [A3] [A4] [A5]
- [X] T008 [P] Update `build.yaml`: `targetAbi: "12.0.0.0"` and `framework: "net10.0"`, leaving `guid`, `name` and the `artifacts` list untouched (the SQLite artefact list does not change — [research.md](./research.md) R4)
- [X] T009 [P] Update `.github/workflows/build.yml` to `dotnet-version: '10.0.x'` in the `actions/setup-dotnet@v6` step
- [X] T010 Amend the constitution with `/speckit-constitution`: principle IV's "pinned to the exact server version in production (10.11.x)" becomes Jellyfin 12.0.x, and Technical Constraints' "C# on `net9.0`, matching the Jellyfin 10.11 host" becomes `net10.0` matching the Jellyfin 12 host. Bump to 1.3.0 with a Sync Impact Report. This closes the deviation recorded in [plan.md](./plan.md) Complexity Tracking. While in there, decide whether to record the standing rule that the plugin takes a host's or integration's newest supported interaction over an older tolerated one — ask before adding it, it is a new principle rather than a version correction
- [X] T011 [P] Update `CLAUDE.md`: the conventions line naming `net9.0` and `Jellyfin.Controller`/`Jellyfin.Model` at 10.11.11, and the Build section's ".NET 9 SDK" requirement

**Checkpoint**: The plugin builds and the whole suite passes against the Jellyfin 12 libraries, and the project's own governing documents say so.

---

## Phase 3: User Story 1 - The plugin works on a Jellyfin 12 server (Priority: P1) 🎯 MVP

**Goal**: The plugin loads, registers everything it owns, and reaches the web client's menu through
the integration's supported interface instead of another plugin's configuration file.

**Independent Test**: Build against the Jellyfin 12 libraries and run the suite — construct the
plugin entry point, resolve every service it registers, drive the refresh and the API, round-trip
the configuration, and register and withdraw the page against a stand-in integration.

### Tests for User Story 1 ⚠️

> Write these first and watch each one fail for the right reason. Record the red in
> `specs/003-jellyfin-12-compat/tdd/cycle-log.md`.

- [X] T012 [P] [US1] Test that the plugin entry point constructs against the Jellyfin 12 libraries, reports the frozen GUID, and offers its configuration page — extend `tests/Jellyfin.Plugin.NewReleases.Tests/PluginSanityTests.cs` (spec scenario 1) Behaviors: [A1] [U1] [U2]
- [X] T013 [P] [US1] Test that running `PluginServiceRegistrator` against a service collection holding substituted Jellyfin host services resolves every service it registers, including the scheduled task and both release sources, in a new `tests/Jellyfin.Plugin.NewReleases.Tests/PluginServiceRegistratorTests.cs` (spec scenario 2) Behaviors: [A2] [U4] [U5] [U6]
- [X] T014 [P] [US1] Test that the plugin project references neither `Jellyfin.Plugin.PluginPages` nor `Newtonsoft.Json`, by asserting over the referenced assemblies of the plugin assembly, in `tests/Jellyfin.Plugin.NewReleases.Tests/PluginSanityTests.cs` (contract statement 6) Behaviors: [U16]
- [X] T015 [P] [US1] Build the stand-in integration in `tests/Jellyfin.Plugin.NewReleases.Tests/Support/FakePluginPages.cs`: a `Jellyfin.Plugin.PluginPages.PluginInterface` type with static `RegisterPage(<payload>)` and `RemovePage(string)`, and a payload type exposing a static `Parse(string)`, recording every call so tests can assert on it Behaviors: [U8] [U9] [U10] [U13] [U14] [U15]
- [X] T037 [P] [US1] Test that constructing the plugin creates nothing under `IApplicationPaths.PluginConfigurationsPath` — no directory and no file — in `tests/Jellyfin.Plugin.NewReleases.Tests/PluginSanityTests.cs` (FR-015) Behaviors: [U3]
- [X] T016 [US1] Test that starting the registration service calls `RegisterPage` exactly once with `Id`, `Url`, `DisplayText` and `Icon` matching [data-model.md](./data-model.md), in a new `tests/Jellyfin.Plugin.NewReleases.Tests/Integration/PluginPagesRegistrationTests.cs` (contract statement 1) Behaviors: [A6] [U8] [U9]
- [X] T017 [US1] Test that stopping the registration service calls `RemovePage` exactly once with `Jellyfin.Plugin.NewReleases`, in `tests/Jellyfin.Plugin.NewReleases.Tests/Integration/PluginPagesRegistrationTests.cs` (contract statement 2) Behaviors: [A6] [U10]
- [X] T018 [US1] Test that with no integration assembly present, starting and stopping the service neither throw and log exactly one message, in `tests/Jellyfin.Plugin.NewReleases.Tests/Integration/PluginPagesRegistrationTests.cs` (contract statement 3) Behaviors: [A7] [U11] [U12]
- [X] T019 [US1] Test that when the integration's `RegisterPage` throws, starting the service does not throw and logs exactly one message, and that starting twice still logs at most one, in `tests/Jellyfin.Plugin.NewReleases.Tests/Integration/PluginPagesRegistrationTests.cs` (contract statements 4 and 5) Behaviors: [U13] [U14] [U15]
- [X] T038 [US1] Test that the registrator registers the page-registration hosted service and that it resolves as an `IHostedService`, in `tests/Jellyfin.Plugin.NewReleases.Tests/PluginServiceRegistratorTests.cs` Behaviors: [U7]

### Implementation for User Story 1

- [X] T020 [US1] Implement `src/Jellyfin.Plugin.NewReleases/Integration/PluginPagesGateway.cs`: locate the integration by scanning `AssemblyLoadContext.All.SelectMany(c => c.Assemblies)`, resolve `PluginInterface` and its `RegisterPage`/`RemovePage`, build the argument by calling `Parse(string)` on `RegisterPage`'s own parameter type, and invoke — never naming Newtonsoft or referencing Plugin Pages. Take the assembly source as an injected seam so tests supply the stand-in without touching load contexts Behaviors: [U8] [U9] [U10] [U11]
- [X] T021 [US1] Implement `src/Jellyfin.Plugin.NewReleases/Integration/PluginPagesRegistrationService.cs` as an `IHostedService`: `StartAsync` registers the page, `StopAsync` withdraws it, every failure logged once and swallowed so the plugin always loads Behaviors: [U12] [U13] [U14] [U15]
- [X] T022 [US1] Register the hosted service in `src/Jellyfin.Plugin.NewReleases/PluginServiceRegistrator.cs` with `serviceCollection.AddHostedService<PluginPagesRegistrationService>()` Behaviors: [U7]
- [X] T023 [US1] Delete `TryRegisterPluginPagesEntry` and `IsPluginPagesInstalled` from `src/Jellyfin.Plugin.NewReleases/Plugin.cs`, along with the `PluginPagesEntryVersion` constant and the now-unused `System.Text.Json` usings — the plugin no longer reads or writes another plugin's configuration file Behaviors: [U3] [U16]
- [X] T024 [US1] Refactor on green: confirm `src/Jellyfin.Plugin.NewReleases/Plugin.cs` is back to entry point and configuration page only, and that the whole suite still passes Behaviors: [A6] [A7]
- [X] T039 [US1] Story gate: every acceptance behaviour for User Story 1 is `DONE` in [tdd/test-list.md](./tdd/test-list.md), each closed on cycle-log evidence recorded against `net10.0` and Jellyfin 12.0.0 Behaviors: [A1] [A2] [A3] [A4] [A5] [A6] [A7]

**Checkpoint**: The plugin builds against Jellyfin 12, every service it owns resolves, and the page registration goes through the supported interface and degrades cleanly without it.

---

## Phase 4: User Story 2 - Operators are offered only a build that fits their server (Priority: P2)

**Goal**: The project publishes a plugin repository an operator can point a server at, and the
release chain keeps it current without anyone editing a file.

**Independent Test**: Run the release chain for a tagged version and inspect what it publishes — the
repository document, the package it points at, and the server version both declare.

### Tests for User Story 2 ⚠️

- [X] T025 [P] [US2] Test that `build.yaml` declares `targetAbi: "12.0.0.0"` and `framework: "net10.0"`, that its `guid` equals `Plugin.PluginGuid`, and that its `artifacts` list names the plugin DLL, the four SQLite assemblies and the native library, in a new `tests/Jellyfin.Plugin.NewReleases.Tests/Packaging/BuildManifestTests.cs` (contract statement 5) Behaviors: [A10] [U17] [U18] [U19] [U20]
- [X] T026 [P] [US2] Test that `repo/manifest.json` parses as an array of exactly one object whose `guid` is the frozen plugin GUID, in `tests/Jellyfin.Plugin.NewReleases.Tests/Packaging/RepositoryManifestTests.cs` (contract statement 1) Behaviors: [U21]
- [X] T027 [US2] Test that every entry in `versions` carries a non-empty `version`, `sourceUrl`, `checksum` and `timestamp`, a `targetAbi` of `12.0.0.0`, and a `sourceUrl` whose filename is `jellyfin-new-releases_<version>.zip` for that entry — passing vacuously while `versions` is empty and biting the moment one is added — in `tests/Jellyfin.Plugin.NewReleases.Tests/Packaging/RepositoryManifestTests.cs` (contract statements 2 and 3) Behaviors: [A9] [U22] [U23] [U24] [U25] [U26]
- [X] T040 [P] [US2] Test that `.github/workflows/package.yml` holds the publishing chain in order — `jprm plugin build`, `jprm repo add`, a commit of `repo/`, the Pages deployment — plus the project-file guard, with no manual editing step between them, in a new `tests/Jellyfin.Plugin.NewReleases.Tests/Packaging/ReleaseWorkflowTests.cs`. A proxy for a real tag push: it asserts the workflow's shape, never a run (spec US2 scenario 1, contract statement 6) Behaviors: [A8] [U27] [U28]
- [X] T041 [P] [US2] Test that `README.md` states Jellyfin 12 as the supported server version and no longer claims 10.11.x, gives the plugin repository URL and where an operator adds it, and states the minimum Plugin Pages version the registration needs, in a new `tests/Jellyfin.Plugin.NewReleases.Tests/Packaging/DocumentationTests.cs` (FR-006) Behaviors: [A11] [U29] [U30] [U31]

### Implementation for User Story 2

- [X] T028 [US2] Create `repo/manifest.json` holding one plugin object with the frozen `guid`, the `name`, `description`, `overview`, `owner` and `category` from `build.yaml`, and an empty `versions` array — correct until the first tag ([contracts/plugin-repository-manifest.md](./contracts/plugin-repository-manifest.md)) Behaviors: [U21] [U22]
- [X] T029 [US2] Rewrite `.github/workflows/package.yml` to run on `v*` tags with `contents: write` and the Pages permissions: `actions/setup-dotnet@v6` at `10.0.x`, `jprm plugin build` into `repo/jellyfin-new-releases/`, `jprm repo add` against `repo/manifest.json` with the Pages site URL, then commit `repo/` back to `main` Behaviors: [A8] [U27]
- [X] T030 [US2] Add the Pages deployment to `.github/workflows/package.yml`: `actions/configure-pages@v6`, `actions/upload-pages-artifact@v5` pointed at `repo/`, `actions/deploy-pages@v5` — every version stays listed because `repo/` is committed, not rebuilt Behaviors: [A8] [U27]
- [X] T031 [US2] Add a step to `.github/workflows/package.yml` that fails the run if `git diff --quiet -- src/Jellyfin.Plugin.NewReleases/Jellyfin.Plugin.NewReleases.csproj` reports a change after the JPRM build — JPRM rewrites `<TargetFramework>` while packaging and must leave it as it found it (contract statement 6) Behaviors: [U28]
- [X] T032 [US2] Enable GitHub Pages on the repository with source "GitHub Actions", and record the resulting site URL in `.github/workflows/package.yml` so `jprm repo add` generates matching `sourceUrl` values
- [X] T033 [US2] Update `README.md`: Jellyfin 12 replaces "Jellyfin 10.11.x" in Requirements, add the plugin repository URL and how to add it in Dashboard → Plugins → Repositories, state the minimum Plugin Pages version the new registration needs, and change the Build section to the .NET 10 SDK (FR-006) Behaviors: [A11] [U29] [U30] [U31]
- [X] T042 [US2] Story gate: every acceptance behaviour for User Story 2 is `DONE` in [tdd/test-list.md](./tdd/test-list.md) Behaviors: [A8] [A9] [A10] [A11]

**Checkpoint**: A tagged version would build, land in `repo/`, appear in the manifest with its checksum and `targetAbi`, and be served from GitHub Pages — without anyone editing a file.

---

## Phase 5: Polish & Cross-Cutting Concerns

- [X] T034 [P] Add the Jellyfin 12 entry to `CHANGELOG.md` under Unreleased: the supported server version, the dropped 10.11.x support, and the page-registration change
- [X] T035 Work through [quickstart.md](./quickstart.md) end to end, including the local JPRM dry run into a temporary directory, and confirm every stated expectation
- [ ] T036 Push to `main` and verify the CI run is green with `gh run watch` — the constitution's final gate; a feature is done when its CI run is verified, not when it is pushed

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: no dependencies
- **Foundational (Phase 2)**: needs Phase 1 — **blocks both user stories**, because nothing compiles against Jellyfin 12 until T003–T007 land
- **User Story 1 (Phase 3)**: needs Phase 2
- **User Story 2 (Phase 4)**: needs Phase 2. Independent of User Story 1 — the publishing chain does not care what the plugin code does
- **Polish (Phase 5)**: needs both stories

### Within Phase 2

T003 → T005 → T006 → T007 is a strict chain. T004 runs beside T003. T008, T009 and T011 are
independent files. T010 comes after T007, so the constitution is corrected once the code it
describes is actually true.

### Within User Story 1

T012, T013, T014, T015 and T037 are independent files and run together. T016–T019 all edit one test
file, so they are sequential, and all four need T015. T038 follows T019 and precedes T022.
Implementation T020 → T021 → T022 → T023 → T024 is a chain. Every test must be red before its
implementation task starts. T039 closes the story and needs every one of them.

### Within User Story 2

T025, T026, T040 and T041 are independent; T027 follows T026 in the same file. T028 precedes T026
and T027 having anything to read. T040 precedes T029–T031 and T041 precedes T033, as every test
does. T029 → T030 → T031 edit one workflow file, so they are sequential. T032 is a repository
setting and can happen any time before T029 is first run for real. T033 is independent. T042 closes
the story.

### Parallel Opportunities

- T003 with T004
- T008, T009, T011 together, once T007 is green
- T012, T013, T014, T015, T037 together
- T025, T026, T040 and T041 together
- User Story 1 and User Story 2 in parallel once Phase 2 closes

## Parallel Example: User Story 1

```bash
# The four independent red tests, together:
Task: "Plugin entry point constructs and reports its identity in tests/.../PluginSanityTests.cs"
Task: "Service registration resolves everything in tests/.../PluginServiceRegistratorTests.cs"
Task: "Plugin references neither PluginPages nor Newtonsoft in tests/.../PluginSanityTests.cs"
Task: "Stand-in integration in tests/.../Support/FakePluginPages.cs"
```

## Implementation Strategy

### MVP (User Story 1 only)

1. Phase 1 — toolchain and baseline
2. Phase 2 — the retarget, then the constitution and `CLAUDE.md` corrected to match
3. Phase 3 — the page registration, driven test-first
4. **Stop and validate**: suite green against the Jellyfin 12 libraries, no warnings, no network

That is a plugin that runs on Jellyfin 12. It cannot yet be obtained by anyone, which is what User
Story 2 adds.

### Incremental delivery

1. Phase 1 + 2 → the plugin compiles and tests against Jellyfin 12
2. + User Story 1 → the plugin is correct on Jellyfin 12 (MVP)
3. + User Story 2 → the plugin can be published and installed
4. + Polish → CI verified green

### Notes

- `[P]` means different files and no dependency on unfinished work
- Every behaviour test is observed failing, for the right reason, before its implementation — the
  failure output goes in `specs/003-jellyfin-12-compat/tdd/cycle-log.md`
- Commit after each task or logical group; Conventional Commits, no AI attribution
- This feature tags no release and touches no running server. Both are out of scope by decision

---

## Phase 6: TDD remediation

**The feature is not done until T043–T051 are cleared.** [tdd/verification.md](./tdd/verification.md)
returns `FAIL` on nine `HIGH` findings: a surviving mutant that leaves the page registration
unwired on a real server, two tests that assert nothing in the committed state, and six weak or
tautological assertions. `HIGH` findings first; each names the finding it closes and the command
that proves it done.

- [ ] T043 Close verification finding 1: add a test that resolves `PluginPagesGateway` from the container `PluginServiceRegistrator` populates and confirms it can find a type in a genuinely loaded assembly, so the production `AssemblyLoadContext.All` source is exercised. Prove it by re-applying mutant M4 — `() => []` in `src/Jellyfin.Plugin.NewReleases/PluginServiceRegistrator.cs` — and confirming the new test fails, then restoring from a file copy and verifying with `cmp -s`
- [ ] T044 Close verification findings 2 and 3: remove the early return from `Manifest_EverySourceUrlSharesOneSiteRoot_AndNamesItsOwnVersion` and the zero-iteration `foreach` from `Manifest_EveryVersionCarriesItsDownloadChecksumTimestampAndJellyfin12` in `tests/Jellyfin.Plugin.NewReleases.Tests/Packaging/RepositoryManifestTests.cs`; assert the entry count explicitly first, then `Assert.All`, so both tests change the day the first tag lands
- [ ] T045 Close verification finding 4: in `Integration/PluginPagesRegistrationTests.cs::WhenRegisterPageThrows_StartingDoesNotThrow_AndLogsOnce`, replace `Assert.Empty(FakePluginPages.Registered)` — which asserts only that the configured fake threw — with an assertion on the subject's own behaviour: the single log entry's level is below `Error`
- [ ] T046 Close verification findings 5 and 6: in `PluginSanityTests.cs::Plugin_ConstructedWithHostServices_ReportsItsIdentityAndOffersAConfigurationPage`, compare `plugin.Id` against the GUID literal rather than `new Guid(Plugin.PluginGuid)`, and assert the single page and its embedded resource path rather than `Assert.NotEmpty`
- [ ] T047 Close verification finding 7: in `Packaging/DocumentationTests.cs::Readme_GivesTheRepositoryUrlAndWhereToAddIt`, assert one URL shape in the Install section (a regex for `https://…/manifest.json`) instead of three bare substrings that pre-existing prose already satisfies
- [ ] T048 Close verification finding 8: in `Packaging/ReleaseWorkflowTests.cs`, assert the guard's mechanism — a `grep -q` step against the project file — and read the comment-stripped `Steps` rather than `Workflow`
- [ ] T049 Close verification finding 9: correct the `U25` row in [tdd/test-list.md](./tdd/test-list.md) to name `Manifest_EverySourceUrlSharesOneSiteRoot_AndNamesItsOwnVersion`, the test that exists. Prove it by re-running the reference check: every `file.cs::method` in the list resolves
- [ ] T050 Close verification finding 11: put the four tests in `PluginSanityTests.cs` that construct `Plugin` — and so assign the process-global `Plugin.Instance` — into one xunit collection with `DisableParallelization`, as `.specify/memory/tdd-profile.md` requires once a second such test exists
- [ ] T051 Close verification finding 12: give `PluginServiceRegistratorTests.BuildContainerAsTheHostWould` an empty assembly source, or add the class to the `PluginPagesRegistrationTests` collection, so the production gateway's scan cannot reach the static stand-in when a future test starts that hosted service
- [ ] T052 Work through the remaining `MED` findings 10, 13–20 and the `LOW` findings 21–24 in [tdd/verification.md](./tdd/verification.md), deciding case by case which are worth acting on; record the ones deliberately left alone and why
- [ ] T053 Re-run `/speckit-tdd-verify` and confirm the verdict is no longer `FAIL`

**Out of this phase, reported not fixed:** the pre-existing flaky test
`Api.ReleasesControllerTests.Decisions_IgnoreAndHaveItStoreTheCallerAndClock_RestoreDeletes_EachReturns204`
(`Support/TestDatabase.cs` calls the process-global `SqliteConnection.ClearAllPools()`, roughly
one failure in ten runs). It predates this feature, breaches constitution III, and needs its own
specification rather than a fix smuggled into this phase.
