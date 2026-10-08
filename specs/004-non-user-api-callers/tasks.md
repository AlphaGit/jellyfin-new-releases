---

description: "Task list for 004-non-user-api-callers"
---

# Tasks: Answer a caller that has no user

**Input**: Design documents from `/specs/004-non-user-api-callers/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md),
[quickstart.md](./quickstart.md)

**Tests**: **Mandatory.** Constitution II is non-negotiable. Every behaviour change is written first
and observed red. The red for the fix is the server's own failure,
`System.ArgumentException: Guid can't be empty`, and it appears only after the test double takes the
host's shape (Phase 2).

**Behaviour markers**: `[A…]`/`[U…]` are the ids in [`tdd/test-list.md`](./tdd/test-list.md).
`/speckit-tdd-run` ticks a task only when every behaviour it names is `DONE`.

**Organization**: by user story. Both stories are P1. The double (`US2`'s subject) is the
prerequisite for an honest red in `US1`, so its reshaping is foundational. `US2`'s own phase then
proves the double does its job.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: can run in parallel (different files, no dependency on an incomplete task)
- **[Story]**: US1, US2
- Exact file paths in every description

## Scale

One production line, one test helper reshaped, five refusal behaviours and two pins in one test
file, two tests of the double in one new test file beside it, one contract row, one changelog
entry. **No new source file, no new dependency.** If this grows a
class, something has gone wrong: `research.md` R3 lists the policy, filter and catch that were
rejected.

---

## Phase 1: Setup

- [X] T001 Confirm the baseline is green before changing anything: `dotnet build --configuration Release` with zero warnings, `dotnet test --configuration Release`, `node --test "tests/web/*.test.js"`. Record the counts. A red baseline means no later red can be attributed to this feature

---

## Phase 2: Foundational: give the test double the host's shape (`research.md` R1, R4)

**Blocks every user story**: with today's double, the plugin's missing guard is invisible. `Guid.Empty`
reaches a user manager that returns null, and the action still answers 401.

- [X] T014 [U1] [U2] Write failing `tests/Jellyfin.Plugin.NewReleases.Tests/Support/ControllerContextFactoryTests.cs` with two tests: `ACallerWithoutAUser_CarriesTheEmptyUserIdTheHostSends` asserts that `ControllerContextFactory.ForCallerWithoutUser().HttpContext.User` carries exactly one `Jellyfin-UserId` claim, whose value is `Guid.Empty.ToString("N")`; `TheUserManager_RejectsAnEmptyId_AsTheHostDoes` asserts that `ControllerContextFactory.UserManager().GetUserById(Guid.Empty)` throws `ArgumentException`. Observe red: `U1` as build error CS0117 (`ForCallerWithoutUser` does not exist yet), `U2` as `Assert.Throws() Failure: No exception was thrown`. Record both in `tdd/cycle-log.md`
- [X] T002 [U1] [U2] In `tests/Jellyfin.Plugin.NewReleases.Tests/Support/ControllerContextFactory.cs`: (a) change `ForUser(Guid? userId)` to `ForUser(Guid userId)`, which always adds the `Jellyfin-UserId` claim; (b) add `ForCallerWithoutUser()`, which adds the `Jellyfin-UserId` claim holding `Guid.Empty.ToString("N")`, the principal `CustomAuthenticationHandler.cs:64` builds for an API key or a deleted user's token; (c) make `UserManager(...)` throw `new ArgumentException("Guid can't be empty", "id")` for `Guid.Empty`, as `UserManager.GetUserById` does at `UserManager.cs:125`, and keep returning null for an unknown non-empty id. Update the class doc comment to name both host sources. No helper may build a principal without the claim. T014's two tests go green
- [X] T003 [A1] [A2] In `tests/Jellyfin.Plugin.NewReleases.Tests/Api/ReleasesControllerTests.cs`, split the `Controller(Guid? caller, params User[] users)` helper into `Controller(Guid caller, params User[] users)` (uses `ForUser`) and `ControllerWithoutUser(params User[] users)` (uses `ForCallerWithoutUser`). Re-point the two no-claim uses: rename `GetReleases_WithoutTheUserIdClaim_Is401` to `GetReleases_ForACallerWithoutAUser_Is401` and call `ControllerWithoutUser()`; move the `Controller(null).GetArtistsAsync` assertion out of `GetArtists_ReturnsOnlyArtistsInLibrariesTheCallerMayAccess` into its own `GetArtists_ForACallerWithoutAUser_Is401`, calling `ControllerWithoutUser().GetArtistsAsync`, so each endpoint has one test that names it (SC-005). The assertion stays `UnauthorizedResult` in both (R4: the double is corrected, the test is not weakened). Build with zero warnings

**Checkpoint**: the suite builds. T014's two tests are green. Exactly the two re-pointed tests are red, with
`System.ArgumentException: Guid can't be empty`. Every other test is green. Record the output in
`specs/004-non-user-api-callers/tdd/cycle-log.md`.

---

## Phase 3: User Story 1: An operator scripting against the plugin gets a usable answer (Priority: P1) 🎯 MVP

**Goal**: every per-user endpoint refuses a caller with no user as `401`, without throwing, so the
host logs nothing. Endpoints that do not read the user still answer.

**Independent Test**: `quickstart.md` step 1.

### Tests for User Story 1 ⚠️

> Write these FIRST and observe them failing for the right reason: the server's `ArgumentException`.

- [X] T004 [A8] [US1] Write failing `EveryDecision_ForACallerWithoutAUser_Is401` in `tests/Jellyfin.Plugin.NewReleases.Tests/Api/ReleasesControllerTests.cs`: a `[Theory]` over `Ignore`, `HaveIt` and `Restore`, each called through `ControllerWithoutUser()` on a seeded release id, asserting `UnauthorizedResult` and that no exception escapes. Observe red with `ArgumentException: Guid can't be empty`. This establishes FR-008 for the decision endpoints by test, not by assumption

### Implementation for User Story 1

- [X] T005 [A1] [A2] [A8] [US1] In `src/Jellyfin.Plugin.NewReleases/Api/ReleasesController.cs`, make `CallerId()` return null when the claim parses to `Guid.Empty`: `Guid.TryParse(User.FindFirst(UserIdClaim)?.Value, out var id) && id != Guid.Empty ? id : null`. Update its doc comment: the host sends `Guid.Empty` for an API key and for a deleted user's token (research R1). T003's two tests and T004 go green; the whole suite is green

### Pins for User Story 1 (test-after: the behaviour already holds)

- [X] T006 [A3] [US1] Add `GetStatus_ForACallerWithoutAUser_Answers` in `tests/Jellyfin.Plugin.NewReleases.Tests/Api/ReleasesControllerTests.cs`: `ControllerWithoutUser().GetStatusAsync` returns a non-null `StatusResponse` (FR-004, US1-AS3). Record it as test-after in `tdd/cycle-log.md`, with a deliberate mutant (add a `CallerId()` guard to `GetStatusAsync`) that turns it red
- [X] T007 [U3] [US1] Add `GetReleases_ForAUserDeletedMidRequest_Is401` in the same file: `Controller(Bob, User(Alice, allFolders: true))` (a non-empty id that the user manager does not know) returns `UnauthorizedResult` without throwing (spec edge case, session 2026-10-04). Record it as test-after, with a deliberate mutant (`AccessOf` returning full access for an unknown user) that turns it red

### Acceptance closes for User Story 1

- [X] T015 [A1] [US1] Confirm `GetReleases_ForACallerWithoutAUser_Is401` is green on the full suite. US1-AS1 is not closed before
- [X] T016 [A2] [US1] Confirm `GetArtists_ForACallerWithoutAUser_Is401` is green on the full suite. US1-AS2 is not closed before
- [X] T017 [A3] [US1] Confirm `GetStatus_ForACallerWithoutAUser_Answers` is green on the full suite and its mutant is recorded. US1-AS3 is not closed before
- [X] T018 [A8] [US1] Confirm `EveryDecision_ForACallerWithoutAUser_Is401` is green for all three decisions on the full suite. SC-005 is not closed before

**Checkpoint**: `quickstart.md` step 1 is green. The five per-user actions (`Releases`, `Artists`,
`Ignore`, `HaveIt`, `Restore`) each have a no-user test (SC-005).

---

## Phase 4: User Story 2: The tests model an absent user the way the host really does (Priority: P1)

**Goal**: the double agrees with the host, there is one way to build a caller with no user, and the
suite fails when the plugin's handling is removed.

**Independent Test**: `quickstart.md` steps 2 and 3.

- [X] T008 [A7] [US2] Confirm FR-007: `grep -rn "new ClaimsIdentity\|ForCallerWithoutUser\|ForUser(" tests/Jellyfin.Plugin.NewReleases.Tests` shows one `ClaimsIdentity` construction (in `Support/ControllerContextFactory.cs`), no `ForUser(null)`, and every no-user caller built through `ForCallerWithoutUser()`. `Acceptance/AcceptanceRig.cs` needs no change: it passes a real id to `ForUser`. Record the search and its output in `tdd/cycle-log.md`
- [X] T009 [A6] [US2] Mutant check for SC-004 and US2-AS2: copy `src/Jellyfin.Plugin.NewReleases/Api/ReleasesController.cs` aside, delete the `&& id != Guid.Empty` condition, run `dotnet test --configuration Release --filter "FullyQualifiedName~ReleasesControllerTests" -- RunConfiguration.TreatNoTestsAsError=true`, and confirm all five no-user tests fail with `ArgumentException: Guid can't be empty`. Restore from the copy, verify with `cmp -s`, and record the result in `tdd/cycle-log.md`

- [X] T019 [A5] [US2] Confirm `U1` and `U2` are `DONE` in `tdd/test-list.md` and green on the full suite. US2-AS1 is not closed before
- [X] T020 [A6] [US2] Confirm T009's mutant record is in `tdd/cycle-log.md`. US2-AS2 and SC-004 are not closed before
- [X] T021 [A7] [US2] Confirm T008's search record is in `tdd/cycle-log.md`. US2-AS3 is not closed before

**Checkpoint**: removing the handling turns the suite red with the real server's error.

---

## Phase 5: Polish & cross-cutting

- [X] T010 [P] In `specs/001-track-new-releases/contracts/http-api.md`, reword the decision endpoints' `no user id claim` → `401` row to `no user (an API key, or a deleted user's token)` → `401`, and add the same row to `GET /Releases` and `GET /Artists` (research R6). Name no route the plugin does not serve: `HttpSurfaceTests.NoContractDocument_NamesARouteThePluginDoesNotServe` scans this file
- [X] T011 [P] In `CHANGELOG.md`, add an `## Unreleased` section above `## 0.2.0` with a `### Fixed` entry: a caller authenticated by API key now gets `401` from the release list, the artist list and the decision endpoints, instead of `400` and an error in the server log
- [X] T012 [A4] Run `quickstart.md` step 4: `dotnet build --configuration Release` with zero warnings, `dotnet test --configuration Release`, `node --test "tests/web/*.test.js"`. Counts equal T001's plus the tests this feature added, and no `001` or `002` acceptance test was edited (SC-003)
- [ ] T013 When JD asks for the push: push `main` and verify the CI run green with `gh run watch` (constitution, Development Workflow). The feature is not done before that

