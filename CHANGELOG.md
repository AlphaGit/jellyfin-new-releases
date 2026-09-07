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
