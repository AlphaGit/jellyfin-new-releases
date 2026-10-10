# Research: Explain each unmatched artist step by step

Decisions for [spec.md](./spec.md). R1 and R5 were put to the maintainer on 2026-10-10 during
planning; the rest follow from the code and the constitution.

## R1 — How a failure is stored

**Decision**: The reason text in `artist_source.unmatched_reason` stays the record of the failed
step and its scores. One nullable JSON column, `artist_source.match_detail`, holds the attempt
detail of FR-015. Migration `002_match_detail.sql` adds it with one `ALTER TABLE … ADD COLUMN`.

**Rationale**: Every reason any released version wrote already names its failed step, and the
scores are in the text (`low score (70)`, `ambiguous (score 92 vs 90)`). One parser serves old and
new rows alike, so FR-011 needs no back-fill. Rows from before the upgrade have a null
`match_detail`; their fix shows no candidates or counts (FR-016).

**Alternatives considered**: Structured columns (step, scores, candidates) with a migration that
converts old rows by pattern-matching reason text in SQLite. Rejected by the maintainer: more
schema, and a SQL text parser is harder to test than a C# one.

## R2 — One home for the reason strings

**Decision**: A static class `MatchReasons` (in `Matching/`) holds the reason strings as constants
and format helpers (`LowScore(int)`, `Ambiguous(int, int)`), and `Parse(source, text)`, which
returns the failed step and its values, or null for an unrecognized text. `MusicBrainzSource` and
`DeezerSource` build their reasons through it.

**Rationale**: The sources write the text and the status endpoint reads it. One class for both
means a reworded reason cannot silently stop parsing. A test parses every reason each source can
produce.

**Alternatives considered**: Parsing with a regex in the controller. Rejected: the format would
live in two places.

## R3 — Capturing the attempt detail

**Decision**: `ArtistMatch` gains an optional `Detail` (`MatchDetail`): up to 3 candidates
(`Name`, and for MusicBrainz `Score` and `Id`), plus for Deezer `ExactNameCount` and
`AlbumsCompared`. `ArtistMatch.Unmatched(reason, detail)` carries it; `Matched` carries none.
`ArtistRepository.SetMatchAsync` writes it to `match_detail` as JSON, and writes null for a
match.

- **MusicBrainz**: the first 3 candidates of the score-sorted list the match already builds.
  `no result` has none.
- **Deezer**: the first 3 artists of the search response, whatever their name. For `no result`
  they are the near names the administrator compares. For `no corroborating album`,
  `ExactNameCount` is the number of exact-name candidates and `AlbumsCompared` is the total of
  the first-page items the match already read for them.

**Rationale**: Every value is already in memory during the match. Nothing new is requested from a
source (FR-015), and no decision changes (FR-012). The cap of 3 is applied when writing, so the
stored detail is never larger than what is shown.

**Alternatives considered**: Storing every candidate and capping in the response. Rejected:
stored data nothing reads (Simplicity).

## R4 — Which artists are listed, and with which sources

**Decision**: `ArtistRepository.GetCountsAsync` takes the enabled source IDs. An artist is listed
when it has an `Unmatched` row at an enabled source (FR-014). For each listed artist the query
returns its rows at every enabled source. An enabled source with no row, or with status
`Pending`, is **not checked**. Rows of disabled sources stay in the table and are not read.

**Rationale**: Today's query lists every `Unmatched` row, which is why a disabled source still
shows (2026-10-10 clarification). Filtering at read time keeps the stored results, so re-enabling
a source shows them again before any refresh.

**Alternatives considered**: Deleting a source's rows when it is disabled. Rejected: FR-014
requires keeping them.

## R5 — Response carries codes; the page writes the sentences

**Decision**: The admin status response replaces each unmatched artist's `sources` and `hint` with
`reasons`: one entry per enabled source, in the order of the `sources` array. Each entry carries
the state, the step trail, the failed step, and the attempt detail as structured data. The page
maps (source, failed step) to the fix sentence of FR-008, and derives the tracking line of FR-009
from the states. `AdminController.UnmatchedHint` is removed. See
[contracts/http-api.md](./contracts/http-api.md).

**Rationale**: Chosen by the maintainer. The server owns the facts (which steps, which order,
which values), so the page parses no reason text (FR-013). The page owns the wording.

**Alternatives considered**: The server writing each fix sentence. Rejected by the maintainer.

**Consequence for tests**: the fix wording is tested with `node:test` against the committed
`admin-status*.json` fixtures. The C# acceptance test asserts the data the page reads, through
`AdminController`.

## R6 — Rendering the trail

**Decision**: Each step renders as `<span class="nr-step nr-step-{state}">{name} <span
aria-hidden="true">{mark}</span><span class="nr-sr">{state in words}</span>{value}</span>`, joined
by ` · `. `nr-step-notreached` is dimmed with `opacity`. `nr-sr` is the visually hidden text
pattern. The trail line has `white-space: nowrap`. A MusicBrainz candidate's ID renders in a
`<code>` with `user-select: all`, so one click selects all of it.

**Rationale**: FR-010 needs the state in words for a screen reader, and not in color alone; the
mark and the dimming are both visual extras. No external asset, no framework.

**Alternatives considered**: `aria-label` on the span. Rejected: the step's visible name would be
replaced, not added to.

## R7 — Measuring the one-line rule (FR-005, SC-004)

**Decision**: A page test asserts `white-space: nowrap` on the trail and that the longest possible
trail text (MusicBrainz with `Unique ✗ 100 vs 95`) is at most 64 characters. The real-browser
pass in [quickstart.md](./quickstart.md) checks the 1280-pixel width by eye.

**Rationale**: The string-capturing fake DOM measures no pixels, the same limit `007` recorded
(its R11).

## R8 — The page's status fixtures

**Decision**: `tests/fixtures/pages/admin-status.json` is rewritten with real reason shapes: one
artist Matched at MusicBrainz and Unmatched at Deezer on Album with detail, one ambiguous at
MusicBrainz with candidates and Unmatched at Deezer on Name with near names, and one row from
before the upgrade (no detail). `ResponseNamingTests` keeps pinning its names against the
record. No new recorded source fixture: `artist_search_ambiguous.json`,
`search_artist_exact.json` and `search_artist_homonyms.json` already hold the candidates the
source tests need.
