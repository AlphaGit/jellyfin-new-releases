---

description: "Task list for 006-upgrade-replaces-old-version"
---

# Tasks: An upgrade leaves exactly one version of the plugin running

**Input**: Design documents from `/specs/006-upgrade-replaces-old-version/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md),
[quickstart.md](./quickstart.md)

**Tests**: **Mandatory.** Constitution II is non-negotiable. Every behaviour below is written first
and observed red. Three existing packaging rules (`003`'s U25, U27, U39) encode the Pages layout; each is
restated to the new rule and observed red **before** the workflow or the catalogue changes. None is
loosened.

**Behaviour markers**: `[A…]`/`[U…]` are this feature's ids from
[`tdd/test-list.md`](./tdd/test-list.md). `/speckit-tdd-run` ticks a task only when every behaviour
it names is `DONE`. Ids written as "`003`'s U25" are the ids in the packaging tests' doc comments —
a different list.

**Organization**: by user story. Both stories are P1. The name test serves both — it is `US1`'s
red and `US2`'s guard — so it is written once, in `US1`'s phase, and marked for both. The release
hosting (`FR-010`–`FR-012`) sits in `US1`: it is how an upgrade reaches a server at all.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: can run in parallel (different files, no dependency on an incomplete task)
- **[Story]**: US1, US2
- Exact file paths in every description

## Scale

One line of packaging, one catalogue reset, one release workflow reworked, one new test and three
restated, five documents. **No new source file, no new dependency.** The release step uses the `gh`
command the runner already carries. If this grows a class, something has gone wrong — `plan.md`
records why the in-plugin cleanup was specified and then removed.

## Order matters

The rename moves the package slug (`research.md` R7), and a slug change turns U25 red against the
two published catalogue entries (`003`'s U25). So the catalogue is cleared **first** (Phase 2), which makes U25
bind only synthetic entries; then the rename can go green with one extra line in the workflow; then
the release hosting is restated and reworked on a green suite.

---

## Phase 1: Setup

- [X] T001 Confirm the baseline is green before changing anything: `dotnet build --configuration Release` with zero warnings, `dotnet test --configuration Release`, `node --test "tests/web/*.test.js"`. Record the counts; a red baseline means no red produced later can be attributed to this feature

---

## Phase 2: Foundational — clear the review releases from the catalogue (`research.md` R9)

**Blocks every user story**: until `repo/manifest.json` lists no version, the rename in T005 turns
U25 red against `0.1.0.0` and `0.1.1.0`.

- [X] T002 [U4] Change `PublishedVersionsToday` from `2` to `0` in `tests/Jellyfin.Plugin.NewReleases.Tests/Packaging/RepositoryManifestTests.cs`, and its doc comment to say the review releases were cleared on 2026-10-04 and `0.2.0` is the first published version. Observe it red: `repo/manifest.json lists 2 version(s), expected 0`
- [X] T027 [U5] Write failing `tests/Jellyfin.Plugin.NewReleases.Tests/PluginSanityTests.cs::Plugin_DisplayName_MatchesTheNameTheCatalogueLists` — the `name` of `repo/manifest.json`'s single entry, read through `RepositoryFiles`, equals `new Plugin(…).Name`. It sits in `PluginSanityTests` because constructing `Plugin` sets the static `Plugin.Instance`. Observe it red: `Jellyfin New Releases` against `New Releases`
- [X] T003 [U4] [U5] In `repo/manifest.json`, remove both entries from `versions` (leaving `[]`) and set the plugin entry's `name` to `"New Releases"`. Keep `guid` and every other field. The one hand edit this file ever gets; from here on only the release workflow writes it. T002 and T027 go green; `003`'s U21 (this feature's `U6`) still finds exactly one plugin

**Checkpoint**: the catalogue lists the plugin and no version. `003`'s U25 and U23 now bind synthetic entries only.

---

## Phase 3: User Story 1 — Someone upgrades and the plugin keeps working (Priority: P1) 🎯 MVP

**Goal**: the two statements of the plugin's displayed name agree, so Jellyfin groups every
installed copy under one name and its own cleanup retires the older ones; and every release reaches
a server as `new-releases.zip` on its own GitHub Release, listed by a catalogue on the raw file
address.

**Independent Test**: upgrade a server from the previous release, restart it several times, and
confirm one copy loads every time (`quickstart.md` pass 2).

### Tests for User Story 1 — the name ⚠️

> Write this FIRST and observe it failing, for the right reason — `Jellyfin New Releases` against
> `New Releases`. Record the failure in `specs/006-upgrade-replaces-old-version/tdd/cycle-log.md`.

- [X] T004 [U1] [US1] [US2] Write failing `tests/Jellyfin.Plugin.NewReleases.Tests/PluginSanityTests.cs::Plugin_DisplayName_MatchesTheNameThePackageDeclares` — assert `new Plugin(…).Name` equals `RepositoryFiles.Scalar(buildYaml, "name")`. It goes in `PluginSanityTests` rather than `Packaging/BuildManifestTests.cs`: constructing `Plugin` sets the static `Plugin.Instance`, so the test must sit in the `ProcessGlobalStateCollection` that `PluginSanityTests` already declares, and the plugin's name is now an identity invariant exactly like its GUID
- [X] T028 [U2] [US1] Write `PluginSanityTests.cs::Plugin_DisplayName_DoesNotClaimToBeJellyfin` — `Plugin.Name` does not contain `Jellyfin`, ignoring case (`FR-004`). It passes on its first run, so apply the deliberate-mutant check: set `Name` to `Jellyfin New Releases` in `src/Jellyfin.Plugin.NewReleases/Plugin.cs`, observe red, restore from a file copy verified with `cmp -s`

### Implementation for User Story 1 — the name

- [X] T005 [U1] [US1] Change `name` in `build.yaml` from `"Jellyfin New Releases"` to `"New Releases"` until T004 is green. In the same step, change `jellyfin-new-releases_` to `new-releases_` at `.github/workflows/package.yml:86` and in the comment at line 45, because `003`'s U39 derives the slug from `build.yaml` and goes red otherwise. Nothing else in the workflow changes here; T007–T012 rework it
- [X] T006 [US1] Update the `overview`/`description` wording in `build.yaml` only if it reads as the old name in prose. Do not touch `guid`, `targetAbi`, `framework` or `artifacts`

**Checkpoint**: the names agree and the suite proves it. The workflow still publishes to Pages under the new slug.

### Tests for User Story 1 — the release hosting (`FR-010`–`FR-012`) ⚠️

> Each restated rule is written against today's workflow or catalogue rule and observed red before
> T012 changes anything. A restatement keeps every assertion the old rule made that is still true;
> it only moves what the decision moved. Record each red in `tdd/cycle-log.md`.

- [X] T007 [U7] [U8] [U9] [U10] [U11] [U12] [U24] [US1] Restate `003`'s U25, U26, U40 and U41 in `tests/Jellyfin.Plugin.NewReleases.Tests/Packaging/RepositoryManifestTests.cs`: `AssertSourceUrlNamesItsOwnVersion` requires `sourceUrl` to be `<root>releases/download/v<tag>/{Slug}.zip`, where `<root>` is shared by every entry and `<tag>` normalised to four parts equals the entry's `version`. Update `WellFormedEntry`'s default `sourceUrl` to that shape, the doc comments that name the Pages site, `ASourceUrlOffTheSiteOrNamingAnotherVersion_IsRejected`'s cases and `EntriesFromTwoDifferentSites_AreRejected`'s roots. One cycle per behaviour: `U7` and `U8` accepting, `U9`–`U12` rejecting. `U10` — a Pages-style `…/{slug}/{slug}_<version>.zip` address — is red against today's rule, which accepts it
- [X] T008 [U13] [US1] Restate `003`'s U39 `TheDerivedSlug_MatchesTheFilenameTheReleaseWorkflowBuilds` in `RepositoryManifestTests.cs`: the release workflow uploads `{Slug}.zip` with `gh release create`. Red against today's workflow, which uploads nothing
- [X] T009 [U14] [US1] Write failing `RepositoryManifestTests.cs::TheReleaseWorkflow_PointsTheCatalogueAtTheReleaseAsset` — `jprm repo add` is given `--plugin-url` naming `releases/download/${GITHUB_REF_NAME}/{Slug}.zip`, the tag the release was created from (`FR-010`). Red: today's step passes `--url` and no `--plugin-url`
- [X] T010 [U15] [US1] Restate `003`'s U27 in `tests/Jellyfin.Plugin.NewReleases.Tests/Packaging/ReleaseWorkflowTests.cs` as `ReleaseWorkflow_BuildsReleasesAddsToTheManifestAndCommits_InThatOrder`: `jprm plugin build` < `gh release create` < `jprm repo add` < `git commit`. Red: no `gh release create` step
- [X] T011 [U16] [U17] [US1] Write failing `ReleaseWorkflowTests.cs::ReleaseWorkflow_DeploysNoPagesSite` — the steps (comments excluded) contain none of `actions/configure-pages`, `actions/upload-pages-artifact`, `actions/deploy-pages`, and the `permissions` block grants neither `pages` nor `id-token` (`FR-012`). Two behaviours, two cycles: the steps (`U16`) and the permissions (`U17`). Both red against today's workflow
- [X] T012 [U18] [U19] [US1] Write failing `ReleaseWorkflowTests.cs::ReleaseWorkflow_GivesTheReleaseTheTaggedVersionsChangelogSection` — `gh release create` takes `--notes-file` written from `entryFor` in `.github/scripts/changelog-entry.js` for `${{ steps.ver.outputs.version }}` (`FR-011`), and the notes are written before the release is created. Red: no such step

### Implementation for User Story 1 — the release hosting

- [X] T013 [U13] [U14] [U15] [U16] [U17] [U18] [U19] [US1] Rework `.github/workflows/package.yml` until T007–T012 are green, keeping `003`'s U28, U33 and U51 green: after `jprm plugin build`, move `./artifacts/new-releases_${{ steps.ver.outputs.version4 }}.zip` to `./artifacts/new-releases.zip`; write the release notes with `node -e` calling `entryFor` from `.github/scripts/changelog-entry.js`; run `gh release create "$GITHUB_REF_NAME" ./artifacts/new-releases.zip --title "$GITHUB_REF_NAME" --notes-file <notes>` with `GH_TOKEN: ${{ github.token }}`; run `jprm repo add ./repo/manifest.json ./artifacts/new-releases.zip --plugin-url "https://github.com/${{ github.repository }}/releases/download/${GITHUB_REF_NAME}/new-releases.zip"`; keep the commit-and-push step; remove `configure-pages`, `upload-pages-artifact`, `deploy-pages`, the `environment: github-pages` block and the `pages`/`id-token` permissions. Rewrite the header comment and the comments that describe Pages. No new action, no new dependency

**Checkpoint**: a tag would publish `new-releases.zip` on its GitHub Release and list it in a catalogue that no Pages site serves.

---

## Phase 4: User Story 2 — The disagreement cannot return unnoticed (Priority: P1)

**Goal**: a future author cannot reintroduce the mismatch without the suite failing, and cannot
rename the plugin without knowing what a rename costs.

**Independent Test**: change the name in one place only and confirm the suite fails
(`quickstart.md` pass 1, scenario 1).

- [X] T014 [A5] [US2] Verify T004 fails for the right reason by mutating `build.yaml`'s `name`, running `--filter "FullyQualifiedName~PluginSanityTests"`, and restoring from a file copy verified with `cmp -s`. **Never restore with `git checkout --`** — it reverts the whole file to `HEAD` and takes uncommitted work with it, which has cost work twice on this project
- [X] T015 [A6] [US2] Run `quickstart.md` pass 1, scenario 2's two mutants on `.github/workflows/package.yml` (`SC-006`) — rename the uploaded file, then move `gh release create` after `jprm repo add` — each restored from a file copy verified with `cmp -s`. Record both in `tdd/cycle-log.md`
- [X] T016 [P] [US2] Record the rule in `docs/http-surface.md`, or in a sibling note beside it: the plugin's displayed name is stated in `build.yaml` and `Plugin.cs`, those two must never disagree, Jellyfin groups and deletes installed copies **by name**, a rename strands every copy under the old name and costs a one-time manual removal, and JPRM derives the package slug from the name, so a rename also moves the release asset's name (`FR-008`)
- [X] T017 [P] [US2] Add a one-line pointer to that rule in the Conventions list of `CLAUDE.md`

**Checkpoint**: the mismatch cannot return silently, and the cost of a deliberate rename is written
down where it will be read.

---

## Phase 5: Polish & Release

- [X] T018 [P] Amend `specs/003-jellyfin-12-compat/contracts/plugin-repository-manifest.md`, which pins `"name": "Jellyfin New Releases"` and the Pages `sourceUrl` shape, to `New Releases` and the release-asset address — the rule `005` applied to its route contracts: a contract must not describe something the project no longer produces
- [X] T029 [U20] [U21] Write failing `tests/Jellyfin.Plugin.NewReleases.Tests/Packaging/DocumentationTests.cs::Install_NamesTheRawCatalogueAddress` (`U20`) and `::Install_NamesNoPagesAddress` (`U21`), one cycle each, against `README.md`'s `## Install` section through the existing `SectionOf`. Both red against today's README
- [X] T019 [U20] [U21] Replace step 2 of `## Install` in `README.md` with the catalogue address `https://raw.githubusercontent.com/<owner>/<repository>/main/repo/manifest.json`, stating this repository's resolved value; drop the Pages wording, until T029 is green. `003`'s U30 must stay green
- [X] T020 [P] Replace the Pages address at `CHANGELOG.md:43` with the raw catalogue address, as confirmed in the planning session
- [X] T030 [U22] [U23] Write failing `DocumentationTests.cs::Changelog_020_NamesTheOldNameDirectoryToRemove` (`U22`) and `::Changelog_020_NamesTheRepositoryAddressThatReplacesTheOldOne` (`U23`), one cycle each, reading the `## 0.2.0` section by the same heading rule as `entryFor` in `.github/scripts/changelog-entry.js`. Red: `CHANGELOG.md` has no `0.2.0` section
- [X] T021 [U22] [U23] Add the `0.2.0` entry to `CHANGELOG.md`. It MUST carry `FR-007`'s one-time operator steps: that a directory remains under the old name, that it is `Jellyfin New Releases_<version>` under the server's plugin directory, that it must be removed once; that the repository address is now `https://raw.githubusercontent.com/AlphaGit/jellyfin-new-releases/main/repo/manifest.json` and replaces the old one in Dashboard → Plugins → Repositories; and that no later upgrade needs anything, until T030 is green
- [X] T031 Add `007-user-view-polish`'s unreleased changes to the `0.2.0` section of `CHANGELOG.md`, read from `specs/007-user-view-polish/spec.md`, in Keep a Changelog form like the `0.1.1` entry. T030 must stay green
- [X] T022 Bump the version to `0.2.0` in `build.yaml` and `0.2.0.0` in `src/Jellyfin.Plugin.NewReleases/Jellyfin.Plugin.NewReleases.csproj`, in the same commit as T021 and T031, as the constitution requires
- [X] T023 Run `quickstart.md` pass 1 in full: build with zero warnings, `dotnet test`, `node --test "tests/web/*.test.js"`, and scenarios 1 and 2
- [ ] T024 Push to `main` and verify the CI run green (`gh run watch`). A feature is done when the CI run for that push is verified green, not when it is pushed
- [ ] T025 Tag `v0.2.0` and run `quickstart.md` scenario 3: the GitHub Release carries `new-releases.zip`; the raw catalogue lists `0.2.0.0` only, under `name: "New Releases"`; its `sourceUrl` is the release asset, answers 200 after redirects, and its checksum matches the downloaded bytes
- [ ] T026 [A1] [A2] [A3] [A4] `quickstart.md` pass 2 — the real-server pass on a running Jellyfin 12. **JD's own pass.** Replace the repository address first, as the `0.2.0` notes say. Steps 4–6 only show the transition survives; **step 7 is the one that matters** — publish a further release, update, and watch the old directory disappear with no manual step, which is the proof the host's cleanup is working now that the names agree. Then turn GitHub Pages off in the repository settings. Record the pass in `docs/`

