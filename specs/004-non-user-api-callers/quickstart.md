# Quickstart: Answer a caller that has no user

How to prove the feature works. Design: [`plan.md`](./plan.md). Host facts: [`research.md`](./research.md).

## Prerequisites

- .NET 10 SDK on `PATH` (`dotnet --list-sdks` shows `10.0.x`).
- Repository root as the working directory.

## 1. The no-user caller is refused cleanly (US1, FR-001, FR-002, SC-001, SC-005)

```bash
dotnet test --configuration Release --filter "FullyQualifiedName~ReleasesControllerTests" -- RunConfiguration.TreatNoTestsAsError=true
```

**Expected**: green. For each of `Releases`, `Artists`, `Ignore`, `HaveIt` and `Restore`, a
caller built by `ControllerContextFactory.ForCallerWithoutUser()` gets `UnauthorizedResult`, and
the action does not throw. `Status` answers that caller normally. A claim that names a missing user
also gets `UnauthorizedResult`.

## 2. The double agrees with the host (US2, FR-006, FR-007)

Read `tests/Jellyfin.Plugin.NewReleases.Tests/Support/ControllerContextFactory.cs`.

**Expected**:

- `ForCallerWithoutUser()` is the only way to build a caller with no user. It puts
  `Jellyfin-UserId` = `Guid.Empty.ToString("N")` on the principal, as
  `CustomAuthenticationHandler` does (R1).
- No helper builds a principal without the `Jellyfin-UserId` claim.
- `UserManager(...)` throws `ArgumentException` for `Guid.Empty`, as `UserManager.GetUserById`
  does.

## 3. The suite fails when the handling is removed (SC-004)

1. In `ReleasesController.CallerId()`, delete the `Guid.Empty` condition.
2. Run the step 1 command.
3. Restore the condition.

**Expected at step 2**: red. Every no-user test fails with
`System.ArgumentException: Guid can't be empty`, the error from the real server.

## 4. Nothing changed for a signed-in person (FR-005, SC-003)

```bash
dotnet test --configuration Release
```

**Expected**: green, with zero build warnings. The acceptance tests for `001` and `002` are not
edited by this feature.

## 5. Optional: the real server

Not part of this feature's done condition (spec Assumptions). On a Jellyfin 12 server with a
throwaway API key:

```bash
curl -s -o /dev/null -w '%{http_code}\n' -H "Authorization: MediaBrowser Token=\"$KEY\"" "$SERVER/Plugins/NewReleases/Releases"
curl -s -o /dev/null -w '%{http_code}\n' -H "Authorization: MediaBrowser Token=\"$KEY\"" "$SERVER/Plugins/NewReleases/Status"
```

**Expected**: `401`, then `200`. No `[ERR]` line for the plugin in the server log. Revoke the key
afterwards.
