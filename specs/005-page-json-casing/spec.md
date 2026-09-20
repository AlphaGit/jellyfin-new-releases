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

**The naming is negotiated, not fixed.** Jellyfin offers each response in either of two namings,
selected by what the caller asks for. Confirmed on the review server against the same endpoint:
asking the default way returns one naming, asking explicitly for the other returns the other, with
identical values. The plugin's endpoints ask for neither, so they receive whatever the host
defaults to — and that default is not what the pages read. The plugin's own source records the
dependency as a comment ("Jellyfin serializes camelCase") without ever stating it to the host.

That makes this a decision rather than a repair, and the decision is taken: **the plugin states
the naming for its own responses.** The pages are left as they are. The contract stops being an
inherited default and becomes something the plugin declares, so a later change to the host's
default cannot silently break the pages again.

This feature is about making them agree, about making a test fail the next time they stop
agreeing, and about settling the convention that would have prevented it. The mismatch itself is a
few characters; it reached a real server because the plugin had no stated convention for its own
HTTP surface and because the one piece of code that reads a server response has no test.

**What is already right, and what is not.** The pages build their request paths through the host's
own facility for relative URLs, so nothing hard-codes a server address. But the route prefix is
written out in four places — both controllers, both pages — and once more in the page-registration
payload, where it must be server-absolute. Route casing is inconsistent with itself: the endpoints
under `.../api/...` are lower-case while the page endpoint is `.../UserView`. Nothing states which
is correct, so both are.

**The standard adopted is the host's own.** Route segments are named as Jellyfin names its
first-party routes, and returned fields are named as Jellyfin's camelCase responses name theirs.
The plugin's endpoints then read as part of the server rather than as a visitor on it, and the
project already holds — from `003`'s `FR-016` — that where the host offers a way, the plugin takes
it rather than inventing one.

Concretely, every segment is PascalCase and multi-word segments are concatenated, as in Jellyfin's
own `/ScheduledTasks`, `/QuickConnect/Initiate` and `/Library/VirtualFolders`. The `api` segment
disappears, because no Jellyfin route has one. `Plugins/NewReleases/api/admin/run-now` becomes
`Plugins/NewReleases/Admin/RunNow`, and `/UserView` — already conforming — stops being the odd one
out.

This supersedes the routes `001` and `002` recorded. Those documents fix the endpoints in
lower-case; they are amended to the new names rather than left to describe endpoints that no
longer exist. Renaming is safe: the plugin has never been released and nothing outside it calls
these paths. The field naming is *not* superseded — `001` already specifies `items`, `total`,
`hasStoredReleases` and `releasesLastCheckedAt`, so stating camelCase restores that contract.

## Clarifications

### Session 2026-09-20

- Q: Is this a Jellyfin 12 bug or ours? → A: Ours. The plugin never states how its own responses
  should be named and inherits whatever the host does. A host is entitled to change that; a plugin
  that silently depends on it is the thing at fault.
- Q: Scope — the two fields observed failing, or all of them? → A: All. Twenty-four distinct field
  names are read across the two pages and every one is affected. Fixing the observed two would
  leave the same defect everywhere else.
- Q: How wide should the naming declaration be — every endpoint with a body, only the four the pages read, or all endpoints including empty ones? → A: Every endpoint that returns a body, read or not. One rule, no judgement about which bodies matter, and the check for it stays structural rather than a curated list that ages.
- Q: Should this feature stop at the naming, or settle the plugin's HTTP surface generally? → A: Settle it generally. Adopt one standard for the plugin's HTTP surface — resource naming, casing, and object property names — and hold to it, rather than fixing one symptom. Build paths through the host's own facility for relative URLs instead of writing them out.
- Q: Which standard — follow Jellyfin's own surface, use the wider REST convention, or keep both shapes and record the boundary? → A: Follow Jellyfin's own surface. PascalCase route segments and camelCase returned fields, matching the host's first-party endpoints, so the plugin reads as part of the server rather than a visitor on it.
- Q: `001` and `002` record the routes in lower-case, which that standard contradicts. Keep the recorded routes, supersede them, or leave them as stale history? → A: Supersede them. Rename the routes to the standard and amend `001`'s and `002`'s API contracts to match. Nothing outside the plugin calls these paths — it has never been released and only its own pages call them — so the cost is editing two documents, and the alternative is a convention with a permanent exception.
- Q: Does the `api` segment survive the rename? → A: No. Routes become `Plugins/NewReleases/Releases`, `/Artists`, `/Status`, `/Admin/Status`, `/Admin/RunNow`, `/Releases/{id}/HaveIt` and so on, with `/UserView` unchanged. No Jellyfin route carries an `api` prefix, so keeping one would add a rule the host does not have, and dropping it makes the page endpoint an ordinary sibling rather than an exception.
- Q: How is the list-versus-empty decision tested, given it touches the DOM and the suite has no install step? → A: Extend the existing sandbox with a minimal fake DOM, written in this repository, with no new dependency. Constitution III and the no-install rule stay intact. The fake is partial by design and the specification says so rather than implying full fidelity.
- Q: Which side states the naming contract — the plugin's endpoints, the pages' requests, or the pages accepting either? → A: The plugin's endpoints. Each of the plugin's own JSON responses declares the naming it uses, so the contract is the plugin's statement rather than an inherited host default, and a future change to that default cannot silently break the pages again. The pages are not changed.

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
- **FR-003**: The plugin MUST state, on its own endpoints, the naming its responses use, so that
  the response a page receives does not depend on the host's default. The naming stated MUST be
  the one `001`'s and `002`'s API contracts already record — those documents specify fields such
  as `items`, `total`, `hasStoredReleases` and `releasesLastCheckedAt`, so this restores the
  recorded contract rather than choosing a new one. The pages MUST NOT be
  changed to compensate, and MUST NOT be required to ask for a naming.