---

## Dependencies & Execution Order

- **Phase 1** first.
- **Phase 2** (T002 → T027 → T003) before any user story: the rename turns `003`'s U25 red while the catalogue lists the review releases.
- **US1 (Phase 3)**: T004 red before T005. T028 any time after T004; it touches only the test file and a mutant. T006 after T005, same file. T007–T012 after T005 (they derive `{Slug}` from the renamed `build.yaml`), each observed red before T013. T007–T009 share `RepositoryManifestTests.cs` and T010–T012 share `ReleaseWorkflowTests.cs`, so each group runs in sequence.
- **US2 (Phase 4)**: T014 needs T004 and T005. T015 needs T013. T016 and T017 are independent of both and of each other.
- **Polish (Phase 5)**: T029 red before T019. T030 red before T021; T031 after T021. T018 and T020 are independent. T021, T031 and T022 share a commit. T023 → T024 → T025 → T026 in order.
- **Acceptance**: `A5` closes with T014, `A6` with T015, `A1`–`A4` with T026. A story is not complete until its acceptance behaviours are closed.

### Parallel Opportunities

- T016, T017, T018 and T020 touch four different files and nothing else depends on them — all parallel. T019 waits for T029.
- The two test groups (T007–T009 and T010–T012) are in different files and could run side by side, but the TDD loop takes one behaviour at a time; running them in sequence costs little.

