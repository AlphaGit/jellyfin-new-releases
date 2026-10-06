# Cycle Log: Answer a caller that has no user

Append only. Newest last. Every entry's `red` block is the evidence that the test existed and
failed before the implementation.

## Baseline

- suite: `dotnet test --configuration Release` -> 352 passed, 0 failed
- web suite: `node --test "tests/web/*.test.js"` -> 363 passed, 0 failed
- commit: `463373e`
- recorded: cycle 0, before any change

## Cycle 1: U1 a caller with no user carries the empty user id the host sends

- test: `Support/ControllerContextFactoryTests.cs::ACallerWithoutAUser_CarriesTheEmptyUserIdTheHostSends` (new file)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~ACallerWithoutAUser_CarriesTheEmptyUserIdTheHostSends" -- RunConfiguration.TreatNoTestsAsError=true`
  -> first `error CS0117: 'ControllerContextFactory' does not contain a definition for 'ForCallerWithoutUser'`;
  with the minimal stub `ForCallerWithoutUser() => ForUser(null)` (today's claimless shape)
  -> `Assert.Single() Failure: The collection was empty` (1 failed)
- green: `Support/ControllerContextFactory.cs` `ForCallerWithoutUser() => ForUser(Guid.Empty)`. Suite -> 353 passed, 0 failed
- refactor: none needed
- commit: `ae9effa`

## Cycle 2: U2 the user manager double rejects an empty id, as the host does

- test: `Support/ControllerContextFactoryTests.cs::TheUserManager_RejectsAnEmptyId_AsTheHostDoes` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~TheUserManager_RejectsAnEmptyId_AsTheHostDoes" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Throws() Failure: No exception was thrown` / `Expected: typeof(System.ArgumentException)` (1 failed)
- green: `Support/ControllerContextFactory.cs` `UserManager(...)` throws `ArgumentException("Guid can't be empty", "id")` for `Guid.Empty`. Suite -> 354 passed, 0 failed
- refactor: class doc comment names both host sources (`CustomAuthenticationHandler.cs:64`, `UserManager.cs:125`); suite re-run 354 passed
- commit: `cf0b263`

## Cycle 3: A1 a release-list caller with no user gets 401 without throwing

- test: `Api/ReleasesControllerTests.cs::GetReleases_ForACallerWithoutAUser_Is401`, re-pointed from
  `GetReleases_WithoutTheUserIdClaim_Is401` (research R4: the old test built a claimless principal the
  host never sends). The assertion stays `UnauthorizedResult`. New helper `ControllerWithoutUser()`.
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~GetReleases_ForACallerWithoutAUser_Is401" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `System.ArgumentException : Guid can't be empty (Parameter 'id')` (1 failed), the real server's error
- green: smallest change, `GetReleasesAsync` refuses `userId == Guid.Empty`. Suite -> 354 passed, 0 failed
- refactor: deferred; the same guard is expected in two more actions (cycles 4, 5)
- commit: `aeb98cf`

## Cycle 4: A2 an artist-list caller with no user gets 401 without throwing

- test: `Api/ReleasesControllerTests.cs::GetArtists_ForACallerWithoutAUser_Is401` (new), replacing the
  claimless `Controller(null).GetArtistsAsync` assertion inside
  `GetArtists_ReturnsOnlyArtistsInLibrariesTheCallerMayAccess` (research R4). That test's signed-in
  assertions are unchanged.
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~GetArtists_ForACallerWithoutAUser_Is401" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `System.ArgumentException : Guid can't be empty (Parameter 'id')` (1 failed)
- green: `GetArtistsAsync` refuses `userId == Guid.Empty`. Suite -> 355 passed, 0 failed
- refactor: deferred to cycle 5
- commit: `595533b`

## Cycle 5: A8 every decision from a caller with no user gets 401 without throwing

- test: `Api/ReleasesControllerTests.cs::EveryDecision_ForACallerWithoutAUser_Is401` (new `[Theory]`: Ignore, HaveIt, Restore on a seeded release)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~EveryDecision_ForACallerWithoutAUser_Is401" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `System.ArgumentException : Guid can't be empty (Parameter 'id')` for each of `"Ignore"`, `"HaveIt"`, `"Restore"` (3 failed).
  FR-008 established by test: the decision endpoints shared the fault.
- green: `DecideAsync` refuses `userId == Guid.Empty`. Suite -> 358 passed, 0 failed
- refactor: (1) the three guards replaced by one condition in `CallerId()`, which now returns null for `Guid.Empty`, doc comment citing research R1; suite 358 passed. (2) no claimless caller remained, so `ForUser(Guid?)` became `ForUser(Guid)` and the test helper `Controller(Guid? caller, …)` became `Controller(Guid caller, …)`; suite 358 passed
- commit: `dd08c8c` (behaviour), `f4aa12b` (structure, production), `7ef5fa9` (structure, tests)

## Cycle 6: A3 the status still answers a caller with no user

- test: `Api/ReleasesControllerTests.cs::GetStatus_ForACallerWithoutAUser_Answers` (new)
- red: none. **Test-after, as planned**: `GetStatusAsync` never read the user, so the test passed on its first run
  (`Passed: 1`). Deliberate mutant: an `if (CallerId() is null) return Unauthorized();` guard at the top of
  `GetStatusAsync` -> `Assert.NotNull() Failure: Value is null` (1 failed). Restored from a file copy, `cmp -s` identical
- green: no production change. Suite -> 359 passed, 0 failed
- refactor: none
- commit: `5b0c049`

## Cycle 7: U3 a user deleted mid-request is refused as 401

- test: `Api/ReleasesControllerTests.cs::GetReleases_ForAUserDeletedMidRequest_Is401` (new: claim for Bob, user manager knows only Alice)
- red: none. **Test-after, as planned**: `AccessOf` already returned null for an unknown user (`Passed: 1`).
  Deliberate mutant: `AccessOf` returning `new LibraryAccess(true, …)` for a null user
  -> `Assert.IsType() Failure: Value is null` (1 failed). Restored from a file copy, `cmp -s` identical
- green: no production change. Suite -> 360 passed, 0 failed
- refactor: none
- commit: `fdaeff5`

## Cycle 8: A7 one way to build a caller with no user

- evidence: `grep -rn "new ClaimsIdentity\|ForCallerWithoutUser\|ForUser(" tests/Jellyfin.Plugin.NewReleases.Tests --include='*.cs'` ->
  one `new ClaimsIdentity` (`Support/ControllerContextFactory.cs:22`, always with the `Jellyfin-UserId` claim);
  `ForCallerWithoutUser()` defined once (`:27`) and used by `ReleasesControllerTests.cs:40` and `ControllerContextFactoryTests.cs:16`;
  `ForUser(` takes a non-null `Guid` (`:20`), called by `AcceptanceRig.cs:50` and `ReleasesControllerTests.cs:33` with real ids. No `ForUser(null)` anywhere
- commit: none (no change; structure landed in `7ef5fa9`)

## Cycle 9: A6 removing the handling fails the suite with the host's error

- deliberate mutant: `CallerId()` with `&& id != Guid.Empty` deleted
- run: `dotnet test --configuration Release --filter "FullyQualifiedName~ReleasesControllerTests" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Failed: 5, Passed: 22`: `GetReleases_ForACallerWithoutAUser_Is401`, `GetArtists_ForACallerWithoutAUser_Is401`,
  `EveryDecision_ForACallerWithoutAUser_Is401` (`Ignore`, `HaveIt`, `Restore`), each
  `System.ArgumentException : Guid can't be empty (Parameter 'id')`
- restored from a file copy, `cmp -s` identical
- commit: none (no change)

## Notes and deviations

- `A5` closes with `U1` and `U2` (cycles 1, 2); it has no test of its own, as planned.
- `A4` was `DONE` at planning. No signed-in assertion was edited. Full suite 360 passed; `dotnet build --configuration Release`
  0 warnings; `node --test "tests/web/*.test.js"` 363 passed. `tasks.md` T012 is left open: its run belongs after
  T010, whose contract edit `HttpSurfaceTests` scans.
- The fix landed in three per-action steps and was then consolidated (cycle 5 refactor), so each endpoint got its own red.

## Cycle 10: A3 remediation, the status answers a caller with no user exactly as a signed-in one (T022, audit finding 1)

- test: `Api/ReleasesControllerTests.cs::GetStatus_ForACallerWithoutAUser_Answers` (strengthened). Before: `Assert.NotNull(result.Value)` on an
  empty database. After: a seeded release and a completed fetch, then `Assert.Equal(signedIn, result.Value)`, where `signedIn` is the
  status a signed-in caller gets (FR-004, "exactly as they do today")
- red: against the audit's surviving mutant M6 (`GetStatusAsync` returning `new StatusResponse(false, null, 0, false)` when
  `CallerId()` is null), applied before the test was changed.
  `dotnet test --configuration Release --filter "FullyQualifiedName~GetStatus_ForACallerWithoutAUser_Answers" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: Values differ` / `Expected: StatusResponse { HasStoredReleases = True, … RefreshIntervalHours = 24 … }` /
  `Actual: StatusResponse { HasStoredReleases = False, ReleasesLastCheckedAt = , RefreshIntervalHours = 0 … }` (1 failed)
