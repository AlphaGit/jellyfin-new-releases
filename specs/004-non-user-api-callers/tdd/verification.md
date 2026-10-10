---
feature: 004-non-user-api-callers
verdict: PASS_WITH_GAPS
standard: .specify/templates/overrides/tdd-test-quality-rubric.md # project override of the extension rubric (TEST_AFTER_ACCEPTED row)
profile: .specify/memory/tdd-profile.md
verified_at: 7ffe944
previous_audit: 1136b2c (FAIL)
behaviors: 11
proven: 6 # U1, U2, A1, A2, A8; A5 through U1 and U2
likely: 0
test_after: 0
test_after_accepted: 4 # A3, U3, A6, A7, accepted 2026-10-07
no_test: 0
not_applicable: 1 # A4: tests that predate 004
high_smells: 0
criteria_total: 12
criteria_covered: 12
mutation_score: unmeasured # profile records mutation: null; deliberate mutants only
deliberate_mutants: 11 run in this audit, 11 caught, each by the behaviour's own test; plus 1 isolation check
suite: 361 passed, 0 failed (dotnet, 17 s with the build) + 363 passed, 0 failed (node)
independent: no # this session wrote the tests; the smell pass came from a fresh-context subagent, and every cited line was re-read here
---

# TDD Verification: Answer a caller that has no user

**Verdict: PASS_WITH_GAPS.** No high-severity smell remains, every criterion has a test through the
controller or the helper it specifies, and every mutant in this audit is caught by its behaviour's
own test. The gaps: four behaviours are test-after, accepted by the maintainer on 2026-10-07; there
is no mutation tool; and two medium findings remain about how two tests are written.

Both blocking findings of the previous audit are cleared:

- A3 now states the expected status as a literal. M11 (the status refused to every caller) fails
  A3's own test.
- A7 has a test, `TheFactory_HasOneBuilderForEachKindOfCaller`. M12 (a second no-user builder)
  fails it.

## Test-first evidence

| Behavior | Class | Evidence |
| --- | --- | --- |
| U1 | PROVEN | Cycle 1 red; `ae9effa`. Strengthened in cycle 11 against M9 |
| U2 | PROVEN | Cycle 2 red; `cf0b263` |
| A1 | PROVEN | Cycle 3 red (`ArgumentException : Guid can't be empty`); `aeb98cf` |
| A2 | PROVEN | Cycle 4 red; `595533b` |
| A8 | PROVEN | Cycle 5 red for all three decisions; `dd08c8c` |
| A5 | PROVEN | Closed by U1 and U2 |
| A3 | TEST_AFTER_ACCEPTED | Test-after in cycles 6, 10 and 12. Accepted 2026-10-07. M6 and M11 caught by its own test today |
| U3 | TEST_AFTER_ACCEPTED | Test-after in cycle 7. Accepted 2026-10-07. M7 caught |
| A6 | TEST_AFTER_ACCEPTED | Cycle 9 mutant. Accepted 2026-10-07. M2 caught by all five no-user tests |
| A7 | TEST_AFTER_ACCEPTED | Test-after in cycle 13, under the 2026-10-07 acceptance. M12 caught by `TheFactory_HasOneBuilderForEachKindOfCaller`; M4 and M10 pin the claim half |
| A4 | NOT_APPLICABLE | Tests predating 004; `git diff 463373e -- tests/…/Acceptance` is empty |

**History against the log.** Since `1136b2c`:

- Two test-only commits (`341b4d8`, `3c927c7`) match cycles 12 and 13, which claim reds against
  mutants with no production change.
- Three refactor commits (`cb54834`, `1757968`, `500bd5d`) keep the suite at 361.
- `git diff 1136b2c..HEAD -- tests` adds two assertions and changes one call site to its helper.
  No assertion was removed or loosened.

**Existing tests across the feature**, unchanged from the previous audit's reading: the no-user
assertions moved to the host's shape, the claimless case left with FR-003's narrowing, and no test
was skipped, excluded or filtered.

**`tasks.md` against the list:** every task ticked with a behaviour marker names `DONE` behaviours,
and no behavioural task is open. T029 and T035 are ticked with this audit, whose verdict meets
their done condition.

## Findings

The smell pass came from a fresh-context subagent. Each cited line was re-read before it was
entered. No HIGH findings.

