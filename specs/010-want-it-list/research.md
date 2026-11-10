# Research: Want it list

## R1. Widening `decision.kind`

**Decision**: Migration `004_decision_want_it.sql` creates `decision_new` with
`CHECK (kind IN ('Ignore', 'HaveIt', 'WantIt'))`, copies every row, drops `decision`, renames
`decision_new`. The runner already wraps each script in a transaction and nothing references
`decision` by foreign key.

**Rationale**: SQLite cannot change a CHECK in place. Removing the CHECK would also work but
loses a guard that `001` set. Rebuild keeps every released install's decisions (FR-005, IV).

**Alternatives**: Edit `001_initial.sql` only, as 009 did for `Interrupted`. Rejected: that
change reaches new installs only, and decisions are user data that must reach every install.
A separate `want_it` table. Rejected: a second table breaks "one decision per release" (FR-007)
and doubles the join.

**Rollback note**: a downgrade to an older plugin version is not supported. Migrations are
forward-only today.

## R2. Clear Archive keeps Want it

**Decision**: `ClearAsync` runs `DELETE FROM decision WHERE kind <> 'WantIt'`.

**Rationale**: FR-006. The admin button and its confirmation text say "Archive"; the Archive
never held Want-it Releases.

## R3. Three views from one query

**Decision**: `ReleaseFilter` gains `Wanted` (default `false`). `ReleaseRepository.ListAsync`
picks a fixed SQL condition by view:

| View | Condition |
| --- | --- |
| List | `ownership_state <> 'Owned' AND d.kind IS NULL` |
| Want-it list | `ownership_state <> 'Owned' AND d.kind = 'WantIt'` |
| Archive | `d.kind IN ('Ignore', 'HaveIt')` |

The conditions are constants chosen in C#; no user text is concatenated.

**Rationale**: FR-001, FR-006a, FR-002a. An In-library Release with a Want-it decision matches
no view, so it leaves the Want-it list and does not enter the Archive. The decision row stays,
so it returns if the library loses the album. Type, state, date and artist filters, grouping and
ordering are unchanged, so FR-002a needs no new code.

**Alternatives**: A `view` query value instead of `wanted`. Rejected: `archived=true` ships in
`0.1.0`..`0.3.1`, so callers still need it; a second way to say the same thing is worse than one
more flag.

## R4. One write action

**Decision**: `POST /Releases/{id}/WantIt` calls `DecideAsync(id, DecisionKind.WantIt)`.
`SetAsync` already upserts, so Want it from the List or the Archive, and Have it or Ignore from
the Want-it list, replace the row (FR-007). Restore deletes it. Visibility and authentication
are the existing checks (FR-004, FR-009).

**Rationale**: No new code path. The `004` and `005` rules for non-user callers apply as they do
to Ignore and Have it.

## R5. Page

**Decision**: `Web/user-view.html` holds `view` (`list` | `wantit` | `archive`) instead of the
boolean `archived`. Buttons per view follow FR-002b. Strings:

- Tab: `Want it`.
- Empty state: `Nothing on the Want it list. Choose Want it on a release to add it.`
- Announcement: `Marked <title> as Want it`.
- The Archive badge code only reads `item.archived`; Want-it rows carry `archived: null`.

**Rationale**: Settles the two Outstanding wording points from the grill using the existing
sentence style. The first-refresh message ("No data yet...") stays List-only, as for the
Archive.

## R6. Domain vocabulary

**Decision**: `CONTEXT.md` gains rows **Want it** and **Want-it list**. **Restore** reads "undo
a decision; the Release returns to the list". **Clear Archive** reads "delete all Ignore and
Have-it decisions; Want-it decisions stay". **Archive** keeps its meaning. Code names:
`DecisionKind.WantIt`, `ReleaseFilter.Wanted`.

## R7. Tests

**Decision**: Extend, do not add suites.

- Storage: `004` on a database holding `Ignore` and `HaveIt` rows keeps them; Clear keeps a
  `WantIt` row.
- Controller and routes: `WantIt` accepted for a visible Release, `Forbid` for a hidden one,
  `wanted=true` lists it, a Want-it Release is absent from the List and the Archive, an Owned
  one is absent from all three, `archived=true&wanted=true` answers `400`.
- Naming: `ResponseNamingTests` gets a `wanted` fixture sample.
- Page: buttons per view, empty state, announcement.
