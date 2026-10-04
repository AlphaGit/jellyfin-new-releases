---
feature: 006-upgrade-replaces-old-version
verdict: FAIL
standard: .specify/templates/overrides/tdd-test-quality-rubric.md # project override of the extension rubric (TEST_AFTER_ACCEPTED row)
profile: .specify/memory/tdd-profile.md
verified_at: c210cd1
previous_audit: 74313fc (FAIL)
behaviors: 33
proven: 14
likely: 0
test_after: 0
test_after_accepted: 13
no_test: 4 # A1–A4: manual by the maintainer's decision, T026 not yet run
not_applicable: 2 # U3, U6: tests that predate 006, credited and re-read
high_smells: 0
criteria_total: 11 # 5 acceptance scenarios + 6 success criteria
criteria_covered: 3 # US2-AS1 and SC-004 (A5), SC-006 (A6); the 8 others wait on the real-server pass
mutation_score: unmeasured # profile records mutation: null; deliberate mutants only
deliberate_mutants: 25 run, 23 caught, 2 survived (S1, S2), both inside behaviours marked DONE
suite: 340 passed, 0 failed, 10 s (dotnet); node not re-run, no page or node test changed since 74313fc
independent: no # this session wrote the tests; the smell pass came from a fresh-context subagent reading through git, and every cited line was re-read here
---

# TDD Verification: An upgrade leaves exactly one version of the plugin running

**Verdict: FAIL.** The decisive reason: two deliberate mutants survive inside `U25` and `U26`, the
behaviours the first audit's remediation added. The tests pin how the `mv` line is spelt and what
it moves the file to. They do not pin that the move comes before the upload (S1), or that the move
reads the folder JPRM writes to (S2). Either mutant gives a release that fails only on a real tag.

The criteria gate is also still closed: the four host-level scenarios wait on the maintainer's
manual pass.

What changed since `74313fc`:

- **Finding 1 is cleared.** The workflow-shape mutants M5 and M14 are now caught by `U25` and `U26`.
- **Finding 2 is cleared.** M12 is caught by `U27`.
- **Finding 3 is cleared.** The branch is gone, and with one simulated entry the rule now rejects
  a Pages address and accepts the `0.2.0` address.
- **Finding 4 is cleared.** The 13 test-after behaviours meet all three conditions of
  `TEST_AFTER_ACCEPTED`:
  - each is labelled in the cycle log with its evidence;
  - the maintainer's decision is dated 2026-10-04;
  - each one's recorded mutant was re-run today and caught.

## Test-first evidence

History source: 38 commits `857440d..c210cd1` on `AlphaGit/tdd-run-red-green`. None is squashed or
amended.

| Behavior | Class | Evidence |
| --- | --- | --- |
| U1, U4, U5, U7, U8, U13, U14, U16, U17, U18, U20, U22, U23, U24 | PROVEN | Unchanged from the previous audit. Each red is in the cycle log, and the commit carries the test with its subject |
| U2, U9, U10, U11, U12, U15, U19, U21, A5, A6 | TEST_AFTER_ACCEPTED | Labelled with evidence in cycles 4, 5, 9–12, 15, 19, 20 and 22, and in the acceptance table. Accepted 2026-10-04 (`c210cd1`). Mutants caught today, see below |
| U25, U26, U27 | TEST_AFTER_ACCEPTED | Cycles 25–27 state "test-after" with the reason. Accepted 2026-10-04. M5, M14 and M12 caught today |
| U3, U6 | NOT_APPLICABLE | Tests that predate 006 |
| A1, A2, A3, A4 | NO_TEST | Host-level; manual by the maintainer's decision (T026), not run |

### Existing tests changed since `74313fc`

`Manifest_EverySourceUrlSharesOneSiteRoot_AndNamesItsOwnVersion` lost its `if (versions.Count > 0)`.

- **Before:** the per-entry rule ran only inside the branch.
- **After:** `Assert.All` reads the root inside its lambda.

The rule is the same for every catalogue, and it is now reachable. No assertion was removed or
loosened in `8a73458..c210cd1`; the smell pass confirmed this from the diff.

### `tasks.md` against the list

- No task is ticked against a behaviour that is not `DONE`.
- T034 and T035 meet their own "done when", but they have no behaviour marker, so they are
  unticked and wait for `/speckit-implement`. The auditor does not tick them.

## Findings

Open from the previous audit, unchanged: findings 5 to 18 of `74313fc`. They are carried here
under their task ids T036–T047. New findings come first.