---

## Implementation Strategy

**MVP is Phases 2 and 3 through T006**: the catalogue cleared and the names aligned. That alone
prevents every future occurrence, because it restores the host's own grouping. T007–T013 make the
next release publishable at all under the new slug; Phases 4 and 5 stop the defect returning
silently and get it onto a server.

**What "done" means here**: not a green suite. The suite can only prove the two strings match and
the workflow has the right shape. The feature's actual claim — an upgrade leaves one copy running —
is proved by T026 step 7 and nowhere else.

---

## Notes

- Commit after each task or logical group. Conventional Commits; no AI co-author trailer.
- Deliberate mutants are restored from a file copy verified with `cmp -s`, never `git checkout --`.
- `dotnet test --filter` requires the trailing `-- RunConfiguration.TreatNoTestsAsError=true`;
  without it a filter matching nothing exits 0. Verified: exit 1 with it, exit 0 without.
- No task here adds a source file. `plan.md`'s Complexity Tracking records the cleanup that was
  specified and removed, and why re-adding it would be a mistake.
- A run that fails after `gh release create` but before the commit leaves a release with no
  catalogue entry, and a re-run fails because the release exists. Delete the release by hand and
  re-run (`research.md` R8).

---

## Phase 6: TDD remediation

From [`tdd/verification.md`](./tdd/verification.md), verdict **FAIL** at `74313fc`. **The feature is
not done until T032–T036 are cleared.** T032–T034 are suite changes and go through
`/speckit-tdd-run`: each is written red first, observed failing against the survivor, then made
green. The single-test command is
`dotnet test --configuration Release --filter "FullyQualifiedName~{name}" -- RunConfiguration.TreatNoTestsAsError=true`.

