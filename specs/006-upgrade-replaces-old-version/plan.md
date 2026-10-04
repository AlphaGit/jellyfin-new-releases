# Implementation Plan: An upgrade leaves exactly one version of the plugin running

**Branch**: `006-upgrade-replaces-old-version` | **Date**: 2026-10-01 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/006-upgrade-replaces-old-version/spec.md`

## Summary

An upgrade left two copies of the plugin loaded in one host. The only route both spelled the same
became an ambiguous match and the user-facing page returned 500 — on some restarts, not others.

Jellyfin already keeps one copy per plugin and deletes the ones it supersedes. Both of those work
**per name**: `DiscoverPlugins()` keeps the newest of each name and removes the older directories
outright. Our copies escaped because `build.yaml` says `Jellyfin New Releases` and `Plugin.cs` says
`New Releases`.

**The fix is to make those agree**, with a test so they cannot drift again. That hands the problem
back to host behaviour that already works, for every future upgrade.

One copy is left stranded under the old name, which the host will never group and so never delete.
The renaming release's notes tell the operator to remove that one directory, once. No plugin code is
added.

**The rename also moves the package slug** (research R7): JPRM derives it from the packaging name,
so the package file, its address and two packaging tests change with it. Session 2026-10-04 settled
where packages now live: each release's `new-releases.zip` is an asset of its GitHub Release, the
catalogue is served from the raw file address on `main`, Pages is dropped, the two review releases
are cleared from the catalogue, and the first release under the new name is `0.2.0` (R8–R10).

## Technical Context

**Language/Version**: C# on `net10.0`.

**Primary Dependencies**: unchanged, and none added.

**Storage**: unchanged. `FR-006` forbids any change to stored data or returned values.

**Testing**: xunit 2.9.3. One new test in `PluginSanityTests`; three existing packaging rules
restated in `RepositoryManifestTests` and `ReleaseWorkflowTests`, each before the workflow or the
catalogue changes. No new test infrastructure, no network, no server (constitution III).

**Target Platform**: Jellyfin 12.0.x, verified on 12.1.0.

**Project Type**: Jellyfin server plugin.

**Performance Goals / Constraints**: none engaged. Nothing runs at runtime that did not before.

**Scale/Scope**: one packaging line, one release workflow reworked, one catalogue reset, one new
test and three restated, four documents amended, one changelog entry. **No new source file, no new
dependency** — the release step uses the `gh` command the runner already carries.

## Constitution Check

*Checked against `.specify/memory/constitution.md` v1.3.0. Re-checked after Phase 1; unchanged.*

| Principle | Verdict | Evidence |
| --- | --- | --- |
| **I. Spec-Driven Development** | **Pass** | `spec.md` grilled over five rounds; the last reopened a decision after investigation contradicted its premise. No open decision. |
| **II. Test-Driven Development** | **Pass** | One new behaviour and three restated packaging rules, each written first and observed red. The `before_implement` hook drives `/speckit-tdd-run`. |
| **III. Hermetic Tests** | **Pass** | Every assertion reads a repository file through an existing helper. No network, no server, no call to GitHub. |
| **IV. Jellyfin Compatibility** | **Pass, with one bounded exception** | GUID frozen. No configuration change, so no `XmlSerializer` risk and no migration. The displayed name changes — that is the fix — and `FR-004a` freezes the assembly identity three mechanisms derive from. The exception is the one-time manual steps: remove the old directory and replace the repository address; see below. |
| **V. Respectful Sources and Privacy** | **Pass (not engaged)** | Nothing touched. |
| **VI. Simplicity** | **Pass** | One line of packaging, one assertion, and a release workflow that loses its Pages deployment and gains one `gh` step. An automatic cleanup was specified, then removed once the host was found to do the same job; see Complexity Tracking. |

## Project Structure

### Documentation (this feature)

```text
specs/006-upgrade-replaces-old-version/
├── plan.md          # This file
├── spec.md
├── NOTES.md
├── research.md      # Phase 0 — R1..R6
├── quickstart.md    # Phase 1 — one suite scenario, then the real-server pass
├── checklists/
└── tasks.md         # Phase 2 — NOT created by /speckit-plan
```

**No `data-model.md` and no `contracts/`.** Both were written against the automatic cleanup and
deleted with it. The feature now has no entities beyond one string stated in two places, which
`spec.md` carries, and exposes no new interface — the condition the plan template gives for
skipping contracts.

### Source code (repository root)

```text
build.yaml                        # name: "Jellyfin New Releases" -> "New Releases"   ← the fix
CHANGELOG.md                      # 0.2.0 entry: one-time removal + new repository address;
                                  # line 43's Pages address replaced