---

## Dependencies & Execution Order

- **Phase 1** first.
- **Phase 2**: T014 red → T002 → T003. T002 turns T014 green; T002 and T003 form one commit, because T002 alone leaves the controller tests uncompiled.
- **US1 (Phase 3)**: T004 red before T005. T006 and T007 after T005; same file, so in sequence. T015–T018 after T007.
- **US2 (Phase 4)**: T008 and T009 after T005. T019–T021 after T009.
- **Polish (Phase 5)**: T010 and T011 any time after T005. T012 after everything else, and it closes `A4`. T013 last.

### Parallel Opportunities

- T010 and T011 touch two different files with no dependency between them.
- Everything else touches `ReleasesControllerTests.cs` or `ReleasesController.cs` in sequence. The
  TDD loop takes one behaviour at a time anyway.

---

## Implementation Strategy

**MVP is Phases 2 and 3 through T005**: the double in the host's shape and the one-line fix. That
alone ends the `400` and the logged error for every per-user endpoint.

**Phase 4 is why the feature exists** (spec, User Story 2). It proves the safety net can now catch
the defect. A no-user test that stays green with the fix removed covers nothing.

**Real-server verification is out of scope** (spec Assumptions). `quickstart.md` step 5 is the
optional check for the maintainer's own pass.

