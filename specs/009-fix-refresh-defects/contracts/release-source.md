# Contract amendment: release sources

Amends `specs/001-track-new-releases/contracts/release-source.md`. The interface signatures do
not change. Rows not listed here are unchanged.

| Aspect | MusicBrainz | Deezer |
| --- | --- | --- |
| Match by identifier | `artist.Mbid` parsed by `MusicBrainzIds.Parse` (split on `;`, trim, drop placeholders, distinct). Non-empty → `Matched(string.Join(';', ids))` without a request. | n/a |
| Catalogue page | `release?artist=<first id>&status=official&inc=release-groups+artist-credits&limit=100&offset=N`. With two or more IDs, a release is kept only when its `artist-credit[].artist.id` contains every ID. `NextOffset` from the first ID's `release-count`, as before. | unchanged |
| `CatalogueItem.Credit` | the kept release's `artist-credit`, as `(artist.id, artist.name)`; empty when absent | always empty: album items carry no artist |
| Request shape | every request carries exactly one artist ID | unchanged |

## Placeholders (caller side)

The refresh task, not the source, applies the placeholder rule (research R2). A source returns a
match normally. When that match is a placeholder ID for the source (`PlaceholderArtists`), the
task records `Unmatched("placeholder artist")` at every enabled source and sends no catalogue
request.

## Retry (`SourceHttpClient`)

| Case | Delay before retry n | Next-allowed floor |
| --- | --- | --- |
| No `Retry-After` | `RetryBackoffs[n]` | not set |
| `Retry-After` ≤ backoff (incl. 0 or a past date) | `RetryBackoffs[n]` | not set |
| `Retry-After` > backoff | `Retry-After` | now + `Retry-After` |
