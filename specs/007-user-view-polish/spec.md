# Feature Specification: Polish the New Releases view

**Feature Branch**: `007-user-view-polish`

**Created**: 2026-09-30

**Status**: Draft

**Input**: User description: "Fix four UI issues in the user view: the artist filter should be an
autocomplete because there might be lots of results; each release should show the record's
image, if available; the Ignore and Have it buttons should look more alike; the source link does
not have enough contrast with the background."

## Context

The New Releases view works, but four things make it harder to use than it needs to be on a
real library:

- **Artist filter.** It is a dropdown with one entry per library artist. A library with hundreds
  of artists makes the dropdown a long scroll.
- **No cover image.** A release card has only text. A person recognises a record faster by its
  cover than by its title.
- **Mismatched buttons.** "Ignore" and "Have it" have different widths and right edges, so the
  pair looks unaligned.
- **Low-contrast source link.** The "MusicBrainz" / "Deezer" link uses the browser's default dark
  blue on the dark card background and is hard to read.

## Clarifications

### Session 2026-09-30

- Q: How does a Deezer release get its cover URL? → A: Build it at read time from the stored Deezer album ID (`api.deezer.com/album/{id}/image?size=medium`, which redirects to the image). No new stored data, no migration.
- Q: Which cover wins when a release has both a MusicBrainz and a Deezer entry? → A: Deezer first, then Cover Art Archive, then the placeholder.
- Q: How does the Artist filter tell apart two library artists with the same name? → A: Show the MusicBrainz disambiguation text beside the name ("Desire — US synthpop band").
- Q: When does a suggestion show the disambiguation text? → A: Only when two or more library artists share the name. A unique name shows the name alone.
- Q: What does a screen reader do with the cover? → A: The cover is decorative (empty alt text) and is skipped; the adjacent title names the release.
- Q: When does the plugin get the disambiguation text? → A: During refresh, only for artists whose name collides with another library artist: one MusicBrainz artist lookup each, stored and reused until the MBID changes.
- Q: What does a colliding suggestion show when no disambiguation text is known? → A: The name alone.
- Q: When does a card load its cover? → A: Only when the card scrolls near the visible area (native lazy loading).
- Q: Is a name collision judged per viewer or across the whole server? → A: Across the whole server, for both fetching and display.
- Q: What does the Artist field show after the person picks a homonym? → A: The same label as the suggestion ("Desire — US synthpop band").

### Session 2026-10-01

- Q: Is the Artist filter a native suggestion list or a hand-written autocomplete? → A: The native suggestion list: the person can start typing and gets substring matches. Its matching and row count are the browser's.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Find an artist by typing (Priority: P1)

A person with a large library wants to see the releases of one artist. They type part of the
artist's name into the Artist filter, see the matching library artists, pick one, and the list
shows only that artist's releases.

**Why this priority**: The filter is hardest to use exactly where the plugin is most useful: big
libraries with many tracked artists.

**Independent Test**: Load the view with a library of many artists. Type a fragment of one name.
Pick the suggestion. The list shows only that artist's releases.

**Acceptance Scenarios**:

1. **Given** the library has artists "ASP", "Aspen" and "Wasp", **When** the person types "asp",
   **Then** all three appear as suggestions and no other artist does.
2. **Given** suggestions are shown, **When** the person picks "ASP", **Then** the list shows
   only releases by "ASP", and the filter shows the suggestion's label, "ASP".
3. **Given** an artist is selected, **When** the person clears the filter text or presses Clear,
   **Then** the list shows releases by all artists again.
4. **Given** the person types text that matches no library artist, **When** they leave the field,
   **Then** no artist filter is applied and the field does not pretend a match exists.
5. **Given** the person uses only the keyboard, **When** they type, move through suggestions and
   confirm one, **Then** the filter applies exactly as with a pointer.
6. **Given** two library artists named "Desire" with known disambiguation texts and one artist
   named "Chromatics", **When** the person types "desire" and then "chrom", **Then** each "Desire"
   suggestion shows its disambiguation text and "Chromatics" shows the name alone.

