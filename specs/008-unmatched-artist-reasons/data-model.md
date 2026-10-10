# Data model: Explain each unmatched artist step by step

## Stored: `artist_source` (migration `002_match_detail.sql`)

One new nullable column. Nothing else in the schema changes.

| Column | Type | Rule |
| --- | --- | --- |
| `match_detail` | `TEXT` (JSON), nullable | Written with every match result. Null when the result is `Matched`, and for every row written before `002`. |

`match_detail` JSON:

```json
{
  "candidates": [ { "name": "Nirvana", "score": 100, "id": "5b11f4ce-…" } ],
  "exactNameCount": 2,
  "albumsCompared": 37
}
```

| Field | Present for | Rule |
| --- | --- | --- |
| `candidates` | every Unmatched result | At most 3, in the order the source returned them. Empty when the search returned nothing. |
| `candidates[].name` | both sources | As the source spells it. |
| `candidates[].score`, `candidates[].id` | MusicBrainz only | The search score and the MusicBrainz artist ID. |
| `exactNameCount`, `albumsCompared` | Deezer `no corroborating album` only | Exact-name candidates checked; first-page albums read across them. |

`unmatched_reason` is unchanged: it stays the record of the failed step and its scores (research
R1).

**Lifecycle**: `SetMatchAsync` overwrites `match_detail` with every result it records. A match is
kept and not tried again, so its null detail stays. An Unmatched result is tried again on every
refresh, so its detail is always from the last attempt.

## Derived: the matching steps

`MatchReasons.Parse(source, reason)` maps a stored reason to the failed step and its values.
Steps before it passed; steps after it were not reached.

| Source | Steps, in order | Reason → failed step (values) |
| --- | --- | --- |
| `musicbrainz` | `Id`, `Search`, `Score`, `Unique` | `no result` → `Search`; `low score (n)` → `Score` (n); `ambiguous (score a vs b)` → `Unique` (a, b) |
| `deezer` | `Name`, `Album` | `no result` → `Name`; `no corroborating album` → `Album` |

`Id` is always failed on an Unmatched MusicBrainz row, because an ID would have matched. Any other
text: no failed step and no trail (spec edge case).

## Read model: `ArtistCounts.Unmatched`

| Record | Fields |
| --- | --- |
| `UnmatchedArtist` | `JellyfinId`, `Name`, `Sources` — one entry per **enabled** source, in the enabled-source order |
| `UnmatchedAt` | `Source`, `Status` (`Matched` / `Unmatched` / `NotChecked`), `Reason` (null unless Unmatched), `Detail` (`MatchDetail?`) |

Listed: an artist with at least one `Unmatched` row at an enabled source (FR-014). No row, or a
`Pending` row, at an enabled source is `NotChecked`.

## In memory: `ArtistMatch`

`ArtistMatch(Status, SourceArtistId, Reason, Detail)`. `Detail` is a `MatchDetail(Candidates,
ExactNameCount, AlbumsCompared)` with `MatchCandidate(Name, Score, Id)`. `Matched(id)` has no
detail; `Unmatched(reason, detail)` has one.

The response shape built from these is in [contracts/http-api.md](./contracts/http-api.md).
