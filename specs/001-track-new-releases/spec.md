# Feature Specification: Track New Releases

**Feature Branch**: `001-track-new-releases`

**Created**: 2026-09-06

**Status**: Draft

**Input**: User description: "Track new music releases by artists already present in the Jellyfin music library that the library does not yet contain, and surface them to users inside the Jellyfin web client: in the same hamburger-menu location as Concert Radar (via Plugin Pages) and, if feasible, as a section inside each artist's detail page."

## Clarifications

### Session 2026-09-06

- Q: Which catalogue sources ship in v1? → A: MusicBrainz plus one open commercial catalogue
  that needs no credentials. Bandcamp is wanted but deferred until Bandcamp answers the pending
  permission request.
- Q: Keep the artist-page New Releases section (User Story 3) in this feature? → A: Drop it.
  The plugin surfaces releases only through the side-menu view; no web-client patching.
- Q: Enforce Jellyfin per-user library access on the list? → A: Yes. A user sees releases only
  for artists in libraries that user may access.
- Q: Show releases whose date is still in the future? → A: Yes, labelled "Upcoming" in their
  own group above the newest month.
- Q: Does a partially present release count as owned? → A: No. The unit of ownership is the
  full release. Auto-detection marks a release owned only when the library album matches the
  catalogue's full track list. Otherwise the release is listed as missing, showing which tracks
  are absent and which source/edition the comparison used. The user can mark any release as
  owned, and that decision supersedes auto-detection.
- Q: Who may dismiss, mark as owned, and undo those? → A: Any authenticated user.
- Q: Does purging release data also erase dismissals and owned overrides? → A: No. Two separate
  admin actions: purge release data, and clear user decisions. Each asks for confirmation.
- Q: Can the admin correct an artist match from the plugin? → A: No override in the plugin.
  Identity is fixed in Jellyfin itself (set the MusicBrainz artist ID). The admin page lists each
  unresolved artist with a link to that artist's Jellyfin metadata page and a short explanation
  of what to set.
- Q: What happens to a stored release a source stops returning? → A: Its listing from that
  source is removed as soon as a complete, successful refresh of the artist from that source no
  longer returns it. Failed fetches and partial fetches never cause removal. A release
  disappears from the list only when no enabled source lists it any more.
- Q: Which open commercial catalogue? → A: Deezer. It exposes explicit release types, dates,
  track lists, and a documented request limit; iTunes Search exposes none of the types.
- Q: How are listings of the same release from both sources handled? → A: Merged into one
  release on artist plus normalized title. MusicBrainz provides the canonical identity and type
  when it lists the release; Deezer fills gaps. The row shows every source's link.
- Q: Add a state filter to the list? → A: Yes: Missing, Incomplete, Upcoming, plus access to
  the hidden list (dismissed and owned-by-decision) for undo.
- Q: Terminology: product and menu name? → A: "New Releases", read as "new to your library". It
  names the plugin, the menu entry, the repository, and the roadmap board.
- Q: Terminology: "source" or "catalogue" for MusicBrainz and Deezer? → A: "Source", as in
  Concert Radar. "Catalogue" is used only for an artist's body of releases.
- Q: Terminology for the work, a version of it, and a source's record of it? → A: "Release" for
  the work as a whole (MusicBrainz "release group", Deezer "album"), "Edition" for one concrete
  version (MusicBrainz "release"). The name for a source's record is settled in a later round.
- Q: Terminology for release states? → A: "Missing" (library has none of it), "Incomplete"
  (library album lacks tracks), "Upcoming" (dated in the future), "In library" (complete, so not
  listed). "Owned" remains the internal name of the automatic check's positive result.
- Q: Terminology for user actions and the bucket they go to? → A: "Ignore" (not wanted), "Have
  it" (counts as in library), "Archive" (where ignored and Have-it releases live), "Restore" as
  the undo for both.
- Q: Terminology for admin actions? → A: "Refresh" (the task), "Run now" (start it), "Purge
  release data", "Clear Archive". Never "Scan" or "Sync".
