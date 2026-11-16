# Feature Specification: Unfollow artist

**Feature Branch**: `011-unfollow-artist`

**Created**: 2026-11-15

**Status**: Draft

**Input**: User description: "Let me unfollow an artist so that their new releases never show up.
An unfollowed artist goes to its own list, the Unfollowed artists. This is for artists I do not
want new releases for: they are in my library only because they collaborate with an artist I do
follow, or they are a guilty pleasure where I like one album or song and do not want to track
the artist. The Unfollow action sits next to the artist name, so it is not confused with the
Release actions."

## Context

New Releases follows every library artist today. Every library artist gets queried at the
sources, and every Missing or Incomplete Release of that artist can reach the list. A user can
refuse single Releases (**Ignore**), but not a whole artist. Two kinds of artist make this
costly:

- **Collaborators.** An artist is in the library only because they appear on albums of an artist
  the user follows. The user does not want their catalogue.
- **Guilty pleasures.** The user owns one album or song and does not want to track the artist.

For both, the user would have to Ignore every Release one by one, and again for each new one
that appears.

This feature adds an artist-level decision: **Unfollow**. An unfollowed artist produces no new
Releases in the list, and the plugin stops querying the sources for them. The artist is shown in
a separate **Unfollowed artists** list, from where the user can **Follow** again.

## Clarifications

### Session 2026-11-15

- Q: What are the action and the list called? → A: The button is **Unfollow**, and **Follow**
  when the artist is already unfollowed. The list is the **Unfollowed artists** tab. A library
  artist is followed by default. (The word "Ignore" stays with Releases, to avoid a clash.)
- Q: Who is affected? → A: Everyone. The decision is server-wide, like Ignore, Have it and
  Want it.
- Q: What does Refresh do for an unfollowed artist? → A: It skips the artist fully: no source
  fetch and no ownership check. Stored Releases and decisions stay as they are.
- Q: Where does the Unfollowed artists list live? → A: In its own tab, beside List, Want it and
  Archive. It is not part of the Archive.
- Q: Where does the Unfollow button sit? → A: Next to the artist name on every Release row in
  the List, Want it and Archive tabs, clearly apart from the Release actions.
- Q: What does the Unfollowed artists tab show? → A: The artist name as a link to the Jellyfin
  artist page and a Follow button, sorted alphabetically. No counts, no filter.
- Q: Is a confirmation needed? → A: No. The action applies at once and is reversible.
- Q: What happens to Releases that already have a decision? → A: Nothing. Have it, Want it and
  Ignore decisions stay as they are, and those Releases stay in the Want-it list and the Archive.
  Follow changes them as little as Unfollow does.
- Q: Does the Artist filter change? → A: No. Its suggestions stay the same; choosing an
  unfollowed artist in the List tab shows no rows.
- Q: How does the admin page treat unfollowed artists? → A: They count as library artists, on a
  separate line "Unfollowed artists: N", and are left out of the matched and unmatched sections.
  A new admin action, "Clear unfollowed artists", follows every artist again. Purge release data
  and Clear Archive leave unfollowed artists alone.
- Q: What if an unfollowed artist leaves the library? → A: The decision stays stored and applies
  again if the artist returns. The Unfollowed artists tab and its admin count include only
  artists currently in the library.
- Q: After Follow, how are the artist's stored Releases shown, given that no ownership check ran
  while the artist was unfollowed? → A: At once, as stored. A Release for an album bought in the
  meantime shows as Missing until the next Refresh. No pending state and no check at Follow time.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Unfollow an artist I do not want to track (Priority: P1)

A user sees a Release by an artist they do not care about. Next to the artist name, they choose
**Unfollow**. At once, every undecided Release of that artist leaves the New Releases list, for
every user. No confirmation is asked. The artist now shows in the **Unfollowed artists** tab.
From now on, Refresh does not look for new Releases by this artist.

**Why this priority**: This is the core of the feature. Without it, the other stories have
nothing to act on.

