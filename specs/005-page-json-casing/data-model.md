# Phase 1 Data Model: Make the pages read what the server actually sends

**Feature**: `005-page-json-casing` | **Date**: 2026-09-20

No stored data changes. `FR-009` forbids it. The entities here are the two `spec.md` names —
**Response shape** and **HTTP surface convention** — plus the one new source-level entity the fix
introduces, `PluginRoutes`.

---

## 1. Response shape

The names and structure of what an endpoint sends to a browser. Distinct from the shape of the
record a controller returns: the same `ListResponse` reaches the browser as `Items` or `items`
depending on which output formatter the host selects, and selecting it is what this feature makes
the plugin do.

**No DTO in `Api/Dtos.cs` changes.** The records already carry the right property names; the
serializer was choosing a different convention for them.

### 1.1 The field enumeration (`FR-002`)

Extracted mechanically from the two page scripts, not assumed. **37 distinct names**: 20 on
`user-view.html`, 23 on `admin.html`, 6 shared.

#### `GET Plugins/NewReleases/Releases` → `ListResponse` (read by `user-view.html`)

| Wire name | DTO property | Read as | Notes |
| --- | --- | --- | --- |
| `items` | `ListResponse.Items` | `data.items` | |
| `hasStoredReleases` | `ListResponse.HasStoredReleases` | `data.hasStoredReleases` | decides list vs. empty state (`FR-008`) |
| `releasesLastCheckedAt` | `ListResponse.ReleasesLastCheckedAt` | `data.releasesLastCheckedAt` | nullable; absent is a legitimate value (`002`) |
| `refreshIntervalHours` | `ListResponse.RefreshIntervalHours` | `data.refreshIntervalHours` | |
| `total` | `ListResponse.Total` | — | unread |
| `serverToday` | `ListResponse.ServerToday` | — | unread |

`items[]` → `ReleaseDto`:

| Wire name | DTO property | Read as |
| --- | --- | --- |
| `id` | `ReleaseDto.Id` | `item.id` |
| `artistName` | `ReleaseDto.ArtistName` | `item.artistName` |
| `artistJellyfinId` | `ReleaseDto.ArtistJellyfinId` | `item.artistJellyfinId` |
| `title` | `ReleaseDto.Title` | `item.title` |
| `type` | `ReleaseDto.Type` | `item.type` |
| `date` | `ReleaseDto.Date` | `item.date` |
| `state` | `ReleaseDto.State` | `item.state` |
| `missingTracks` | `ReleaseDto.MissingTracks` | `item.missingTracks` |
| `comparedEdition` | `ReleaseDto.ComparedEdition` | `item.comparedEdition` |
| `sources` | `ReleaseDto.Sources` | `item.sources` |
| `archived` | `ReleaseDto.Archived` | `item.archived` |
| `datePrecision` | `ReleaseDto.DatePrecision` | — (unread) |

Nested: `comparedEdition.{source,title}` (`ComparedEditionDto`), `sources[].{source,url}`
(`SourceLinkDto`), `archived.kind` (`ArchivedDto`; `decidedAt` unread).

#### `GET Plugins/NewReleases/Artists` → `ArtistsResponse` (read by `user-view.html`)

`items` → `ArtistDto`: `jellyfinId`, `name`. Both read.

#### `GET Plugins/NewReleases/Admin/Status` → `AdminStatusResponse` (read by `admin.html`)

| Wire name | DTO property | Read as |
| --- | --- | --- |
| `sources` | `Sources` | `status.sources` |
| `lastRun` | `LastRun` | `status.lastRun` |
| `releasesLastCheckedAt` | `ReleasesLastCheckedAt` | `status.releasesLastCheckedAt` |
| `nextRunAt` | `NextRunAt` | `status.nextRunAt` |
| `isRunning` | `IsRunning` | `status.isRunning` |
| `libraryArtists` | `LibraryArtists` | `status.libraryArtists` |
| `unmatched` | `Unmatched` | `status.unmatched` |
| `matchedArtists` | `MatchedArtists` | — (unread) |

`sources[]` → `SourceStatusDto`: `id`, `health`, `lastError`, `callsToday`, `dailyBudget`,
`cooldownUntil` read; `displayName`, `enabled`, `lastSuccessAt` unread.

`lastRun` → `RunDto`: `endedAt`, `outcome`, `artistsProcessed`, `releasesFound`, `errors` read;
`startedAt`, `editionsFetched` unread.

`unmatched[]` → `UnmatchedArtistDto`: `jellyfinId`, `name`, `sources`, `hint` read;
`sources[]` → `UnmatchedSourceDto`: `source`, `reason` read.

#### `GET Plugins/NewReleases/Status` → `StatusResponse`

Read by **neither** page. Kept (`001` specifies it; `FR-009` forbids removing it) and it still
declares the naming under `FR-010`.

### 1.2 Validation rules

- Every name above MUST appear, with that exact spelling, in the response the host writes when the
  endpoint declares `JsonDefaults.CamelCaseMediaType`.
- A `null` value is a value. `releasesLastCheckedAt: null` before any fetch MUST still produce
  `002`'s "no age reported" behaviour and MUST NOT be confused with an unreadable field — that
  distinction is the first Edge Case in `spec.md` and needs its own test.
- No name may be added to a page without being present in the matching fixture.

## 2. HTTP surface convention

The project's rules for how routes are named and how returned fields are named. One document,
`docs/http-surface.md`, linked from `CLAUDE.md`. Full statement in
[`contracts/http-surface.md`](./contracts/http-surface.md).

| Field | Value |
| --- | --- |
| Route segments | PascalCase, multi-word concatenated, no `api` segment |
| Returned object fields | camelCase, declared per endpoint via `[Produces(JsonDefaults.CamelCaseMediaType)]` |
| Path construction in pages | `ApiClient.getUrl(...)` only; never a server address or sub-path |
| Exceptions list | `UserViewController` — serves `text/html`, so it declares no JSON profile |

## 3. `PluginRoutes` (new)

A static class of `const string` members in `src/Jellyfin.Plugin.NewReleases/Api/`. The single
authoritative source for the route prefix (`FR-013`).

| Member | Value | Used by |
| --- | --- | --- |
| `Base` | `Plugins/NewReleases` | `ReleasesController` `[Route]` |
| `Admin` | `Base + "/Admin"` | `AdminController` `[Route]` |
| `UserView` | `Base + "/UserView"` | `UserViewController` `[Route]` |
| `UserViewAbsolute` | `"/" + UserView` | Plugin Pages registration payload |

`const`, not `static readonly`: `[Route]` needs a compile-time constant, and constant concatenation
composes the derived values without a second statement of the prefix.

The two pages keep one `API` literal each. A test holds both to `PluginRoutes`; see
[`research.md` R4](./research.md) for why substitution is not used and what that means for `SC-007`.

## 4. Test fixtures (new)

`tests/fixtures/pages/{releases,artists,admin-status,status}.json` — one committed response per
endpoint, fully populated, every nullable field present in its non-null form, plus the cases the
edge cases require (`hasStoredReleases: false`, `releasesLastCheckedAt: null`).

They are the contract's only physical form. The C# side asserts the server produces exactly these
names; the node side asserts the pages read exactly these names. Committed, never generated by the
code under test.

Scrubbing applies as it does to every fixture (constitution III): no keys, no personal data. These
are synthetic, so nothing real is in them.
