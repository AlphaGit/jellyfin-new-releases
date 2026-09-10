# Feature Specification: Report the age of the data, not the age of the run

**Feature Branch**: `002-report-data-age`

**Created**: 2026-09-08

**Status**: Draft

**Input**: User description: "That's a great insight. Can we modify the specs so that we keep the age of the data and not the age of the run as the datapoint for that screen?"

## Context

Track New Releases (`specs/001-track-new-releases`) already requires the New Releases page to
state how old its data is. `FR-015` there reads: "System MUST show the age of the data behind the
list when it is older than one refresh interval, as `Last refreshed <relative time> ago`."

What was built reports something different: the end time of the last refresh **run** that
completed, whether or not that run reached a source. A refresh completes normally when every
source is cooling down, disabled, or out of daily budget — it walks the artists, finds nothing to
ask, recomputes ownership locally, and records a completed run. The page then says the data was
refreshed moments ago while the releases on screen are hours or days old.

This is the failure `001`'s own `SC-007` exists to prevent ("With no sources reachable, the list
still shows the last stored data and states its age"), and the as-built behaviour is pinned by
its acceptance test `A20`. This feature replaces the run-time datapoint with a data-age
datapoint and fixes the wording that invites the confusion.

## Clarifications

### Session 2026-09-08

- Q: Which instant counts as "the data was confirmed" — the last completed catalogue fetch, the last successful source contact, or the last run that reached a source? → A: The last completed catalogue fetch. The age counts from the most recent moment a catalogue fetch finished completely for any artist at any enabled source, because that is the only one of the three that means the releases on screen were actually checked.
- Q: After a purge, should the page return to the empty state, given that today the empty state is gated on run history rather than on whether data exists? → A: Yes. Both the empty state and the age key off whether stored release data exists, not off run history, so the two stay consistent.
- Q: What should the page do when a clock correction leaves the stored instant in the future, and how absolute should the no-understatement criterion be? → A: Keep it simple. A future-dated instant counts as the present moment; a past-dated one is treated like any other. Because the line only appears once the data is older than one refresh interval, that rule hides it on its own with no special case. `SC-002` drops its absolute "zero cases" wording, which was the source of two contradictions and promised more than any single datapoint can deliver.
- Q: The page-side requirements (`FR-006`, `FR-007`, `FR-010`, `FR-012`) have no automated test, because the stack has no JavaScript runner — leave them to a manual pass, or add a runner? → A: Add the runner, in this feature. It is test-time tooling, not shipped code. Node's built-in test runner needs no dependency; the only shipped change is a small seam so the staleness sentence can be computed without touching the DOM.
- Q: How should the line read when the data is very old, given the built page counts days without bound? → A: Step through units — hours, then days, then weeks, then months, then "over a year ago". Each unit gives way to the next once the count reaches two of the larger unit.
- Q: When an administrator disables the source that has been doing the confirming, the newest remaining completed fetch is older — should the stated age jump backwards? → A: Yes. A disabled source will never confirm anything again, so its past fetches stop counting; the jump is a true signal that the remaining sources are not keeping the data current.
- Q: What wording replaces `Last refreshed <relative time> ago.`? → A: `Releases last checked <relative time> ago.` — "checked" is exactly what the datapoint means (a catalogue fetch completed) and cannot be read as "the job ran"; "Releases" names the subject so the sentence stands alone above the list.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Trust the age shown on the New Releases page (Priority: P1)

A user opens **New Releases** during an outage at one of the release sources. The list still
shows what the plugin knows. The header tells the user how old that knowledge is, so they can
decide whether a missing release is genuinely missing or just not fetched yet. The age never
gets younger unless the plugin actually learned something new.

**Why this priority**: The age is the only thing on the page that tells a user whether to trust
it. An age that resets without new data is worse than no age at all, because it converts "I do
not know" into a confident false statement.

**Independent Test**: Complete one refresh that finishes a catalogue fetch, then make every
source unreachable and refresh again. Open the page and confirm the stated age counts from the
first refresh, not the second.

**Acceptance Scenarios**:

