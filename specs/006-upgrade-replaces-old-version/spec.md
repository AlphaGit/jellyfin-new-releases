# Feature Specification: An upgrade leaves exactly one version of the plugin running

**Feature Branch**: `006-upgrade-replaces-old-version`

**Created**: 2026-10-01

**Status**: Draft

**Input**: Found by the real-server pass of `0.1.1` on Jellyfin 12.1, the pass `005-page-json-casing`
placed inside that feature. Recorded in [`NOTES.md`](./NOTES.md).

## Context

Someone upgrades the plugin from one release to the next. Afterwards, the page they came for shows
nothing but a black rectangle — some days. Other days it works. Nobody touched anything in between.

Both versions are running. The server loads the old release alongside the new one, into the same
host, and the two of them answer the same web address. Asked for that address, the server cannot
choose, so it fails the request outright. Only one address behaves this way, and only because it is
the one address the two releases spell the same; `005` renamed every other one, which is the only
reason the rest of the plugin kept working.

**The intermittency is the serious part.** The server restarts once a day. Over one week it loaded
only the new release on three of those days and both releases on two of them. The page was
therefore working or broken depending on the restart, with no change by the operator and nothing in
the plugin's own behaviour to explain it. A person who checks after an upgrade, sees it working, and
walks away has learned nothing about whether it will work tomorrow.

**The cause is ours, not the host's, and the host already has the guarantee we need.** Read from the
host's own source: when it discovers installed copies it keeps the newest **per name**, compared
case-insensitively, and retires the rest *before* it starts any of them. The permanent identifier is
not what it groups by. So a single-running-version guarantee already exists — and our two copies
slipped through it only because they report different names.

The project states the plugin's name in two places, the packaging manifest and the plugin itself,
and those two places say different things. The host persists a name into each installed copy's
record, taking it from the running plugin, so a copy that has been started ends up filed under a
different name than one that has not. Two copies filed under two names are two plugins as far as
the host is concerned, and it starts both.

**This is therefore preventable outright, not merely survivable.** Making the two statements of the
name agree puts every copy under one name and hands the problem back to a guarantee the host already
enforces.

**The operator's only remedy was to delete files.** The person who hit this had to remove the old
release's directory by hand and restart the server. The project's own constitution forbids exactly
that: no release may require the operator to delete plugin data.

**What a release pipeline can and cannot do.** The ask that prompted this feature was "make the
pipeline remove the previous version on every update". It cannot. The pipeline publishes a package;
the host owns the directory the package is installed into, on a machine the pipeline has never heard
of. The outcome the ask wants — an upgrade never leaves two versions running — is reachable, but by
making the host's own replace-the-old-copy behaviour work correctly, not by anything the publishing
workflow does. This specification is written against the outcome, not against the mechanism.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Someone upgrades and the plugin keeps working (Priority: P1)

An operator updates the plugin from the version they have to the newest one and restarts. Everything
that worked before the upgrade works after it, and keeps working across every subsequent restart.

**Why this priority**: it is what an upgrade means. A plugin that may or may not work after an
update is worse than one that does not update at all, because the failure arrives later and looks
like something else.

**Independent Test**: upgrade a server from the previous release, restart it several times, and
confirm every page and endpoint answers correctly after each restart.

**Acceptance Scenarios**:

1. **Given** a server running the previous release, **When** the operator upgrades and restarts,
   **Then** only the new release is running.
2. **Given** that upgraded server, **When** it restarts repeatedly, **Then** the outcome is the same
   every time. No restart changes which releases are running.
3. **Given** that upgraded server, **When** a person opens the New Releases view, **Then** it
   renders. It does not fail, and it does not fail only on some days.
4. **Given** an upgrade, **When** it completes, **Then** the operator deletes nothing by hand and
   runs no command on the server beyond the restart the host already asks for.

---

### User Story 2 - A stale release cannot take the page down (Priority: P1)

Even where an older release is still present and running — because the host chose to, because an
upgrade half-finished, because an operator deliberately kept it — the thing a person actually opens
still works.

**Why this priority**: equal to the first. The first story prevents the condition; this one removes
its consequence. Without it, the fix depends entirely on behaviour the project does not control, and
the same black page returns the first time that behaviour changes.

**Independent Test**: with two releases deliberately present and running, confirm the user-facing
page still renders.

**Acceptance Scenarios**:

1. **Given** two releases of the plugin running at once, **When** a person opens the New Releases
   view, **Then** it renders rather than failing.
2. **Given** two releases running at once, **When** any address the plugin serves is requested,
   **Then** exactly one of them answers it.

---

### User Story 3 - The disagreement cannot return unnoticed (Priority: P1)

A developer changes what the plugin is called in one of the two places that state it, and forgets
the other. A test fails. Nobody finds out from an operator whose page went black a week after an
upgrade.

**Why this priority**: the defect was invisible to a green suite, to the build, and to the plugin's
own logging, and it survived a deliberate real-server verification pass that was looking for
problems. The condition it needs — two releases, one host — is not reachable from the test suite, so
what the suite can reach has to be pinned precisely.

**Independent Test**: change the plugin's name in one place only and confirm the suite fails.

**Acceptance Scenarios**:

1. **Given** the suite, **When** the name in the packaging manifest and the name in the plugin stop
   agreeing, **Then** at least one test fails.
