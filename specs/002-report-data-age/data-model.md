# Data Model: Report the age of the data, not the age of the run

**Feature**: `002-report-data-age` | **Date**: 2026-09-08

## Schema changes

**None.** No table, column, index or migration is added, altered or removed. `001`'s
`001_initial.sql` remains the only migration and `schema_version` stays at 1.

This feature changes which stored value the page reports, not what is stored. The check that
proves it: `git diff` on `src/Jellyfin.Plugin.NewReleases/Storage/Migrations/` is empty when the
feature is done.

## The value this feature reports

**Releases last checked at** — a derived, read-time value. Not persisted.

| Property | Value |
| -------- | ----- |
| Type | instant, or absent |
| Source | `MAX(artist_source.last_complete_at)` |
| Restricted to | rows whose `source` is enabled in `PluginConfiguration` at read time |
| Absent when | no enabled source has ever completed a catalogue fetch |
| Written by | nothing — it is computed on each read |

Derivation, in words: of every artist-and-source pair, take those whose source the administrator
currently has switched on, discard the pairs that have never completed a catalogue fetch, and
report the newest completion instant among what remains.

### Why these rows and not others

`artist_source.last_complete_at` is written by `ArtistRepository.SetFetchOutcomeAsync` and only
when the outcome is `Complete`. The upsert reads
`last_complete_at = COALESCE(excluded.last_complete_at, artist_source.last_complete_at)`, so a
`Partial` or `Failed` outcome cannot move it backwards or clear it. That property is what makes
`FR-003` hold without any new code: a refresh that reaches nothing, matches nothing, or is cut
short by the daily budget writes no completion, so the maximum does not move.

## Existing entities, unchanged

| Entity | Role in this feature |
| ------ | -------------------- |
| `artist_source` | Holds `last_complete_at`, the per-artist-and-source confirmation instant this feature aggregates. Not written differently. |
| `refresh_run` | Still one row per execution with counts and outcome. Still the administrator view's unit of reporting (`FR-009`). No longer feeds the user-facing age. |
| `source_state` | `last_success_at` still records the last successful call per source and still appears on the administrator page. Explicitly *not* the datapoint — see `research.md` R1. |
| `library_artist` | `last_refreshed_at` still drives rotation order. Explicitly not the datapoint. |
| `release` | Its mere existence now gates the empty state (`FR-008`), through a new existence query. No column changes. |

## Read-model additions

Two new repository reads. Both are queries; neither writes.

| Method | Returns | Query |
| ------ | ------- | ----- |
| `ArtistRepository.GetReleasesLastCheckedAtAsync(ISet<string> enabledSources, ct)` | `DateTimeOffset?` | `SELECT MAX(last_complete_at) FROM artist_source WHERE source IN (…)` |
| `ReleaseRepository.HasAnyAsync(ct)` | `bool` | `SELECT EXISTS(SELECT 1 FROM release LIMIT 1)` |

`GetReleasesLastCheckedAtAsync` takes the enabled set as an argument rather than reading
configuration itself, matching how `ReleaseFilter` already carries `EnabledTypes` into
`ReleaseRepository.ListAsync`. An empty enabled set yields no rows and therefore no instant,
which is the "every source disabled" edge case.

## Validation rules

| Rule | From | Where enforced |
| ---- | ---- | -------------- |
| A disabled source's completions do not count | `FR-002` | The `IN (…)` clause, built from configuration on each read |
| A partial or failed fetch does not move the value | `FR-003` | Already guaranteed by the existing `COALESCE` upsert; pinned by a test, not by new code |
| The value is never in the future | `FR-010` | Presentation: an instant later than now is treated as now, which `FR-006` then hides |
| Every reader reports the same instant | `FR-011` | One repository method, called by the list, status and admin responses |
| No age without data | `FR-008` | `HasAnyAsync` gates both the empty state and the age |

## State transitions

None. The value has no lifecycle of its own; it is the maximum of instants written by a process
this feature does not change.