---

## Notes

- Commit after each task or logical group. Conventional Commits; no AI co-author trailer.
- Deliberate mutants are restored from a file copy verified with `cmp -s`, never `git checkout --`.
- `dotnet test --filter` requires the trailing `-- RunConfiguration.TreatNoTestsAsError=true`.
  Without it, a filter that matches nothing exits 0.
- `AdminController` and `UserViewController` are not touched (research R2).

---

## Phase 6: TDD remediation

From [`tdd/verification.md`](./tdd/verification.md), verdict **FAIL** at `0edb1d5`. **The feature is
not done until T022–T025 are cleared.** T022–T024 are suite changes and go through
`/speckit-tdd-run`: each is written red first and observed failing against the survivor or mutant
named, then made green. The single-test command is
`dotnet test --configuration Release --filter "FullyQualifiedName~{name}" -- RunConfiguration.TreatNoTestsAsError=true`.

- [X] T022 [A3] Finding 1 (HIGH): make `GetStatus_ForACallerWithoutAUser_Answers` (`tests/Jellyfin.Plugin.NewReleases.Tests/Api/ReleasesControllerTests.cs:300`) assert FR-004's "exactly as today": seed a release and a completed fetch, then assert the no-user caller's `StatusResponse` equals the one a signed-in caller gets. Done when the audit's mutant M6 (`GetStatusAsync` returning `new StatusResponse(false, null, 0, false)` when `CallerId()` is null) fails it
- [X] T023 Finding 2 (HIGH, the maintainer's decision): FR-003's "no user identity" half has no test. Either add a test in `tests/Jellyfin.Plugin.NewReleases.Tests/Api/ReleasesControllerTests.cs` that builds a claimless principal inline (not through a helper, so `A7` still holds) and asserts `UnauthorizedResult` from the release list, done when mutant M8 (`CallerId()` throwing on a missing claim) fails it; or amend `spec.md` FR-003 to name only the empty identity the host sends (research R1), with the reason, and drop the case from the test list
- [X] T024 [A5] Finding 3 (HIGH): pin the claim name the host writes. In `tests/Jellyfin.Plugin.NewReleases.Tests/Support/ControllerContextFactoryTests.cs:18`, find the claim by the literal `"Jellyfin-UserId"` (from `CustomAuthenticationHandler`) instead of `ReleasesController.UserIdClaim`. Done when changing the constant at `src/Jellyfin.Plugin.NewReleases/Api/ReleasesController.cs:27` fails the suite
- [X] T025 Finding 4 (HIGH, the maintainer's decision): for A3, U3 and A6, either record in `tdd/cycle-log.md` the maintainer's dated decision to accept each as test-after (A3 only after T022, so that its mutant is caught), or re-drive the behaviour red-first. For A7, either add a test that pins "one way to build a caller with no user", or amend the test list to record the search as accepted evidence, with the reason. Done when `/speckit-tdd-verify` classes none of them `TEST_AFTER` or `NO_TEST`
- [X] T026 Finding 5 (MED): in `EveryDecision_ForACallerWithoutAUser_Is401` (`ReleasesControllerTests.cs:313-318`), give `"Restore"` its own arm and make the default arm throw, or replace the strings with `MemberData` of action delegates. Done when a mistyped `InlineData` value fails the test instead of testing Restore
- [X] T027 Finding 6 (LOW): remove the duplicated controller construction between `Controller` and `ControllerWithoutUser` (`ReleasesControllerTests.cs:30-41`), and drop `ControllerWithoutUser`'s unused `users` parameter. Structural commit, suite count unchanged
- [X] T028 Finding 7 (LOW): declare `Bob` (`ReleasesControllerTests.cs:323`) beside `Alice`, before its first use at `:290`. Structural commit, suite count unchanged
- [ ] T029 Re-run `/speckit-tdd-verify` after T022–T028. Done when the verdict is `PASS` or `PASS_WITH_GAPS`

---

## Phase 7: TDD remediation, second audit

From [`tdd/verification.md`](./tdd/verification.md), verdict **FAIL** at `1ba5365`. **The feature
is not done until T030 and T031 are cleared.** T030 and T031 go through `/speckit-tdd-run`. The
single-test command is
`dotnet test --configuration Release --filter "FullyQualifiedName~{name}" -- RunConfiguration.TreatNoTestsAsError=true`.

- [X] T030 [A3] Finding 1 (HIGH): in `GetStatus_ForACallerWithoutAUser_Answers` (`tests/Jellyfin.Plugin.NewReleases.Tests/Api/ReleasesControllerTests.cs:306`), assert the expected status as a literal, `new StatusResponse(true, _clock.GetUtcNow(), 24, false)`, for the seeded release and fetch. The signed-in comparison may stay as a second assertion. Done when the audit's mutant M11 (`GetStatusAsync` returning `Unauthorized()` to every caller) fails this test on its own
- [X] T031 [A7] Finding 2 (HIGH): add a test in `tests/Jellyfin.Plugin.NewReleases.Tests/Support/ControllerContextFactoryTests.cs` that pins FR-007's "one way": the factory's only members that build a `ControllerContext` are `ForUser` and `ForCallerWithoutUser`, and each builds exactly one `Jellyfin-UserId` claim. Record it in `tdd/cycle-log.md` as test-after under the maintainer's 2026-10-07 acceptance, with a mutant it catches (a second no-user builder added to the factory, or M10). Done when `/speckit-tdd-verify` classes A7 `TEST_AFTER_ACCEPTED`
- [X] T032 Finding 3 (LOW): extract a `SignedInController()` and a `SeedCheckedArtistAsync()` in `ReleasesControllerTests.cs`, and use them at `:148-150`, `:164-166`, `:299-302` and wherever `Controller(Alice, ControllerContextFactory.User(Alice, allFolders: true))` appears. Structural commit, suite count unchanged
- [X] T033 Finding 4 (LOW): rename `GetStatus_ForACallerWithoutAUser_Answers` (`ReleasesControllerTests.cs:297`) to say it answers as for a signed-in caller, and update the test name in `tdd/test-list.md` A3. Structural commit, suite count unchanged
- [X] T034 Finding 5 (LOW): in `EveryDecision_ForACallerWithoutAUser_Is401` (`ReleasesControllerTests.cs:315`), read the seeded release id from `_db.Releases` instead of through `GetReleasesAsync`. Structural commit, suite count unchanged
- [ ] T035 Re-run `/speckit-tdd-verify` after T030–T034. Done when the verdict is `PASS` or `PASS_WITH_GAPS`