**Independent Test**: Unfollow one artist who has several listed Releases. Confirm all of them
leave the list, the artist shows in the Unfollowed artists tab, and a later Refresh adds nothing
for the artist.

**Acceptance Scenarios**:

1. **Given** an artist with listed Releases, **When** the user chooses Unfollow next to the
   artist name, **Then** every Release of that artist with no decision leaves the list at once,
   with no confirmation.
2. **Given** an unfollowed artist, **When** a Refresh runs, **Then** the plugin makes no source
   request and no ownership check for that artist, and no new Release of that artist shows in the
   list.
3. **Given** an artist that another user unfollowed, **When** any user opens the page, **Then**
   that artist's Releases with no decision are not in the list.
4. **Given** the Unfollow button next to an artist name, **When** the user looks at a Release
   row, **Then** the button is clearly apart from the Release actions (Want it, Ignore, Have it)
   and cannot be mistaken for one of them.
5. **Given** a new library artist, **When** the next Refresh runs, **Then** the artist is
   followed.

---

### User Story 2 - Follow an artist again (Priority: P2)

A user opens the **Unfollowed artists** tab and sees every unfollowed library artist, sorted by
name. Each row has the artist name, as a link to the Jellyfin artist page, and a **Follow**
button. Choosing Follow returns the artist to the followed state. The stored Releases of the
artist show in the list again at once. New Releases appear at the next Refresh.

**Why this priority**: An artist-level decision with no way back would be a trap. The tab is
also the only place that answers "whom did I unfollow?".

**Independent Test**: Unfollow an artist, open the Unfollowed artists tab, choose Follow, and
confirm the artist's undecided Releases are in the list again.

**Acceptance Scenarios**:

1. **Given** unfollowed artists, **When** a user opens the Unfollowed artists tab, **Then**
   every unfollowed library artist the user may access shows once, sorted alphabetically, with
   a name link and a Follow button, and with no counts and no filter.
2. **Given** an unfollowed artist, **When** the user chooses Follow, **Then** the artist leaves
   the tab and its stored Releases with no decision show in the list at once.
3. **Given** a followed artist after Follow, **When** the next Refresh runs, **Then** the
   plugin queries the sources for the artist again, and Releases that are new since the Unfollow
   show up.
4. **Given** no unfollowed artists, **When** the user opens the tab, **Then** an empty state
   explains how an artist gets there.
5. **Given** an unfollowed artist whose library the user may not access, **When** the user opens
   the tab, **Then** the artist is not shown to that user.
6. **Given** a Release row of an already unfollowed artist (in the Want-it list or the
   Archive), **When** the user looks at the artist name, **Then** the button says Follow and
   Follow works from there.

---

### User Story 3 - Keep decisions I already made (Priority: P3)

Unfollow and Follow act on the artist only. A Release of the artist that has a Have it, Want it
or Ignore decision keeps it, and stays in the Want-it list or the Archive. The user can still
act on those Releases there, including Restore.

**Why this priority**: The user made those decisions on purpose. Losing them silently would
break trust in the other lists.

**Independent Test**: Mark one Release of an artist Want it and another Ignore. Unfollow the
artist. Confirm both stay where they are. Follow the artist. Confirm both are still there.

**Acceptance Scenarios**:

1. **Given** a Want-it Release of an artist, **When** the user unfollows the artist, **Then**
   the Release stays in the Want-it list.
2. **Given** an Archive Release (Have it or Ignore) of an artist, **When** the user unfollows
   the artist, **Then** the Release stays in the Archive.
3. **Given** an unfollowed artist with Releases in the Want-it list and the Archive, **When**
   the user follows the artist, **Then** those Releases stay where they are.
4. **Given** a Release of an unfollowed artist in the Archive, **When** the user chooses
   Restore, **Then** the Release returns to the New Releases list but is not shown there while
   the artist is unfollowed. It shows again after Follow.
5. **Given** a Want-it Release of an unfollowed artist, **When** the library gains the album,
   **Then** the Release stays in the Want-it list until the artist is followed and a Refresh
   runs. No ownership check runs for an unfollowed artist.

