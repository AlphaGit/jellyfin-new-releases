# Data model: Want it list

One table changes. No column is added.

## decision (amends 001, FR-005b, FR-013, FR-016)

| Column | Type | Notes |
| --- | --- | --- |
| artist_key | TEXT NOT NULL | unchanged |
| normalized_title | TEXT NOT NULL | unchanged |
| kind | TEXT NOT NULL | `Ignore` \| `HaveIt` \| **`WantIt`** (new) |
| user_id | TEXT NOT NULL | unchanged |
| decided_at | TEXT NOT NULL | unchanged |
| PK (artist_key, normalized_title) | | one decision per release; a new decision replaces the old |

Migration `004` rebuilds the table to widen the CHECK and copies every row.

## The three views

A Release row is in exactly one view, or in none:

| Decision row | Owned by the automatic check? | View |
| --- | --- | --- |
| none | no | List |
| none | yes | none (In library) |
| `WantIt` | no | Want-it list |
| `WantIt` | yes | none; the row stays and applies again if the Release stops being Owned |
| `Ignore` or `HaveIt` | either | Archive |

## Transitions

| From | Action | To |
| --- | --- | --- |
| List | Want it | Want-it list |
| List | Ignore / Have it | Archive |
| Want-it list | Have it / Ignore | Archive |
| Want-it list | Restore | List |
| Archive | Want it | Want-it list |
| Archive | Restore | List |

**Purge release data** leaves the table alone. **Clear Archive** deletes `Ignore` and `HaveIt`
rows only.
