---
feature: 004-non-user-api-callers
verdict: FAIL
standard: .specify/templates/overrides/tdd-test-quality-rubric.md # project override of the extension rubric (TEST_AFTER_ACCEPTED row)
profile: .specify/memory/tdd-profile.md
verified_at: 1ba5365
previous_audit: 8cc88b6 (FAIL)
behaviors: 11
proven: 6 # U1, U2, A1, A2, A8; A5 through U1 and U2
likely: 0
test_after: 0
test_after_accepted: 3 # A3, U3, A6, accepted 2026-10-07
no_test: 1 # A7: its "no claimless helper" half is pinned (M4, M10); its "one way" half is not
not_applicable: 1 # A4: tests that predate 004
high_smells: 1
criteria_total: 12
criteria_covered: 11 # US2-AS3 partly: the "one way" half has no test
mutation_score: unmeasured # profile records mutation: null; deliberate mutants only
deliberate_mutants: 10 run in this audit, 10 caught by the suite; 1 (M11) passes A3's own test and is caught only by two other tests
suite: 360 passed, 0 failed (dotnet, 13 s with the build) + 363 passed, 0 failed (node)
independent: no # this session wrote the tests; the smell pass came from a fresh-context subagent, and every cited line was re-read here
---

# TDD Verification: Answer a caller that has no user

**Verdict: FAIL.** The decisive reason: A3's strengthened test takes its expected value from the same
code path as its actual value. `GetStatus_ForACallerWithoutAUser_Answers` compares the no-user
status with the signed-in status. If `GetStatusAsync` refuses every caller, both sides are null and
the test passes (M11, finding 1). Two other tests catch that mutant, but A3's own test does not.

A7 still has no test for its "one way" half. The maintainer accepted the recorded search, but the
rubric admits acceptance only for a test-after behaviour whose test catches a mutant. The previous
audit's T025 offered "amend the test list" as a way to clear A7, and that path cannot clear
`NO_TEST` (finding 2).

Everything the previous audit blocked on is cleared:

- A3's empty-status survivor M6 is now caught.
- The claim name is pinned to the host's literal (M9 caught).
- FR-003 was narrowed to the empty identity Jellyfin sends, with the source cited.
- A3, U3 and A6 meet all three `TEST_AFTER_ACCEPTED` conditions.

## Test-first evidence

| Behavior | Class | Evidence |
| --- | --- | --- |
| U1 | PROVEN | Cycle 1 red; `ae9effa`. Strengthened in cycle 11 (`caa281c`), red against mutant M9 (claim constant renamed), caught again in this audit |
| U2 | PROVEN | Cycle 2 red; `cf0b263`. M5 caught |
| A1 | PROVEN | Cycle 3 red (`ArgumentException : Guid can't be empty`); `aeb98cf` |
| A2 | PROVEN | Cycle 4 red; `595533b` |
| A8 | PROVEN | Cycle 5 red for all three decisions; `dd08c8c`. Since `ab8b024`, a mistyped decision name throws instead of testing Restore |
| A5 | PROVEN | Closed by U1 and U2; the claim name is now checked against the host's literal |
| A3 | TEST_AFTER_ACCEPTED | Labelled test-after in cycles 6 and 10. Accepted 2026-10-07. M6 caught today. Carries finding 1 |
| U3 | TEST_AFTER_ACCEPTED | Labelled test-after in cycle 7. Accepted 2026-10-07. M7 caught today |
| A6 | TEST_AFTER_ACCEPTED | Cycle 9 mutant. Accepted 2026-10-07. M2 caught today: all five no-user tests fail |
| A7 | NO_TEST | "No helper builds a principal without the claim" is pinned: M4 by U1, M10 by every signed-in test. "One way to build a caller with no user" is pinned by nothing; its evidence is the cycle 8 search. Finding 2 |
| A4 | NOT_APPLICABLE | Tests predating 004; `git diff 463373e -- tests/…/Acceptance` is empty |

**History against the log.** The remediation commits since `8cc88b6` (`378fe1d`, `caa281c`,
`ab8b024`, `9da871d`, `818470b`) change test files only. That matches the log's claim that cycles
10 and 11 were red against mutants with no production change. The two refactor commits keep the
suite count at 360.

**Existing tests changed by the feature, re-checked:**

- `GetStatus_ForACallerWithoutAUser_Answers` (`ReleasesControllerTests.cs:297-307`): before,
  `Assert.NotNull(result.Value)`; after, `Assert.Equal(signedIn, result.Value)`. The new check is
  stronger in the normal case. It is weaker in one: when both calls return no value, it passes where
  the old one failed. This is finding 1.
- `ACallerWithoutAUser_CarriesTheEmptyUserIdTheHostSends`: the claim is found by the host's literal
  instead of the plugin's constant. Stronger.
- The old claimless assertions removed by the feature now match FR-003 as narrowed on 2026-10-07.
  The previous audit's M8 is out of scope.
- No test was skipped, excluded, or filtered. No threshold changed.

