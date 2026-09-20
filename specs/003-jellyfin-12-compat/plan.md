# Implementation Plan: Run on Jellyfin 12

**Branch**: `003-jellyfin-12-compat` | **Date**: 2026-09-13 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/003-jellyfin-12-compat/spec.md`

## Summary

Move the plugin from Jellyfin 10.11.x to Jellyfin 12, and give the project a way to publish itself.

Three strands, in dependency order:

1. **Retarget.** `net9.0` → `net10.0`, Jellyfin packages `10.11.11` → `12.0.0`, two packages that
   used to arrive transitively become direct references, SQLite moves onto the matching runtime
   line, and the test stack gains versions that understand the new target framework. No plugin code
   changes for this: every host surface the plugin binds to is byte-identical between the two
   versions (research `R1`).
2. **Modernise the one integration that has a newer supported path.** Stop writing Plugin Pages'
   `config.json`; register and withdraw the page through its public `PluginInterface`, reached by
   reflection so Plugin Pages stays optional. This is the only real code change in the feature.
3. **Publish.** A `repo/` directory holds the plugin repository manifest and the packages; the
   release workflow builds with JPRM, adds the version to the manifest, commits it, and deploys the
   directory to GitHub Pages. No version is tagged here — the chain is built and proven, not run.

The feature is complete when the suite is green against the Jellyfin 12 libraries. Confirming it on
a running server is the maintainer's own pass, outside this feature.

## Technical Context

**Language/Version**: C# on `net10.0`, `LangVersion` latest, nullable and implicit usings enabled

**Primary Dependencies**: `Jellyfin.Controller` / `Jellyfin.Model` / `Jellyfin.Data` /
`Jellyfin.Database.Implementations` 12.0.0 (all `ExcludeAssets=runtime`, `PrivateAssets=all`);
`Microsoft.Data.Sqlite` 10.0.11. No new runtime dependency: Plugin Pages is reached by reflection.

**Storage**: unchanged — SQLite at `<data>/newreleases/newreleases.db`, same schema, same
migrations, same native artefacts

**Testing**: xunit 2.9.3 with `xunit.runner.visualstudio` 3.1.5 and `Microsoft.NET.Test.Sdk` 18.9.0;
NSubstitute 5.3.0; Node's built-in test runner for the page logic under `tests/web/`

**Target Platform**: Jellyfin 12 server, Linux x64 (the only native SQLite artefact shipped)

**Project Type**: Jellyfin server plugin — one library project, one test project

**Performance Goals**: none new. The feature changes no hot path.

**Constraints**: `TreatWarningsAsErrors` stays on; tests stay hermetic (no network, no Jellyfin
server); plugin GUID `b8a15db8-e368-42c4-9048-390faf0094db` frozen; no user-visible behaviour change

**Scale/Scope**: two `.csproj` files, two lock files, `build.yaml`, two CI workflows, one new
hosted service replacing one private method in `Plugin.cs`, one new published directory

## Constitution Check

*GATE: checked before Phase 0, re-checked after Phase 1.*

| Principle | Verdict |
| --- | --- |
| **I. Spec-Driven Development** | **Pass.** Spec written, grilled over four rounds, eleven decisions recorded. Nothing here exceeds it; the catalogue icon is left Outstanding rather than quietly built. |
| **II. Test-Driven Development** | **Pass.** The behaviour change (Plugin Pages registration) goes through `/speckit-tdd-run`. The retarget's evidence is the existing suite recompiled against the new libraries, plus new tests for registration and withdrawal. |
| **III. Hermetic Tests** | **Pass.** No new network. The Plugin Pages integration is tested against a stub assembly and a substituted registrar, never a real Plugin Pages install. |
| **IV. Jellyfin Compatibility** | **Deviation, see Complexity Tracking.** GUID frozen and still guarded; packages pinned to the exact server version with `ExcludeAssets=runtime`; `targetAbi` moves with them; `PluginConfiguration` collections unchanged; Plugin Pages stays optional at runtime and still logs at most once. Forward migration is not required — the plugin has never been released (FR-004). The deviation is that the constitution names 10.11.x and this plan targets 12.0.0. |
| **V. Respectful Sources and Privacy** | **Pass.** No source, rate limit, budget, User-Agent or fixture changes. |
| **VI. Simplicity** | **Pass.** Two new direct package references, both replacing transitive ones the code already uses (research `R2`). No new third-party dependency: the Plugin Pages call is reflection, adding no reference to Plugin Pages or Newtonsoft. Test-stack versions bumped only where the new target framework requires it. |

### Deviations

**The constitution targets the wrong Jellyfin version.** It states `Jellyfin.Controller` and
`Jellyfin.Model` are "pinned to the exact server version in production (10.11.x)" and that the
language and runtime are "C# on `net9.0`, matching the Jellyfin 10.11 host". This plan contradicts
both, deliberately and as the whole point of the feature. Its own Governance section says the
constitution is reviewed when the Jellyfin target major version changes, so the amendment is due
now. It must be made with `/speckit-constitution`, not edited from inside this feature. Recorded in
Complexity Tracking.

## Project Structure

### Documentation (this feature)

```text
specs/003-jellyfin-12-compat/
├── plan.md                              # This file
├── research.md                          # Phase 0 output
├── data-model.md                        # Phase 1 output
├── quickstart.md                        # Phase 1 output
├── contracts/
│   ├── plugin-pages-registration.md     # The payload and call the plugin makes
│   └── plugin-repository-manifest.md    # What the published site serves
├── checklists/requirements.md
└── tasks.md                             # /speckit-tasks output, not created here
```

### Source code (repository root)

```text
src/Jellyfin.Plugin.NewReleases/
├── Jellyfin.Plugin.NewReleases.csproj        # net10.0; Jellyfin 12.0.0; +Jellyfin.Data,
│                                             #   +Jellyfin.Database.Implementations; Sqlite 10.0.11
├── packages.lock.json                        # regenerated
├── Plugin.cs                                 # TryRegisterPluginPagesEntry and IsPluginPagesInstalled removed
├── PluginServiceRegistrator.cs               # + AddHostedService<PluginPagesRegistrationService>()
└── Integration/                              # new
    ├── PluginPagesRegistrationService.cs     # IHostedService: StartAsync registers, StopAsync withdraws
    └── PluginPagesGateway.cs                 # reflection over AssemblyLoadContext.All; no PluginPages reference

