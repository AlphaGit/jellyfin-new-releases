# Implementation Plan: An upgrade leaves exactly one version of the plugin running

**Branch**: `006-upgrade-replaces-old-version` | **Date**: 2026-10-01 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/006-upgrade-replaces-old-version/spec.md`

## Summary

An upgrade left two copies of the plugin loaded in one host. The only route both spelled the same
became an ambiguous match, and the user-facing page returned 500 — on some restarts, not others.

Jellyfin already guarantees one running copy per plugin: `DiscoverPlugins()` keeps the newest **per
name**, case-insensitively, and supersedes the rest before any assembly loads. Our copies escaped it
because `build.yaml` says `Jellyfin New Releases` and `Plugin.cs` says `New Releases`.

**The fix is to make those agree**, with a test so they cannot drift again. That hands the problem
back to a guarantee the host already enforces, for every future upgrade.

It does not repair servers that already carry copies under both names: the host never compares two
names, so it will never retire either. Those copies would load forever, sharing the database and
registering a second refresh task. So the plugin also **removes retired copies of itself at
startup** — matched on the frozen GUID, never the running directory, and never a version at or above
the running one.

## Technical Context

**Language/Version**: C# on `net10.0`.

**Primary Dependencies**: `Jellyfin.Controller` / `Jellyfin.Model` / `Jellyfin.Data` /
`Jellyfin.Database.Implementations` 12.0.0, `Microsoft.Data.Sqlite` 10.0.11. **No new dependency.**
`IApplicationPaths.PluginsPath` is on `Jellyfin.Common` 12.0.0, already referenced transitively;
`System.Text.Json` and `System.IO` cover the rest.

**Storage**: unchanged. `FR-006` forbids any change to stored data or returned values. The feature
touches the plugin's displayed name and what it deletes from its own install directory.

**Testing**: xunit 2.9.3 + NSubstitute 5.3.0. Directory selection is driven against a temporary
directory tree the test creates and removes — no installed server, no network (constitution III).

**Target Platform**: Jellyfin 12.0.x, verified on 12.1.0.

**Project Type**: Jellyfin server plugin.

**Performance Goals**: none. The cleanup enumerates one directory once per server start.

**Constraints**: the cleanup deletes directories on an operator's machine, so its matching rule is
the highest-risk part of this feature and is pinned by tests before it is written. A failed removal
must never stop startup. `TreatWarningsAsErrors` stays on.

**Scale/Scope**: 1 one-line packaging change, 1 new class, 1 service registration, 3 test classes,
1 contract document amended.

## Constitution Check

*Checked against `.specify/memory/constitution.md` v1.3.0. Re-checked after Phase 1; unchanged.*

| Principle | Verdict | Evidence |
| --- | --- | --- |
| **I. Spec-Driven Development** | **Pass** | `spec.md` grilled over four rounds, eight questions, no open decision. Every artifact here traces to an `FR-`. One narrowing `research.md` R5 introduces is flagged for the spec rather than assumed. |
| **II. Test-Driven Development** | **Pass** | The `before_implement` hook drives `/speckit-tdd-run`. The removal rule is a predicate, so its accepting and rejecting cases are written as a table from the requirement before the predicate exists — the profile's standing rule. |
| **III. Hermetic Tests** | **Pass** | Temporary directories created and removed by the test. No network, no server, no installed plugin. |
| **IV. Jellyfin Compatibility** | **Pass** | GUID frozen and unchanged — it becomes *more* load-bearing here. No `PluginConfiguration` change, so no `XmlSerializer` risk and no migration. Plugin Pages stays optional. The displayed name changes, which is the fix; `FR-004a` freezes the assembly identity that three mechanisms derive from. |
| **V. Respectful Sources and Privacy** | **Pass (not engaged)** | No source, budget, `User-Agent` or outgoing request is touched. |
| **VI. Simplicity** | **Pass, with one recorded cost** | No new dependency. The packaging fix is one line and would alone prevent every future occurrence. The cleanup is the addition — see Complexity Tracking. |

**Technical Constraints**: `net10.0`, xunit + NSubstitute, no assertion library added — all held.

**Development Workflow**: Spec Kit order followed. CI on `main` is the final gate, plus the
real-server pass, which for this feature must start from a deliberately broken server.

## Project Structure

### Documentation (this feature)

```text
specs/006-upgrade-replaces-old-version/
├── plan.md                      # This file
├── spec.md
├── NOTES.md
├── research.md                  # Phase 0 — R1..R7
├── data-model.md                # Phase 1 — identity and on-disk states
├── quickstart.md                # Phase 1 — suite pass + real-server pass
├── contracts/
│   └── installed-copy-record.md # What the plugin reads, and the exact removal rule
├── checklists/
└── tasks.md                     # Phase 2 — NOT created by /speckit-plan
```

### Source code (repository root)

```text
build.yaml                       # name: "Jellyfin New Releases" -> "New Releases"  (the fix)

src/Jellyfin.Plugin.NewReleases/
├── Plugin.cs                    # unchanged — Name is the canonical value
├── PluginServiceRegistrator.cs  # registers the cleanup beside PluginPagesRegistrationService
└── Installation/
    └── StaleCopyCleanup.cs      # NEW — IHostedService; selection is a static, testable method

tests/Jellyfin.Plugin.NewReleases.Tests/
├── Installation/
│   └── StaleCopyCleanupTests.cs # NEW — the removal rule against a temp tree, both sides
├── Packaging/
│   └── BuildManifestTests.cs    # gains: build.yaml name equals Plugin.Name
└── PluginServiceRegistratorTests.cs  # gains: the cleanup is registered as a hosted service

specs/003-jellyfin-12-compat/contracts/plugin-repository-manifest.md   # amended to the new name
docs/http-surface.md             # unchanged; the naming convention goes in its own note
```

**Structure Decision**: the existing single-project layout stands. One new source folder,
`Installation/`, because this is neither an `Integration/` with a third-party plugin nor `Storage/`
— it is about this plugin's own install. The test project mirrors it, as the conventions require.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
| --- | --- | --- |
| The plugin deletes directories on an operator's server — new code doing something irreversible, against principle VI's "prefer not doing it" | A copy filed under a name the host no longer groups is one the host will **never** retire: grouping is by name, so the two are never compared. Left alone it loads on every start, shares the plugin database, and registers a second refresh task. Every server upgraded before this fix is in that state. | **Do nothing beyond the name fix**: correct for all future upgrades, leaves every existing install permanently double-loaded. **Tell the operator to delete it**: breaches constitution IV, "no release requires the operator to delete plugin data". **Overwrite the old directory in place**: the plugin does not choose its install location, so the directory name would contradict its contents, written by code overwriting its own loaded assembly. The risk is bounded by the rule in `contracts/installed-copy-record.md` — frozen GUID only, never the running directory, never a version at or above running — and by writing that rule's rejecting cases as tests before the predicate. |

**One decision this plan makes that the spec does not yet carry**: `FR-007a` says the cleanup removes
any copy carrying our GUID that is not the running one. Taken literally that also removes a copy
*newer* than the running one — which exists exactly when an operator has deliberately rolled back
with the newer version disabled. `research.md` R5 narrows it to copies **older than** the running
version, so the destructive path is monotonic and no deliberate downgrade is undone. `FR-007a` needs
that sentence; it is raised in the completion report rather than edited in silently, because
`/speckit-plan` may not rewrite a requirement.
