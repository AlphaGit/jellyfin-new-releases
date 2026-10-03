# Research: Polish the New Releases view

Each entry: **Decision**, **Rationale**, **Alternatives considered**. Facts were measured in this
repository at `a3b3579` unless stated otherwise. Re-planned 2026-10-03 at `d88c618`: R3, R4
and R5 are dropped, and R1 now maps names, not labels.

## R1 — The Artist filter is a native `<input list>` + `<datalist>`

**Decision**: `<input id="nr-f-artist" list="nr-f-artist-list" autocomplete="off">` and a
declared `<datalist id="nr-f-artist-list">` with one `<option value="{name}">` per
artist. On `input` the page looks the text up in a name → `jellyfinId` map. A match with case
ignored applies the filter, and anything else clears it (FR-002). The map is keyed by the name in lower case. The Clear button empties the field and
the filter. Two library artists never share a name (R3), so the map needs no duplicate rule.

**Rationale**: The native rung. The person can start typing and gets the browser's substring
match, keyboard navigation and screen-reader semantics, with no widget code (decided
2026-10-01). Matching rules and the number of rows shown are the browser's (FR-001).

**Alternatives considered**: A hand-written ARIA combobox. It gives accent folding and a
20-row cap, but costs about 60 lines of widget code. Rejected by the maintainer in favour of the
native control.

## R2 — Dropped

The page does no matching of its own, so it needs no text folding.

## R3 — Dropped: two library artists never share a name

**Fact** (measured 2026-10-02, cycle 1 of `tdd/cycle-log.md`): `LibraryScanner.Scan` groups
albums by album-artist name with `StringComparer.OrdinalIgnoreCase` before it reads any MBID, and
Jellyfin keeps one artist item per name. A refresh over two tagged "Desire" artists stores one
`library_artist` row. Only spelling variants that `TitleNormalizer.NormalizeName` folds ("Sigur
Rós" / "Sigur Ros") can collide, and those are almost always one artist spelt twice.

**Decision** (2026-10-03): no disambiguation text in 007. A suggestion shows the name alone
(FR-005). Real homonym support needs its own specification, because it needs the scanner and
the filter key to separate same-name artists first.

## R4 — Dropped with R3

There is no MusicBrainz artist lookup and no refresh step.

## R5 — Dropped with R3

There is no migration. The schema stays at `001`.

## R6 — Cover URLs are built in `ToDto` from the stored source IDs

**Decision**: The `sources` JSON in `ReleaseRepository`'s list query also selects
`source_release_id`, so `SourceLink` gains `SourceReleaseId`. `ReleaseDto` gains
`Covers: IReadOnlyList<string>`, in fixed order:

1. `deezer` → `https://api.deezer.com/album/{id}/image?size=medium`
2. `musicbrainz` → `https://coverartarchive.org/release-group/{id}/front-250`

The IDs are URL-escaped. Both images are 250 px, which is sharp at 64 CSS px on a 3× screen.

**Rationale**: FR-006 and FR-006a. The server builds the list, so the page does not know the URL
formats, and the order is testable in C#.

**Alternatives considered**: The page builds the URLs. Rejected: it would duplicate the source
URL formats in a second language.

## R7 — Cover markup: lazy, decorative, no referrer, placeholder by removal

**Decision**: Each card starts with
`<div class="nr-cover"><img src="{covers[0]}" data-fallback="{covers[1..] joined by space}" alt="" loading="lazy" referrerpolicy="no-referrer" width="64" height="64"></div>`.
One `error` listener on the panel, in the capture phase (`error` does not bubble), gets
`nextCover(img)`. That function moves the first fallback URL into `src`, or removes the `<img>`
when no fallback is left. `.nr-cover` is a 64×64 box with a neutral background, so when the `<img>` is removed, the box is the placeholder. The `<img>` uses
`object-fit: cover`. A release with an empty `covers` list renders the box without an `<img>`.

**Rationale**:
- `loading="lazy"` is native and covers FR-007a.
- `alt=""` follows the Clarifications decision.
- `referrerpolicy="no-referrer"` keeps the address of the Jellyfin server out of third-party
  requests (constitution V: the request carries the viewer's address and the image URL, nothing
  else).
- When the `<img>` is removed, a broken-image icon cannot show (SC-002).

**Alternatives considered**: An inline `onerror=` attribute. Rejected: the page writes markup as
strings, and one listener is easier to test. Cover links. Not asked for.

## R8 — Equal buttons through stretch, not fixed widths

**Decision**: `.nr-actions { align-items: stretch; }` and `.nr-actions button { width: 100%; }`.
The actions column of the grid is `auto`, so it is as wide as the widest label. Both buttons fill
it and share both edges. "Restore" already uses the same selector (FR-009).

**Rationale**: Two CSS declarations. A fixed `min-width` would break with longer translated
labels.

## R9 — The source link uses Jellyfin's accent colour

**Decision**: `.nr-links a, .nr-links a:visited { color: #00a4dc; }`. The underline stays.
`:focus-visible` keeps the existing green outline.

**Rationale**: The current colour is the browser default, `#0000ee`: **1.81:1** against the card
(the card is about `#1a1a1a`: Jellyfin dark `#101010` under an 8 % grey overlay). `#00a4dc` gives
**5.96:1** against `#1c1c1c`, and more against the darker real background, so it meets the 4.5:1
of FR-010 and SC-004. `#00a4dc` is the accent of the Jellyfin web client, so the link looks native.
`:visited` must be set too, because the default visited purple has lower contrast still.

**Alternatives considered**: `color: inherit` with underline, like the artist link. This also
passes. Rejected because the source link then looks the same as the artist link, which goes
somewhere else.

## R10 — The card grid gains a cover column and stacks on narrow screens

**Decision**: `.nr-row { grid-template-columns: 64px 1fr auto; }`. Below `600px`, the actions
move to a second row under the details, laid out horizontally with equal widths
(`grid-auto-flow: column; grid-auto-columns: 1fr`).

**Rationale**: This is the narrow-screen edge case: the view has no horizontal scroll, and the
buttons stay equal.

## R11 — What the hermetic suite can and cannot prove about layout

**Decision**: The node tests assert the stylesheet rules that R8, R9 and R10 depend on. They
read the `<style>` block of `user-view.html` and compute the contrast ratio of the declared link
colour against the declared card background. Pixel equality (SC-003) and real lazy loading
(FR-007a) are checked in the real browser pass in `quickstart.md`.

**Rationale**: The fake DOM captures strings and does no layout (`specs/005-page-json-casing/
contracts/page-sandbox.md`). A test that claims to measure pixels would be false. Asserting the
declarations is the strongest hermetic check that exists.