1. **Given** a catalogue fetch completed at 03:00 and a later refresh at 15:00 completed none,
   **When** a user opens New Releases at 15:05, **Then** the page states the releases were last
   checked about 12 hours ago, not 5 minutes ago.
2. **Given** the data is older than one refresh interval, **When** a user opens the page,
   **Then** the age is stated prominently enough to be read before the list.
3. **Given** the data was confirmed within the last refresh interval, **When** a user opens the
   page, **Then** no age warning is shown.
4. **Given** no refresh has ever reached a source, **When** a user opens the page, **Then** the
   page shows the existing "waiting for its first refresh" empty state and states no age.
5. **Given** the administrator purges the stored release data, **When** a user opens the page,
   **Then** the page returns to the empty state and states no age, even though refresh runs are
   still on record.

---

### User Story 2 - A partly successful refresh reports honestly (Priority: P2)

One source is cooling down after repeated failures while the other answers normally. The user
should not be told the data is fully current, nor that it is entirely stale.

**Why this priority**: This is the common real case — sources fail independently, and the plugin
already isolates them. The age must degrade gracefully rather than flipping between "fresh" and
"ancient".

**Independent Test**: Put one source in cooldown, leave the other working, refresh, and confirm
the stated age reflects the completed fetch rather than the cooling-down source.

**Acceptance Scenarios**:

1. **Given** one source is cooling down and the other completes a catalogue fetch during a
   refresh, **When** a user opens the page, **Then** the stated age counts from that completed
   fetch.
2. **Given** the administrator disables every source rather than the sources failing,
   **When** refreshes continue to run, **Then** the stated age keeps growing, because nothing is
   confirming the data.

---

### User Story 3 - The page's own logic is covered by tests (Priority: P3)

The requirements that decide *when* the line appears and *what* it says are implemented in the
page, not on the server. Today nothing tests them: a regression in the unit ladder, or a missed
escape of a release title, reaches a user before it reaches CI. The maintainer wants the page's
decision-making under test so the suite catches those.

**Why this priority**: it delivers no user-visible behaviour, so it ranks below both behaviour
stories. It is in this feature rather than a later one because `FR-006`, `FR-007`, `FR-010` and
`FR-012` are introduced here, and shipping them with no test contradicts the project's
test-first rule.

**Independent Test**: run the page-side test suite on a machine with no network and no Jellyfin
server, and confirm it fails when the unit ladder's boundaries are moved by one unit.

**Acceptance Scenarios**:

1. **Given** the page's staleness logic, **When** the test suite runs, **Then** each band of the
   unit ladder and each of its boundaries is asserted, and moving any boundary makes a test fail.
2. **Given** the page's escaping of text that came from a release source, **When** the test suite
   runs, **Then** a title containing HTML markup is asserted to be escaped.
3. **Given** a machine with no network, **When** the whole suite runs, **Then** the page-side
   tests run without installing anything.

---

### Edge Cases

- A refresh reaches a source successfully but that source has no match for any library artist:
  nothing about the listed releases was confirmed, so the age does not move. Settled by `FR-003`.
- A refresh is cancelled or fails partway after reaching a source: any catalogue fetch that
  completed before the interruption counts, and a fetch left partial does not.
- Every artist's fetch in a run comes back partial because the daily budget ran out mid-catalogue:
  the age does not move, even though the plugin did useful work. Accepted as the cost of `FR-002`,
  and correct in the sense that no artist's catalogue was fully confirmed.
- The administrator disables the source whose fetches were the newest: the stated age immediately
  becomes older, because only enabled sources count and a disabled one will never confirm anything
  again. The jump happens with no change to the releases on screen and is intended.
- Every source is disabled: nothing can confirm the data, so the age keeps growing without bound
  while the list continues to show what is stored.
- A clock correction leaves the stored instant in the future: it counts as the present moment, so
  the line stays hidden. A correction the other way needs no special handling — the data simply
  looks older, which is how any other elapsed time is treated.
- The stored data is older than a year: the line says "over a year ago" rather than counting, so
  the reader never has to do arithmetic on a large number of days.
- Stored release data exists but no catalogue fetch has ever completed — every fetch so far was
  partial or failed: there is data on screen with no confirmation instant behind it, so the page
  must state no age rather than invent one, while still showing the releases.
