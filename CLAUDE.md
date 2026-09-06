# Jellyfin New Releases — project notes

Jellyfin plugin. Tracks new releases by library artists that the library does not contain yet.
Sibling project and reference implementation: `../jellyfin-concert-radar` (same author, same
plugin shape, same Plugin Pages menu integration). When in doubt about a Jellyfin plugin
pattern, look there first.

## Workflow: Spec Kit

Specs live in `specs/`, driven by the `speckit-*` skills in `.claude/skills/`. Order:
`/speckit-constitution` → `/speckit-specify` → `/speckit-grill-me` → `/speckit-plan` →
`/speckit-tasks` → `/speckit-implement`. Do not add behaviour outside a spec'd feature.

Installed extensions (`specify extension list`; hooks in `.specify/extensions.yml`):

- **grill** — `/speckit-grill-me` replaces `/speckit-clarify`: one question at a time until
  the spec has no open decision. `/speckit-grill-with-docs` also syncs `docs/domain_knowledge/`.
- **tdd** — `before_implement` hook is mandatory: `/speckit-tdd-run` drives red-green-refactor
  from `specs/<feature>/tdd/test-list.md` before `/speckit-implement` writes anything.
  Stack commands live in `.specify/memory/tdd-profile.md`; re-run `/speckit-tdd-setup refresh`
  when the test stack changes.
- **worktrees** — `after_specify` offers a nested `.worktrees/<branch>` worktree. `auto_create`
  is off because Orca already isolates each agent in its own worktree; decline unless you
  need a second one.
- **tasks-to-project** — one-way mirror of `tasks.md` onto a public GitHub Project (v2) so
  third parties can see the roadmap. The repo is the source of truth; `sync` never writes back
  to `tasks.md`. Config: `.specify/extensions/tasks-to-project/tasks-to-project-config.yml`
  (`project.number` must be set before the `after_tasks` hook can publish).

## Conventions (inherited from concert-radar)

- Target `net9.0`, `Jellyfin.Controller`/`Jellyfin.Model` pinned to the server version (10.11.11),
  `ExcludeAssets=runtime` in the plugin project, not in the test project.
- Plugin GUID `b8a15db8-e368-42c4-9048-390faf0094db` is frozen. Guarded by `PluginSanityTests`.
- `PluginConfiguration` collections are `List<T>`, never seeded in the constructor (XmlSerializer
  duplicates entries on round-trip).
- Web pages are embedded resources under `Web/`. `admin.html` is a full Jellyfin config page.
  `user-view.html` is a fragment served by `UserViewController` for Plugin Pages.
- Tests: xunit + NSubstitute. No live network calls. Recorded responses go in `tests/fixtures/`,
  scrubbed of keys and PII.
- `TreatWarningsAsErrors` is on. Keep it on.

## Build

Needs .NET 9 SDK. `dotnet build --configuration Release && dotnet test`.
Packaging: `jprm plugin build . --version X.Y.Z --output ./artifacts` (CI does this on `v*` tags).
