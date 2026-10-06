# Changelog

All notable changes to this project are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## Unreleased

### Fixed

- **A caller authenticated by API key gets a clean `401`.** The release list, the artist list and
  the Ignore, Have it and Restore actions need a signed-in user. Called with an API key, they
  answered `400` and wrote an error to the server log; they now refuse the request as
  unauthenticated and log nothing. Signed-in users see no change.

## 0.2.0 — 2026-10-04

### Upgrading from 0.1.x — once

- **Remove the copy left under the old name.** The plugin was published as "Jellyfin New
  Releases" and is now "New Releases". Jellyfin replaces an older copy only when the names match,
  so after this update the server's plugin directory holds a folder named
  `Jellyfin New Releases_<version>` beside the new `New Releases_0.2.0.0`. Stop the server, delete
  the `Jellyfin New Releases_<version>` folder and nothing else, and start it again. No later
  upgrade needs this.
- **Replace the repository address.** The catalogue is no longer served from GitHub Pages. In
  Dashboard → Plugins → Repositories, replace the old `github.io` address with
  `https://raw.githubusercontent.com/AlphaGit/jellyfin-new-releases/main/repo/manifest.json`.
  A server left on the old address does not see 0.2.0 or any later version.

### Changed

- **The Artist filter is a text field with suggestions.** Type part of a library artist's name
  and pick from the browser's own suggestions. The filter applies only to a name that matches a
  library artist; clearing the field, or Clear, removes it. It stays operable by keyboard.
- **"Ignore", "Have it" and "Restore" share one button style.** "Ignore" and "Have it" have the
  same width and line up on each card.
- **The source link is readable in the dark theme.** Its text has a contrast of at least 4.5:1
  against the card, and it still reads as a link.

### Added

- **Each release card shows the record's cover.** The cover comes from Deezer, then from the
  Cover Art Archive when Deezer has none, and a neutral placeholder of the same size shows when
  neither loads. Covers load only as cards come into view, directly from those image hosts; the
  plugin server does not fetch, store or proxy them.

## 0.1.1 — 2026-09-20

### Fixed

- **Both embedded pages were unusable on Jellyfin 12 and now work.** The plugin never stated how
  its own JSON responses should be named, so it inherited the host's default; on Jellyfin 12 that
  default writes PascalCase, which neither page could read. The New Releases view showed "No data
  yet" with releases stored, and every value on the administrator page showed a dash. Each endpoint
  now declares its naming with `[Produces(JsonDefaults.CamelCaseMediaType)]`, so a later change to
  the host's default cannot silently blank the pages again.

### Changed

- Routes renamed to the convention Jellyfin uses for its own endpoints: PascalCase segments,
  multi-word segments concatenated, and no `api` segment. `Plugins/NewReleases/api/admin/run-now`
  becomes `Plugins/NewReleases/Admin/RunNow`, and so on for every route;
  `Plugins/NewReleases/UserView` is unchanged. The prefix now has one authoritative source, and
  `docs/http-surface.md` records the convention. **No installed client is affected**: only the
  plugin's own pages call these paths.

## 0.1.0 — 2026-09-20

### Added

- Track New Releases: a daily Refresh matches library artists at MusicBrainz and Deezer, merges
  both sources into one release per artist and normalized title, and decides ownership by track
  list against the best-overlapping Official edition.
- New Releases view (Plugin Pages): Missing, Incomplete and Upcoming releases grouped by year with
  artist, type, date, state and source links; filters by artist, type, state and date range;
  keyboard operable with announced actions.
- Ignore / Have it / Restore decisions shared server-wide, kept in an Archive that survives
  Purge release data.
- Admin page: sources, release types, "released since" cutoff, User-Agent contact, source health,
  run status, unmatched artists with fix-it links, Run now, Purge release data, Clear Archive.
- Per-user library access on the list and the Archive.
- Per-source request rate, daily budget and cooldown; identifying User-Agent.
- A published plugin repository at
  `https://raw.githubusercontent.com/AlphaGit/jellyfin-new-releases/main/repo/manifest.json`,
  kept current by the release workflow, so the plugin can be installed from the Jellyfin catalogue
  rather than sideloaded.

### Removed

- Support for Jellyfin 10.11.x. One package is published, declaring Jellyfin 12 as the server
  version it supports, so no older server is offered a build it cannot load.

### Changed

- **The plugin now runs on Jellyfin 12, and only on Jellyfin 12.** It targets `net10.0` and pins
  `Jellyfin.Controller`, `Jellyfin.Model`, `Jellyfin.Data` and `Jellyfin.Database.Implementations`
  to `12.0.0`. Nothing a user sees behaves differently.
- The New Releases page entry is registered through Plugin Pages' own registration interface at
  server start and withdrawn at shutdown, instead of by writing into that plugin's configuration
  file. Plugin Pages 3.0.0.0 is the minimum for the menu entry; without it, or with an older
  build, the plugin still loads and the admin page, API and refresh all still work.
- The New Releases page now states when the releases were last checked, not when a refresh last
  ran: "Releases last checked <relative time> ago." A refresh that reaches no source no longer
  makes the data look fresh, and the age keeps growing until a source actually answers.
- The empty state now follows whether any release is stored, so it appears again after
  Purge release data even though refresh runs are still on record.
- The admin page shows the data age beside the last run, so the two can be seen to diverge.