| # | Severity | Finding | Evidence |
| --- | --- | --- | --- |
| 1 | MED | **Implementation coupled.** `TheFactory_HasOneBuilderForEachKindOfCaller` pins the factory's method names by reflection. A rename with no change in behaviour fails it. A second way to build a no-user caller outside the factory passes it: a hand-rolled `ClaimsIdentity` in another test file, or `ForUser(Guid.Empty)` called directly. M12 shows it catches a second builder inside the factory. A stronger check of FR-007: no test file outside `Support/` builds a `ClaimsIdentity`, names the `Jellyfin-UserId` claim, or calls `ForUser(Guid.Empty)`, read through `RepositoryFiles`. None does today. | `tests/Jellyfin.Plugin.NewReleases.Tests/Support/ControllerContextFactoryTests.cs:28-36`; `grep` outside `Support/` finds none of the three today |
| 2 | MED | **Magic values.** `new StatusResponse(true, _clock.GetUtcNow(), 24, false)`: `24` is the refresh interval used when no trigger is readable, and the two booleans are positional. Named arguments, or a named constant for the fallback interval, would say why each value is right. | `tests/Jellyfin.Plugin.NewReleases.Tests/Api/ReleasesControllerTests.cs:311` |
| 3 | LOW | **Duplicated setup.** `EveryDecision` repeats the first two lines of `SeedCheckedArtistAsync` (seed Daft Punk, read the artist back). The limited-library controller `Controller(Alice, ControllerContextFactory.User(Alice, allFolders: false, Library))` appears three times; a helper would match `SignedInController()`. | `ReleasesControllerTests.cs:321-322`; `:89`, `:258`, `:342` |

**Raised by the subagent and not entered:**

- "No test asserts FR-002 through a logger." The plugin writes nothing on this path; the logged error
  came from the host middleware, which logs only what escapes the action. "Returns without throwing"
  is the observable, as research R5 and the test list state. A `RecordingLogger` assertion on the
  controller would pass before and after the fix.

**Judged not to be smells:** the status test now pins an independent value, so its two sides cannot
both be wrong together. The decision `switch` picks the call, and its default throws. The refusal
tests are exact (`IsType`), not redundant, and each pins its own call site. The helper tests have
the helper as their subject. Styles, doubles and determinism match the suite.

## Mutation results

No mutation tool (`mutation: null`). Eleven deliberate mutants in the two files the feature changed,
run one at a time, each restored from a copy and verified with `cmp`, the suite re-run green
afterwards (361 passed).

| Mutant | Behavior | Survived | Judgment |
| --- | --- | --- | --- |
| M1 `CallerId()` `!=` → `==` | A4, A1, A8 | No | 25 of 30 controller and factory tests fail |
| M2 `&& id != Guid.Empty` dropped | A1, A2, A8, A6 | No | All five no-user tests fail |
| M3 no-user helper sends a random id | U1 | No | Caught by U1 |
| M4 no-user helper sends no claim | U1, A7 | No | Caught by U1 |
| M5 user manager double stops throwing | U2 | No | Caught by U2 |
| M6 no-user caller given an empty status | A3 | No | Caught by A3 |
| M7 unknown user given an empty list | U3 | No | Caught by U3 |
| M9 plugin claim constant renamed | U1, A5 | No | Caught by U1 |
| M10 `ForUser` builds no claim | A7 | No | 21 signed-in tests fail |
| M11 the status refused to every caller | A3 | No | **Now caught by A3's own test** (previous audit's finding 1) |
| M12 a second no-user builder in the factory | A7 | No | Caught by `TheFactory_HasOneBuilderForEachKindOfCaller` |

**Isolation check (T034):** with the release list throwing for a signed-in caller,
`EveryDecision_ForACallerWithoutAUser_Is401` passes all three cases. `GetReleases_FollowsTheCallersLibraryAccess`
fails. The decision test no longer depends on the list.

M8 was not run: FR-003 no longer covers a request without the claim (spec session 2026-10-07).

## Traceability

| Criterion | Tests | End to end |
| --- | --- | --- |
| US1-AS1 | A1 | Yes, controller |
| US1-AS2 | A2 | Yes, controller |
| US1-AS3 | A3 | Yes, controller |
| US1-AS4, SC-003 | A4 | Yes |
| US2-AS1 | A5 through U1, U2 | Helper level by nature; claim name and value pinned to the host |
| US2-AS2, SC-004 | A6, mutant M2 | Suite property, evidenced by mutant |
| US2-AS3 | A7 | Helper level by nature; finding 1 |
| SC-001, SC-002, SC-005 | A1, A2, A8 | Yes, controller |
| FR-003 | A1, A2, A8 | Yes, controller |

Untested criteria: none. Tests tracing to nothing: none.

## What was not audited

- No mutation tool: strength rests on 11 deliberate mutants, not an exhaustive run.
- No coverage tool (`coverage: null`).
- The real server: out of scope by the spec's Assumptions; `quickstart.md` step 5 is the manual
  check.
- The ASP.NET pipeline: routing and `[Authorize]` are not in the loop; the host behaviour they rely
  on is cited from source (research R1), not exercised.
- `AdminController` and `UserViewController`: they never read the identity (research R2).
- Independence: the session that wrote the tests ran this audit. The smell pass was delegated to a
  fresh-context subagent, and its findings were checked line by line.
