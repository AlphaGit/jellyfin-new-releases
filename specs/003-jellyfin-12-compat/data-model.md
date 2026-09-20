# Data model: Run on Jellyfin 12

## No change to persisted data

Nothing this feature touches is stored. Stated explicitly because a host upgrade is the kind of
change that usually does move data, and this one does not:

- **Database.** Same file at `<data>/newreleases/newreleases.db`, same schema, same numbered
  migrations under `Storage/Migrations/`. No new migration script. `Microsoft.Data.Sqlite` moves
  from 9.0.19 to 10.0.11, which resolves the identical `SQLitePCLRaw` 2.1.12 chain, so the file
  format and the shipped native library are unchanged.
- **Configuration.** `PluginConfiguration` gains and loses nothing. Its collections stay `List<T>`
  and stay unseeded in the constructor, so the `XmlSerializer` round-trip keeps behaving.
- **Archive decisions, releases, source state, run history.** Untouched.

`FR-004` records why no forward migration is needed: the plugin has never been released or
installed, so no operator holds data written by an earlier build.

## Artefacts this feature introduces

Neither is persisted state. Both are messages the plugin or the build emits, and both have a
contract that a test can assert against.

### Page registration payload

What the plugin hands Plugin Pages to make the New Releases entry appear in the web client's menu.
Replaces the entry the plugin writes into Plugin Pages' `config.json` today.

| Field | Value | Notes |
| --- | --- | --- |
| `Id` | `Jellyfin.Plugin.NewReleases` | Identity for the entry. `RemovePage` takes this string. Plugin Pages ignores a second registration with the same `Id`, so registering twice is safe. |
| `Url` | `/Plugins/NewReleases/UserView` | Served by `UserViewController`; unchanged. |
| `DisplayText` | `New Releases` | Menu label. |
| `Icon` | `new_releases` | Material icon name. |

Three optional fields exist on Plugin Pages' model — `IsEnabledAssembly`, `IsEnabledClass`,
`IsEnabledMethod`, a callback that can hide the entry per user. The plugin does not set them: the
page is visible to every authenticated user, and per-user library filtering already happens inside
the view's own API calls.

**Dropped field.** The entry written to `config.json` today also carries `Version`, an integer the
plugin invented so it could recognise and replace its own stale entries. Plugin Pages' `PluginPage`
model has no such field, and the problem disappears with the file: registrations now live in memory
and are rebuilt on every server start.

Exact wire shape and call mechanics: [contracts/plugin-pages-registration.md](./contracts/plugin-pages-registration.md).

### Published repository entry

What the release chain adds to `repo/manifest.json` for each published version. Produced by
`jprm repo add` from `build.yaml`, never hand-edited.

| Field | Source | Notes |
| --- | --- | --- |
| `version` | `build.yaml` `version`, from the git tag | |
| `changelog` | `build.yaml` `changelog` | |
| `targetAbi` | `build.yaml` `targetAbi` | `12.0.0.0`. Jellyfin offers the plugin only to servers at or above this. |
| `sourceUrl` | derived | `{pages root}/jellyfin-new-releases/jellyfin-new-releases_{version}.zip` |
| `checksum` | computed | MD5 of the package |
| `timestamp` | build time | |

The surrounding document carries the plugin's fixed identity — `guid`, `name`, `description`,
`overview`, `owner`, `category` — and a `versions` array that accumulates. `jprm repo add` merges a
new version into the existing document rather than replacing it, and the document is committed, so
no published version is ever dropped.

Until the first tag the document exists with an empty `versions` array. That is correct, not
broken: `FR-014` builds the publishing chain, and running it is the maintainer's act.

Full field contract: [contracts/plugin-repository-manifest.md](./contracts/plugin-repository-manifest.md).
