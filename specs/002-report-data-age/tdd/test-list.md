---
feature: 002-report-data-age
loop: outside-in
profile: .specify/memory/tdd-profile.md
spec_criteria: 10
planned_at: 0fa9999
updated_at: 0fa9999
suite_baseline: green
---

# Test List: Report the age of the data, not the age of the run

## Trace id key

`spec.md` numbers acceptance scenarios per user story without ids. This list uses:

- `US<n>-AS<m>`: acceptance scenario *m* of user story *n* (10 in total: US1 1–5, US2 1–2,
  US3 1–3).
- `FR-nnn`, `SC-nnn`: functional requirements and success criteria as written in `spec.md`.
- `EC-<n>`: the *n*-th bullet of "Edge Cases" in `spec.md`.
- `R<n>`: decision in `research.md` when a behaviour pins a recorded design ceiling.

## Two ecosystems

The server behaviours run under xunit, as `001` established. The page behaviours run under Node's
built-in runner, which **this feature introduces** (`research.md` R8). The stack profile currently
describes only the `dotnet` ecosystem; task `T033` adds the `node` entry. Until it does, the
commands at the bottom of this file are the authority for the page side, taken from `plan.md` and
`research.md` rather than guessed.

## Outer loop: acceptance behaviors

One per acceptance scenario. No host-level acceptance runner exists (`acceptance: null` in the
profile), so each server-side behaviour is an integration test composing the real entry points
with substituted Jellyfin services, as `001`'s `AcceptanceRig` already does. For US3 the real
entry point is the page's own exported logic, reached through the sandbox loader.

| id  | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| A1 | A catalogue fetch completes at 03:00; a later refresh at 15:00 completes none → at 15:05 the list response reports the 03:00 instant, not the 15:00 run | US1-AS1 | example | DONE | `Acceptance/ConfigureAndRunTests.cs::A20_WithEverySourceInCooldown_TheListStillShowsTheStoredDataAndItsAge` |
| A2 | Data older than one refresh interval → the page produces a staleness sentence rather than nothing | US1-AS2 | example | PENDING | |
| A3 | Data confirmed within one refresh interval → the page produces no staleness sentence | US1-AS3 | example | PENDING | |
| A4 | No enabled source has ever completed a fetch → the list response reports no stored releases and no instant | US1-AS4 | example | PENDING | |
| A5 | After a purge → the list response reports no stored releases and no instant, though completed runs are still on record | US1-AS5 | example | PENDING | |
| A6 | One source cooling down while the other completes a fetch → the reported instant is that completed fetch and is within one refresh interval | US2-AS1 | example | PENDING | |
| A7 | The administrator disables every source → refreshes keep running and the reported instant stops moving | US2-AS2 | example | PENDING | |
| A8 | Every band and boundary of the unit ladder is asserted, and moving any boundary by one unit makes a test fail | US3-AS1, SC-007 | example | PENDING | see note below |
| A9 | A release title containing HTML markup is escaped rather than rendered as markup | US3-AS2, SC-008 | characterization | PENDING | |
| A10 | The whole suite, page side included, runs with no network and no installation step | US3-AS3, SC-009, FR-014 | example | PENDING | see note below |

**A8 and A10 are not conventional tests.** A8's first half is `U21`–`U28` below; its second half —
"moving any boundary makes a test fail" — is the deliberate-mutant check from the loop playbook,
recorded in `cycle-log.md`, not an assertion. A10 is verified by running the suite with the
network down and no install step, recorded in the cycle log and re-checked by CI (`T034`); there
is nothing for a test to assert about itself.

## Inner loop: unit behaviors

Grouped by the component from `plan.md` that owns them.

### `src/Jellyfin.Plugin.NewReleases/Storage/ArtistRepository.cs`

