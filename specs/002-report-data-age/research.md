# Research: Report the age of the data, not the age of the run

**Feature**: `002-report-data-age` | **Date**: 2026-09-08

Every unknown below was resolved by reading the built system. No new technology is involved and
no new dependency is proposed, so there are no best-practice or integration-pattern questions.

## R1: Where the datapoint comes from

**Decision**: `MAX(artist_source.last_complete_at)` restricted to the sources currently enabled
in `PluginConfiguration`, evaluated at read time.

**Rationale**: `artist_source.last_complete_at` shipped in `001`'s initial schema and is written
by `ArtistRepository.SetFetchOutcomeAsync` only when the outcome is `Complete` — the SQL uses
`last_complete_at = COALESCE(excluded.last_complete_at, artist_source.last_complete_at)`, so a
`Partial` or `Failed` outcome leaves the previous value untouched. That is exactly `FR-002`'s
"a catalogue fetch completed fully", already recorded, with no migration and no new write path.

Filtering by enabled source at read time follows the pattern `001` already uses for the release
type set (`ReleaseRepository.ListAsync` applies `EnabledTypes` from configuration on each read,
research decision R7 of `001`), so a configuration change takes effect immediately without a
refresh. This is what makes `FR-002`'s "currently enabled" cheap.

**Alternatives considered**:

- `source_state.last_success_at` — one row per source, simpler aggregate, already surfaced on the
  admin page. Rejected: it is written on every successful HTTP response, including an artist
  search that matched nothing, so it would report freshness the releases do not have. This is the
  contradiction the grill session found between `FR-002` and the zero-match edge case.
- A new column on `refresh_run` marking "this run reached a source" — rejected: needs a migration
  and a new write path to record something two existing columns already imply.
- `library_artist.last_refreshed_at` — rejected: written when every enabled source was
  *attempted*, not when any succeeded, so a run where both sources were in cooldown would still
  stamp it.

## R2: Naming in the HTTP contract

**Decision**: rename the response fields. `lastRefreshedAt` → `releasesLastCheckedAt`, and
`hasCompletedRefresh` → `hasStoredReleases`, in both the list response and the status response.

**Rationale**: the misleading name is not incidental to this bug, it is the cause. A field called
`lastRefreshedAt` invites exactly the wiring that shipped — the end of the last refresh run — and
the next person to touch it would make the same choice. `hasCompletedRefresh` has the same
problem under `FR-008`, which regates it on stored data rather than run history. Renaming costs a
change in `Api/Dtos.cs`, `ReleasesController`, both embedded pages, `001`'s `contracts/http-api.md`
and three tests, all of which this feature already touches.

There is no external consumer to break. The API is served by the plugin to its own embedded pages,
which ship in the same assembly and the same version; `manifest.json` publishes no API surface.

**Alternatives considered**:

- Keep the names, change only the meaning — rejected: it leaves a field whose name states the
  opposite of its value, which is how this defect was introduced.
- Keep the old names as duplicates alongside the new ones — rejected by constitution VI: no
  consumer needs them, and two fields carrying one value is a defect waiting to happen.

## R3: Whether Purge clears the confirmation timestamps

**Decision**: no. `AdminController.PurgeAsync` keeps calling
`ReleaseRepository.PurgeAsync` and `ArtistRepository.ResetResumeOffsetsAsync` unchanged;
`last_complete_at` survives a purge.

**Rationale**: `FR-008` gates the age on whether stored release data exists, so after a purge the
page shows the empty state and no age regardless of what `artist_source` holds. Clearing the
timestamps as well would be a second write that changes nothing a user can see, and it would
destroy a true fact about when each artist was last checked. The next completed fetch overwrites
it anyway.

**Alternatives considered**: clearing `last_complete_at` in `ResetResumeOffsetsAsync` — rejected
as a redundant write, and it would make the admin page's per-artist diagnostics worse after a
purge for no user-visible gain.

## R4: The relative-time ladder

**Decision**: thresholds in whole units, each giving way at two of the next unit, as `FR-012`
requires:

| Age | Wording |
| --- | ------- |
| `< 48 h` | hours |
| `48 h` to `< 14 days` | days |
| `14 days` to `< 61 days` | weeks |
| `61 days` to `< 365 days` | months |
| `>= 365 days` | `over a year ago` |

**Rationale**: `Intl.RelativeTimeFormat` is already used by the page and supports `hour`, `day`,
`week` and `month`, so four of the five bands are one more branch in an expression that exists.
The two-of-the-larger-unit rule gives the boundaries directly: 2 days = 48 h, 2 weeks = 14 days,
2 months = 61 days (taken as two 30.5-day months, so the boundary never lands mid-word). The
final band is a literal string because `Intl.RelativeTimeFormat` would say "1 year ago" for
anything from 12 to 23 months, which is less honest than declining to count.

**Alternatives considered**: `year` as a fifth `Intl` unit — rejected because "1 year ago" for a
23-month-old install understates by nearly half; the spec's own wording is `over a year ago`.

## R5: How the page decides the list is empty

**Decision**: add `ReleaseRepository.HasAnyAsync` returning
`SELECT EXISTS(SELECT 1 FROM release LIMIT 1)`, and gate the empty state on it.

**Rationale**: `FR-008` requires the empty state to follow stored data, not run history. The check
must be server-wide, not per-caller: a user whose libraries are all filtered out should see an
empty list, not "waiting for its first refresh", and the existing behaviour already draws that
line server-wide. `EXISTS` stops at the first row, so the cost does not grow with the 5 000-row
list cap.

