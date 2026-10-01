---

description: "Task list for 006-upgrade-replaces-old-version"
---

# Tasks: An upgrade leaves exactly one version of the plugin running

**Input**: Design documents from `/specs/006-upgrade-replaces-old-version/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md),
[quickstart.md](./quickstart.md)

**Tests**: **Mandatory.** Constitution II is non-negotiable. This feature has exactly one testable
behaviour and it is the one that would have prevented the defect, so it is written first and
observed red.

**Organization**: by user story. Both stories are P1. The single test serves both — it is `US1`'s
red and `US2`'s guard — so it is written once, in `US1`'s phase, and marked for both.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: can run in parallel (different files, no dependency on an incomplete task)
- **[Story]**: US1, US2
- Exact file paths in every description

## Scale

One line of packaging, one test assertion, three documents. **No new source file, no new
dependency.** If this grows a class, something has gone wrong — `plan.md` records why the
in-plugin cleanup was specified and then removed.

---

## Phase 1: Setup

- [ ] T001 Confirm the baseline is green before changing anything: `dotnet build --configuration Release` with zero warnings, `dotnet test --configuration Release`, `node --test "tests/web/*.test.js"`. Record the counts; a red baseline means no red produced later can be attributed to this feature

---

## Phase 2: Foundational

**None.** Nothing blocks the user stories. The feature adds no infrastructure, no new folder and no
new type. Recorded explicitly so a reader does not go looking for a missing phase.

---

## Phase 3: User Story 1 — Someone upgrades and the plugin keeps working (Priority: P1) 🎯 MVP

**Goal**: the two statements of the plugin's displayed name agree, so Jellyfin groups every
installed copy under one name and its own cleanup retires the older ones.

**Independent Test**: upgrade a server from the previous release, restart it several times, and
confirm one copy loads every time (`quickstart.md` pass 2).

### Tests for User Story 1 ⚠️

> Write this FIRST and observe it failing, for the right reason — `Jellyfin New Releases` against
> `New Releases`. Record the failure in `specs/006-upgrade-replaces-old-version/tdd/cycle-log.md`.

- [ ] T002 [US1] [US2] Write failing `tests/Jellyfin.Plugin.NewReleases.Tests/PluginSanityTests.cs::Plugin_DisplayName_MatchesTheNameThePackageDeclares` — assert `new Plugin(…).Name` equals `RepositoryFiles.Scalar(buildYaml, "name")`. It goes in `PluginSanityTests` rather than `Packaging/BuildManifestTests.cs` as `plan.md` suggested: constructing `Plugin` sets the static `Plugin.Instance`, so the test must sit in the `ProcessGlobalStateCollection` that `PluginSanityTests` already declares, and the plugin's name is now an identity invariant exactly like its GUID

### Implementation for User Story 1

- [ ] T003 [US1] Change `name` in `build.yaml` from `"Jellyfin New Releases"` to `"New Releases"` until T002 is green. This single line is the fix; everything else in this feature records it or verifies it
- [ ] T004 [US1] Update the `overview`/`description` wording in `build.yaml` only if it reads as the old name in prose. Do not touch `guid`, `targetAbi`, `framework` or `artifacts`

**Checkpoint**: the names agree and the suite proves it.

---

## Phase 4: User Story 2 — The disagreement cannot return unnoticed (Priority: P1)

**Goal**: a future author cannot reintroduce the mismatch without the suite failing, and cannot
rename the plugin without knowing what a rename costs.

**Independent Test**: change the name in one place only and confirm the suite fails
(`quickstart.md` pass 1, scenario 1).

- [ ] T005 [US2] Verify T002 fails for the right reason by mutating `build.yaml`'s `name`, running the suite, and restoring from a file copy verified with `cmp -s`. **Never restore with `git checkout --`** — it reverts the whole file to `HEAD` and takes uncommitted work with it, which has cost work twice on this project
- [ ] T006 [P] [US2] Record the rule in `docs/http-surface.md`, or in a sibling note beside it: the plugin's displayed name is stated in `build.yaml` and `Plugin.cs`, those two must never disagree, Jellyfin groups and deletes installed copies **by name**, and a rename therefore strands every copy under the old name and costs a one-time manual removal (`FR-008`)
- [ ] T007 [P] [US2] Add a one-line pointer to that rule in the Conventions list of `CLAUDE.md`

**Checkpoint**: the mismatch cannot return silently, and the cost of a deliberate rename is written
down where it will be read.

---

## Phase 5: Polish & Release

- [ ] T008 [P] Amend `specs/003-jellyfin-12-compat/contracts/plugin-repository-manifest.md`, which pins `"name": "Jellyfin New Releases"`, to the new value — the rule `005` applied to its route contracts: a contract must not describe something the project no longer produces
- [ ] T009 Add the release entry to `CHANGELOG.md`. It MUST carry the one-time operator step required by `FR-007`: that a directory remains under the old name, that it is `Jellyfin New Releases_<version>` under the server's plugin directory, that it must be removed once, and that no later upgrade needs anything
- [ ] T010 Bump the version in `build.yaml` and `src/Jellyfin.Plugin.NewReleases/Jellyfin.Plugin.NewReleases.csproj` in the same commit as the changelog entry, as the constitution requires
- [ ] T011 Run `quickstart.md` pass 1 in full: build with zero warnings, `dotnet test`, `node --test "tests/web/*.test.js"`, and scenario 1's mutant
- [ ] T012 Push to `main` and verify the CI run green (`gh run watch`). A feature is done when the CI run for that push is verified green, not when it is pushed
- [ ] T013 Tag the release and confirm the package workflow publishes it: the manifest carries the new version under the new `name`, its `sourceUrl` returns 200, and its checksum matches the downloaded bytes
- [ ] T014 `quickstart.md` pass 2 — the real-server pass on a running Jellyfin 12. **JD's own pass.** Steps 4–6 only show the transition survives; **step 7 is the one that matters** — publish a further release, update, and watch the old directory disappear with no manual step, which is the proof the host's cleanup is working now that the names agree. Record the pass in `docs/`

---

## Dependencies & Execution Order

- **Phase 1** first. **Phase 2** is empty.
- **US1 (Phase 3)**: T002 red before T003. T004 after T003, same file.
- **US2 (Phase 4)**: T005 needs T002 and T003 done. T006 and T007 are independent of both and of each other.
- **Polish (Phase 5)**: T008 is independent. T009 and T010 share a commit. T011 → T012 → T013 → T014 in order.

### Parallel Opportunities

- T006, T007 and T008 touch three different files and nothing else depends on them — all parallel.
- Everything else is sequential. The feature is too small for parallelism to buy much.

---

## Implementation Strategy

**MVP is Phase 3**: two tasks, one of them a single line. That alone prevents every future
occurrence, because it restores the host's own grouping. Phases 4 and 5 stop it returning silently
and get it onto a server.

**What "done" means here**: not a green suite. The suite can only prove the two strings match. The
feature's actual claim — an upgrade leaves one copy running — is proved by T014 step 7 and nowhere
else.

---

## Notes

- Commit after each task or logical group. Conventional Commits; no AI co-author trailer.
- Deliberate mutants are restored from a file copy verified with `cmp -s`, never `git checkout --`.
- `dotnet test --filter` requires the trailing `-- RunConfiguration.TreatNoTestsAsError=true`;
  without it a filter matching nothing exits 0. Verified: exit 1 with it, exit 0 without.
- No task here adds a source file. `plan.md`'s Complexity Tracking records the cleanup that was
  specified and removed, and why re-adding it would be a mistake.
