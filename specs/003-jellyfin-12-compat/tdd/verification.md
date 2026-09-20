---
feature: 003-jellyfin-12-compat
verdict: FAIL
verified_at: 53bbf86
standard: .specify/extensions/tdd/templates/tdd-test-quality-rubric.md
profile: .specify/memory/tdd-profile.md
behaviors: 45
proven: 26
test_after: 6
baseline: 12
dropped: 1
high_findings: 3
criteria_total: 10
criteria_covered: 10
criteria_with_entry_point_test: 6
suite: 238 passed, 0 failed, 10 s (xunit) + 33 passed, 0 failed (node)
mutation_tool: none (profile records mutation: null)
deliberate_mutants: 10 applied, 9 killed, 1 survived
independent: false
audits: 3
---

# TDD Verification: Run on Jellyfin 12

**Verdict: FAIL.** The two repository-manifest checks still prove nothing: mutating their shared
helper to reject every entry leaves all 13 tests in the class green, because no test ever calls
it with a valid one.

This is the **third** audit. The counts asked for are refreshed — `test_after` falls from 21 to 6
after the classification refresh — but refreshing them surfaced three `HIGH` smells that the
second audit's remediation introduced or left behind. The first audit's report is preserved
below.

## Findings

| # | Sev | Finding | Evidence |
| --- | --- | --- | --- |
| A | HIGH | **The manifest helpers are never exercised against a valid entry.** `AssertInstallable` and `AssertSourceUrlNamesItsOwnVersion` are called positively only at `:81` and `:118`, both `Assert.All` over a `versions` list that is empty until the first tag. The nine negative theories accept a throw for *any* reason. **Proved**: adding `Assert.Fail(...)` at the top of `AssertInstallable` leaves the class at 13 passed, 0 failed. | `Packaging/RepositoryManifestTests.cs:81, 99, 118, 139-140, 143` |
| B | HIGH | **A conditional was reintroduced by the very task that removed one.** `T044` removed the early `return` from `Manifest_EverySourceUrlSharesOneSiteRoot_AndNamesItsOwnVersion` and replaced it with `if (versions.Count > 0)`. The site-root and version-naming rules still never run; the test's only live assertion is the count. | `Packaging/RepositoryManifestTests.cs:115` |
| C | HIGH | **The flaky-test regression guard cannot fail for the right reason.** `Assert.Null(observed)` is the only assertion; nothing asserts the eight workers ever opened a connection, so a run in which they were never scheduled passes identically. The cycle log already records that it passed *before* the fix; the class comment calling it "the regression test" overstates it. | `Storage/TestDatabaseIsolationTests.cs:51`, `tdd/cycle-log.md` cycle 33 |
| D | MED | **A negative assertion was loosened by the remediation.** `DoesNotContain("10.11", …)` now runs against the Requirements section only. Before `T047` it covered the whole README. A stale 10.11 claim in the intro, "What it does" or "Install" now passes. | `Packaging/DocumentationTests.cs:38` |
| E | MED | **The new regression guard is itself a plausible source of flakiness.** Eight unbounded thread-pool tasks hammering SQLite while 300 databases are created, migrated and deleted — and the class is **not** in `ProcessGlobalStateCollection`, so it runs beside every other collection. | `Storage/TestDatabaseIsolationTests.cs:23-46`, `:11` |
| F | MED | **The csproj-path assertion does not bind to the guard it checks.** `package.yml:73`'s `git checkout --` line satisfies `Assert.Contains("…csproj", Steps)` on its own, so the path is not tied to the `grep -q`. | `Packaging/ReleaseWorkflowTests.cs:59`; `.github/workflows/package.yml:72-73` |
| G | MED | Two manifest tests now share one live assertion (`AssertPublishedVersionCount`); one manifest edit fails both with the same message. `Manifest_MayListNoVersionsAtAll` asserts only that a JSON array is an array. | `RepositoryManifestTests.cs:63-66, 80, 113` |
| H | MED | `A1` became a duplicate: after `T046` it asserts the same GUID literal as `U1` and the same page name as `U2`. Nothing asserts the composite fact it could carry — that the constructor sets `Plugin.Instance` to the same object. | `PluginSanityTests.cs:28-34` |
| I | MED | Nothing ties `TargetVersions.Framework` to the **csproj's** `<TargetFramework>`. `build.yaml` and the workflow grep string are both checked against the test constant, so a csproj/`build.yaml` divergence is invisible to the suite. | `Support/TargetVersions.cs`, `BuildManifestTests.cs:31` |
| J | LOW | The new wiring test hand-copies `BuildContainerAsTheHostWould` rather than sharing it. | `Integration/PluginPagesRegistrationTests.cs:46-50` vs `PluginServiceRegistratorTests.cs:31-42` |
| K | LOW | `AfterAFailedRegistration_…` arranges the *absent* integration (`() => []`), not the failing one. The name describes a case it does not set up. | `Integration/PluginPagesRegistrationTests.cs:187` |
| L | LOW | `build.yaml`'s `artifacts` list is pinned in file order; a reorder JPRM does not care about fails the test. | `Packaging/BuildManifestTests.cs:51-60` |

