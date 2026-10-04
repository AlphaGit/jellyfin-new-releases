---
feature: 006-upgrade-replaces-old-version
verdict: FAIL
standard: .specify/templates/overrides/tdd-test-quality-rubric.md # project override of the extension rubric (TEST_AFTER_ACCEPTED row)
profile: .specify/memory/tdd-profile.md
verified_at: 74313fc
behaviors: 30
proven: 14
likely: 0
test_after: 10
test_after_accepted: 0
no_test: 4 # A1–A4: manual by the maintainer's decision, T026 not yet run
not_applicable: 2 # U3, U6: tests that predate 006, credited and re-read
high_smells: 1
criteria_total: 11 # 5 acceptance scenarios + 6 success criteria
criteria_covered: 3 # US2-AS1 and SC-004 (A5), SC-006 (A6); the 8 others wait on the real-server pass
mutation_score: unmeasured # profile records mutation: null; deliberate mutants only
deliberate_mutants: 13 run, 10 caught, 3 survived (M5, M12, M14), all inside behaviours marked DONE
suite: 337 passed, 0 failed, 10 s (dotnet) + 358 passed, 0 failed (node)
independent: no # this session wrote the tests; the smell pass came from a fresh-context subagent and every cited line was re-read here
---

# TDD Verification: An upgrade leaves exactly one version of the plugin running

**Verdict: FAIL.** The decisive reason: three deliberate mutants survive inside behaviours marked
`DONE`. A release workflow that moves a file JPRM never writes (M5, M14) and an address rule
without its asset-name check (M12) both leave the suite green. M5 is the coverage `003`'s U39 had
before 006 restated it, so an existing guard was weakened.

Ten behaviours also have no recorded red, and no maintainer acceptance exists for them yet. The
four host-level criteria wait on the manual real-server pass.

What holds: 14 behaviours are `PROVEN`. Each has its red in the cycle log and its test committed
with its subject, and the history agrees with the log in every case. The two identity pins (`U1`,
`U5`), the tag rule (`U8`), the catalogue address (`U14`), the permissions (`U17`), the notes
(`U18`) and both changelog lines caught their mutants.

## Test-first evidence

History source: 29 commits `857440d..74313fc` on `AlphaGit/tdd-run-red-green`. None is squashed or
amended. Every cycle commit carries its test file together with its subject (`git log --name-only`).

| Behavior | Class | Evidence |
| --- | --- | --- |
| U4 | PROVEN | cycle 1 red `lists 2 version(s), expected 0`; `857440d` test + `repo/manifest.json` |
| U5 | PROVEN | cycle 2 red `Expected "New Releases" / Actual "Jellyfin New Releases"`; `d4237a1` |
| U1 | PROVEN | cycle 3 red, same mismatch against `build.yaml`; `8ecf180` |
| U24 | PROVEN | cycle 6 red `…releases/download/` vs `…/v3.0.0/`; `00c9563` (subject is the test helper `ReleaseRootOf`) |
| U7 | PROVEN | cycle 7 red `Assert.EndsWith() Failure`; `66e198c` |
| U8 | PROVEN | cycle 8 red `v1.2.3.0` vs `v1.2.3`; `a12cdc9` |
| U13 | PROVEN | cycle 13 red `Assert.Matches() Failure`; `3388e4f` test + `package.yml` |
| U14 | PROVEN | cycle 14 red `Assert.Matches() Failure`; `49b614b` |
| U16 | PROVEN | cycle 16 red, three `Found: "actions/…"`; `ad530d2` |
| U17 | PROVEN | cycle 17 red `Assert.DoesNotMatch() Failure`, twice; `74c16e3` |
| U18 | PROVEN | cycle 18 red `gh release create is given no --notes-file`; `0c846ad` |
| U20 | PROVEN | cycle 21 red `Sub-string not found`; `eb6512e` test + `README.md` |
| U22 | PROVEN | cycle 23 red `CHANGELOG.md has no section for 0.2.0`; `d2c37f8` |
| U23 | PROVEN | cycle 24 red `Sub-string not found`; `c918032` |
| U2, U9, U10, U11, U12, U15, U19, U21 | TEST_AFTER | Each passed on its first run against code an earlier cycle wrote; the log records a deliberate mutant for each, but no red before the code. No maintainer acceptance is on record |
| A5, A6 | TEST_AFTER | Closed by mutants after their units were green (cycles 5, 20); no red was recorded before `U1`/`U13`–`U19` existed. No acceptance on record |
| U3, U6 | NOT_APPLICABLE | Tests that predate 006, re-read: they assert the literal identity strings and the frozen GUID |
| A1, A2, A3, A4 | NO_TEST | Host-level; the maintainer chose manual tests on their own server (T026), not run yet |

