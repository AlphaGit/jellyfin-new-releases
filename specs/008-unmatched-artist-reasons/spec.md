# Feature Specification: Explain each unmatched artist step by step

**Feature Branch**: `008-unmatched-artist-reasons`

**Created**: 2026-10-10

**Status**: Draft

**Input**: User description: "Let's change the settings page "Artist / Source / How to fix" with
"Artist / Reason / How to fix". Let's look at the steps that we take to match artists and albums
and provide indications for those. Maybe "Reason" can be unfolded in different columns.
- It should say what worked and what failed. Consider a condensed representation.
- The how to fix should be specific to the failures (ideally one, but more than one could be
  acceptable)."

## Context

The administrator page lists every library artist that is Unmatched at one or more enabled
sources. Each row has three columns: Artist, Sources, and How to fix. The Sources column gives
one short reason per source, for example `Deezer: no corroborating album`. The How to fix column
gives the same text on every row: "Set the MusicBrainz artist ID in Jellyfin's metadata editor or
artist.nfo; the plugin picks it up on the next refresh."

That text is correct only for a MusicBrainz failure. The Deezer match never reads the MusicBrainz
artist ID, so an administrator who follows the hint for a Deezer failure changes nothing. Found on
the maintainer's server after the `0.2.1` install, where many rows showed
`Deezer: no corroborating album` with the MusicBrainz hint beside them.

The page also does not show what succeeded. A row does not tell whether the artist is still
tracked through another source, or which part of the match failed.

### The matching steps today

Each enabled source matches each library artist in a fixed order of steps. The first step that
fails stops the match, and the artist is Unmatched at that source. A match that succeeded once is
kept and is not tried again.

**MusicBrainz**

1. **ID.** The artist, or one of its albums, has a MusicBrainz artist ID in Jellyfin. When
   present, the artist is Matched with that ID and the other steps do not run.
2. **Search.** A name search returns at least one artist. Failure: `no result`.
3. **Score.** The best result scores 85 or more. Failure: `low score (<n>)`.
4. **Unique.** No other result scores within 5 points of the best one. Failure:
   `ambiguous (score <a> vs <b>)`.

**Deezer**

1. **Name.** A name search returns at least one artist whose normalized name is exactly the
   library artist's normalized name. Failure: `no result`.
2. **Album.** For at least one of those artists, one album on the first page of its Deezer albums
   (up to 100) has the same normalized title as an album that the library holds for this artist.
   Failure: `no corroborating album`.

## Clarifications

### Session 2026-10-10

- Q: Does the table explain artist matching only, or also ownership matching (whether the library
  holds a release, from track lists)? → A: Artist matching only. Ownership results belong to
  releases, not artists; explaining them is a separate feature on the release view.
- Q: Does the table show a source that the administrator disabled? → A: No. A disabled source is
  hidden, and an artist that is Unmatched only at disabled sources leaves the table. The stored
  results are kept, so re-enabling the source shows them again. Today such rows are listed only
  because the list does not filter by enabled source.
- Q: Is the step detail built from the stored reason text only, or does the plugin store
  structured detail for each match attempt? → A: Store structured detail: the candidate artists a
  search found and, for Deezer, the counts it checked. Fixes can then point at the exact
  candidates. Rows from before the upgrade show the trail from their reason text, and the extra
  detail after the next refresh.
- Q: Is Reason one column per source, or one column with one line per source? → A: One Reason
  column with one line per source, each line led by the source's name. It extends to more
  sources without widening the table.
- Q: What does a fix show from the stored candidates? → A: The candidates' names, and scores for
  MusicBrainz, at most 3 per source, as plain text without links to the source.
- Q: What is the condensed format of a trail line? → A: The step's short name followed by a
  mark: ✓ passed, ✗ failed, – not reached and shown dimmed. Every step is shown, so each source's
  line has the same steps on every row. Example: `MusicBrainz: ID ✗ · Search ✓ · Score ✗ 70 ·
  Unique –`.
- Q: Does a MusicBrainz fix show each candidate's MusicBrainz ID? → A: Yes, as copyable plain
  text beside its name and score, so the administrator can paste the right one into Jellyfin.
- Q: How does a Matched source show on a listed row? → A: "<Source>: Matched", with no match
  method and no step trail. The plugin stores no match method, and it would change nothing the
  administrator does.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - See a fix that applies to the failure (Priority: P1)

An administrator opens the plugin's settings page and reads the unmatched artists. For each
artist, the How to fix column gives only fixes that act on a step that failed. When no change in
Jellyfin can fix a failure, the page says so, and says whether the artist is still tracked through
another source.

**Why this priority**: the current hint sends the administrator to a change that does nothing for
most Deezer failures. A wrong instruction costs more than no instruction.

**Independent Test**: seed one artist Unmatched at Deezer with `no corroborating album` and Matched
at MusicBrainz. Open the status. The fix for that artist does not mention the MusicBrainz artist
ID, and it says that the artist is tracked through MusicBrainz.

