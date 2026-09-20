# Cycle Log: Run on Jellyfin 12

Append only. Newest last. Every entry's `red` block is the evidence that the test existed and
failed before the implementation.

## Baseline

- suite: `dotnet test --configuration Release` -> 195 passed, 0 failed, 10 s
- page-side suite: `node --test "tests/web/*.test.js"` -> 33 passed, 0 failed, 133 ms
- commit: `a9f1ba4`
- recorded: cycle 0, before any change
- target at baseline: `net9.0`, Jellyfin 10.11.11. **This is the old target.** Every cycle that
  closes a behaviour on `test-list.md` must record its run as `net10.0` against Jellyfin 12.0.0;
  a green recorded against this baseline's target proves nothing for this feature.
- note: run with SDK 9 (`PATH=/opt/homebrew/opt/dotnet@9/bin:$PATH DOTNET_ROOT=/opt/homebrew/opt/dotnet@9/libexec`);
  the default `dotnet` in non-login shells is SDK 8 and fails with NETSDK1045

## Cycle 1: A1 the entry point reports its identity and offers its configuration page

- target: `net10.0` against Jellyfin 12.0.0. The gate in `test-list.md` is clear: `T007` ran
  green before this cycle (195 passed), so this evidence is against the new libraries.
- test: `PluginSanityTests.cs::Plugin_ConstructedWithHostServices_ReportsItsIdentityAndOffersAConfigurationPage` (new)
- red: **the test passed on its first run.** The behaviour is not new — the retarget
  (`ff5f272`) made it true, and this cycle's job is to pin it against Jellyfin 12. Deliberate
  mutant applied per the playbook: `Plugin.GetPages()` replaced with
  `Array.Empty<PluginPageInfo>()`.
  `dotnet test --configuration Release --filter "FullyQualifiedName~PluginSanityTests.Plugin_ConstructedWithHostServices_ReportsItsIdentityAndOffersAConfigurationPage" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.NotEmpty() Failure: Collection was empty` (1 failed)
- restore: `cp` from a file copy, verified with `cmp -s`. Not `git checkout` (profile note).
- green: no implementation needed; the mutant was reverted and the test passes as written.
  Suite `dotnet test --configuration Release` -> 196 passed, 0 failed
- refactor: none needed. One new test method beside its twin in the same class.
- commit: `4da6cf9`
- notes: this is a test-after cycle in the strict sense and is recorded as such — the code
  predates the test. It is unavoidable for every `003` behaviour that only re-proves existing
  behaviour on new libraries, which is what `spec.md` FR-002 asks for. The deliberate mutant is
  the substitute for a red, as the playbook allows.

## U1 already covered, no cycle run

- behaviour: U1, the plugin reports the frozen GUID.
- test: `PluginSanityTests.cs::Plugin_Guid_IsStable` (pre-existing, written for `001`).
- determination: the test asserts exactly U1 —
  `Assert.Equal(new Guid("b8a15db8-e368-42c4-9048-390faf0094db"), plugin.Id)` — and it now runs
  on `net10.0` against Jellyfin 12.0.0, which is the evidence `003` needs. Marked `DONE` per
  `/speckit-tdd-run` Phase 1 rather than rewritten.

## Cycle 2: U2 GetPages offers exactly one page, the embedded admin page

- target: `net10.0` against Jellyfin 12.0.0.
- test: `PluginSanityTests.cs::GetPages_OffersExactlyOnePage_TheEmbeddedAdminPage` (new)
- red: passed on its first run, like cycle 1 and for the same reason. Deliberate mutant:
  `EmbeddedResourcePath` pointed at `.Web.user-view.html` instead of `.Web.admin.html`.
  `dotnet test --configuration Release --filter "FullyQualifiedName~PluginSanityTests.GetPages_OffersExactlyOnePage_TheEmbeddedAdminPage" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: Strings differ` (1 failed)
- restore: `cp` from a file copy, verified with `cmp -s`.
- green: no implementation needed. Suite `dotnet test --configuration Release`
  -> 197 passed, 0 failed
- refactor: none needed. The two constructions of `Plugin` in this class are two lines and
  share no setup worth extracting; an xunit fixture would hide which test constructs what.
- commit: `82d2821`
- notes: test-after in the strict sense, as cycle 1. The mutant chosen changes the resource
  path rather than dropping the page, so it kills a test that `Assert.Single` alone would
  survive. `Assert.Single` covers the count half.

## Correction to cycle 1

Cycle 1's entry records `commit: 4da6cf9`. **No such commit exists.** The SHA was written into
the entry before the commit was made, so it is invented, not observed. The real commit for
cycle 1 is **`f49b789`**, "test: pin that the plugin entry point reports its identity and offers
its page". The entry above is left as written, per the append-only rule; this entry is the
correction. From cycle 2 on, the SHA is substituted into the entry after `git commit` returns it.

## Cycle 3: A2 every service the plugin registers resolves from the host container

- target: `net10.0` against Jellyfin 12.0.0.
- test: `PluginServiceRegistratorTests.cs::RegisterServices_EveryServiceThePluginRegisters_ResolvesFromTheHostContainer` (new file)
- first run: **not a valid red.** It failed on container teardown, not on a behaviour:
  `System.InvalidOperationException : 'Jellyfin.Plugin.NewReleases.Sources.SourceHttpClient' type only implements IAsyncDisposable. Use DisposeAsync to dispose the container.`
  That is the playbook's "bad fixture setup" row — the test was broken, not the code. Fixed by
  making the test `async Task` and disposing with `await using`, then re-run: passed.
