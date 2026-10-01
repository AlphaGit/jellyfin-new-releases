# Quickstart: validate Polish the New Releases view

## 1. Hermetic suite (the gate for this feature)

```bash
PATH=/opt/homebrew/opt/dotnet/bin:$PATH dotnet build --configuration Release   # zero warnings
PATH=/opt/homebrew/opt/dotnet/bin:$PATH dotnet test --configuration Release
node --test "tests/web/*.test.js"
```

Expected: all green. Coverage by area:

| Area | Where | Proves |
| --- | --- | --- |
| Migration `002` | `Storage/` tests | a `001` database gains both columns; library sync keeps them |
| Disambiguation fetch | `ScheduledTasks/` tests, `musicbrainz/artist_lookup.json` | only colliding artists with an MBID are fetched; no refetch; refetch after an MBID change; budget exhaustion stops the step (FR-005b) |
| `GET Artists` | `Api/` tests + `pages/artists.json` | text only on server-wide collision, including one with an artist the caller cannot see (FR-005a) |
| `GET Releases` | `Api/` tests + `pages/releases.json` | `covers` order and URL formats (FR-006, FR-006a) |
| Artist filter | `tests/web/artist-filter.test.js` | options carry the labels, exact label applies, duplicate label → first, free text does not filter, Clear (US1) |
| Cover card | `tests/web/render.test.js` | lazy, `alt=""`, no referrer, fallback chain, placeholder (US2) |
| Styles | `tests/web/styles.test.js` | the declarations in `contracts/user-view.md` and the computed contrast (US3, US4) |

## 2. Real-browser pass (yours, after the suite is green)

Install the build on a Jellyfin 12 server with a large music library. Then open
**New Releases** in the dark theme.

1. Type `asp` in Artist. The browser suggests the artists whose name contains "asp". Pick one
   with the keyboard only. The list filters, and the field shows the picked label.
2. Type text that matches nothing, then Tab away. The list is not filtered.
3. If the library has a homonym, run a refresh. Both suggestions show their disambiguation text.
   A unique name shows only the name.
4. Scroll the list. Covers show for Deezer releases. MusicBrainz-only releases show a cover or
   the placeholder, never a broken-image icon. In DevTools → Network, image requests start only
   as cards come near the visible area.
5. "Ignore" and "Have it" have the same width and both edges aligned. "Restore" in Archive looks
   the same.
6. The "MusicBrainz" / "Deezer" links are easy to read. Narrow the window below 600 px: no
   horizontal scroll, and the buttons sit under the details.

A defect found here becomes a new spec (project rule), not a change to this one.
