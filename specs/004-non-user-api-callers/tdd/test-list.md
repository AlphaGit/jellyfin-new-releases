---
feature: 004-non-user-api-callers
loop: outside-in
profile: .specify/memory/tdd-profile.md
spec_criteria: 12
planned_at: 463373e
updated_at: 3c927c7
suite_baseline: green
---

# Test List: Answer a caller that has no user

`spec_criteria` counts the seven acceptance scenarios (`US1-AS1`–`AS4`, `US2-AS1`–`AS3`) and the
five success criteria (`SC-001`–`SC-005`). Four success criteria restate scenarios and close with
them: `SC-001` and `SC-002` with `A1`, `A2` and `A8`; `SC-003` with `A4`; `SC-004` with `A6`.
`SC-005` has no scenario and gets its own outer behaviour, `A8`.

## A note on the acceptance level available here

`.specify/memory/tdd-profile.md` records `acceptance: null`. The feature's real entry point is the
controller action, which is what the host invokes after authentication. Constitution II names the
controller as a real entry point. The outer behaviours therefore run against `ReleasesController`,
with the request context built the way the host builds it (research R1). Nothing beneath the
controller is a separate unit: the fix is one condition in a private method. So the inner loop
holds the **test double**, which this feature specifies as a subject of its own (`FR-006`), plus
one edge case.

"No error in the server log" (`FR-002`) is observed as **the action returns and does not throw**.
The logged error came from the host's exception middleware, which logs only what escapes the action
(research R5).

`A5`–`A7` are behaviours **of the suite**. `A6` is evidenced by a recorded deliberate mutant and
`A7` by a recorded search, not by a test file of their own. `006`'s `A5` and `A6` set the precedent.

## Outer loop: acceptance behaviors

| id | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| A1 | A caller with no user asking for the release list gets `401`, and the action returns without throwing | US1-AS1, SC-001, SC-002, FR-001, FR-002, FR-003 | example | DONE | `Api/ReleasesControllerTests.cs::GetReleases_ForACallerWithoutAUser_Is401` |
| A2 | A caller with no user asking for the artist list gets `401`, and the action returns without throwing | US1-AS2, SC-001, SC-002, FR-001, FR-002 | example | DONE | `Api/ReleasesControllerTests.cs::GetArtists_ForACallerWithoutAUser_Is401` |
| A3 | A caller with no user asking for the status gets the status, as today | US1-AS3, FR-004 | example | DONE | `Api/ReleasesControllerTests.cs::GetStatus_ForACallerWithoutAUser_Answers` |
| A4 | A caller signed in as a person gets exactly today's answers from every per-user endpoint, filtered to their libraries | US1-AS4, SC-003, FR-005 | example | DONE | `Api/ReleasesControllerTests.cs::GetReleases_FollowsTheCallersLibraryAccess`, `::GetArtists_ReturnsOnlyArtistsInLibrariesTheCallerMayAccess`, `::Decisions_IgnoreAndHaveItStoreTheCallerAndClock_RestoreDeletes_EachReturns204`, `::Decisions_UnknownReleaseIs404_ReleaseOutsideTheCallersLibrariesIs403WithNothingWritten`, `Acceptance/BrowseReleasesTests.cs` (all) |
| A5 | The request context the suite builds for a caller with no user agrees with the one the host builds | US2-AS1, FR-006 | example | DONE | closed by `U1` and `U2` |
| A6 | Removing the plugin's handling of an empty user identity fails the suite with the host's `ArgumentException` | US2-AS2, SC-004, FR-006 | example | DONE | deliberate mutant, cycle 9 of `tdd/cycle-log.md` |
| A7 | The suite has one way to build a caller with no user, and no helper builds a principal without the user-id claim | US2-AS3, FR-007 | example | DONE | `Support/ControllerContextFactoryTests.cs::TheFactory_HasOneBuilderForEachKindOfCaller` (test-after, cycle 13); search of cycle 8 |
| A8 | A caller with no user asking to ignore, mark as owned, or restore a release gets `401` from each, and none throws | SC-005, FR-001, FR-002, FR-008 | example | DONE | `Api/ReleasesControllerTests.cs::EveryDecision_ForACallerWithoutAUser_Is401` |

