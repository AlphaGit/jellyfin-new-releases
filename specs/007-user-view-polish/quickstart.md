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
| Source links | `Storage/` tests | each listed source link carries its stored `source_release_id` |
| `GET Releases` | `Api/` tests + `pages/releases.json` | `covers` order and URL formats (FR-006, FR-006a) |
| Artist filter | `tests/web/artist-filter.test.js` | options carry the names, a name applies with case ignored, free text does not filter, Clear (US1) |
| Cover card | `tests/web/render.test.js` | lazy, `alt=""`, no referrer, fallback chain, placeholder (US2) |
| Styles | `tests/web/styles.test.js` | the declarations in `contracts/user-view.md` and the computed contrast (US3, US4) |

## 2. Real-browser pass (yours, after the suite is green)

Install the build on a Jellyfin 12 server with a large music library. Then open
**New Releases** in the dark theme.

1. Type `asp` in Artist. The browser suggests the artists whose name contains "asp". Pick one
   with the keyboard only. The list filters, and the field shows the picked name.
2. Type text that matches nothing, then Tab away. The list is not filtered.
3. Scroll the list. Covers show for Deezer releases. MusicBrainz-only releases show a cover or
   the placeholder, never a broken-image icon. In DevTools → Network, image requests start only
   as cards come near the visible area.
4. "Ignore" and "Have it" have the same width and both edges aligned. "Restore" in Archive looks
   the same.
5. The "MusicBrainz" / "Deezer" links are easy to read. Narrow the window below 600 px: no
   horizontal scroll, and the buttons sit under the details.

A defect found here becomes a new spec (project rule), not a change to this one.
