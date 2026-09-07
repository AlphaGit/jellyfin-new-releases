# Research: Track New Releases

Phase 0 output. Every "NEEDS CLARIFICATION" from the Technical Context is resolved here as a
numbered decision. Verified against Jellyfin 10.11.11 assemblies in the local NuGet cache and
against the sibling project `../jellyfin-concert-radar` (same author, same plugin shape).

## R1. SQLite provider version

- **Decision**: `Microsoft.Data.Sqlite` **9.0.19**, pinned exactly.
- **Rationale**: latest stable on the 9.0 line (matches the `net9.0` host). Published 2026-08-11
  on nuget.org, 26 days before adoption; satisfies the 7-day rule and constitution VI. MIT licence.
  The 10.0.x line (latest 10.0.11, 2026-08-11) targets .NET 10 and is not used by the host.
- **Alternatives**: `9.0.*` floating (Concert Radar) — rejected, constitution requires a pin.
  `System.Data.SQLite` — rejected, not the constitution's named provider. Files/JSON — rejected,
  relational queries (joins for Archive, filters, per-user visibility) are the core of the list.

## R2. MusicBrainz endpoints

- **Decision** (all `fmt=json`, base `https://musicbrainz.org/ws/2/`):
  - Artist by name: `artist?query=artist:"<name>"&limit=5` → `artists[]{id,name,score}`. Accept
    only if top `score ≥ 85` and no second result within 5 points (FR-002).
  - Catalogue: `release?artist=<mbid>&status=official&inc=release-groups&limit=100&offset=N` →
    `release-count`, `releases[]{id,title,status,date,release-group{id,title,primary-type,
    secondary-types[],first-release-date}}`. Group by `release-group.id`. Only Official editions
    are returned, so "has at least one Official edition" is satisfied by presence.
  - Editions with tracks (only for releases with a library album candidate):
    `release?release-group=<rgid>&status=official&inc=recordings+media&limit=25&offset=N` →
    `releases[]{id,title,date,country,media[]{tracks[]{title}}}`.
  - Source link: `https://musicbrainz.org/release-group/<rgid>`.
- **Rationale**: browsing *releases* with `inc=release-groups` yields the Official rule, release
  group identity, types, and first-release date in one paged stream; browsing release groups
  gives no status. Track lists are a separate call so the request count scales with library
  albums, not catalogue size (spec edge case; ADR-0001).
