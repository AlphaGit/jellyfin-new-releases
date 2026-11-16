# Research: Unfollow artist

## R1. Where the decision lives

**Decision**: A new table `unfollowed_artist (artist_key TEXT PRIMARY KEY, user_id TEXT NOT NULL,
decided_at TEXT NOT NULL)`, added by migration `005_unfollowed_artist.sql`. No foreign key.

**Rationale**: `Refresh` calls `DeleteMissingAsync`, which deletes a departed artist's
`library_artist` row and cascades to its Releases. The `decision` table survives that because it
is keyed by `artist_key`, not by a foreign key. A column on `library_artist` would be deleted
with the artist and break FR-014. Keyed by `artist_key`, the rule "applies again on return"
needs no code. Purge release data and Clear Archive touch other tables, so FR-015 also needs no
code.

**Alternatives**: A column `unfollowed` on `library_artist`. Rejected: lost when the artist
leaves the library (FR-014). A new `kind` in `decision`. Rejected: that table is keyed per
Release, so an artist row would need a fake title and every Release query would have to skip it.

## R2. Refresh skips unfollowed artists

**Decision**: At the start of `ExecuteAsync`, after `DeleteMissingAsync`, the task reads the set
of unfollowed keys once. It drops those artists from `rotation` before step 3, so they get no
source request, no `SetLastRefreshedAsync` and no match write. Step 4 (ownership) loops over the
same filtered list. Progress uses the filtered count.

**Rationale**: FR-008 says "no source fetch, no ownership check, no change to stored data". One
filter on the list both loops read covers all three. The library snapshot still holds every
artist, so a followed artist's ownership check still sees albums credited to an unfollowed
collaborator (`CreditIndex` reads the snapshot, not the rotation).

**Side effect, accepted**: an unfollowed artist keeps its old `last_refreshed_at`. On Follow it
is the oldest in the rotation, so the next Refresh processes it first. The spec says the same:
new Releases appear at the next Refresh.

**Race, accepted**: a user may unfollow while a Refresh is running. The run may still fetch that
artist once. The List condition (R3) hides the stored rows at once, so the user sees no effect.

**Alternatives**: Skip inside `RefreshArtistAtSourceAsync` only. Rejected: leaves the ownership
pass and the rotation timestamp writes.

## R3. Hiding in the List view

**Decision**: `ReleaseRepository.ListAsync` adds
`AND NOT EXISTS (SELECT 1 FROM unfollowed_artist u WHERE u.artist_key = a.artist_key)` to the
List condition only. The Want-it and Archive conditions do not change (FR-006). `ListedRelease`
gains `ArtistUnfollowed`, computed as `EXISTS(...)` in the select, so the Want-it and Archive
rows can show "Follow" (FR-013).

**Rationale**: the three views are already fixed conditions chosen in C# (010 R3). One more
condition keeps that shape. `unfollowed_artist.artist_key` is the primary key, so the lookup is
an index probe.

**Effect on Restore**: Restore a Release of an unfollowed artist deletes its decision. The row
then matches the List condition's decision test but not the `NOT EXISTS`, so it stays hidden
until Follow (spec US3 scenario 4).

**Effect on `HasAny` and the "No data yet" text**: unchanged. The page decides that text from
`hasStoredReleases`. A user who unfollowed every artist sees "Nothing missing for this
selection", which is correct.

## R4. Identifying the artist on the wire

**Decision**: Routes take the artist's `jellyfinId`, as the page already has it on every row
(`artistJellyfinId`) and in `GET Artists`. The controller maps it to the stored artist with a
new `ArtistRepository.GetByJellyfinIdAsync` and uses that row's `ArtistKey` for storage.

**Rationale**: the page never sees `artist_key`. Release actions use the numeric Release `id`
the same way and resolve the key server-side.

**Library access**: the controller applies `access.CanSee(artist.LibraryIds)` before writing and
answers `403` otherwise, as `DecideAsync` does (010 FR-007 pattern). An unknown id answers `404`.

**Idempotence**: Unfollow of an unfollowed artist and Follow of a followed artist answer `204`
and change nothing. Two users acting at once end in the last write (spec edge case).

## R5. The Unfollowed artists list

**Decision**: `GET Artists/Unfollowed` returns `ArtistsResponse` (the existing shape:
`jellyfinId`, `name`) for every row of `library_artist` that has an `unfollowed_artist` row,
filtered by `access.CanSee(a.LibraryIds)`, ordered by name. An unfollowed artist that is not in
`library_artist` is not listed (FR-011, FR-014).

**Rationale**: FR-010 asks for name and link only, sorted, no counts, no filter. The existing
shape already carries both. No new DTO.

**Route shape**: `Artists/Unfollowed` is a literal segment and `Artists/{jellyfinId}/Unfollow`
has a different shape, so they do not clash. `GET Artists` keeps its meaning (FR-016).

