# Research: Answer a caller that has no user

Phase 0 for [`plan.md`](./plan.md). Host behaviour is read from Jellyfin's source on `master`
(the 12.x line), not from the 10.x documentation.

## R1 — How the host presents "no user"

**Decision**: a caller with no user carries the `Jellyfin-UserId` claim, holding
`Guid.Empty` in `N` format (`00000000000000000000000000000000`). The plugin treats that value as
no user.

**Evidence**:

- `Jellyfin.Api/Auth/CustomAuthenticationHandler.cs:64` always adds
  `new Claim(InternalClaimTypes.UserId, authorizationInfo.UserId.ToString("N", …))`, for every
  request that carries a token.
- `MediaBrowser.Controller/Net/AuthorizationInfo.cs:16`:
  `public Guid UserId => User?.Id ?? Guid.Empty;`
- `Jellyfin.Server.Implementations/Security/AuthorizationContext.cs`: an API key sets
  `IsApiKey = true` and never sets `User`. A device token sets `User = GetUserById(device.UserId)`,
  which is null when that user was deleted.
- `Jellyfin.Server.Implementations/Users/UserManager.cs:125`: `GetUserById` throws
  `ArgumentException("Guid can't be empty")` for an empty id. It returns null for an unknown
  non-empty id.
- A request with no token gets `AuthenticateResult.NoResult()`, so `[Authorize]` refuses it before
  the plugin runs. **A principal with no `Jellyfin-UserId` claim never reaches the plugin.** The
  current test double builds exactly that principal.

**Consequence**: an API key and a deleted user's token look the same to the plugin (spec session
2026-10-04). A present, non-empty id that names a missing user occurs only when the user is deleted
while a request is in flight. `GetUserById` returns null for it, and the plugin already answers 401.

## R2 — Which endpoints read the caller's identity (FR-008)

**Decision**: five actions, all in `ReleasesController`, and all through one private method,
`CallerId()`.

| Action | Reads identity | Through |
| --- | --- | --- |
| `GET Releases` | yes | `CallerId()` → `AccessOf()` |
| `GET Artists` | yes | `CallerId()` → `AccessOf()` |
| `POST Releases/{id}/Ignore` | yes | `DecideAsync` → `CallerId()` → `AccessOf()` |
| `POST Releases/{id}/HaveIt` | yes | same |
| `POST Releases/{id}/Restore` | yes | same |
| `GET Status` | no | — |
| `AdminController`, all four actions | no | elevation policy only |
| `UserViewController` | no | serves a static fragment |

Established by reading every `ControllerBase` subclass in `src/…/Api/`. The decision endpoints
were not called with an API key on the real server. They share the fault because they share the
path: `CallerId()` accepts `Guid.Empty`, and `AccessOf()` passes it to `GetUserById`.

## R3 — Where the plugin recognises an empty identity

**Decision**: in `CallerId()`. It returns null when the claim parses to `Guid.Empty`, the same
result as a claim that does not parse. Every per-user action already refuses on null, so one
condition covers all five (FR-001, FR-003).

**Rationale**: `CallerId()` is the single place the plugin reads the identity. A future per-user
action that calls it inherits the rule.

**Alternatives rejected**:

- **Read the `Jellyfin-IsApiKey` claim.** It misses a deleted user's token, which is not an API
  key but has the same empty id. FR-003 names the empty identity, not the key.
- **Catch `ArgumentException` around `GetUserById`.** Uses an exception for control flow, and
  the host still constructs and throws it on every such call.
- **An authorization policy or filter.** A new type for one comparison (constitution VI).
- **`Jellyfin.Extensions`' `IsEmpty()`.** Same result as `!= Guid.Empty`; the base library
  comparison needs no extra `using`.

## R4 — How the tests model the caller (FR-006, FR-007)

**Decision**: two changes in `tests/…/Support/ControllerContextFactory.cs`.

1. **One way to build a caller with no user.** A `ForCallerWithoutUser()` method builds the
   principal the host builds: the `Jellyfin-UserId` claim holding `Guid.Empty.ToString("N")`.
   `ForUser` takes a non-null `Guid`. The no-claim branch is removed, because the host never
   produces that principal (R1).
2. **`UserManager(...)` throws for an empty id, as the host does.** It throws
   `ArgumentException` for `Guid.Empty` and returns null for an unknown id.

**Rationale**: change 2 is what makes SC-004 hold. With today's double, removing the plugin's
guard is invisible: `Guid.Empty` reaches the double, the double returns null, `AccessOf` returns
null, and the action still answers 401. With the throwing double, the same removal makes every
no-user test fail with the server's own exception. A test that cannot fail when the handling is
removed does not cover the handling.

**Not modelled**: the `Jellyfin-IsApiKey` claim and the host's other claims. The plugin does not
read them, and R3 rules out reading `IsApiKey`.

**Replaced tests**: `GetReleases_WithoutTheUserIdClaim_Is401` and the `Controller(null)` assertion
in the artists test model a principal the host never sends. Both move to
`ForCallerWithoutUser()`. This replaces a wrong double with the host's shape. It does not weaken
the assertion, which stays "401" (constitution II; FR-006 decides).

## R5 — How "no error in the server log" is observed without a server (FR-002, SC-002)

**Decision**: the test asserts that the action **returns** `UnauthorizedResult` and does not
throw.

**Rationale**: the plugin writes nothing on this path. The `[ERR]` line on the real server came
from the host's exception middleware, which logged the unhandled `ArgumentException` and answered
400. An action that returns normally gives the middleware nothing to log. A logger assertion on the
controller would pass both before and after the fix, so it proves nothing.

## R6 — Status code and contract

**Decision**: `401`, from the existing `Unauthorized()` call. No new status code, no response body.

- `specs/001-track-new-releases/contracts/http-api.md` states `no user id claim → 401` for the
  decision endpoints only. It is reworded to "no user (an API key, or a deleted user's token)" and
  the same row is added to `GET Releases` and `GET Artists`, which already declare
  `ProducesResponseType(401)`.
- `CHANGELOG.md` gains an `Unreleased` → `Fixed` entry: an API-key caller now gets `401` from the
  per-user endpoints, instead of `400` and a logged error.

**Alternatives rejected**: `403` — the request is not "this user may not see this", it is "there
is no user to decide for" (spec Assumptions).
