---
detected_at: ed8d2f5
ecosystems: [dotnet]
default: dotnet
stacks:
  dotnet:
    cwd: .
    runner: xunit
    # `--` RunConfiguration.TreatNoTestsAsError=true is mandatory: without it a filter that
    # matches nothing exits 0 (verified), which would turn every red into a false green.
    single: 'dotnet test --configuration Release --filter "FullyQualifiedName~{name}" -- RunConfiguration.TreatNoTestsAsError=true'
    file: null
    suite: dotnet test --configuration Release
    watch: null
    coverage: null
    mutation: null
    acceptance: null
    property: null
    approval: null
    contract: null
    test_glob: "tests/Jellyfin.Plugin.NewReleases.Tests/**/*Tests.cs"
    exemplar:
      unit: tests/Jellyfin.Plugin.NewReleases.Tests/PluginSanityTests.cs
    helpers:
      - tests/Jellyfin.Plugin.NewReleases.Tests/Support/StubHttpMessageHandler.cs
      - tests/Jellyfin.Plugin.NewReleases.Tests/Support/FixtureLoader.cs
      - tests/Jellyfin.Plugin.NewReleases.Tests/Support/TimeProviderStub.cs
verified: [single, suite]
suite_baseline: green
suite_seconds: 2
---

# TDD Stack Profile

## Conventions to match

- One test project: `tests/Jellyfin.Plugin.NewReleases.Tests/` (xunit 2.9.3, NSubstitute 5.3.0,
  `TreatWarningsAsErrors`). Mirror the source folder layout: a class under `src/.../Storage/`
  gets `tests/.../Storage/<Class>Tests.cs`.
- Assertions use xunit `Assert.*`. Doubles use NSubstitute `Substitute.For<T>()`. No
  assertion library is referenced yet.
- `{name}` in the single-test command is an xunit fully qualified name fragment, e.g.
  `PluginSanityTests.Plugin_Guid_IsStable`. `FullyQualifiedName~` is a substring match, so use
  `Class.Method` to hit exactly one test.
- No live network calls in tests. External HTTP goes through `HttpClient` with a stubbed
  `HttpMessageHandler`; recorded, scrubbed bodies live in `tests/fixtures/<source>/` and are
  copied to the test output as `fixtures/` (see the test csproj). Sibling project
  `../jellyfin-concert-radar/tests/.../Support/` shows the shape of stub handler, fixture
  loader and temp SQLite helpers; port them into `tests/.../Support/` when first needed and
  add them to `helpers` here.
- Jellyfin services (`ILibraryManager`, `IApplicationPaths`, `IXmlSerializer`) are substituted,
  never real. Tests that construct `Plugin` set the static `Plugin.Instance`; put them in one
  xunit collection when a second such test appears.
- Exemplar to imitate: `tests/Jellyfin.Plugin.NewReleases.Tests/PluginSanityTests.cs` (unit).
  There is no acceptance exemplar yet.

## Notes and constraints

- Suite wall time is 2 s including build. Per-cycle full runs are fine.
- Build requires .NET SDK 9 (`net9.0`). On this Mac `~/.zshenv` puts Homebrew `dotnet@9` first;
  CI uses `actions/setup-dotnet` 9.0.x. `--no-build` variants are safe after one build.
- CI gate: `dotnet build --configuration Release` then
  `dotnet test --configuration Release --no-build --logger "trx;LogFileName=test-results.trx"`.
- `file: null` — xunit has no run-one-file switch; filter by class name instead
  (`FullyQualifiedName~<ClassName>`).
- `coverage: null` — `--collect:"XPlat Code Coverage"` fails: no `coverlet.collector` package.
  Audit falls back to trace checking. Ecosystem default to add: `coverlet.collector`.
- `mutation: null` — Stryker.NET not installed (`dotnet tool list -g`). Audit uses deliberate
  mutants. Ecosystem default to add: `dotnet-stryker` global tool.
- `property: null` — no FsCheck/CsCheck. Invariants become boundary example tests.
- `acceptance: null` — no host-level runner. Controller and scheduled-task behaviour is tested
  at the class level with substituted Jellyfin services, as in concert-radar.
- `watch: null` — `dotnet watch test` exists in the SDK but was not run here.
- Constitution principle not applied: `.specify/memory/constitution.md` is still the template.
  Add the TDD principle when running `/speckit-constitution` (text in
  `.specify/extensions/tdd/templates/tdd-stack-profile.md`, "Constitution principle").