- Q: Terminology for the followed artist and the failed match? → A: "Library artist" (formerly
  "tracked artist") and "Unmatched" (formerly "unresolved").
- Q: Terminology for a source's record of a Release? → A: "Source entry" (formerly "listing").
- Q: Localization of plugin UI text? → A: English only. Labels are the canonical terms from
  `docs/domain_knowledge/CONTEXT.md`; no string table, no language setting.
- Q: Accessibility baseline for the user page? → A: Keyboard operable end to end, visible
  focus, ARIA roles and labels on filters, buttons, and state badges, and an announced
  confirmation when Ignore, Have it, or Restore succeeds.
- Q: Reference condition for the list-rendering criterion? → A: Browser on the same LAN as
  the server; first meaningful render within 2 s for 500 releases; list API response under
  500 ms.
- Q: Heading for releases without a full date? → A: The list groups by year, not by month. A
  year-only release sits in its year group after fully dated releases. A release with no date at
  all sits in an "Undated" group at the end.
- Q: Display name of the scheduled task in Jellyfin? → A: "Refresh new releases", category
  "New Releases".
- Q: Empty-state and staleness wording? → A: Empty: "No data yet. New Releases is waiting for
  its first refresh." Stale: "Last refreshed <relative time> ago."
- Q: How do source types map to our release types and decide inclusion? → A: Displayed type is
  the first secondary type by precedence Live, Remix, Soundtrack, Compilation, else the primary.
  Included only if the primary type is enabled and every secondary type maps to an enabled type.
  MusicBrainz types outside our list always exclude. Deezer album→Album, ep→EP, single→Single,
  compile→Compilation. Rules recorded in `docs/domain_knowledge/release-types.md`.
- Q: Bootleg handling? → A: "Bootleg" is not a type. A Release counts only if it has at least
  one Official edition; Promotion, Bootleg, and Pseudo-release editions are ignored for
  inclusion and for the track-list comparison.
- Q: When is a name-based artist match acceptable? → A: MusicBrainz: top search result with
  score ≥ 85 and no second result within 5 points. Deezer: exact normalized-name match and at
  least one of the candidate's releases matches the title of an album the library has for that
  artist. Otherwise the artist is Unmatched at that source.
- Q: What is a "normalized title"? → A: Unicode NFKC with diacritics stripped, case-folded,
  "&" → "and", punctuation removed, whitespace collapsed; albums also lose a trailing bracketed
  or dashed edition qualifier; tracks also lose a trailing "feat."/"ft." segment. Titles match
  only when equal after normalization; no fuzzy matching. Rules canonical in
  `docs/domain_knowledge/title-normalization.md`.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Browse releases not in my library (Priority: P1)

A Jellyfin user opens **New Releases** from the web client's side menu (the same place Concert
Radar's **Concerts** entry lives) and sees a list of releases by artists already in the music
library that the library does not contain, across each artist's whole catalogue. Each entry
shows the artist, release title, release type (album, EP, or another enabled type), release
date, its state (Missing, Incomplete, or Upcoming), and a link to the release's page at each
source. The list is newest first and can be filtered by artist, release type, date range, and
state. Artist names link to the artist's page in Jellyfin.

**Why this priority**: This is the whole point of the plugin. Without the list, nothing else has
value.

**Independent Test**: Seed a library with three artists, provide release data in which two of
them have a release the library lacks, open the New Releases view, and confirm exactly those two
releases appear with correct fields and working links.

**Acceptance Scenarios**:

1. **Given** the library contains artist A with albums X and Y, and the sources list A's
   albums X, Y, and Z, **When** a user opens New Releases, **Then** Z is listed under A and X
   and Y are not.
2. **Given** releases exist from several years, **When** the user opens the view, **Then**
   releases are ordered newest first and grouped by year.
3. **Given** the list contains 40 releases, **When** the user filters by one artist, **Then** only
   that artist's releases remain, and clearing the filter restores the full list.
