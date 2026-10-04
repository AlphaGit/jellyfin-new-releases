---
feature: 006-upgrade-replaces-old-version
verdict: FAIL
standard: .specify/templates/overrides/tdd-test-quality-rubric.md # project override of the extension rubric (TEST_AFTER_ACCEPTED row)
profile: .specify/memory/tdd-profile.md
verified_at: 2b97421
previous_audit: c210cd1 (FAIL)
behaviors: 35
proven: 14
likely: 4 # A1–A4: red recorded in NOTES.md before the fix, green in docs/real-server-upgrade-0.2.0.md; manual, outside the cycle log
test_after: 0
test_after_accepted: 15
no_test: 0
not_applicable: 2 # U3, U6: tests that predate 006
high_smells: 0
criteria_total: 11
criteria_covered: 11 # 8 of them by the manual real-server pass only
mutation_score: unmeasured # profile records mutation: null; deliberate mutants only
deliberate_mutants: 38 run, 36 caught, 2 survived (F1 inside U26; F2 equivalent for the shipped workflow)
suite: 350 passed, 0 failed (dotnet) + 361 passed, 0 failed (node); 18 s with the build
independent: no # this session wrote the tests; the smell pass came from a fresh-context subagent reading through git, and every cited line was re-read here
---

# TDD Verification: An upgrade leaves exactly one version of the plugin running

**Verdict: FAIL.** The decisive reason: an existing test was loosened, and a mutant survives inside
`DONE` behaviour `U26`. T050 changed U26 to accept any `mv` line that ends in the uploaded file. A
workflow can now move JPRM's package to `staged.zip`, move some other file to `new-releases.zip`,
and stay green (F1). Only a real tag would show the failure. The cycle log entry for T050 says
"No test was loosened". The history and the files contradict it.

The rest holds:

- All second-audit blockers are cleared. S1 and S2 are caught.
- 15 test-after behaviours meet all three acceptance conditions, mutants re-run today.
- Every criterion has a recorded test. A1–A4 come from the maintainer's real-server pass, with a
  red recorded before the fix.
- 36 of 38 deliberate mutants are caught.

## Test-first evidence

History source: 72 commits `857440d..2b97421`, not squashed or amended. `v0.2.0` was released from
`c32c2f6`.

| Behavior | Class | Evidence |
| --- | --- | --- |
| U1, U4, U5, U7, U8, U13, U14, U16, U17, U18, U20, U22, U23, U24 | PROVEN | Red in the cycle log, and the test committed with its subject. Unchanged from the previous audits. U18, U22 and U23 were later restated (T039, T040) with a fresh red for U18 |
| U2, U9–U12, U15, U19, U21, U25, U26, U27, A5, A6 | TEST_AFTER_ACCEPTED | Accepted 2026-10-04 (`c210cd1`). Each mutant caught today |
| U28, U29 | TEST_AFTER_ACCEPTED | Labelled test-after in cycles 28–29. Accepted 2026-10-04 (`c32c2f6`). S1 and S2 caught today |
| A1, A2, A3, A4 | LIKELY | Red: `NOTES.md` (`4e5c269`, 2026-09-30, before the fix `8ecf180`) records both copies loaded, the 500 on `UserView` with `AmbiguousMatchException`, the outcome changing by restart, and the manual delete. Green: `docs/real-server-upgrade-0.2.0.md` (`2b97421`). The red is outside the cycle log, and A2/A3's green rests on the maintainer's unlogged checks |
| U3, U6 | NOT_APPLICABLE | Tests that predate 006 |

### Existing tests changed since `c210cd1`