- A user can see only some libraries, and the artists in those libraries were refreshed on a
  different schedule from the rest: the stated age is server-wide, so it does not narrow to the
  caller's artists. Accepted, and recorded under Assumptions as a known limit.
- The administrator page is an operational view and keeps run semantics: it must still show the
  last run, including a run that reached nothing, so an operator can see the plugin is alive.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The New Releases page MUST state the age of the release data it is showing, not the
  time at which a refresh last ran.
- **FR-002**: The datapoint behind that age MUST be the most recent instant at which a catalogue
  fetch completed fully for any artist at any **currently enabled** source. Fetches by a source
  that is now disabled MUST NOT count, even if they were the most recent. It is one server-wide instant, taken
  as the newest such value across all artists and enabled sources, not a per-artist or per-release
  value shown separately.
- **FR-003**: A refresh that reaches no source MUST NOT change the stated age. Neither MUST a
  refresh that reaches a source but completes no catalogue fetch — a successful call that matched
  no artist, or one that returned only part of a catalogue, confirms nothing about the releases on
  screen.
- **FR-004**: A refresh that completes a catalogue fetch for at least one artist at an enabled
  source MUST update the stated age.
- **FR-005**: The stated age MUST NEVER be younger than the age of the newest completed catalogue
  fetch. Where the system cannot tell which of several instants applies, it MUST report the older
  of the candidates.
- **FR-006**: The page MUST show the age only when the data is older than one refresh interval,
  as `001`'s `FR-015` already requires; within that interval it stays quiet.
- **FR-007**: The staleness line MUST read `Releases last checked <relative time> ago.`,
  replacing `001`'s `Last refreshed <relative time> ago.`. It MUST name the releases as its
  subject and MUST NOT contain any word for the refresh job, so a reader cannot take it for "the
  plugin ran recently".
- **FR-008**: Both the empty state and the stated age MUST depend on whether stored release data
  exists, not on whether a refresh has ever run. With no stored release data the page MUST show
  `001`'s existing empty state and state no age, whether that is because no refresh has completed
  yet or because the data was purged.
- **FR-009**: The administrator view MUST keep reporting refresh runs, including runs that
  reached no source, and MUST additionally report the same age the user page shows, so an operator
  can see the two diverge.
- **FR-012**: The relative time MUST step through units as the data ages — hours, then days, then
  weeks, then months — and MUST read "over a year ago" beyond a year. Each unit gives way to the
  next once the count reaches two of the larger unit, so the page says "36 hours" but "2 days",
  "13 days" but "2 weeks", "7 weeks" but "2 months".
- **FR-011**: Every place that reports this age MUST report the same instant. The New Releases
  page reads it both when it loads the list and when it polls for status, and the two MUST NOT
  disagree.
- **FR-010**: A stated age MUST never be negative or in the future. A stored instant later than
  the current time — which only a server clock correction can cause — counts as the present
  moment. No separate handling is needed: an age of zero is within one refresh interval, so
  `FR-006` hides the line by itself.

- **FR-013**: The logic that decides whether the staleness line appears, and what it says, MUST be
  reachable by a test without a browser or a Jellyfin server. Where that logic currently reads or
  writes the page directly, the part that computes the sentence MUST be separated from the part
  that displays it.
- **FR-014**: The page-side test suite MUST run with no network access and no installation step,
  so it holds to the project's rule that the suite passes on a machine with neither.
- **FR-015**: The project's build gate MUST run the page-side tests alongside the existing ones,
  so a page regression fails the same check.
- **FR-016**: Adding the page-side suite MUST NOT change what the plugin ships: no new file in the
  packaged output, no script the page fetches at runtime, and no build step before packaging.

### Key Entities

- **Data confirmation time**: the instant at which a catalogue fetch last completed fully for an
  artist at an enabled source, meaning that artist's releases were checked end to end and found
  current. Distinct from a refresh run's start or end time, and distinct from a merely successful
  call to a source.
- **Refresh run**: unchanged from `001` — one execution of the scheduled task, with its counts and
  outcome. Remains the administrator view's unit of reporting.

