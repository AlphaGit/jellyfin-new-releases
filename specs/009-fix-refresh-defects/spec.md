# Feature Specification: Fix four refresh defects found on the live server

**Feature Branch**: `009-fix-refresh-defects`

**Created**: 2026-10-10

**Status**: Draft

**Input**: Found on the live server during refresh run 2 (2026-10-10, plugin `0.2.1`, library of
964 artists across 1,808 albums), from the server log and the plugin's stored data.

## Context

The refresh walks every library artist, fetches each artist's catalogue from every enabled
source, and then decides which of those releases the library already owns. On the live server
the second step never started. Two and a half hours into the run, the plugin had stored 231,675
releases, decided ownership for none of them, and was still paging one artist's catalogue. Four
separate defects contributed.

1. **Placeholder artists flood the catalogue.** MusicBrainz keeps a small set of
   *special-purpose artists*: placeholders such as "Various Artists" and "[unknown]" that stand
   for "no single artist" rather than for a real one. The library's "Various Artists" carries
   the Various Artists identifier, and MusicBrainz lists 279,285 official releases under it,
   about 2,800 catalogue pages. The run had stored 187,765 releases for it (81% of everything
   stored) and was still going. The library's "Soundtrack" carries the "[unknown]" identifier
   and brought in 2,851 more. None of these are "new releases by an artist you follow", and
   deciding ownership for them would spend the whole daily request budget.
2. **Joint artists never refresh.** Jellyfin stores more than one MusicBrainz identifier for a
   joint artist, separated by `;`. "Seven Lions + Kill the Noise" carries two. The plugin sends
   the pair to MusicBrainz as if it were one identifier, MusicBrainz rejects the request as
   malformed, and the artist fails on every run.
3. **Retries after "busy" happen at once.** When MusicBrainz is over its rate limit it answers
   "service unavailable" and tells the client it may retry after zero seconds. The plugin
   trusts that value over its own backoff schedule, so its three retries land within about
   three seconds and the artist fails. The server log shows 164 such answers in one day and two
   artists ("Bare & Datsik", "Dr Deimos") failed this way. Jellyfin's own MusicBrainz metadata
   lookups share the server's address and its rate limit, so these answers are expected to
   recur.
4. **An interrupted run never ends.** Run 1 started on 2026-09-20 and has no end time and no
   outcome. A server restart stopped it, and nothing closes a run left open this way. Until the
   next run starts, the administrator page reports the latest run as "(running)" although
   nothing is running.

## Clarifications

### Session 2026-10-10

- Q: When one source identifies a library artist as a placeholder, is the artist skipped at every
  source or only at that one? → A: At every source. A placeholder means "no single artist"; that is
  a fact about the library artist, not about one source.
- Q: How does the administrator page show a placeholder artist? → A: In the unmatched list, with a
  hint written for placeholders instead of the general "set the MusicBrainz ID" hint, so a real
  artist wrongly tagged with a placeholder identifier stays visible. (The hint part is replaced
  by the last answer of this session.)
- Q: What happens to releases already stored for placeholder artists? → A: The plugin has no code
  for them. The maintainer's server is the only install that holds such rows, and they are
  deleted there once, by hand. The plugin only prevents new ones.
- Q: Which releases belong to a joint artist that carries several MusicBrainz identifiers? → A:
  The library artist is the album artist. A joint artist's catalogue is only the releases whose
  album-artist credit at the source lists all of its identifiers; other credited artists are
  allowed. A release is owned when the albums of any library artist credited on it match it, for
  every artist.
  (Replaces an earlier answer in this session: "every release by any member".)
- Q: Does this feature add a way to ignore a whole artist? → A: No. The plugin has only per-release
  Ignore and Have-it decisions. An artist-level ignore is a new capability for its own feature.
- Q: Which other library artists' albums count when deciding ownership? → A: Only those of a
  library artist credited on the release, by MusicBrainz ID where both sides have one, otherwise
  by name. An unrelated artist's album with the same title never counts.
- Q: When is a run left open by a restart closed? → A: When the plugin starts. No refresh can be
  running before the plugin has loaded, and the administrator page is correct right after the
  restart.
