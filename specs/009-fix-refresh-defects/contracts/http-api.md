# Contract amendment: administrator status

Amends `specs/001-track-new-releases/contracts/http-api.md` as reshaped by
`specs/008-unmatched-artist-reasons/contracts/http-api.md` and `admin-page.md`. No field is added
or removed; the response shape is unchanged. Two values are new.

## `unmatched[].reasons[]`: the `Placeholder` step

A placeholder artist is `Unmatched` at every enabled source with the stored reason
`placeholder artist`. Each of its entries reads:

| Field | Value |
| --- | --- |
| `state` | `Unmatched` |
| `failedStep` | `Placeholder` (new value, both sources) |
| `steps` | `[ { "name": "Placeholder", "state": "Failed" } ]`: one step only, however the placeholder was found |
| `reason` | `placeholder artist` |
| `candidates`, `exactNameCount`, `albumsCompared` | `null` (left out) |

Example row:

```json
{ "jellyfinId": "…", "name": "Various Artists",
  "reasons": [
    { "source": "musicbrainz", "state": "Unmatched", "failedStep": "Placeholder",
      "steps": [ { "name": "Placeholder", "state": "Failed" } ],
      "reason": "placeholder artist" },
    { "source": "deezer", "state": "Unmatched", "failedStep": "Placeholder",
      "steps": [ { "name": "Placeholder", "state": "Failed" } ],
      "reason": "placeholder artist" }
  ] }
```

## `admin.html`: Unmatched artists table

Additions to `008`'s `admin-page.md`:

- **Reason column**: the step shows as `Placeholder ✗`. The short name is the step name.
- **How to fix column**: a new row in the fix table.

| Source, failed step | Fix |
| --- | --- |
| Either source, `Placeholder` | Placeholder artist, such as Various Artists. If this is a real artist, correct its MusicBrainz ID in Jellyfin's metadata editor or artist.nfo. |

The fix shows once per row, though every enabled source fails at this step (`008` FR-007). The
tracking line reads `Not tracked.`, as for any row with no `Matched` entry.

## `lastRun.outcome`

May also be `Interrupted`: a run left open by a restart, closed when the plugin started.
`admin.html` already prints the outcome word in brackets, so the page shows
`<time> (Interrupted)` with no page change.
