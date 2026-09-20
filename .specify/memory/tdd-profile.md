---
detected_at: b1b4c7e
ecosystems: [dotnet, node]
default: dotnet
stacks:
  dotnet:
    cwd: .
    runner: xunit
    # `--` RunConfiguration.TreatNoTestsAsError=true is mandatory: without it a filter that
    # matches nothing exits 0 (re-verified on SDK 10 / Test.Sdk 18.9.0), which would turn every
    # red into a false green.
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
      - tests/Jellyfin.Plugin.NewReleases.Tests/Support/FakePluginPages.cs
      - tests/Jellyfin.Plugin.NewReleases.Tests/Support/RecordingLogger.cs
      - tests/Jellyfin.Plugin.NewReleases.Tests/Support/RepositoryFiles.cs
      - tests/Jellyfin.Plugin.NewReleases.Tests/Support/HostContainer.cs
      - tests/Jellyfin.Plugin.NewReleases.Tests/Support/TargetVersions.cs
      - tests/Jellyfin.Plugin.NewReleases.Tests/Support/ProcessGlobalStateCollection.cs
  node:
    cwd: .
    runner: node:test
    # Node's built-in runner. No package.json, no install step: `node:test`, `node:assert`
    # and `node:vm` are standard library, so the suite still passes with no network.
    # The path must be a glob, not a directory: `node --test tests/web` resolves it as a module.
    # single is null on purpose: no node invocation fails when the name matches nothing, which
    # constitution II forbids. `file` is the loop's unit of work here. See the note below.
    single: null
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
      - tests/web/fixed-clock.js
verified: [single, file, suite]  # single: dotnet only; node's is null, see the note
suite_baseline: green
suite_seconds: 13
---

# TDD Stack Profile

Refreshed 2026-09-20, twice. The second refresh followed `003-jellyfin-12-compat`'s TDD loop and
records what it added: three new test helpers, two new test folders, and a flaky test the loop
never saw because it passes nine runs in ten. `detected_at` is `b1b4c7e`, the loop's last commit;
the tree is clean.

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
- New test folders since `003`: `Integration/` for the optional Plugin Pages integration, and
  `Packaging/` for behaviours that read repository-root files (`build.yaml`, `repo/manifest.json`,
  `.github/workflows/package.yml`, `README.md`) through `Support/RepositoryFiles.cs`.
- Exemplar to imitate: `tests/Jellyfin.Plugin.NewReleases.Tests/Matching/TitleNormalizerTests.cs`
  for a unit test, `tests/.../Acceptance/BrowseReleasesTests.cs` for an acceptance-shaped one.
  The latter is an integration test over the real entry points with substituted Jellyfin
  services, not a host-level end-to-end test; `acceptance: null` records that no such runner
  exists.

## Page-side conventions (`node` ecosystem)

- Tests live in `tests/web/*.test.js` and use `node:test` (`test`, `describe`) with `node:assert`
  in strict mode. No assertion library, no framework, no `package.json`.
- One file per subject, named for it: `staleness.test.js` and `checked.test.js` hold the two
  copies of the unit ladder (`user-view.html` and `admin.html`), `esc.test.js` the escaping, and
  `page-helpers.test.js` what is left. The two ladder files are twins — change one page's ladder
  and the other must follow. Fixed instants and the `ago`/`ahead` helpers come from
  `tests/web/fixed-clock.js`; never redeclare them in a test file.
- The embedded pages under `src/Jellyfin.Plugin.NewReleases/Web/` are not modules. Each exposes
  its pure helpers on `NewReleasesInternals` as the first statement of its IIFE; the recorded
  helper `tests/web/load-page.js` runs the page's script in a `node:vm` sandbox and returns them.
  Never hand-roll a second loader, and never read a page's source as text to assert on it.
- Only the helpers that can be computed without a page are reachable this way. Anything that
  reads or writes elements (`row`, `render`, `refreshStatus`, `read`, `fill`, `query`) needs a
  simulated browser this project does not have; those stay manual.

## Notes and constraints

- **The toolchain moved to .NET SDK 10.** `net9.0` is gone from both csproj files. Homebrew keeps
  three kegs — `dotnet` (10.0.400), `dotnet@9`, `dotnet@8` — and each reports only its own SDK
  from `dotnet --list-sdks`, so that command never shows all three. `~/.zshenv` now puts
  `/opt/homebrew/opt/dotnet/bin` first and sets `DOTNET_ROOT` to match, verified with
  `env -i HOME=$HOME /bin/zsh -lc 'dotnet --version'` → `10.0.400`. A plain `dotnet` is therefore
  correct in any newly started shell. **A shell started before 2026-09-20 keeps a stale `PATH`**
  and still resolves `dotnet@9`, which fails with
  `NETSDK1045: The current .NET SDK does not support targeting .NET 10.0`. In such a shell,
  prefix with `PATH=/opt/homebrew/opt/dotnet/bin:$PATH DOTNET_ROOT=/opt/homebrew/opt/dotnet/libexec`.