- **FR-004**: The empty state MUST remain reachable when there is genuinely nothing to show. A fix
  that makes the list always render would replace one wrong answer with another.
- **FR-005**: Every behaviour `001` and `002` specify for the pages MUST hold on Jellyfin 12,
  including `002`'s staleness wording, which reads an affected field.
- **FR-006**: A test MUST fail if the naming the plugin's endpoints declare stops matching what
  the pages read — whether because the declaration is removed, changed, or was never added to a
  new endpoint.
- **FR-007**: A test MUST fail if a page reads a field the server does not send.
- **FR-008**: The decision between rendering the list and rendering the empty state MUST be
  exercised by a test against a response of the shape the server really produces.
- **FR-009**: This feature MUST NOT change any server-side behaviour beyond the naming of
  responses: no change to what data is returned, to ownership rules, to sources, or to stored data.
- **FR-010**: Every endpoint that returns a body MUST declare the naming, including any added
  later and including bodies no page currently reads. An endpoint that omits it is the defect
  recurring. Endpoints that return no body are out of this rule, having nothing to name.
- **FR-011**: The project MUST record one convention for the plugin's HTTP surface — how routes
  are named and cased, and how returned object fields are named — in a place a future author will
  find before adding an endpoint. That convention MUST be the host's own: route segments named as
  Jellyfin names its first-party routes, returned fields named as Jellyfin's camelCase responses
  name theirs.
- **FR-012**: Every existing route and returned object MUST conform to that convention, or be
  recorded as a deliberate exception with its reason.
- **FR-013**: The route prefix MUST have a single source rather than being written out in each
  controller, each page, and the registration payload. Where a path must be server-absolute, that
  MUST be derived from the same source rather than written again.
- **FR-014**: Request paths MUST continue to be built through the host's facility for relative
  URLs. No page may construct a path that assumes a server address or a deployment sub-path.
- **FR-015**: The API contracts of `001` and `002` MUST be amended to the renamed routes, so no
  document describes an endpoint that does not exist. Records of what was true at an earlier time
  — cycle logs, verification reports, the real-server install notes — MUST NOT be rewritten; they
  are history, not specification.
- **FR-016**: Route segments MUST be PascalCase with multi-word segments concatenated, and MUST
  NOT include an `api` segment, matching the host's own routes.
- **FR-017**: That test MUST run with no network and no installation step, as the rest of the
  suite does. The page test sandbox MUST be extended with a stand-in for the page's surroundings
  written in this repository; no third-party library may be introduced for it.
- **FR-018**: The stand-in MUST be documented as partial. It covers what the tested behaviour
  needs and no more, and what it does not cover MUST be recorded so a later author does not read a
  passing test as proof of something it never exercised.
### Key Entities

- **Response shape**: the names and structure of what a plugin endpoint sends to a browser. Fixed
  by the host's serializer unless the plugin states otherwise, and — as this defect shows — not
  the same thing as the shape of the object a controller returns.
- **HTTP surface convention**: the project's own rules for how the plugin's endpoints are named
  and cased, and how the objects they return name their fields. Recorded once, applied everywhere,
  and checkable.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: With releases stored, the view lists them; with none stored, it shows the empty
  state. Both verified against a real server.
- **SC-002**: Every status value on the administrator page shows a real value after a completed
  refresh; none shows a dash.
- **SC-003**: All 24 field names read by the two pages resolve against a real response.
- **SC-004**: Removing the naming declaration from any of the plugin's JSON endpoints fails the
  suite.
- **SC-005**: 100% of the acceptance scenarios specified for `001` and `002` that concern the
  pages hold on Jellyfin 12.
- **SC-006**: Every route the plugin serves, and every field name in every object it returns,
  conforms to the recorded convention or appears in its list of deliberate exceptions.
- **SC-007**: Changing the route prefix requires editing exactly one place.
- **SC-008**: No specification or contract in the repository names a route the plugin does not
  serve.
- **SC-009**: The whole suite, including the new page test, runs on a machine with no network and
  with no installation step.

## Assumptions

- The pages were written against Jellyfin 10.11, where the host's default produced the naming
  they expect. That the default differed on 10.11 is inferred from the pages having been written
  that way and having worked, not measured against a 10.11 server. What *is* measured is that
  Jellyfin 12 offers both namings and defaults to the one the pages cannot read.
- No stored data is wrong. The 812 releases on the review server were read back correctly through
  the API; only the pages cannot display them.
- The fix is small. The cost of this feature is in the testing requirement, not the correction.
- Verification on a running Jellyfin 12 server is part of this feature, unlike `003`. The defect
  was invisible to a green suite, so a suite going green again is not sufficient evidence.

## Out of Scope

- Any change to what the API returns to callers other than the pages, or to any server-side
  behaviour, ownership rule, source or stored data.
- Introducing a browser test framework or any third-party test dependency. `002` recorded the
  absence of one as a deliberate constraint and it stands: the stand-in is written here.
- Making every page function testable. This feature extends the sandbox only as far as the
  behaviour it must cover; `row`, `refreshStatus`, `read`, `fill` and `query` remain outside it
  unless the work happens to reach them.
- The unrelated defect in `004`, where a caller with no user receives the wrong status.
