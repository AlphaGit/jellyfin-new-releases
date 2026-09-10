# Contract: HTTP API

All routes are served by the plugin inside Jellyfin's ASP.NET host, JSON camelCase, and require
Jellyfin authentication (`[Authorize]`). Admin routes additionally require
`Policies.RequiresElevation`. Confirmation for destructive actions is a client concern
(`Dashboard.confirm` in `admin.html`); the endpoints themselves are idempotent.

Base: `/Plugins/NewReleases`

## User endpoints (`ReleasesController`, any authenticated user)

### GET `/api/releases`

Lists releases visible to the caller (FR-007, FR-008, FR-015).

Query parameters (all optional):

| Name | Type | Meaning |
| --- | --- | --- |
| `artistId` | GUID | Jellyfin artist id (`library_artist.jellyfin_id`) |
| `type` | string | one displayed type: `Album`, `EP`, `Single`, `Compilation`, `Live`, `Remix`, `Soundtrack` |
| `state` | string | `Missing` \| `Incomplete` \| `Upcoming` |
| `from`, `to` | `yyyy-MM-dd` | inclusive bounds on `date_sort`; undated rows are excluded when either is set |
| `archived` | bool | `false` (default) = the list; `true` = the Archive |

Response `200`:

```json
{
  "items": [ReleaseDto],
  "total": 412,
  "hasStoredReleases": true,
  "releasesLastCheckedAt": "2026-09-06T03:14:09Z",
  "refreshIntervalHours": 24,
  "serverToday": "2026-09-06"
}
```

`ReleaseDto`:

```json
{
  "id": 1187,
  "artistName": "Daft Punk",
  "artistJellyfinId": "b2c3…",
  "title": "Random Access Memories",
  "type": "Album",
  "date": "2013-05-17",
  "datePrecision": "Day",
  "state": "Incomplete",
  "missingTracks": ["horizon"],
  "comparedEdition": { "source": "musicbrainz", "title": "Random Access Memories (Japan)" },
  "sources": [
    { "source": "musicbrainz", "url": "https://musicbrainz.org/release-group/…" },
    { "source": "deezer", "url": "https://www.deezer.com/album/…" }
  ],
  "archived": null
}
```

- `datePrecision`: `Day` | `Month` | `Year` | `None`. `date` is `null` when `None`.
- `state` is `Upcoming` when `date > serverToday`, else the ownership state.
- `archived` is `null` in the list; in the Archive it is `{ "kind": "Ignore" | "HaveIt",
  "decidedAt": "…" }`.
- `missingTracks` and `comparedEdition` are present only when `state` is `Incomplete`.
- Order: undated last, then `date_sort` descending, then title. No paging; hard cap 5 000 rows.
- Rows whose artist is in no library the caller may access are omitted; if the caller can
  access no music library the response is an empty list, not an error.

### GET `/api/artists`

Library artists visible to the caller, for the artist filter.

```json
{ "items": [ { "jellyfinId": "b2c3…", "name": "Daft Punk" } ] }
```

### POST `/api/releases/{id}/ignore` · POST `/api/releases/{id}/have-it` · POST `/api/releases/{id}/restore`

Record or remove a decision (FR-005b, FR-016). `ignore` and `have-it` upsert the decision for
the release's natural key with the caller's user id; `restore` deletes it.

| Result | Status |
| --- | --- |
| done | `204 No Content` |
| unknown release id | `404` |
| release not visible to the caller (library access) | `403` |
| no user id claim | `401` |

### GET `/api/status`

Small status for the fragment header (also embedded in the list response; kept for polling).

```json
{ "hasStoredReleases": true, "releasesLastCheckedAt": "…", "refreshIntervalHours": 24, "isRunning": false }
```

## Admin endpoints (`AdminController`, `Policies.RequiresElevation`)

### GET `/api/admin/status`

