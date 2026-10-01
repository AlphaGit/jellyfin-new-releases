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

## Clarifications

### Session 2026-10-01

- Q: Now the mechanism is confirmed, does the "no release may serve an address a previously published release serves" requirement survive? → A: No. Drop it and the test behind it. The name fix removes the condition at its source, and the blanket rule would bind every future release — 0.1.2 shares nine routes with 0.1.1 — forcing either a rename every release or a version-stamped path. That contradicts `005`'s convention and breaks an operator scripting the API across an upgrade, which `004` establishes is a real use. Defence in depth is not worth a permanent convention against a mechanism now understood and controlled.
- Q: Which of the two stated names becomes canonical? → A: The one the plugin itself reports, `New Releases`. The packaging manifest is aligned to it rather than the reverse, because the host writes the running plugin's name into a copy's record once that copy has run, so every path then converges on one string with no window where the two differ. Aligning the other way would leave every already-installed copy filed under the old name and recreate the condition on the very next upgrade.
- Q: The decision adds a principle — "Jellyfin" does not belong in our naming, because this plugin is not part of the official distribution. How far does it reach? → A: The displayed name only. The assembly identity `Jellyfin.Plugin.NewReleases` stays: it is the host's own convention for every plugin assembly, it claims no official status, and it is load-bearing in the Plugin Pages entry id, the migration resource prefix and both pages' embedded resource paths, each of which fails silently on an existing install. The repository title stays too; the repository and its published catalogue URL carry that word regardless.
- Q: Who removes the installed copy that is filed under the old name, given Jellyfin's dedup can never retire it? → A: The plugin, at startup, automatically. An operator instruction was considered and rejected: the constitution forbids a release that requires the operator to delete plugin data, and a copy under a stale name would otherwise load forever beside the live one, sharing its database and registering a second refresh task. Overwriting the old directory in place was also considered and rejected — the plugin does not choose its install location, so an overwrite would leave a directory whose name contradicts its contents, written by code overwriting its own loaded assembly.
- Q: That conflicts with the recorded requirement that rollback stay possible. Which gives way? → A: The requirement is narrowed, not dropped. The published manifest retains every released version and the host's catalogue can reinstall any of them, so what is lost is *instant local* rollback to a copy already on disk, not the ability to return to a published release. The requirement now states the guarantee as rollback by reinstall.
- Q: What evidence is achievable, given the suite cannot load two copies into one host? → A: Three things in the suite — the two declared names agree, the cleanup selects the right directories when driven against a stand-in directory tree including every case it must refuse to delete, and the cleanup is actually invoked at startup — and then a real upgrade on a running server as the closing step. The startup wiring is tested rather than assumed, because correct code that nothing calls is the shape of defect a green suite hides.
- Q: What is the cleanup allowed to delete? → A: Only a directory whose own record carries this plugin's permanent identifier, and never the version that is running. The identifier is the one thing about this plugin that is frozen and already guarded by a test; names and versions change across releases. A directory whose record is missing or unreadable is left alone. Matching on the directory name was rejected: a rename makes it miss, and a similarly named plugin makes it delete another author's files. Requiring the host to have marked the copy retired was also rejected, because a copy under a stale name may never be marked, which is the case being fixed.
- Q: What happens when a delete fails — permissions, a locked file, a half-extracted directory? → A: Log it once and carry on. This matches how the plugin already treats an absent Plugin Pages: optional work that fails never stops the rest. A failed cleanup leaves the status quo, which is a server that works on most restarts, so refusing to start would be worse than the problem.
- Q: Does the plugin record what it deleted? → A: Yes — each removal is logged by name, at Information level. Removing files from an operator's server without a trace is hard to defend, and the log is the only evidence anyone has if something later looks wrong.

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

### User Story 2 - The disagreement cannot return unnoticed (Priority: P1)

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

---

### Edge Cases

- **An upgrade across more than one release** — from the oldest published version straight to the
  newest, skipping those in between. The guarantee must hold for every published version, not only
  the immediately preceding one.
- **A deliberate rollback** to an older release, which now means reinstalling it from the catalogue
  rather than the host switching to a copy still on disk.
- **The already-broken server.** Someone who upgraded before this fix has two copies on disk right
  now, under two different names. Installing the fixed release must return them to one by itself.
