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
