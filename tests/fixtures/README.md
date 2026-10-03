# Test Fixtures

Recorded HTTP response bodies used by contract and unit tests. Tests make no live network calls.

**No API keys, session tokens, or PII may appear in any committed fixture.** Scrub before commit:

```bash
grep -rn "apikey\|api_key\|access_token\|Authorization" tests/fixtures/ --include='*.json' --include='*.txt'
```

Layout: one directory per external source (`musicbrainz/`, `deezer/`), one file per recorded scenario.

`pages/` is different in kind and is **not** a recording. It holds synthetic responses from this
plugin's own endpoints, one file per case the two embedded pages must handle. They are the single
artifact the C# and node suites meet at: `Api/ResponseNamingTests.cs` asserts the server produces
exactly those field names, and `tests/web/render.test.js` and `render-status.test.js` assert the
pages read exactly those field names. That pairing is what makes the Jellyfin 12 naming mismatch
visible without a running host — see `specs/005-page-json-casing/`. Nothing in `pages/` came from a
server, so there is nothing to scrub and nothing to refresh on a schedule; change a file there only
when the contract it states changes.

Recording commands: `specs/001-track-new-releases/quickstart.md`. Large pages were trimmed by hand to
the rows a scenario needs; envelope fields (`release-count`, `total`, `next`) were adjusted to stay
consistent with the trimmed content. Refresh a fixture when a contract test fails or the fixture is
older than 6 months. Do not suppress a failing contract test.

Hand-edited values (the live API did not produce the scenario on the recording day, 2026-09-06):

- `musicbrainz/artist_search_ambiguous.json`: runner-up score set to 96 (recorded 75) so it lies within 5 points of the top result.
- `musicbrainz/artist_search_low_score.json`: derived from the confident search with scores 84 and 40; the nonsense query returned no results.
- `musicbrainz/releases_page1.json`, `releases_page2.json`: `release-count` set to 110 so page 1 has a next page and page 2 is the last.
- `musicbrainz/editions_two_official.json`: a bonus track appended to the second edition (the "extra track" scenario).
- `musicbrainz/error_503_retry_after.txt`, `deezer/error_quota.json`: written by hand from the documented error bodies.
- `deezer/artist_albums_unknown_date.json`: first row's `release_date` set to `0000-00-00`.
