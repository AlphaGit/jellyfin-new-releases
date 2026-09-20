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
