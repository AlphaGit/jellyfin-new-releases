# Quickstart: validate Explain each unmatched artist step by step

## 1. Suite gate (hermetic)

Prerequisite: .NET 10 SDK and Node 22. No network, no Jellyfin server.

```bash
dotnet build --configuration Release      # zero warnings
dotnet test --configuration Release
node --test "tests/web/*.test.js"
```

What covers what:

| Spec | Test area | Proves |
| --- | --- | --- |
| FR-003, FR-004, FR-011 | `Matching/MatchReasonsTests` | Every reason each source writes parses to its failed step and values; old texts parse the same; unknown text gives no step |
| FR-015, FR-016 (cap) | `Sources/MusicBrainzSourceTests`, `Sources/DeezerSourceTests` | Unmatched results carry up to 3 candidates (MusicBrainz with score and ID); Deezer Album carries the counts; a match carries none |
| FR-015 (stored) | `Storage/ArtistRepositoryTests`, `Storage/DatabaseTests` | `match_detail` round-trips; a match clears it; `002` applies on a `001` database with existing rows |
| FR-002, FR-014 | `Storage/ArtistRepositoryTests` | Only artists Unmatched at an enabled source are listed; every enabled source has an entry; a missing or `Pending` row is `NotChecked`; a re-enabled source shows its stored row |
| FR-013, contract | `Api/AdminControllerTests`, `Api/ResponseNamingTests` | `reasons` shape and order; `hint` gone; names match `admin-status.json` |
| US1, US2 end to end | `Acceptance/ConfigureAndRunTests` | A refresh over recorded fixtures, then `Admin/Status`, gives the expected `reasons` |
| FR-001, FR-003, FR-005, FR-010 | `tests/web/render-status.test.js` | Columns, trail marks and words, one step per bullet, dimmed not-reached, no-wrap, every step ≤ 20 characters |
| FR-006–FR-009, FR-016 | `tests/web/render-status.test.js` | Each fix sentence per (source, step); candidates and `<code>` IDs; no detail → no candidates; tracking line; no MusicBrainz ID fix on a Deezer-only failure (SC-001) |

## 2. Real-server pass (maintainer)

Per the project's practice, the maintainer runs this pass on their own server after release.

1. Install the release on a server that ran `0.2.1` and has unmatched artists. Do not refresh.
2. Open Dashboard → Plugins → New Releases. Expected:
   - Columns read **Artist / Reason / How to fix**.
   - Every row has a trail per enabled source (SC-005). Fixes show no candidates yet.
   - No row with only a Deezer failure shows the MusicBrainz artist ID fix (SC-001).
   - Each row ends with "Tracked through …" or "Not tracked." (SC-002).
3. At a 1280-pixel browser width, every step stays on one line, and How to fix takes at least half
   the table width (SC-004).
4. Run a refresh. Expected: MusicBrainz `Score` and `Unique` fixes list candidates with IDs, and a
   double-click on an ID selects all of it. Deezer `Album` fixes give the counts.
5. Disable Deezer and save. Expected: artists Unmatched only at Deezer leave the table. Enable it
   again: they return before any refresh.
6. With a screen reader, read one trail: each step is announced as passed, failed or not reached.