---

### User Story 2 - Recognise a release by its cover (Priority: P2)

A person scanning the list sees each release's cover image next to its title, when a cover is
known for that release.

**Why this priority**: Faster recognition, but the list is usable without it.

**Independent Test**: Load the view with one release that has a known cover and one that does
not. The first shows the cover; the second shows the same layout without a broken image.

**Acceptance Scenarios**:

1. **Given** a release with a known cover, **When** the list renders, **Then** the card shows the
   cover at a fixed thumbnail size beside the release details.
2. **Given** a release with Deezer and MusicBrainz entries whose Deezer cover fails to load,
   **When** the list renders, **Then** the card shows the Cover Art Archive cover.
3. **Given** every cover URL of a release fails to load, **When** the list renders, **Then** the
   card keeps the same alignment and shows a neutral placeholder, never a broken-image icon.
4. **Given** a cover is shown, **When** a screen reader reaches it, **Then** it is skipped as
   decorative (empty alt text). The adjacent title names the release.

---

### User Story 3 - Action buttons read as a pair (Priority: P3)

A person sees "Ignore" and "Have it" as two equal choices: same width, same shape, same style,
aligned on both edges.

**Why this priority**: Cosmetic, but visible on every card.

**Independent Test**: Render a card in the List tab. Both buttons have the same width and the
same left and right edges. In the Archive tab the "Restore" button uses the same style.

**Acceptance Scenarios**:

1. **Given** a card in the List tab, **When** it renders, **Then** "Ignore" and "Have it" have
   equal width, equal height, and share both left and right edges.
2. **Given** a card in the Archive tab, **When** it renders, **Then** "Restore" uses the same
   button style as "Ignore" and "Have it".

---

### User Story 4 - Read the source link (Priority: P3)

A person can read the source link ("MusicBrainz", "Deezer") on the card without effort, in the
same way as the other card text.

**Why this priority**: Readability defect on every card; small change.

**Independent Test**: Measure the contrast of the source link text against the card background
in the dark theme.

**Acceptance Scenarios**:

1. **Given** the default dark Jellyfin theme, **When** a card renders, **Then** the source link
   text has a contrast ratio of at least 4.5:1 against the card background.
2. **Given** the source link, **When** it has keyboard focus or pointer hover, **Then** it stays
   distinguishable as a link (underline or focus outline) and keeps the same contrast.

### Edge Cases

- A library with more than 1,000 artists: typing stays responsive. The browser decides how many
  suggestions it shows.
- Artist names with accents or different case ("Björk", "björk", "Bjork"): the browser's matching
  applies. Current browsers ignore case. Most do not ignore accents, so "bjork" can miss
  "Björk".
- Two library artists with the same name: both appear as separate suggestions, each with its
  MusicBrainz disambiguation text when known, so the person can tell them apart. Without known
  text, both show the name alone and look identical, and a pick applies the first in name order. Two artists without an MBID
  and with the same name share one artist key and are already one row.
- The artist list fails to load: the filter is still usable as "all artists" and the view does not
  break.
- A release with several sources: each source link meets the contrast rule.
- A card on a narrow screen: the cover, details and buttons stay readable without horizontal
  scroll.
- A cover image that is very large or not square: it is shown inside the fixed thumbnail size
  without stretching the card.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The Artist filter MUST be a text input with the browser's native suggestion list of
  library artists. The person can start typing, and the browser shows the suggestions whose
  label contains the typed text. Matching rules (case, accents) and the number of rows shown
  are the browser's.
- **FR-002**: The filter MUST apply only when the field text equals a suggestion label, by a pick
  or by typing it in full. When two artists have the same label, the first one in name order
  applies. Free text that
  matches no artist MUST NOT filter the list and MUST NOT show as an applied filter.
- **FR-003**: Clearing the Artist text, or pressing Clear, MUST remove the artist filter.
- **FR-004**: The Artist filter MUST be fully operable by keyboard and MUST keep its visible
  label "Artist".