| id  | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U1 | Returns the newest `last_complete_at` across all artists at the enabled sources | FR-002 | example | DONE | `Storage/ArtistRepositoryTests.cs::GetReleasesLastCheckedAtAsync_IsTheNewestCompletedFetchAcrossArtists` |
| U2 | A completed fetch at a source outside the enabled set is ignored, even when it is the newest | FR-002, EC-disabled | example | DONE | `Storage/ArtistRepositoryTests.cs::GetReleasesLastCheckedAtAsync_IgnoresASourceThatIsNotEnabled_EvenWhenItIsTheNewest` |
| U3 | An empty enabled set returns no instant | FR-002, EC-all-disabled | example | DONE | `Storage/ArtistRepositoryTests.cs::GetReleasesLastCheckedAtAsync_WithNoEnabledSource_IsNothing` |
| U4 | No artist-and-source pair has ever completed a fetch → returns no instant | FR-008, EC-no-fetch | example | DONE | `Storage/ArtistRepositoryTests.cs::GetReleasesLastCheckedAtAsync_WithNoFetchEverCompleted_IsNothing` |
| U5 | A `Partial` outcome leaves the value where the last `Complete` left it | FR-003 | example | DONE | `Storage/ArtistRepositoryTests.cs::GetReleasesLastCheckedAtAsync_AnOutcomeOtherThanComplete_LeavesTheValueWhereTheLastCompleteLeftIt` |
| U6 | A `Failed` outcome leaves the value where the last `Complete` left it | FR-003 | example | DONE | `Storage/ArtistRepositoryTests.cs::GetReleasesLastCheckedAtAsync_AnOutcomeOtherThanComplete_LeavesTheValueWhereTheLastCompleteLeftIt` |

### `src/Jellyfin.Plugin.NewReleases/Storage/ReleaseRepository.cs`

| id  | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U7 | `HasAnyAsync` is false against an empty database | FR-008 | example | DONE | `Storage/ReleaseRepositoryTests.cs::HasAnyAsync_FollowsWhetherReleaseRowsExist` |
| U8 | `HasAnyAsync` is true with one release row | FR-008 | example | DONE | `Storage/ReleaseRepositoryTests.cs::HasAnyAsync_FollowsWhetherReleaseRowsExist` |
| U9 | `HasAnyAsync` is false again after `PurgeAsync` | FR-008, US1-AS5 | example | DONE | `Storage/ReleaseRepositoryTests.cs::HasAnyAsync_FollowsWhetherReleaseRowsExist` |

### `src/Jellyfin.Plugin.NewReleases/Api/ReleasesController.cs`

| id  | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U10 | The list response reports the newest completed fetch, not the last completed run's end | FR-001, FR-002 | example | DONE | `Api/ReleasesControllerTests.cs::GetReleases_ReportsTheNewestCompletedFetch_RefreshIntervalFollowsTheTrigger` |
| U11 | A refresh that completed no fetch leaves the reported instant unchanged | FR-003, FR-004 | example | PENDING | rewrites `001`'s `A20` |
| U12 | The list response and the status response report the same instant for one caller at one moment | FR-011 | example | PENDING | |
| U13 | The stored-releases flag follows whether release rows exist, not whether a run has completed | FR-008 | example | PENDING | |
| U14 | Release rows with no completed fetch anywhere → the releases are listed and no instant is reported | FR-008, EC-no-fetch | example | PENDING | |
| U15 | Disabling the source whose fetch was newest makes the reported instant fall back to the newest remaining enabled source | FR-002, US2-AS2 | example | PENDING | |

### `src/Jellyfin.Plugin.NewReleases/Api/AdminController.cs`

| id  | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U16 | Reports the last run, including a run that reached no source | FR-009 | example | PENDING | |
| U17 | Reports the same instant the user page reports | FR-009, FR-011 | example | PENDING | |
| U18 | After a run that completed no fetch, the reported run end and the reported instant differ | FR-009, SC-005 | example | PENDING | |

### `src/Jellyfin.Plugin.NewReleases/Web/user-view.html` — `stalenessText`

New logic, extracted so it can be computed without a page (`FR-013`, `research.md` R9). Every
boundary below is tested on both sides: a threshold with one test pins nothing.