`A4` is already `DONE`: those tests build every signed-in caller with a real id and do not change.
It stays `DONE` only while they pass unedited, which `tasks.md` T012 confirms at the end.

`A1` and `A2` already have a test each that asserts `401`, but against a principal with no
claim, which the host never sends. They are re-pointed at the host's shape (research R4) and go red
then. That red is the evidence. The artist assertion moves out of
`GetArtists_ReturnsOnlyArtistsInLibrariesTheCallerMayAccess` into its own test, so each endpoint has
one test that names it (`SC-005`).

`A3` passes on its first run: `GetStatusAsync` does not read the user. It is a pin of `FR-004`,
recorded as test-after with a deliberate mutant that turns it red.

## Inner loop: unit behaviors

### `tests/Jellyfin.Plugin.NewReleases.Tests/Support/ControllerContextFactory.cs`: the test double

Hosted by a new `Support/ControllerContextFactoryTests.cs`, beside its subject.

| id | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U1 | A caller with no user carries exactly one `Jellyfin-UserId` claim, whose value is `Guid.Empty` in `N` format | US2-AS1, FR-006, FR-007 | example | DONE | `Support/ControllerContextFactoryTests.cs::ACallerWithoutAUser_CarriesTheEmptyUserIdTheHostSends` |
| U2 | The user manager double throws `ArgumentException` for `Guid.Empty`, as the host's `GetUserById` does | US2-AS1, US2-AS2, FR-006 | example | DONE | `Support/ControllerContextFactoryTests.cs::TheUserManager_RejectsAnEmptyId_AsTheHostDoes` |

`U1` is what stops the double drifting back. Without it, a `ForCallerWithoutUser()` that built no
claim at all would keep `A1`, `A2` and `A8` green: no claim also means no user. `U2` is what makes
`A6` possible. With a double that returns null for `Guid.Empty`, the unguarded action still answers
`401`.

The other side of `U2`'s boundary, a non-empty unknown id returning null, is pinned through the
controller by `U3`. A double that threw for it would fail `U3`.

### `src/Jellyfin.Plugin.NewReleases/Api/ReleasesController.cs`

| id | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U3 | A caller whose non-empty user id names no user the host knows gets `401` from the release list, and the action returns without throwing | spec Edge Cases (deleted user, session 2026-10-04), FR-001 | example | DONE | `Api/ReleasesControllerTests.cs::GetReleases_ForAUserDeletedMidRequest_Is401` |

`U3` passes on its first run: `AccessOf` already returns null for an unknown user. It is a pin,
recorded as test-after with a deliberate mutant (`AccessOf` granting full access to an unknown user)
that turns it red.

The boundary of the fix itself is `Guid.Empty` against any other id. `A1` holds the empty side;
`A4` holds a known non-empty id; `U3` holds an unknown non-empty id.

## Invariants and edge cases still to place

None.

## Out of scope

- `AdminController`'s four actions: they never read the identity, and their elevation rule is
  unchanged and out of scope (spec Out of Scope; research R2). No test.
- `UserViewController`: serves a static fragment and never reads the identity (research R2).
- A claim that does not parse as a GUID: the host always writes one in `N` format, so this is not
  a situation the plugin meets. `CallerId()` already answers it with null.
- A principal with no `Jellyfin-UserId` claim at all: the host never sends it behind `[Authorize]`
  (research R1). `A7` removes the helper that built it. FR-003 was narrowed to the empty identity
  in spec session 2026-10-07, after the TDD audit's finding 2 (mutant M8).
- The `Jellyfin-IsApiKey` claim: the plugin does not read it, and must not (research R3, R4).
- Verification on a running server: spec Assumptions. `quickstart.md` step 5 is the optional check.

## Verification commands

Copied verbatim from `.specify/memory/tdd-profile.md` at planning time, so this file is readable on
its own:

- Single test: `dotnet test --configuration Release --filter "FullyQualifiedName~{name}" -- RunConfiguration.TreatNoTestsAsError=true`
- Full suite: `dotnet test --configuration Release`
- Web suite (unchanged by this feature, run at the end): `node --test "tests/web/*.test.js"`
- Coverage: none recorded (`coverage: null`)
- Mutation: none recorded (`mutation: null`). Deliberate-mutant spot checks instead, restored from a
  file copy verified with `cmp -s`
