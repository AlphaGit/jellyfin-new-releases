# HTTP API delta: Polish the New Releases view

This document amends `specs/001-track-new-releases/contracts/http-api.md`. No route is added or
renamed, so the route table in `docs/http-surface.md` does not change. When this feature is
implemented, apply both changes below to the `001` contract, as `005` did for its renames.

## GET `Plugins/NewReleases/Releases`: `ReleaseDto` gains `covers`

```json
{
  "id": 1187,
  "...": "unchanged fields",
  "sources": [
    { "source": "deezer", "url": "https://www.deezer.com/album/6575789" },
    { "source": "musicbrainz", "url": "https://musicbrainz.org/release-group/48117b90-…" }
  ],
  "covers": [
    "https://api.deezer.com/album/6575789/image?size=medium",
    "https://coverartarchive.org/release-group/48117b90-…/front-250"
  ]
}
```

- `covers`: array of absolute URLs, never `null`. Deezer first, then MusicBrainz. One entry per
  source the release has. The page tries them in order (FR-006a).
- `sources` keeps its current order (by source ID). `covers` has its own order.

## GET `Plugins/NewReleases/Artists`: `ArtistDto` gains `disambiguation`

```json
{ "items": [
  { "jellyfinId": "b21e…", "name": "Desire", "disambiguation": "US synthpop band" },
  { "jellyfinId": "c9d0…", "name": "Desire", "disambiguation": "1980s Swedish metal band" },
  { "jellyfinId": "6f3a…", "name": "Chromatics", "disambiguation": null }
] }
```

- `disambiguation` is non-null only when two or more library artists **on the server** share
  the normalized name, and that artist's stored text is non-empty (FR-005a). The check uses
  all artists, including those the caller cannot see. The `items` list still holds only the
  artists the caller can see.
- Order is unchanged: by `name`.

Both fields are camelCase under the existing `[Produces(JsonDefaults.CamelCaseMediaType)]`
(`docs/http-surface.md` rule 2). Update the fixtures `tests/fixtures/pages/releases*.json` and
`artists.json` in the same change, because `ResponseNamingTests` holds the DTOs to them.

## Outgoing: MusicBrainz artist lookup (new)

`GET https://musicbrainz.org/ws/2/artist/{mbid}?fmt=json`, sent through `SourceHttpClient`
(rate limit, daily budget, circuit breaker, `User-Agent`). The plugin reads only the
`disambiguation` field. Fixture: `tests/fixtures/musicbrainz/artist_lookup.json`, recorded and
scrubbed under constitution III.