- **`single: null` for the `node` stack, because no invocation of it can be made safe.**
  Constitution II requires that a single-test invocation fail when it matches no test. Node's
  does not. Verified on node v22.20.0, three ways, all exiting **0** on a name that matches
  nothing:
  - `--test-name-pattern "ZzzNope" "tests/web/*.test.js"` → `# pass 5`. Those five are the five
    test *files*, each reporting `1..0` subtests — not five passing tests.
  - the same pattern against one file → `# pass 1`, the file itself.
  - `--test-reporter=tap` shows the truth (`1..0` then `1..1`) but still exits 0.

  `--test-skip-pattern` is the inverse filter, not a guard, and node 22 has no "fail on no
  match" switch. The pattern flag *does* report a real failure correctly (a deliberate mutant on
  `checked.test.js:19` gave exit 1, `# fail 1`), so the flaw is narrow: a mistyped name reads as
  a green, which is exactly the red-phase hazard. **Drive the page side with `file`.**
  `node --test tests/web/checked.test.js` gives true counts, exits 1 on a real failure, and
  exits 1 with `Could not find …` on a wrong path — all three verified.
- **A predicate needs a table, not an example.** Three consecutive remediations on this feature
  each fixed one defect and introduced another, every time because the fix was demonstrated with
  a single example chosen *after* the implementation was written. The README `10.11` check is the
  clearest case: a regex proved with one hand-picked sentence turned out to be strictly weaker
  than the assertion it replaced, missing `Requires Jellyfin 10.11.` on capitalisation alone.
  When a fix is a predicate — a regex, a filter, a source scan — write the accepting and
  rejecting cases from the requirement first, put them in the suite as a `[Theory]`, and only
  then write the predicate. `Packaging/DocumentationTests.cs` carries such a table.
- **The flaky database test is fixed.** It failed roughly once in ten full runs with
  `System.ObjectDisposedException : Cannot access a disposed object. Object name: 'SQLitePCL.sqlite3'`
  inside `SqliteConnection.Open()`, measured at 2 failures in 14 runs before the fix and 0 in 20
  after. Cause: two teardowns called the **process-global** `SqliteConnection.ClearAllPools()`
  while xunit runs collections in parallel, so one test's teardown could dispose a pooled handle
  another test was opening. Both now call `SqliteConnection.ClearPool(connection)`, which clears
  only that connection string's pool; every test database has its own temp path, so its pool is
  its own. **Never reintroduce `ClearAllPools()` in this suite** — `grep` should find no live
  call. Sites: `Support/TestDatabase.cs` `DisposeAsync`, `Storage/DatabaseTests.cs`
  `SqliteConnectionPoolReset`. `Storage/TestDatabaseIsolationTests.cs` is the regression guard,
  and it is a weak one: it could not reproduce the race before the fix either.
- Suite wall time is 13 s including build, 10 s of it the test run. Per-cycle full runs are fine.
- CI gate: `dotnet restore`, then `dotnet build --configuration Release --no-restore`, then
  `dotnet test --configuration Release --no-build --logger "trx;LogFileName=test-results.trx"`,
  then `node --test "tests/web/*.test.js"`. `actions/setup-dotnet` is pinned to `10.0.x`.
- `file: null` for dotnet — xunit has no run-one-file switch; filter by class name instead
  (`FullyQualifiedName~<ClassName>`).
- `coverage: null` — re-checked on SDK 10: `--collect:"XPlat Code Coverage"` still fails with
  `Unable to find a datacollector with friendly name 'XPlat Code Coverage'`, because no
  `coverlet.collector` package is referenced. Audit falls back to trace checking. Ecosystem
  default to add: `coverlet.collector`.
- `mutation: null` — `dotnet tool list -g` is empty; Stryker.NET is not installed. Audit uses
  deliberate mutants. Ecosystem default to add: `dotnet-stryker` global tool.
- `property: null` — no FsCheck/CsCheck. Invariants become boundary example tests.
- `acceptance: null` — no host-level runner. Controller and scheduled-task behaviour is tested
  at the class level with substituted Jellyfin services, as in concert-radar.
- `watch: null` for dotnet — `dotnet watch test` exists in the SDK but was not run here.
- **Restore a deliberate mutant from a file copy, never with `git checkout`.** `git checkout -- <file>`
  reverts the whole file to HEAD, taking any uncommitted work with it. This has now cost work twice
  on `002`: cycle 3 of its cycle log, and mutant M1 of its verification report. Copy the file aside,
  apply the mutant, run, copy back, and verify the restore with `cmp -s`. `git diff` being empty is
  not proof when the baseline itself is uncommitted. The retarget is uncommitted right now, so this
  matters more than usual.
- **Page tests must not depend on the machine's locale.** The pages pass `undefined` to
  `Intl.RelativeTimeFormat` on purpose, so a Jellyfin user reads the sentence in their own language.
  `tests/web/load-page.js` pins the sandbox's `Intl` to `en` for that reason. Check a change with
  `LANG=de_DE.UTF-8 node --test "tests/web/*.test.js"`, not only the default locale.
- **Known conflict in the `tdd` extension, not worked around here.** `/speckit-tdd-run` Phase 6
  forbids ticking a task whose behaviour is `BASELINE`, but characterization behaviours terminate at
  `BASELINE` and can never reach `DONE`, so such a task could never be ticked. `T023` and `T024` of
  `002` are ticked against `BASELINE` behaviours, which is right in substance. Do not "fix" it by
  promoting `U29`-`U33` to `DONE`; that would falsify the record. Reporting it upstream was
  considered and dropped: the conflict is in the third-party extension, not in this plugin. Keep
  ticking such tasks and leave this note in place.
- Constitution principle applied: `.specify/memory/constitution.md` has been at version 1.3.0
  since 2026-09-19 and its principle II, "Test-Driven Development (NON-NEGOTIABLE)", governs this
  profile. Nothing further to add. Principle III, "Hermetic Tests", requires the suite to pass on
  a machine with no network and no Jellyfin server; the flaky test that breached it is fixed.
