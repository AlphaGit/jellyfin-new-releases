<!--
Sync Impact Report
- Version change: 1.0.0 → 1.1.0
- Modified principles: none
- Modified sections: Development Workflow (single-maintainer branching model: commit to
  `main`, no feature branches, no pull requests; CI gate wording), Governance (review wording
  no longer assumes pull requests)
- Initial ratification (1.0.0) added:
- Added sections:
  - Core Principles I–VI (Spec-Driven Development; Test-Driven Development; Hermetic Tests;
    Jellyfin Compatibility; Respectful Sources and Privacy; Simplicity)
  - Technical Constraints
  - Development Workflow
  - Governance
- Removed sections: none
- Follow-up TODOs: none
-->

# Jellyfin New Releases Constitution

## Core Principles

### I. Spec-Driven Development

Behaviour exists only when a specification under `specs/` describes it.

- Every feature starts with `/speckit-specify`, is clarified with `/speckit-grill-me` until no
  material decision is open, and is planned and tasked before any implementation begins.
- `specs/` is the single source of truth. The public GitHub Project board is a one-way mirror
  of `tasks.md`; nothing on the board changes a spec.
- Scope not written in the active spec MUST NOT be implemented. Discoveries during
  implementation go back into the spec first, then into code.

Rationale: the plugin is developed largely by agents. A written spec is the only durable
statement of intent across sessions.

### II. Test-Driven Development (NON-NEGOTIABLE)

Every behaviour change is driven by a test that failed first.

- A test exists and has been observed failing, for the right reason, before the code that
  makes it pass. The failure output is recorded in `specs/<feature>/tdd/cycle-log.md`.
- Test tasks are not optional. `tasks.md` places each behaviour's test task before its
  implementation task, and the implementation task is not started until the test is red.
- Tests are never weakened, skipped, deleted, or filtered out to reach green. When a test and
  the code disagree, `spec.md` decides which is wrong.
- Every acceptance criterion in `spec.md` has at least one test that exercises the real entry
  point (controller, scheduled task, or repository), not only a unit with doubles at every
  boundary.
- Refactoring happens only on a green suite, and never changes a test in the same commit as a
  behaviour change.
- Test strength is verified, not assumed: mutation testing on the changed files where a
  mutation tool exists, and a deliberate-mutant spot check where it does not.
- The verified stack commands live in `.specify/memory/tdd-profile.md`. A single-test
  invocation MUST fail when it matches no test.

### III. Hermetic Tests

The test suite MUST pass on a machine with no network and no Jellyfin server.

- No test makes a live network call. External HTTP goes through `HttpClient` with a stubbed
  `HttpMessageHandler`; response bodies are recorded fixtures under `tests/fixtures/<source>/`.
- Fixtures are scrubbed before commit: no API keys, session tokens, or personal data. A failing
  contract test is fixed by refreshing the fixture or the parser, never by suppressing the test.
- Jellyfin services (`ILibraryManager`, `IApplicationPaths`, `IXmlSerializer`, …) are
  substituted, never real. Persistent state in tests uses temporary files that the test
  removes.
- The suite is fast enough to run on every red-green cycle. If it stops being so, a fast
  subset is agreed and recorded in the TDD profile before the loop continues.

### IV. Jellyfin Compatibility

The plugin MUST load, upgrade, and degrade cleanly on the Jellyfin version it targets.

- The plugin GUID `b8a15db8-e368-42c4-9048-390faf0094db` never changes. Jellyfin keys installs
  by it; a test guards it.
- `Jellyfin.Controller` and `Jellyfin.Model` are pinned to the exact server version in
  production (10.11.x) with `ExcludeAssets=runtime`. `targetAbi` in `build.yaml` moves with
  them.
- `PluginConfiguration` MUST round-trip through `XmlSerializer`: collections are `List<T>`,
  never seeded in constructors, no dictionaries or sets.
- Optional integrations (Plugin Pages, File Transformation) MUST be optional at runtime. When
  absent, the admin page, API, and scheduled tasks still work and the plugin logs once, at
  most.
- Schema and configuration changes MUST migrate forward from every released version. No
  release requires the operator to delete plugin data.

### V. Respectful Sources and Privacy

The plugin is a polite client of other people's services and a custodian of the operator's
library data.

- Official or open APIs are the default. HTML scraping of a source is permitted only behind an
  explicit per-source terms-of-service opt-in in the admin page, off by default, and clearly
  labelled as scraping.
