# Data Model: Fix four refresh defects

Changes only. Everything else is as in `specs/001-track-new-releases/data-model.md`.

## Stored

### `source_entry` (migration `003_source_entry_credit.sql`)

| Field | Type | Rule |
| --- | --- | --- |
| `credit` | TEXT, NOT NULL, default `'[]'` | JSON array of `{ "mbid": string \| null, "name": string }`, the release's album-artist credit at that source. MusicBrainz: from `artist-credit`. Deezer: always `[]`. Rewritten on every upsert. |

Migration:

```sql
ALTER TABLE source_entry ADD COLUMN credit TEXT NOT NULL DEFAULT '[]';
```

### `refresh_run.outcome`

Allowed values: `Completed`, `Cancelled`, `Failed`, **`Interrupted`**. The check changes in
`001_initial.sql` only (new installs). No migration. The maintainer's server was changed by hand
on 2026-10-10.

State transitions:

```text
(started, outcome NULL) ── run ends ──────────────▶ Completed | Cancelled | Failed
(started, outcome NULL) ── host starts, run open ─▶ Interrupted   (ended_at = host start)
```

### `artist_source.unmatched_reason`

New value: `placeholder artist` (`MatchReasons.Placeholder`). Written for every enabled source
of a placeholder artist. `status` is `Unmatched`. `source_artist_id` is null.

## In memory

| Record | Change |
| --- | --- |
| `CatalogueItem` | + `Credit`: `IReadOnlyList<CreditedArtist>` |
| `CreditedArtist` | new: `(string? Mbid, string Name)` |
| `Release` (read model) | + `Credit`: the union of its entries' credits |

## Derived rules

- **MusicBrainz IDs of a library artist**: `MusicBrainzIds.Parse(Mbid)`: split on `;`, trim,
  drop empties and placeholders, distinct, in order.
- **Placeholder artist**: the snapshot has at least one MBID and every one is a placeholder, or
  a source matched it to that source's placeholder (R1, R2).
- **Joint artist**: two or more parsed MBIDs. Its catalogue keeps a release only when the
  release's credit MBIDs contain all of them (FR-005).
- **Credited library artist** for a release (FR-005a): its MBIDs intersect the credit's MBIDs;
  or, when it has no MBID or a credit entry has none, its normalized name equals that entry's
  normalized name.
