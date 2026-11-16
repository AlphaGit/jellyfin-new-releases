# Data model: Unfollow artist

Vocabulary follows [CONTEXT.md](../../docs/domain_knowledge/CONTEXT.md). Table and column names
follow `001` data-model conventions: ISO-8601 UTC text for timestamps, GUID text for ids.

## `unfollowed_artist` (new, migration `005_unfollowed_artist.sql`)

| Column | Type | Meaning |
| --- | --- | --- |
| `artist_key` | TEXT PRIMARY KEY | The same key as `library_artist.artist_key` and `decision.artist_key`. |
| `user_id` | TEXT NOT NULL | The user who unfollowed, as for `decision.user_id`. |
| `decided_at` | TEXT NOT NULL | When, UTC. |

- **No foreign key** to `library_artist`. The row outlives the artist's presence in the library
  and the artist's Releases (FR-014).
- **A Follow deletes the row.** There is no "followed" row. An artist is followed when it has
  no row (FR-001).
- **Server-wide**: one row per artist, whoever acts. The last write wins (spec edge case).
- **Not touched by**: `DeleteMissingAsync`, Purge release data, Clear Archive, Refresh, or the
  Release decision actions. **Removed by**: Follow, and Clear unfollowed artists.

```sql
CREATE TABLE unfollowed_artist (
    artist_key TEXT PRIMARY KEY,
    user_id    TEXT NOT NULL,
    decided_at TEXT NOT NULL
);
```

The migration only creates the table. It reads and rewrites no existing row (FR-020).

## State of an artist

```text
followed  --Unfollow-->  unfollowed
followed  <--Follow----  unfollowed
                          (also: Clear unfollowed artists)
```

An unfollowed artist that leaves the library keeps its row. It is invisible in the Unfollowed
artists tab and in the admin count while absent (FR-011, FR-017).

## The List condition (changed)

| View | Condition |
| --- | --- |
| List | `ownership_state <> 'Owned' AND d.kind IS NULL AND NOT EXISTS (unfollowed_artist u WHERE u.artist_key = a.artist_key)` |
| Want-it list | `ownership_state <> 'Owned' AND d.kind = 'WantIt'` (unchanged) |
| Archive | `d.kind IN ('Ignore', 'HaveIt')` (unchanged) |

Every row also selects `EXISTS (unfollowed_artist ...)` as `ArtistUnfollowed`.

## Read models

- `ListedRelease` gains `bool ArtistUnfollowed`.
- `ReleaseDto` gains `bool ArtistUnfollowed` (camelCase `artistUnfollowed`).
- `ArtistCounts` gains `int Unfollowed`. `LibraryArtists` stays the total of `library_artist`.
  `Matched` and `Unmatched` exclude unfollowed artists.
- `AdminStatusResponse` gains `int UnfollowedArtists`.

## Refresh reads

`UnfollowedArtistRepository.GetKeysAsync()` returns the set of keys once per run. The task
filters `rotation` with it. The library snapshot is not filtered.

## Invariants

- `libraryArtists = followed + unfollowed`, with an unfollowed artist counted once and only
  while it is a library artist (SC-007).
- No Release, Edition, Source entry, `artist_source` row or `decision` row changes because of
  Unfollow or Follow (SC-004).
- An unfollowed artist gets zero source requests and zero ownership writes in a Refresh
  (SC-003).
