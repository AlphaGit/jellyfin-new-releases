# Contract amendment: Unfollow artist

Amends `specs/001-track-new-releases/contracts/http-api.md` and the 010 amendment. Route and
field naming follow [docs/http-surface.md](../../../docs/http-surface.md): PascalCase
concatenated segments, no `api` segment, camelCase fields. Prefix: `Plugins/NewReleases`.

## POST `/Artists/{jellyfinId}/Unfollow`

Unfollows the library artist with that Jellyfin id, for every user.

| Status | When |
| --- | --- |
| `204` | The artist is now unfollowed. Also when it already was. |
| `401` | No user. |
| `403` | The artist is only in libraries the caller may not see. |
| `404` | No library artist has that id. |

Changes no Release decision and no Release row.

## POST `/Artists/{jellyfinId}/Follow`

Removes the Unfollow decision. Same statuses as Unfollow. `204` also when the artist was
already followed. Changes no Release decision.

## GET `/Artists/Unfollowed`

The unfollowed library artists the caller may see, sorted by name.

```json
{ "items": [ { "jellyfinId": "…", "name": "…" } ] }
```

- Same shape as `GET /Artists` (`ArtistsResponse`).
- Only artists currently in the library. An unfollowed artist that is absent is not listed.
- `401` without a user.

## GET `/Artists` (unchanged)

Still returns every library artist the caller may see, unfollowed ones included (FR-016).

## GET `/Releases`: new row field

Every row gains:

| Field | Type | Meaning |
| --- | --- | --- |
| `artistUnfollowed` | bool | `true` when the row's artist is unfollowed. Only Want-it and Archive rows can carry `true`. |

- **List** (`archived=false`, `wanted=false`): omits every Release of an unfollowed artist.
- **Want-it list** and **Archive**: unchanged. They keep their rows whatever the artist state.
- Query parameters are unchanged. `artistId` of an unfollowed artist in the List answers an
  empty list.

## GET `/Admin/Status`: new field

| Field | Type | Meaning |
| --- | --- | --- |
| `unfollowedArtists` | int | Unfollowed artists that are currently library artists. |

- `libraryArtists` is unchanged: all library artists, unfollowed included.
- `matchedArtists` and `unmatched` leave out unfollowed artists.

## POST `/Admin/ClearUnfollowedArtists`

Deletes every Unfollow decision, including those of artists not in the library now. `204`.
Elevation required, as for the other admin actions. Changes no Release, Want-it decision or
Archive decision. **Purge release data** and **Clear Archive** do not touch Unfollow decisions.

## Page (`user-view.html`)

- Tabs (`role="tablist"`): **List**, **Want it**, **Archive**, **Unfollowed artists**.
- **Artist button.** On every Release row of the List, Want it and Archive tabs, a small
  text-style button after the artist name: **Unfollow**, or **Follow** when
  `artistUnfollowed` is `true`. It lives in the artist line, not in the Release actions. Its
  `aria-label` names the artist (`Unfollow <artist>`, `Follow <artist>`). Marker attribute:
  `data-artist-action`; the Release buttons keep `data-action`.
- `aria-live="polite"` announces `Unfollowed <artist>` and `Followed <artist>`.
- **Unfollowed artists tab.** One row per artist: the name as a link to the Jellyfin artist
  page, and a **Follow** button. Sorted by name. No filter form, no counts. Empty state:
  `No unfollowed artists. Choose Unfollow next to an artist name to add one.`

## Admin page (`admin.html`)

- A line `Unfollowed artists: N` beside the library artist count.
- A button **Clear unfollowed artists**, with the same confirm step as Clear Archive:
  `Follow every unfollowed artist again? Their Releases show in the list again.`