- Q: Does SC-001 keep its claim that a full refresh reaches the ownership step within the daily
  budget? → A: No. It keeps only "zero catalogue requests for placeholders". The budget depends on
  library size and edition-fetch volume, which this feature does not change.
- Q: Which outcome does an interrupted run get, given the stored outcome is limited to
  Completed, Cancelled and Failed? → A: A new outcome, "Interrupted". The schema script allows it
  for new installs, with no migration. The maintainer's server, the only existing install, gets
  the wider outcome rule once, by hand, before the fixed version starts.
- Q: Feature `008` replaced the per-row hint with a step trail and a fix per failed source. How
  does a placeholder show there? → A: As a new failed step, "Placeholder", whose fix is the
  placeholder message. (Replaces the earlier answer in this session about a placeholder hint.)
- Q: Which trail does a placeholder show for each source? → A: One step only, "Placeholder"
  failed, on every enabled source, however the placeholder was found. No step that did not run
  is shown as passed.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A refresh skips placeholder artists (Priority: P1)

An operator whose library has compilations tagged "Various Artists", or albums tagged with
another placeholder artist, runs a refresh. The refresh does not fetch catalogues for those
placeholders, finishes in a time proportional to the real artists in the library, and the
release list shows no releases for them. The administrator page says why each placeholder was
skipped.

**Why this priority**: this defect alone stops every run from reaching the ownership step, so
no user sees a correct list until it is fixed.

**Independent Test**: refresh a library that contains a "Various Artists" artist carrying the
MusicBrainz Various Artists identifier and one real artist; confirm no catalogue request is made
for the placeholder, the real artist's releases are found and their ownership decided, and the
release list has no rows for the placeholder.

**Acceptance Scenarios**:

1. **Given** a library artist that carries a MusicBrainz special-purpose artist identifier,
   **When** a refresh runs, **Then** no catalogue or name-search request is sent for that artist
   to any source.
2. **Given** a library artist with no identifier whose name search at any source resolves to
   that source's placeholder, **When** a refresh runs, **Then** the artist is a placeholder and
   no catalogue request is sent for it to any source.
3. **Given** a placeholder artist, **When** the administrator opens the plugin's status, **Then**
   the artist is listed as unmatched, the trail of every enabled source is the single failed step
   "Placeholder", and How to fix says:
   "Placeholder artist, such as Various Artists. If this is a real artist, correct its
   MusicBrainz ID in Jellyfin's metadata editor or artist.nfo." Other unmatched artists keep the
   trails and fixes of feature `008`.
4. **Given** a library of real artists and placeholders, **When** a refresh runs, **Then** every
   real artist is refreshed exactly as before.

---

### User Story 2 - A joint artist refreshes (Priority: P2)

An operator has an album credited to two artists together, and Jellyfin has stored both
MusicBrainz identifiers for that joint artist. The refresh fetches that joint artist's releases
without an error. The release list shows, under the joint artist, only the releases the source
credits to all of its members together, such as "The Blood" by "Seven Lions + Kill The Noise",
and not either member's solo work. A release already in the library under any artist credited on
it is not reported as missing.

**Why this priority**: it affects only joint artists, but each one fails on every run with no
way for the operator to fix it.

**Independent Test**: refresh a library artist that carries two MusicBrainz identifiers; confirm
no malformed request is sent, the fetch completes, and the releases kept are exactly those whose
album-artist credit lists both identifiers.

**Acceptance Scenarios**:

1. **Given** a library artist that carries two or more MusicBrainz identifiers, **When** a
   refresh runs, **Then** every request sent to MusicBrainz carries exactly one identifier.
2. **Given** that artist, **When** its catalogue is fetched, **Then** only releases whose
   album-artist credit lists every one of its identifiers are kept. A credit that also lists other
   artists still counts.
3. **Given** that artist, **When** the refresh finishes, **Then** its fetch outcome is complete,
   not failed.
4. **Given** a library artist with one identifier, **When** a refresh runs, **Then** its catalogue
   is fetched exactly as before.