4. **Given** a listed release, **When** the user activates one of its source links, **Then** the
   release's page at that source opens in a new tab.
5. **Given** no refresh has completed yet, **When** the user opens the view, **Then** the view
   shows "No data yet. New Releases is waiting for its first refresh." rather than an empty list.
6. **Given** the user later adds release Z to the library, **When** the next refresh completes,
   **Then** Z no longer appears in the list.
7. **Given** a release dated after today, **When** the user opens the view, **Then** it appears
   in an "Upcoming" group above the newest year, labelled as not yet released.
8. **Given** the library has album W by artist A with 8 of the source's 10 tracks, **When** a
   user opens New Releases, **Then** W is listed as incomplete, naming the 2 missing tracks and
   the source and edition the track list came from.

---

### User Story 2 - Configure and run release tracking (Priority: P2)

A Jellyfin administrator opens the plugin's page in the Dashboard, chooses which sources
to use, supplies any required credentials, chooses which release types count and
optionally a "released since" cutoff, and can trigger a refresh immediately. The page shows
each source's health, the time of the last and next refresh, how many artists have been
processed, and offers **Purge release data** and **Clear Archive**.

**Why this priority**: Without configuration and a running refresh, User Story 1 has no data.
It is second only because a sensible default configuration lets the list work out of the box.

**Independent Test**: Open the admin page on a fresh install, confirm defaults are shown, change
the release-type selection, save, reload, confirm the value persisted, click Run now, and
confirm the status area reports a completed run.

**Acceptance Scenarios**:

1. **Given** a fresh install, **When** the admin opens the plugin page, **Then** at least one
   credential-free source is enabled by default and the page loads without error.
2. **Given** the admin changes a setting and saves, **When** the page is reloaded, **Then** the
   change is still there.
3. **Given** the admin clicks Run now, **When** the run completes, **Then** the status area shows
   the completion time, number of artists processed, and number of releases found.
4. **Given** a source has failed repeatedly, **When** the admin views the page, **Then** that
   source is shown as failing with the last error, and the other sources continue to work.
5. **Given** the admin purges release data, **When** a user opens New Releases, **Then** the list
   is empty until the next refresh, and after that refresh every previously archived release
   is still in the Archive.
6. **Given** a source requires acceptance of its terms before use, **When** the admin has not
   accepted them, **Then** the source stays disabled and the reason is shown.
7. **Given** the admin chooses Clear Archive, **When** the next view loads, **Then** the Archive
   is empty, its releases reappear in the list, and the stored release data is untouched.

---

### User Story 3 - Ignore a release, or say I have it (Priority: P3)

A user marks a listed release **Ignore** (not wanted) or **Have it** (the library has it in a
form the automatic check does not recognise, or the user considers it complete as is). Either
way the release moves to the **Archive** and stops appearing in the New Releases list for every
user of the server. The Archive can be opened from the same view, and any release in it can be
**Restored**.

**Why this priority**: Keeps the list useful over time, but the list is valuable without it.

**Independent Test**: Ignore one release and mark another Have it, reload the view, confirm
both are gone, open the Archive, restore both, confirm they are back.

**Acceptance Scenarios**:

1. **Given** a listed release, **When** the user chooses Ignore, **Then** it moves to the Archive
   immediately and stays there after a refresh.
2. **Given** an archived release, **When** the user restores it, **Then** it reappears in the
   list in its date position.
3. **Given** an Incomplete release W, **When** the user chooses Have it, **Then** W moves to the
   Archive and stays there after later refreshes even though the automatic check still finds
   tracks missing.
4. **Given** a release marked Have it, **When** the library later gains the full track list,
   **Then** the release stays in the Archive; the decision is not reopened.

---

### Edge Cases

- An artist name matches several artists at a source (homonyms): the system must not attribute
  another artist's releases; when confidence is low the artist is reported as unmatched in the
  admin page rather than guessed.
- A library artist has no match at any enabled source: shown as unmatched in the admin
  page with the fix-it link and explanation, excluded from the list, retried on a later refresh.