tests/Jellyfin.Plugin.NewReleases.Tests/
├── Jellyfin.Plugin.NewReleases.Tests.csproj  # net10.0; Jellyfin 12.0.0; Test.Sdk 18.9.0; runner 3.1.5
├── packages.lock.json                        # regenerated
├── Integration/PluginPagesRegistrationTests.cs   # new
└── Support/FakePluginPages.cs                    # new: stand-in for PluginInterface + payload type

repo/                                         # new — the published plugin repository
├── manifest.json                             # jprm repo add writes here; starts with no versions
└── jellyfin-new-releases/                    # jprm drops packages here on release

.github/workflows/
├── build.yml                                 # setup-dotnet 10.0.x
└── package.yml                               # jprm build → jprm repo add → commit repo/ → deploy Pages

build.yaml                                    # targetAbi 12.0.0.0, framework net10.0
README.md                                     # Jellyfin 12; minimum Plugin Pages version; repository URL
```

**Structure Decision**: unchanged layout. One new folder in the plugin project (`Integration/`)
holding the two files that replace `Plugin.cs`'s private config-file writer, its mirror in the test
project, and one new top-level `repo/` directory that is the published site.

## Phase 0: research

Complete — see [research.md](./research.md). Nine findings, all verified against published
artefacts rather than recalled: the Jellyfin 12 API diff (`R1`), the two packages that stop being
transitive (`R2`), runtime and toolchain (`R3`), every dependency pin with its publication date
(`R4`), the Plugin Pages contract and how to call it without a reference (`R5`), the publishing
chain and why the site lives in git (`R6`), the web-client contract that makes the pages safe
(`R7`), what Jellyfin 12 offers that this feature deliberately does not adopt (`R8`), and the
constitution conflict (`R9`).

No `NEEDS CLARIFICATION` remains.

## Phase 1: design and contracts

- [data-model.md](./data-model.md) — no persisted entity changes. Describes the two artefacts this
  feature does introduce: the page-registration payload and the published repository entry.
- [contracts/plugin-pages-registration.md](./contracts/plugin-pages-registration.md) — the exact
  type, method, payload shape and failure behaviour of the Plugin Pages call.
- [contracts/plugin-repository-manifest.md](./contracts/plugin-repository-manifest.md) — what the
  published site serves and what each field must contain.
- [quickstart.md](./quickstart.md) — how to install the toolchain, build, test, and prove the
  publishing chain without publishing.

## Risks

| Risk | Handling |
| --- | --- |
| The hosted service starts before Plugin Pages' own plugin object exists, so `PluginInterface` throws. | Registration is wrapped: any failure logs once and leaves the plugin running. Research `R5` establishes that plugin objects are constructed before the generic host starts hosted services, so this is a guard, not the expected path. |
| Plugin Pages renames or removes `PluginInterface` in a later release. | Reflection resolves by name and degrades to "integration unavailable" rather than failing to load. A test covers the absent case. |
| `net10.0` analyzers raise new warnings, and `TreatWarningsAsErrors` turns them into build failures. | Expected and wanted. They get fixed, never suppressed — the constitution is explicit. Budget for it in the retarget task. |
| JPRM rewrites `<TargetFramework>` in the csproj during a build. | Known behaviour (research `R6`); the project has exactly one such element. The packaging task verifies the file is unchanged after a build. |
| A GitHub Pages deployment drops older packages. | The site is a committed directory, so every published version survives in git. This is why `repo/` exists rather than an artifact-only deployment. |
| The Jellyfin 12.0.0 packages are newer than the project's 7-day dependency rule allows. | Explicit, recorded exception, scoped to those packages; every other pin in `R4` is at least 7 days old. |

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
| --- | --- | --- |
| Constitution IV names Jellyfin 10.11.x and `net9.0`; this plan targets Jellyfin 12.0.0 and `net10.0`. | The feature exists to move the plugin to Jellyfin 12. Jellyfin 12 ships `lib/net10.0` only, so the runtime moves with it. | Staying on 10.11.x means not doing the feature. The constitution's Governance section already anticipates this: it is reviewed when the Jellyfin target major version changes. Resolve by amending it through `/speckit-constitution` — a task in `tasks.md`, not an edit from here. |
| Jellyfin 12.0.0 is pinned 5 days after publication, against the 7-day dependency-age rule. | No older stable Jellyfin 12 release exists; the alternatives are release candidates. | Waiting to 2026-09-15 was offered and declined. The exception is explicit, recorded in the specification, and scoped to the Jellyfin 12.0.0 packages alone. |