Three timestamps are already recorded today, established by inspecting the built system. No new
kind of information has to be collected; the decision is which of them the page should report:

- **Last successful call to a source** — recorded per source, updated on every successful
  response, including a search that matched nothing. Already shown on the administrator page.
- **Last completed catalogue fetch** — recorded per artist and source, written only when a
  catalogue fetch finishes completely. Not written for a partial or failed fetch.
- **Last attempted refresh of an artist** — recorded per artist, written when every enabled source
  was *attempted*, whether or not any answered.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: After a refresh that reaches no source, the age stated on the page is never smaller
  than it was before that refresh.
- **SC-002**: The stated age is never younger than the moment the releases were last checked, so
  the page cannot claim the data is fresher than it is. Where rotation means some artists were
  checked longer ago than the stated age, that gap is a recorded limit of the datapoint rather
  than a failure of this criterion.
- **SC-003**: A user who opens the page while every source has been unreachable for a day is told
  the data is about a day old.
- **SC-004**: The staleness line names the releases as its subject and contains no word for the
  refresh job — no "refresh", "run", "scan", or "update" — so it cannot be read as a statement
  about the plugin's schedule.
- **SC-006**: With one source cooling down and the other completing catalogue fetches, the stated
  age counts from the completed fetch and is no older than one refresh interval.
- **SC-007**: Moving any boundary of the unit ladder by one unit makes at least one test fail.
- **SC-008**: A release title containing HTML markup appears on the page as text, never as markup,
  and a test asserts it.
- **SC-009**: The whole suite, page-side tests included, passes on a machine with no network and
  nothing installed beyond the language runtimes the project already requires.
- **SC-005**: The administrator page lets an operator see, in one screen, both the last run and
  the data age, and tell that they differ when they do.

## Assumptions

- This feature changes only what the New Releases page and the administrator page report. It does
  not change what is fetched, how often, or how releases are matched.
- `001`'s `FR-015` intent is unchanged and already correct; this feature makes the built behaviour
  match it and sharpens the wording. `001`'s `SC-007` is unchanged in intent. `001`'s acceptance
  test `A20` pins the old behaviour and will need updating, which the plan for this feature will
  cover.
- The administrator view keeps run semantics because its purpose is operational: an operator needs
  to know the job is running even when it achieves nothing.
- "One refresh interval" keeps `001`'s definition, taken from the scheduled task's trigger.
- Relative-time wording stays coarse as in `001`. The built page already states hours below two
  days and days above, and hides the line entirely when the data is within one refresh interval;
  this feature keeps both rules and changes only which instant is measured and how it is worded.
- The canonical wording `Last refreshed <relative time> ago.` is recorded in
  `docs/domain_knowledge/CONTEXT.md`. Replacing it with `Releases last checked <relative time>
  ago.` obliges a matching update there, which is outside what this command may edit and belongs
  in the plan.
- The field carrying this value in `001`'s HTTP contract is named for a refresh. Whether to rename
  it, and how to handle any consumer of the old name, is a contract decision for the plan; this
  spec fixes only what the value means and what the page says.
- No migration is required. The per-artist, per-source completion timestamp shipped in `001`'s
  initial schema and is already being written on every complete fetch, so existing installs
  already hold the value this feature reports.
- The page-side tests cover the logic that can be computed without a page: the staleness
  sentence, year grouping, source-health wording, artist link building, and escaping. Anything
  that reads or writes form fields and rendered rows stays outside them and is still checked by
  hand, because covering it needs a simulated browser this feature does not introduce.
- Keyboard operation and screen-reader announcement (`001`'s `FR-019` and `SC-008`) are **not**
  unblocked by this. They need a real browser and assistive technology, and remain a manual pass.
- **Known limit of the chosen datapoint (FR-002)**, recorded rather than promised away: one
  server-wide instant cannot express rotation lag. On a library large enough that a refresh cycle
  does not reach every artist, the stated age describes the last catalogue fetch that completed
  for *some* artist, not the last time *this* artist was checked, so an individual artist's
  information can be considerably older than the header says. A per-artist minimum was rejected
  because it would read as alarmingly stale on a healthy large library, and the per-artist
  timestamp already exists should that trade ever be revisited.