## What the remediation did fix

Verified independently, not taken on trust. Five of the eight claimed fixes **hold**: the
tautological `Assert.Empty` on a throwing fake, the re-implemented GUID expectation, the
workflow's `grep -q` mechanism over a comment-stripped view, the `DisableParallelization`
collection, the `ClearPool` scoping, and the DI-gateway wiring test. Two are **partial** (A, B
above, and D). One caveat on the wiring test: it proves the default load context only, not the
separate `AssemblyLoadContext` a real Plugin Pages install uses.

## Test-first evidence, refreshed

The classification refresh at `53bbf86` recast twelve behaviours from `example`/`DONE` to
`characterization`/`BASELINE`, on evidence that this feature's diff never touched the code they
pin. That is what moves `test_after` from 21 to 6.

| Class | Count | Which |
| --- | --- | --- |
| `PROVEN` | 26 | the Plugin Pages code, the manifest, the workflow, the docs — red recorded and history corroborating |
| `BASELINE` | 12 | `A1`-`A5`, `U1`, `U2`, `U5`, `U6`, `U19`, `U20`, `U32` — pin untouched code |
| `TEST_AFTER` | 6 | `U17`, `U18` (values the feature changed, tested three commits later); `U11`, `U14`, `U15`, `U16` (new code satisfied by an earlier cycle, mutant-verified) |
| `DROPPED` | 1 | `U4` |

**No pre-existing test was weakened**: the whole-feature diff touches none. The remediation
removed 38 assertion lines, every one a paired replacement of equal or greater strength — except
finding D, which is a genuine loosening.

**`tasks.md`**: all 53 ticked; five now name `BASELINE` behaviours (`T007`, `T012`, `T013`,
`T025`, `T039`), which the stack profile records as a known conflict in the extension, with
precedent from `002`.

## Test strength

No mutation tool; ten deliberate mutants, each restored from a file copy and verified with
`cmp -s`.