| id  | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U19 | No instant → no sentence | FR-008 | example | PENDING | |
| U20 | An instant later than the current time counts as an age of zero, so no sentence | FR-010, EC-clock | example | PENDING | |
| U21 | Age exactly one refresh interval → no sentence | FR-006 | example | PENDING | |
| U22 | Age one second past one refresh interval → a sentence | FR-006 | example | PENDING | |
| U23 | 47 hours renders in hours | FR-012 | example | PENDING | |
| U24 | 48 hours renders in days, not hours | FR-012, SC-007 | example | PENDING | |
| U25 | 13 days renders in days; 14 days renders in weeks | FR-012, SC-007 | example | PENDING | |
| U26 | 60 days renders in weeks; 61 days renders in months | FR-012, SC-007 | example | PENDING | |
| U27 | 364 days renders in months; 365 days renders as `over a year ago` | FR-012, SC-007 | example | PENDING | |
| U28 | The sentence reads `Releases last checked <relative time> ago.` and contains none of "refresh", "run", "scan" or "update" | FR-007, SC-004 | example | PENDING | |

### `src/Jellyfin.Plugin.NewReleases/Web/user-view.html` — existing helpers

Untouched by this feature and untested today. These are characterization behaviors: they capture
what the code already does, are green against unchanged code, and exist so the Phase 5 rename and
any later change cannot break them silently.

| id  | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U29 | `esc` escapes `&`, `<`, `>`, `"` and `'` in text that came from a release source | SC-008, US3-AS2 | characterization | PENDING | |
| U30 | `esc` turns null and undefined into the empty string | SC-008 | characterization | PENDING | |
| U31 | `groupOf` returns `Upcoming` for an upcoming release, `Undated` for one with no date, and the year otherwise | US3-AS2 | characterization | PENDING | pins `001`'s US1-AS2 grouping |
| U32 | `artistLink` builds the Jellyfin artist deep link from the artist id and the server id | US3-AS2 | characterization | PENDING | |

### `src/Jellyfin.Plugin.NewReleases/Web/admin.html`

| id  | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U33 | `healthText` renders each source health value the administrator page can receive | US3-AS2 | characterization | PENDING | pins `001`'s FR-012 wording |
| U34 | Both pages expose their pure helpers on one named object, so the sandbox can reach them without a browser | FR-013, FR-016 | example | PENDING | |

## Invariants and edge cases still to place

None. Every edge case in `spec.md` is placed: the disabled-source jump on `U2` and `U15`, every
source disabled on `U3` and `A7`, the clock correction on `U20`, data with no completed fetch on
`U4` and `U14`, and the purge on `U9` and `A5`.

## Out of scope

- **The page's rendering functions**: `row`, `render`, `refreshStatus`, `read`, `fill` and
  `query` all read or write the page and need a simulated browser this feature does not
  introduce (`spec.md` Assumptions, `research.md` R8). They stay manual.
- **`001`'s `FR-019` and `SC-008`** — keyboard operation and screen-reader announcement. A real
  browser and assistive technology, not a runner. Explicitly *not* unblocked by US3.
- **Where the staleness line sits on the page.** `US1-AS2` says the age must be readable "before
  the list"; `A2` covers that a sentence is produced, not where it is placed. Placement is checked
  by hand in `quickstart.md`.
- **The sandbox loader itself** (`tests/web/load-page.js`): exercised by every page test; a test
  for the test helper would pin nothing the others do not.
- **Rotation lag.** The reported instant is server-wide by decision, so an individual artist's data
  can be older than it says (`spec.md` Assumptions). No test, because it is the accepted design.
- **Renaming the response fields** (`T022`): a mechanical refactor on a green suite, proven by the
  same tests passing before and after, not by a behaviour of its own.

## Verification commands

Server side, copied verbatim from `.specify/memory/tdd-profile.md`:

- Single test: `dotnet test --configuration Release --filter "FullyQualifiedName~{name}" -- RunConfiguration.TreatNoTestsAsError=true`
- Full suite: `dotnet test --configuration Release`
- Coverage: not available (`coverage: null`)
- Mutation: not available (`mutation: null`); the loop uses deliberate mutants

Page side, from `plan.md` and `research.md` R8. **Not yet in the profile — `T033` adds them:**

- Single test: `node --test --test-name-pattern "<name>" tests/web/`
- Full suite: `node --test tests/web/`

`{name}` is `Class.Method` for xunit. In a shell that did not source `~/.zshenv`, prefix the
dotnet commands with `PATH=/opt/homebrew/opt/dotnet@9/bin:$PATH DOTNET_ROOT=/opt/homebrew/opt/dotnet@9/libexec`.