- The library's copy of a release is titled differently from the source's (deluxe edition,
  remaster, "[Explicit]"): it must still be recognised as the same release; whether it is owned
  or incomplete is then decided by the track-list check against the best-matching edition.
- A source lists a release with only a year: it sits in that year's group after the fully dated
  releases. A release with no date at all sits in an "Undated" group at the end of the list.
- A source is unavailable or rate-limited mid-run: previously stored releases stay visible,
  their staleness is shown, nothing is removed on the strength of that run, and the run resumes
  with the next artist on the next cycle.
- The library has more artists than one refresh can cover within source budgets: artists are
  processed in rotation so every artist is revisited over time and none is starved.
- Track lists are needed only for releases that have a corresponding library album; releases
  the library lacks entirely are listed without fetching their track lists, so the extra
  requests scale with the library, not with the catalogue.
- A prolific artist has hundreds of catalogue releases the library lacks: the list stays usable
  through grouping and filters, and the artist's first refresh is spread across runs if it
  would exceed a source's budget.
- An artist is removed from the library: its releases disappear from the list after the next
  refresh.
- The library holds two artist entries for the same artist (a folder-based entry and a
  metadata-only entry created by a featured credit): they are treated as one library artist when
  they share a public identifier; otherwise only the entry that owns albums is a library artist.
- A user without permission to a music library must not see releases derived from it.
- Plugin Pages is not installed: the admin page and refresh still work; the user view is
  simply not linked from the menu.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST determine the set of library artists from the music libraries: every
  artist that is credited as the album artist of at least one album in the library.
- **FR-002**: System MUST match each library artist to its identity at each enabled source,
  using stable public identifiers from the library's metadata when present and name matching
  otherwise. A name match is accepted at MusicBrainz only when the top result scores at least 85
  and no other result is within 5 points of it; at Deezer only when the candidate's name equals
  the artist's normalized name and at least one of the candidate's releases matches the title of
  an album the library holds for that artist. Anything else leaves the artist Unmatched at that
  source.
- **FR-003**: System MUST retrieve, for each matched artist, every release in the artist's
  catalogue regardless of date, so that back-catalogue gaps are surfaced alongside recent
  releases. Administrators MAY narrow this with a "released since" date filter; the default is
  unrestricted.
- **FR-004**: System MUST include the release types the administrator selected and exclude the
  rest. The default selection is albums and EPs; singles, compilations, live albums, remix
  albums, and soundtracks are excluded until the administrator enables them. A
  release is included only when its primary type is enabled and every secondary type it carries
  maps to an enabled type; its displayed type is the first secondary type by the precedence
  Live, Remix, Soundtrack, Compilation, else the primary type. Source types with no counterpart
  in our list always exclude the release. Only Official editions count: a release with no
  Official edition is excluded, and Promotion, Bootleg, and Pseudo-release editions never take
  part in the track-list comparison. The mapping table is canonical in
  `docs/domain_knowledge/release-types.md`.
- **FR-005**: System MUST treat the full release as the unit of ownership. It MUST first find
  the library album that corresponds to a release, by stable identifier when both sides have
  one and otherwise by normalized title comparison that ignores case, punctuation, and edition
  suffixes. It MUST then mark the release owned only when every track in the source's track list
  has a matching library track by normalized title. When a release exists in several
  editions, the comparison uses the edition whose track list overlaps most with the library
  album.
- **FR-005a**: When a library album corresponds to a release but the track-list match is
  incomplete, System MUST list the release as incomplete, showing the missing track titles and
  the source and edition the track list came from.
- **FR-005b**: Users MUST be able to mark a release Have it. A Have-it decision supersedes
  automatic detection in both directions: the release is archived even if tracks are missing,
  and it stays archived if the automatic check later changes. Users MUST be able to restore it.
- **FR-005c**: "Normalized title" throughout this spec means the deterministic normalization
  defined in `docs/domain_knowledge/title-normalization.md`; two titles match only when they are
  equal after normalization. No similarity scoring is used.
