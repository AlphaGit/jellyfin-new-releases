# Contract: the plugin's HTTP surface

The convention `FR-011` requires. This is the specification of it; the implementation copies it to
`docs/http-surface.md`, which is where a future author reads it, linked from `CLAUDE.md`.

**The standard is the host's own.** The plugin's endpoints are named as Jellyfin names its
first-party endpoints, so they read as part of the server rather than as a visitor on it.

---

## Rule 1 — Route segments are PascalCase, concatenated, with no `api` segment

Every segment starts with a capital. A multi-word segment is one word with no separator. There is
no version segment and no `api` segment, because no Jellyfin route has one — compare the host's own
`/ScheduledTasks`, `/QuickConnect/Initiate`, `/Library/VirtualFolders`.

```text
Plugins/NewReleases/Releases                 not  .../api/releases
Plugins/NewReleases/Releases/{id}/HaveIt     not  .../releases/{id}/have-it
Plugins/NewReleases/Admin/ClearArchive       not  .../api/admin/clear-archive
```

## Rule 2 — Every endpoint that returns a body declares its field naming

A controller that returns JSON carries:

```csharp
[Produces(JsonDefaults.CamelCaseMediaType)]   // Jellyfin.Extensions.Json
```

`JsonDefaults.CamelCaseMediaType` is `application/json; profile="CamelCase"`. Jellyfin offers each
response in either naming and picks by what the caller asks for; without this attribute the plugin
receives the host's default, whatever that happens to be on the server it is installed on. Stating
it makes the naming the plugin's own contract (`FR-003`, `FR-010`).

Returned object fields are therefore camelCase, at every level of nesting.

**The structural rule a test enforces** (`SC-004`):

> For every public action on every `ControllerBase` subclass in the plugin assembly, the effective
> produced content types — the action's `ProducesAttribute` if present, otherwise its controller's
> — MUST either contain `JsonDefaults.CamelCaseMediaType`, or contain no `application/json` type
> at all.

## Rule 3 — The route prefix has one authoritative source

`PluginRoutes` (`src/Jellyfin.Plugin.NewReleases/Api/PluginRoutes.cs`) holds it as `const string`.
Every `[Route]` and the Plugin Pages registration payload reference it. A server-absolute path is
derived from it by concatenation, never written again (`FR-013`).

The two embedded pages each hold one derived `API` literal, because they are static resources with
no build step and `admin.html` is streamed by Jellyfin without passing through plugin code. A test
holds both literals to `PluginRoutes` and fails when they diverge.

## Rule 4 — Pages build paths through the host's relative-URL facility

Only `ApiClient.getUrl(...)` (`FR-014`). No page may construct a path containing a server address,
a scheme, or an assumed deployment sub-path.

## The route surface

| Method | Route | Controller | Body |
| --- | --- | --- | --- |
| `GET` | `Plugins/NewReleases/Releases` | `ReleasesController` | `ListResponse` |
| `GET` | `Plugins/NewReleases/Artists` | `ReleasesController` | `ArtistsResponse` |
| `GET` | `Plugins/NewReleases/Status` | `ReleasesController` | `StatusResponse` |
| `POST` | `Plugins/NewReleases/Releases/{id:long}/Ignore` | `ReleasesController` | none (204) |
| `POST` | `Plugins/NewReleases/Releases/{id:long}/HaveIt` | `ReleasesController` | none (204) |
| `POST` | `Plugins/NewReleases/Releases/{id:long}/Restore` | `ReleasesController` | none (204) |
| `GET` | `Plugins/NewReleases/Admin/Status` | `AdminController` | `AdminStatusResponse` |
| `POST` | `Plugins/NewReleases/Admin/RunNow` | `AdminController` | none (202), or `{message}` (409) |
| `POST` | `Plugins/NewReleases/Admin/Purge` | `AdminController` | none (204) |
| `POST` | `Plugins/NewReleases/Admin/ClearArchive` | `AdminController` | none (204) |
| `GET` | `Plugins/NewReleases/UserView` | `UserViewController` | `text/html` fragment |

Status codes, query parameters, ownership rules and body contents are unchanged from
`specs/001-track-new-releases/contracts/http-api.md` and `specs/002-report-data-age/contracts/http-api.md`,
both of which this feature amends to these route names (`FR-015`).

## Deliberate exceptions (`FR-012`, `SC-006`)

| Exception | Reason |
| --- | --- |
| `UserViewController` declares no JSON profile | It serves an HTML fragment. `[Produces("text/html")]` is its correct declaration; a JSON profile would be false. |

No other exception exists. An endpoint that cannot follow a rule is added to this table with its
reason, in the same change that adds the endpoint.
