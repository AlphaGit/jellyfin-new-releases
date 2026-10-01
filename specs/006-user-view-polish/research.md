# Research: Polish the New Releases view

Each entry: **Decision**, **Rationale**, **Alternatives considered**. Facts were measured in this
repository at `a3b3579` unless stated otherwise.

## R1 — The Artist filter is a native `<input list>` + `<datalist>`

**Decision**: `<input id="nr-f-artist" list="nr-f-artist-list" autocomplete="off">` and a
declared `<datalist id="nr-f-artist-list">` with one `<option value="{artistLabel(a)}">` per
artist. On `input` the page looks the text up in a label → `jellyfinId` map. An exact match
applies the filter, and anything else clears it (FR-002). For duplicate labels, the map keeps
the first artist in name order. The Clear button empties the field and the filter.

**Rationale**: The native rung. The person can start typing and gets the browser's substring
match, keyboard navigation and screen-reader semantics, with no widget code (decided
2026-10-01). Matching rules and the number of rows shown are the browser's (FR-001).

**Alternatives considered**: A hand-written ARIA combobox. It gives accent folding and a
20-row cap, but costs about 60 lines of widget code. Rejected by the maintainer in favour of the
native control.

## R2 — Dropped

The page does no matching of its own, so it needs no text folding. The server still uses
`TitleNormalizer.NormalizeName` for collisions (R3).

## R3 — The server decides collisions and sends the text only when it applies

**Decision**: `GET Artists` groups **all** library artists (before the per-viewer access filter)
by `TitleNormalizer.NormalizeName(name)`. An artist in a group of two or more gets
`disambiguation` = its stored text when that text is non-empty. Every other artist gets
`disambiguation: null`. The page shows `name + " — " + disambiguation` when the field is
non-null, and `name` otherwise. It uses that label in the suggestion and in the field after a
pick (FR-005a, FR-005c). The label is also the `<option value>`, so the field shows it after a pick.

**Rationale**: The rule "server-wide for both" (Clarifications) puts the collision check where
the server sees all artists. The page stays dumb. Text that goes stale, because the other
homonym left the library, is suppressed at read time without any write.

**Alternatives considered**: The page detects collisions. Rejected: the page sees only the
viewer's artists, and the rule is server-wide.

## R4 — One MusicBrainz artist lookup per colliding artist, after the rotation

**Decision**: `MusicBrainzSource` gains
`FetchArtistDisambiguationAsync(string mbid, CancellationToken)`. It sends
`GET {Base}artist/{mbid}?fmt=json` through `SourceHttpClient`, so the call shares the rate limit,
the daily budget, the circuit breaker and the `User-Agent`. It returns the `disambiguation`
string (empty when MusicBrainz has none). `RefreshNewReleasesTask` runs a new step after the
artist × source rotation and before ownership:

1. Skip the step when MusicBrainz is disabled or unavailable (`IsAvailableAsync`).
2. Find the candidates: colliding artists (R3 grouping) whose *effective MBID* is
   `library_artist.mbid`, or else the `musicbrainz` `artist_source.source_artist_id` when the
   status is `Matched`, and whose `disambiguation_mbid` differs from that MBID.
3. For each candidate, fetch the text and store `(disambiguation, disambiguation_mbid)`. Store an
   empty text too, so it is not fetched again.
4. On `DailyBudgetExhaustedException`, stop the step. On any other exception, count an error, log
   a warning and continue. A candidate without a stored text is retried on the next run.

**Rationale**: The step runs after the rotation, so artists matched by search in this run
already have an MBID (FR-005b covers them too). `MatchArtistAsync` returns early for tagged
artists, so the search response cannot supply the text for them.

**Alternatives considered**: A method on `IReleaseSource`. Rejected: Deezer has no such concept,
and a no-op implementation is an interface method with one real implementation (constitution VI).
Reading `disambiguation` from the search response for untagged artists. Rejected: it saves one
call for a rare case and needs a second code path.

## R5 — Migration `002` adds two nullable columns to `library_artist`

**Decision**: `002_artist_disambiguation.sql`:
`ALTER TABLE library_artist ADD COLUMN disambiguation TEXT;` and
`ALTER TABLE library_artist ADD COLUMN disambiguation_mbid TEXT;`. `ArtistRepository.UpsertAsync`
does not touch them, so the refresh's library sync keeps them. A new
`SetDisambiguationAsync(id, mbid, text)` writes them.

**Rationale**: Constitution IV requires a forward migration from every released version. 0.1.0
and 0.1.1 have schema `001`. Nullable columns migrate with no default and no backfill.
`disambiguation_mbid` is the "fetch again only when the MBID changes" rule of FR-005b.

**Alternatives considered**: A separate table. Rejected: one-to-one data with no lifecycle of
its own.

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
`<div class="nr-cover"><img src="{covers[0]}" data-fallback="{covers[1..] joined by space}" alt="" loading="lazy" decoding="async" referrerpolicy="no-referrer" width="64" height="64"></div>`.
One `error` listener on the panel, in the capture phase (`error` does not bubble), gets
`nextCover(img)`. That function moves the first fallback URL into `src`, or removes the `<img>`
when no fallback is left. `.nr-cover` is a 64×64 box with a neutral background and a centred
note glyph, so when the `<img>` is removed, the box is the placeholder. The `<img>` uses
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
