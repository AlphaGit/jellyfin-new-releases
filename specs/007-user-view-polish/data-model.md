# Data Model: Polish the New Releases view

Changes against the schema in `specs/001-track-new-releases/data-model.md`. Nothing else changes.

## Stored: `library_artist` (migration `002_artist_disambiguation.sql`)

| Column | Type | Rule |
| --- | --- | --- |
| `disambiguation` | TEXT NULL | MusicBrainz disambiguation text. `NULL` = never fetched. `''` = fetched, and MusicBrainz has none. |
| `disambiguation_mbid` | TEXT NULL | The MBID the text was fetched for. A fetch happens when this differs from the effective MBID (FR-005b). |

- **Effective MBID**: `library_artist.mbid`, or else `artist_source.source_artist_id` for
  `source = 'musicbrainz' AND status = 'Matched'`. `NULL` when neither exists. Then no fetch
  happens, and the suggestion shows the name alone.
- **Colliding artist**: an artist whose `TitleNormalizer.NormalizeName(name)` equals the
  normalized name of at least one other `library_artist` row. Computed at read time, never
  stored.
- `UpsertAsync` (library sync) MUST NOT write either column. `DeleteMissingAsync` removes them
  with the row.
- Migration: two `ALTER TABLE … ADD COLUMN` statements, no backfill. Forward from `001` (0.1.0,
  0.1.1).

### Fetch states

```text
(NULL, NULL) ──fetch ok──▶ (text|'', mbid)          stored, never refetched
     │                         │
     └─fail/budget─▶ unchanged  └─effective MBID changes─▶ fetch again
```

## Derived: `SourceLink` (read model)

Gains `SourceReleaseId` (from `source_entry.source_release_id`), so `ToDto` can build cover URLs.
The wire `SourceLinkDto` does not change.

## Wire: `ReleaseDto.covers`

`string[]`, never `null`. One URL per source entry. Order: Deezer, then MusicBrainz. The list is
empty only for a release with no source entries, which the repository does not keep.

| Source | URL |
| --- | --- |
| `deezer` | `https://api.deezer.com/album/{source_release_id}/image?size=medium` |
| `musicbrainz` | `https://coverartarchive.org/release-group/{source_release_id}/front-250` |

## Wire: `ArtistDto.disambiguation`

`string | null`. Non-null only when the artist is colliding **and** its stored text is
non-empty.