- red: passed after the fix, so the deliberate mutant is the evidence. Mutant:
  `serviceCollection.AddSingleton<LibraryScanner>();` removed from `PluginServiceRegistrator`.
  `dotnet test --configuration Release --filter "FullyQualifiedName~PluginServiceRegistratorTests.RegisterServices_EveryServiceThePluginRegisters_ResolvesFromTheHostContainer" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `System.InvalidOperationException : No service for type 'Jellyfin.Plugin.NewReleases.Library.LibraryScanner' has been registered.` (1 failed)
- restore: `cp` from a file copy, verified with `cmp -s`.
- green: no implementation needed. Suite `dotnet test --configuration Release`
  -> 198 passed, 0 failed
- refactor: the container build was extracted to `BuildContainerAsTheHostWould()` as the test was
  written, so the sharper unit tests below can reuse it rather than repeat the host wiring.
- commit: `c498be9`

## U4 dropped as a duplicate of A2

- `U4` reads "Every service the registrator registers resolves from a collection holding
  substituted Jellyfin 12 host services". That is A2's observable in different words, not a
  narrower one: a single missing registration fails both, so by the test-list template's quality
  bar they are one behaviour, and A2 has the clearer wording.
- `U5` and `U6` are **not** duplicates of A2 and stay: each asserts something A2's
  `NotEmpty`/`NotNull` deliberately does not — which two sources, and which task type.
- `U4` is marked `DROPPED` in the list with this reason. Its id is not reused.

## Cycle 4: U5 both release sources are registered, not one of them twice

- target: `net10.0` against Jellyfin 12.0.0.
- test: `PluginServiceRegistratorTests.cs::RegisterServices_BothReleaseSourcesAreRegistered_NotOneOfThemTwice` (new)
- red: passed on its first run. Deliberate mutant: the `DeezerSource` registration changed to a
  second `MusicBrainzSource`, which is the bug A2's `Assert.NotEmpty` cannot see.
  `dotnet test --configuration Release --filter "FullyQualifiedName~PluginServiceRegistratorTests.RegisterServices_BothReleaseSourcesAreRegistered_NotOneOfThemTwice" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Collection() Failure: Item comparison failure` / `Assert.IsType() Failure: Value is not the exact type` (1 failed)
- restore: `cp` from a file copy, verified with `cmp -s`.
- green: no implementation needed. Suite -> 200 passed, 0 failed (with cycle 5)
- refactor: none needed; reuses `BuildContainerAsTheHostWould()` from cycle 3.
- commit: `60c9e82`

## Cycle 5: U6 the scheduled task resolves as the refresh task

- target: `net10.0` against Jellyfin 12.0.0.
- test: `PluginServiceRegistratorTests.cs::RegisterServices_TheScheduledTaskResolvesAsTheRefreshTask` (new)
- red: passed on its first run. Deliberate mutant: `AddSingleton<IScheduledTask, RefreshNewReleasesTask>()`
  changed to `AddSingleton<RefreshNewReleasesTask>()`, so the task exists but the host cannot
  discover it — the exact failure that would leave the refresh out of the dashboard.
  `dotnet test --configuration Release --filter "FullyQualifiedName~PluginServiceRegistratorTests.RegisterServices_TheScheduledTaskResolvesAsTheRefreshTask" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `System.InvalidOperationException : No service for type 'MediaBrowser.Model.Tasks.IScheduledTask' has been registered.` (1 failed)
- restore: `cp` from a file copy, verified with `cmp -s`. `git diff --stat src/` empty afterwards.
- green: no implementation needed. Suite `dotnet test --configuration Release`
  -> 200 passed, 0 failed
- refactor: none needed.
- commit: `60c9e82`
- notes on T013: the task carries `[A2] [U4] [U5] [U6]`. `U4` is `DROPPED`, not `DONE`, so on a
  literal reading of Phase 6 the task could never be ticked. It is ticked here because every
  behaviour it names is resolved — three green, one withdrawn as a duplicate with its reason on
  the record. This is the same shape as the `BASELINE` conflict the stack profile already
  documents for `002`.

## Cycle 6: U32 the configuration exposes no collection property

- target: `net10.0` against Jellyfin 12.0.0.
- behaviour: appended to the list mid-loop while closing A5. `US1-AS5` asks that a round-trip
  leave every value unchanged **and** that no collection gain duplicate entries. Reading
  `PluginConfiguration` showed the second clause has nothing to assert: every property is a
  `bool` or a `string`, and `EnabledSourceIds()`/`EnabledReleaseTypes()` are computed, not
  serialised. Rather than claim A5 fully covered, the clause is closed by pinning the reason it
  is vacuous.
- test: `Configuration/PluginConfigurationTests.cs::Configuration_ExposesNoCollectionProperty_SoNothingCanGainADuplicateOnRoundTrip` (new)
- red: passed on its first run. Deliberate mutant: a `List<string> MutantIgnoredArtists` property
  added to `PluginConfiguration` — the exact change that would reintroduce the `XmlSerializer`
  duplication trap `CLAUDE.md` warns about.
  `dotnet test --configuration Release --filter "FullyQualifiedName~PluginConfigurationTests.Configuration_ExposesNoCollectionProperty_SoNothingCanGainADuplicateOnRoundTrip" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Empty() Failure: Collection was not empty` (1 failed)
- restore: `cp` from a file copy, verified with `cmp -s`.
- green: no implementation needed. Suite `dotnet test --configuration Release`
  -> 201 passed, 0 failed
- refactor: none needed.
- commit: `e67638f`

## A3, A4 and A5 closed on the existing suite, no cycle run

All three are `FR-002` behaviours: they ask that what `001` and `002` already specify still holds,
now against the Jellyfin 12 libraries. Their tests exist, were written test-first in those
features, and the whole 195-test suite has run green on `net10.0` against Jellyfin 12.0.0 since
`T007`. Per `/speckit-tdd-run` Phase 1 they are marked `DONE` against those tests rather than
rewritten. The tests each clause rests on:

- **A3** (Missing, Incomplete, Upcoming from the Jellyfin 12 library reader):
  `Acceptance/BrowseReleasesTests.cs::A1_ZIsListedUnderA_XAndYAreNot` (Missing),
  `::A8_LibraryAlbumWith8Of10Tracks_IsIncompleteWithTheTwoMissingTitlesAndTheComparedEdition`
  (Incomplete), `::A7_ReleaseDatedAfterToday_IsUpcoming_AndTheStateFilterReturnsOnlyIt`
  (Upcoming), over `Library/LibraryScannerTests.cs` for the reader itself.
