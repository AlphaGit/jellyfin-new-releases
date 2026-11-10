# Feature Specification: Want it list

**Feature Branch**: `010-want-it-list`

**Created**: 2026-11-10

**Status**: Draft

**Input**: User description: "Add a "Want it" status for albums, alongside the existing Ignore /
Have it actions. Marking an album "Want it" moves it to a separate list, similar to the archive,
that shows all albums marked this way. It works like a shopping list. From that list, the user
can mark an album Ignore (decided not to buy) or "Have it" (finally bought it)."

## Context

A user who sees a Missing or Incomplete Release has two decisions today: **Ignore** (not wanted)
or **Have it** (counts as In library). Both move the Release to the Archive. A third state is
missing: "I want this, but I do not have it yet." Such a Release must leave the New Releases list
so the list stays a list of undecided Releases, but it must not go to the Archive, which holds
closed decisions. The user needs a place to keep these Releases until they buy them, or decide
against them.

## Clarifications

### Session 2026-11-10

- Q: Where does a Want-it Release live? → A: In its own list, shown beside the New Releases list
  and the Archive. It is not part of the Archive. (From the feature request.)
- Q: How does the Want-it list order its Releases? → A: Same as the List: grouped by year, newest
  first, Upcoming above, Undated below, with the same filters (Artist, Type, State, From, To).
- Q: What do the buttons say? → A: Want-it list: "Have it", "Ignore", "Restore". Archive:
  "Restore" and "Want it". List: "Want it", "Ignore", "Have it". "Restore" keeps one meaning: undo
  the decision and return the Release to the New Releases list.
- Q: What can a user do with a Release in the Want-it list? → A: Ignore it (not buying it) or mark
  it Have it (bought it). Both move it to the Archive. (From the feature request.)

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Put a release on my Want-it list (Priority: P1)

A user sees a listed Release and chooses **Want it**. The Release leaves the New Releases list
and appears in the **Want it** list. The user can open the Want-it list from the same page and
see every Release marked this way.

**Why this priority**: This is the core of the feature. Without it, the other stories have
nothing to act on.

**Independent Test**: Mark one Release Want it, confirm it is gone from the list, open the
Want-it list, confirm it is there with its usual details.

**Acceptance Scenarios**:

1. **Given** a listed Release, **When** the user chooses Want it, **Then** it leaves the list
   immediately and appears in the Want-it list.
2. **Given** a Release in the Want-it list, **When** a refresh runs, **Then** it stays in the
   Want-it list.
3. **Given** a Release that another user marked Want it, **When** any user with access to its
   library opens the page, **Then** it shows in the Want-it list, not in the New Releases list.
4. **Given** the Want-it list has no Releases, **When** the user opens it, **Then** an empty
   state explains how a Release gets there.
5. **Given** an archived Release, **When** the user chooses Want it, **Then** it leaves the
   Archive and shows in the Want-it list.
6. **Given** a Want-it Release, **When** a refresh finds the full track list in the library,
   **Then** the Release leaves the Want-it list and is not in the Archive.

---

### User Story 2 - Close a Want-it release: bought or dropped (Priority: P2)

From the Want-it list, the user marks a Release **Have it** (bought it) or **Ignore** (decided
not to buy it). Either way the Release moves to the **Archive**, as the same actions do from the
New Releases list.

**Why this priority**: It completes the shopping-list life cycle. The list is useful without it
for a short time, but it only grows otherwise.

**Independent Test**: Put two Releases on the Want-it list. Mark one Have it and the other
Ignore. Confirm both leave the Want-it list and show in the Archive.

**Acceptance Scenarios**:

1. **Given** a Release in the Want-it list, **When** the user chooses Have it, **Then** it moves
   to the Archive and counts as In library, as for any Have-it decision.
2. **Given** a Release in the Want-it list, **When** the user chooses Ignore, **Then** it moves
   to the Archive as an Ignore decision.
3. **Given** a Release in the Want-it list, **When** the user chooses Restore, **Then** it
   returns to the New Releases list in its date position.

---

### User Story 3 - Keep the Want-it list through refreshes and admin actions (Priority: P3)

A Want-it decision is a user decision. It survives refreshes and **Purge release data**, as
Ignore and Have it do. **Clear Archive** does not remove Want-it decisions.

**Why this priority**: The list loses its value if a routine admin action erases it.

**Independent Test**: Mark a Release Want it. Run Purge release data, then a refresh. Confirm
the Release is in the Want-it list again.

**Acceptance Scenarios**:

1. **Given** a Want-it Release, **When** the admin runs Purge release data and the Release is
   fetched again, **Then** it is in the Want-it list.
2. **Given** a Want-it Release, **When** the admin runs Clear Archive, **Then** it stays in the
   Want-it list.

---

### Edge Cases

- **A Want-it Release becomes In library on its own.** The library gains the full track list
  after the user marked it Want it. The Release leaves the Want-it list by itself, as an
  In-library Release leaves the New Releases list. It does not go to the Archive. The decision
  is kept: if the Release stops being In library, it shows in the Want-it list again.
- **A Release in the Archive.** The user can mark it Want it directly. It leaves the Archive
  and shows in the Want-it list.
- **Two users act on one Release at once.** The last decision wins, as for Ignore and Have it.
- **The Release loses its last Source entry or its artist stops being a library artist.** It
  leaves the Want-it list, by the same rule that removes it from the list. The decision is kept
  and applies again if the Release is fetched again.
- **Library access.** A user sees only Want-it Releases of music libraries that user may access.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Users MUST be able to mark a listed Release **Want it**. The Release leaves the
  New Releases list and is not in the Archive.
- **FR-002**: The page MUST offer a **Want it** list, reachable from the same view as the list
  and the Archive. It shows every Release with a Want-it decision, with the same details as a
  list row.
- **FR-002a**: The Want-it list MUST group and order Releases as the New Releases list does
  (by year, newest first, Upcoming above, Undated below) and MUST offer the same filters.
- **FR-002b**: Each view MUST offer these buttons and no others: List: Want it, Ignore, Have it.
  Want-it list: Have it, Ignore, Restore. Archive: Restore, Want it.
- **FR-003**: From the Want-it list, users MUST be able to mark a Release **Have it**, **Ignore**,
  or **Restore**. Have it and Ignore move the Release to the Archive with the same meaning as
  today. Restore returns it to the New Releases list.
- **FR-004**: A Want-it decision MUST be shared server-wide. Any authenticated user may make or
  change it. What each viewer sees MUST follow the viewer's library access.
- **FR-005**: A Want-it decision MUST survive refreshes and Purge release data, and MUST
  re-apply when the same Release is fetched again.
- **FR-006**: Clear Archive MUST NOT remove Want-it decisions.
- **FR-006a**: A Want-it Release that the automatic check finds In library MUST NOT show in the
  Want-it list. The Want-it decision is kept and applies again if the Release stops being In
  library.
- **FR-006b**: From the Archive, users MUST be able to mark a Release **Want it**. The Release
  leaves the Archive and shows in the Want-it list.
- **FR-007**: A Release has at most one decision at a time: Want it, Ignore, or Have it. A new
  decision replaces the old one.
- **FR-008**: The Want-it action, the Want-it list, and the new buttons MUST be operable by
  keyboard alone with a visible focus, MUST have accessible names and labels, and MUST announce
  the result of Want it to assistive technology, as FR-019 of the first spec requires for the
  existing actions.
- **FR-009**: The new action MUST be available through the same kinds of callers as Ignore and
  Have it, including callers that are not a signed-in user.
- **FR-010**: The user-facing words "Want it" and "Want-it list" MUST be added to the domain
  vocabulary with their meaning, and the other terms MUST stay unchanged.

### Key Entities

- **Want it**: a user's decision that a Release is wanted but not yet in the library. Which
  Release, who decided, when.
- **Want-it list**: the set of Releases with a Want it decision. Shared server-wide, filtered by
  the viewer's library access. Not part of the Archive.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A user can put a Release on the Want-it list with one action, and the Release
  shows there with no page reload.
- **SC-002**: A user can close a Want-it Release as Have it or Ignore with one action.
- **SC-003**: After Purge release data and a refresh, 100% of Want-it decisions show again in
  the Want-it list.
- **SC-004**: Every new action can be completed using only the keyboard.
- **SC-005**: Existing Ignore, Have it, Restore, and Archive behaviour is unchanged for every
  Release that has no Want-it decision.

## Assumptions

- The Want-it list is shown as a third tab, beside the List and Archive tabs.
- Want-it decisions follow the same shared, server-wide rule as Ignore and Have it. There are no
  per-user lists.
- Restore from the Want-it list returns the Release to the New Releases list. The request does
  not name this action. It mirrors Restore in the Archive.
- There is no new admin action. Purge release data keeps Want-it decisions, and Clear Archive
  does not touch them.
- Existing stored decisions from released versions MUST keep working after an upgrade.