To move the ten `TEST_AFTER` rows to `TEST_AFTER_ACCEPTED`, the rubric override needs three things
for each one:

- the cycle log labels the behaviour test-after with its evidence;
- the cycle log records the maintainer's dated decision to accept it;
- a recorded mutant inside it is caught today.

The third holds for all ten. The first is partly there, as "red: none on the first run". The second
is missing for every one of them.

### Existing tests changed by 006

| Test | Before | After | Judgment |
| --- | --- | --- | --- |
| `003` U39 `RepositoryManifestTests.cs:107-112` | `Assert.Contains($"{Slug}_${{ steps.ver.outputs.version4 }}.zip", workflow)` tied the slug to the file JPRM builds | anchored match on the `gh release create` upload name only | **Weakened.** The tie to JPRM's output name is gone; M5 proves it (finding 1) |
| `003` U27 `ReleaseWorkflowTests.cs:38-45` | `build < add < commit < deploy` | `build < release < add < commit` | Legitimate: `deploy` left with Pages (`FR-012`), and `U16` pins its absence. `build < add` holds by transitivity |
| `003` U23/U25 count `RepositoryManifestTests.cs:32` | `PublishedVersionsToday = 2` | `0` | Rule change decided by the spec (session 2026-10-04). It leaves no real entry for the per-entry rules until `0.2.0` ships (finding 3) |
| `003` U25 helper `:296-303` | `StartsWith(root)` + `EndsWith($"{Slug}_{number}.zip")` | `StartsWith(root+"v")` + `EndsWith("/{Slug}.zip")` + tag equality | Stricter, but its `EndsWith` is unpinned (finding 2) |
| `003` U26 rows `:238-240` | rejected for "another site / another version / no version" | still rejected, but the first prefix check rejects rows 239–240 before their stated reason is reached | Kept, but they no longer test what they say (finding 6) |
| `003` U40 `:128-136` | Pages-shape literals | release-shape literals, assertions unchanged | Legitimate (cycle 7) |
| `003` U41 `:142` | root = directory of the file | root = above the tag directory | Behaviour change with its own red (`U24`) |

### `tasks.md` against the list

- No task is ticked against a behaviour that is not `DONE`.
- T005 `[U1]` is ticked, but its second half (the `package.yml:86` path) did not happen in that
  cycle. Cycle 3 records the move to T013, and T013 is done. LOW, recorded.
- T026 `[A1]–[A4]` is open, correctly.

## Findings