```json
{
  "sources": [
    { "id": "musicbrainz", "displayName": "MusicBrainz", "enabled": true,
      "health": "Ok", "lastError": null, "callsToday": 812, "dailyBudget": 10000,
      "cooldownUntil": null, "lastSuccessAt": "…" }
  ],
  "lastRun": { "startedAt": "…", "endedAt": "…", "outcome": "Completed",
               "artistsProcessed": 500, "releasesFound": 3210, "editionsFetched": 140, "errors": 2 },
  "releasesLastCheckedAt": "2026-09-06T03:14:09Z",
  "nextRunAt": "2026-09-07T03:00:00+02:00",
  "isRunning": false,
  "libraryArtists": 500,
  "matchedArtists": { "musicbrainz": 488, "deezer": 470 },
  "unmatched": [
    { "jellyfinId": "…", "name": "Blur",
      "sources": [ { "source": "musicbrainz", "reason": "ambiguous (score 100 vs 97)" } ],
      "hint": "Set the MusicBrainz artist ID in Jellyfin's metadata editor or artist.nfo; the plugin picks it up on the next refresh." }
  ]
}
```

`health` ∈ `Ok` | `Failing` | `CoolingDown` | `Disabled`. `nextRunAt` is computed from the task's
triggers (server local time, as Jellyfin fires them) and is `null` when no trigger is set.
`releasesLastCheckedAt` is the same instant the user page reports, so an administrator can see the
last run and the age of the data diverge.

Field names and the meaning of `releasesLastCheckedAt` are settled by
`specs/002-report-data-age/contracts/http-api.md`.

### POST `/api/admin/run-now`

Queues "Refresh new releases" via `ITaskManager.QueueScheduledTask<RefreshNewReleasesTask>()`.
`202 Accepted`. If Jellyfin reports the task already running, `409 Conflict` with
`{ "message": "A refresh is already running." }`.

### POST `/api/admin/purge`

Deletes all `release`, `source_entry`, `edition` rows and resets `artist_source.resume_offset`.
Leaves `decision`, `library_artist`, `artist_source` match status. `204`.

### POST `/api/admin/clear-archive`

Deletes all `decision` rows. `204`.

## Existing endpoints (unchanged)

- GET `/Plugins/NewReleases/UserView` → `Web/user-view.html` fragment for Plugin Pages.
- Jellyfin's plugin configuration endpoints (`ApiClient.getPluginConfiguration` /
  `updatePluginConfiguration` with the plugin GUID) carry `PluginConfiguration`; see
  [plugin-configuration.md](plugin-configuration.md).

## Web page contracts

### `user-view.html` (Plugin Pages fragment; FR-007, FR-008, FR-015, FR-019)

- Root `<div id="nr-user-view">` with an inline IIFE; uses `ApiClient.ajax`/`ApiClient.getUrl`.
- Header: `<h1>New Releases</h1>`; staleness line `Last refreshed <relative> ago.` only when
  stale; empty state text exactly `No data yet. New Releases is waiting for its first refresh.`
- Tabs (`role="tablist"`): **List**, **Archive**.
- Filters (`role="search"`): Artist `<select>`, Type `<select>`, State `<select>`
  (Missing/Incomplete/Upcoming), From/To `<input type="date">`, Clear button.
- Groups: `Upcoming`, one heading per year, `Undated` last (`<h2>`).
- Row: artist link to `#/details?id=<jellyfinId>&serverId=<ApiClient.serverId()>`, title, type
  badge, date, state badge (`role="status"` text, not colour only), `<details>` with missing
  tracks and "compared with <edition> from <source>" when Incomplete, one link per source
  (`target="_blank" rel="noopener"`), buttons **Ignore** and **Have it** (List) or **Restore**
  (Archive).
- `aria-live="polite"` region announces "Ignored <title>", "Marked <title> as Have it",
  "Restored <title>".
- All controls are native `<button>`, `<a>`, `<select>`, `<input>`; focus styles are never removed.

### `admin.html` (Jellyfin config page; FR-009, FR-010, FR-012, FR-013)

- Sections: **Sources** (MusicBrainz, Deezer checkboxes with health text), **Release types**
  (seven checkboxes), **Released since** (`<input type="date">`), **Contact for User-Agent**
  (`<input type="text">`), Save.
- **Status**: last refresh, next refresh, artists processed, releases found; buttons **Run now**,
  **Purge release data**, **Clear Archive** (each with `Dashboard.confirm`).
- **Unmatched artists** table: name (link to the artist's Jellyfin page), sources and reasons,
  the fix hint sentence.
