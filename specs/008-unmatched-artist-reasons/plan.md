# Implementation Plan: Explain each unmatched artist step by step

**Branch**: `008-unmatched-artist-reasons` | **Date**: 2026-10-10 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/008-unmatched-artist-reasons/spec.md`

## Summary

The Unmatched artists table on the admin page becomes **Artist / Reason / How to fix**. Reason
shows each enabled source's matching steps with a mark; How to fix gives only fixes for the steps
that failed, then whether the artist is still tracked.

1. **Storage**: migration `002` adds one nullable JSON column, `artist_source.match_detail`, for
   the candidates and Deezer counts of the last failed match (R1, R3). The reason text stays the
   record of the failed step.
2. **Matching**: `MatchReasons` holds the reason strings and parses them into a failed step and
   values (R2). The two sources build their reasons through it and attach the attempt detail to
   `ArtistMatch`. No match decision changes.
3. **Listing**: the admin query lists an artist only when it is Unmatched at an enabled source,
   with an entry for every enabled source (R4).
4. **Response**: `unmatched[].reasons` replaces `sources` and `hint` and carries states, steps,
   failed step and detail as data (R5).
5. **Page**: `admin.html` renders the trail and writes the fix sentences and the tracking line
   from that data (R5, R6).

## Technical Context

**Language/Version**: C# on `net10.0`. ES5-style JavaScript in the embedded page. Node 22 for
the page tests.

**Primary Dependencies**: `Jellyfin.*` 12.0.0 (`ExcludeAssets=runtime`), `Microsoft.Data.Sqlite`
10.0.11. **No new direct dependency.**

**Storage**: SQLite. Migration `002_match_detail.sql`: one `ALTER TABLE artist_source ADD COLUMN
match_detail TEXT`. See [data-model.md](./data-model.md).

**Testing**: xunit 2.9.3 + NSubstitute 5.3.0. `node:test` + the fake DOM in `tests/web/`.
No new recorded source fixture (R8). `tests/fixtures/pages/admin-status.json` rewritten.

**Target Platform**: Jellyfin 12.0.x server; the admin page in the Jellyfin web client.

**Project Type**: Jellyfin server plugin with embedded web pages.

**Performance Goals**: The admin status adds one query over `artist_source` for the listed
artists. Hundreds of rows at most.

**Constraints**: Hermetic tests (III). No new request to a source (FR-015, V). No external asset.
`TreatWarningsAsErrors` stays on.

**Scale/Scope**: 1 column, 1 migration, 1 new static class, 2 sources, 1 repository query, 1 DTO
reshape, 1 page section, 1 page fixture, the `001` contract amended.

## Constitution Check

*Checked against `.specify/memory/constitution.md` v1.4.0. Re-checked after Phase 1 design; the
verdicts did not change.*

| Principle | Verdict | Evidence |
| --- | --- | --- |
| **I. Spec-Driven Development** | **Pass** | Grilled on 2026-10-10: eight clarifications, no open decision. Two plan-level choices (R1, R5) were put to the maintainer. The `001` contract is amended through [contracts/http-api.md](./contracts/http-api.md). |
| **II. Test-Driven Development** | **Pass, with one recorded limit** | `before_implement` runs `/speckit-tdd-run`. Every FR maps to a hermetic test ([quickstart.md](./quickstart.md) §1). US1 and US2 run end to end through the refresh task and `AdminController`. The fix wording is tested in `node:test`, because the page writes it (R5). Pixel width (SC-004) is not measurable in the fake DOM; the test asserts no-wrap and a character bound, and the real-server pass checks the width (R7). |
| **III. Hermetic Tests** | **Pass** | No new outgoing call. Source tests reuse the recorded search fixtures. |
| **IV. Jellyfin Compatibility** | **Pass** | GUID unchanged. No `PluginConfiguration` change. `002` migrates forward from `001`, the schema of every released version, and adds a nullable column, so existing rows stay valid. No operator action. |
| **V. Respectful Sources and Privacy** | **Pass** | The attempt detail is what the match already received. Nothing new is requested or sent. The page links to no source site (FR-016). |
| **VI. Simplicity** | **Pass** | One column, not new tables. The reason text is reused, not migrated. The candidate cap is applied when writing, so nothing unused is stored. No dependency. |

**Technical Constraints**: Held. SQLite with an embedded numbered script; the Web UI has no build
step, no framework and no external asset.

**Development Workflow**: Spec Kit order is followed. CI gate: `dotnet build` with zero warnings,
`dotnet test`, `node --test`. `CHANGELOG.md` gets an entry; the reshaped admin status response is
noted there, because a script reading `hint` stops finding it.

## Project Structure

### Documentation (this feature)

```text
specs/008-unmatched-artist-reasons/
├── plan.md              # This file
├── spec.md
├── research.md          # Phase 0: R1..R8
├── data-model.md        # Phase 1: match_detail, derived steps, read model
├── quickstart.md        # Phase 1: suite gate + real-server pass
├── contracts/
│   ├── http-api.md      # unmatched[].reasons; amends 001
│   └── admin-page.md    # Reason trail, fix sentences, tracking line
├── checklists/
└── tasks.md             # Phase 2: NOT created by /speckit-plan
```

### Source code (repository root)

```text
src/Jellyfin.Plugin.NewReleases/
├── Storage/
│   ├── Migrations/002_match_detail.sql   # NEW: ADD COLUMN match_detail (R1)
│   └── ArtistRepository.cs               # SetMatchAsync writes detail; GetCountsAsync(enabled) (R3, R4)
├── Matching/MatchReasons.cs              # NEW: reason constants, formats, Parse (R2)
├── Model/
│   ├── SourceRecords.cs                  # ArtistMatch + Detail; MatchDetail, MatchCandidate (R3)
│   └── StoredRecords.cs                  # UnmatchedAt: Status, Reason, Detail (R4)
├── Sources/
│   ├── MusicBrainzSource.cs              # reasons via MatchReasons; top-3 candidates (R3)
│   └── DeezerSource.cs                   # reasons via MatchReasons; candidates, counts (R3)
├── Api/
│   ├── Dtos.cs                           # UnmatchedArtistDto.Reasons; ReasonDto, StepDto, CandidateDto (R5)
│   └── AdminController.cs                # builds reasons; UnmatchedHint removed (R5)
└── Web/admin.html                        # Reason trail, fix sentences, tracking line (R6)

tests/Jellyfin.Plugin.NewReleases.Tests/
├── Matching/MatchReasonsTests.cs         # NEW
├── Sources/                              # candidates and counts on Unmatched results
├── Storage/                              # 002 on a 001 database; detail round-trip; listing by enabled source
├── Api/                                  # reasons shape; ResponseNamingTests fixture
└── Acceptance/ConfigureAndRunTests.cs    # US1/US2 through refresh and Admin/Status

tests/web/render-status.test.js           # trail, fixes, tracking line, a11y words, no-wrap
tests/fixtures/pages/admin-status.json    # rewritten with real reason shapes (R8)

specs/001-track-new-releases/contracts/http-api.md   # amended per contracts/http-api.md
CHANGELOG.md                                         # Unreleased entry
```

**Structure Decision**: The existing single plugin project and single test project. One new
source file and one new test file; no new folder.

## Complexity Tracking

None. No principle is deviated from.
