---
detected_at: ed8d2f5
ecosystems: [dotnet, node]
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
      unit: tests/Jellyfin.Plugin.NewReleases.Tests/Matching/TitleNormalizerTests.cs
      acceptance: tests/Jellyfin.Plugin.NewReleases.Tests/Acceptance/BrowseReleasesTests.cs
    helpers:
      - tests/Jellyfin.Plugin.NewReleases.Tests/Support/StubHttpMessageHandler.cs
      - tests/Jellyfin.Plugin.NewReleases.Tests/Support/FixtureLoader.cs
      - tests/Jellyfin.Plugin.NewReleases.Tests/Support/TimeProviderStub.cs
      - tests/Jellyfin.Plugin.NewReleases.Tests/Support/TestDatabase.cs
      - tests/Jellyfin.Plugin.NewReleases.Tests/Support/LibraryFakes.cs
      - tests/Jellyfin.Plugin.NewReleases.Tests/Support/SourceHarness.cs
      - tests/Jellyfin.Plugin.NewReleases.Tests/Support/ControllerContextFactory.cs
      - tests/Jellyfin.Plugin.NewReleases.Tests/Support/SourceJson.cs
      - tests/Jellyfin.Plugin.NewReleases.Tests/Acceptance/AcceptanceRig.cs
  node:
    cwd: .
    runner: node:test
    # Node's built-in runner. No package.json, no install step: `node:test`, `node:assert`
    # and `node:vm` are standard library, so the suite still passes with no network.
    # The path must be a glob, not a directory: `node --test tests/web` resolves it as a module.
    single: 'node --test --test-name-pattern "{name}" "tests/web/*.test.js"'
    file: 'node --test tests/web/{file}'
    suite: 'node --test "tests/web/*.test.js"'
    watch: 'node --test --watch "tests/web/*.test.js"'
    coverage: 'node --test --experimental-test-coverage "tests/web/*.test.js"'
    mutation: null
    acceptance: null
    property: null
    approval: null
    contract: null
    test_glob: "tests/web/**/*.test.js"
    exemplar:
      unit: tests/web/staleness.test.js
    helpers:
      - tests/web/load-page.js
verified: [single, suite]
suite_baseline: green
suite_seconds: 10
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

## Page-side conventions (`node` ecosystem)

- Tests live in `tests/web/*.test.js` and use `node:test` (`test`, `describe`) with `node:assert`
  in strict mode. No assertion library, no framework, no `package.json`.
- The embedded pages under `src/Jellyfin.Plugin.NewReleases/Web/` are not modules. Each exposes
  its pure helpers on `NewReleasesInternals` as the first statement of its IIFE; the recorded
  helper `tests/web/load-page.js` runs the page's script in a `node:vm` sandbox and returns them.
  Never hand-roll a second loader, and never read a page's source as text to assert on it.
- Only the helpers that can be computed without a page are reachable this way. Anything that
  reads or writes elements (`row`, `render`, `refreshStatus`, `read`, `fill`, `query`) needs a
  simulated browser this project does not have; those stay manual.

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
- Constitution principle applied: `.specify/memory/constitution.md` has been at version 1.2.0
  since 2026-09-06 and its principle II, "Test-Driven Development (NON-NEGOTIABLE)", governs this
  profile. Nothing further to add.
