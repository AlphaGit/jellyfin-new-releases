# Contract: HTTP responses carrying the data age

**Feature**: `002-report-data-age`. Amends `specs/001-track-new-releases/contracts/http-api.md`.
Only the fields below change; every other field, route, status code and authorization rule in
`001`'s contract stands.

## Renamed fields

| `001` | `002` | Why |
| ----- | ----- | --- |
| `lastRefreshedAt` | `releasesLastCheckedAt` | The value is now the last completed catalogue fetch, not a run's end. The old name is what invited the defect. |
| `hasCompletedRefresh` | `hasStoredReleases` | `FR-008` regates the empty state on stored data rather than run history. |

Both names change in every response that carries them. No alias is kept: the only consumers are
this plugin's own embedded pages, which ship in the same assembly.

## GET `/Plugins/NewReleases/api/releases`

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

| Field | Rule |
| ----- | ---- |
| `hasStoredReleases` | `true` when any release row exists server-wide, independent of the caller's library access and of whether a refresh has ever run (`FR-008`) |
| `releasesLastCheckedAt` | The newest `last_complete_at` across currently enabled sources; `null` when no enabled source has ever completed a catalogue fetch (`FR-002`) |

`releasesLastCheckedAt` is `null`, never absent, when there is no value. A run that reached no
source leaves it unchanged (`FR-003`).

## GET `/Plugins/NewReleases/api/status`

```json
{ "hasStoredReleases": true, "releasesLastCheckedAt": "…", "refreshIntervalHours": 24, "isRunning": false }
```

Both fields MUST equal the values the list response returns for the same caller at the same
instant (`FR-011`). They come from the same repository call, not a second derivation.

## GET `/Plugins/NewReleases/api/admin/status`

Adds one field to `001`'s response; nothing is removed.

```json
{
  "sources": [SourceStatusDto],
  "lastRun": { "startedAt": "…", "endedAt": "…", "outcome": "Completed", "…": 0 },
  "releasesLastCheckedAt": "2026-09-06T03:14:09Z",
  "nextRunAt": "…",
  "isRunning": false,
  "…": "unchanged"
}
```

| Field | Rule |
| ----- | ---- |
| `lastRun` | Unchanged. Still the last run of any outcome, including one that reached no source (`FR-009`) |
| `releasesLastCheckedAt` | The same instant the user page shows, from the same repository call (`FR-009`, `FR-011`) |
| `sources[].lastSuccessAt` | Unchanged. Per-source last successful call — a different question, kept for diagnostics |

An operator can therefore read `lastRun.endedAt` and `releasesLastCheckedAt` side by side and see
them diverge, which is the point of `FR-009`.
