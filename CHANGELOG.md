# Changelog

All notable changes to this project are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## Unreleased

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
  `https://alphagit.github.io/jellyfin-new-releases/manifest.json`, kept current by the release
  workflow, so the plugin can be installed from the Jellyfin catalogue rather than sideloaded.

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