5. **Given** a release found for one library artist whose album is in the library under a
   different library artist credited on that release, **When** ownership is decided, **Then**
   the release is owned, not missing. This holds for every artist, joint or not.
6. **Given** a release titled "Greatest Hits" by one artist and a library album "Greatest Hits"
   by an artist not credited on that release, **When** ownership is decided, **Then** that
   album is not compared and does not make the release owned or incomplete.

---

### User Story 3 - A busy source is given time before a retry (Priority: P2)

MusicBrainz answers "busy" because the server is over its rate limit, and says it may be retried
at once. The plugin waits at least its own backoff before each retry, so the busy spell passes
and the artist is fetched instead of failing.

**Why this priority**: each failure costs the artist one run's worth of freshness and counts
toward the source's cooldown. The fix is small.

**Independent Test**: have a source answer "busy" with a retry hint of zero seconds and then
succeed; confirm each retry waits at least the plugin's own backoff for that attempt and the
fetch succeeds.

**Acceptance Scenarios**:

1. **Given** a source that answers "busy" with a retry hint of zero, **When** the plugin
   retries, **Then** it waits at least its own backoff for that attempt.
2. **Given** a retry hint longer than the plugin's own backoff, **When** the plugin retries,
   **Then** it waits the hinted time, as today.
3. **Given** a retry hint expressed as a moment that has already passed, **When** the plugin
   retries, **Then** it waits its own backoff and does not fail on the past moment.
4. **Given** a "busy" answer with no retry hint, **When** the plugin retries, **Then** it waits
   its own backoff, as today.

---

### User Story 4 - An interrupted run is closed (Priority: P3)

The server restarts while a refresh is running. When the plugin next starts, that run is
recorded as interrupted, with an end time, so the administrator page never reports a run as
running when none is.

**Why this priority**: cosmetic in effect, but it misleads an operator reading the run history.

**Independent Test**: leave a run with no end time in storage, start the plugin, and confirm the
run now has an end time and an interrupted outcome while finished runs are unchanged.

**Acceptance Scenarios**:

1. **Given** a stored run with no end time, **When** the plugin starts, **Then** that run gets an
   end time and the outcome "Interrupted".
2. **Given** stored runs that already have an end, **When** the plugin starts, **Then** they are
   unchanged.
3. **Given** the live server's existing open run from 2026-09-20, **When** the release carrying
   this fix first starts, **Then** that run is closed the same way.

---

### Edge Cases

- **A placeholder name with no identifier.** The library's "Various Artists" may carry no
  MusicBrainz identifier. A name search then lands on the source's own placeholder, which
  User Story 1 scenario 2 rejects. A real artist whose name merely resembles a placeholder is
  not affected, because the rule keys off the identifier the match resolves to, not the name.
- **Deezer's own placeholder.** Deezer also has a "Various Artists" entry. A Deezer match that
  resolves to it makes the library artist a placeholder, the same as a MusicBrainz one.
- **A joint artist where one identifier is itself a placeholder.** The placeholder identifier is
  dropped and the remaining identifiers are used. If none remain, the artist is a placeholder.
- **Duplicate identifiers or stray spaces** around the separator are treated as one identifier.
- **A joint artist with no release credited to all its identifiers** gets a complete fetch with
  zero releases, not a failure.
- **A retry hint far in the future** (hours) is honoured as today: the plugin waits the hinted
  time before the retry and records it as the source's "next allowed" floor.
- **A run interrupted twice** (two restarts before the next run) is closed once; the second start
  finds nothing open.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: A library artist whose identity at any source is that source's special-purpose
  (placeholder) artist is a placeholder. For a joint artist, this holds only when every one of its
  identifiers is a placeholder (see Edge Cases). The refresh MUST NOT request a catalogue for a
  placeholder from any source.
- **FR-002**: The set of placeholder identifiers MUST be fixed in the plugin and MUST cover every
  artist MusicBrainz designates as special-purpose, plus Deezer's "Various Artists".
- **FR-003**: A name search that resolves to a placeholder at any source MUST make the library
  artist a placeholder at every source, recorded as unmatched with a reason stating the artist is
  a placeholder.
