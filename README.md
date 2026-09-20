# Jellyfin New Releases

A Jellyfin plugin that tracks releases by the **library artists** in your music library that the
library does not contain yet — "new" as in *new to your library*. It reads two open catalogue
sources, decides ownership by comparing track lists, and shows the result in the web client.

## What it does

- A daily **Refresh** (scheduled task "Refresh new releases", category "New Releases") reads the
  music library, matches each library artist at **MusicBrainz** and **Deezer**, and pulls the
  artist's whole catalogue.
- Listings of the same **Release** from both sources are merged. MusicBrainz identity and typing
  are canonical when MusicBrainz lists the release.
- Ownership is decided per release by comparing the library album's track titles with the
  best-overlapping Official **Edition**: **Missing** (library has none of it), **Incomplete**
  (library album lacks tracks, listed with the missing titles), or In library (not listed).
  Releases dated in the future are shown as **Upcoming**.
- Users open **New Releases** from the web client's side menu (through the optional
  [Plugin Pages](https://github.com/IAmParadox27/jellyfin-plugin-pages) integration), filter by
  artist, type, state and date range, and can **Ignore** a release or mark it **Have it**. Those
  decisions move it to the shared **Archive**; **Restore** undoes them. Decisions are shared by
  every user of the server.
- Users see releases only for artists in music libraries they are allowed to access.

## What leaves the server

Only what a source needs: artist names and public identifiers (MusicBrainz ids, Deezer ids).
Requests carry the `User-Agent` `JellyfinNewReleases/<version> ( <contact> )`, with the contact
you configure. No library contents, user identities or usage data are sent. Both sources are
open APIs; the plugin enforces their request rates and a daily budget per source, and stops
using a source for six hours after five consecutive failures.

## Configuration (Dashboard → Plugins → New Releases)

| Field | Default | Meaning |
| --- | --- | --- |
| MusicBrainz / Deezer | both on | Sources to read |
| Release types | Album, EP | Types listed; a release counts only when its primary and every secondary type are enabled |
| Released since | empty | Optional cutoff (`yyyy-MM-dd`); undated releases are always kept |
| Contact for User-Agent | empty | Your e-mail or URL for the sources' operators |

The page also shows each source's health, the last and next refresh, artists processed,
releases found, and the **Unmatched artists** list with a link to each artist's Jellyfin page.
To fix an unmatched artist, set its MusicBrainz artist ID in Jellyfin's metadata editor or
`artist.nfo`; the plugin picks it up on the next refresh.

Admin actions: **Run now**, **Purge release data** (keeps the Archive), **Clear Archive**. The
refresh interval is changed in Dashboard → Scheduled Tasks, not in the plugin.

## Requirements

Jellyfin 12. Older servers are not supported and are not offered the plugin.

The plugin works on its own: the configuration page, the API and the daily refresh need nothing
else installed.

**The New Releases entry in the side menu is a different matter, and on Jellyfin 12 it currently
does not appear at all.** It is published by [Plugin Pages](https://github.com/IAmParadox27/jellyfin-plugin-pages),
a separate plugin, which in turn needs
[File Transformation](https://www.iamparadox.dev/jellyfin/plugins/manifest.json) to load its
browser script — a chain of two third-party plugins:

```
New Releases  ->  Plugin Pages 3.0.0.0+  ->  File Transformation
```

With both installed, this plugin's half works: the page is registered and Plugin Pages serves it
to the client. The entry still does not render, because Plugin Pages 3.0.1.0 only initialises when
it finds the pre-12 web client's navigation drawer, and Jellyfin 12 replaced the client with a
React application that has no such element. That is a Plugin Pages issue, not one this plugin can
fix or work around. Details and evidence:
[`docs/real-server-install-0.1.0.md`](docs/real-server-install-0.1.0.md) finding 3.

Until that is resolved, reach the plugin through Dashboard → Plugins → New Releases, and drive the
refresh from Dashboard → Scheduled Tasks → Refresh new releases.

Data lives in `<data>/newreleases/newreleases.db` (SQLite).

## Install

Add the plugin repository to your server, then install from the catalogue:

1. Open Dashboard → Plugins → Repositories.
2. Add the repository URL: `https://<owner>.github.io/<repository>/manifest.json`, where
   `<owner>` and `<repository>` are this project's GitHub owner and repository name. For this
   repository that resolves to the address shown on its GitHub Pages settings page; a fork
   publishes to its own.
3. Install New Releases from Dashboard → Plugins → Catalogue, and restart the server.

## Build and test

Needs the .NET 10 SDK.

```bash
dotnet build --configuration Release
dotnet test --configuration Release
jprm plugin build . --version X.Y.Z --output ./artifacts   # packaging (CI does this on v* tags)
```

Tests make no network calls: source responses are recorded fixtures under `tests/fixtures/`.

## Development

Specs live in `specs/` (Spec Kit). Every behaviour is test-driven; the evidence per feature is in
`specs/<feature>/tdd/cycle-log.md`. Sibling project and reference implementation:
[jellyfin-concert-radar](https://github.com/AlphaGit/jellyfin-concert-radar).