| Mutant | Result |
| --- | --- |
| M1 gateway `catch` returns true | killed |
| M2 `StopAsync` drops `TryRemovePage` | killed |
| M3 wrong page `Url` | killed |
| M4 registrator hands the gateway `() => []` | killed (was the first audit's survivor) |
| M5 log-once guard never latches | killed |
| M6 `build.yaml` guid altered | killed |
| M7 `repo/manifest.json` guid altered | killed |
| M8 README Jellyfin version | killed |
| M9 README Plugin Pages minimum | killed |
| **M10 `AssertInstallable` always rejects** | **SURVIVED** — finding A |

Coverage remains unavailable (`Unable to find a datacollector with friendly name 'XPlat Code Coverage'`).

## Traceability

All 10 acceptance criteria covered; 36 of 36 referenced tests resolve; no behaviour without a
named test. Four criteria (`US2-AS1`-`AS4`) rest on declared proxies over file text rather than a
real entry point — a recorded planning decision, not a lapse.

## What was not audited

- **Independence**: I wrote both the tests and the remediation. The smell pass was delegated to a
  fresh context for the second time, and every line it cited was re-opened and checked here.
- Mutation is a sample of ten, still none inside `001`/`002` code.
- Coverage unavailable; performance not assessed beyond the 10 s suite.
- The page side (`tests/web/`, 33 tests) is untouched by this feature and was not re-audited.
- A real Jellyfin 12 server — out of scope by `spec.md`, still the largest unknown.

---

## First audit (verdict FAIL, at `70b72db`)

Kept for the record. Every `HIGH` below is now closed.

**This audit is not independent.** The same session wrote these tests, planned the list and ran
the loop. The smell pass in Phase 3 was delegated to a fresh-context subagent for that reason,
and every line it cited was re-opened and checked here before entering this report — one of its
findings had mis-stated evidence and is corrected below. Treat the classifications as
self-assessment that failed itself, not as an outside opinion.

## Findings

Ordered by severity. `HIGH` is a correctness or evidence defect; `MED` weakens the safety net;
`LOW` is readability.

| # | Sev | Finding | Evidence |
| --- | --- | --- | --- |
| 1 | HIGH | **The production assembly source is never exercised.** Replacing `AssemblyLoadContext.All.SelectMany(...)` with `() => []` in the registrator — so the gateway can never find Plugin Pages on a real server — leaves all 236 tests green. Every Plugin Pages test injects its own stand-in source; `U7` asserts only that the hosted service *type* is registered. A survivor inside behaviours marked `DONE` (`U7`, and `A6` which rests on it). | `src/Jellyfin.Plugin.NewReleases/PluginServiceRegistrator.cs:49-50`; mutant M4 below |
| 2 | HIGH | **`U25` asserts nothing in the committed state.** `if (versions.Count == 0) { return; }` and `versions` is empty until the first tag, so the method returns before any assertion. | `Packaging/RepositoryManifestTests.cs::Manifest_EverySourceUrlSharesOneSiteRoot_AndNamesItsOwnVersion` |
| 3 | HIGH | **`U23` asserts nothing in the committed state.** `foreach (var version in Versions())` over an empty list; `AssertInstallable` never runs. | `Packaging/RepositoryManifestTests.cs::Manifest_EveryVersionCarriesItsDownloadChecksumTimestampAndJellyfin12` |
| 4 | HIGH | **Tautological assertion.** `Assert.Empty(FakePluginPages.Registered)` after the test configured `RegisterThrows`. The fake throws *instead of* recording, so this asserts the double did what the test told it to; it cannot fail for any implementation of the subject. | `Integration/PluginPagesRegistrationTests.cs::WhenRegisterPageThrows_StartingDoesNotThrow_AndLogsOnce`, the `Assert.Empty` line |
| 5 | HIGH | **Re-implemented expectation.** `Assert.Equal(new Guid(Plugin.PluginGuid), plugin.Id)` where `Plugin.Id => new Guid(PluginGuid)` — the same expression on both sides. `Plugin_Guid_IsStable` already does this correctly with the literal. | `PluginSanityTests.cs::Plugin_ConstructedWithHostServices_...`; `src/.../Plugin.cs` `Id` |
| 6 | HIGH | **Vacuous assertion** where the specific value is known and asserted two tests away: `Assert.NotEmpty(plugin.GetPages())`. | same test as finding 5 |
| 7 | HIGH | **Prose satisfies a documentation assertion.** `Assert.Contains("Dashboard", Readme)` passes on any of the four pre-existing "Dashboard" mentions, none of them the Install section the test claims to check. | `Packaging/DocumentationTests.cs::Readme_GivesTheRepositoryUrlAndWhereToAddIt` |
| 8 | HIGH | **The guard's mechanism is not asserted.** The test named `...FailsTheRunIfPackagingDidNotRestoreTheTargetFramework` asserts the workflow *text* contains the framework element, not that a `grep -q` runs and fails the step. It also reads `Workflow` rather than the comment-stripped `Steps`, so a comment would satisfy it. | `Packaging/ReleaseWorkflowTests.cs::ReleaseWorkflow_FailsTheRunIfPackagingDidNotRestoreTheTargetFramework` |
| 9 | HIGH | **The test list names a test that does not exist.** `Manifest_EverySourceUrlIsUnderTheSiteRoot_AndNamesItsOwnVersion` was renamed to `...SharesOneSiteRoot...` in the de-hardcoding and the list was not updated. 34 of 35 references resolve. | `tdd/test-list.md`, the `U25` row |
| 10 | MED | `Assert.Contains("version4", Workflow)` does not check that the *zip path* interpolates it. **Corrected evidence:** the subagent reported `version4` appearing only in a comment; it does not — `grep -n` finds it at `package.yml:51,52,54,80`, all script lines. The assertion is weak, not defeated. | `ReleaseWorkflowTests.cs::ReleaseWorkflow_NamesThePackageByTheFourPartVersionJprmWrites` |
| 11 | MED | **Four tests set the process-global `Plugin.Instance` with no xunit collection**, while `RefreshNewReleasesTask`, `SourceHttpClient`, `AdminController` and `ReleasesController` all read it. The stack profile requires a collection "when a second such test appears"; there are now four. | `PluginSanityTests.cs`, all four `new Plugin(...)` sites |
| 12 | MED | **Latent cross-collection leak.** `BuildContainerAsTheHostWould` wires the *production* gateway, whose `AssemblyLoadContext.All` scan can reach the test assembly's `PluginInterface` stand-in. Nothing starts that hosted service today; the day one does, it corrupts the static fake mid-run, and this class is not in the `PluginPagesRegistrationTests` collection. | `PluginServiceRegistratorTests.cs`, the container factory |
| 13 | MED | `Assert.ThrowsAny<Exception>` in the `U24`/`U26` theories passes on a `KeyNotFoundException` from `GetProperty`, not on the rule being enforced. | `RepositoryManifestTests.cs`, both theory bodies |
| 14 | MED | **The slug rule is re-derived, not pinned.** `Slug` recomputes JPRM's package-name rule from `build.yaml`. If JPRM's real rule differs, the test agrees with itself while the published URL is wrong. | `RepositoryManifestTests.cs`, the `Slug` field |
| 15 | MED | `Assert.Single(logger.Entries)` after Start-then-Stop passes if Start logged nothing and Stop logged once — the exact inversion `U15`'s name forbids. | `PluginPagesRegistrationTests.cs::AfterAFailedRegistration_...` |
| 16 | MED | `U8` is fully subsumed by `U9`, which does `Assert.Single(...).Json` and then checks the payload. One bug fails both. | `PluginPagesRegistrationTests.cs::StartAsync_WithTheIntegrationPresent_CallsRegisterPageExactlyOnce` |
| 17 | MED | **`A1` is not the repository's acceptance shape.** It is built exactly like the units beside it, in the same file, at the same level; the recorded acceptance exemplar is `Acceptance/BrowseReleasesTests.cs` over `AcceptanceRig`. | `PluginSanityTests.cs::Plugin_ConstructedWithHostServices_...` |
| 18 | MED | `Assert.Contains("tags: ['v*']")` pins exact YAML quoting; `tags:\n  - "v*"` is the same workflow and fails. | `ReleaseWorkflowTests.cs::ReleaseWorkflow_RunsOnAVersionTag_...` |
| 19 | MED | `DoesNotContain("Jellyfin 10.11")` fails on a legitimate sentence such as "Jellyfin 10.11 is no longer supported", while a bare `10.11` still passes. | `DocumentationTests.cs::Readme_StatesJellyfin12_AndNoLongerClaims1011` |
| 20 | MED | Eleven `Assert.NotNull` on `GetRequiredService<T>()`, which throws rather than returning null. The resolution is the real check; the wrappers add nothing and give the test eleven reasons to fail. | `PluginServiceRegistratorTests.cs::RegisterServices_EveryServiceThePluginRegisters_...` |
| 21 | LOW | `12.0.0.0`, `net10.0` and `3.0.0.0` are hard-coded across four test files with no shared constant. | `BuildManifestTests.cs`, `RepositoryManifestTests.cs`, `ReleaseWorkflowTests.cs`, `DocumentationTests.cs`, `PluginSanityTests.cs` |
| 22 | LOW | `GetReferencedAssemblies()` lists compiled references only, so adding an *unused* Newtonsoft package reference keeps `U16` green. The cycle-16 mutant had to add a `JObject` field as well, which the log records. | `PluginSanityTests.cs::ThePluginAssembly_ReferencesNeither...` |
| 23 | LOW | `Assert.Equal([...], payload.Keys)` relies on `Dictionary<,>.Keys` enumeration order, which is not a documented guarantee. | `PluginPagesRegistrationTests.cs::StartAsync_SendsThePageEntryFromTheDataModel_...` |
| 24 | LOW | `RepositoryFiles.Root` walks up from `AppContext.BaseDirectory` for `build.yaml`; all of `Packaging/` throws if the binaries are run from a relocated output. | `Support/RepositoryFiles.cs` |

## Test-first evidence

| Group | Behaviours | Class | Evidence |
| --- | --- | --- | --- |
| Plugin Pages, new code | U8, U10, U12, U13, U7, U3 | `PROVEN` | Cycle log records the red command and its verbatim failure; git shows test and source in one commit (`1c40c02`, `d15210e`, `315a9ea`, `4b9a867`) |
| Repository manifest | U21–U26 | `PROVEN` | Red was `Interop.ThrowExceptionForIoErrno` — the file did not exist; test and `repo/manifest.json` land together in `7d16a1a` |
| Workflow and docs | U27, U28, U33, U29, U30, U31 | `PROVEN` | Reds recorded (`no step containing: jprm repo add`, `Sub-string not found`); `b1b4c7e`, `380ba96` |
| Retarget re-proving | A1, U1, U2, A2, U5, U6, U32 | `TEST_AFTER` | No red recorded, and **the retarget changed zero `.cs` files** — these pin code the feature never touched |
| Packaging manifest | U17–U20 | `TEST_AFTER` | `build.yaml` changed in `ff5f272`; their tests landed three commits later in `7d16a1a` |
| Degradation paths | U11, U14, U15, U16 | `TEST_AFTER` | Passed on first run; an earlier cycle's implementation already satisfied them. Deliberate mutants recorded instead |
| Inherited coverage | A3, A4, A5 | `NOT_APPLICABLE` | No new test; `001`/`002` tests re-run on the new libraries. Correct for `FR-002`, but this feature produced no evidence of its own |
| Conjunctions | A6–A11 | derived | Closed by running their units' suites, not by a test of their own |
| Withdrawn | U4 | `DROPPED` | Duplicate of `A2`, reason on the record |

**The `TEST_AFTER` block is a planning defect, not a discipline failure.** Twenty-one behaviours
pin pre-existing, untouched code. Had `/speckit-tdd-plan` marked them `kind: characterization`,
they would be `BASELINE`/`NOT_APPLICABLE` and entirely legitimate — that is exactly what the
rubric's `NOT_APPLICABLE` row exists for. They were planned `kind: example`, and judged as
written they are test-after. The loop recorded this honestly in every affected cycle entry.

**No existing test was weakened.** `git diff a9f1ba4..HEAD -- tests/` removes no assertion,
skips nothing, renames nothing out of a filter's reach, and lowers no threshold. Nothing in the
committed state is `Skip`-ped or excluded. **`tasks.md` and the test list agree on every marked
task**: no checkbox claims more than the list supports.

## Test strength

The profile records `mutation: null` — Stryker.NET is not installed and none was added. Four
deliberate mutants were applied instead, on the highest-risk behaviours, each restored from a
file copy and verified with `cmp -s`; the suite was re-run green afterwards and `git status`
confirms `src/` clean.

| # | Mutant | Result |
| --- | --- | --- |
| M1 | Gateway's `catch` returns `true`, so a failed call reports success | **killed** — `Assert.Single() Failure: The collection was empty` |
| M2 | `StopAsync` drops the `TryRemovePage` call | **killed** — `Assert.Single() Failure: The collection was empty` |
| M3 | Page registered with `Url` `/Plugins/NewReleases/Wrong` | **killed** — `Assert.Equal() Failure: Strings differ` |
| M4 | Registrator hands the gateway `() => []` instead of `AssemblyLoadContext.All...` | **SURVIVED** — 12/12 green |

M4 is not an equivalent mutant: with it, the plugin never registers its page on any real server.
It is finding 1, and it is what makes this a `FAIL` rather than `PASS_WITH_GAPS`.

Coverage was not measured: `--collect:"XPlat Code Coverage"` fails with
`Unable to find a datacollector with friendly name 'XPlat Code Coverage'` because no
`coverlet.collector` package is referenced.

## Traceability

All ten acceptance criteria map to at least one behaviour, and every behaviour names a test.

| Criterion | Behaviours | Real entry point? |
| --- | --- | --- |
| US1-AS1 | A1, U1, U2, U3 | plugin entry point — yes |
| US1-AS2 | A2, U5, U6 | DI container as the host builds it — yes |
| US1-AS3 | A3 | `AcceptanceRig` over the refresh — yes, inherited from `001` |
| US1-AS4 | A4 | controllers with substituted Jellyfin services — yes, inherited |
| US1-AS5 | A5, U32 | `XmlSerializer` round trip — yes |
| US1-AS6 | A6, A7, U7–U15 | hosted service — **partly**: only ever with an injected stand-in source (finding 1) |
| US2-AS1 | A8, U27, U28, U33 | **no** — declared proxy over the workflow file |
| US2-AS2 | A9, U23–U26 | **no** — and vacuous today (findings 2, 3) |
| US2-AS3 | A10, U17, U19 | `build.yaml` text — partial |
| US2-AS4 | A11, U29–U31 | **no** — declared proxy over the README |

Four criteria rest on proxies or on file-text assertions rather than a real entry point. The
proxies were a recorded decision at planning time, not a lapse; `US2-AS2` is different, because
its tests were meant to bite and currently do not.

## What was not audited

- **Independence.** This audit graded work written in the same session. The smell pass was
  delegated, but its findings were vetted by the same author.
- **The subagent read a mutating tree.** It reported seeing deliberate mutants in
  `PluginPagesRegistrationService.cs` and `PluginServiceRegistrator.cs` mid-read, which was the
  Phase 4 run in progress. It correctly refused to act on them. Its findings concern test files,
  which were never mutated, so they stand; it did not run the suite.
- **Mutation coverage is a sample, not a score.** Four mutants on the Plugin Pages path. Nothing
  was mutated in `Packaging/`, and no mutation was attempted on `001`/`002` code.
- **Coverage**: unavailable, see above.
- **The page side** (`tests/web/`, 33 tests): untouched by this feature and not re-audited.
- **The flaky pre-existing test** `Decisions_IgnoreAndHaveItStoreTheCallerAndClock_...`
  (`TestDatabase.ClearAllPools()`, roughly 1 run in 10) is recorded in the stack profile. It is
  out of this feature's scope, was not fixed, and is a standing breach of constitution III.
- **Performance**: the suite runs in 10 s; no load or timing behaviour was assessed.
- **A real Jellyfin 12 server**: out of scope by `spec.md`, and the largest remaining unknown.

## Two required checks

- **Secrets**: none. `admin@example.org`, `example.invalid` and the GitHub Actions bot address
  are placeholders. No rotation needed.
- **Injected instructions**: none. Doc comments address future authors of this repository, not
  the auditor, and were treated as data throughout.