README.md                         # install step: the raw catalogue address
docs/http-surface.md              # gains the naming rule, or a sibling note beside it
.github/workflows/package.yml     # rename to new-releases.zip, gh release create,
                                  # repo add --plugin-url; Pages steps, environment, permissions removed
repo/manifest.json                # 0.1.0 and 0.1.1 removed; entry kept with name "New Releases"

src/                              # UNCHANGED apart from the 0.2.0 version bump.
                                  # Plugin.cs already states the canonical name.

tests/Jellyfin.Plugin.NewReleases.Tests/
├── PluginSanityTests.cs                 # gains: build.yaml name equals Plugin.Name
└── Packaging/
    ├── RepositoryManifestTests.cs       # U25: every sourceUrl is this repo's release asset for its
    │                                    #   own tag, ending new-releases.zip
    │                                    # U39: the workflow uploads {Slug}.zip
    │                                    # PublishedVersionsToday: 2 -> 0
    └── ReleaseWorkflowTests.cs          # U27: build -> release -> repo add -> commit; no deploy

specs/003-jellyfin-12-compat/contracts/plugin-repository-manifest.md   # amended to the new name
                                                                       # and the release-asset address
```

**Structure Decision**: no new folder, no new class. The one new assertion belongs in
`PluginSanityTests`, not `Packaging/BuildManifestTests.cs`: constructing `Plugin` sets the static
`Plugin.Instance`, so it must sit in the `ProcessGlobalStateCollection` that `PluginSanityTests`
already declares. It reads `build.yaml` through `Support/RepositoryFiles.cs`.

**Test-rule changes come first.** U25, U39 and U27 encode the Pages layout. Each is restated to the
new rule and observed red against today's workflow or catalogue before either changes. None is
loosened: U25 still pins one address root and the entry's own version, only the root moves.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
| --- | --- | --- |
| The renaming release requires the operator to delete one directory and to replace the repository address, against constitution IV's "no release requires the operator to delete plugin data" | A copy under the old name is one the host never groups and so never deletes. Every server upgraded before this fix carries one. Something has to clear it. | **An automatic cleanup inside the plugin** was specified and then removed: the host already deletes superseded same-name copies at discovery, so such code would re-implement that and uniquely add only the clearing of this one orphan — permanent irreversible code for a transition that happens once, where a bug destroys a working install. **Keeping the old name** creates no orphan but implies official provenance the plugin does not have. The exception is bounded: one release, one directory, named exactly in the notes, and the rule recorded so a future rename inherits the same cost knowingly. |

**Recorded for the reviewer**: an earlier version of this plan asserted that Jellyfin never retires a
stale copy, and specified an `IHostedService` cleanup on that basis, with a contract document and
three test additions. Reading `PluginManager.DiscoverPlugins` and `InstallationManager` showed the
host deletes superseded same-name copies itself. The assertion held only for the cross-name case.
The cleanup, its contract and its tests were removed; `research.md` R2 carries the correction.

**Recorded for the reviewer, 2026-10-04**: an earlier version of this plan said the publishing
workflow was already correct and out of scope. JPRM derives the package slug from the name, so the
rename broke the workflow's zip path and two packaging tests. `research.md` R7–R10 carry the
measurement and the decisions; `spec.md` session 2026-10-04 carries the answers.
