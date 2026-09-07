# Jellyfin New Releases — Domain Context

Canonical vocabulary for the New Releases plugin: what each concept means, where its boundary
is, and the exact label it carries in the user interface, the admin page, and code. Specs,
plans, UI strings, and identifiers use these terms verbatim.

## Canonical terms

| Term | Meaning | Where it appears | _Avoid_ |
| --- | --- | --- | --- |
| **New Releases** | The plugin and its user-facing page: releases by library artists that the library does not contain, read as "new to your library". | Plugin name, side-menu entry, page title, repository, roadmap board. | Release Radar, Missing Releases |
| **Source** | An external release database the plugin reads: MusicBrainz, Deezer. | Admin "Sources" section; per-row source links. | catalogue, provider, adapter (in UI) |
| **Catalogue** | An artist's whole body of releases as known to the sources. Not a system, not a UI word. | Prose only. | — |
| **Library artist** | An artist credited as album artist of at least one album in a Jellyfin music library. Featured-credit-only artists are not library artists. | Admin counts ("Library artists"). | tracked artist, followed artist |
| **Unmatched** | A library artist for which no source identity could be established with confidence. Fixed in Jellyfin by setting the MusicBrainz artist ID, never in the plugin. | Admin "Unmatched artists" section. | unresolved, not found |
| **Release** | A musical work as a whole (an album or EP). MusicBrainz "release group", Deezer "album". All editions belong to one Release. | Row title in the list. | album (as the generic word), release group |
| **Edition** | One concrete version of a Release (a pressing, a deluxe or regional variant). Carries the track list used by the ownership check. MusicBrainz "release". Only Official editions count; Promotion, Bootleg, and Pseudo-release editions are ignored. | Incomplete rows: "compared with <edition> from <source>". | version, pressing, release (MusicBrainz sense) |
| **Source entry** | One source's record of a Release: source identifier, source page link, last complete refresh that returned it. A Release exists while it has at least one Source entry. | Never as a word in UI; rendered as the row's source links. Code: `SourceEntry`. | listing, reference, sighting |
| **Release type** | The kind of Release: Album, EP, Single, Compilation, Live, Remix, Soundtrack. Albums and EPs are on by default. Mapping and inclusion rules: [release-types.md](release-types.md). | Admin type checkboxes; "Type" filter values; row badge. | record type, primary type, secondary type (in UI) |
| **Missing** | State: the library has no album for this Release. | State badge; "State" filter value. | absent, wanted |
| **Incomplete** | State: the library has an album for this Release but not every track of the best-matching Edition. The missing track titles are shown. | State badge; "State" filter value. | partial |
| **Upcoming** | State: the Release is dated after today. Shown in its own group above the newest year. | State badge; "State" filter value; group heading. | announced, pre-release |
| **Undated** | Group of Releases for which no source gives any date. Always the last group. Year-only Releases are not Undated; they sit in their year after fully dated ones. | Group heading "Undated". | unknown date, no date |
| **In library** | State: the library has every track of the best-matching Edition, or a user said Have it. Not listed. | Explanatory text only (e.g. in the Archive). | owned (in UI), have |
| **Owned** | Internal name for the automatic check's positive result. | Code and plan only, never UI. | — |
| **Ignore** | User action: this Release is not wanted. Moves it to the Archive for every user. | Row button "Ignore". | dismiss, hide, not interested |
| **Have it** | User action: this Release counts as In library regardless of the automatic check. Moves it to the Archive for every user. | Row button "Have it". | mark as owned, already own |
| **Archive** | The set of Releases with an Ignore or Have-it decision. Shared server-wide, browsable, filtered by the viewer's library access. | Tab or link "Archive" on the New Releases page. | hidden, hidden list, ignored list, dismissed |
| **Restore** | User action: undo Ignore or Have it; the Release returns to the list. | Button "Restore" in the Archive. | undo, unhide, unarchive |
| **Refresh** | The plugin's scheduled task: enumerate library artists, query sources, recompute ownership. Runs daily by default; interval edited in Jellyfin's Scheduled Tasks. | Jellyfin task "Refresh new releases", category "New Releases"; admin status ("Last refresh", "Next refresh"). | scan, sync, update |
| **Run now** | Admin action: start a Refresh immediately. | Admin button "Run now". | scan now, sync now, fetch |
| **Purge release data** | Admin action: delete all stored Releases, Source entries, and ownership results. Leaves the Archive intact. | Admin button "Purge release data". | purge, clear cache, reset |
| **Clear Archive** | Admin action: delete all Ignore and Have-it decisions. Leaves release data intact. | Admin button "Clear Archive". | clear user decisions, reset decisions |
| **Released since** | Optional admin cutoff: only Releases dated on or after this day are considered. Default unrestricted. | Admin date field "Released since". | lookback, window, since date |
| **Complete / Partial / Failed** | Outcome of one source fetch for one artist within a Refresh. Only a Complete fetch may remove a Source entry. | Admin run status. | success/error (as the only words) |

## Domain boundaries

- The plugin reads the Jellyfin library and never writes to it. Artist identity belongs to
  Jellyfin; an Unmatched artist is corrected there.
- Name-based artist matching is conservative: a source's candidate is accepted only when it is
  unambiguous (MusicBrainz score ≥ 85 with no runner-up within 5 points) or corroborated by the
  library (Deezer: exact normalized name and a release title matching an album the library
  already holds). Otherwise the artist is Unmatched at that source.
- Ownership is judged per Release, by comparing the library album's tracks with the
  best-matching Edition's track list, using exact equality of normalized titles (see
  [title-normalization.md](title-normalization.md)). A user's Have it or Ignore decision overrides the
  automatic result and survives later refreshes and purges.
- The list and the Archive are one shared, server-wide set. What each viewer sees is filtered
  by the Jellyfin music libraries that viewer may access.
- A Release leaves the list only when the library has it, its artist stops being a library
  artist, or no Source entry remains. Source entries are removed only by a Complete fetch that
  omits them.

## UI conventions

- All UI text is English only; there is no translation layer. Labels use the canonical term
  verbatim, in sentence case as in the Jellyfin web client ("Purge release data", "Run now").
  Proper terms keep their capital: "Archive", "Have it".
- The list is grouped by year, newest first, with "Upcoming" above and "Undated" below.
- Filter values and badges use the state and type names exactly: Missing, Incomplete,
  Upcoming; Album, EP, Single, Compilation, Live, Remix, Soundtrack, Bootleg.
- Fixed sentences: empty state "No data yet. New Releases is waiting for its first refresh.";
  stale data "Last refreshed <relative time> ago." (relative time, never a raw timestamp).
- Code identifiers are the PascalCase form of the term: `LibraryArtist`, `SourceEntry`,
  `ReleaseState.Incomplete`, `HaveIt`, `Archive`.

## Related documents

- [ADR-0001 Ownership is decided per Release by track list](adr/0001-ownership-per-release-by-track-list.md)
- [ADR-0002 v1 sources are MusicBrainz and Deezer](adr/0002-sources-musicbrainz-and-deezer.md)
- [Release types](release-types.md) — how source types map to ours, inclusion rule, displayed badge.
- [Title normalization](title-normalization.md) — the deterministic transformation behind every title comparison.
- [Feature spec: Track New Releases](../../specs/001-track-new-releases/spec.md) — the feature this vocabulary was settled for.
