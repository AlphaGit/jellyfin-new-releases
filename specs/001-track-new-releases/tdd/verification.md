---
feature: 001-track-new-releases
verdict: PASS_WITH_GAPS
standard: .specify/extensions/tdd/templates/tdd-test-quality-rubric.md
verified_at: 0fa9999 + working tree (TDD remediation, Phase 7)
behaviors: 153
proven: 150
likely: 2
test_after: 0
no_test: 0
dropped: 1
high_smells: 0
criteria_total: 19
criteria_covered: 18
mutation_score: n/a
mutants_applied: 17
mutants_survived: 1
suite: 178 passed, 0 failed, 10s
---

# TDD Verification: Track New Releases

**Verdict: PASS_WITH_GAPS.** Both HIGH findings are cleared and every mutant that mattered now
dies, but the strength evidence is still a 17-mutant sample with no mutation tool and no
coverage instrument behind it.

This run supersedes the `FAIL` at `0fa9999`. That verdict rested on two deliberate mutants
surviving the whole suite inside behaviours marked `DONE`; both boundaries are now pinned and
both mutants die. Six behaviours were added, one test-helper defect was found and fixed, and one
remediation task was attempted and reverted.

## What changed since the FAIL

| Finding | Severity | Resolution |
| ------- | -------- | ---------- |
| 1 | HIGH | `U130` added: a runner-up exactly 5 points behind is ambiguous. `MusicBrainzSource.cs:56` `<= 5` → `< 5` now fails 1 test |
| 2 | HIGH | `U129` added: a top score of exactly 85 matches. `MusicBrainzSource.cs:51` `<` → `<=` now fails 1 test |
| 3 | MED | Cycle log now carries entries for cycles 118 and 132, stating plainly that **no red was recorded** and adding an after-the-fact mutant check for each |
| 4 | MED | `SC-005` asserted at the 500 ms the criterion states (measured 3 ms), not 2 000 ms |
| 5 | MED | `A20` added: with every source in cooldown, a run contacts nothing and the list still returns the stored releases |
| 6 | MED | `U131` added: exactly one missing track is `Incomplete`. `OwnershipMatcher.cs:33` `== 0` → `<= 1` now fails inside `OwnershipMatcherTests`, not only in `ArchiveTests` |
| 7 | MED | `MusicBrainzSourceTests`'s hand-rolled search-body builder replaced by `Support/SourceJson.MusicBrainz.Search` behind a `SearchReturns(...)` helper; `U77` moved onto it too |
| 8 | MED | **Not done.** Attempted and reverted — see "The one task left" below |
| 9 | MED | `FR-005b` now traced from `U120` and `A18` |
| 10 | LOW | Test-list heading renamed to `Storage/PluginDatabase.cs`, the file that exists |
| 11 | LOW | `U132` and `U133` added, so both contract-required fixtures are now exercised rather than deleted |
| 12 | LOW | `FixtureLoader`'s doc comment names a fixture that exists |
| 13 | LOW | Left alone on purpose: cycle 8 already proved that inequality assertion discriminates |

### A defect the audit missed

`Support/SourceHarness.cs` built **two** independent `TimeProviderStub`s: the `Clock` property
initialiser created one, and `CreateAsync` passed a different one to `TestDatabase`. Every
timestamp `SourceStateRepository` wrote — `cooldown_until`, `calls_day`, run start and end — ran
on a clock no test could reach, frozen at `Start`, while `Harness.Clock` drove the sources, the
HTTP policy client and the task.

The suite stayed green throughout, which is why nobody noticed: no existing test both advanced
the harness clock and asserted a repository timestamp. `A20` was the first to do so and failed
on it. The harness now has one clock and all 178 tests pass.

Nothing was silently wrong before, but any future test of a time-dependent source-state
behaviour would have been asserting against a frozen clock. This is the kind of finding a
12-mutant sample does not reach, and it is a fair argument that the sample is too small.

### The one task left

`T082` — drive the rate limiter from the injected `TimeProvider` so nothing asserts on wall
time — was implemented and then reverted whole. `System.Threading.RateLimiting` replenishes from
a private `Stopwatch`, so it was replaced with a clock-driven request spacer and
`TimeProviderStub` gained an opt-in auto-advance. Two things then went wrong:

- Every test that makes two calls to one source had to pump the clock, or hang. That is a dozen
  tests made harder to write and read.
- The retry-timing tests (`U61`, `U62`) began racing the auto-advance, because it has to resolve
  the pending delay off the `CreateTimer` stack — firing it inline deadlocks inside
  `Task.Delay`'s own machinery.