- **FR-005a**: When two or more library artists on the server share a name (compared ignoring
  case and accents), each of their suggestions MUST show that artist's MusicBrainz disambiguation text
  beside the name. A suggestion for a unique name, or a colliding artist with no known
  disambiguation text, MUST show the name alone. The collision is judged across all library
  artists, not only the ones the viewer can see.
- **FR-005b**: During refresh, the plugin MUST fetch the disambiguation text only for library
  artists that have an MBID and whose name collides with another library artist. It MUST use
  one MusicBrainz artist lookup per such artist, inside the existing MusicBrainz rate limit and
  daily budget. It MUST store the text and MUST NOT fetch it again unless the artist's MBID
  changes. Artists with a unique name MUST cause no extra request.
- **FR-005c**: After the person picks a suggestion, the Artist field MUST show the same label as
  that suggestion, with the disambiguation text when the suggestion had one.
- **FR-006**: Each listed release MUST carry a cover image URL built at read time from the source
  identifiers already stored for it: `https://coverartarchive.org/release-group/{id}/front-250`
  for a MusicBrainz entry, `https://api.deezer.com/album/{id}/image?size=medium` for a Deezer
  entry. No cover data is stored and no refresh is needed for a cover to appear.
- **FR-006a**: When a release has entries at both sources, the card MUST try the Deezer cover
  first, then the Cover Art Archive cover if the Deezer one fails to load, then show the
  placeholder. A release with one source tries only that source's cover.
- **FR-007**: The release card MUST show the first cover URL that loads as a fixed-size
  thumbnail, and a neutral placeholder of the same size when every cover URL fails to load.
- **FR-007a**: A card MUST request its cover only when the card comes near the visible area.
  Opening the view MUST NOT request covers for cards far below the visible area.
- **FR-008**: The person's browser MUST load the cover directly from the image host of the source
  (Cover Art Archive, or Deezer's album image endpoint). The plugin server MUST NOT fetch, cache or
  proxy cover images.
- **FR-009**: "Ignore", "Have it" and "Restore" MUST share one button style. "Ignore" and
  "Have it" MUST have equal width and share their left and right edges on each card.
- **FR-010**: Source link text MUST have a contrast ratio of at least 4.5:1 against the card
  background in the default dark theme, and MUST stay identifiable as a link.
- **FR-011**: Existing release, artist and archive behaviour MUST be unchanged apart from the
  four items above.

### Key Entities

- **Release** (as listed): gains a cover image URL, derived at read time from its stored source
  identifiers. Nothing new is persisted.
- **Library artist** (filter suggestion): name and Jellyfin identifier, as today, plus an optional
  MusicBrainz disambiguation text. The plugin stores this new field only for artists whose
  name collides, which needs a schema migration.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: With a library of 1,000 artists, a person selects a named artist in under
  5 seconds by typing at most 4 characters.
- **SC-002**: Every release whose source provides a cover shows it. No card shows a broken-image
  icon.
- **SC-003**: On every card, "Ignore" and "Have it" differ by 0 pixels in width and in both edge
  positions.
- **SC-004**: Source link contrast is at least 4.5:1 against the card background in the dark
  theme.

## Assumptions

- The visible changes are in the user view (`user-view.html` under Plugin Pages) only. The admin
  page is out of scope. Behind the view, the refresh, the stored artist data and the Artists and
  Releases responses change to supply disambiguation text and cover URLs.
- Suggestions come from the same library artist list the dropdown uses today. No new search
  endpoint is needed for libraries up to a few thousand artists.
- Every stored Deezer entry has a cover through its album image endpoint. A MusicBrainz release
  group can have a cover in the Cover Art Archive; when it has none, the request fails and the
  placeholder shows.
- Contrast is judged against the default dark Jellyfin theme, which the view is used with today.
- Each viewer's browser contacts the cover endpoints directly. Constitution 1.4.0 (Principle V)
  permits this for cover images from the reporting source; the exact endpoint is an
  implementation detail.
- The buttons stay neutral (no primary/secondary emphasis). Neither choice is preferred.
