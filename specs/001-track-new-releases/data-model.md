# Data Model: Track New Releases

Phase 1 output. Vocabulary follows `docs/domain_knowledge/CONTEXT.md`; code identifiers are the
PascalCase form of each term. Storage is SQLite (`{DataPath}/newreleases/newreleases.db`),
created by `Storage/Migrations/001_initial.sql`. All timestamps are ISO-8601 UTC text; all ids
that Jellyfin owns are GUID text.

## Entity overview

```text
library_artist 1──* artist_source            (match + fetch state per source)
library_artist 1──* release 1──* source_entry (one per source that lists it)
                       release 1──* edition   (track lists, per source release id)
                       release ·──? decision  (joined on artist_key + normalized_title)
source_state   (one row per source: health, budget, cooldown)
refresh_run    (one row per run)
```

Library albums and tracks are **not** stored: `LibraryScanner` rebuilds a `LibrarySnapshot` each
run from Jellyfin, and ownership results are written onto `release`.

## Tables

### library_artist — Library artist (FR-001)

| Column | Type | Notes |
| --- | --- | --- |
| id | INTEGER PK | surrogate |
| artist_key | TEXT UNIQUE NOT NULL | MBID when known, else `name:<normalized name>`; the merge and Archive key |
| jellyfin_id | TEXT NOT NULL | `MusicArtist` item id owning albums (link target) |
| name | TEXT NOT NULL | display name |
| mbid | TEXT | from `MusicBrainzArtist` / `MusicBrainzAlbumArtist` provider id |
| library_ids | TEXT NOT NULL | JSON array of collection folder GUIDs holding its albums (FR-007) |
| album_count | INTEGER NOT NULL | for the admin page |
| last_refreshed_at | TEXT | set when every enabled source was attempted in a run; rotation order key |

Index: `ix_library_artist_rotation (last_refreshed_at, name)`.
Removal: artists absent from the snapshot are deleted; `ON DELETE CASCADE` removes their
`artist_source`, `release`, `source_entry`, `edition` rows (FR-014). Decisions are untouched.

### artist_source — match and fetch state per (artist, source) (FR-002, FR-014)

| Column | Type | Notes |
| --- | --- | --- |
| library_artist_id | INTEGER FK CASCADE | |
| source | TEXT NOT NULL | `musicbrainz` \| `deezer` |
| status | TEXT NOT NULL | `Pending` \| `Matched` \| `Unmatched` |
| source_artist_id | TEXT | MBID or Deezer artist id when Matched |
| unmatched_reason | TEXT | e.g. `no result`, `ambiguous (score 88 vs 86)`, `no corroborating album` |
| resume_offset | INTEGER NOT NULL DEFAULT 0 | next catalogue page offset; 0 after a Complete fetch |
| pass_run_id | INTEGER | run in which the current paging pass started (kept across Partial outcomes); NULL when no pass is open. Pruning after a Complete uses this run, so entries seen on an earlier page of a resumed pass survive |
| last_outcome | TEXT | `Complete` \| `Partial` \| `Failed` (admin run status) |
| last_complete_at | TEXT | last run in which this source's catalogue fetch completed |
| last_error | TEXT | redacted message |
| PK (library_artist_id, source) | | |

Rules: only `last_outcome = Complete` may prune `source_entry` rows for this pair. `Unmatched`
artists are retried on every run (spec: "retried on a later refresh").

### release — Release (FR-006, FR-006a, FR-005, FR-005a)

| Column | Type | Notes |
| --- | --- | --- |
| id | INTEGER PK | surrogate used by the API (`/releases/{id}/ignore`) |
| library_artist_id | INTEGER FK CASCADE | |
| normalized_title | TEXT NOT NULL | album normalization; `UNIQUE (library_artist_id, normalized_title)` |
| title | TEXT NOT NULL | display title from the canonical entry |
| canonical_source | TEXT NOT NULL | `musicbrainz` if a MusicBrainz entry exists, else `deezer` (R17) |
| canonical_source_id | TEXT NOT NULL | release-group MBID or Deezer album id |
| primary_type | TEXT NOT NULL | `Album` \| `EP` \| `Single` \| `Other` (unmapped → always excluded) |
| secondary_types | TEXT NOT NULL | JSON array ⊂ {`Compilation`,`Live`,`Remix`,`Soundtrack`,`Other`} |
| release_date | TEXT | `YYYY`, `YYYY-MM`, `YYYY-MM-DD`, or NULL (R16) |
| date_sort | TEXT | padded `YYYY-MM-DD` with `00` for missing parts; NULL when undated |
| first_seen_at | TEXT NOT NULL | |
| last_seen_at | TEXT NOT NULL | max over its entries |
| ownership_state | TEXT NOT NULL | `Missing` \| `Incomplete` \| `Owned` (internal name of the automatic positive) |
| match_method | TEXT | `Identifier` \| `Title` \| NULL when Missing |
| library_album_id | TEXT | Jellyfin `MusicAlbum` id when a candidate exists |
| compared_edition_id | INTEGER FK edition | edition used for the track comparison |
| missing_tracks | TEXT NOT NULL DEFAULT '[]' | JSON array of source track titles absent from the library |
| ownership_checked_at | TEXT | |