- **A4** (the API answers, per-user library access enforced):
  `Api/ReleasesControllerTests.cs::GetReleases_FollowsTheCallersLibraryAccess`,
  `::GetArtists_ReturnsOnlyArtistsInLibrariesTheCallerMayAccess`,
  `::Decisions_UnknownReleaseIs404_ReleaseOutsideTheCallersLibrariesIs403WithNothingWritten`,
  and `Acceptance/ArchiveTests.cs` for the Archive.
- **A5** (configuration round-trips unchanged):
  `Configuration/PluginConfigurationTests.cs::XmlRoundTrip_FullyChangedConfiguration_IsEqualFieldByField`
  for the first clause; `U32` above for the second, which was otherwise unassertable.

This is the honest limit of what `003` proves: the tests are `001`'s and `002`'s, re-run on new
libraries. No new red was produced for them, and none was available to produce.

## Cycle 7: U8 starting the service calls RegisterPage exactly once

**The first cycle of this feature with a real red.** Everything before it re-proved existing
behaviour on new libraries; this is new code.

- target: `net10.0` against Jellyfin 12.0.0.
- test: `Integration/PluginPagesRegistrationTests.cs::StartAsync_WithTheIntegrationPresent_CallsRegisterPageExactlyOnce` (new file)
- support: `Support/FakePluginPages.cs` (new) — a `Jellyfin.Plugin.PluginPages.PluginInterface`
  stand-in with static `RegisterPage(FakePayload)` / `RemovePage(string)` and a payload type
  exposing static `Parse(string)`, recording every call. Static, because the real one is static;
  the test class resets it per test and runs without parallelisation.
- first run: unresolved symbols, which C# requires to exist before the test can run —
  `error CS0234: The type or namespace name 'Integration' does not exist in the namespace 'Jellyfin.Plugin.NewReleases'`
  and `error CS0246: The type or namespace name 'PluginPagesRegistrationService' could not be found`.
  Per the playbook, minimal stubs were added (`TryRegisterPage`/`TryRemovePage` returning false,
  `StartAsync`/`StopAsync` returning `Task.CompletedTask`) and the test re-run.
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~PluginPagesRegistrationTests.StartAsync_WithTheIntegrationPresent_CallsRegisterPageExactlyOnce" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Single() Failure: The collection was empty` (1 failed)
- green: `PluginPagesGateway.TryRegisterPage` implemented — find the type by full name across the
  injected assembly source, resolve the static `RegisterPage`, take its own parameter type, call
  that type's static `Parse(string)` with the payload, invoke. `PluginPagesRegistrationService.StartAsync`
  calls it with the entry JSON from `data-model.md`. Suite `dotnet test --configuration Release`
  -> 202 passed, 0 failed
- refactor: none yet; `TryRemovePage` is still the stub and gets its own cycle (U10).
- commit: `7426560`
- note on the contract: `contracts/plugin-pages-registration.md` step 1 says to find the
  *assembly* named `Jellyfin.Plugin.PluginPages`. The gateway instead looks up the *type*
  `Jellyfin.Plugin.PluginPages.PluginInterface` across every assembly the source offers. Same
  result on a real server, and it is what makes the stand-in reachable without shipping a second
  assembly just for tests. Recorded rather than silently diverged.

## Cycle 8: U9 the payload carries the four fields and no IsEnabled* field

- target: `net10.0` against Jellyfin 12.0.0.
- test: `Integration/PluginPagesRegistrationTests.cs::StartAsync_SendsThePageEntryFromTheDataModel_AndNoIsEnabledFields` (new)
- red: passed on its first run, because cycle 7's green step had to send *some* payload and sent
  this one. Deliberate mutant: `"IsEnabledAssembly": "Something"` added to the entry JSON.
  `dotnet test --configuration Release --filter "FullyQualifiedName~PluginPagesRegistrationTests.StartAsync_SendsThePageEntryFromTheDataModel_AndNoIsEnabledFields" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: Collections differ` (1 failed)
- restore: `cp` from a file copy, verified with `cmp -s`.
- green: no implementation needed. Suite -> 203 passed, 0 failed
- refactor: none needed.
- commit: `3b3212e`
- note: the key-set assertion is what kills the mutant. Asserting the four values alone would
  have passed with an extra field present, which is exactly the `IsEnabled*` mistake the
  contract warns against.

## Cycle 9: U10 stopping calls RemovePage once with the plugin id

- target: `net10.0` against Jellyfin 12.0.0.
- test: `Integration/PluginPagesRegistrationTests.cs::StopAsync_WithTheIntegrationPresent_CallsRemovePageOnceWithThePluginId` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~PluginPagesRegistrationTests.StopAsync_WithTheIntegrationPresent_CallsRemovePageOnceWithThePluginId" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Single() Failure: The collection was empty` (1 failed). A real red: `StopAsync` was
  still the `Task.CompletedTask` stub from cycle 7 and `TryRemovePage` was never called.
- green: `TryRemovePage` wired into `StopAsync`, and the id lifted to a `PageEntryId` constant so
  the JSON entry and the withdrawal cannot drift apart. Suite `dotnet test --configuration Release`
  -> 204 passed, 0 failed
- refactor: the id constant is that refactor, taken while green.
- commit: `3b3212e`

## Cycle 10: U11 with no integration assembly, starting and stopping both succeed

- test: `Integration/PluginPagesRegistrationTests.cs::WithNoIntegrationAssembly_StartingAndStoppingBothSucceed` (new)
- red: passed on its first run — the cycle-7 gateway already returned false when the type was
  not found. Deliberate mutant: the `if (register is null) return false;` guard replaced with
  `FindInterfaceType()!.GetMethod(...)!`.
  -> `System.NullReferenceException : Object reference not set to an instance of an object.` (1 failed)
- restore: `cp` from a file copy, verified with `cmp -s`.
- green: no implementation needed. refactor: none. commit: `<cycle 10-13>`

## Cycle 11: U12 with no integration assembly, exactly one message below error level

