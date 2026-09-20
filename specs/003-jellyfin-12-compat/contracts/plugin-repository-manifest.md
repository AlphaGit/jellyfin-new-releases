# Contract: published plugin repository

Satisfies `FR-005`, `FR-009`, `FR-013`, `FR-014`, `SC-004` and User Story 2.

An operator adds one URL to Dashboard → Plugins → Repositories, and Jellyfin fetches this document
to decide what to offer them. It is produced by the release workflow and never hand-edited.

## Address

```text
https://<owner>.github.io/<repository>/manifest.json
```

Served by GitHub Pages from the committed `repo/` directory. The packages sit beside it under
`repo/jellyfin-new-releases/`, so both the document and what it points at come from the same origin
and the same commit.

## Document

A JSON array of plugin objects. This project publishes exactly one.

```json
[
  {
    "guid": "b8a15db8-e368-42c4-9048-390faf0094db",
    "name": "Jellyfin New Releases",
    "description": "Tracks releases by the library artists ...",
    "overview": "Releases by your library artists that your library does not have yet.",
    "owner": "Alpha",
    "category": "General",
    "versions": [
      {
        "version": "1.0.0.0",
        "changelog": "...",
        "targetAbi": "12.0.0.0",
        "sourceUrl": "https://<owner>.github.io/<repository>/jellyfin-new-releases/jellyfin-new-releases_1.0.0.0.zip",
        "checksum": "<md5 of the zip>",
        "timestamp": "2026-09-13T00:00:00Z"
      }
    ]
  }
]
```

### Fields

| Field | Must be | Source |
| --- | --- | --- |
| `guid` | `b8a15db8-e368-42c4-9048-390faf0094db`, never anything else | `build.yaml` `guid` |
| `name`, `description`, `overview`, `owner`, `category` | Identity shown in the catalogue | `build.yaml` |
| `versions[].version` | The tag, without its `v` prefix | git tag → `jprm plugin build --version` |
| `versions[].targetAbi` | `12.0.0.0` | `build.yaml` `targetAbi` |
| `versions[].sourceUrl` | An address under the same Pages site that resolves to the package | derived by `jprm repo add` from the repository URL and the plugin slug |
| `versions[].checksum` | MD5 of the exact bytes at `sourceUrl` | computed by JPRM |
| `versions[].timestamp` | Build time | JPRM |
| `versions[].changelog` | Matching the `CHANGELOG.md` entry for the version | `build.yaml` `changelog` |

`targetAbi` is what makes `SC-004` true. Jellyfin offers a version only to a server whose own
version is at or above it, so a server older than Jellyfin 12 is never offered this plugin, and a
Jellyfin 12 server is.

`versions` accumulates. `jprm repo add` merges a new entry into the existing document instead of
replacing it, and because `repo/` is committed, no previously published version can be dropped by a
later deployment.

## Empty is valid

Before the first tag the document exists with `"versions": []`. A server pointed at it sees a
repository with nothing to install, which is exactly right — this feature builds the publishing
chain; tagging a release is the maintainer's act, and is out of scope.

## Package layout

Each package is a zip named `jellyfin-new-releases_<version>.zip` containing the artefacts listed in
`build.yaml`:

```text
Jellyfin.Plugin.NewReleases.dll
Microsoft.Data.Sqlite.dll
SQLitePCLRaw.batteries_v2.dll
SQLitePCLRaw.core.dll
SQLitePCLRaw.provider.e_sqlite3.dll
runtimes/linux-x64/native/libe_sqlite3.so
meta.json
```

Unchanged by the retarget: `Microsoft.Data.Sqlite` 10.0.11 resolves the same `SQLitePCLRaw` 2.1.12
chain as 9.0.19. `FR-009` requires this list to stay complete, so that a catalogue install needs no
manual file step.

## Build manifest changes

`build.yaml`:

| Key | From | To |
| --- | --- | --- |
| `targetAbi` | `10.11.0.0` | `12.0.0.0` |
| `framework` | `net9.0` | `net10.0` |

JPRM rewrites `<TargetFramework>` in the project file from `framework` while building, and fails if
the project has more than one such element. The project has exactly one.

`image` / `imageUrl` are not set. JPRM warns about their absence; the catalogue icon is Outstanding
by the maintainer's decision and is not part of this feature.

## Testable statements

1. `repo/manifest.json` parses as a JSON array of one object whose `guid` is the frozen plugin GUID.
2. Every entry in `versions` has a non-empty `version`, `sourceUrl`, `checksum` and `timestamp`, and
   a `targetAbi` of `12.0.0.0`.
3. Every `sourceUrl` is under the published site root and its filename matches
   `jellyfin-new-releases_<version>.zip` for that entry's version.
4. For a package built locally, the `checksum` recorded equals the MD5 of the built zip.
5. `build.yaml` declares `targetAbi: "12.0.0.0"` and `framework: "net10.0"`, and its `guid` matches
   the GUID compiled into the plugin.
6. A build leaves `<TargetFramework>net10.0</TargetFramework>` in the project file unchanged.
7. Adding a second version to a manifest that already holds one leaves both present.
