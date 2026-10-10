# Contract amendment: administrator status

Amends `specs/001-track-new-releases/contracts/http-api.md`, the admin status response. No field
is added or removed; the response shape is unchanged.

## `unmatched[].hint`

Chosen per row:

| Row | `hint` |
| --- | --- |
| Any `sources[].reason` is `placeholder artist` | `Placeholder artist, such as Various Artists. If this is a real artist, correct its MusicBrainz ID in Jellyfin's metadata editor or artist.nfo.` |
| Any other row | `Set the MusicBrainz artist ID in Jellyfin's metadata editor or artist.nfo; the plugin picks it up on the next refresh.` (unchanged) |

Example row:

```json
{ "jellyfinId": "…", "name": "Various Artists",
  "sources": [ { "source": "musicbrainz", "reason": "placeholder artist" },
               { "source": "deezer", "reason": "placeholder artist" } ],
  "hint": "Placeholder artist, such as Various Artists. If this is a real artist, correct its MusicBrainz ID in Jellyfin's metadata editor or artist.nfo." }
```

## `lastRun.outcome`

May also be `Interrupted`: a run left open by a restart, closed when the plugin started.
`admin.html` already prints the outcome word in brackets, so the page shows
`<time> (Interrupted)` with no page change.