**Alternatives considered**: reusing the already-fetched list count — rejected because it is
per-caller and would tell a restricted user the plugin has never run.

## R6: Showing both values on the administrator page

**Decision**: add the same `releasesLastCheckedAt` to `AdminStatusResponse` alongside the existing
`lastRun`, and display it next to the run. Per-source `lastSuccessAt` stays as it is.

**Rationale**: `FR-009` requires an operator to see the two diverge, which needs both on one
screen, and `FR-011` requires every place reporting the age to report the same instant — so the
admin value must be the same computation, not a second definition. The three source-level
timestamps already on the page answer a different question ("is this source responding") and are
still useful; removing them would lose diagnostics.

**Alternatives considered**: deriving the age in the page's JavaScript from per-source values —
rejected: it would be a second implementation of `FR-002`, free to drift from the first, which
`FR-011` exists to prevent.

## R7: What `001` owns that this feature must change

Established by reading `001`'s artifacts and the suite. All of these are edits to `001`, not new
files, and each is a task in this feature:

| Artifact | Change |
| -------- | ------ |
| `specs/001-track-new-releases/spec.md` `FR-015` | Wording updated to the new line and the new datapoint; intent unchanged |
| `specs/001-track-new-releases/contracts/http-api.md` | Renamed fields in the list, status and admin responses |
| `docs/domain_knowledge/CONTEXT.md` line 64 | Canonical wording replaced |
| `tests/.../Api/ReleasesControllerTests.cs` `U118` | Asserts the last completed run's end; must assert the last completed fetch |
| `tests/.../Acceptance/ConfigureAndRunTests.cs` `A20` | Pins the old behaviour — that a no-source run resets the age. Must be inverted |
| `tests/.../Acceptance/BrowseReleasesTests.cs` `A5` | Still correct under `FR-008` (no data → no age), but the field rename touches it |
| `specs/001-track-new-releases/tdd/test-list.md` | `U118` and `A20` rows restated |

`A20` is the sharpest case: it was written three hours ago by this feature's own audit and pins
precisely the behaviour `002` removes. Constitution II forbids weakening a test to reach green,
so it is rewritten to assert the new rule, not deleted.

## R8: Which page-side test runner

**Decision**: Node's built-in test runner — `node --test`, with `node:test`, `node:assert` and
`node:vm` from the standard library. No `package.json`, no npm install, no framework.

**Rationale**: Node 22.20.0 is already on the development machine and preinstalled on the
`ubuntu-latest` GitHub runner the build workflow uses, and all three modules are built in and
stable. That makes `FR-014` ("no network access and no installation step") true by construction
rather than by discipline, and it means constitution VI's rule that a new direct dependency needs
a pinned version published at least seven days earlier **does not engage at all**: there is no
dependency to pin. CI gains one `node --test` step next to the existing `dotnet test`.

**Alternatives considered**:

- Vitest or Jest — rejected: each pulls a large dependency tree into a repository that has no
  JavaScript dependencies at all, needs `npm ci` in CI (breaking `FR-014`'s no-install rule and
  straining constitution III's "passes with no network"), and buys nothing over `node:test` for
  five pure functions.
- jsdom, to reach the rendering functions too — rejected for this feature. It is a real
  dependency, and the functions it would unblock (`row`, `render`, `refreshStatus`, `read`,
  `fill`, `query`) all read or write the page. Worth its own decision later; see `spec.md`
  Assumptions.
- A C# test that asserts on the HTML file's text — rejected: it would pin the source of the
  script, not its behaviour, so the unit ladder's boundaries would still be unverified.

## R9: How the page's functions get under test without shipping differently

**Decision**: extract one seam and expose the pure helpers on a single object. The test loads the
page's `<script>` body into a `node:vm` sandbox with stubs for the browser globals and reads that
object.

Two changes to shipped code, both behaviour-preserving:

1. `staleness(data)` in `Web/user-view.html` splits into `stalenessText(checkedAt, now,
   intervalHours)`, which returns the sentence or `null`, and the existing `staleness(data)`,
   which calls it and touches the DOM. This is the seam `FR-013` requires.
2. Each page ends its script with one line assigning its pure helpers to a single global object,
   which the sandbox reads.

**Rationale**: this keeps `FR-016` true — no new file in the packaged output, no `<script src>`
the page fetches at runtime, no build step. It was worth checking, because the obvious approach
(moving the helpers into `Web/nr-core.js`, which the csproj's `Web/*.js` glob already anticipates)
*would* have been a shipped change: a second resource the page has to load, and a new failure mode
if it does not, which constitution IV would then require handling.

The functions this reaches: `esc`, `groupOf`, `healthText`, `artistLink` and the new
`stalenessText`. `esc` is the one worth having regardless of this feature — release titles and
artist names arrive from MusicBrainz and Deezer and are concatenated into HTML by `row()` in about
ten places, and `esc` is the only thing between them and the DOM.

**Alternatives considered**:

- Move the helpers to `Web/nr-core.js` — rejected: a shipped change with a new runtime load path,
  for no test-time benefit over the sandbox.
- Duplicate the ladder in the test — rejected outright: a re-implemented expectation, which the
  project's own test-quality rubric grades as a HIGH smell.

## Non-issue: no new dependency

Nothing here needs a package, in either language. The page-side runner is Node's standard
library (R8) and the .NET side is unchanged, so constitution VI's seven-day-old pinned-version
rule does not apply: no dependency is added. The one CI addition, `actions/setup-node@v4`, is
first-party GitHub tooling of the same class as the `actions/setup-dotnet@v4` the workflow already
uses, and pins the runtime version rather than adding a package.
