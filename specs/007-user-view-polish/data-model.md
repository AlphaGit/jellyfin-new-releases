# Data Model: Polish the New Releases view

Changes against the schema in `specs/001-track-new-releases/data-model.md`. Nothing else changes.

No stored data changes, and there is no migration. The schema stays at `001` (research R3–R5).

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