## R6. Admin counts and the reset action

**Decision**: `ArtistRepository.GetCountsAsync` keeps `libraryArtists` as the total of
`library_artist`. It excludes artists with an `unfollowed_artist` row from the per-source
matched counts and from the unmatched list, and returns a new `UnfollowedArtists` count of
`library_artist` rows with such a row. `AdminStatusResponse` gains `UnfollowedArtists`.
`POST Admin/ClearUnfollowedArtists` runs `DELETE FROM unfollowed_artist` and answers `204`.

**Rationale**: FR-017, FR-018 and SC-007 (library = followed + unfollowed). The reset deletes
rows for absent artists too, so nothing hides in the table (spec Q12 option 2, Q13 option 1).
The name follows the existing `ClearArchive` pattern and rule 1 of `docs/http-surface.md`.

**Elevation**: the admin route sits under `PluginRoutes.Admin`, which already requires
elevation. No new policy.

## R7. The page

**Decision**:

- **Artist button.** In `row()`, the artist line gets a small text-style button after the name
  link, with `data-artist-action="Unfollow"` (or `"Follow"` when `item.artistUnfollowed`) and an
  `aria-label` that names the artist. It uses `data-artist-action`, not `data-action`, so the
  existing Release click handler (`button[data-action]`) cannot catch it, and the two stay
  visibly and structurally apart (FR-003). It sits in `.nr-artist`, not in `.nr-actions`.
- **Behavior.** `POST Artists/{artistJellyfinId}/Unfollow|Follow`, then reload. No confirmation.
  `aria-live` announces "Unfollowed <artist>" or "Followed <artist>".
- **Tab.** A fourth tab `nr-tab-unfollowed` "Unfollowed artists", after Archive. The view
  renders its own simple list from `GET Artists/Unfollowed`: name link plus a "Follow" button.
  The filter form is hidden on this tab (FR-010: no filter). Empty state:
  `No unfollowed artists. Choose Unfollow next to an artist name to add one.`
- **Admin page.** One line "Unfollowed artists: N" in the artist counts, and a button
  "Clear unfollowed artists" with the same confirmation pattern as Clear Archive
  ("Follow every unfollowed artist again? Their Releases show in the list again.").

**Rationale**: reuses the tab, panel, `api()` and announce helpers. A separate click handler
for `data-artist-action` is about ten lines.

## R8. Domain documents

**Decision**: `docs/domain_knowledge/CONTEXT.md` gains the terms **Followed artist**,
**Unfollowed artist**, **Unfollow**, **Follow** and **Unfollowed artists** (tab). The Library
artist row drops "tracked artist" and "followed artist" from its _Avoid_ cell. The "Domain
boundaries" gain one bullet: an Unfollow decision is artist-level, server-wide, never changes a
Release decision, and Refresh skips the artist fully. "Related documents" links this spec. Code
identifiers: `UnfollowedArtist`, `Follow`, `Unfollow`.

**Rationale**: FR-021 and the project rule that specs, UI and code use one vocabulary.

## R9. Tests

**Decision**:

- **Storage** (temporary database): `005` migrates a database holding `decision` rows with none
  lost; the repository sets, removes, clears and lists; a row survives `DeleteMissingAsync`,
  Purge and Clear Archive; a re-added artist is unfollowed again (FR-014, FR-015, FR-020).
- **Refresh** (stubbed sources): an unfollowed artist produces zero source calls and zero
  ownership writes; its stored Releases, Want-it and Archive decisions are unchanged; a followed
  artist credited with an unfollowed collaborator's album still gets its check (SC-003, FR-008,
  FR-019).
- **List query**: the List hides an unfollowed artist's undecided Releases; Want-it and Archive
  rows stay; rows carry `artistUnfollowed`; Restore of an archived Release of an unfollowed
  artist stays hidden in the List (FR-005, FR-006, FR-013, US3).
- **Controllers**: Unfollow and Follow answer `204`, `401`, `403`, `404`; `Artists/Unfollowed`
  is sorted and access-filtered; `GET Artists` is unchanged; admin status counts add up and
  `ClearUnfollowedArtists` follows everyone (FR-016, FR-017, FR-018, SC-007).
  `HttpSurfaceTests` already scans every action for PascalCase routes and the camelCase profile.
- **Page** (`node --test tests/web`, fake DOM): the artist button is outside `.nr-actions`, does
  not carry `data-action`, and reads "Follow" for an unfollowed artist; the fourth tab lists
  name and Follow, hides the filters, and shows the empty state; the admin page shows the line
  and the button.
- **Fixtures**: `releases.json` gains `artistUnfollowed`; a new `unfollowed-artists.json` and
  `admin-status.json` gain their fields; `ResponseNamingTests` pins the names.

No new test dependency.
