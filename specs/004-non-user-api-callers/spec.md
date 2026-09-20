# Feature Specification: Answer a caller that has no user

**Feature Branch**: `004-non-user-api-callers`

**Created**: 2026-09-20

**Status**: Draft

**Input**: Found by the real-server install of `0.1.0` on Jellyfin 12.1, recorded in
[`docs/real-server-install-0.1.0.md`](../../docs/real-server-install-0.1.0.md) finding 1 and
seeded in [`NOTES.md`](./NOTES.md).

## Context

The plugin's release list and artist list are per-user: each one filters to the music libraries the
caller is allowed to see. To do that they read the caller's user identity from the request.

Jellyfin lets a request authenticate in two ways. A person signed into the web client carries a
user identity. A script or another service authenticating with an **API key** carries no user at
all — the key belongs to the server, not to a person.

The plugin only anticipated the first. Asked for the list by a caller with no user, it fails with
an internal error and writes a stack trace to the server log, instead of answering plainly that
the request needs a user.

**No user of the plugin can reach this.** The New Releases page calls these endpoints from the
browser with the signed-in person's session, so there is always a user. This feature is about two
other things: an operator scripting against the plugin, and a test that claims to cover this case
and does not.

## Clarifications

### Session 2026-09-20

- Q: Is this worth fixing at all, given no user can reach it? → A: Yes, but at low priority, and
  for the second reason more than the first. The plugin's own test asserts the right answer for a
  situation that cannot occur, and misses the one that can, so the safety net has a hole in it that
  the next per-user endpoint would inherit.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - An operator scripting against the plugin gets a usable answer (Priority: P1)

An operator automates something around the plugin — a scheduled check, a dashboard, a health
probe — and authenticates with a server API key, as they would for any other Jellyfin endpoint.
They call the release list. Because an API key has no person behind it, the plugin cannot decide
which libraries to show, and says so: the request is refused cleanly and the server log stays
quiet. The operator sees immediately that these endpoints need a signed-in user, and switches to
the endpoints that do not.

**Why this priority**: it is the only externally visible behaviour in this feature. Everything
else is about the tests.

**Independent Test**: call the release list and the artist list with an API key and no user
session, and observe the refusal and the absence of a logged error.

**Acceptance Scenarios**:

1. **Given** a caller authenticated by API key with no user, **When** the release list is
   requested, **Then** the request is refused as unauthenticated and no error is written to the
   server log.
2. **Given** the same caller, **When** the artist list is requested, **Then** it is refused the
   same way.
3. **Given** the same caller, **When** an endpoint that does not depend on a user is requested,
   **Then** it answers normally, as it does today.
4. **Given** a caller signed in as a person, **When** any of these endpoints is requested,
   **Then** the answer is exactly what it is today, filtered to that person's libraries.

---

### User Story 2 - The tests model an absent user the way the host really does (Priority: P1)

A developer adds a new endpoint that filters by the caller's libraries, and copies the existing
tests as the pattern. Those tests describe an absent user the way the host actually represents
one, so the new endpoint is tested against the situation that really happens rather than an
invented one.

**Why this priority**: equal to User Story 1, and the reason this feature exists. The behaviour
above is small; a test suite that disagrees with the host about what "no user" looks like is a
defect that spreads.

**Independent Test**: inspect the test for an absent user and confirm the request it builds
matches what Jellyfin sends for an API-key caller; confirm it fails if the plugin stops handling
that case.

**Acceptance Scenarios**:

1. **Given** the test double used for an unauthenticated caller, **When** it is compared with what
   the host sends for an API-key caller, **Then** they agree.
2. **Given** the plugin's handling of an absent user is removed, **When** the suite runs,
   **Then** it fails.
3. **Given** the suite, **When** it is read for how an unauthenticated caller is built,
   **Then** there is one way to do it, reusable by a future endpoint.

---

### Edge Cases

- **A caller whose user identity is present but names a user that no longer exists.** Distinct
  from having no user at all; the plugin must not present it as the same thing, and must not fail
  with an internal error either.
- **The endpoints that do not read a user.** The administrative status endpoint and the plain
  status endpoint answered normally to an API key during the install pass. They must keep doing
  so; this feature must not make them stricter.
- **The decision endpoints** (ignore, have it, restore) were not exercised by an API key during
  the install pass. Whether they share the fault is unknown and must be established, not assumed.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Every endpoint that filters by the caller's libraries MUST refuse a caller that has
  no user, as an unauthenticated request.
- **FR-002**: Such a refusal MUST NOT write an error or a stack trace to the server log. A request
  that the plugin knows how to answer is not a fault.
- **FR-003**: The plugin MUST treat "the request carries no user identity" and "the request
  carries an empty user identity" as the same situation, because the host uses the second to mean
  the first.
- **FR-004**: Endpoints that do not depend on the caller's identity MUST keep answering an
  API-key caller exactly as they do today.
- **FR-005**: For a caller signed in as a person, every one of these endpoints MUST behave exactly
  as it does today. This feature MUST NOT change what any user sees.
- **FR-006**: The test suite MUST build an unauthenticated caller the way the host really presents
  one, and MUST fail if the plugin stops handling that case.
- **FR-007**: There MUST be a single, reusable way for a test to construct an unauthenticated
  caller, so a future per-user endpoint is tested the same way without re-deciding it.
- **FR-008**: It MUST be established, not assumed, which endpoints read the caller's identity, and
  every one of them MUST be covered by this feature.

### Key Entities

- **Caller identity**: what a request carries about who is asking. Either a person, or nothing —
  and the host expresses "nothing" in a way the plugin must recognise.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A caller with no user receives a clean refusal from every per-user endpoint, and the
  server log records nothing for it.
- **SC-002**: Zero errors appear in the server log when the per-user endpoints are called with an
  API key.
- **SC-003**: 100% of the acceptance scenarios specified for `001-track-new-releases` and
  `002-report-data-age` still pass, unchanged.
- **SC-004**: Removing the plugin's handling of an absent user causes the suite to fail.
- **SC-005**: Every endpoint that reads the caller's identity is listed, and each has a test for
  the absent-user case.

## Assumptions

- Refusing as unauthenticated is the right answer, rather than refusing as forbidden. The request
  is not "this user may not see this"; it is "there is no user to decide for". This matches what
  the plugin's existing test already intended.
- An API key is the only way a real caller arrives with no user. Anonymous access to these
  endpoints is not possible: the host requires authentication before the plugin is reached.
- The fix is small — recognising one more representation of "no user" — and needs no change to how
  libraries are filtered, to the page, or to any stored data.
- Verification on a running Jellyfin server is again outside this feature. The behaviour is
  reproducible against the test suite once the suite models the caller correctly, and the real
  server confirmed the fault once already.

## Out of Scope

- Any change to what a signed-in person sees. This feature changes only the answer given to a
  caller that has no user.
- Adding a way for an API key to act on behalf of a person.
- The administrative endpoints' own authorisation rules, which are unchanged and already correct.
- Removing the error from the server log by suppressing logging, rather than by not failing.
