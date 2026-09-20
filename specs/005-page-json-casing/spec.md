# Feature Specification: Make the pages read what the server actually sends

**Feature Branch**: `005-page-json-casing`

**Created**: 2026-09-20

**Status**: Draft

**Input**: Found by the real-server install of `0.1.0` on Jellyfin 12.1, recorded in
[`docs/real-server-install-0.1.0.md`](../../docs/real-server-install-0.1.0.md) finding 4 and
seeded in [`NOTES.md`](./NOTES.md).

## Context

Everything the plugin does on the server works on Jellyfin 12. The refresh runs, the sources
answer, releases are stored, and the API returns them. **Both pages a person actually looks at
show nothing.**

The New Releases view says "No data yet. New Releases is waiting for its first refresh." with 812
releases stored. The administrator page shows a dash where every status value should be. Neither
is empty because there is nothing to show; both are empty because they cannot read the answer they
were given.

The pages ask for fields by one set of names and the server sends another. On the server this
plugin ran on before, the two agreed. On Jellyfin 12 they do not, and nothing in the plugin
noticed — not the 257 tests, not the build, not the plugin's own logging.

This feature is about making them agree, and about making a test fail the next time they stop
agreeing. The second half matters more: the mismatch itself is a few characters, but the reason it
reached a real server is that the one piece of code which reads a server response is the one piece
with no test.

## Clarifications

### Session 2026-09-20

- Q: Is this a Jellyfin 12 bug or ours? → A: Ours. The plugin never states how its own responses
  should be named and inherits whatever the host does. A host is entitled to change that; a plugin
  that silently depends on it is the thing at fault.
- Q: Scope — the two fields observed failing, or all of them? → A: All. Twenty-four distinct field
  names are read across the two pages and every one is affected. Fixing the observed two would
  leave the same defect everywhere else.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A person sees their releases (Priority: P1)

Someone opens New Releases after a refresh has run. They see the releases their library is missing,
grouped by year, with the artist, type, date, state and source links — and they can filter, and
they can Ignore or mark Have it. What they see matches what the server holds.

**Why this priority**: it is the plugin's entire purpose, and it does not work.

**Independent Test**: with releases stored, open the view and confirm the list matches what the
API returns.

**Acceptance Scenarios**:

1. **Given** releases are stored, **When** a person opens the view, **Then** the releases appear,
   and the "waiting for its first refresh" message does not.
2. **Given** no releases are stored, **When** a person opens the view, **Then** that message does
   appear. The empty state must stay reachable for the reason it exists.
3. **Given** releases are stored, **When** a person filters by artist, type, state or date,
   **Then** the list narrows accordingly.
4. **Given** a release in the list, **When** it is shown, **Then** its artist, title, type, date,
   state, missing track titles, compared edition and source links are all populated — not blank.

---

### User Story 2 - An administrator sees the plugin's true state (Priority: P1)

An administrator opens the plugin's page to see whether it is working: when the releases were last
checked, when the last refresh ran, how each source is behaving, and which artists could not be
matched. Every one of those reads a value the server sent.

**Why this priority**: this page is how an operator decides whether anything is wrong. Showing a
dash for every value is worse than showing nothing, because it looks like a plugin that has never
run rather than one that cannot read.

**Independent Test**: with a completed refresh on record, open the administrator page and confirm
each status value matches the API.

**Acceptance Scenarios**:

1. **Given** a completed refresh, **When** the administrator page is opened, **Then** the last
   refresh instant, the releases-last-checked instant, the next run, the artists processed and the
   releases found all show their real values.
2. **Given** source activity on record, **When** the page is opened, **Then** each source's
   health, calls today, daily budget, cooldown and last error are shown.
3. **Given** artists that no source matched, **When** the page is opened, **Then** they are listed
   with their reason.
4. **Given** data older than one refresh interval, **When** either page is opened, **Then** the
   staleness wording specified by `002` appears. That feature's acceptance criteria are currently
   unmet on Jellyfin 12 for the same reason.

---

### User Story 3 - The mismatch cannot return unnoticed (Priority: P1)