- [ ] T032 Finding 1 (HIGH): pin the release workflow's `mv` in `.github/workflows/package.yml:82` — its source must be `{Slug}_${{ steps.ver.outputs.version4 }}.zip` and its target the file `gh release create` uploads — with a test in `tests/Jellyfin.Plugin.NewReleases.Tests/Packaging/RepositoryManifestTests.cs` beside `TheDerivedSlug_MatchesTheFilenameTheReleaseWorkflowBuilds` (restores `003`'s U39 half). Done when the verification report's mutants M5 (`mv` source `jellyfin-new-releases_<v4>.zip`) and M14 (`mv` target `new-release.zip`) each fail the suite
- [ ] T033 Finding 2 (HIGH): add rejecting rows to `ASourceUrlOffTheSiteOrNamingAnotherVersion_IsRejected` (`RepositoryManifestTests.cs:238-243`) that pair the right tag with a wrong asset name, e.g. `v1.0.0.0/new_releases.zip` and `v1.0.0.0/{another slug}.zip`. Done when mutant M12 (delete `Assert.EndsWith(asset, …)` at `:302`) fails the suite
- [ ] T034 Finding 3 (HIGH): remove the `if (versions.Count > 0)` branch from `Manifest_EverySourceUrlSharesOneSiteRoot_AndNamesItsOwnVersion` (`RepositoryManifestTests.cs:220-230`) so the test has no conditional assertion and binds the first published entry the moment it exists. Done when `grep -n "if (versions.Count" RepositoryManifestTests.cs` is empty, the suite is green, and a synthetic Pages-style entry added to a copy of `repo/manifest.json` fails it
- [ ] T035 Finding 4 (HIGH, the maintainer's decision): for U2, U9, U10, U11, U12, U15, U19, U21, A5 and A6, either record in `tdd/cycle-log.md` the maintainer's dated decision to accept each as test-after (with the rubric override's three conditions met; U11 only after T033), or re-drive the behaviour red-first. Done when `/speckit-tdd-verify` classes none of them `TEST_AFTER`
- [ ] T036 Finding 5 (HIGH, gate): run T026, the manual real-server pass covering A1–A4 and SC-005, and record it in `docs/`. Done when the report's traceability rows for US1-AS1–AS4 and SC-001/002/003/005 cite that record
- [ ] T037 Finding 6 (MED): rebuild `003`'s rows `RepositoryManifestTests.cs:239-240` in the release shape so each differs from a valid address only in its stated reason (another version: `v2.0.0.0/{Slug}.zip` for `1.0.0.0`; no version: `/{Slug}.zip` with no tag directory). Done when each row fails on its own reason with the other checks passing, shown by temporarily deleting the check that should reject it
- [ ] T038 Finding 7 (MED): turn `ReleaseWorkflow_GrantsNoPagesPermission` (`ReleaseWorkflowTests.cs:62-72`) into a predicate with a `[Theory]` table of accepting and rejecting permission blocks, including `permissions: write-all` and the flow form `{ pages: write }`, written before the predicate changes. Done when both new rejecting rows fail against today's regex and pass after
- [ ] T039 Finding 8 (MED): make U18 assert the notes' dataflow, not their shape: move the `node -e` body at `.github/workflows/package.yml:81` into `.github/scripts/changelog-entry.js` as a second entry point with a `tests/web` node test, or run the command in a test against a temporary CHANGELOG. Done when swapping `process.argv[1]` and `[2]` fails a test
- [ ] T040 Finding 9 (MED): read the `0.2.0` section for U22/U23 through the real `entryFor` (node test) instead of the C# copy `ChangelogSection` at `DocumentationTests.cs:103-112`. Done when `ChangelogSection` is gone and both behaviours still fail their recorded mutants M10/M11
- [ ] T041 Finding 10 (MED): add a test that pins the literal `New Releases` as `Plugin.Name`, next to `Plugin_Guid_IsStable` in `PluginSanityTests.cs`. Done when changing `Plugin.cs`, `build.yaml` and the catalogue name together fails the suite
- [ ] T042 Finding 11 (MED): make U22 assert what `FR-007` asks — the folder, that it is removed once, and how it is recognised — or drop the "and nothing else" claim from its comment (`DocumentationTests.cs:114-123`). Done when the test's assertions match its comment
- [ ] T043 Finding 12 (MED): read `package.yml` lazily in `RepositoryManifestTests` instead of in a static initializer (`:21-22`). Done when an unreadable workflow fails only the two workflow tests, not U7–U12 or U24
- [ ] T044 Finding 13 (MED): build the rejecting rows at `RepositoryManifestTests.cs:238-243` from `Slug` rather than hard-coded slugs, using `MemberData` if `InlineData` cannot take it. Done when a temporary rename of `build.yaml`'s `name` leaves each row rejected for its stated reason
- [ ] T045 Finding 14 (MED): add the missing sides of the copied predicates — a two-part tag `v1.2` accepted for `1.2.0.0`, a five-part tag rejected, and an address with no tag directory rejected by `ReleaseRootOf` (`RepositoryManifestTests.cs:285-317`). Done when each new case is observed failing against a mutant of the helper it pins
- [ ] T046 Finding 15 (MED): decide whether U14 and U18 may pin the workflow's exact spelling (`${{ github.repository }}`, a one-line `node -e`) or should accept equivalent forms; record the decision in `tdd/cycle-log.md`, and loosen nothing without it. Done when the decision is recorded
- [ ] T047 Findings 16–18 (LOW): rename `Install_*`/`Changelog_020_*` to the file's `Readme_` style or record why not; share the `Plugin` construction and the manifest read in `PluginSanityTests`; note T005's moved half in its task text only if the maintainer wants the record changed. Done when the suite is green and the report's LOW rows are resolved or waived in writing