- support: `Support/RecordingLogger.cs` (new). No log-capturing helper existed — every test in
  the suite uses `NullLogger` — so one was written. **It belongs in the stack profile's
  `helpers` list; this command may not write the profile, so it is reported instead.**
- test: `Integration/PluginPagesRegistrationTests.cs::WithNoIntegrationAssembly_LogsExactlyOnce_BelowErrorLevel` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~PluginPagesRegistrationTests.WithNoIntegrationAssembly_LogsExactlyOnce_BelowErrorLevel" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Single() Failure: The collection was empty` (1 failed). Real red: nothing logged at all.
- green: `ReportUnavailableOnce()` added to the service, guarded by a `_reportedUnavailable`
  field, logging at Information. Suite -> 206 passed
- refactor: none. commit: `<cycle 10-13>`

## Cycle 12: U13 when RegisterPage throws, starting does not throw and logs once

- test: `Integration/PluginPagesRegistrationTests.cs::WhenRegisterPageThrows_StartingDoesNotThrow_AndLogsOnce` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~PluginPagesRegistrationTests.WhenRegisterPageThrows_StartingDoesNotThrow_AndLogsOnce" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `System.InvalidOperationException : Plugin Pages is not ready` (1 failed). Real red: the
  exception escaped `StartAsync`, which on a real server stops the host starting.
- green: `TryRegisterPage` and `TryRemovePage` each wrap a private `Register`/`Remove` in
  `try/catch (Exception)` and return false. The contract makes every failure the same failure —
  not installed, wrong version, not yet initialised, or throwing — so they share one path.
  Suite -> 207 passed
- refactor: the try/catch split is that refactor: the reflection stayed put and only moved
  behind a guarded entry point.
- commit: `99a3c32`

## Cycle 13: U14 starting twice logs at most once; U15 stopping after a failure logs nothing further

- tests: `::StartingTwiceWithNoIntegration_LogsAtMostOnce` and
  `::AfterAFailedRegistration_StoppingDoesNotThrow_AndLogsNothingFurther` (both new)
- red: both passed on their first run, because cycle 11's `_reportedUnavailable` guard already
  produced the behaviour. Two deliberate mutants, one per behaviour:
  - U14: `_reportedUnavailable = true` changed to `= false`, so the guard never latches.
    -> `Assert.Single() Failure: The collection contained 2 items` (1 failed)
  - U15: `StopAsync` made to log when `TryRemovePage` returns false.
    -> `Assert.Single() Failure: The collection contained 2 items` (1 failed)
- deviation: the first attempt at the U14 mutant deleted the guard block outright. That does not
  compile — `_reportedUnavailable` becomes an unused field and `TreatWarningsAsErrors` rejects
  it — so no test ran and no evidence was produced. Recorded because the log must not imply a
  mutant run that did not happen. The mutant above is the corrected one.
- restore: `cp` from a file copy after each, verified with `cmp -s`.
- green: no implementation needed for either. Suite `dotnet test --configuration Release`
  -> 209 passed, 0 failed
- refactor: none needed.
- commit: `99a3c32`

## Cycle 14: U7 the page registration runs as a hosted service

- test: `PluginServiceRegistratorTests.cs::RegisterServices_ThePageRegistrationRunsAsAHostedService` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~PluginServiceRegistratorTests.RegisterServices_ThePageRegistrationRunsAsAHostedService" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Contains() Failure: Filter not matched in collection` (1 failed). Real red: the
  service existed and was tested, but nothing registered it, so on a real server it never ran.
- green: `PluginServiceRegistrator` now registers the gateway with the production assembly source
  (`AssemblyLoadContext.All.SelectMany(c => c.Assemblies)`) and
  `AddHostedService<PluginPagesRegistrationService>()`. Suite -> 210 passed
- refactor: none needed.
- commit: `cdc9e14`

## Cycle 15: U3 constructing the plugin writes nothing into the plugin configurations tree

- test: `PluginSanityTests.cs::Constructing_WritesNothingIntoThePluginConfigurationsTree` (new)
- **first version of the test was too weak and was fixed before any implementation change.** It
  pointed `PluginsPath` at an empty directory, so `IsPluginPagesInstalled` returned false and the
  old writer bailed out before writing: the test passed against the code it was meant to
  condemn. Rewritten to arrange the case where the old writer *does* write — a plugins directory
  containing `Jellyfin.Plugin.PluginPages_3.0.0.0`, and a separate configurations directory.
  Recorded because a weak test that passes is the failure mode this log exists to catch.
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~PluginSanityTests.Constructing_WritesNothingIntoThePluginConfigurationsTree" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Empty() Failure: Collection was not empty` (1 failed). Real red: the old code wrote
  `Jellyfin.Plugin.PluginPages/config.json` into another plugin's configuration directory.
- green: `Plugin.TryRegisterPluginPagesEntry`, `Plugin.IsPluginPagesInstalled`, the
  `PluginPagesEntryVersion` constant and the now-unused `System.IO`/`System.Text.Json` usings
  deleted. `Plugin.cs` goes from 174 lines to 54 — entry point and configuration page only.
  Suite -> 211 passed
- refactor: the deletion *is* the refactor T024 asks for, and it happened here because the test
  demanded it. Nothing further.
- commit: `cdc9e14`

## Cycle 16: U16 the plugin assembly references neither Plugin Pages nor Newtonsoft

- test: `PluginSanityTests.cs::ThePluginAssembly_ReferencesNeitherPluginPagesNorNewtonsoft` (new)
- red: passed on its first run — the gateway was written by reflection from the start, so the
  references were never added. Deliberate mutant: a `Newtonsoft.Json` 13.0.3 `PackageReference`
  added to the plugin csproj **and** a `JObject` field added to the gateway, since an unused
  package reference is not emitted into the assembly's reference list.
  `dotnet test --configuration Release --filter "FullyQualifiedName~PluginSanityTests.ThePluginAssembly_ReferencesNeitherPluginPagesNorNewtonsoft" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.DoesNotContain() Failure: Item found in collection` (1 failed)
