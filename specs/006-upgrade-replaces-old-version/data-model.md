# Phase 1 Data Model: An upgrade leaves exactly one version running

**Feature**: `006-upgrade-replaces-old-version` | **Date**: 2026-10-01

No stored data changes. `FR-006` forbids it. The entities here are identity and on-disk state.

---

## 1. Displayed name

What an operator reads in the catalogue and the plugin list, and — decisively — the key Jellyfin
groups installed copies by.

| Stated in | Before | After |
| --- | --- | --- |
| `src/Jellyfin.Plugin.NewReleases/Plugin.cs` → `Name` | `New Releases` | unchanged — canonical |
| `build.yaml` → `name` | `Jellyfin New Releases` | `New Releases` |

**Rule**: the two MUST be equal. A test asserts it (`FR-005`).

**Not** the displayed name, and unchanged (`FR-004a`): the assembly and namespace
`Jellyfin.Plugin.NewReleases`, the Plugin Pages entry id, the migration resource prefix, and both
pages' embedded resource paths. These say "a plugin for Jellyfin", not "shipped by Jellyfin", and
three of them fail silently if changed on an existing install.

## 2. Installed copy

One release of this plugin present in one directory on one server. Read-only to the plugin; shape
fixed in [`contracts/installed-copy-record.md`](./contracts/installed-copy-record.md).

| Field | Source | Used for |
| --- | --- | --- |
| directory path | `IApplicationPaths.PluginsPath` enumeration | what gets removed |
| `guid` | the copy's `meta.json` | the only identity match |
| `version` | the copy's `meta.json` | compared with the running version |

**States**, as this feature sees them:

| State | Condition | Action |
| --- | --- | --- |
| Running | directory holds the loaded assembly | never removed |
| Stale | our GUID, not running, version lower than running | removed |
| Newer | our GUID, not running, version at or above running | left alone — a deliberate downgrade's target |
| Foreign | another GUID, or no readable record | left alone |

## 3. Running copy

The one the host actually loaded. Identified by the directory containing
`typeof(Plugin).Assembly.Location` — exact, and independent of name and version, which is the point.

Its version is the comparison point for the Stale/Newer split.
