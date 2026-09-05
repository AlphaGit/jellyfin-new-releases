# Jellyfin New Releases

A Jellyfin server plugin that tracks new releases by the artists in your music library that the
library does not yet contain, and shows them inside the Jellyfin web client.

Sibling of [jellyfin-concert-radar](../jellyfin-concert-radar): same plugin shape, same
[Plugin Pages](https://github.com/IAmParadox27/jellyfin-plugin-pages) hamburger-menu entry.

**Status:** scaffold only. Behaviour is being specified with [Spec Kit](https://github.com/github/spec-kit)
under `specs/`. Nothing fetches releases yet.

## Layout

```
build.yaml                                  JPRM packaging manifest (plugin GUID, ABI, artifacts)
Jellyfin.Plugin.NewReleases.sln
src/Jellyfin.Plugin.NewReleases/
  Plugin.cs                                 Entry point; registers the Plugin Pages menu entry
  PluginServiceRegistrator.cs               DI registrations (empty until the spec lands)
  Configuration/PluginConfiguration.cs      XML-serialized settings (empty until the spec lands)
  Api/UserViewController.cs                 Serves Web/user-view.html to Plugin Pages
  Web/admin.html                            Dashboard → Plugins config page
  Web/user-view.html                        User-facing fragment (hamburger menu → New Releases)
tests/Jellyfin.Plugin.NewReleases.Tests/    xunit + NSubstitute; no live network calls
tests/fixtures/                             Recorded, scrubbed HTTP responses
.specify/  .claude/skills/speckit-*/        Spec Kit workflow (constitution → specify → plan → tasks)
.github/workflows/                          build (PR/main) and package (v* tags, JPRM)
```

## Requirements

- Jellyfin 10.11.x (targets `Jellyfin.Controller` 10.11.11).
- .NET 9 SDK to build.
- Optional, for the user-facing menu entry: `File Transformation` + `Plugin Pages` from
  `https://www.iamparadox.dev/jellyfin/plugins/manifest.json`. Without them the admin page still works.

## Build

```bash
dotnet build --configuration Release
dotnet test
```

## Open design questions (for the spec)

- Release source: MusicBrainz release-groups is the obvious no-key candidate. Others need keys or scraping.
- "Library does not contain" match rule: by MusicBrainz release-group ID, or by normalized album title?
- Artist-page section: Jellyfin has no plugin hook inside the artist detail view. Injecting one needs the
  `File Transformation` plugin to patch the web client bundle. Decide whether the menu view is enough for v1.