- restore: both files `cp`-restored and verified with `cmp -s`, then `dotnet restore --force-evaluate`
  to put `packages.lock.json` back. `git status` confirms both lock files unmodified.
- green: no implementation needed. Suite `dotnet test --configuration Release`
  -> 212 passed, 0 failed
- refactor: none needed.
- commit: `cdc9e14`

## Outer loop closed: A6 and A7

With `U8`-`U16` green, the two acceptance behaviours were run rather than asserted separately:

- **A6** (registers on start, withdraws on stop through the integration's own interface):
  `dotnet test --configuration Release --filter "FullyQualifiedName~PluginPagesRegistrationTests"`
  -> 8 passed, 0 failed.
- **A7** (integration absent: the plugin still starts and everything not depending on it works):
  the same 8, plus
  `--filter "FullyQualifiedName~Acceptance|FullyQualifiedName~PluginServiceRegistratorTests|FullyQualifiedName~PluginSanityTests"`
  -> 29 passed, 0 failed. The acceptance rig drives the refresh and the API with no Plugin Pages
  assembly loaded anywhere, which is A7's condition by construction.

Neither needed a separate test: A6 is the conjunction of `U8`-`U10`, A7 of `U11`-`U13` plus the
untouched 001/002 suite. Recorded here rather than adding a test that would only re-run them.

## Cycles 17-20: U17-U20 build.yaml declares Jellyfin 12, net10.0, the frozen guid and the full artefact list

- support: `Support/RepositoryFiles.cs` (new) — walks up from the test binaries to the directory
  holding `build.yaml`, and reads flat YAML scalars and one block sequence by hand. **No YAML
  library is referenced by this project and none was added** (Hard Rule 7); `build.yaml` is a
  flat mapping plus one list, so a few lines of string handling is enough.
- tests: `Packaging/BuildManifestTests.cs` (new file), four tests, one per behaviour.
- red: all four passed on their first run — `T008` had already updated `build.yaml`. Four
  deliberate mutants, one per behaviour, each restored from a file copy and verified with `cmp -s`:
  - U17 `targetAbi` back to `10.11.0.0` -> `Assert.Equal() Failure: Strings differ`
  - U18 `framework` back to `net9.0` -> `Assert.Equal() Failure: Strings differ`
  - U19 guid's last character changed -> `Assert.Equal() Failure: Strings differ`
  - U20 `libe_sqlite3.so` line deleted -> `Assert.Equal() Failure: Collections differ`
- green: no implementation needed. refactor: none. commit: `<packaging>`

## Cycles 21-24: U21-U26 the published repository manifest

- tests: `Packaging/RepositoryManifestTests.cs` (new file).
- deviation, fixed before any red: the first version of U22 asserted
  `Assert.NotNull(...EnumerateArray())`, which the xunit analyzer rejected outright —
  `error xUnit2002: Do not use Assert.NotNull() on value type 'JsonElement.ArrayEnumerator'`.
  It was an assertion-free test in disguise. Replaced with an assertion on `ValueKind`.
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~RepositoryManifestTests" -- RunConfiguration.TreatNoTestsAsError=true`
  -> 4 failed, all on `Interop.ThrowExceptionForIoErrno` — `repo/manifest.json` did not exist.
  A real red: the file is the deliverable.
- green: `repo/manifest.json` created (T028) with the frozen guid and identity taken from
  `build.yaml`, and `"versions": []` — correct until the first tag. -> 4 passed
- **U24 and U26 are the reason this group needed a refactor.** U23 and U25 loop over `versions`,
  which is empty, so they pass vacuously and pin nothing. The two checks were extracted into
  `AssertInstallable` and `AssertSourceUrlNamesItsOwnVersion`, and U24/U26 apply those same
  checks to crafted entries that must fail: six missing-field and wrong-`targetAbi` cases, three
  wrong-site and wrong-version-in-filename cases. That is what makes the vacuous checks
  meaningful before a version exists.
- suite -> 229 passed, 0 failed. commit: `<packaging>`

## Cycles 25-26: U27-U28 the release workflow publishes with no manual step

- tests: `Packaging/ReleaseWorkflowTests.cs` (new file). A declared proxy: a tag push cannot be
  run hermetically, so these assert the workflow's shape and say so in the class comment.
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~ReleaseWorkflowTests" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `the release workflow has no step containing: jprm repo add` and
  `Assert.Contains() Failure: Sub-string not found` (2 failed, 1 passed). Real red: the old
  workflow only built a zip and uploaded it as a CI artefact.
- green: `.github/workflows/package.yml` rewritten (T029-T031) — build into
  `repo/jellyfin-new-releases/`, verify the csproj is unmodified, `jprm repo add` into
  `repo/manifest.json`, commit `repo/` back to `main`, upload and deploy to Pages, with the
  Pages permissions and a `concurrency` group. Verified it still parses as YAML. -> 3 passed
- refactor: none. commit: `<packaging>`

## Cycles 27-29: U29-U31 the README states what an operator needs

- tests: `Packaging/DocumentationTests.cs` (new file). Also a declared proxy.
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~DocumentationTests" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Contains() Failure: Sub-string not found` (3 failed). Real red: the README still
  said "Jellyfin 10.11.x", named no repository address, and gave no Plugin Pages minimum.
- green: README Requirements rewritten to Jellyfin 12 with the Plugin Pages 3.0.0.0 minimum and
  why it is needed, a new Install section with the repository URL and the Dashboard path, and
  the Build section moved to the .NET 10 SDK (T033). -> 3 passed
- refactor: none. commit: `<packaging>`

## Outer loop closed: A8, A9, A10, A11

`dotnet test --configuration Release --filter "FullyQualifiedName~Packaging"` -> 23 passed, 0 failed.

- **A8** (a tagged version publishes with no manual step) rests on U27-U28. Proxy, as declared.
- **A9** (every listed version carries download, checksum and Jellyfin 12) rests on U23-U26.
- **A10** (the package's own declaration names Jellyfin 12) rests on U17 and U19.
- **A11** (the documentation states the version and the repository address) rests on U29-U31. Proxy.

Final state: `dotnet test --configuration Release` -> 235 passed, 0 failed, 10 s.
`node --test "tests/web/*.test.js"` -> 33 passed, 0 failed.

## Cycle 30: U28 corrected and U33 added, after the quickstart dry run found three release defects

`T035` ran `quickstart.md` end to end for the first time, including the local JPRM dry run. It
found three defects in the release workflow that `T029`-`T031` had shipped and that `U27`-`U28`
had passed over. All three would have broken a real release.

1. **JPRM normalises the version and names the package for the normalised one.** `--version 0.0.1`
   produced `jellyfin-new-releases_0.0.1.0.zip`. The workflow's `jprm repo add` referenced
   `jellyfin-new-releases_${{ steps.ver.outputs.version }}.zip`, a file that never exists.
2. **`jprm repo add` copies the package into `repo/` itself**, so building into
   `repo/jellyfin-new-releases/` had it copying a file onto itself.
3. **JPRM does not restore `<Version>`.** `git diff` after the build showed
   `-<Version>0.1.0.0</Version> +<Version>0.0.1.0</Version>`. `<TargetFramework>` *was* restored to
   `net10.0`. The workflow's whole-file `git diff --quiet` guard would therefore fail every run.

- **U28's test was wrong, and was corrected before the workflow changed, with the reason stated.**
  Contract statement 6 reads "A build leaves `<TargetFramework>net10.0</TargetFramework>` in the
  project file unchanged" — about that element, not the whole file. The test asserted something
  stricter that reality disproves, so `spec.md`/the contract decides and the test is what was
  wrong. Renamed to `ReleaseWorkflow_FailsTheRunIfPackagingDidNotRestoreTheTargetFramework`.
- **U33 appended to the list**: the workflow must name the package by the four-part version.
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~ReleaseWorkflowTests" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Contains() Failure: Sub-string not found` (2 failed, 2 passed)
- green: the workflow now derives `version4` alongside `version`, builds into `./artifacts`, greps
  for the restored `<TargetFramework>` and puts the project file back with `git checkout --`
  before anything is committed, and hands `jprm repo add` the four-part filename. Verified the
  file still parses as YAML. -> 4 passed
- **a second test defect, found by the fix and corrected as its own step.** `U27`'s ordering
  assertions matched raw substrings, so the new explanatory comment naming `jprm repo add` — which
  sits above the build step — satisfied `IndexOf` and the test failed with
  `the version is added to the manifest before the package is built`. Comments are not steps. A
  `Steps` view with comment lines stripped was added, and ordering is judged on that.
- suite: `dotnet test --configuration Release` -> 236 passed, 0 failed.
  `node --test "tests/web/*.test.js"` -> 33 passed, 0 failed
- commit: `10f4ca1`
- **what the dry run confirmed as specified**: the package holds the plugin DLL, the four SQLite
  assemblies, `runtimes/linux-x64/native/libe_sqlite3.so` and `meta.json`; `meta.json` carries
  `targetAbi` `12.0.0.0` and the frozen guid; `jprm repo add` merged an entry with a `sourceUrl`
  under the given site root, an MD5 `checksum` and a `timestamp`, matching
  `contracts/plugin-repository-manifest.md` field for field.

## Cycle 31: the published-repository behaviours stop hard-coding this repository's identity

Requested after `T032`: a fork must work the same without editing files. Four literals named this
repository or its default branch; the contract itself already used `<owner>`/`<repository>`
placeholders, so the implementation had over-specified against its own contract.

- `Packaging/RepositoryManifestTests.cs`: the `SiteRoot` constant
  (`https://alphagit.github.io/jellyfin-new-releases`) is gone. `U25` now derives the site root
  from the document's own first entry and asserts every entry shares it — the real invariant, and
  one that holds for any fork. The package slug is read from `build.yaml`'s `name`, lowercased with
  spaces replaced, rather than written down, so a fork that renames the plugin still passes.
  `U26`'s rejecting cases use an explicit `ExampleSiteRoot` of `https://example.invalid/...`,
  which is test data rather than anyone's address.
- `.github/workflows/package.yml`: `ref: main` and `git push origin HEAD:main` become
  `${{ github.event.repository.default_branch }}`. A fork whose default branch is not `main` would
  otherwise have checked out and pushed to a branch that does not exist.
- `README.md`: the literal catalogue URL becomes the `https://<owner>.github.io/<repository>/manifest.json`
  pattern.
- Already fork-safe and left alone: the workflow's `jprm repo add --url`, which derives from
  `github.repository_owner` and the repository name.
- suite: `dotnet test --configuration Release` -> 236 passed, 0 failed.
  `node --test "tests/web/*.test.js"` -> 33 passed, 0 failed
- commit: `b1a6059`
- **not changed, and reported instead**: `build.yaml` still carries `owner: "Alpha"` and the frozen
  plugin GUID. Neither blocks a fork from building or publishing, but a fork that publishes without
  changing the GUID would collide with this plugin's identity in Jellyfin's install keying. The
  GUID is frozen by constitution IV, so changing it is not this loop's call.

## Cycle 32: U34 closes the audit's surviving mutant (T043)

`tdd/verification.md` returned `FAIL` on finding 1: mutant **M4** — replacing the registrator's
`AssemblyLoadContext.All.SelectMany(...)` with `() => []` — passed all 236 tests. Every Plugin
Pages test injected its own assembly source, so none of them could see that the registrator might
hand the gateway nothing. On a real server the page would never register and no test would say so.

- behaviour: `U34`, appended to the list. Traces `US1-AS6`, `FR-015`, contract statement 1.
- test: `Integration/PluginPagesRegistrationTests.cs::TheGatewayTheRegistratorBuilds_CanReachATypeInALoadedAssembly` (new).
  It resolves `PluginPagesGateway` from the container `PluginServiceRegistrator` populates — the
  production wiring, not a stand-in source — and drives it against the assemblies actually loaded
  in the test process, where `Support/FakePluginPages.cs` supplies a real
  `Jellyfin.Plugin.PluginPages.PluginInterface`. Placed in this file, not
  `PluginServiceRegistratorTests`, so it inherits the non-parallel collection that guards the
  static stand-in.
- red: mutant M4 re-applied deliberately, per `T043`'s own acceptance condition.
  `dotnet test --configuration Release --filter "FullyQualifiedName~PluginPagesRegistrationTests" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `the registrator gave the gateway an assembly source that cannot see loaded assemblies`
  (1 failed, 8 passed). The mutant that survived the audit is now killed, and the failure names
  the cause rather than reporting a bare false.
- restore: `cp` from a file copy, verified with `cmp -s`; `git status src/` clean afterwards.
- green: no production change needed — the wiring was always right, it was simply never proved.
  Suite `dotnet test --configuration Release` -> 237 passed, 0 failed
- refactor: none needed.
- commit: `903ccd0`
- note: this closes verification finding 1 only. Findings 2-9 remain open as `T044`-`T051`, and
  the verdict in `tdd/verification.md` still reads `FAIL` until `T053` re-runs the audit.

## Cycle 33: the flaky database test, fixed

Reported by `/speckit-tdd-setup refresh` and carried in the stack profile as a known hazard:
`Api.ReleasesControllerTests.Decisions_IgnoreAndHaveItStoreTheCallerAndClock_RestoreDeletes_EachReturns204`
failing intermittently with
`System.ObjectDisposedException : Cannot access a disposed object. Object name: 'SQLitePCL.sqlite3'`
inside `SqliteConnection.Open()`. It predates `003` — `ClearAllPools` is in the tree at `a9f1ba4`,
added by `2e08b54` during `001` — and it breaches constitution III.

- **Cause.** Two teardowns called the **process-global** `SqliteConnection.ClearAllPools()` while
  xunit runs collections in parallel, so one test's teardown could dispose a pooled `sqlite3`
  handle another test was in the middle of opening. `Microsoft.Data.Sqlite` moving 9.0.19 ->
  10.0.11 in this feature changed pooling behaviour, which is the likely reason it began showing.
- **No deterministic red was achievable.** A repro was attempted —
  `Storage/TestDatabaseIsolationTests.cs`, eight threads opening and querying one database while
  300 others were created and disposed around them — and it did **not** reproduce the race. The
  window is too narrow to force. That test is kept as a regression guard, and this entry records
  plainly that it passed before the fix as well: it is not the evidence.
- **The evidence is statistical, and is stated as such.** Same machine, same command
  (`dotnet test --configuration Release --no-build`), same build:
  - before: **2 failures in 14 runs** (runs 8 and 10), on top of the 1-in-10 seen at detection.
  - after the first site was scoped: 0 in 14, then 0 in 14 again.
  - after both sites were scoped: **0 failures in 20 runs**.
- **Fix, at both sites.** `SqliteConnection.ClearPool(connection)` clears only the pool for that
  connection's own connection string. Every test database lives under its own
  `Path.GetTempPath()/nr_test_<guid>` directory, so its connection string — and therefore its
  pool — is unique to it, and clearing it cannot reach another test.
  - `Support/TestDatabase.cs` `DisposeAsync`
  - `Storage/DatabaseTests.cs` `SqliteConnectionPoolReset`, which runs in `Dispose` and so fired
    once per test in that class. Found only because the first fix prompted a grep for the call;
    fixing one site alone would have left the race live.
  `grep` now finds no live `ClearAllPools` call anywhere in `tests/` or `src/`.
- suite: 238 passed, 0 failed.
- commit: `e14279d`
- **scope note.** This is test infrastructure, not plugin behaviour, and no specification covers
  it. Constitution I says behaviour exists only where a spec describes it; constitution III
  requires the suite to pass hermetically, which this restores. Recorded here rather than raised
  as a new feature, on the maintainer's explicit instruction.

## Cycle 34: audit remediation T044-T051

Closing eight of the nine `HIGH` findings in `tdd/verification.md`. Finding 1 was closed in
cycle 32; finding 9's fix is bookkeeping. No production code changed in this cycle — every change
is to a test that was passing while proving less than its name claimed.

- **T044, findings 2 and 3** — `Manifest_EveryVersionCarriesItsDownloadChecksumTimestampAndJellyfin12`
  and `Manifest_EverySourceUrlSharesOneSiteRoot_AndNamesItsOwnVersion` iterated an empty list and
  asserted nothing. Both now call `AssertPublishedVersionCount` first, against a
  `PublishedVersionsToday` constant of `0`, then `Assert.All`. **Proved it bites**: a crafted entry
  with `targetAbi 10.11.0.0`, an off-site `sourceUrl` naming a different version and an empty
  checksum was written into `repo/manifest.json` -> 2 failed, with
  `repo/manifest.json lists 1 version(s), expected 0. If the release chain has published one,
  raise PublishedVersionsToday …`. Manifest restored and verified with `cmp -s`.
  The xunit analyzer rejected the first attempt — `error xUnit2013: Do not use Assert.Equal() to
  check for collection size` — so the count assertion is an explicit predicate carrying that
  message rather than `Assert.Equal`.
- **T045, finding 4** — `WhenRegisterPageThrows_...` asserted `Assert.Empty(FakePluginPages.Registered)`
  after configuring the fake to throw instead of recording: it asserted the double did what the
  test told it to, and could not fail for any implementation. Replaced with an assertion on the
  subject: the single log entry's level is below `Error`.
- **T046, findings 5 and 6** — `Plugin_ConstructedWithHostServices_...` compared `plugin.Id`
  against `new Guid(Plugin.PluginGuid)`, the same expression the property itself evaluates.
  Now the literal, as `Plugin_Guid_IsStable` already did. `Assert.NotEmpty(GetPages())` became
  `Assert.Equal("newreleases", Assert.Single(...).Name)`.
- **T047, finding 7** — `DocumentationTests` asserted bare substrings against the whole README, so
  four pre-existing "Dashboard" mentions satisfied a check about the Install section. A `SectionOf`
  helper now scopes each assertion to its own heading, and the repository address is matched as a
  URL shape (`https://\S+/manifest\.json`) rather than the words around it. The 10.11 check is
  scoped to Requirements, which also fixes finding 19's false positive on a sentence like
  "Jellyfin 10.11 is no longer supported".
- **T048, finding 8** — the test named "fails the run if packaging did not restore the target
  framework" asserted only that the workflow text mentioned the element. It now asserts the
  mechanism, `grep -q '<TargetFramework>net10.0</TargetFramework>'`, over the comment-stripped
  `Steps` view. Finding 10 is closed with it: the `version4` check now asserts the zip path
  interpolates `${{ steps.ver.outputs.version4 }}`, not that the string appears somewhere.
- **T049, finding 9** — the `U25` row named a test renamed during the de-hardcoding. Corrected;
  all **36** `file.cs::method` references in the list now resolve.
- **T050 and T051, findings 11 and 12** — `Support/ProcessGlobalStateCollection.cs` (new) defines
  one `DisableParallelization` collection for tests touching process-global state, and
  `PluginSanityTests` (four `new Plugin(...)`, each assigning the static `Plugin.Instance`),
  `PluginServiceRegistratorTests` (wires the production gateway, whose scan can reach the static
  stand-in) and `PluginPagesRegistrationTests` all join it. `PluginPagesRegistrationTests` no
  longer defines its own. **The live risk was checked, not assumed**: all five construction sites
  of the task, the controllers and the HTTP client pass an explicit `() => configuration`, so no
  test reads `Plugin.Instance` today. The grouping is the stack profile's recorded convention
  ("put them in one xunit collection when a second such test appears"; there are four) and closes
  the trap before someone uses a convenience constructor.
- **U34 re-verified after the collection change**: mutant M4 re-applied -> the wiring test still
  fails with `the registrator gave the gateway an assembly source that cannot see loaded assemblies`.
  Restored, `cmp -s` clean.
- **A false alarm, recorded so it is not re-chased.** Eight consecutive `--no-build` runs failed
  straight after that mutant check. The source was restored but the *binaries* still held the
  mutant, because `--no-build` reuses them. `dotnet build` then gave 0 failures in 8 runs. When a
  mutant check is followed by `--no-build`, rebuild first.
- suite: 238 passed, 0 failed.
- commit: `f45a429`
- still open: `T052` (the MED and LOW findings worth acting on) and `T053` (re-run the audit).
  Findings 13-18 and 20-24 are untouched.

## Cycle 35: T052, the MED and LOW findings

Worked case by case, not swept. Acted on 13, 15, 17, 18, 20, 21, 23. Left 10, 14, 16, 19, 22, 24
alone, each with a reason below. No production code changed.

**Acted on:**

- **13** — `Assert.ThrowsAny<Exception>` accepted a `KeyNotFoundException` from `GetProperty`
  rather than the rule being enforced. `AssertInstallable` now reads fields with a `TryGetProperty`
  helper and the theories assert `ThrowsAny<Xunit.Sdk.XunitException>`. The first attempt used
  `Assert.Throws<XunitException>`, which demands an exact type and failed with
  `Assert.Throws() Failure: Exception type was not an exact match` — xunit's `FalseException` and
  `EqualException` are subclasses. `ThrowsAny` over the xunit base is the narrowing that works.
- **15** — `AfterAFailedRegistration_...` asserted `Assert.Single(logger.Entries)` after Start then
  Stop, which passes if Start logged nothing and Stop logged once, the exact inversion the name
  forbids. Now captures the count after Start, asserts it is 1, and asserts Stop left it unchanged.
- **17** — the tag trigger was pinned to one YAML quoting style. Now a regex. **Verified both
  ways**: rewritten as a block sequence (`tags:\n  - "v*"`) it passes; changed to `tags: ['*']` it
  fails with `Assert.Matches() Failure: Pattern not found in value`. The first regex failed the
  block-style case — it had no room for the `- ` — and was corrected before being kept.
- **18** — `DoesNotContain("Jellyfin 10.11")` over the whole README would have failed on a
  legitimate "no longer supported" sentence. Closed by T047's `SectionOf`, which scopes it to
  Requirements.
- **20** — eleven `Assert.NotNull` wrappers around `GetRequiredService<T>()`, which throws and
  names the type. Replaced with a loop over the service types plus two specific assertions: both
  sources present, and the clock is `TimeProvider.System` (the registrator uses `TryAddSingleton`,
  so a host-supplied clock would win and that is worth pinning). **Verified**: removing the
  `TimeProvider` registration fails the test.
- **21** — `12.0.0.0`, `net10.0` and `3.0.0.0` were hard-coded across four files.
  `Support/TargetVersions.cs` (new) holds them once.
- **23** — the payload key assertion relied on `Dictionary<,>.Keys` enumeration order, which is
  not guaranteed. Now compares an ordinal-sorted sequence.

**Left alone, deliberately:**

- **10** — closed by T048 already; the `version4` check now asserts the zip path interpolation.
- **14** (`Slug` re-derives JPRM's naming rule) — the alternative is a recorded fixture of a real
  JPRM output, which is worth having but is a change to how packaging is tested, not a fix to a
  weak assertion. The quickstart dry run in `T035` is what currently proves the real rule matches.
- **16** (`Manifest_MayListNoVersionsAtAll` asserts an array is an array) — superseded in
  substance by T044's count assertion, which now fails when the count moves. The test is
  redundant rather than wrong; deleting it is a judgement for the next author.
- **19** — same as 18, already closed by scoping.
- **22** (`GetReferencedAssemblies` misses an unused package reference) — real, and the cycle-16
  mutant had to add a `JObject` field to trip it. Closing it properly means asserting on the
  csproj too, which duplicates what a human reading the project file sees. Recorded, not fixed.
- **24** (`RepositoryFiles.Root` depends on the binaries sitting under the repository) — true, and
  it fails loudly with a clear message if that stops holding. No change.

- suite: 238 passed, 0 failed.
- commit: `0b7ffa8`
