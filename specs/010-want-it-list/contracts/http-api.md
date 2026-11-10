# Contract amendment: Want it

Amends `specs/001-track-new-releases/contracts/http-api.md`. Route and field naming follow
[docs/http-surface.md](../../../docs/http-surface.md). Prefix: `Plugins/NewReleases`.

## GET `/Releases`: new query value

| Query | Type | Meaning |
| --- | --- | --- |
| `wanted` | bool | `false` (default) = not the Want-it list; `true` = the Want-it list |

- `archived=true` and `wanted=true` together answer `400 Bad Request`.
- With `wanted=true`, every other filter and the grouping, ordering and library-access rules are
  as for the List. The row shape is unchanged.
- `archived` in a row is `null` in the List and in the Want-it list. In the Archive it is
  `{ "kind": "Ignore" | "HaveIt", "decidedAt": "…" }` as before; it never carries `WantIt`.
- A Release the automatic check finds In library is in none of the three results.

## POST `/Releases/{id}/WantIt`

Same as `/Ignore` and `/HaveIt`: `204` on success, `401` without a user, `404` for an unknown
id, `403` for a Release in a library the caller may not see. Sets the decision `WantIt`,
replacing any earlier decision.

`POST /Releases/{id}/Restore` also undoes a `WantIt` decision.

## POST `/Admin/ClearArchive`

Deletes every `Ignore` and `HaveIt` decision. `WantIt` decisions stay.

## Page (`user-view.html`)

- Tabs (`role="tablist"`): **List**, **Want it**, **Archive**.
- Buttons per view: List: **Want it**, **Ignore**, **Have it**. Want-it list: **Have it**,
  **Ignore**, **Restore**. Archive: **Restore**, **Want it**. Each has an `aria-label` that names
  the Release, as today.
- `aria-live="polite"` announces `Marked <title> as Want it` for the new action.
- Empty state: `Nothing on the Want it list. Choose Want it on a release to add it.`
