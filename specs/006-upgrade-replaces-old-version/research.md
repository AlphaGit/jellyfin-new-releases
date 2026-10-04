# Phase 0 Research: An upgrade leaves exactly one version running

**Feature**: `006-upgrade-replaces-old-version` | **Date**: 2026-10-01

---

## R1 — Why two copies loaded

**Measured**, from `Emby.Server.Implementations/Plugins/PluginManager.cs`:

```csharp
if (!string.Equals(lastName, entry.Name, StringComparison.OrdinalIgnoreCase))
```

`DiscoverPlugins()` sorts installed copies, walks them backward, keeps the newest **per name**, and
supersedes the rest **before** `LoadAssemblies()` runs. The grouping key is the name, not the GUID.

Our copies escaped it because `build.yaml` declares `Jellyfin New Releases` and `Plugin.cs` declares
`New Releases`. The host persists a name into each copy's `meta.json` — `manifest.Name =
plugin.Instance.Name` on instantiation, with a package-supplied name preserved where one already
exists — so copies end up under two names. Two names, two plugins, both loaded.

## R2 — What the host already does about it

**Measured**, same file and `Emby.Server.Implementations/Updates/InstallationManager.cs`:

- **Install/update** extracts to a version-specific directory — `targetDir += "_" + package.Version`
  — and deletes only that exact path if it already exists. It never touches other versions. So an
  update always leaves two directories.
- **The next discovery** cleans up: where a newer enabled copy of the same name exists, the older
  one's directory is removed with `Directory.Delete(path, true)`. If that fails it is marked
  `PluginStatus.Deleted`.

**So the host's cleanup is complete within one name.** The only copies it cannot reach are those
filed under a name it no longer groups — and a name change is the only thing that creates those.

**This corrects an earlier version of this plan**, which asserted the host never retires a stale
copy and specified an automatic cleanup inside the plugin on that basis. The assertion was true only
for the cross-name case, not generally.

## R3 — Decision: fix the name, document one manual step, add no cleanup code

**Decision**: align `build.yaml` to `Plugin.Name` — `New Releases` everywhere — and have the
renaming release's notes tell the operator to remove the one directory left under the old name.

**Rationale**: the rename is a single event in the project's life. A permanent destructive code path
to tidy up after it would re-implement deletion the host already performs, and would uniquely add
only the clearing of that one orphan. The constitution's "no release requires the operator to delete
plugin data" governs the finished product, not a pre-release transition, so this is a bounded
exception recorded in the spec rather than a breach.

**Alternatives rejected**:

- **A cleanup inside the plugin** (`IHostedService` enumerating `IApplicationPaths.PluginsPath`,
  matching the frozen GUID, never the running directory, never a version at or above running).
  Fully automatic and needs no operator step, but it is permanent irreversible code whose only
  unique job happens once, and a bug in it destroys a working install.
- **Keeping `Jellyfin New Releases` as the name.** Costs nothing and creates no orphan on the one
  server known to be affected, because the copy surviving there is already under that name. Rejected
  because the plugin is not part of the official distribution and should not imply it.

## R4 — Which name is canonical, and why that direction

`Plugin.Name` wins; `build.yaml` is aligned to it. The host writes the running plugin's name into a
copy that has run, so aligning the package to the instance makes every path converge on one string.
Aligning the other way leaves every already-installed copy under the old name.

## R5 — What the rename touches

| Place | Change |
| --- | --- |
| `build.yaml` `name` | `Jellyfin New Releases` → `New Releases` |
| `repo/manifest.json` | `0.1.0` and `0.1.1` removed by hand, once (R9); from then on written only by the release workflow, which overwrites `name` from the package (R7) |
| `specs/003-jellyfin-12-compat/contracts/plugin-repository-manifest.md` | pins `"name": "Jellyfin New Releases"`; amended, as `005` amended its route contracts |
| `CHANGELOG.md` | the `0.2.0` entry carries the one-time removal step and the new repository address; line 43's Pages address is replaced |
| `.github/workflows/package.yml` | renames the package, creates the GitHub Release, adds the version with `--plugin-url`; Pages steps removed (R8) |
| `README.md` install step | the repository address becomes the raw catalogue address (R8) |
| `Plugin.cs` `Name` | unchanged — it is the canonical value |
| `Jellyfin.Plugin.NewReleases` namespace, `PageEntryId`, migration prefix, embedded resource paths | unchanged, per `FR-004a` |
| `README.md` title, `CLAUDE.md`, constitution titles | unchanged — they name the repository, not the plugin |

New installs land in `New Releases_<version>`. The copy under `Jellyfin New Releases_<version>` is
the orphan the release notes name.

## R6 — Evidence

The suite cannot load two copies into one host, and after this decision there is no new code to
test. What remains is the assertion that would have prevented the whole defect:

- `build.yaml`'s `name` equals `Plugin.Name`. One test, in `PluginSanityTests`, reading `build.yaml`
  through `Support/RepositoryFiles.cs`. Not in `Packaging/BuildManifestTests.cs`: constructing
  `Plugin` sets the static `Plugin.Instance`, so the test must sit in the
  `ProcessGlobalStateCollection` that `PluginSanityTests` already declares.

R7 and R8 add evidence of their own: three existing packaging tests change rule, each before the
workflow does.

The rest is closed on a real server: [`quickstart.md`](./quickstart.md).

**No new dependency, and no new source file.**

## R7 — The rename moves the package slug

**Measured**, from JPRM 1.1.0 (`jprm/__init__.py`, the version `package.yml` pins):

- `plugin build` names its output `{slug}_{version}.zip` with `slug = slugify(build_cfg['name'])`.
  No key overrides the slug.
- `repo add` copies the package to `<repo>/{slug}/{slug}_{version}.zip` and writes that as the
  `sourceUrl` — unless `--plugin-url` is given, which replaces the `sourceUrl` and copies nothing.
- `repo add` finds the existing catalogue entry **by GUID** and merges with `old.update(new)`, so the
  entry's `name` becomes whatever the package declares. Old versions are kept as they were.

So `New Releases` makes the slug `new-releases`, and three things break at once:
`package.yml:86` hardcodes `jellyfin-new-releases_<v>.zip`; U39
(`TheDerivedSlug_MatchesTheFilenameTheReleaseWorkflowBuilds`) fails against it; U25
(`Manifest_EverySourceUrlSharesOneSiteRoot_AndNamesItsOwnVersion`) fails against the two published
entries.

## R8 — Decision: packages on GitHub Releases, catalogue on the raw file address

**Decision**: the workflow renames JPRM's output to `new-releases.zip`, creates the version's GitHub
Release with `gh release create v<version> new-releases.zip --notes-file <entry>`, then runs
`jprm repo add --plugin-url https://github.com/AlphaGit/jellyfin-new-releases/releases/download/v<version>/new-releases.zip`
and commits `repo/manifest.json`. Servers read the catalogue from
`https://raw.githubusercontent.com/AlphaGit/jellyfin-new-releases/main/repo/manifest.json`. Pages is
dropped: `configure-pages`, `upload-pages-artifact`, `deploy-pages`, the `github-pages` environment
and the `pages`/`id-token` permissions.