Indexes: `ix_release_artist (library_artist_id)`, `ix_release_sort (date_sort DESC)`.

Derived (read time, not stored):
- **Displayed type**: first present of Live, Remix, Soundtrack, Compilation in `secondary_types`,
  else `primary_type` (release-types.md).
- **Included**: `primary_type` enabled AND every secondary enabled AND no `Other` anywhere
  AND (`ReleasedSince` empty OR `date_sort ≥ ReleasedSince` OR undated) — undated releases are
  never cut by "released since".
- **State** shown to users: `Upcoming` when `date_sort > today (UTC)`; else `ownership_state`
  (`Missing`, `Incomplete`). `Owned` rows are never listed ("In library").
- **Archived**: a `decision` row exists for `(artist_key, normalized_title)`.

### source_entry — Source entry (FR-006a, FR-014)

| Column | Type | Notes |
| --- | --- | --- |
| release_id | INTEGER FK CASCADE | |
| source | TEXT NOT NULL | |
| source_release_id | TEXT NOT NULL | release-group MBID or Deezer album id; `UNIQUE (source, source_release_id)` |
| url | TEXT NOT NULL | source page link shown on the row |
| source_title | TEXT NOT NULL | title as this source spells it |
| source_primary_type / source_secondary_types / source_date | TEXT | this source's values, so canonical can be recomputed when an entry is removed |
| last_seen_run_id | INTEGER NOT NULL | run that last returned it |
| PK (release_id, source) | | one entry per source per release |

Prune rule: after a **Complete** fetch of (artist, source) in run *R*, delete this pair's entries
where `last_seen_run_id < R`; then delete releases with no entries.

### edition — Edition (FR-005, ADR-0001)

| Column | Type | Notes |
| --- | --- | --- |
| id | INTEGER PK | |
| release_id | INTEGER FK CASCADE | |
| source | TEXT NOT NULL | |
| source_edition_id | TEXT NOT NULL | MusicBrainz release MBID or Deezer album id; `UNIQUE (source, source_edition_id)` |
| title | TEXT NOT NULL | shown as "compared with <title> from <source>" |
| status | TEXT NOT NULL | always `Official` (others are never stored) |
| tracks | TEXT NOT NULL | JSON array of normalized track titles (track normalization) |
| fetched_at | TEXT NOT NULL | |

Fetched only for releases with a library album candidate and only once per
`(source, source_edition_id)` (R11).

### decision — Ignore / Have it (FR-005b, FR-013, FR-016)

| Column | Type | Notes |
| --- | --- | --- |
| artist_key | TEXT NOT NULL | = `library_artist.artist_key` |
| normalized_title | TEXT NOT NULL | |
| kind | TEXT NOT NULL | `Ignore` \| `HaveIt` |
| user_id | TEXT NOT NULL | Jellyfin user who decided |
| decided_at | TEXT NOT NULL | |
| PK (artist_key, normalized_title) | | one decision per release; a new decision replaces the old |

The **Archive** is the set of releases with a decision row. **Restore** deletes the row.
**Clear Archive** deletes all rows. **Purge release data** leaves this table alone; when the same
release is fetched again its decision applies immediately (join by natural key).

### source_state — Source health (FR-011, FR-012)

| Column | Type | Notes |
| --- | --- | --- |
| source | TEXT PK | |
| consecutive_failures | INTEGER NOT NULL DEFAULT 0 | |
| cooldown_until | TEXT | set when failures ≥ threshold; source skipped until then |
| calls_today | INTEGER NOT NULL DEFAULT 0 | |
| calls_day | TEXT | UTC date the counter belongs to; counter resets when it changes |
| next_allowed_at | TEXT | from `Retry-After` |
| last_error | TEXT | redacted |
| last_success_at | TEXT | |

Health shown to the admin: `CoolingDown` when `cooldown_until > now`; `Failing` when
`consecutive_failures > 0`; else `Ok`. Disabled sources show `Disabled` from configuration.