**`tasks.md` against the list:** every task ticked with a behaviour marker names `DONE` behaviours.
T025 has no marker. It is ticked, but its done condition ("classes none of them `TEST_AFTER` or
`NO_TEST`") is not met for A7 (finding 2).

## Findings

| # | Severity | Finding | Evidence |
| --- | --- | --- | --- |
| 1 | HIGH | **Re-implemented expectation in A3.** The expected value, `signedIn`, comes from the same `GetStatusAsync` as the actual value. If the endpoint returns no value to every caller, both sides are null and the test passes. The test should state the expected status as a literal, `new StatusResponse(true, _clock.GetUtcNow(), 24, false)`, for the seeded release and fetch. The signed-in comparison may stay as a second assertion. | `tests/Jellyfin.Plugin.NewReleases.Tests/Api/ReleasesControllerTests.cs:302,306`; mutant M11 passes this test alone and fails only `:154` and `:172` |
| 2 | HIGH | **A7 has no test for "one way to build a caller with no user", and T025 is ticked against an unmet done condition.** The override's `TEST_AFTER_ACCEPTED` needs a test that catches a mutant; a search is not one. A small test would close it, for example: the factory's only members that build a `ControllerContext` are `ForUser` and `ForCallerWithoutUser`, and each carries exactly one `Jellyfin-UserId` claim. It would be test-after, and the maintainer's acceptance already covers A7. | `tdd/test-list.md` A7 row; `tdd/cycle-log.md` cycle 8 and the 2026-10-07 decision; `tasks.md` T025 |
| 3 | LOW | **Duplicated setup.** The seed-and-fetch block repeats an existing one, and the signed-in controller construction appears 17 times in the file, 2 of them added by 004. One `SignedInController()` and one `SeedCheckedArtistAsync()` would serve all of them. | `ReleasesControllerTests.cs:299-301` repeats `:148-150` and `:164-166`; new construction at `:302`, `:315` |
| 4 | LOW | **Unclear name.** `GetStatus_ForACallerWithoutAUser_Answers` asserts the same status as a signed-in caller gets, which the name does not say. | `ReleasesControllerTests.cs:297` |
| 5 | LOW | **Isolation.** `EveryDecision_ForACallerWithoutAUser_Is401` gets the release id through Alice's `GetReleasesAsync`, so a list regression also fails all three decision cases. Read the id from `_db.Releases`. | `ReleasesControllerTests.cs:315` |

**Judged not to be smells** (raised or checked by the subagent, re-read here):

- `TheUserManager_RejectsAnEmptyId_AsTheHostDoes` is not tautological. Its subject is the helper,
  and M5 shows it catches a change to that helper.
- The claim test is not a re-implemented expectation. The claim type and the 32-zero value are both
  independent literals from the host.
- The `switch` in `EveryDecision` picks the call, not the assertion, and its default arm throws.
  T026 checked this (`ab8b024`): `"Restor"` fails with `ArgumentOutOfRangeException`.
- The per-endpoint refusal tests and `GetReleases_ForAUserDeletedMidRequest_Is401` are not
  redundant. Each catches a defect the others miss (M2 per endpoint, M7).
- The decision test does not assert that the archive is unwritten. No requirement asks for it, and
  the refusal returns before any write.
- Style: sentence-style names match the suite. The only doubles are the host services the profile
  names. The tests are deterministic.

## Mutation results

No mutation tool (`mutation: null`). Ten deliberate mutants, run one at a time in the two files the
feature changed. Each was restored from a copy and verified with `cmp`, and the suite was re-run
green afterwards (360 passed).

| Mutant | Behavior | Survived | Judgment |
| --- | --- | --- | --- |
| M1 `CallerId()` `!=` → `==` | A4, A1, A8 | No | Signed-in and no-user tests fail |
| M2 `&& id != Guid.Empty` dropped | A1, A2, A8, A6 | No | All five no-user tests fail with the host's error |
| M3 no-user helper sends a random id | U1 | No | Caught by U1 |
| M4 no-user helper sends no claim | U1, A7 | No | Caught by U1 |
| M5 user manager double stops throwing | U2 | No | Caught by U2 |
| M6 no-user caller given an empty status | A3 | No | Caught by A3 (previous audit's survivor) |
| M7 unknown user given an empty list | U3 | No | Caught by U3 |
| M9 plugin claim constant renamed | U1, A5 | No | Caught by U1 (previous audit's finding 3) |
| M10 `ForUser` builds no claim | A7 | No | Caught by every signed-in test |
| M11 `GetStatusAsync` refuses every caller | A3 | **Passes A3's own test** | Caught by `:154`, `:172`. Finding 1 |

M8 (`CallerId()` throwing on a missing claim) was not re-run. FR-003 no longer covers that case
(spec session 2026-10-07).

## Traceability

| Criterion | Tests | End to end |
| --- | --- | --- |
| US1-AS1 | A1 | Yes, controller |
| US1-AS2 | A2 | Yes, controller |
| US1-AS3 | A3 | Yes, controller; finding 1 |
| US1-AS4, SC-003 | A4 | Yes |
| US2-AS1 | A5 through U1, U2 | Helper level by nature; claim name and value both pinned to the host |
| US2-AS2, SC-004 | A6, mutant M2 | Suite property, evidenced by mutant |
| US2-AS3 | A7 | **Partly**: the claimless-helper half by U1 and the signed-in tests; the "one way" half by no test |
| SC-001, SC-002, SC-005 | A1, A2, A8 | Yes, controller |
| FR-003 (narrowed) | A1, A2, A8 | Yes, controller |

Untested criteria: US2-AS3's "one way" half. Tests tracing to nothing: none.

## What was not audited

- No mutation tool: strength rests on 10 deliberate mutants, not an exhaustive run.
- No coverage tool (`coverage: null`).
- The real server: out of scope by the spec's Assumptions; `quickstart.md` step 5 is the manual
  check.
- The ASP.NET pipeline: routing and `[Authorize]` are not in the loop; the host behaviour they
  depend on is cited from source (research R1), not exercised.
- `AdminController` and `UserViewController`: they never read the identity (research R2).
- Independence: the session that wrote the tests ran this audit. The smell pass was delegated to a
  fresh-context subagent, and its findings were checked line by line, with M11 run to confirm
  finding 1.
