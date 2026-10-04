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

**The host also cleans up after itself, within one name.** When it finds a newer enabled copy, it
deletes the older one's directory outright at discovery — not merely marks it retired. Two copies
under two names are never compared, so neither is ever deleted. That is the entire gap, and a name
change is the only thing that opens it.

**This is therefore preventable outright, not merely survivable.** Making the two statements of the
name agree puts every copy under one name and hands the problem back to a guarantee the host already
enforces, including its own deletion of what it supersedes.

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
- Q: Does the host really never retire a stale copy — the premise the automatic cleanup rested on? → A: No, and the premise was wrong. Read from the host's source: at discovery it calls `Directory.Delete(path, true)` on an older copy whenever a newer enabled copy **of the same name** exists, and marks it deleted if the delete fails. Its cleanup is complete within one name. The only copies it cannot reach are those under a name it no longer groups, and a name change is the only thing that creates those.
- Q: Given that, does the plugin still remove copies itself? → A: No. Removing it would re-implement deletion the host already performs, and the only thing it uniquely adds is clearing the orphan left by this one rename. That is permanent destructive code for a transition that happens once. The rename release documents a single manual removal instead. The constitution's "no release requires the operator to delete plugin data" is about the finished product, not a pre-release transition, so this is a bounded exception rather than a breach.

### Session 2026-10-04

Opened by test planning, which found the rename breaks the release workflow: JPRM derives the
package slug from the packaging name — `slugify(build_cfg['name'])` in JPRM 1.1.0, with no override
— so the package file, its folder in the published repository and two existing tests all change with
it. The earlier statement that the publishing workflow "is already correct" was wrong for this
rename.

- Q: The two versions already in the catalogue, `0.1.0` and `0.1.1`, sit under the old slug. Keep them, or treat the project as having no published version? → A: Remove them. They were review releases for the real-server passes. The catalogue keeps the plugin's entry, with its permanent identifier, the new name and no versions. The `v0.1.0` and `v0.1.1` tags stay in git as history.
- Q: What is the package file called? → A: `new-releases.zip`, with no version in the file name. Jellyfin does not require one: it checks only that the source location ends in `.zip` and that the bytes match the version's checksum, and names the install directory from the catalogue, not from the file.
- Q: Where does each version's package live, given one file name and many versions? → A: As an asset of that version's GitHub Release. Its public download address is distinct per tag and is never overwritten, so it serves as the historical address of every version, and rollback by reinstall keeps working.
- Q: Where is the catalogue served from? → A: From the repository's raw file address on the default branch, `https://raw.githubusercontent.com/AlphaGit/jellyfin-new-releases/main/repo/manifest.json`. GitHub Pages is no longer used.
- Q: How is the GitHub Release created, and what does it say? → A: By the release workflow, with the `gh` command the runner already carries — no new dependency. Its text is the tagged version's section of `CHANGELOG.md`, the same text the catalogue shows. The release is created before the catalogue entry that points at it is committed.
- Q: Does this belong in this feature or a new one? → A: This feature. The rename is what breaks the workflow, so the two cannot ship apart.
- Q: Does the one-time removal step survive, now the catalogue starts empty? → A: Yes. The repository is public and an unknown operator may have installed `0.1.0` or `0.1.1`. The release notes also tell the operator to replace the repository address, because the old one stops answering.
- Q: Which version is the first release under the new name? → A: `0.2.0`. It must be above every installed copy for the host to offer it as an update, and it also carries `007`'s unreleased changes.

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
- **The already-broken server.** Someone who upgraded before this fix has a copy under the old name.
  The renaming release's notes must name it precisely enough that the operator removes the right
  directory and nothing else.
- **A release that renames the plugin on purpose.** Permitted, but both statements of the name must
  change in the same release — `FR-004` requires them to agree — and the copies left under the old
  name are removed by `FR-007`, which is the same mechanism this feature already needs.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: An upgrade from any published release to any later published release MUST leave
  exactly one release of the plugin running.
- **FR-002**: That outcome MUST NOT depend on which restart it is. Repeated restarts of an upgraded
  server MUST produce the same set of running releases every time.
- **FR-003**: Every upgrade after the release that renames the plugin MUST NOT require the operator
  to delete files, edit configuration, or run any command beyond the restart the host already asks
  for. The renaming release is the single exception, and `FR-007` governs it.
- **FR-004**: The plugin MUST state its displayed name identically everywhere it is stated, so that a
  release the host retires stays recognisable as an older copy of the live plugin. The canonical
  value is the name the plugin itself reports, `New Releases`; the packaging manifest is aligned to
  it. The word "Jellyfin" does not belong in the displayed name, because this plugin is not part of
  the official distribution.
