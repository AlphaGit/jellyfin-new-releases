# Contract: IReleaseSource

The one interface in the plugin, with two implementations (`MusicBrainzSource`, `DeezerSource`),
both registered as `IReleaseSource` so the task receives `IEnumerable<IReleaseSource>`. Both use
`SourceHttpClient` for every request (rate limit, daily budget, cooldown, `User-Agent`, retry
with `Retry-After`, redacted logging). Neither reads `PluginConfiguration` directly beyond
enabled flags handled by the task.

```csharp
public interface IReleaseSource
{
    string Id { get; }            // "musicbrainz" | "deezer" — stored in every source-scoped row
    string DisplayName { get; }   // "MusicBrainz" | "Deezer"

    /// FR-002. Never guesses: returns Unmatched with a reason when confidence is low.
    Task<ArtistMatch> MatchArtistAsync(LibraryArtistSnapshot artist, CancellationToken ct);

    /// FR-003. One page of the artist's catalogue starting at offset. NextOffset null = last page.
    Task<CataloguePage> FetchCataloguePageAsync(string sourceArtistId, int offset, CancellationToken ct);

    /// FR-005. Official editions with normalized track lists for one source release id.
    Task<IReadOnlyList<EditionTrackList>> FetchEditionsAsync(string sourceReleaseId, CancellationToken ct);
}
```

Failure semantics: any exception from these methods is a **Failed** outcome for the (artist,
source) pair — the caller records it, never removes data, and continues with the next source or
artist. `DailyBudgetExhaustedException` / cooldown are checked by the caller before calling, and
surface as **Partial** when they interrupt paging.

## Mapping per source

| Concern | MusicBrainz | Deezer |
| --- | --- | --- |
| Match by identifier | `artist.Mbid` present → `Matched(mbid)` without a request | n/a |
| Match by name | `artist?query=artist:"<name>"&limit=5`; top score ≥ 85 and second < top − 5 | `search/artist?q=<name>&limit=25`; exact normalized name AND first albums page contains a library album title |
| Catalogue page | `release?artist=<id>&status=official&inc=release-groups&limit=100&offset=N`; items grouped by release-group id; `NextOffset = offset+100 < release-count ? … : null` | `artist/<id>/albums?index=N&limit=100`; `NextOffset` from `next` presence |
| `CatalogueItem.SourceReleaseId` | release-group MBID | album id |
| `Url` | `https://musicbrainz.org/release-group/<id>` | `link` |
| Types | `primary-type` → Album/EP/Single/Other; `secondary-types[]` → Compilation/Live/Remix/Soundtrack/Other | `record_type`: album→Album, ep→EP, single→Single, compile→Compilation (as primary); no secondaries |
| Date | `first-release-date` (may be `YYYY`, `YYYY-MM`, empty) | `release_date` (`0000-00-00` → null) |
| Editions | `release?release-group=<id>&status=official&inc=recordings+media&limit=25&offset=N`; each release → one edition; tracks = all `media[].tracks[].title` normalized as tracks | `album/<id>/tracks?limit=100` (+`next`); one edition = the album; title = album title |
| Error handling | 503/429 → `Retry-After` → backoff; 404 on artist → `Unmatched("not found")` | HTTP 200 with `error.code 4` → transient (backoff 5 s); other `error` → Failed |

## Fixtures (`tests/fixtures/`)

Recorded once by the developer with `curl` (see quickstart), scrubbed (no keys exist; strip
nothing personal), one file per scenario:

- `musicbrainz/artist_search_confident.json`, `artist_search_ambiguous.json`,
  `artist_search_low_score.json`, `artist_search_empty.json`
- `musicbrainz/releases_page1.json`, `releases_page2.json` (a two-page catalogue with mixed
  primary/secondary types and a year-only date), `releases_empty.json`
- `musicbrainz/editions_two_official.json` (two editions, one with an extra track)
- `musicbrainz/error_503_retry_after.txt`
- `deezer/search_artist_exact.json`, `search_artist_homonyms.json`, `search_artist_empty.json`
- `deezer/artist_albums_page1.json`, `artist_albums_page2.json` (with `next`),
  `artist_albums_unknown_date.json`
- `deezer/album_tracks.json`, `error_quota.json`
