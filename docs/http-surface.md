# The plugin's HTTP surface

Read this before adding an endpoint. Four rules, one exceptions table, and a test behind each.

**The standard is the host's own.** The plugin's endpoints are named as Jellyfin names its
first-party endpoints, so they read as part of the server rather than as a visitor on it.

Why this exists: version 0.1.0 shipped to a real Jellyfin 12 server with both embedded pages
blank. The plugin had never stated how its own responses should be named, so it inherited the
host's default — and the default there was not what the pages read. The 257 tests were green,
because they all asserted the shape of the record a controller returns, on the near side of the
serializer. Specified and fixed in `specs/005-page-json-casing/`.

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

Enforced by `HttpSurfaceTests.EveryRouteThePluginServes_UsesPascalCaseSegmentsAndNoApiSegment`,
over a `[Theory]` table of accepting and rejecting segments.

## Rule 2 — Every endpoint that returns a body declares its field naming

A controller that returns JSON carries:

```csharp
[Produces(JsonDefaults.CamelCaseMediaType)]   // Jellyfin.Extensions.Json
```

`JsonDefaults.CamelCaseMediaType` is `application/json; profile="CamelCase"`. Jellyfin offers every
response in either naming and picks by what the caller asks for. Measured against
`Jellyfin.Extensions` 12.0.0:

```text
JsonDefaults.Options           -> {"Items":[1],"HasStoredReleases":true, ...}   <- the host default
JsonDefaults.CamelCaseOptions  -> {"items":[1],"hasStoredReleases":true, ...}
JsonDefaults.PascalCaseOptions -> {"Items":[1],"HasStoredReleases":true, ...}
```

Without the attribute the plugin gets the first of those — whatever the host happens to do on the
server it is installed on. Stating it makes the naming the plugin's own contract, so a later change
to the host's default cannot silently blank the pages again.

Returned object fields are therefore camelCase, at every level of nesting.

**The structural rule a test enforces:**

> For every public action on every `ControllerBase` subclass in the plugin assembly, the effective
> produced content types — the action's `ProducesAttribute` if present, otherwise its controller's
> — MUST be **non-empty**, and MUST either contain `JsonDefaults.CamelCaseMediaType` or contain no
> `application/json` type at all.

**The non-empty clause is the whole rule, not a detail.** An endpoint that declares nothing is an
endpoint that inherits whatever the host does, which is the defect. The first version of this rule
omitted it, and a deliberate mutant proved the omission: with the declaration removed from
`AdminController`, the scan still passed.

Enforced by `HttpSurfaceTests.EveryActionThatReturnsJson_DeclaresTheCamelCaseProfile`. The names
the responses actually carry are pinned separately by `ResponseNamingTests` against the committed
fixtures in `tests/fixtures/pages/`, which the page tests read from the other side.

## Rule 3 — The route prefix has one authoritative source

`PluginRoutes` (`src/Jellyfin.Plugin.NewReleases/Api/PluginRoutes.cs`) holds it as `const string`.
Every `[Route]` and the Plugin Pages registration payload read it from there. A server-absolute
path is derived by concatenation, never written again.

The two embedded pages each hold one derived `API` literal, because they are static resources with
no build step and `admin.html` is streamed by Jellyfin without passing through plugin code. A test
holds both literals to `PluginRoutes` and names the page that has not followed.

Enforced by `HttpSurfaceTests.EachEmbeddedPage_BuildsItsPathsFromThePrefixItsEndpointsAreServedUnder`.

## Rule 4 — Pages build paths through the host's relative-URL facility

Only `ApiClient.getUrl(...)`. No page may construct a path containing a server address, a scheme,
or an assumed deployment sub-path.

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

Status codes, query parameters, ownership rules and body contents are specified in
`specs/001-track-new-releases/contracts/http-api.md` and
`specs/002-report-data-age/contracts/http-api.md`. No contract may name a route the plugin does not
serve; `HttpSurfaceTests.NoContractDocument_NamesARouteThePluginDoesNotServe` scans for it.

## Deliberate exceptions

| Exception | Reason |
| --- | --- |
| `UserViewController` declares no JSON profile | It serves an HTML fragment. `[Produces("text/html")]` is its correct declaration; a JSON profile would be false. |

No other exception exists. An endpoint that cannot follow a rule is added to this table with its
reason, in the same change that adds the endpoint.