- Every source has a rate limit and a daily request budget, enforced server-side, plus a
  circuit breaker that disables a failing source for a cooldown window.
- Outgoing requests carry a `User-Agent` identifying the plugin and version, with the
  operator's contact when configured, and never a hard-coded personal contact.
- Only what a source needs leaves the server: artist names and public identifiers such as
  MusicBrainz IDs. No telemetry, no usage reporting, no library contents beyond that.
- Secrets live only in the plugin configuration. Logs and error messages redact API keys and
  tokens from URLs and headers.

### VI. Simplicity

Ship the smallest change that satisfies the spec.

- Before adding code, prefer in order: not doing it (YAGNI), the .NET base library, a Jellyfin
  host service, a dependency already referenced, one line, then the minimum that works.
- No interface with one implementation, no factory for one product, no configuration for a
  value that never changes, no scaffolding for a feature not yet specified.
- A new direct dependency requires a stated reason in `plan.md` and a pinned version published
  at least 7 days before adoption. Licence must be compatible with MIT.
- `TreatWarningsAsErrors` stays on. Deliberate simplifications with a known ceiling carry a
  comment naming the ceiling and the upgrade path.

## Technical Constraints

- Language and runtime: C# on `net9.0`, matching the Jellyfin 10.11 host. Nullable and implicit
  usings enabled; `LangVersion` latest.
- Test stack: xunit and NSubstitute. An assertion library may be added when xunit `Assert`
  becomes limiting; the choice is made in the feature's `plan.md`.
- If scraping is ever enabled, AngleSharp is the only HTML parser. Two parsers in one plugin is
  a defect.
- Persistent state lives under the plugin's data directory. When a database is needed it is
  SQLite via `Microsoft.Data.Sqlite`, migrated by embedded, numbered SQL scripts, with the
  native library shipped in `build.yaml` artifacts.
- Web UI ships as embedded resources under `Web/`. `admin.html` is a standard Jellyfin plugin
  configuration page; the user-facing view is an HTML fragment served to Plugin Pages. No build
  step, no framework, no external assets.
- Packaging: JPRM `build.yaml`, `manifest.json` served from GitHub Pages, MIT licence. Every
  released version has a `CHANGELOG.md` entry.

## Development Workflow

- Spec Kit order per feature: `/speckit-specify` → `/speckit-grill-me` → `/speckit-plan` →
  `/speckit-tasks` (accept `speckit-tdd-plan`; accept `speckit-tasks-to-project-publish` to
  mirror the roadmap) → `speckit-tdd-run` (mandatory pre-implement hook) →
  `/speckit-implement` → `speckit-tdd-verify` → `speckit-tasks-to-project-sync`.
- Single-maintainer project: no feature branches and no pull requests. Finished work is
  committed to `main` and pushed. Parallel work and isolation use Orca worktrees; a worktree's
  scratch branch is fast-forwarded onto `main` and is never pushed on its own. The worktrees
  extension only prompts and MUST NOT auto-create worktrees or branches. Revisit this rule when
  a second contributor joins.
- Every plan's Constitution Check lists each principle above with a pass or a justified
  deviation in Complexity Tracking. An unjustified deviation blocks `/speckit-tasks`.
- CI gate on every push to `main`: `dotnet build --configuration Release` with zero warnings,
  then `dotnet test`. Red CI is fixed before new work starts.
- Commits follow Conventional Commits. Releases are semver tags `vX.Y.Z`; the tag triggers the
  JPRM package workflow, and `build.yaml`, `manifest.json`, and `CHANGELOG.md` change in the
  same commit as the version.
- `CLAUDE.md` is runtime guidance for agents and MUST NOT contradict this constitution. When
  they disagree, this document wins and `CLAUDE.md` is corrected.

## Governance

This constitution supersedes every other practice document in the repository.

- Amendments are made by editing `.specify/memory/constitution.md` through `/speckit-constitution`
  in a commit that states the reason, updates the Sync Impact Report, and bumps the version:
  MAJOR for removing or redefining a principle, MINOR for adding a principle or materially
  expanding guidance, PATCH for clarifications.
- Every `plan.md` records a Constitution Check; `/speckit-analyze` and the implementer verify it
  before work lands on `main`. A principle that is repeatedly deviated from is amended or
  enforced, not ignored.
- The constitution is reviewed when the Jellyfin target major version changes, when Spec Kit
  changes its artifact contract, or at the first release, whichever comes first.

**Version**: 1.1.0 | **Ratified**: 2026-09-06 | **Last Amended**: 2026-09-06