**Measured**, from `Emby.Server.Implementations/Updates/InstallationManager.cs`: Jellyfin requires
only that `sourceUrl` ends in `.zip` and that the downloaded bytes match `checksum`. The install
directory is `{package.Name}_{version}`, from the catalogue. The file name carries nothing.

**Rationale**: one file name across versions needs one address per version, or each release
overwrites the bytes earlier entries' checksums describe. A release asset's address is per tag and
immutable, and the repository is public, so it doubles as the historical address `FR-009` needs.
`gh` ships on the runner image, so no third-party action joins a job holding `contents: write`. The
release text comes from `entryFor` in `.github/scripts/changelog-entry.js`, which already fails the
run on a missing or empty section.

**Order**: build → release → `repo add` → commit. The catalogue never names an asset that does not
exist yet. Known ceiling: a run that fails after the release but before the commit leaves a release
with no catalogue entry, and a re-run fails at `gh release create` because the release exists. Delete
the release by hand and re-run; add `--clobber` handling only if it ever happens.

**Alternatives rejected**:

- **One fixed address, `…/new-releases.zip`, on Pages.** Each release overwrites the bytes, so only
  the newest entry's checksum stays true — `FR-009` lost — and Pages' cache serves old bytes with the
  new checksum for minutes after a release.
- **A versioned folder on Pages, `…/<version>/new-releases.zip`.** Works, but keeps Pages for files
  GitHub Releases hosts already.
- **JPRM's default, `new-releases_<version>.zip`.** No workflow change beyond the slug, but the file
  name carries the version, which was not wanted.
- **Pages for the catalogue only.** Smallest workflow change, but keeps a deployment for one file.
  The raw address is rate-limited and cached for about five minutes; a catalogue read once a day by
  a handful of servers is far inside both.
- **`softprops/action-gh-release`.** A third-party dependency in a job that can write the
  repository, for what one `gh` line does.
- **`gh release create --generate-notes`.** Commit titles, not the changelog, so the release and
  the catalogue would say different things.

## R9 — Decision: clear `0.1.0` and `0.1.1` from the catalogue

**Decision**: remove both versions from `repo/manifest.json`, by hand, once. The plugin's entry stays
— GUID, `name: "New Releases"`, empty `versions` — so U21 (`Manifest_IsAnArrayOfOnePlugin_CarryingTheFrozenGuid`)
still finds exactly one plugin and JPRM's GUID merge has an entry to merge into.
`PublishedVersionsToday` becomes 0; the per-entry rules run over synthetic entries until `0.2.0`
publishes and the count becomes 1.

**Rationale**: both were review releases for the real-server passes. Keeping them would bind U25 to
a table of pre-rename exceptions for addresses that stop answering once Pages is off.

**Consequence**: the one-time removal step (`FR-007`) stays — an unknown operator may hold a copy —
and gains a second line: replace the repository address.

## R10 — Decision: the first release is `0.2.0`

The host offers an update only when the catalogue version is above the installed one, and the
known server holds `0.1.1`. MINOR, not PATCH: a new displayed name and a new repository address
both need the operator, and `007`'s unreleased changes ship in it too. `1.0.0` waits for a clean
real-server pass.