- **FR-003a**: The administrator page MUST list a placeholder among the unmatched artists with
  the placeholder fix from User Story 1 scenario 3. Each enabled source's trail MUST be the single
  failed step "Placeholder", however the placeholder was found. The fix
  appears once per row, though every enabled source fails at that step (`008` FR-007). Every other
  unmatched artist keeps the trails and fixes of feature `008`.
- **FR-004**: The refresh MUST NOT store releases for a placeholder. The plugin has no clean-up
  for releases stored by earlier versions (Clarifications, session 2026-10-10).
- **FR-005**: A library artist carrying several MusicBrainz identifiers MUST be fetched with one
  identifier per request. Its catalogue MUST be only the releases whose album-artist credit at
  the source lists every one of its identifiers; other credited artists are allowed.
- **FR-005a**: Ownership MUST be decided against the albums of the artist the release was found
  for and of every other library artist credited on the release. A library artist is credited
  when its MusicBrainz ID is in the release's credit, or, when either side has no ID, when its
  name matches a credited name. This applies to every artist, joint or not.
- **FR-006**: Each retry after a transient answer MUST wait at least the plugin's own backoff for
  that attempt, whatever retry hint the source gives.
- **FR-007**: A retry hint longer than the plugin's own backoff MUST still be honoured, as today.
- **FR-008**: When the plugin starts, every stored run with no end time MUST be closed with an
  end time and the outcome "Interrupted". New installs MUST accept this outcome. The plugin ships
  no migration for it (Clarifications, session 2026-10-10).
- **FR-009**: Every behaviour not named above MUST be unchanged: catalogue fetches for real
  single-identifier artists, source budgets, cooldowns, and finished run records.

### Key Entities

- **Placeholder artist**: an identity at a source that stands for "no single artist" (Various
  Artists, [unknown], [anonymous], [traditional], and the rest of MusicBrainz's special-purpose
  list; Deezer's Various Artists). Never refreshed, never shown.
- **Joint artist**: one library artist (an album artist) that Jellyfin links to several
  MusicBrainz identities. Its releases are those whose album-artist credit lists all of them.
- **Refresh run**: one execution of the refresh. Gains a fourth outcome, "Interrupted", beside
  Completed, Cancelled and Failed.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A full refresh sends zero catalogue requests for placeholder artists.
- **SC-002**: The release list shows zero releases for placeholder artists.
- **SC-003**: A joint artist's fetch completes on every run, with zero malformed requests.
- **SC-004**: No retry after a "busy" answer happens sooner than the plugin's own backoff for
  that attempt.
- **SC-005**: After a restart during a run, zero runs remain without an end time once the plugin
  has started.
- **SC-006**: 100% of the acceptance scenarios already specified for `001` to `008` still pass,
  unchanged.

## Assumptions

- The placeholder list is short and changes rarely, so a fixed list in the plugin is enough.
  Asking MusicBrainz at run time would cost a request per artist for no practical gain.
- The end time recorded for an interrupted run is the moment the plugin closes it, which is close
  to the restart. The real moment of interruption is not stored anywhere.
- Reducing contention with Jellyfin's own MusicBrainz lookups is out of reach: the plugin cannot
  see or pace them. Waiting properly before retrying is the plugin's part.
- Verification on the live server is the maintainer's own pass after release.

## Out of Scope

- Retrying, later in the same run, an artist that failed after all its retries. The next run
  picks it up, as today.
- Any change to the per-source rate, daily budget, failure threshold or cooldown values.
- A migration that lets existing installs store the "Interrupted" outcome. The maintainer's
  server is the only existing install and is changed once by hand. This is a deliberate
  deviation from constitution IV, which the plan records.
- A way to ignore a whole artist. Only per-release decisions exist; an artist-level ignore is
  its own feature.
- Fetching joint artists from Deezer differently. Deezer has no multi-identifier case.
- Clean-up code for placeholder releases stored by earlier versions. The only install that
  holds them is the maintainer's server, where they are deleted once by hand.