---

### User Story 4 - See and manage unfollowed artists as admin (Priority: P4)

On the admin page, unfollowed artists count as library artists, with their own line
**Unfollowed artists: N**. They are not in the matched counts or in the Unmatched artists
section. A new action, **Clear unfollowed artists**, follows every artist again. **Purge release
data** and **Clear Archive** do not touch unfollowed artists.

**Why this priority**: The admin numbers must still add up, and an administrator needs one way
to undo the decision for everyone. The feature works without it for a short time.

**Independent Test**: Unfollow two artists, one of them Unmatched. Confirm the admin page
shows "Unfollowed artists: 2", the Unmatched artist is no longer in that section, and the
library artist count is unchanged. Run Clear unfollowed artists and confirm both are followed.

**Acceptance Scenarios**:

1. **Given** unfollowed artists, **When** the admin opens the page, **Then** the library artist
   count includes them, and a separate line shows how many are unfollowed.
2. **Given** an Unmatched artist, **When** the user unfollows the artist, **Then** the artist no
   longer shows in the Unmatched artists section.
3. **Given** unfollowed artists, **When** the admin runs Clear unfollowed artists, **Then**
   every artist is followed, and Releases, Want-it decisions and Archive decisions are
   unchanged.
4. **Given** unfollowed artists, **When** the admin runs Purge release data or Clear Archive,
   **Then** the artists stay unfollowed.

---

### Edge Cases

- **An unfollowed artist leaves the library.** The decision stays stored. If the artist
  returns, the artist is unfollowed again with no user action. While absent, the artist is not
  in the Unfollowed artists tab or the admin count.
- **A Release credits an unfollowed collaborator.** A Release is stored under the artist whose
  Refresh found it. A Release stored under a followed artist is not affected when it credits an
  unfollowed one. Unfollow acts on the artist whose Releases are stored.
- **Two users act on one artist at once.** The last decision wins, as for Release decisions.
- **Artist filter on an unfollowed artist.** The suggestions stay as they are. Choosing the
  artist in the List tab shows no rows. In the Want-it list and the Archive it shows the
  artist's decided Releases.
- **An artist with no listed Release.** The user can only Unfollow from a Release row, so an
  artist with no Release in any tab cannot be unfollowed from the page.
- **Follow after a long time.** The stored Releases, and their Missing or Incomplete state, are
  as they were at Unfollow until the next Refresh. A Release for an album the user bought in the
  meantime shows as Missing until then. This is accepted: Follow shows the stored Releases at
  once, and the user can mark such a Release Have it. The page shows its usual "Releases last
  checked" text.
- **An unfollowed artist with no stored decision and no Release.** Nothing is listed for the
  artist. The artist still shows in the Unfollowed artists tab.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Every library artist MUST be followed by default.
- **FR-002**: A user MUST be able to unfollow an artist with one action, labelled "Unfollow",
  placed next to the artist name on every Release row of the List, Want it and Archive tabs. The
  action MUST NOT ask for confirmation.
- **FR-003**: The Unfollow button MUST be visually separate from the Release actions (Want it,
  Ignore, Have it, Restore) so that the two cannot be confused.
- **FR-004**: An Unfollow or Follow decision MUST be server-wide. It applies to every user and
  survives restarts.
- **FR-005**: While an artist is unfollowed, the New Releases list MUST NOT show any Release of
  that artist.
- **FR-006**: Unfollow MUST NOT change any Have it, Want it or Ignore decision. Releases with
  such decisions MUST stay in the Want-it list or the Archive, and the user MUST still be able
  to act on them there.
- **FR-007**: Follow MUST NOT change any Have it, Want it or Ignore decision. After Follow, the
  stored Releases of the artist that have no decision MUST show in the list at once.
- **FR-008**: Refresh MUST skip an unfollowed artist fully: no source fetch, no ownership check,
  and no change to stored Releases, Source entries or decisions of that artist.