**Acceptance Scenarios**:

1. **Given** an artist Unmatched at MusicBrainz with `low score`, `ambiguous` or `no result`,
   **When** the administrator reads its row, **Then** the fix tells them to set the MusicBrainz
   artist ID in Jellyfin's metadata editor or `artist.nfo`, and that the plugin uses it on the next
   refresh; **and** for `low score` or `ambiguous` it lists up to 3 candidates with their names,
   scores and MusicBrainz artist IDs.
2. **Given** an artist Unmatched at Deezer with `no corroborating album`, **When** the
   administrator reads its row, **Then** the fix gives the number of exact-name Deezer artists
   checked and albums compared, says that no album title matches one in the library, and does
   not mention the MusicBrainz artist ID.
3. **Given** an artist Unmatched at Deezer with `no result` whose search returned artists with
   other names, **When** the administrator reads its row, **Then** the fix lists up to 3 of those
   names and says that Deezer matches by name only; **and given** a search that returned no
   artists, **Then** the fix says that Deezer may not list this artist.
4. **Given** an artist Unmatched at one source and Matched at another, **When** the administrator
   reads its row, **Then** the row says that the artist is still tracked through the matched
   source.
5. **Given** an artist Unmatched at every enabled source, **When** the administrator reads its
   row, **Then** the row says that the artist is not tracked.
6. **Given** an artist Unmatched at both sources, **When** the administrator reads its row,
   **Then** the row gives one fix for each failed source, and no fix twice.

---

### User Story 2 - See which steps passed and which failed (Priority: P2)

For each unmatched artist, the administrator sees each enabled source's steps in order. Each step
is marked as passed, failed, or not reached. The step that failed carries the value that decided
it, for example the scores for an ambiguous MusicBrainz result. The display is compact enough that
one row stays one row on a normal desktop width.

**Why this priority**: it lets the administrator judge a failure that has no direct fix, and tell
a close miss from an artist that a source does not list. It builds on the fixes in User Story 1.

**Independent Test**: seed one artist Unmatched at MusicBrainz with `ambiguous (score 92 vs 90)`.
Open the status. The MusicBrainz trail shows ID failed, Search passed, Score passed,
Unique failed with both scores.

**Acceptance Scenarios**:

1. **Given** an artist Unmatched at MusicBrainz with `low score (70)`, **When** the
   administrator reads its row, **Then** the MusicBrainz trail shows ID failed, Search
   passed, Score failed with the score 70, and Unique not reached.
2. **Given** an artist Unmatched at Deezer with `no corroborating album`, **When** the
   administrator reads its row, **Then** the Deezer trail shows Name passed and Album failed.
3. **Given** an artist Matched at a source, **When** the administrator reads its row, **Then**
   that source's line reads "<Source>: Matched", without a step trail or a match method.
4. **Given** a source that the administrator disabled, **When** the administrator reads any row,
   **Then** that source is not shown.
5. **Given** an artist Unmatched only at a source that the administrator disabled, **When** the
   administrator opens the page, **Then** the artist is not listed; **and when** the
   administrator enables that source again, **Then** the artist is listed again with its stored
   result, before any refresh.
6. **Given** a step marked passed, failed or not reached, **When** a screen reader reads the
   step, **Then** it announces the step's state in words, and the state is not shown by color
   alone.

---

### Edge Cases

- **A reason recorded before the upgrade.** Rows that an older version wrote hold only the short
  reason text and no attempt detail. The step trail is derived from that text. The attempt
  detail (FR-015), and the fixes that point at candidates, appear after the next refresh tries
  the match again; their absence does not stop the trail or a general fix from showing.
- **A reason text that the page does not recognize.** No released version writes one. If it
  occurs, the source's line shows "<Source>: " and the stored text in place of the marks, and
  How to fix gives no fix for that source, because no failed step is known (FR-006).
- **An artist with no Jellyfin artist page.** Out of scope: the name keeps today's link.
- **A MusicBrainz artist ID that is set but wrong.** The source matches by that ID, so the artist
  is Matched and not in this list. Out of scope.
- **A source that has not run for an artist yet.** It has no state, so it shows as not checked,
  with no fix.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The unmatched artists table MUST have the columns Artist, Reason and How to fix,
  in that order. The Reason column replaces the Sources column. Reason holds one line per enabled
  source, led by the source's name, in the same source order on every row.
- **FR-002**: For each listed artist, Reason MUST show every enabled source: Matched, Unmatched,
  or not checked.
- **FR-003**: For each source where the artist is Unmatched, Reason MUST show all of that
  source's matching steps in the order the source runs them, each as its short name followed by a
  mark: ✓ passed, ✗ failed, – not reached (shown dimmed), separated by ` · `. The steps and
  their short names are the ones listed under "The matching steps today": ID, Search, Score and
  Unique for MusicBrainz; Name and Album for Deezer.
