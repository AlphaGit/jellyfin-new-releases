---
feature: 004-non-user-api-callers
verdict: FAIL
standard: .specify/templates/overrides/tdd-test-quality-rubric.md # project override of the extension rubric (TEST_AFTER_ACCEPTED row)
profile: .specify/memory/tdd-profile.md
verified_at: 0edb1d5
behaviors: 11
proven: 6 # U1, U2, A1, A2, A8; A5 through U1 and U2
likely: 0
test_after: 3 # A3, U3, A6
test_after_accepted: 0
no_test: 1 # A7
not_applicable: 1 # A4: tests that predate 004
high_smells: 3
criteria_total: 12
criteria_covered: 11 # US2-AS3 rests on a recorded search, no test
mutation_score: unmeasured # profile records mutation: null; deliberate mutants only
deliberate_mutants: 8 run in this audit, 6 caught, 2 survived (M6 inside DONE behaviour A3; M8 on FR-003's no-claim half)
suite: 360 passed, 0 failed (dotnet, 10 s) + 363 passed, 0 failed (node)
independent: no # this session wrote the tests; the smell pass came from a fresh-context subagent, and every cited line was re-read here
---

# TDD Verification: Answer a caller that has no user

**Verdict: FAIL.** The decisive reason: a mutant survives inside `DONE` behaviour `A3`. The status
endpoint can give a caller with no user a different, empty status, and
`GetStatus_ForACallerWithoutAUser_Answers` stays green, because it asserts only that a value exists
(M6, finding 1). FR-004 says "exactly as they do today".

Three more items block the feature:

- No test covers FR-003's "no user identity" half. The old claimless test was re-pointed, and
  nothing replaced it (M8, finding 2).
- No test pins the claim name the host sends (finding 3).
- Three behaviours are test-after and one has no test, and none has the maintainer's acceptance
  (finding 4).

The core fix holds. Each of the five refusals has a recorded red, the history confirms the order,
and removing the fix fails all five with the real server's error.

## Test-first evidence

| Behavior | Class | Evidence |
| --- | --- | --- |
| U1 | PROVEN | Cycle 1 red recorded (`Assert.Single() Failure: The collection was empty`, after a CS0117 stub step); `ae9effa` adds the test and the helper change together |
| U2 | PROVEN | Cycle 2 red recorded (`Assert.Throws() Failure: No exception was thrown`); `cf0b263` adds test and double together |
| A1 | PROVEN | Cycle 3 red recorded (`ArgumentException : Guid can't be empty`); `aeb98cf` holds the re-pointed test and the guard |
| A2 | PROVEN | Cycle 4 red recorded, same error; `595533b` |
| A8 | PROVEN | Cycle 5 red recorded for all three decisions; `dd08c8c` |
| A5 | PROVEN | Closed by U1 and U2. Finding 3 limits what it proves: the claim name is not checked |
| A3 | TEST_AFTER | Cycle 6 labels it test-after with a mutant; no maintainer acceptance recorded. Its own mutant M6 survives (finding 1) |
| U3 | TEST_AFTER | Cycle 7 labels it test-after with a mutant, caught today (M7); no maintainer acceptance recorded |
| A6 | TEST_AFTER | Cycle 9 records a mutant (re-run here as M2, caught); a suite property with no red-green cycle of its own. `006`'s A5/A6 were classed the same way |
| A7 | NO_TEST | Cycle 8 records a `grep`; no test pins "one way to build a caller with no user" |
| A4 | NOT_APPLICABLE | Tests predating 004; `git diff 463373e -- tests/…/Acceptance` is empty and no signed-in assertion changed |

**The history agrees with the cycle log.** Every behaviour commit holds its test and its source
together, in the cycle order. The two refactor commits (`f4aa12b` production, `7ef5fa9` tests) change
no assertion, and the suite count stays at 358 across them.

**Existing tests changed by the feature:**

- `ReleasesControllerTests.cs`, `GetReleases_WithoutTheUserIdClaim_Is401` became
  `GetReleases_ForACallerWithoutAUser_Is401`. Before: `Controller(caller: null)`, a principal with
  no claim. After: `ControllerWithoutUser()`, a claim holding `Guid.Empty`. The assertion is still
  `UnauthorizedResult`. The case it covered, a principal with no claim, is now covered by nothing.
  This is finding 2.
- `ReleasesControllerTests.cs`, `GetArtists_ReturnsOnlyArtistsInLibrariesTheCallerMayAccess` lost
  its last line, `Assert.IsType<UnauthorizedResult>((await Controller(null).GetArtistsAsync(…)).Result)`.
  The same assertion now lives in `GetArtists_ForACallerWithoutAUser_Is401`, against the host's
  shape. That fixes an eager test, and no signed-in assertion was loosened. The no-claim case it
  carried is again finding 2.
- No test was skipped, excluded, or filtered. No threshold changed.

**`tasks.md` against the list:** every ticked behavioural task names behaviours that are `DONE`, and
no behavioural task is left open. `A3`, `A6`, `A7` and `U3` are `DONE` on the list but not proven
here. Their tasks (T006, T007, T008, T009, T017, T020, T021) are ticked on the list's say-so.

## Findings

Ordered by severity. The smell pass came from a fresh-context subagent; each cited line was re-read
before it was entered.

| # | Severity | Finding | Evidence |
| --- | --- | --- | --- |
| 1 | HIGH | **Vacuous assertion in `DONE` behaviour A3.** `GetStatus_ForACallerWithoutAUser_Answers` asserts `Assert.NotNull(result.Value)` on an empty database. FR-004 requires "exactly as they do today". It should seed a release and a completed fetch, and assert the status equals the one a signed-in caller gets (`StatusResponse` is a record). | `tests/Jellyfin.Plugin.NewReleases.Tests/Api/ReleasesControllerTests.cs:300`; mutant M6 survives |
| 2 | HIGH | **FR-003's "no user identity" half is untested, and the feature removed the test that covered it.** FR-003 requires a request with no identity and one with an empty identity to be treated the same. Before 004, the old 401 tests used a claimless principal. Both now use the empty claim, and A7 bans a claimless helper. `CallerId()` can throw on a missing claim and the whole suite stays green. The test list's Out of Scope drops this case on the host's behaviour (research R1); the spec does not. | `ReleasesControllerTests.cs:43-49` (re-pointed), old `:249` (removed); mutant M8 survives the full suite |
| 3 | HIGH | **Re-implemented expectation: the claim name.** U1 reads the claim type from `ReleasesController.UserIdClaim`, the same constant the helper and the production code use. If the constant were wrong, all three would agree and U1 would pass. No test pins the literal `"Jellyfin-UserId"` that `CustomAuthenticationHandler` writes, so US2-AS1 ("agrees with the host") checks the value only. | `tests/Jellyfin.Plugin.NewReleases.Tests/Support/ControllerContextFactoryTests.cs:18`; `grep '"Jellyfin-UserId"'` finds only `ReleasesController.cs:27` |
| 4 | HIGH | **Test-after and no-test behaviours without the maintainer's acceptance.** A3, U3 and A6 are `TEST_AFTER`, and A7 is `NO_TEST`. The override's `TEST_AFTER_ACCEPTED` needs a dated maintainer decision in the cycle log, plus a mutant caught by the behaviour's own test. U3 and A6 meet the mutant condition today. A3 does not (finding 1). A7 cannot, because a search is not a test. | `tdd/cycle-log.md` cycles 6–9; no acceptance entry |
| 5 | MED | **Fragile theory dispatch.** `EveryDecision_ForACallerWithoutAUser_Is401` maps a string to an action, and its default arm calls `RestoreAsync`. A mistyped `InlineData` such as `"Ignor"` would test Restore twice and Ignore never, and stay green. Each run still asserts, so this is not the catalogue's HIGH "conditional logic" smell. | `ReleasesControllerTests.cs:313-318` |
| 6 | LOW | **Duplicated setup.** `ControllerWithoutUser` repeats `Controller`'s eight-argument construction, and no test passes its `users` parameter. | `ReleasesControllerTests.cs:30-41` |
| 7 | LOW | **Constant declared after first use.** `GetReleases_ForAUserDeletedMidRequest_Is401` uses `Bob`, which is declared further down the file. | `ReleasesControllerTests.cs:290` uses it, `:323` declares it |

**Raised by the subagent and rejected after re-reading:**

- **"Tautological assertion" in `TheUserManager_RejectsAnEmptyId_AsTheHostDoes`.** The test does not
  configure the double; the helper does, and FR-006 makes the helper the subject. Mutant M5 removes
  the throw from the helper, and this test catches it. A tautology cannot catch a change to its
  subject.
- **"Foreign style" in the two names in `ControllerContextFactoryTests.cs`.** The suite already uses
  sentence-style names: `TheReleaseWorkflow_…` five times, `AWellFormedEntry_…`, `TheDerivedSlug_…`
  and others.

**Judged not to be smells:**

- The three refusal tests are not redundant. Removing one endpoint's guard fails only that
  endpoint's test (cycles 3–5).
- The 32-zero literal is the host's format, explained in the class doc. It is not a magic value.
- `EveryDecision` seeds and lists as Alice so that a 404 cannot hide a missing 401.
- Doubles cover only `IUserManager` and `ITaskManager`, and the database is real.
- The tests are deterministic: `TimeProviderStub`, no network, no sleep.
- FR-002 ("no logged error") is observed as "the action returns without throwing". That is sound:
  the host middleware logs only an exception that escapes the action (research R5).

## Mutation results

No mutation tool (`mutation: null`). Eight deliberate mutants, run one at a time in the two files
the feature changed. Each was restored from a copy and verified with `cmp`, and the suite was re-run
green afterwards (360 passed).

| Mutant | Behavior | Survived | Judgment |
| --- | --- | --- | --- |
| M1 `CallerId()` `!= Guid.Empty` → `== Guid.Empty` | A4, A1, A8 | No | Every signed-in test and the no-user tests fail |
| M2 `CallerId()` condition `&& id != Guid.Empty` dropped | A1, A2, A8, A6 | No | All five no-user tests fail with `ArgumentException : Guid can't be empty` |
| M3 `ForCallerWithoutUser()` sends `Guid.NewGuid()` | U1 | No | Caught by U1 |
| M4 `ForCallerWithoutUser()` sends no claim (the old shape) | U1 | No | Caught by U1 only, as intended. A1, A2 and A8 alone would not catch the double drifting back |
| M5 `UserManager` double stops throwing on `Guid.Empty` | U2 | No | Caught by U2 |
| M6 `GetStatusAsync` answers a no-user caller with an empty status | A3 | **Yes** | **Finding 1.** A3 does not test what it claims |
| M7 `GetReleasesAsync` answers an unknown user with an empty list | U3 | No | Caught by U3 |
| M8 `CallerId()` throws on a missing claim | FR-003 (no behaviour) | **Yes** | **Finding 2.** Survives the full suite, not only the feature's tests |

Sampled: every behaviour with production or helper code behind it (A1–A4, A6, A8, U1–U3), plus
FR-003. Not sampled: A5 and A7, which have no code of their own.

## Traceability

| Criterion | Tests | End to end |
| --- | --- | --- |
| US1-AS1 | A1 `GetReleases_ForACallerWithoutAUser_Is401` | Yes, controller |
| US1-AS2 | A2 `GetArtists_ForACallerWithoutAUser_Is401` | Yes, controller |
| US1-AS3 | A3 `GetStatus_ForACallerWithoutAUser_Answers` | Yes, controller, but the assertion is vacuous (finding 1) |
| US1-AS4, SC-003 | A4: existing `ReleasesControllerTests` and `Acceptance/BrowseReleasesTests.cs` | Yes |
| US2-AS1 | A5 through U1, U2 | Helper level by nature; the claim name is unchecked (finding 3) |
| US2-AS2, SC-004 | A6: mutant M2 (cycle 9) | Suite property, evidenced by mutant |
| US2-AS3 | A7: recorded search only | **No test** |
| SC-001, SC-002 | A1, A2, A8 | Yes, controller |
| SC-005 | A1, A2, A8 | Yes, controller |
| FR-003 | A1, A2, A8 cover the empty identity; **nothing covers the missing identity** | Half (finding 2) |

Untested criteria: US2-AS3 (by test), and FR-003's missing-identity half. Tests tracing to nothing:
none.

## What was not audited

- No mutation tool: strength rests on 8 deliberate mutants, not an exhaustive run.
- No coverage tool (`coverage: null`).
- The real server: the 401 and the quiet log were not observed on Jellyfin. This is out of scope by
  the spec's Assumptions; `quickstart.md` step 5 is the manual check.
- The ASP.NET pipeline: routing and `[Authorize]` are not in the loop. The tests call actions
  directly, as the existing suite does.
- `AdminController` and `UserViewController`: they never read the identity (research R2), so nothing
  was graded there.
- Independence: the session that wrote the tests ran this audit. The smell pass was delegated to a
  fresh-context subagent, and its findings were checked line by line.