| # | Severity | Finding | Evidence |
| --- | --- | --- | --- |
| 19 | HIGH | **Surviving mutant inside U26 (S1).** With `gh release create` moved above `mv`, the suite stays at 340 passed. Nothing orders the move before the upload, although U26's own comment names this failure. It should also assert that the `mv` index is above the `jprm plugin build` index and below the `gh release create` index, as `ReleaseWorkflowTests.IndexOf` does for the other steps | `package.yml:82-83`; `RepositoryManifestTests.cs:134-147` |
| 20 | HIGH | **Surviving mutant inside U25 (S2).** With `jprm plugin build … --output ./out`, the suite stays green, and the `mv` then reads a folder JPRM never wrote. U25 pins the literal `./artifacts`, not the folder JPRM writes to. It should assert that the `mv` source folder equals the `--output` argument of `jprm plugin build` | `package.yml:63,82`; `RepositoryManifestTests.cs:47-48,126-131` |
| 5 | HIGH (gate) | **Eight criteria without an executed test:** US1-AS1–AS4, SC-001, SC-002, SC-003, SC-005. Manual, the maintainer's own pass (T026, T036) | `tdd/test-list.md` A1–A4 |
| 21 | MED | **Redundant test.** U26 matches the same `MovesJprmsPackage` pattern that U25 asserts, so one bug fails both. It should find the `mv` by its destination | `RepositoryManifestTests.cs:130,141` |
| 22 | MED | **Spelling, not meaning.** `MovesJprmsPackage` needs the literal `${{ steps.ver.outputs.version4 }}` with inner spaces, plain `mv`, and `./artifacts`. Equivalent workflow lines (`${{steps.ver.outputs.version4}}`, `mv -f`, `cp`) fail it, and the padding loop (`-lt 3`) is not tied to it | `RepositoryManifestTests.cs:47-48`; `package.yml:48` |
| 23 | LOW | The upload regex takes the token after the tag as the file. A flag before the file, a `\` continuation, or `artifacts/x.zip` against `./artifacts/x.zip` would give a false red | `RepositoryManifestTests.cs:142` |
| 24 | LOW | The two `"?` in `MovesJprmsPackage` are independent, so an unterminated quote — a shell error — still matches | `RepositoryManifestTests.cs:48` |
| 25 | LOW | The comment "binds the first published version the moment it appears" overstates. The count check fails first, and the rule binds only once `PublishedVersionsToday` is raised. A bad `versions[0]` is also now reported against every entry | `RepositoryManifestTests.cs:259-263` |
| 26 | LOW | Magic value: the row `new_releases` is literal while the class derives `Slug` for forks. It could be derived, e.g. `Slug.Replace('-', '_')`, with a guard that the two differ | `RepositoryManifestTests.cs:277` |

## Mutation results

No mutation tool (`mutation: null`). There were 25 deliberate mutants, one at a time. Each was
restored by a byte copy with a fresh timestamp (so MSBuild recompiles), confirmed with `filecmp`
and a clean `git diff`, and followed by a full suite run (340) before the next one. After the run,
the tree was clean and had no backup left.

| Mutant | File | Behavior | Survived | Judgment |
| --- | --- | --- | --- | --- |
| `Name` = `"JELLYFIN New Releases"` | `Plugin.cs` | U2 | No | condition 3 for U2 |
| `name` = `"New Releases Tracker"` | `build.yaml` | A5 | No | condition 3 for A5 |
| tag-to-version line deleted | test helper | U9 | No | condition 3 for U9 |
| Pages-era rule restored | test helper | U10 | No | condition 3 for U10 |
| asset `"/{Slug}_{number}.zip"` | test helper | U11 | No | condition 3 for U11 |
| root from the entry itself | test helper | U12 | No | condition 3 for U12 |
| release step after `repo add` | `package.yml` | U15 | No | condition 3 for U15 |
| notes line below `gh release create` | `package.yml` | U19 | No | condition 3 for U19 |
| uploaded file `plugin.zip` | `package.yml` | A6 | No | condition 3 for A6 |
| Pages address added to Install | `README.md` | U21 | No | condition 3 for U21 |
| M5 `mv` source `jellyfin-new-releases_…` | `package.yml:82` | U25 | No | cleared finding 1 |
| M14 `mv` target `new-release.zip` | `package.yml:82` | U26 | No | cleared finding 1 |
| M12 `EndsWith(asset)` deleted | test helper | U27 | No | cleared finding 2 |
| M1–M4, M6–M11 (as in `74313fc`) | various | U1, U5, U8, U14, U17, U18, U20, U22, U23 | No | all still caught |
| **S1** `gh release create` above `mv` | `package.yml:82-83` | U26 | **Yes** | real: the upload runs before the file exists (finding 19) |
| **S2** `--output ./out` | `package.yml:63` | U25 | **Yes** | real: `mv` reads a folder JPRM never wrote (finding 20) |

S1 and S2 were proposed by the fresh-context smell pass and confirmed here. The pass also proposed
`-lt 3` → `-lt 2` in the padding loop. That mutant was not run here; it is recorded under
finding 22.

## Traceability

| Criterion | Tests | End to end |
| --- | --- | --- |
| US1-AS1, SC-001 | A1 — manual, T026 | No; suite proxy `U1` |
| US1-AS2, SC-002 | A2 — manual, T026 | No |
| US1-AS3 | A3 — manual, T026 | No |
| US1-AS4, SC-003 | A4 — manual, T026 | No |
| US2-AS1, SC-004 | A5 ← U1, U2, U5 | Yes, at suite level |
| SC-005 | none in the suite; T026 steps 4–5 | No |
| SC-006 | A6 ← U7–U19, U24–U27 | Yes at suite level, with the gaps of findings 19–20 |

Tests tracing to nothing: none. Every claimed test exists and ran in the 340.

## What was not audited

- Mutation by a tool: none is installed. The 25 hand mutants are a sample, not a score.
- The padding-loop mutant (`-lt 2`) and the equivalent-spelling cases of finding 22: not run.
- Coverage: `coverlet.collector` is not referenced.
- The real release workflow: no tag pushed (T025).
- The host-level criteria (A1–A4, SC-005): manual, not run.
- The node suite: not re-run; nothing it covers changed since `74313fc`, when it passed 358.
- MED and LOW findings 6–18 from `74313fc`: carried forward unchanged, not re-examined.
- Independence: tests and audit come from one session. The smell pass on the delta came from a
  fresh-context subagent and every line it cited was re-read here.