- **FR-004**: A failed step MUST show the value that decided it, when the source reported one:
  the best result's score for Score, the two closest scores for Unique.
- **FR-005**: The Reason for one source MUST fit on one line of the table at a desktop width of
  1280 pixels, for an artist name of up to 40 characters.
- **FR-006**: How to fix MUST contain only fixes for steps that failed on that row. Each fix
  names the change to make in Jellyfin, or states that no change in Jellyfin fixes it.
- **FR-007**: How to fix MUST give at most one fix per failed source, and MUST NOT repeat a fix
  on the same row.
- **FR-008**: The fixes for each failed step MUST be:
  - MusicBrainz Search: set the MusicBrainz artist ID in Jellyfin's metadata editor or
    `artist.nfo`; the plugin uses it on the next refresh.
  - MusicBrainz Score or Unique: the same instruction, followed by the candidates the search
    returned, each with its name, score and MusicBrainz artist ID.
  - Deezer Name: when the search returned artists, list them by name and say that if one is this
    artist, its name on Deezer differs from the name in Jellyfin, and that Deezer matches by name
    only; when it returned none, say that Deezer may not list this artist. In both cases no
    change to the MusicBrainz artist ID fixes it.
  - Deezer Album: say how many Deezer artists with this exact name were checked and how many of
    their albums were compared, and that none has the same title as an album in the library,
    ignoring case, punctuation, accents and one trailing edition tag such as `(Deluxe Edition)`.
    Only the first 100 albums of each artist are compared.
- **FR-009**: How to fix MUST state whether the artist is still tracked: "Tracked through
  <source>" when at least one enabled source matched, "Not tracked" when none did.
- **FR-010**: Pass, fail and not-reached MUST be shown with text or a symbol that has a text
  alternative, never with color alone.
- **FR-011**: The page MUST show the step trail for reasons recorded by any released version,
  without a refresh.
- **FR-012**: This feature MUST NOT change how artists are matched or when a source tries a match
  again. The only change to which artists are listed is FR-014.
- **FR-013**: The administrator status response MUST carry the step outcomes and the fixes as
  data, so that the page does not parse reason text to build them.
- **FR-014**: The table MUST list an artist only when it is Unmatched at one or more enabled
  sources. Results of a disabled source MUST be kept, and MUST show again when the source is
  enabled again.
- **FR-015**: Each time a source leaves an artist Unmatched, the plugin MUST record the attempt
  detail beside the reason: the candidate artists that the search returned (name; for
  MusicBrainz also the score and the MusicBrainz artist ID), and for Deezer the number of
  exact-name candidates checked and the number of their albums compared. Recording this detail
  MUST NOT send anything new to a source.
- **FR-016**: A fix MUST list at most 3 candidates per source, in the order the source returned
  them, as plain text. A MusicBrainz candidate's ID MUST be selectable and copyable as text. A
  fix MUST NOT link to a source's site. When the attempt detail is absent (FR-015), the fix
  gives the instruction without the candidates or counts.

### Key Entities

- **Match step**: one check in a source's matching order. Has a name, an order, a state (passed,
  failed, not reached) and an optional detail value, such as a score.
- **Source diagnosis**: one enabled source's result for one library artist: Matched, Unmatched
  with its step trail, or not checked.
- **Match attempt detail**: what one source's last failed match found for one library artist:
  its candidate artists (name; for MusicBrainz also score and artist ID) and, for Deezer, the
  counts checked. Replaced on each new attempt; absent for results written before this feature.
- **Fix**: one instruction tied to one failed step at one source. Has the text shown to the
  administrator and whether a change in Jellyfin can resolve the failure.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: On every unmatched row, every fix shown acts on a step that failed on that row.
  Zero rows show the MusicBrainz artist ID fix for a Deezer-only failure.
- **SC-002**: For any unmatched row, the administrator can tell in under 10 seconds whether the
  artist is still tracked, without reading another part of the page.
- **SC-003**: For any unmatched row, the administrator can name the step that failed at each
  source without reading documentation.
- **SC-004**: Each row stays one line per source at a 1280-pixel desktop width, for an artist
  name of up to 40 characters.
- **SC-005**: After the upgrade and before any refresh, every row recorded by an earlier version
  shows a step trail.

## Assumptions

- **Scope is artist matching only.** The steps shown are the artist-matching steps of each
  source, which include the Deezer album check. Ownership matching (whether the library holds a
  release, from track lists) is out of scope; explaining it is a separate feature on the release
  view.
- **Matching stays the same.** This feature only explains the existing match. Checking more
  Deezer album pages, or a looser match, is a separate feature.
- **Administrator only.** The table is on the administrator page and its data comes from the
  administrator status endpoint, as today. The user-facing New Releases view is unchanged.
