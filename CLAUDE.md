# Jellyfin New Releases — project notes

Jellyfin plugin. Tracks new releases by library artists that the library does not contain yet.
Sibling project and reference implementation: `../jellyfin-concert-radar` (same author, same
plugin shape, same Plugin Pages menu integration). When in doubt about a Jellyfin plugin
pattern, look there first.

## Workflow: Spec Kit

Specs live in `specs/`, driven by the `speckit-*` skills in `.claude/skills/`. Order:
`/speckit-constitution` → `/speckit-specify` → `/speckit-clarify` → `/speckit-plan` →
`/speckit-tasks` → `/speckit-implement`. Do not add behaviour outside a spec'd feature.

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