Rewriting the component that keeps this plugin polite to MusicBrainz and Deezer, and making the
suite harder to extend, is not a trade worth one MED finding. `src/` is untouched. The wall-clock
assertion at `Sources/SourceHttpClientTests.cs:206` stands as a recorded ceiling: it fails loudly
rather than hanging, and no other assertion depends on real time. Re-open it only alongside a
stub clock that resolves pending delays when nothing else is runnable.

## Test-first evidence

Two forms, both accepted by the extension's own playbook (`tdd-loop-playbook.md`, "Step 3"):

| Class | Count | Evidence |
| ----- | ----- | -------- |
| `PROVEN` | 98 | Cycle log records the single-test command and its assertion failure; the commit carries test and source together |
| `PROVEN` | 52 | Test was green on first run; a deliberate mutant made it fail, the code was restored, `git diff` verified empty |
| `LIKELY` | 2 | `U115`, `U124` — see below |
| `DROPPED` | 1 | `A14` (US2-AS6) — no v1 source requires terms acceptance (research R12) |

`U115` and `U124` were `TEST_AFTER` at `0fa9999` and are now `LIKELY`. **No red was recorded for
either at the time and none can be manufactured now** — you cannot observe a test failing before
code that already exists. What changed is that the gap is written down instead of missing, and
each test has been shown to discriminate: removing the guard it covers makes it fail, and the
code was restored exactly. That is the same evidence shape the 52 rows above rest on, with one
real difference — for those the mutant ran inside the cycle, before the commit, so it speaks to
ordering as well as to power; here it ran afterwards and speaks only to power. `LIKELY` is the
class for evidence that cannot corroborate the order, so that is where they sit.

Cycles 118 and 132 also stand as a warning: commits `a5ddd11` and `993d797` say "docs: record TDD
cycle for U115 / U124" and add no log entry at all. A commit message that claims evidence it does
not add is worse than a missing entry, because the next reader stops looking.

### What the change did to tests that already existed

Nothing was weakened, in either the original branch or this remediation.

- Removed or loosened `Assert.*` lines: **0**. The one bound that moved, `SC-005`, moved
  **tighter** — 2 000 ms → 500 ms.
- `Skip`, `[Ignore]`, or excluded tests: **0**.
- Coverage or mutation thresholds lowered: **0**.
- `9f5570a` (original branch) changed `SourceHarness.RequestedUrls` from `Uri.ToString()` to
  `AbsoluteUri`, which strengthens the URL assertion by keeping percent-escapes.

### `tasks.md` against the test list

Phase 7 holds 12 remediation tasks: 11 ticked, `T082` left unticked with the reason written into
the section. `T052` (manual smoke run) and `T053` (final CI gate) remain outstanding as before.
No task is ticked whose behaviour ids are not `DONE`.

## Findings

None open at HIGH or MED severity. One LOW stands, deliberately:

| # | Severity | Finding | Evidence |
| - | -------- | ------- | -------- |
| 13 | LOW | A normalization test asserts only that two titles differ, not the two normalized values | `Matching/TitleNormalizerTests.cs:66` |

Cycle 8 recorded a deliberate mutant for it (`Finish()` dropping a leading `the `) and the test
caught it, so a stronger form would add nothing.

## Mutation results

`mutation: null` in the profile — Stryker.NET is not installed — so this is deliberate mutants
only. 17 applied across both passes, each alone, each restored with `git diff` verified empty
before the next. The tree is clean and the suite green.

This is a **sample, not a score**: 17 of 152 completed behaviours were probed.