- **FR-006**: System MUST store the resulting missing releases so that the user view does not
  call external sources.
- **FR-006a**: Records of the same release from different sources MUST be merged into one
  Release, matched on artist plus normalized title. When MusicBrainz lists the release, its
  identifier and release type are canonical; otherwise the other source's values are used. The
  Release keeps one Source entry per source, and the user view shows every source's link.
- **FR-007**: System MUST show authenticated users a list of missing releases with artist, title,
  type, date, state, source links, and a link to the artist in Jellyfin, newest first, grouped by
  year. Releases dated after the current day are grouped as "Upcoming" above the newest year and
  labelled as not yet released. The list MUST contain only releases of artists in music libraries
  the requesting user is allowed to access; the Archive follows the same rule.
- **FR-008**: Users MUST be able to filter the list by artist, release type, date range, and
  state (Missing, Incomplete, Upcoming), and to open the Archive (ignored and Have-it releases)
  from the same view.
- **FR-009**: System MUST run a **Refresh** on a schedule and on administrator demand (**Run
  now**). In Jellyfin's Scheduled Tasks the task is named "Refresh new releases" under the
  category "New Releases".
- **FR-010**: System MUST let the administrator enable or disable each source, supply
  credentials where a source requires them, and accept a source's terms where the source
  requires that before use. Sources that require acceptance stay off until accepted.
- **FR-011**: System MUST enforce a per-source request rate and daily request budget, and
  MUST stop using a source for a cooldown period after repeated failures.
- **FR-012**: System MUST show the administrator each source's health, last and next refresh
  time, artists processed, releases found, and unmatched artists. Each unmatched artist MUST
  link to that artist's page in Jellyfin and carry a short explanation of how to fix it: set the
  artist's MusicBrainz ID in Jellyfin's metadata editor or `artist.nfo`; the plugin picks it up
  on the next refresh. The plugin itself stores no identity overrides.
- **FR-013**: Administrators MUST be able to **Purge release data** (all stored releases and
  ownership results) and, separately, to **Clear Archive** (all Ignore and Have-it decisions).
  Each action requires its own confirmation. Purge release data MUST NOT touch the Archive, and
  archived decisions MUST re-apply when the same release is fetched again.
- **FR-014**: System MUST remove a release from the list once the library contains it, its artist
  is no longer a library artist, or no enabled source lists it any more. A release's Source entry
  for a source is dropped only when a complete, successful refresh of that artist from that source
  omits it; a failed fetch or a fetch known to be partial (interrupted, budget-limited, or
  paginated but cut short) MUST NOT remove anything.
- **FR-015**: System MUST show the age of the data behind the list when it is older than one
  refresh interval, as "Releases last checked <relative time> ago". The age is measured from the
  last completed catalogue fetch at an enabled source, not from the end of the last refresh run.
  Superseded in that detail by `specs/002-report-data-age/`; the intent here is unchanged and
  `SC-007` still holds.
- **FR-016**: Any authenticated user MUST be able to Ignore a release, mark it Have it, and
  Restore either from the Archive. These decisions are shared server-wide: once made, the
  release is archived for every user until someone restores it. No administrator role is
  required.
- **FR-017**: System MUST send external sources only artist names and public identifiers, never
  library contents, user identities, or usage data.
- **FR-018**: System MUST identify itself to sources with a product name and version and, when
  configured, the operator's contact.
- **FR-019**: The New Releases page and the Archive MUST be operable by keyboard alone with a
  visible focus indicator, MUST expose filters, buttons, and state badges with accessible roles
  and labels, and MUST announce the result of Ignore, Have it, and Restore to assistive
  technology.

### Key Entities *(include if feature involves data)*

Canonical meanings, boundaries, synonyms to avoid, and UI labels for every term below live in
`docs/domain_knowledge/CONTEXT.md`. This section lists only the attributes this feature needs.

