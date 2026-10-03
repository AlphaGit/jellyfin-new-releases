# User-view contract: Polish the New Releases view

What `src/Jellyfin.Plugin.NewReleases/Web/user-view.html` renders and exposes after this feature.
The tests in `tests/web/` hold the page to this contract.

## Artist filter (native suggestion list)

Declared markup (ids are declarations, so the fake DOM models them):

```html
<label for="nr-f-artist">Artist</label>
<input id="nr-f-artist" type="text" list="nr-f-artist-list" autocomplete="off" placeholder="All artists">
<datalist id="nr-f-artist-list"></datalist>
```

| Event | Behaviour |
| --- | --- |
| Artists loaded | One `<option value="{name}">` per artist, in response order. Build the name → `jellyfinId` map. |
| `input` | Applied artist = the `artistIndex` entry for the text with case ignored, or none. When it changes, reload the list (FR-002, FR-005). Empty text clears the filter (FR-003). |
| Clear button | Empty the text and clear the applied artist. |

## Exposed on `NewReleasesInternals` (new members)

| Function | Contract |
| --- | --- |
| `artistIndex(artists)` | Map from `name` to `jellyfinId`. |
| `nextCover(img)` | When `img.dataset.fallback` has a URL, move the first one into `img.src`. Otherwise remove `img` from its parent. |

## Release card

```html
<article class="nr-row" ...>
  <div class="nr-cover"><img src="{covers[0]}" data-fallback="{covers[1..] joined by ' '}"
       alt="" loading="lazy" referrerpolicy="no-referrer" width="64" height="64"></div>
  <div> …title, artist, meta, links (unchanged)… </div>
  <div class="nr-actions"> …buttons (unchanged markup)… </div>
</article>
```

- An empty `covers` list renders `<div class="nr-cover"></div>` with no `<img>`.
- The panel has one capture-phase `error` listener that calls `nextCover(e.target)` for
  `img` targets.

## Stylesheet rules the tests read

| Rule | Declaration | Requirement |
| --- | --- | --- |
| `.nr-actions` | `align-items: stretch` | FR-009 |
| `.nr-actions button` | `width: 100%` | FR-009 |
| `.nr-links a, .nr-links a:visited` | `color: #00a4dc`, contrast ≥ 4.5:1 against `#1c1c1c` | FR-010, SC-004 |
| `.nr-cover` | `width: 64px; height: 64px` + neutral background | FR-007 |
| `.nr-cover img` | `object-fit: cover` | edge case "not square" |
| `@media (max-width: 600px)` | actions under the details, equal columns | edge case "narrow screen" |