| # | Severity | Finding | Evidence |
| --- | --- | --- | --- |
| 1 | HIGH | **Surviving mutants inside U13; existing guard weakened.** Nothing ties the workflow's `mv` source to the file JPRM writes, or the `mv` target to the uploaded file. A workflow that moves `jellyfin-new-releases_<v4>.zip` (M5) or writes `new-release.zip` (M14) stays green, and fails only on a real tag. It should assert the `mv` source is `{Slug}_${{ steps.ver.outputs.version4 }}.zip` and its target is the file `gh release create` uploads | `.github/workflows/package.yml:82-83`; `RepositoryManifestTests.cs:107-112`; M5, M14 |
| 2 | HIGH | **Surviving mutant inside U11.** Deleting `Assert.EndsWith(asset, …)` leaves all accepting and rejecting rows green. `v1.0.0.0/new-releases_1.0.0.0.zip` is still rejected, through the slice and `Equal`, and no row pairs the right tag with a wrong asset name of the same length. It should add rejecting rows such as `v1.0.0.0/new_releases.zip` and `v1.0.0.0/{another slug}.zip` | `RepositoryManifestTests.cs:302`, rows `:238-243`; M12 |
| 3 | HIGH | **Conditional logic in a changed test (003 code).** `Manifest_EverySourceUrlSharesOneSiteRoot_AndNamesItsOwnVersion` asserts only inside `if (versions.Count > 0)`. 006's U4 set the count to 0, so the branch never runs, and the test now asserts only the count `:94` already pins. The source-address rule binds no real catalogue entry until `0.2.0` ships. It should assert without a branch, so it is non-vacuous the moment an entry appears | `RepositoryManifestTests.cs:220-230`; branch from `b62fbd7` (003) |
| 4 | HIGH (gate) | **Ten behaviours without a recorded red** and with no maintainer acceptance: U2, U9, U10, U11, U12, U15, U19, U21, A5, A6. Each has a mutant caught today, except U11's own guard (finding 2) | `tdd/cycle-log.md` cycles 4, 5, 9–12, 15, 19, 20, 22 |
| 5 | HIGH (gate) | **Eight criteria without an executed test:** US1-AS1–AS4, SC-001, SC-002, SC-003, SC-005. The suite cannot reach them by design; the maintainer runs them by hand in T026, and that has not been run | `tdd/test-list.md` A1–A4; `tasks.md` T026 |
| 6 | MED | `003`'s rejecting rows `jellyfin-new-releases_2.0.0.0.zip` and `jellyfin-new-releases.zip` now fail the first `StartsWith(root+"v")`, so neither tests "another version" or "no version" as its name says | `RepositoryManifestTests.cs:239-240` |
| 7 | MED | **Predicate without a table.** The permissions check `^\s*pages\s*:` misses `permissions: write-all` and the flow form `{ pages: write }`. `tdd-profile.md` requires a `[Theory]` of accepting and rejecting cases for a predicate | `ReleaseWorkflowTests.cs:62-72` |
| 8 | MED | **Shape, not dataflow.** U18 passes as long as the notes path and the version stand on the `entryFor(` line. Swapping `process.argv[1]` and `[2]` would still pass. It should run the notes command, or move it into `changelog-entry.js` and test it with node | `ReleaseWorkflowTests.cs:79-88`; `package.yml:81` |
| 9 | MED | **Re-implemented reader.** `ChangelogSection` copies `entryFor` without its trim and empty-section rule. If the two drift, the test reads text the release does not publish | `DocumentationTests.cs:103-112` |
| 10 | MED | **Missing pin of the canonical value.** `FR-004` fixes `New Releases`, but `U1`/`U5` only compare two derived values. Changing `Plugin.cs`, `build.yaml` and the catalogue together stays green, unlike `Plugin_Guid_IsStable` | `PluginSanityTests.cs:37,66` |
| 11 | MED | **Weaker than the requirement.** U22 asserts only `Jellyfin New Releases_`. `FR-007` also asks for "once" and "how to recognise it", and the test's comment claims "and nothing else", which nothing asserts | `DocumentationTests.cs:114-123` |
| 12 | MED | **Mystery guest / isolation.** `RepositoryManifestTests` reads `package.yml` in a static initializer. A broken workflow file fails the pure predicate tests (U7–U12, U24) with `TypeInitializationException` | `RepositoryManifestTests.cs:21-22` |
| 13 | MED | **Magic values.** The rejecting rows hard-code `new-releases`/`jellyfin-new-releases`, while the class says it is slug-agnostic for forks. After a rename the rows are rejected for the wrong reason | `RepositoryManifestTests.cs:238-243`, `:35-36` |
| 14 | MED | **Predicate copied from the workflow with one side missing.** `InFourParts` has no two-part (`v1.2`) or five-part case, and `ReleaseRootOf` has no rejecting case (an address with no tag directory) | `RepositoryManifestTests.cs:285-290,309-317` |
| 15 | MED | **Coupled to the implementation.** U14 needs the literal `${{ github.repository }}`, and U18 needs a one-line `node -e`. Equivalent rewrites of the workflow turn them red | `RepositoryManifestTests.cs:123`; `ReleaseWorkflowTests.cs:85-87` |
| 16 | LOW | **Foreign style.** The prefixes `Install_`/`Changelog_020_` differ from the file's `Readme_`, and `003`'s U30 is now a subset of U20 | `DocumentationTests.cs:65,79,93,120,131` |
| 17 | LOW | **Duplicated setup.** `new Plugin(Substitute…, Substitute…)` repeated, and U5 parses the manifest again instead of sharing `TheOnlyPlugin` | `PluginSanityTests.cs:33,48,61-64` |
| 18 | LOW | T005 is ticked though its workflow half moved to T013 (see `tasks.md` above) | `tasks.md` T005; cycle 3 |

Vetted from the subagent's report and dropped: "U40 works only because both hosts have the same
length". `StartsWith` rejects any different host, whatever its length.

## Mutation results