2. **Given** the suite, **When** a release would serve an address that a previously published
   release also serves, **Then** at least one test fails.

---

### Edge Cases

- **An upgrade across more than one release** — from the oldest published version straight to the
  newest, skipping those in between. The guarantee must hold for every published version, not only
  the immediately preceding one.
- **A downgrade, or a deliberate rollback** to an older release. The host offers this; whatever this
  feature changes must not take it away.
- **The already-broken server.** Someone who upgraded before this fix has two releases on disk right
  now. Installing the fixed release has to resolve that, or the specification has to say plainly
  what the operator must do and why it is a one-time cost.
- **A release that renames the plugin on purpose.** If the displayed name is ever changed
  deliberately, the same condition is created. Whether that is forbidden, or merely has to be done
  in both places at once, must be stated.
- **The address a retired release still answers.** Until a stale copy stops running, it answers the
  addresses it was built with. Whether those are harmless or must be considered live is a decision,
  not an assumption.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: An upgrade from any published release to any later published release MUST leave
  exactly one release of the plugin running.
- **FR-002**: That outcome MUST NOT depend on which restart it is. Repeated restarts of an upgraded
  server MUST produce the same set of running releases every time.
- **FR-003**: An upgrade MUST NOT require the operator to delete files, edit configuration, or run
  any command on the server beyond the restart the host already asks for.
- **FR-004**: The plugin MUST state its name identically everywhere it is stated, so that a release
  the host retires stays recognisable as an older copy of the live plugin.
- **FR-005**: A test MUST fail when the places that state the plugin's name stop agreeing.
- **FR-006**: No release MUST serve an address that a previously published release also serves, so
  that two releases running at once cannot both answer the same request.
- **FR-007**: A test MUST fail when a release would serve an address that a previously published
  release also serves.
- **FR-008**: The user-facing view MUST remain reachable through the host's page integration after
  its address changes, without the operator reconfiguring anything.
- **FR-009**: This feature MUST NOT change what any endpoint returns, what data is stored, who may
  see what, or how sources are used. It changes identity and addresses only.
- **FR-010**: The behaviour of a server that already has two releases installed MUST be stated: what
  installing the fixed release does, and what — if anything — the operator must do once.
- **FR-011**: The project MUST record, where a future author will find it before publishing a
  release, that the plugin's name is fixed and that addresses are never reused between releases.
- **FR-012**: Rollback to an earlier published release MUST remain possible.

### Key Entities

- **Plugin identity**: what the host uses to recognise two installed copies as the same plugin. Not
  the same thing as the permanent identifier, as this defect shows: the identifier was identical in
  both copies and they were still treated as separate.
- **Published release**: a version that has been offered to operators. Its addresses are a matter of
  record from the moment it is published, because a copy of it may be installed somewhere forever.
- **Installed copy**: one release present on one server, which the host may be running or may have
  retired. Retired is not the same as absent, and — as this defect shows — not reliably the same as
  stopped.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An upgrade from every published release to the newest one leaves exactly one release
  running, verified on a real server.
- **SC-002**: An upgraded server restarted five times in a row loads the same single release every
  time.
- **SC-003**: Upgrading requires zero manual steps on the server beyond the restart the host asks
  for.
- **SC-004**: With two releases deliberately running at once, every address the plugin serves is
  answered by exactly one of them, and the user-facing view renders.
- **SC-005**: Changing the plugin's name in one of the places that states it, and not the other,
  fails the suite.
- **SC-006**: No address served by the newest release is served by any previously published release.
- **SC-007**: The New Releases entry still reaches its view after the address change, with no
  operator action.
- **SC-008**: A server that already has two releases installed is working — by the operator's own
  observation — after installing the fixed release and following whatever one-time step `FR-010`
  states.

## Assumptions

- The host groups installed copies by name, case-insensitively, and retires all but the newest
  before starting any. **Confirmed against the host's source**, not inferred. The measurement on the
  real server agrees with it: both copies carried the same permanent identifier, differed only in
  name, and both were started.
- Exactly which path persists which name into an installed copy's record is **not** fully
  established. The host's source shows the running plugin's name being written into that record, and
  shows a package-supplied name being preserved where one is already present; the observed server had
  the retired copy under the plugin's name and the live copy under the packaging's. The feature does
  not depend on resolving this, because making the two statements of the name identical removes the
  difference whichever path runs.
- Changing the address of the user-facing view is safe, because the page registration is rewritten
  every time the server starts. Established by `003`, not re-verified here.
- Nothing outside the plugin calls its addresses. True as of the newest release and asserted by
  `005`.
- A test suite cannot reproduce two releases in one host. The condition needs a second copy of the
  plugin assembly loaded into a running server, which the suite has no way to arrange.

## Out of Scope

- **The host's own behaviour.** Whether loading two copies of one permanent identifier is a defect
  in the host is worth recording, and worth reporting upstream, but it is not fixed here. This
  feature makes the plugin correct under the behaviour the host actually has.
- Any change to what the plugin does: endpoints' contents, stored data, ownership rules, sources,
  the refresh, or either page's behaviour.
- The publishing workflow's steps. It is already correct; it publishes a package, and the defect is
  in what that package declares, not in how it is built or served.
- Removing an installed copy from a server from inside the plugin. Considered and recorded as a
  candidate during specification; whether it is adopted is a decision for the grilling phase, and
  the specification states the outcome rather than that mechanism.
