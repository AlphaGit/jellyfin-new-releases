---

description: "Task list for 008-unmatched-artist-reasons"
---

# Tasks: Explain each unmatched artist step by step

**Input**: Design documents from `/specs/008-unmatched-artist-reasons/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md),
[data-model.md](./data-model.md), [contracts/](./contracts/), [quickstart.md](./quickstart.md)

**Tests**: **Mandatory** (constitution II). Each test task comes before the implementation task
that makes it pass, and is observed failing first, with the failure recorded in
`tdd/cycle-log.md`. Every behavioural task carries the ids of the behaviours it covers in
brackets (`[U3]`, `[A1]`), from [`tdd/test-list.md`](./tdd/test-list.md). `/speckit-tdd-run`
ticks a task only through these ids. A task with no id is documentation or a gate run.
`/speckit-tdd-plan` moved T027 into Foundational, because the code it tests lands there, and
added T034–T045: one outer-loop gate per acceptance scenario.

**Organization**: A foundational phase reshapes the data both stories read (reason parsing, the
enabled-source listing, the `reasons` response, a minimal Reason column). Then one phase per user
story, in spec priority order. US1 and US2 both edit `admin.html` and `render-status.test.js`, so
run them one after the other.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel: different files, and no dependency on an incomplete task.
- **[Story]**: US1–US2 from `spec.md`.

## Commands

```bash
PATH=/opt/homebrew/opt/dotnet/bin:$PATH dotnet test --configuration Release --filter "FullyQualifiedName~{Class}.{Method}" -- RunConfiguration.TreatNoTestsAsError=true
PATH=/opt/homebrew/opt/dotnet/bin:$PATH dotnet test --configuration Release
node --test tests/web/{file}
node --test "tests/web/*.test.js"
```

---

## Phase 1: Setup (Shared Infrastructure)

None. No new project, dependency or folder.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The status response carries `reasons` built from the stored reason text, for enabled
sources only, and the page shows one Reason line per source. Both stories build on this.

### Tests (write first, observe red)

- [X] T001 [P] Create failing `tests/Jellyfin.Plugin.NewReleases.Tests/Matching/MatchReasonsTests.cs` for `MatchReasons` (research R2, [data-model.md](./data-model.md) "Derived: the matching steps"):
  - `LowScore(70)` is `low score (70)`; `Ambiguous(92, 90)` is `ambiguous (score 92 vs 90)`; the `NoResult` and `NoCorroboratingAlbum` constants are `no result` and `no corroborating album`.
  - `Parse("musicbrainz", …)` maps `no result` → `Search`, `low score (70)` → `Score` with values `[70]`, `ambiguous (score 92 vs 90)` → `Unique` with `[92, 90]`.
  - `Parse("deezer", …)` maps `no result` → `Name`, `no corroborating album` → `Album`, with no values.
  - Any other text, an empty text, and a known text under the wrong source return null.
  - `Steps(source, failedStep)` returns every step of the source in order: before the failed step `Passed`, the failed step `Failed` with its values, after it `NotReached`. For MusicBrainz, `Id` is always `Failed`. [U1] [U2] [U3] [U4] [U5] [U6] [U7] [U8] [U9] [U10] [U11] [U12] [U13] [U14] [U15]
- [X] T002 [P] Add failing cases to `tests/Jellyfin.Plugin.NewReleases.Tests/Sources/MusicBrainzSourceTests.cs` and `tests/Jellyfin.Plugin.NewReleases.Tests/Sources/DeezerSourceTests.cs`: for every existing Unmatched fixture case, `MatchReasons.Parse(source.Id, match.Reason)` returns the expected failed step. Red because `MatchReasons` does not exist. [U16] [U23]
- [X] T003 [P] Add failing cases to `tests/Jellyfin.Plugin.NewReleases.Tests/Storage/ArtistRepositoryTests.cs` for `GetCountsAsync(enabledSourceIds, ct)` (research R4):
  - An artist Unmatched only at a source outside `enabledSourceIds` is not listed.
  - A listed artist has one `UnmatchedAt` per enabled source, in the order given, with `Status` `Matched`, `Unmatched` or `NotChecked`.
  - An enabled source with no row, or a `Pending` row, is `NotChecked`; `Reason` is null for every status but `Unmatched`.
  - The disabled source's row is still in `artist_source`; calling again with that source enabled lists the artist with its stored reason. [U30] [U31] [U32] [U33] [U34] [U35]
- [X] T004 Add failing cases to `tests/Jellyfin.Plugin.NewReleases.Tests/Api/AdminControllerTests.cs` for `GET Admin/Status` ([contracts/http-api.md](./contracts/http-api.md)):
  - Each `unmatched[]` entry has `Reasons`, one per enabled source in the `Sources` order, and no `Sources` or `Hint` member.
  - An Unmatched MusicBrainz `ambiguous (score 92 vs 90)` row gives `FailedStep` `Unique` and the four steps with states and `Values` `[92, 90]` on `Unique` only.
  - A `Matched` entry and a `NotChecked` entry have `FailedStep` null, empty `Steps`, and null `Reason`.
  - An unrecognized reason gives `State` `Unmatched`, `FailedStep` null, empty `Steps`, and the stored text in `Reason`.
  - With Deezer disabled in the configuration, an artist Unmatched only at Deezer is absent.
  - `Candidates`, `ExactNameCount` and `AlbumsCompared` are null (no attempt detail yet). [U40] [U41] [U42] [U43] [U44] [U45] [U46]
- [X] T005 [P] Rewrite `tests/fixtures/pages/admin-status.json` `unmatched` with the shape of [contracts/http-api.md](./contracts/http-api.md), per research R8: an artist Matched at MusicBrainz and Unmatched at Deezer on `Album` with detail; an artist ambiguous at MusicBrainz with 3 candidates and Unmatched at Deezer on `Name` with near names; an artist with a row from before the upgrade (`candidates` null). Update the sample in `tests/Jellyfin.Plugin.NewReleases.Tests/Api/ResponseNamingTests.cs` to build the same shape. `ResponseNamingTests` fails. [U49]
- [X] T006 [P] Add failing cases to `tests/web/render-status.test.js`, reading `admin-status.json`:
  - The table header is **Artist / Reason / How to fix**.
  - The Reason cell has one line per `reasons` entry, led by the source's `displayName` from `sources`.
  - A `Matched` entry reads `Matched`, a `NotChecked` entry reads `Not checked`, and an entry with `failedStep` null shows its `reason` text, escaped.
  - The old fixed hint text appears nowhere. [U52] [U53] [U54] [U55] [U56] [U57] [A9]

- [X] T027 Add failing cases to `tests/Jellyfin.Plugin.NewReleases.Tests/Acceptance/ConfigureAndRunTests.cs`, using `AcceptanceRig`:
  - After a refresh where the MusicBrainz search returns `artist_search_low_score.json`, `AdminStatusAsync` gives steps `Id` Failed, `Search` Passed, `Score` Failed with `[n]`, `Unique` NotReached.
  - With Deezer disabled, an artist Unmatched only at Deezer is absent; with Deezer enabled again, it is listed with its stored result, with no refresh between. [A10] [A11] [U51]

### Implementation

- [X] T007 Create `src/Jellyfin.Plugin.NewReleases/Matching/MatchReasons.cs` with the constants, `LowScore`, `Ambiguous`, `Parse` and `Steps`. Build every reason in `src/Jellyfin.Plugin.NewReleases/Sources/MusicBrainzSource.cs` and `src/Jellyfin.Plugin.NewReleases/Sources/DeezerSource.cs` through it. T001 and T002 pass; the existing reason-text assertions stay green. [U1] [U2] [U3] [U4] [U5] [U6] [U7] [U8] [U9] [U10] [U11] [U12] [U13] [U14] [U15] [U16] [U23]
- [X] T008 Change `UnmatchedAt` in `src/Jellyfin.Plugin.NewReleases/Model/StoredRecords.cs` to `(Source, Status, Reason)`. Rewrite `GetCountsAsync` in `src/Jellyfin.Plugin.NewReleases/Storage/ArtistRepository.cs` to take the enabled source IDs and list per research R4. T003 passes. [U30] [U31] [U32] [U33] [U34] [U35] [A10] [A11]
- [X] T009 In `src/Jellyfin.Plugin.NewReleases/Api/Dtos.cs`, replace `UnmatchedArtistDto.Sources` and `Hint` with `Reasons` (`ReasonDto`: `Source`, `State`, `FailedStep`, `Steps`, `Reason`, `Candidates`, `ExactNameCount`, `AlbumsCompared`; `StepDto`: `Name`, `State`, `Values`; `CandidateDto`: `Name`, `Score`, `Id`), and remove `UnmatchedSourceDto`. In `src/Jellyfin.Plugin.NewReleases/Api/AdminController.cs`, pass `configuration.EnabledSourceIds()` in the `sources` order, build `Reasons` through `MatchReasons`, and remove `UnmatchedHint`. T004 and T005 pass. [U40] [U41] [U42] [U43] [U44] [U45] [U46] [U49] [U51]
- [X] T010 In `src/Jellyfin.Plugin.NewReleases/Web/admin.html`, rename the header to Artist / Reason / How to fix and render one Reason line per `reasons` entry as T006 states. Leave How to fix empty for now. T006 passes; the existing `render-status.test.js` cases that read the old shape are updated in the same step. [U52] [U53] [U54] [U55] [U56] [U57] [A9]
- [ ] T011 Amend the admin status example and the `admin.html` table description in `specs/001-track-new-releases/contracts/http-api.md` per [contracts/http-api.md](./contracts/http-api.md).

**Checkpoint**: Both suites green. The page lists the right artists with a per-source state line.

---

## Phase 3: User Story 1 — See a fix that applies to the failure (Priority: P1) 🎯 MVP

**Goal**: The plugin stores the candidates and Deezer counts of each failed match. How to fix
gives one fix per failed source, specific to the failed step, then the tracking line.

**Independent Test**: Seed an artist Matched at MusicBrainz and Unmatched at Deezer with
`no corroborating album`. The status and page give a Deezer Album fix with counts, no
MusicBrainz artist ID fix, and "Tracked through MusicBrainz."

### Tests for User Story 1 (write first, observe red)

- [X] T012 [P] [US1] Add a failing case to `tests/Jellyfin.Plugin.NewReleases.Tests/Storage/DatabaseTests.cs`: a database built with `001` only, holding an `artist_source` row, migrates to `002`; the row survives with `match_detail` null, and `schema_version` records 2. [U39]
- [X] T013 [P] [US1] Add failing cases to `tests/Jellyfin.Plugin.NewReleases.Tests/Sources/MusicBrainzSourceTests.cs`:
  - `artist_search_ambiguous.json` gives `Detail.Candidates` = the 3 results in score order with name, score and ID.
  - `artist_search_low_score.json` gives its candidates, at most 3.
  - `artist_search_empty.json` gives empty candidates.
  - A search returning 4 or more results (built with `SourceJson.MusicBrainz.Search`) keeps only the first 3 candidates.
  - A match by stored ID and a confident match carry no detail.
  - An Unmatched attempt sends the same requests as before (`StubHttpMessageHandler.ReceivedRequests`). [U17] [U18] [U19] [U20] [U21] [U22]
- [X] T014 [P] [US1] Add failing cases to `tests/Jellyfin.Plugin.NewReleases.Tests/Sources/DeezerSourceTests.cs`:
  - `search_artist_exact.json` with library artist `Daft Punk Live` gives `no result` and the first 3 names in response order, with null score and ID.
  - `search_artist_empty.json` gives empty candidates.
  - `search_artist_homonyms.json` with no album title in common gives `no corroborating album`, `ExactNameCount` 2, and `AlbumsCompared` equal to the first-page items served for both candidates.
  - A corroborated match carries no detail.
  - The requests sent are the same as before (FR-015: nothing new to a source). [U24] [U25] [U26] [U27] [U28] [U29]
- [X] T015 [P] [US1] Add failing cases to `tests/Jellyfin.Plugin.NewReleases.Tests/Storage/ArtistRepositoryTests.cs`: `SetMatchAsync` with an Unmatched result and detail is read back by `GetCountsAsync` with the same candidates and counts; a later `Matched` result for the same pair leaves `match_detail` null. [U36] [U37] [U38]
- [X] T016 [US1] Add failing cases to `tests/Jellyfin.Plugin.NewReleases.Tests/Api/AdminControllerTests.cs`: stored detail appears as `Candidates` (Deezer with null `Score` and `Id`), `ExactNameCount` and `AlbumsCompared`; a row without detail keeps them null. [U47] [U48]
- [ ] T017 [US1] Add failing cases to `tests/web/render-status.test.js` for How to fix, per [contracts/admin-page.md](./contracts/admin-page.md):
  - MusicBrainz `Search`: the artist-ID sentence only.
  - MusicBrainz `Unique` with candidates: the sentence, then each `{name} ({score})` and its ID inside a `<code>`; the stylesheet gives that `<code>` `user-select: all`.
  - Deezer `Name` with names: the names, comma-separated, and "Deezer matches by name only"; with empty or null candidates: "Deezer may not list this artist."
  - Deezer `Album` with detail: both counts; without detail: the sentence from "No Deezer album has the same title".
  - A row failing at both sources has two different paragraphs; a Deezer-only failure never contains "MusicBrainz artist ID" (SC-001).
  - The last line is `Tracked through MusicBrainz.` when an entry is Matched, otherwise `Not tracked.`
  - Every candidate name is escaped. [U68] [U69] [U70] [U71] [U72] [U73] [U74] [U75] [U76] [U77] [U78] [U79] [U80] [U81] [U82] [U83] [A1] [A2] [A3] [A4] [A5] [A6]
- [X] T018 [US1] Add a failing case to `tests/Jellyfin.Plugin.NewReleases.Tests/Acceptance/ConfigureAndRunTests.cs`, using `AcceptanceRig`: after a refresh where MusicBrainz matches and Deezer finds exact-name artists with no album in common, `AdminStatusAsync` gives the Deezer entry `FailedStep` `Album` with both counts, and the MusicBrainz entry `Matched`. [U50]

### Implementation for User Story 1

- [X] T019 [US1] Add `src/Jellyfin.Plugin.NewReleases/Storage/Migrations/002_match_detail.sql` with `ALTER TABLE artist_source ADD COLUMN match_detail TEXT;`. T012 passes. [U39]
- [X] T020 [US1] In `src/Jellyfin.Plugin.NewReleases/Model/SourceRecords.cs`, add `MatchCandidate(Name, Score, Id)`, `MatchDetail(Candidates, ExactNameCount, AlbumsCompared)`, and `ArtistMatch.Detail`; `Unmatched(reason, detail)` carries it and `Matched` does not. [U17] [U20] [U21] [U24] [U28] [U36]
- [X] T021 [US1] In `src/Jellyfin.Plugin.NewReleases/Sources/MusicBrainzSource.cs`, attach the first 3 sorted candidates to each Unmatched result (research R3). T013 passes. [U17] [U18] [U19] [U20] [U21] [U22]
- [X] T022 [US1] In `src/Jellyfin.Plugin.NewReleases/Sources/DeezerSource.cs`, attach the first 3 search results' names, and for `no corroborating album` the exact-name count and the albums compared (research R3). T014 passes. [U24] [U25] [U26] [U27] [U28] [U29]
- [X] T023 [US1] In `src/Jellyfin.Plugin.NewReleases/Storage/ArtistRepository.cs`, write `match_detail` as JSON in `SetMatchAsync` (null for a match) and read it into `UnmatchedAt.Detail` in `GetCountsAsync`; add `Detail` to `UnmatchedAt` in `src/Jellyfin.Plugin.NewReleases/Model/StoredRecords.cs`. T015 passes. [U36] [U37] [U38]
- [X] T024 [US1] In `src/Jellyfin.Plugin.NewReleases/Api/AdminController.cs`, map `Detail` to `Candidates`, `ExactNameCount` and `AlbumsCompared`. T016 and T018 pass. [U47] [U48] [U50]
- [ ] T025 [US1] In `src/Jellyfin.Plugin.NewReleases/Web/admin.html`, write the How to fix paragraphs, the candidate `<code>` with `user-select: all`, and the tracking line per [contracts/admin-page.md](./contracts/admin-page.md). T017 passes. [U68] [U69] [U70] [U71] [U72] [U73] [U74] [U75] [U76] [U77] [U78] [U79] [U80] [U81] [U82] [U83] [A1] [A2] [A3] [A4] [A5] [A6]

- [ ] T034 [US1] Confirm that the acceptance test for US1-AS1 is green in the full suite before US1 counts as complete. [A1]
- [ ] T035 [US1] Confirm that the acceptance test for US1-AS2 is green in the full suite before US1 counts as complete. [A2]
- [ ] T036 [US1] Confirm that the acceptance test for US1-AS3 is green in the full suite before US1 counts as complete. [A3]
- [ ] T037 [US1] Confirm that the acceptance test for US1-AS4 is green in the full suite before US1 counts as complete. [A4]
- [ ] T038 [US1] Confirm that the acceptance test for US1-AS5 is green in the full suite before US1 counts as complete. [A5]
- [ ] T039 [US1] Confirm that the acceptance test for US1-AS6 is green in the full suite before US1 counts as complete. [A6]

**Checkpoint**: US1 complete. Every fix on the page acts on a failed step.

---

## Phase 4: User Story 2 — See which steps passed and which failed (Priority: P2)

**Goal**: Each Unmatched source line shows the full step trail with marks, values, dimmed
not-reached steps and words for a screen reader, on one line.

**Independent Test**: Seed an artist Unmatched at MusicBrainz with `ambiguous (score 92 vs 90)`.
The line reads `MusicBrainz: ID ✗ · Search ✓ · Score ✓ · Unique ✗ 92 vs 90`, and each step
carries its state in words.

### Tests for User Story 2 (write first, observe red)

- [ ] T026 [US2] Add failing cases to `tests/web/render-status.test.js`, per [contracts/admin-page.md](./contracts/admin-page.md) "Reason column":
  - An Unmatched entry with a `failedStep` renders every step as `<short name> <mark>`, joined by ` · `, with `Id` shown as `ID`.
  - Marks: `✓` passed, `✗` failed, `–` not reached; each mark is `aria-hidden="true"` and is followed by visually hidden words (`passed`, `failed`, `not reached`) in an `nr-sr` element.
  - A not-reached step carries the class that the stylesheet dims; the stylesheet declares that class with an `opacity` below 1 and `.nr-sr` as visually hidden.
  - The failed step's values follow its mark: `Score ✗ 70`, `Unique ✗ 92 vs 90`.
  - The trail line declares `white-space: nowrap`, and the text of the longest trail (`MusicBrainz: ID ✗ · Search ✓ · Score ✓ · Unique ✗ 100 vs 95`, marks only) is at most 64 characters (research R7). [U58] [U59] [U60] [U61] [U62] [U63] [U64] [U65] [U66] [U67] [A7] [A8] [A12]

### Implementation for User Story 2

- [ ] T028 [US2] In `src/Jellyfin.Plugin.NewReleases/Web/admin.html`, render the trail spans of research R6 and add the CSS for `.nr-sr`, the dimmed not-reached step, and `white-space: nowrap` on the line. T026 passes. [U58] [U59] [U60] [U61] [U62] [U63] [U64] [U65] [U66] [U67] [A7] [A8] [A12]

- [ ] T040 [US2] Confirm that the acceptance test for US2-AS1 is green in the full suite before US2 counts as complete. [A7]
- [ ] T041 [US2] Confirm that the acceptance test for US2-AS2 is green in the full suite before US2 counts as complete. [A8]
- [X] T042 [US2] Confirm that the acceptance test for US2-AS3 is green in the full suite before US2 counts as complete. [A9]
- [X] T043 [US2] Confirm that the acceptance test for US2-AS4 is green in the full suite before US2 counts as complete. [A10]
- [X] T044 [US2] Confirm that the acceptance test for US2-AS5 is green in the full suite before US2 counts as complete. [A11]
- [ ] T045 [US2] Confirm that the acceptance test for US2-AS6 is green in the full suite before US2 counts as complete. [A12]

**Checkpoint**: US1 and US2 complete. Both suites green.

---

## Phase 5: Polish & Cross-Cutting Concerns

- [ ] T029 [P] Add an Unreleased entry to `CHANGELOG.md`: the Unmatched artists table shows per-step reasons and specific fixes; a disabled source no longer lists its artists; the admin status response replaces `sources` and `hint` with `reasons`.
- [ ] T030 Run `PATH=/opt/homebrew/opt/dotnet/bin:$PATH dotnet build --configuration Release`. The build has zero warnings (`TreatWarningsAsErrors`).
- [ ] T031 Run the full `dotnet test --configuration Release` and `node --test "tests/web/*.test.js"`. Both are green.
- [ ] T032 [P] Confirm that `HttpSurfaceTests.NoContractDocument_NamesARouteThePluginDoesNotServe` in `tests/Jellyfin.Plugin.NewReleases.Tests/Api/HttpSurfaceTests.cs` passes with the amended `001` contract and this feature's `contracts/http-api.md`.
- [ ] T033 Commit to `main`. Push only when the maintainer asks, then verify that the CI run for the push is green (`gh run list --branch main`).

The real-server pass in [quickstart.md](./quickstart.md) §2 is the maintainer's own pass after
release. A defect found there becomes a new spec.

---

## Dependencies & Execution Order

- **Foundational (T001–T011, T027)** blocks both stories. Inside it: T007 after T001–T002; T008
  after T003 and T027; T009 after T004–T005 and T007–T008; T010 after T006 and T009.
- **US1 (T012–T025)** after Foundational. Order: T019 (migration), T020 (model), then T021/T022
  (sources), T023 (repository), T024 (controller), T025 (page).
- **US2 (T026, T028)** after Foundational. Its server behaviour lands in Foundational (T027 tests
  it there); US2 adds page rendering only. Run after US1, because both edit `admin.html` and
  `render-status.test.js`.
- **Gates (T034–T045)**: each after the story's implementation tasks, one per acceptance scenario.
- **Polish (T029–T033)** after the stories you ship.

## Parallel Opportunities

- Foundational tests: T001, T002, T003, T005 and T006 touch different files.
- US1 tests: T012, T013, T014 and T015 touch different files.
- Implementation: T021 and T022 (one source each) after T020.

```text
# Foundational, red phase, together:
T001 MatchReasonsTests.cs   T002 source tests   T003 ArtistRepositoryTests.cs
T005 admin-status.json      T006 render-status.test.js

# US1, red phase, together:
T012 DatabaseTests.cs   T013 MusicBrainzSourceTests.cs   T014 DeezerSourceTests.cs   T015 ArtistRepositoryTests.cs
```

## Implementation Strategy

1. **MVP**: Foundational, then US1. The page already lists the right artists with a state line
   per source, and every fix is specific. This fixes the wrong hint that started this feature.
2. **Increment**: US2 adds the step trail.
3. Ship both in one release: the response reshape lands in Foundational, so a release with only
   part of the page would show a Reason column without its trail.