No mutation tool is installed (`mutation: null`). There were 13 deliberate mutants, one at a time.
Each was restored by a byte copy (`filecmp`) and confirmed by a clean `git diff` and a green full
suite (337) before the next one. A first run was stopped after M1. Its restore kept the backup's
old timestamp, MSBuild reused the mutated DLL, and the suite stayed red after the restore. The tree
was rebuilt and confirmed green, and all 13 mutants were run again with the fixed restore. Only the
second run is recorded here.

| Mutant | File | Behavior | Survived | Judgment |
| --- | --- | --- | --- | --- |
| M1 `Name` → `"New Release"` | `Plugin.cs:31` | U1, U5 | No | caught by both |
| M2 `name` → `"New releases"` (case only) | `build.yaml:3` | U1 | No | the ordinal comparison is pinned |
| M3 catalogue `name` → old name | `repo/manifest.json` | U5 | No | caught |
| M4 `InFourParts` pads to 3 parts | test helper `:311` | U8 | No | caught |
| M5 `mv` source → `jellyfin-new-releases_<v4>.zip` | `package.yml:82` | U13 | **Yes** | real: a workflow that cannot find its package ships green (finding 1) |
| M6 `--plugin-url` tag → `v<version4>` | `package.yml:88` | U14 | No | caught |
| M7 `pages: write` added back | `package.yml:12` | U17 | No | caught |
| M8 `--notes-file` path differs from the written one | `package.yml:83` | U18 | No | caught |
| M9 README address `main` → `master` | `README.md` | U20 | No | caught |
| M10 old folder name removed from 0.2.0 | `CHANGELOG.md` | U22 | No | caught |
| M11 0.2.0 address `main` → `master` | `CHANGELOG.md` | U23 | No | caught |
| M12 `EndsWith(asset)` deleted | test helper `:302` | U11 | **Yes** | real: no row tests the asset name alone (finding 2) |
| M14 `mv` target → `new-release.zip` | `package.yml:82` | U13 | **Yes** | real: same gap as M5 (finding 1) |

Sampled: U1, U5, U8, U11, U13, U14, U17, U18, U20, U22, U23. Not sampled here: U2, U4, U7, U9,
U10, U12, U15, U16, U19, U21, U24. Each of those has a mutant in the cycle log, from the same
session that wrote it. M13 was defined and skipped as a duplicate of M14.

## Traceability

| Criterion | Tests | End to end |
| --- | --- | --- |
| US1-AS1, SC-001 | A1 — manual, T026 | No; suite proxy `U1` |
| US1-AS2, SC-002 | A2 — manual, T026 | No |
| US1-AS3 | A3 — manual, T026 | No |
| US1-AS4, SC-003 | A4 — manual, T026 | No |
| US2-AS1, SC-004 | A5 ← U1, U2, U5 | Yes, at suite level (mutant cycle 5, M1–M3 here) |
| SC-005 | none in the suite; T026 steps 4–5 | No |
| SC-006 | A6 ← U7–U19, U24 | Yes at suite level, with the gaps of findings 1–2 |

Functional requirements that reach a test:

- `FR-004` (U1, U2, U5);
- `FR-004a` (U3);
- `FR-005` (A5);
- `FR-007` (U22, U23);
- `FR-009`, as the catalogue half (U4, U7, U9);
- `FR-010` (U7–U14, U24);
- `FR-011` (U15, U18, U19);
- `FR-012` (U10, U16, U17, U20, U21).

No test: `FR-001`, `FR-002`, `FR-003` and `FR-007a` (host-level), `FR-006` (guarded only by the
unchanged suite) and `FR-008` (prose).

Tests tracing to nothing: none. Every claimed test exists and ran in the 337.

## What was not audited

- Mutation by a tool: none is installed. Thirteen hand mutants are a sample, not a score.
- Coverage: `coverlet.collector` is not referenced.
- The real release workflow: no tag was pushed. Its shape is tested; its run is T025.
- The host-level criteria (A1–A4, SC-005): manual, the maintainer's own pass, not run.
- The node suite: 006 changed no page or node test; it was run (358 passed), not graded.
- `docs/plugin-name.md`, `CLAUDE.md` and the `003` contract amendment: prose, read for accuracy,
  not tested.
- Independence: the tests and this audit come from one session. The smell pass came from a
  fresh-context subagent and every line it cited was re-read here. The ten `TEST_AFTER` rows and
  the cycle-log mutants were not re-run by anyone independent.
- Performance: no criterion asks for it. The suite takes 10 s.