- **Alternatives**: `release-group?artist=` browse (no status → can't apply the Official rule);
  per-release-group lookups (N+1).
- **Limits**: 1 request/second, `User-Agent` mandatory (`Name/Version ( contact )`). 503 with
  `Retry-After` on overrun.

## R3. Deezer endpoints

- **Decision** (base `https://api.deezer.com/`, no credentials):
  - Artist by name: `search/artist?q=<name>&limit=25` → `data[]{id,name,link,nb_album}`.
    Candidates with `normalize(name) == normalize(artist)`; accept the first whose first albums
    page contains a title matching a library album title for that artist (FR-002).
  - Catalogue: `artist/<id>/albums?index=N&limit=100` → `data[]{id,title,link,record_type,
    release_date,explicit_lyrics}`, `total`, `next`. `record_type ∈ album|ep|single|compile`.
    `release_date` `0000-00-00` means unknown → Undated.
  - Tracks: `album/<id>/tracks?limit=100` → `data[]{title}`, `next`. One Edition per Deezer album;
    always Official.
  - Source link: the `link` field (`https://www.deezer.com/album/<id>`).
- **Error envelope**: Deezer returns HTTP 200 with `{"error":{"type","message","code"}}`; code 4
  = quota exceeded. `DeezerSource` must check for `error` before reading `data` and treat code 4
  as a transient failure with backoff.
- **Limits**: documented 50 requests / 5 seconds per IP. We cap at 5 req/s (R10).

## R4. Per-user library access (FR-007)

- **Decision**: at scan time store, per library artist, the collection folder ids of the
  libraries that hold its albums (`ILibraryManager.GetCollectionFolders(album)`). At list time
  resolve the caller: `IUserManager.GetUserById(userId)`; allowed folders =
  `user.HasPermission(PermissionKind.EnableAllFolders)` ? all :
  `user.GetPreferenceValues<Guid>(PreferenceKind.EnabledFolders)`. A release is visible when its
  artist's library ids intersect the allowed set. Filter in C# after the SQL query (≤ 5 000 rows).
- **Verified**: `Jellyfin.Data.UserEntityExtensions.HasPermission/GetPreferenceValues<T>` exist in
  Jellyfin.Data 10.11.11; enums in `Jellyfin.Database.Implementations.Enums`; `User` entity in
  `Jellyfin.Database.Implementations.Entities` with a public 3-arg constructor (testable in-memory).
- **Alternatives**: `BaseItem.IsVisibleStandalone(user)` per item — rejected, relies on the static
  `BaseItem.LibraryManager` and hits the library DB per row; untestable hermetically.

## R5. Requesting user id from the HTTP context

- **Decision**: read the claim `"Jellyfin-UserId"` from `User` and parse as `Guid`; 401 if absent.
  Constant lives in `Jellyfin.Api` (not on NuGet), so the plugin declares its own
  `const string UserIdClaim = "Jellyfin-UserId"` with a comment naming the origin.
- **Verified**: no `ClaimsPrincipalExtensions.GetUserId` in MediaBrowser.Common, Controller,
  Model, Data, or Extensions 10.11.11. `MediaBrowser.Common.Api.Policies.RequiresElevation`
  exists for the admin controller.
- **Tests**: `ControllerContextFactory` builds a `DefaultHttpContext` with a `ClaimsPrincipal`
  carrying that claim.

## R6. Read-only library enumeration (FR-001, edge case: duplicate artist entries)

- **Decision**: `LibraryScanner` issues `GetItemList(MusicArtist)` once and
  `GetItemList(MusicAlbum, Recursive=true)` once; joins album `AlbumArtists` names to artist
  items by name (Jellyfin keeps one `MusicArtist` per name); reads MBIDs from
  `ProviderIds[MetadataProvider.MusicBrainzArtist]` (artist) with
  `MusicBrainzAlbumArtist` (album) as fallback; reads album ids from `MusicBrainzAlbum` (release)
  and `MusicBrainzReleaseGroup`. Tracks: `GetItemList(Audio, ParentId = album.Id, Recursive)`.
  Library artist key: MBID when present, else `name:<normalized name>`; entries sharing a key are
  one library artist, and only artists credited on ≥ 1 album are yielded.
- **Rationale**: never calls `ILibraryManager.GetArtist(name)` (it creates the item when missing;
  the plugin must not write to the library). Every call is substitutable with NSubstitute
  (Concert Radar's `LibraryArtistEnumeratorTests` shows `MusicArtist`/`MusicAlbum` are
  constructible in tests). `MusicAlbum.Tracks` is avoided because it needs the static
  `BaseItem.LibraryManager`.
- **Verified**: `InternalItemsQuery.AlbumArtistIds/ArtistIds/ParentId/Recursive/IncludeItemTypes`,
  `ILibraryManager.GetCollectionFolders`, `MetadataProvider.MusicBrainz{Artist,AlbumArtist,Album,
  ReleaseGroup}` present in 10.11.11.

## R7. Where type inclusion, "released since", and Upcoming are evaluated

- **Decision**: at **read time**, in the list query. Releases are stored with their mapped
  primary type and secondary types regardless of the admin's selection; the list applies the
  inclusion rule and displayed-type precedence from `docs/domain_knowledge/release-types.md`,
  the "released since" cutoff, and `Upcoming = date > today (UTC)`.
- **Rationale**: an admin toggling "Live" sees the change immediately (US2) with no re-fetch;
  release rows are small. Ownership checks still only run for releases with a library album
  candidate, so no extra source traffic.
- **Alternatives**: filter at fetch time — rejected, changing a type requires a full refresh.

## R8. Archive decisions that survive Purge (FR-013)

- **Decision**: `decision` table keyed by the natural release key `(artist_key, normalized_title)`
  — exactly the cross-source merge key — with `kind ∈ Ignore|HaveIt`, user id, timestamp. The list
  LEFT JOINs it; Restore deletes the row; Clear Archive truncates it; Purge never touches it.
- **Rationale**: surrogate release ids die with a purge, and canonical source ids can change when
  MusicBrainz later lists a Deezer-only release; the natural key is what "the same release" means
  in this system.
- **Ceiling** (`// ponytail:`): two distinct releases by one artist with identical normalized
  titles (two different "Greatest Hits") merge into one row. Upgrade path: include first-release
  year in the key.

## R9. PluginConfiguration shape

- **Decision**: scalar properties only — `bool MusicBrainzEnabled = true`, `bool DeezerEnabled =
  true`, seven `bool Include<Type>` (Album, EP true; others false), `string ReleasedSince = ""`
  (ISO date or empty), `string UserAgentContact = ""`. See `contracts/plugin-configuration.md`.
- **Rationale**: `XmlSerializer` restores bool/string defaults correctly when an element is
  missing, so no first-start seeding step and no "empty list means default or means none?"
  ambiguity (Concert Radar needed `PluginConfigurationDefaults.SeedIfEmpty` for its lists).
  No credentials or terms toggles exist in v1 (R12).

## R10. Rate limits, budgets, cooldown

- **Decision**: constants in `SourceLimits`, not configuration:

  | Source | Requests/s | Daily budget | Failure threshold | Cooldown |
  | --- | --- | --- | --- | --- |
  | musicbrainz | 1 | 10 000 | 5 consecutive | 6 h |
  | deezer | 5 | 20 000 | 5 consecutive | 6 h |

  Token bucket: BCL `TokenBucketRateLimiter`. Budget counter and cooldown persisted in
  `source_state` (reset at UTC midnight). `Retry-After` from 429/503 sets `next_allowed_at`.
- **Rationale**: FR-011 requires enforcement, not tunability; constitution VI forbids config for
  values that never change. SC-006 budget check: 500 artists ≈ 500 searches + ~1 000 catalogue
  pages + ~2 500 first-run track-list calls ≈ 4 000 MusicBrainz requests ≈ 67 min at 1 req/s.
- **Alternatives**: Concert Radar's `RateLimits` config list — rejected (YAGNI, list seeding).

## R11. Track-list fetch policy

- **Decision**: editions (with track lists) are fetched only for releases that have a library
  album candidate, and only when no edition row exists yet for that source release id. Stored in
  the `edition` table. Ownership is recomputed every run from stored editions plus the fresh
  library snapshot (local, no network).
- **Ceiling** (`// ponytail:`): a corrected track list at the source is not picked up until Purge
  release data. Upgrade path: `fetched_at` TTL re-fetch.

## R12. Credentials and terms acceptance (FR-010, US2 scenario 6)

- **Decision**: no credential fields and no terms toggle in v1. Both sources are open APIs
  requiring neither. US2 acceptance scenario 6 is not applicable to any v1 source; it becomes
  testable with the Bandcamp feature (ADR-0002). `IReleaseSource` carries no `RequiresTerms`
  member until a source needs one.
- **Rationale**: constitution VI (no scaffolding for a feature not yet specified).

## R13. Schema bootstrap without a hosted service or migration gate

- **Decision**: `Database` owns the path and a `Lazy<Task>` that applies pending embedded
  migrations; `OpenAsync(ct)` awaits it, then opens a connection. Repositories take `Database`.
- **Rationale**: removes Concert Radar's `SchemaBootstrapHostedService`, `IMigrationGate`,
  `MigrationGate`, `AlreadyReadyGate` (an interface with a test-only second implementation).
  Concurrent first callers share the same task.
- **Ceiling** (`// ponytail:`): migration errors surface on first use, not at server start; they
  are logged by the first caller (task or API).

## R14. Repository split

- **Decision**: four repositories by aggregate: `ArtistRepository` (`library_artist`,
  `artist_source`), `ReleaseRepository` (`release`, `source_entry`, `edition`, ownership columns),
  `ArchiveRepository` (`decision`), `SourceStateRepository` (`source_state`, `refresh_run`).
  Plain `SqliteCommand` with parameters, no ORM.

## R15. "Last refreshed … ago" and staleness (FR-015)

- **Decision**: the list API returns `lastRefreshedAt` (end of last completed run) and
  `refreshIntervalHours` computed from the task's triggers via `ITaskManager.ScheduledTasks`
  (default 24 when no trigger is readable). The fragment shows "Last refreshed <relative> ago."
  via `Intl.RelativeTimeFormat` only when `now − lastRefreshedAt > interval`.
  `hasCompletedRefresh = false` drives the empty-state sentence.

## R16. Partial release dates and ordering

- **Decision**: store `release_date` as the source string (`YYYY`, `YYYY-MM`, `YYYY-MM-DD`, or
  NULL) and `date_sort` padded with `-00` (`2024` → `2024-00-00`). List order:
  `undated last, date_sort DESC, title`. Year-only rows therefore sit after fully dated rows of
  the same year; the client groups by the first four characters (year) with "Upcoming" and
  "Undated" groups.
- **Ceiling** (`// ponytail:`): the API returns all matching rows (cap 5 000) with no paging;
  the client groups. Upgrade path: cursor paging by `date_sort`.

## R17. Cross-source merge and canonical identity (FR-006a)

- **Decision**: release identity is `(library_artist_id, normalized_title)`. After each upsert of
  source entries, `canonical_source` is recomputed: `musicbrainz` when a MusicBrainz entry exists,
  else `deezer`; `canonical_source_id`, types, and date come from the canonical entry (Deezer
  date fills a missing MusicBrainz date). Every entry keeps its own URL for the row's links.

## Verified host API surface (10.11.11)

- `ITaskManager.QueueScheduledTask<T>()` (Run now), `.ScheduledTasks` (last/next run).
- `Policies.RequiresElevation` in `MediaBrowser.Common.Api`.
- `IScheduledTask` with `Name`, `Key`, `Description`, `Category`, `GetDefaultTriggers()`
  (`TaskTriggerInfoType.DailyTrigger`, `TimeOfDayTicks` = 03:00 server-local).
- `IPluginServiceRegistrator.RegisterServices` supports `AddHttpClient(name)` and `TryAddSingleton(TimeProvider.System)`.
