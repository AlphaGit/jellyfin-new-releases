# HTTP API delta: Polish the New Releases view

This document amends `specs/001-track-new-releases/contracts/http-api.md`. No route is added or
renamed, so the route table in `docs/http-surface.md` does not change. When this feature is
implemented, apply the change below to the `001` contract, as `005` did for its renames.

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

The field is camelCase under the existing `[Produces(JsonDefaults.CamelCaseMediaType)]`
(`docs/http-surface.md` rule 2). Update the fixtures `tests/fixtures/pages/releases*.json` in the
same change, because `ResponseNamingTests` holds the DTOs to them.