- green: mutant removed, source restored from a file copy (`cmp -s` identical); no production change. Suite -> 360 passed, 0 failed
- refactor: none. The three seed-and-fetch lines repeat `GetReleases_ListAndStatusReportTheSameInstant:148-150`; extracting them
  would edit a test outside this cycle, so it is reported, not done
- commit: `378fe1d`
- note: the red is against a mutant, not against missing behaviour. The behaviour already held; the remediation is the test's strength

## Cycle 11: A5 remediation, the claim name is pinned to the host's literal (T024, audit finding 3)

- test: `Support/ControllerContextFactoryTests.cs::ACallerWithoutAUser_CarriesTheEmptyUserIdTheHostSends` (strengthened). Before: the
  claim was found by `ReleasesController.UserIdClaim`, the plugin's own constant. After: by `HostUserIdClaim = "Jellyfin-UserId"`,
  stated in the test from the host's `InternalClaimTypes.UserId`
- red: against a mutant renaming the plugin's constant to `"Jellyfin-User-Id"` (`ReleasesController.cs:27`), applied before the test
  was changed.
  `dotnet test --configuration Release --filter "FullyQualifiedName~ACallerWithoutAUser_CarriesTheEmptyUserIdTheHostSends" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Single() Failure: The collection was empty` (1 failed)
- green: mutant removed, source restored from a file copy (`cmp -s` identical); no production change. Suite -> 360 passed, 0 failed
- refactor: none
- commit: `caa281c`
- note: as cycle 10, the red is against a mutant