| Mutant | Behaviour | Survived | Judgment |
| ------ | --------- | -------- | -------- |
| `Migrations/001_initial.sql` — `edition` table removed | U19 | No | Caught at `DatabaseTests.cs:51` |
| `Api/ReleasesController.cs:188` — `CanSee(...)` → `true` | U116, U119, U121 | No | 3 failures; library access is real |
| `Api/ReleasesController.cs:68` — `Unauthorized()` → `Forbid()` | U115 | No | Reconstruction for cycle 118 |
| `Api/AdminController.cs:52` — running-state guard removed | U124 | No | Reconstruction for cycle 132 |
| `Storage/ReleaseRepository.cs:344` — purge also `DELETE FROM decision` | U42, U126, A13 | No | 4 failures across three levels |
| `Storage/SourceStateRepository.cs:84` — cooldown `>` → `>=` | U51 | No | Boundary pinned at the second |
| `Sources/SourceHttpClient.cs:49` — budget `<= 0` → `< 0` | U60 | No | Last unit of budget pinned |
| `Storage/ReleaseRepository.cs:194` — `@archived = 1 OR` removed | A19 | No | Caught by A19 alone, which is its purpose |
| `Matching/OwnershipMatcher.cs:33` — `missing.Length == 0` → `<= 1` | U98, U99, **U131** | No | **4 failures now, including the owning unit** — was acceptance-only |
| `Sources/MusicBrainzSource.cs:56` — ambiguity `<= 5` → `< 5` | U76, **U130** | No | **Was the finding-1 survivor** |
| `Sources/MusicBrainzSource.cs:51` — `< MinimumScore` → `<=` | U75, **U129** | No | **Was the finding-2 survivor** |
| `Sources/MusicBrainzSource.cs:89` — `next < total ? next : null` → `next` | U81, U132 | No | 2 failures |
| `Sources/DeezerSource.cs:102` — edition track titles left un-normalized | U91, U133 | No | 2 failures |
| `ScheduledTasks/RefreshNewReleasesTask.cs:108` — availability check inverted | A20 | No | Caught; the skip is real |
| `Storage/ReleaseRepository.cs:253` — `Upcoming` `> 0` → `>= 0` | U38, A7 | No | "Not a row dated today" pinned |
| `Storage/ReleaseRepository.cs:143` — prune `< @runId` → `<= @runId` | U31, U105 | No | 19 failures |
| `Storage/ReleaseRepository.cs:197` — `ORDER BY` drops `date_sort IS NULL` | U34, A2 | Yes | **Equivalent.** SQLite already sorts NULLs last on `DESC`; the explicit term is defensive, not load-bearing |

One survivor, judged equivalent. No survivor inside a `DONE` behaviour.

## Traceability

All 19 acceptance scenarios and every `FR-` except `FR-019` carry a `traces` entry, and every
named test resolves: a script matched all 152 `DONE` rows to a real `[Fact]`/`[Theory]` method in
the file the row names. **0** dangling `traces` values. Of the 153 test methods in the suite (178
cases, counting theory rows), 152 trace to a behaviour and the one that does not is
`PluginSanityTests.Plugin_Guid_IsStable`, which predates the feature.

Untested criteria, with the reason each is or is not acceptable:

- `FR-019`, `SC-001`, `SC-008` — keyboard operation and navigation depth of the web pages. No
  JavaScript runner in the stack profile; out of scope, checked by hand per `quickstart.md`.
- `SC-002`, `SC-003`, `SC-006` — measured on a real library after release. Written down.
- `SC-004` — pinned through `INV-1` (`U113`, `A6`).
- `SC-005` — `U43`, now at the criterion's own 500 ms.
- `SC-007` — `A20`, added by this remediation. **One question is left for a human**: after a run
  that reached no source, `lastRefreshedAt` reports *that* run, not the run whose data is on
  screen, so the page can say "refreshed just now" over hours-old data. `A20` pins the behaviour
  as built. Whether SC-007's "states its age" wants the last *reaching* run is a spec decision,
  not a test fix, and it is not made here.

Tests tracing to nothing: none, other than the pre-existing sanity test.

## What was not audited

- **Mutation is a 17-mutant sample**, not a tool run. 135 of 152 behaviours were never probed for
  strength, and the `SourceHarness` two-clock defect is direct evidence that a sample this size
  misses real problems.
- **Coverage was not measured at all.** `coverage: null`; `--collect:"XPlat Code Coverage"` fails
  for want of `coverlet.collector`. Nothing here says which branches never executed.
- **The web pages were not audited.** `Web/user-view.html` and `Web/admin.html` (362 lines changed
  in `7854e92`) have no test and no runner.
- **`PluginServiceRegistrator.cs` was not audited.** Changed in `7854e92` with no test.
- **No test runs against a real Jellyfin server.** `acceptance: null`: `ILibraryManager`,
  `IUserManager`, `ITaskManager`, `IApplicationPaths` and `IHttpClientFactory` are substituted in
  every test, acceptance included. Routing, `[Authorize]` enforcement and Plugin Pages
  registration are outside every assertion here.
- **Performance and load** beyond `U43`'s single 500-row timing.
- **Suite flakiness was not characterised.** Wall-clock waits remain in `U67` and in the
  `RunAdvancingAsync` drivers; they were not stress-tested on a loaded machine.