- **FR-009**: After Follow, the next Refresh MUST process the artist like any followed artist.
- **FR-010**: The page MUST have a tab "Unfollowed artists", beside List, Want it and Archive. It
  MUST list each unfollowed library artist once, sorted alphabetically by name. A row MUST show
  the artist name as a link to the Jellyfin artist page and a "Follow" button. It MUST show no
  counts and no filter.
- **FR-011**: The Unfollowed artists tab MUST show only artists in libraries the viewer may
  access, and only artists that are currently library artists.
- **FR-012**: When the Unfollowed artists tab is empty, it MUST show an empty state that
  explains how an artist gets there.
- **FR-013**: On a Release row of an unfollowed artist (Want-it list, Archive), the button next
  to the artist name MUST say "Follow". Choosing it MUST follow the artist.
- **FR-014**: The decision of an artist that leaves the library MUST stay stored and apply
  again if the artist returns.
- **FR-015**: Purge release data and Clear Archive MUST NOT remove unfollowed artists.
- **FR-016**: The Artist filter suggestions MUST NOT change. Choosing an unfollowed artist in
  the List tab MUST show no rows.
- **FR-017**: The admin page MUST count unfollowed artists as library artists and show a
  separate line "Unfollowed artists: N". It MUST leave unfollowed artists out of the matched
  counts and out of the Unmatched artists section.
- **FR-018**: The admin page MUST have an action "Clear unfollowed artists" that follows every
  artist again and changes no Release, Want-it decision or Archive decision.
- **FR-019**: A Release stored under a followed artist MUST NOT be affected when it credits an
  unfollowed artist.
- **FR-020**: The upgrade from the previous version MUST keep all stored data. Every artist is
  followed after the upgrade.
- **FR-021**: The domain vocabulary document MUST define the new terms (Followed artist,
  Unfollowed artist, Unfollow, Follow, Unfollowed artists tab) and MUST no longer list
  "tracked artist" and "followed artist" as terms to avoid for Library artist.

### Key Entities

- **Unfollow decision**: An artist-level, server-wide decision that an artist is not tracked.
  It records which artist, who decided and when. It is separate from Release decisions and
  does not change them. Removing it is Follow.
- **Followed artist**: A library artist with no Unfollow decision. The default.
- **Unfollowed artist**: A library artist with an Unfollow decision. Produces no new Releases
  and is skipped by Refresh.
- **Release**, **Release decision**, **Archive**, **Want-it list**: unchanged. See the domain
  vocabulary document.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A user can stop all new Releases from an artist with one action, in one click, and
  without opening another page.
- **SC-002**: After Unfollow, 0 Releases of the artist with no decision appear in the list,
  immediately and after any number of Refreshes.
- **SC-003**: After Unfollow, a Refresh makes 0 source requests for the artist.
- **SC-004**: After Unfollow and again after Follow, every Have it, Want it and Ignore decision
  of the artist is unchanged: 0 decisions lost or moved.
- **SC-005**: A user finds an unfollowed artist and follows it again in under 15 seconds,
  starting from the page.
- **SC-006**: 100% of unfollowed artists survive a Purge release data, a Clear Archive, a
  restart, and an upgrade from the previous version.
- **SC-007**: The admin counts add up: library artists equal followed artists plus unfollowed
  artists, with no artist counted twice.

## Assumptions

- Users and the admin use the existing New Releases page and admin page. No new page is added.
- Unfollow is available to every user who may see the Release, the same as Ignore, Have it and
  Want it. There is no separate permission.
- The decision is stored for the artist, not for a Release, and is not affected by a change in
  the artist's match state at a source.
- A Release is stored under one library artist. Credits to other artists do not move it.
- An artist can be unfollowed only from a Release row, which is where the artist name shows.
  An artist with no Release in any tab is out of reach, which is accepted.
- The decision applies to library artists the plugin already identifies. A merge or rename of an
  artist in Jellyfin follows the same artist identity rules as Release decisions today.
- The feature adds no new setting and no new external source or network call.