### refresh_run — Refresh run (FR-012, FR-015)

| Column | Type | Notes |
| --- | --- | --- |
| id | INTEGER PK | |
| started_at / ended_at | TEXT | `ended_at` NULL while running or if the process died |
| artists_processed | INTEGER NOT NULL DEFAULT 0 | artists for which at least one source was attempted |
| releases_found | INTEGER NOT NULL DEFAULT 0 | source entries upserted |
| editions_fetched | INTEGER NOT NULL DEFAULT 0 | |
| errors | INTEGER NOT NULL DEFAULT 0 | |
| outcome | TEXT | `Completed` \| `Cancelled` \| `Failed` |

`lastRefreshedAt` for the list = max `ended_at` where `outcome = Completed`.
`hasCompletedRefresh` = such a row exists.

### schema_version

`(version INTEGER PK, applied_at TEXT)`; `Database` applies embedded `NNN_*.sql` files with
`version > MAX(version)` in one transaction each.

## In-memory records (not persisted)

- **LibrarySnapshot**: `LibraryArtistSnapshot[]` — `ArtistKey`, `JellyfinId`, `Name`, `Mbid`,
  `LibraryIds`, `Albums: LibraryAlbumSnapshot[]` — `JellyfinId`, `Title`, `NormalizedTitle`,
  `MusicBrainzReleaseId`, `MusicBrainzReleaseGroupId`, `NormalizedTrackTitles`.
- **ArtistMatch**: `Matched(SourceArtistId)` | `Unmatched(Reason)`.
- **CataloguePage**: `Items: CatalogueItem[]`, `NextOffset: int?` (null = last page),
  `Total: int`.
- **CatalogueItem**: `SourceReleaseId`, `Title`, `Url`, `PrimaryType`, `SecondaryTypes`,
  `Date` (source string, may be null/partial).
- **EditionTrackList**: `SourceEditionId`, `Title`, `NormalizedTrackTitles`.
- **OwnershipResult**: `State`, `MatchMethod`, `LibraryAlbumId`, `EditionId`, `MissingTracks`.

## Ownership algorithm (`OwnershipMatcher`, pure function)

Inputs: one release with its editions, the artist's `LibraryAlbumSnapshot[]`.

1. Candidate album: first album whose `MusicBrainzReleaseGroupId == canonical_source_id`
   (MusicBrainz canonical) or whose `MusicBrainzReleaseId` equals any stored MusicBrainz
   `edition.source_edition_id` → `Identifier`; else first album with equal `NormalizedTitle` →
   `Title`; else → `Missing`.
2. If the candidate exists but the release has no stored editions yet → editions are fetched by
   the caller (all sources listing the release), then step 3.
3. For each edition: `matched = |edition.tracks ∩ album.tracks|`, `missing = edition.tracks \
   album.tracks`. Pick max `matched`, then min `|missing|`, then `musicbrainz` before `deezer`,
   then lowest `source_edition_id` (deterministic).
4. `missing.Count == 0` → `Owned`; else `Incomplete` with `missing`, the edition, and its source.
5. No edition has any tracks (source gave none) → `Incomplete` with empty `missing` is wrong;
   treat as `Owned` by album presence and log once (`// ponytail:` ceiling: trackless edition).

Have it / Ignore are not inputs here; they are applied by the Archive join at read time
(FR-005b: a `HaveIt` decision keeps the release archived whatever this algorithm says).

## State transitions

```text
Release visibility (read time):
  decision exists ─────────────────────────────► Archive (Ignore | Have it)
  else ownership Owned ────────────────────────► In library (not shown)
  else not Included by type / released-since ──► not shown
  else date_sort > today ──────────────────────► listed as Upcoming
  else ───────────────────────────────────────► listed as Missing | Incomplete

Release lifetime:
  first source entry upserted ─► exists
  Complete fetch omits it at a source ─► that entry removed; canonical recomputed
  zero entries ─► deleted          artist leaves library ─► deleted (cascade)
  Purge release data ─► release/source_entry/edition truncated; decision, library_artist, artist_source kept
  Clear Archive ─► decision truncated
```

## Validation rules

- `artist_key`, `normalized_title`, `source`, `source_release_id`, `url` non-empty.
- `url` must be an absolute `https` URL whose host is `musicbrainz.org` or `www.deezer.com`
  (guards against a compromised upstream injecting links).
- `primary_type`/`secondary_types` only from the closed vocabularies above; unknown source values
  map to `Other`, never dropped silently.
- `ReleasedSince` in configuration must parse as `yyyy-MM-dd` or be empty; otherwise treated as empty and logged.
