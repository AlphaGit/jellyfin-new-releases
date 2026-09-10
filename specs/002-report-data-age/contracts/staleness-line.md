# Contract: the staleness line on the New Releases page

**Feature**: `002-report-data-age`. Replaces the copy contract in `001` (`FR-015`) and the
canonical wording recorded at `docs/domain_knowledge/CONTEXT.md`.

## When the line appears

| Condition | Result |
| --------- | ------ |
| `hasStoredReleases` is `false` | Line hidden. The empty state is shown instead (`FR-008`) |
| `releasesLastCheckedAt` is `null` | Line hidden. Releases still listed (`FR-008`, no-completed-fetch edge case) |
| Age `<=` `refreshIntervalHours` | Line hidden (`FR-006`) |
| Stored instant later than now | Treated as an age of zero, so the rule above hides it (`FR-010`) |
| Otherwise | Line shown above the list |

## Wording

```text
Releases last checked <relative time> ago.
```

Replaces `001`'s `Last refreshed <relative time> ago.`

The subject is the releases; the sentence contains no word for the refresh job — not "refresh",
"run", "scan" or "update" (`FR-007`, `SC-004`). A reader seeing only this
sentence must take it as a statement about the data.

## Relative time

Age measured from `releasesLastCheckedAt` to now. Each unit gives way at two of the next
(`FR-012`):

| Age | Renders as | Example |
| --- | ---------- | ------- |
| `< 48 h` | hours | `Releases last checked 36 hours ago.` |
| `48 h` to `< 14 days` | days | `Releases last checked 2 days ago.` |
| `14 days` to `< 61 days` | weeks | `Releases last checked 3 weeks ago.` |
| `61 days` to `< 365 days` | months | `Releases last checked 4 months ago.` |
| `>= 365 days` | fixed string | `Releases last checked over a year ago.` |

Boundaries are exact: 48 hours renders as `2 days`, not `48 hours`; 14 days renders as `2 weeks`;
61 days renders as `2 months`. The 61-day boundary is two 30.5-day months, chosen so the
changeover never lands mid-word.

The first four bands use `Intl.RelativeTimeFormat`, already used by the page. The last is a
literal, because `Intl` renders everything from 12 to 23 months as "1 year ago", which understates
by up to half.

## Administrator page

The administrator page shows the same instant (`FR-009`, `FR-011`) beside the last run. It is an
operational view, so it MAY show the value whenever it is known rather than only when the data is
stale; the wording and unit ladder above still apply when it does.