- **FR-004a**: The assembly identity `Jellyfin.Plugin.NewReleases` MUST NOT change. It is the host's
  convention for plugin assemblies rather than a claim of provenance, and the Plugin Pages entry id,
  the migration resource prefix and both pages' embedded resource paths are derived from it.
- **FR-005**: A test MUST fail when the places that state the plugin's displayed name stop agreeing.
- **FR-006**: This feature MUST NOT change what any endpoint returns, what data is stored, who may
  see what, or how sources are used, and MUST NOT change any address the plugin serves. It changes
  the plugin's displayed name, what it removes from disk, and how releases are published, nothing
  else.
- **FR-007**: The release that renames the plugin MUST tell the operator, in its release notes, that
  one directory left under the old name must be removed once, which directory it is, and how to
  recognise it. The same notes MUST give the new repository address and say that it replaces the old
  one. After those single steps the host's own cleanup covers every later upgrade.
- **FR-007a**: No release after the renaming one MUST require any manual step. If a future release
  ever changes the displayed name again it inherits this same one-time cost, which is why `FR-008`
  records the rule.
- **FR-008**: The project MUST record, where a future author will find it before publishing a
  release, that the plugin's displayed name is fixed, that it is stated in more than one place, and
  that those places must never disagree.
- **FR-009**: An operator MUST be able to return to any previously published release by reinstalling
  it from the catalogue. Rollback to a copy still on disk was never available — the host deletes a
  superseded copy at discovery — so nothing here takes it away. The published manifest retains every
  version released from `0.2.0` on, each pointing at a package that is never overwritten, which is
  what makes the guarantee hold. `0.1.0` and `0.1.1` are removed from it and are not covered.
- **FR-010**: Every release's package MUST be named `new-releases.zip` and MUST be published as an
  asset of that version's GitHub Release. The catalogue entry for the version MUST point at that
  asset's public download address.
- **FR-011**: The GitHub Release MUST be created by the release workflow from the version's tag, and
  its text MUST be the tagged version's section of `CHANGELOG.md`.
- **FR-012**: The catalogue MUST be served from
  `https://raw.githubusercontent.com/AlphaGit/jellyfin-new-releases/main/repo/manifest.json`. No
  release MUST depend on GitHub Pages.

### Key Entities

- **Plugin identity**: what the host uses to recognise two installed copies as the same plugin. Not
  the same thing as the permanent identifier, as this defect shows: the identifier was identical in
  both copies and they were still treated as separate.
- **Published release**: a version that has been offered to operators. A copy of it may sit on a
  server forever, so the name it was published under is a fact the project cannot later take back —
  only clean up after.
- **Installed copy**: one release present on one server. The host deletes a copy it supersedes, but
  only when it recognises it as an older copy of the live plugin — which it does by name. A copy
  under a name it no longer groups is never superseded, never deleted, and started on every run.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An upgrade from every published release to the newest one leaves exactly one release
  running, verified on a real server.
- **SC-002**: An upgraded server restarted five times in a row loads the same single release every
  time.
- **SC-003**: Every upgrade after the renaming release requires zero manual steps beyond the restart
  the host asks for.
- **SC-004**: Changing the plugin's name in one of the places that states it, and not the other,
  fails the suite.
- **SC-005**: A server carrying a copy under the old name returns to one copy after the operator
  performs the single documented removal, and never needs another.
- **SC-006**: Every version in the catalogue installs from its own GitHub Release asset,
  `new-releases.zip`, and the suite fails if the release workflow or the catalogue stop producing
  that address.

## Assumptions

- The host groups installed copies by name, case-insensitively, keeps the newest, and **deletes the
  older ones' directories** at discovery, before starting any. **Confirmed against the host's
  source**, not inferred. The measurement on the real server agrees: both copies carried the same
  permanent identifier, differed only in name, and both were started.
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
- Any change to the publishing workflow beyond what `FR-010` to `FR-012` require. The defect is in
  what the package declares; the workflow changes only because the rename moves the package's slug,
  and the decisions of session 2026-10-04 settle where the package and the catalogue now live.
- **Turning GitHub Pages off** in the repository settings. A manual step for the maintainer once
  `0.2.0` is out, not something a release performs.
- **Giving each release its own addresses** so a stale copy cannot collide. Considered and rejected:
  it would bind every future release to renaming routes it has no other reason to change, contradict
  `005`'s convention of naming routes as the host names its own, and break an operator scripting the
  API across an upgrade. The name fix removes the condition instead of surviving it.