| Test | Before | After | Judgment |
| --- | --- | --- | --- |
| U26 `RepositoryManifestTests.cs:158-166` | took the destination of the `mv` whose source is JPRM's package, and required it to equal the uploaded file | requires only that **some** `mv` line ends in the uploaded file | **Loosened** (T050). The link from JPRM's output to the upload is now pinned by no test (F1) |
| `Changelog_020_*` (C#) | `Assert.Contains` on a C# copy of `entryFor` | node tests through the real `entryFor`, with backticked `<version>` and more terms | Moved and strengthened (T040) |
| `ReleaseWorkflow_GrantsNoPagesPermission` | two-row theory, one regex | fact over a predicate with a seven-row table | Strengthened (T038) |
| U18, U19 | `entryFor(` on an inline `node -e` line | the script call; argument order tested in node | Restated with a red (T039) |
| `003`'s rejecting rows | seven literals | eight rows from `Slug`, one reason each | Restated (T037, T044, T045) |
| `PublishedVersionsToday` | 0 | 1 | Raised after the `0.2.0` release, per the test's own rule |

### `tasks.md` against the list

Every task is ticked, and every ticked marker names a `DONE` behaviour. No unticked task has a
`DONE` behaviour.

## Findings

Carried findings 6–26 are fixed or waived in writing (cycle log, T037–T051). New findings:

| # | Severity | Finding | Evidence |
| --- | --- | --- | --- |
| 27 | HIGH | **Loosened existing test, with a survivor inside U26 (F1).** U25, U26 and U29 can now each match a different `mv`, so none ties JPRM's output to the uploaded file. The mutant — JPRM's file moved to `staged.zip`, and a second `mv ./artifacts/old.zip ./artifacts/new-releases.zip` — stays at 350 passed. The cycle log's "No test was loosened" (T050) is wrong. Fix: keep the lookup by destination, and also require that the same line's source matches `MovesJprmsPackage` | `RepositoryManifestTests.cs:158-166`; `package.yml:82`; `tdd/cycle-log.md`, T050 entry |
| 28 | MED | **Permissions table: the flow form has no `false` row.** Treating any `{…}` mapping as a grant passes all seven rows (mutant F2, survived). That only makes the predicate stricter, so no granting workflow passes, but the key check is unpinned. A quoted `permissions: "write-all"` is not detected and has no row | `ReleaseWorkflowTests.cs:91-107` |
| 29 | MED | **The 0.2.0 notes tests are not tied to the instruction.** `/\bonce\b/` is met by the heading "Upgrading from 0.1.x — once" alone; the removal sentence has no "once". `/\bnothing else\b/` and `/\breplace\b/i` match anywhere in the section. It should match the instruction sentences themselves | `tests/web/changelog-entry.test.js:102-103,110`; `CHANGELOG.md` 0.2.0 lines 3, 9, 11-12 |
| 30 | MED | **Redundant test.** Any name that fails `Plugin_DisplayName_DoesNotClaimToBeJellyfin` (U2) also fails `Plugin_DisplayName_IsStable`, so U2 can never fail alone | `PluginSanityTests.cs:35-39,63-68` |
| 31 | LOW | **The comment over `RejectedSourceUrls` overstates.** A fork whose slug starts with `v` breaks it: the no-tag row then throws `ArgumentOutOfRangeException`, a false red under `ThrowsAny<XunitException>`. The row data also uses a ternary | `RepositoryManifestTests.cs:325,330-359` |
| 32 | LOW | **`Slug` and `MovesJprmsPackage` read `build.yaml` in the type initializer.** A missing `build.yaml` fails every test in the class (the same class of problem as finding 12) | `RepositoryManifestTests.cs:41-51` |
| 33 | LOW | **The spawn test does not prove what its name says.** It never asserts that no `build.yaml` appeared in the temp folder. It also uses inline `require`s, unlike the exemplar, and `spawnSync` has no timeout | `tests/web/changelog-entry.test.js:73-88` |
| 34 | LOW | **The raw catalogue address is written in both suites** | `tests/web/changelog-entry.test.js:93`; `DocumentationTests.cs:17-18` |
| 35 | LOW | **Stale comments and a self-comparison.** Several comments still say the list is empty until the first tag. The "one root" half compares the single entry with its own root, so it proves nothing until a second version exists | `RepositoryManifestTests.cs:13-14,55-57,216,274,307` |

## Mutation results

No mutation tool. 38 deliberate mutants, one at a time, each restored by a byte copy with a fresh
timestamp, checked with `filecmp` and `git diff`, then followed by both suites. The tree was clean
afterwards.

| Mutants | Behaviors | Survived | Judgment |
| --- | --- | --- | --- |
| U2, A5, U9–U12, U15, U19, A6, U21, U25 (M5), U26 (M14), U27 (M12) | the 13 accepted on `c210cd1` | No | acceptance condition 3 still holds |
| M1–M4, M6–M11 | U1, U5, U8, U14, U17, U18, U20, U22, U23 | No | M10 and M11 are caught by node now |
| S1, S2 | U28, U29 | No | acceptance condition 3 for U28 and U29 |
| `write-all`; flow `pages` in the workflow | U17 | No | T038 |
| arguments swapped; `--notes` ignored | U18 (node) | No | T039 |
| all three names renamed together | `Plugin_DisplayName_IsStable` | No | T041 |
| "nothing else" removed | U22 (node) | No | T042 |
| `ReleaseRootOf` tag check removed; padding once; truncation to four parts; root check removed | T045, T037 rows | No | caught |
| a phantom 0.2.0-shaped version added to the catalogue | U4 | No | caught by the count and the root check |
| **F1** JPRM file → `staged.zip`, second `mv` → `new-releases.zip` | U26 | **Yes** | real: the release would upload a file the build never made (finding 27) |
| **F2** any flow mapping treated as a grant | U17 table | **Yes** | equivalent for the shipped workflow (stricter only); precision gap (finding 28) |

## Traceability

| Criterion | Tests | End to end |
| --- | --- | --- |
| US1-AS1, SC-001 | A1 ← U1, U5 | Yes, manual on the real server |
| US1-AS2, SC-002 | A2 | Yes, manual (the maintainer's restarts) |
| US1-AS3 | A3 | Yes, manual (the maintainer's check) |
| US1-AS4, SC-003 | A4 | Yes, manual. The one manual step was the repository address, which `FR-007` documents |
| US2-AS1, SC-004 | A5 ← U1, U2, U5, `IsStable` | Yes, suite level |
| SC-005 | the host removed the old copy itself (`docs/real-server-upgrade-0.2.0.md`) | Yes, manual |
| SC-006 | A6 ← U7–U19, U24–U29 | Yes at suite level, with the gap of finding 27, and the real `v0.2.0` run |

Untested criteria: none. Tests tracing to nothing: none.

## What was not audited

- Mutation by a tool: none is installed. 38 hand mutants are a sample, not a score.
- Coverage: `coverlet.collector` is not referenced.
- The manual pass: the maintainer's checks of A2 and A3 were taken as reported. Only the deploy
  and start-up facts were observed here.
- A second published version: the "one root" check binds only one entry so far (finding 35).
- Node tests other than `changelog-entry.test.js`: they ran (361 passed) and nothing they cover
  changed.
- Independence: tests and audit come from one session. The smell pass on the delta came from a
  fresh-context subagent, and every cited line was re-read here.
