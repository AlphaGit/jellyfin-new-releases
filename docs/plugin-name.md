# The plugin's name

Read this before you rename the plugin, or before you publish a release that changes
`build.yaml`'s `name`.

**The displayed name is `New Releases`, and it is fixed.** It is written in three places:

- `build.yaml` `name` — the name the package declares;
- `Plugin.Name` in `src/Jellyfin.Plugin.NewReleases/Plugin.cs` — the name the running plugin
  reports;
- `repo/manifest.json` — the name the catalogue lists. The release workflow rewrites it from the
  package on every release; it is never edited by hand.

**These places must never disagree.** Enforced by
`PluginSanityTests.Plugin_DisplayName_MatchesTheNameThePackageDeclares` and
`PluginSanityTests.Plugin_DisplayName_MatchesTheNameTheCatalogueLists`.

Why this exists: from `0.1.0` to `0.1.1` the package said "Jellyfin New Releases" and the plugin
said "New Releases". After an upgrade the server loaded both versions, and the New Releases view
failed on some restarts and not on others. Specified and fixed in
`specs/006-upgrade-replaces-old-version/`.

---

## How Jellyfin uses the name

- **Jellyfin groups installed copies by name**, ignoring case, keeps the newest of each name, and
  deletes the older copies' folders when it starts. It does not group by the plugin's GUID.
- **A copy filed under another name is a different plugin to Jellyfin.** Jellyfin never compares
  it with the new copy, never deletes it, and starts it beside the new one.
- **The install folder is `<name>_<version>`**, taken from the catalogue.

## What a rename costs

A rename strands every installed copy under the old name. The operator must delete that folder by
hand, once, on every server. The release notes of the renaming release must name the folder.
`0.2.0` did this for `Jellyfin New Releases_<version>`.

A rename also moves the release asset. JPRM derives the package's file name from the name
(`slugify(name)`), so `new-releases.zip` follows the name. `RepositoryManifestTests` derives the
same slug from `build.yaml` and fails until the release workflow uploads the new file name.

The word "Jellyfin" does not belong in the displayed name, because this plugin is not part of the
official distribution. Enforced by `PluginSanityTests.Plugin_DisplayName_DoesNotClaimToBeJellyfin`.
The assembly identity `Jellyfin.Plugin.NewReleases` is a different thing: it is the host's
convention for plugin assemblies, and it must not change.