- **A directory the cleanup cannot read or cannot delete.** Left alone, reported once, and the plugin
  carries on. A cleanup that cannot finish must never be worse than one that never ran.
- **A directory belonging to another plugin, or to no plugin.** Never touched, whatever it is named.
- **A release that renames the plugin on purpose.** Permitted, but both statements of the name must
  change in the same release — `FR-004` requires them to agree — and the copies left under the old
  name are removed by `FR-007`, which is the same mechanism this feature already needs.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: An upgrade from any published release to any later published release MUST leave
  exactly one release of the plugin running.
- **FR-002**: That outcome MUST NOT depend on which restart it is. Repeated restarts of an upgraded
  server MUST produce the same set of running releases every time.
- **FR-003**: An upgrade MUST NOT require the operator to delete files, edit configuration, or run
  any command on the server beyond the restart the host already asks for.
- **FR-004**: The plugin MUST state its displayed name identically everywhere it is stated, so that a
  release the host retires stays recognisable as an older copy of the live plugin. The canonical
  value is the name the plugin itself reports, `New Releases`; the packaging manifest is aligned to
  it. The word "Jellyfin" does not belong in the displayed name, because this plugin is not part of
  the official distribution.
- **FR-004a**: The assembly identity `Jellyfin.Plugin.NewReleases` MUST NOT change. It is the host's
  convention for plugin assemblies rather than a claim of provenance, and the Plugin Pages entry id,
  the migration resource prefix and both pages' embedded resource paths are derived from it.
- **FR-005**: A test MUST fail when the places that state the plugin's displayed name stop agreeing.
- **FR-005a**: A test MUST cover which directories the cleanup removes and which it refuses to
  remove, driven against a stand-in directory tree rather than a real installation.
- **FR-005b**: A test MUST fail if the cleanup stops being invoked at startup.
- **FR-006**: This feature MUST NOT change what any endpoint returns, what data is stored, who may
  see what, or how sources are used, and MUST NOT change any address the plugin serves. It changes
  the plugin's displayed name and what it removes from disk, nothing else.
- **FR-007**: The plugin MUST remove installed copies of itself that the host has retired, including
  copies filed under a name it no longer uses, so that a server which already carries two copies
  returns to one without operator action. A copy filed under a superseded name is the case the host
  cannot resolve on its own, because its grouping never matches the live copy.
- **FR-007a**: The cleanup MUST only remove a directory whose own record carries this plugin's
  permanent identifier, and MUST NOT remove the running version. A directory whose record is absent,
  unreadable, or carries another identifier MUST be left untouched.
- **FR-007b**: A failed removal MUST NOT stop the plugin from starting or affect anything else it
  does. It MUST be reported once per run, at most.
- **FR-007c**: Each successful removal MUST be recorded, naming what was removed, so an operator can
  see afterwards what the plugin deleted and when.
- **FR-008**: The project MUST record, where a future author will find it before publishing a
  release, that the plugin's displayed name is fixed, that it is stated in more than one place, and
  that those places must never disagree.
- **FR-009**: An operator MUST be able to return to any previously published release by reinstalling
  it from the catalogue. Instant rollback to a copy still on disk is explicitly **not** guaranteed,
  because `FR-007` removes those copies; the published manifest retains every released version, which
  is what makes the guarantee hold.

### Key Entities

- **Plugin identity**: what the host uses to recognise two installed copies as the same plugin. Not
  the same thing as the permanent identifier, as this defect shows: the identifier was identical in
  both copies and they were still treated as separate.
- **Published release**: a version that has been offered to operators. A copy of it may sit on a
  server forever, so the name it was published under is a fact the project cannot later take back —
  only clean up after.
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
- **SC-004**: Changing the plugin's name in one of the places that states it, and not the other,
  fails the suite.
- **SC-005**: A server that already has two copies installed returns to one, with no operator action
  beyond the upgrade itself, and the copy left running is the newest.

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
- **Giving each release its own addresses** so a stale copy cannot collide. Considered and rejected:
  it would bind every future release to renaming routes it has no other reason to change, contradict
  `005`'s convention of naming routes as the host names its own, and break an operator scripting the
  API across an upgrade. The name fix removes the condition instead of surviving it.