- **Library Artist**: an artist present in the music library; name, Jellyfin identity, the
  music libraries that contain its albums, public identifiers per source, match status per
  source (matched or unmatched), last refresh time.
- **Release**: the work as a whole (an album or EP), as reported by one or more sources; what
  MusicBrainz calls a release group and Deezer an album. Regional, deluxe, and remastered
  editions belong to the same Release. Attributes: artist, title, type, date, canonical identifier,
  first-seen and last-seen times, and one Source entry per source.
- **Source Entry**: one source's record of a Release: the source, the source's identifier, the
  source page link, and the last complete refresh that returned it. A Release is removed when it
  has no Source entry left.
- **Edition**: one concrete version of a Release (a pressing, a deluxe or regional variant); what
  MusicBrainz calls a release. Editions carry the track lists the ownership check compares.
- **Ownership Match**: the judgement about a Release's presence in the library: the library
  album it corresponds to and how it was found (identifier or title), the edition and source
  whose track list was compared, the matched and missing track titles, and the resulting state
  (In library, Incomplete, or Missing).
- **Have it**: a user's decision that a Release counts as In library regardless of the automatic
  check; which release, who decided, when.
- **Source**: an external release database (MusicBrainz, Deezer); enabled flag, credential and
  terms state, rate limit, daily budget, health, cooldown until.
- **Refresh Run**: one scheduled or manual pass; start and end time, artists processed, releases
  found, errors, and for each artist and source whether the fetch was complete, partial, or
  failed.
- **Ignore**: a user's decision that a Release is not wanted; which release, who decided, when.
- **Archive**: the set of Releases with an Ignore or Have-it decision; excluded from the list,
  browsable, each entry restorable.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A user reaches the New Releases list in two interactions from the Jellyfin home
  screen.
- **SC-002**: After one full refresh cycle, at least 95% of library artists with a source
  match have their releases evaluated.
- **SC-003**: On a sample of 50 listed releases, fewer than 3 are in fact In library.
- **SC-004**: A release added to the library disappears from the list within one refresh
  cycle.
- **SC-005**: With 500 releases stored, a browser on the same LAN as the server reaches first
  meaningful render of the list within 2 seconds, and the list request itself completes in under
  500 milliseconds.
- **SC-006**: A library of 500 library artists completes a full refresh within 24 hours without
  exceeding any source's daily budget.
- **SC-007**: With no sources reachable, the list still shows the last stored data and states
  its age.
- **SC-008**: Every user action on the New Releases page (filter, open source link, Ignore, Have
  it, open Archive, Restore) can be completed using only the keyboard.

## Assumptions

- All users of the server see the same list and the same Archive; visibility follows the
  user's library access.
- "Album artist" is the tracking criterion, so artists that appear only as featured credits on
  other artists' albums are not library artists.
- v1 ships two sources, both credential-free: MusicBrainz (identifier-based matching) and Deezer
  (name-based matching). "Source" is the only word for them in UI and code. Both are enabled by
  default. Scraping-based sources are off until terms are accepted, per the constitution.
- Bandcamp is a wanted future source. It is out of scope for this feature until Bandcamp
  answers the permission request already sent; when it does, it becomes its own feature.
- Release dates are compared in UTC calendar days; time of day is irrelevant.
- The user-facing view reaches the side menu through the same optional Plugin Pages mechanism
  Concert Radar uses and degrades to "absent" when it is missing. The plugin does not patch
  the web client, so there is no section inside artist pages.
- Deluxe, remastered, explicit, and regional edition variants are the same release. The
  library's edition is compared with the source's edition whose track list overlaps it most;
  extra tracks in the source's edition make the release incomplete, not a new release.
- All plugin UI text is English only and uses the canonical terms verbatim. There is no
  translation layer and no language setting.
- Refresh runs once a day by default. The interval is changed in Jellyfin's own Scheduled Tasks
  settings, where the plugin's task appears; the plugin adds no second scheduler.
- Jellyfin does not start a scheduled task that is already running, so a manual run during a
  scheduled run is refused by the host, not by the plugin.
