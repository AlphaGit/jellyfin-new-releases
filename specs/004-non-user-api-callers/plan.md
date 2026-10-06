# Implementation Plan: Answer a caller that has no user

**Branch**: `004-non-user-api-callers` | **Date**: 2026-10-04 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/004-non-user-api-callers/spec.md`

## Summary

A caller authenticated by API key gets `400` and a logged `ArgumentException` from the per-user
endpoints, instead of `401`. Jellyfin represents "no user" as a `Jellyfin-UserId` claim holding
`Guid.Empty`. The plugin's `CallerId()` accepts that value and passes it to
`IUserManager.GetUserById`, which throws (research R1).

**The fix is one condition**: `CallerId()` returns null for `Guid.Empty`. All five per-user actions
read the identity through `CallerId()` and already refuse on null (R2, R3).

**The larger half is the test double.** Today it models "no user" as a principal with no claim,
which the host never sends. Its `IUserManager` also returns null for an empty id, where the host
throws. Because of that, removing the fix would leave the suite green. The double moves to the
host's shape: one way to build a caller with no user, and a user manager that throws as the host
does (R4).

## Technical Context

**Language/Version**: C# on `net10.0`.

**Primary Dependencies**: unchanged, and none added.

**Storage**: unchanged. No schema, configuration or stored-data change.

**Testing**: xunit + NSubstitute. Changes are in `Support/ControllerContextFactory.cs` and
`Api/ReleasesControllerTests.cs`. No network and no server (constitution III).

**Target Platform**: Jellyfin 12.0.x; host behaviour read from the 12.x source (R1).

**Project Type**: Jellyfin server plugin.

**Performance Goals / Constraints**: none engaged. One comparison per request.

**Scale/Scope**: one production line, one test helper reshaped, about eight tests added or
re-pointed, one contract document amended, one changelog entry.

No `NEEDS CLARIFICATION` remains. The one open spec decision (a deleted user) was settled in spec
session 2026-10-04.

## Constitution Check

*Checked against `.specify/memory/constitution.md` v1.4.0. Re-checked after Phase 1; unchanged.*

| Principle | Verdict | Evidence |
| --- | --- | --- |
| **I. Spec-Driven Development** | **Pass** | Two clarification sessions. The second corrected an edge case whose premise the host's source contradicted. No open decision. |
| **II. Test-Driven Development** | **Pass** | The double change lands first and alone. The no-user tests then go red with the server's own `ArgumentException`, and the fix turns them green. Two tests only re-point to the corrected double, and their assertion stays `401` (R4). Pins for behaviour that already holds are recorded as test-after. |
| **III. Hermetic Tests** | **Pass** | The double reproduces the host's claim and the host's `GetUserById` rule. Nothing calls a server. |
| **IV. Jellyfin Compatibility** | **Pass** | GUID, configuration, schema and routes unchanged. The fix follows the host's own representation of "no user". |
| **V. Respectful Sources and Privacy** | **Pass (not engaged)** | No source, no outgoing request, nothing logged. |
| **VI. Simplicity** | **Pass** | One condition in the method that already reads the identity. A policy, a filter, and a catch around the host call were rejected (R3). |

## Project Structure

### Documentation (this feature)

```text
specs/004-non-user-api-callers/
├── plan.md          # This file
├── spec.md
├── NOTES.md
├── research.md      # Phase 0: R1..R6
├── quickstart.md    # Phase 1: suite scenarios, mutant check, optional server check
├── checklists/
└── tasks.md         # Phase 2: NOT created by /speckit-plan
```

**No `data-model.md`.** The only entity is the caller identity: a person or nothing. `spec.md`
carries it, and R1 states its one representation.

**No `contracts/` folder.** The feature adds no endpoint, field or status code. It applies the
existing `401` to one more caller. The existing contract is amended in place (R6).

### Source code (repository root)

```text
src/Jellyfin.Plugin.NewReleases/Api/
└── ReleasesController.cs        # CallerId(): Guid.Empty -> null   ← the fix

tests/Jellyfin.Plugin.NewReleases.Tests/
├── Support/
│   ├── ControllerContextFactory.cs   # ForUser(Guid); ForCallerWithoutUser() = claim of Guid.Empty "N";
│   │                                 # UserManager throws ArgumentException for Guid.Empty
│   └── ControllerContextFactoryTests.cs  # NEW: pins both, so the double cannot drift from the host
├── Api/
│   └── ReleasesControllerTests.cs    # no-user caller -> 401 without throwing, for each of the five
│                                     #   per-user actions (SC-005)
│                                     # no-user caller -> Status answers 200 (FR-004)
│                                     # claim naming a missing user -> 401 (deleted-user race)
│                                     # the two no-claim tests re-pointed to ForCallerWithoutUser()
└── Acceptance/AcceptanceRig.cs       # unchanged: already calls ForUser with a real id

specs/001-track-new-releases/contracts/http-api.md   # 401 row reworded, added to Releases and Artists
CHANGELOG.md                                         # Unreleased -> Fixed
```

**Structure Decision**: no new source file, and one new test file. The new test file,
`Support/ControllerContextFactoryTests.cs`, sits beside the helper it tests. The fix sits in the one method that reads the identity. The
test helper keeps its home, so a future per-user controller builds its no-user caller through the
same method (FR-007).

**Order of work**:

1. Reshape the double: `ForCallerWithoutUser()`, the throwing `UserManager`, and `ForUser(Guid)`.
   Re-point the two no-claim tests. The suite goes red on exactly those tests, with
   `ArgumentException: Guid can't be empty`, the server's failure. That is the red for FR-001.
2. Add the no-user tests for Artists and the three decision actions. They fail the same way.
3. Fix `CallerId()`. Everything goes green.
4. Add the pins: Status with no user, and a claim naming a missing user. Both pass already; record
   them as test-after.
5. Mutant check: delete the `Guid.Empty` condition, and confirm the suite fails (SC-004).
6. Amend the contract and the changelog.

**Not touched**: `AdminController` and `UserViewController` never read the identity (R2). The
admin elevation rule is out of scope. `FR-005` and `SC-003` are held by the existing suite, which
builds every signed-in caller with a real id.

## Complexity Tracking

No violations. Nothing to justify.