A developer changes a response, or a future Jellyfin changes how responses are named. A test fails.
Nobody finds out from a person opening the page on a real server.

**Why this priority**: equal to the other two. Without it this feature fixes one instance of a
defect whose cause is still present.

**Independent Test**: change the naming of the server's response and confirm the suite fails.

**Acceptance Scenarios**:

1. **Given** the suite, **When** the server's responses are renamed in the way Jellyfin 12 renamed
   them, **Then** at least one test fails.
2. **Given** the suite, **When** a page is changed to read a field the server does not send,
   **Then** at least one test fails.
3. **Given** the page logic that decides between the list and the empty state, **When** the suite
   runs, **Then** that decision is exercised against a response of the shape the server really
   sends.

---

### Edge Cases

- **A response field that is legitimately absent**, such as the last-checked instant before any
  fetch has completed. Distinguishable from a field the page simply cannot read; the first must
  still produce `002`'s "no age reported" behaviour, the second must fail a test.
- **The Archive tab**, which asks for a different set and must be checked too, not assumed to
  share the list's fate.
- **A host that changes naming again.** The fix must not merely match Jellyfin 12; it must remove
  the plugin's silent dependence on whatever the host happens to do.
- **The fields no observation covered.** Twenty-four are read; only a handful were seen failing.
  The rest must be established, not presumed correct.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Both embedded pages MUST display the data the server sends, on Jellyfin 12.
- **FR-002**: Every field either page reads MUST be covered, not only the fields observed failing.
  The set MUST be enumerated from the pages rather than assumed.
- **FR-003**: The plugin MUST NOT silently depend on the host's choice of response naming. Either
  it states the naming for its own responses, or the pages read a response whatever the host's
  choice — and whichever is chosen MUST be recorded with its reason.
- **FR-004**: The empty state MUST remain reachable when there is genuinely nothing to show. A fix
  that makes the list always render would replace one wrong answer with another.
- **FR-005**: Every behaviour `001` and `002` specify for the pages MUST hold on Jellyfin 12,
  including `002`'s staleness wording, which reads an affected field.
- **FR-006**: A test MUST fail if the server's response naming changes in the way that caused this
  defect.
- **FR-007**: A test MUST fail if a page reads a field the server does not send.
- **FR-008**: The decision between rendering the list and rendering the empty state MUST be
  exercised by a test against a response of the shape the server really produces.
- **FR-009**: This feature MUST NOT change what the API returns to any other caller, nor any
  server-side behaviour. It is about the pages and the tests that cover them.

### Key Entities

- **Response shape**: the names and structure of what a plugin endpoint sends to a browser. Fixed
  by the host's serializer unless the plugin states otherwise, and — as this defect shows — not
  the same thing as the shape of the object a controller returns.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: With releases stored, the view lists them; with none stored, it shows the empty
  state. Both verified against a real server.
- **SC-002**: Every status value on the administrator page shows a real value after a completed
  refresh; none shows a dash.
- **SC-003**: All 24 field names read by the two pages resolve against a real response.
- **SC-004**: Renaming the server's response in the way Jellyfin 12 renamed it fails the suite.
- **SC-005**: 100% of the acceptance scenarios specified for `001` and `002` that concern the
  pages hold on Jellyfin 12.

## Assumptions

- The pages were written against Jellyfin 10.11, where the host produced the naming they expect.
  This is inferred from the pages having been written that way and having worked, not measured
  against a 10.11 server; it does not change what must be done.
- No stored data is wrong. The 812 releases on the review server were read back correctly through
  the API; only the pages cannot display them.
- The fix is small. The cost of this feature is in the testing requirement, not the correction.
- Verification on a running Jellyfin 12 server is part of this feature, unlike `003`. The defect
  was invisible to a green suite, so a suite going green again is not sufficient evidence.

## Out of Scope

- Any change to what the API returns to callers other than the pages, or to any server-side
  behaviour, ownership rule, source or stored data.
- Introducing a browser test framework. `002` recorded the absence of one as a deliberate
  constraint; whether that must change is a planning decision, not a given.
- The unrelated defect in `004`, where a caller with no user receives the wrong status.
