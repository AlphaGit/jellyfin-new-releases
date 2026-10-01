---

description: "Task list for 006-user-view-polish"
---

# Tasks: Polish the New Releases view

**Input**: Design documents from `/specs/006-user-view-polish/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md),
[data-model.md](./data-model.md), [contracts/](./contracts/), [quickstart.md](./quickstart.md)

**Tests**: **Mandatory** (constitution II). Each test task comes before the implementation task
that makes it pass, and is observed failing first. Every behavioural task carries the ids of
the behaviours it covers in brackets (`[U3]`, `[A1]`), from [`tdd/test-list.md`](./tdd/test-list.md).
`/speckit-tdd-run` ticks a task only through these ids. A task with no id is infrastructure or
documentation. T040–T055 were added by `/speckit-tdd-plan` and continue the ID sequence, so
they are not in numeric order in the file.

**Organization**: One phase per user story, in spec priority order. The stories are
independent: each one touches different DTO fields, a different part of the card, or different
CSS rules. US3 and US4 share `styles.test.js` and the `<style>` block, so run them one after the
other.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel: different files, and no dependency on an incomplete task.
- **[Story]**: US1–US4 from `spec.md`.

## Commands

```bash
PATH=/opt/homebrew/opt/dotnet/bin:$PATH dotnet test --configuration Release --filter "FullyQualifiedName~{Class}.{Method}" -- RunConfiguration.TreatNoTestsAsError=true
PATH=/opt/homebrew/opt/dotnet/bin:$PATH dotnet test --configuration Release
node --test tests/web/{file}
node --test "tests/web/*.test.js"
```

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: The recorded MusicBrainz responses that the US1 lookup tests read.

- [ ] T001 [P] Create `tests/fixtures/musicbrainz/artist_lookup.json` from a real `GET https://musicbrainz.org/ws/2/artist/{mbid}?fmt=json` response for an artist with a non-empty `disambiguation`. Scrub it under constitution III and `tests/fixtures/README.md`: keep `id`, `name`, `disambiguation` and enough surrounding fields to stay realistic.
- [ ] T002 [P] Create `tests/fixtures/musicbrainz/artist_lookup_empty.json`: the same shape with `"disambiguation": ""`.
- [ ] T003 Record both fixtures and the endpoint they come from in `tests/fixtures/README.md`.
- [ ] T056 Restore a green baseline before the loop starts. In `tests/Jellyfin.Plugin.NewReleases.Tests/Packaging/RepositoryManifestTests.cs`, raise `PublishedVersionsToday` from 1 to 2, because `repo/manifest.json` lists 0.1.0 and 0.1.1. Then run the full `dotnet test --configuration Release` and confirm 0 failed. Commit it on its own, separate from this feature's behaviour (`tdd/cycle-log.md`, Notes). This fixes a defect that predates the feature, so it carries no behaviour ID.

---

## Phase 2: Foundational (Blocking Prerequisites)

None. No story depends on another story's code. Migration `002` serves only US1, so it is in
that phase.

---

## Phase 3: User Story 1 — Find an artist by typing (Priority: P1) 🎯 MVP

**Goal**: A native suggestion list replaces the Artist dropdown. Homonyms show their MusicBrainz
disambiguation text. The server fetches the text only for colliding artists.

**Independent Test**: Load the view with many artists, type a fragment, and pick a suggestion.
The list shows only that artist's releases. Two server-wide homonyms show their disambiguation
text, and a unique name shows the name alone.

### Tests for User Story 1 (write first, observe red)

- [ ] T004 [P] [US1] Add a failing case to `tests/Jellyfin.Plugin.NewReleases.Tests/Storage/DatabaseTests.cs`: a database created at schema `001`, opened by the current `PluginDatabase`, gains `library_artist.disambiguation` and `library_artist.disambiguation_mbid`. Both are `NULL` on the existing rows. In the same file, change the three existing assertions of schema version 1 / one `schema_version` row to version 2 / two rows, as their own behavior change (U2). [U1] [U2]
- [ ] T005 [P] [US1] Add failing cases to `tests/Jellyfin.Plugin.NewReleases.Tests/Storage/ArtistRepositoryTests.cs`:
  - `SetDisambiguationAsync` round-trips through `GetAllAsync`.
  - A later `UpsertAsync` of the same artist keeps both columns.
  - `GetDisambiguationCandidatesAsync` returns colliding artists, by `TitleNormalizer.NormalizeName`, whose effective MBID (tag MBID, else the `Matched` MusicBrainz `source_artist_id`) differs from `disambiguation_mbid`.
  - It excludes unique names, artists with no effective MBID, and artists already fetched for the same MBID.
  - It includes an artist whose MBID changed since the fetch. [U3] [U4] [U5] [U6] [U7] [U8] [U9] [U10] [U11] [U12]
- [ ] T006 [P] [US1] Add failing cases to `tests/Jellyfin.Plugin.NewReleases.Tests/Sources/MusicBrainzSourceTests.cs`: `FetchArtistDisambiguationAsync` requests `artist/{mbid}?fmt=json` with the MBID URL-escaped. It returns the `disambiguation` of `artist_lookup.json`, and `""` for `artist_lookup_empty.json`. It goes through `SourceHttpClient`, so an exhausted budget throws `DailyBudgetExhaustedException`. [U13] [U14] [U15] [U16]
- [ ] T007 [US1] Add failing cases to `tests/Jellyfin.Plugin.NewReleases.Tests/ScheduledTasks/RefreshNewReleasesTaskTests.cs`:
  - A run with two homonyms and one unique artist sends exactly two lookups and stores both texts.
  - A second run sends none.
  - An artist matched by search in the same run is looked up in that run, because the step runs after the rotation.
  - MusicBrainz disabled or unavailable means no lookup.
  - `DailyBudgetExhaustedException` stops the step, and the run still completes.
  - Any other lookup exception counts one error and continues with the next candidate. [U17] [U18] [U19] [U20] [U21] [U22] [U23] [U24]
- [ ] T008 [P] [US1] Add failing cases to `tests/Jellyfin.Plugin.NewReleases.Tests/Api/ReleasesControllerTests.cs` for `GET Artists`:
  - Two artists with the same normalized name and stored texts get `Disambiguation` = their texts.
  - A unique name gets `null`, even when a stale text is stored.
  - A stored `""` gives `null`.
  - A caller who can see only one of two homonyms still gets the text, because the collision is server-wide (FR-005a). [U25] [U26] [U27] [U28] [U29]
- [ ] T009 [P] [US1] Update `tests/fixtures/pages/artists.json` to one homonym pair carrying `disambiguation` strings and one artist with `"disambiguation": null`. `ResponseNamingTests` in `tests/Jellyfin.Plugin.NewReleases.Tests/Api/ResponseNamingTests.cs` now fails on the missing field. [U30]
- [ ] T010 [P] [US1] Write failing `tests/web/artist-filter.test.js`:
  - `artistLabel` gives `name — disambiguation` or `name`.
  - `artistIndex` maps a label to a `jellyfinId`, and the first artist wins on a duplicate label.
  - After `loadArtists` with `tests/fixtures/pages/artists.json`, the `#nr-f-artist-list` datalist holds one `<option value>` per label.
  - An `input` event with an exact label sends `Releases?artistId={id}` through a recording `ApiClient`.
  - Free text sends no `artistId`. Empty text, or the Clear button, removes `artistId`. [A1] [A2] [A3] [A4] [A5] [U40] [U41] [U42] [U43] [U44] [U45] [U46]
- [ ] T011 [US1] Add `artistLabel` and `artistIndex` to the exact set asserted in `tests/web/exposure.test.js`. The test fails. [U47]
- [ ] T012 [US1] Add a failing acceptance case to `tests/Jellyfin.Plugin.NewReleases.Tests/Acceptance/ConfigureAndRunTests.cs`, using `AcceptanceRig` with the stubbed MusicBrainz lookup: after a refresh over a library with two tagged homonyms, `GET Artists` returns both with their texts, and the unique artist without one. [A6]

### Implementation for User Story 1

- [ ] T013 [US1] Create `src/Jellyfin.Plugin.NewReleases/Storage/Migrations/002_artist_disambiguation.sql` with the two `ALTER TABLE library_artist ADD COLUMN` statements in [data-model.md](./data-model.md). T004 passes. [U1] [U2]
- [ ] T014 [US1] Add `Disambiguation` and `DisambiguationMbid` to `LibraryArtist` in `src/Jellyfin.Plugin.NewReleases/Model/StoredRecords.cs`. In `src/Jellyfin.Plugin.NewReleases/Storage/ArtistRepository.cs`, read both columns in `QueryArtistsAsync`, and add `SetDisambiguationAsync(long id, string mbid, string text, CancellationToken)` and `GetDisambiguationCandidatesAsync(CancellationToken)` (returns `(LibraryArtist, string EffectiveMbid)`). Leave `UpsertAsync` unchanged. T005 passes. [U3] [U4] [U5] [U6] [U7] [U8] [U9] [U10] [U11] [U12]
- [ ] T015 [US1] Add `FetchArtistDisambiguationAsync(string mbid, CancellationToken)` to `src/Jellyfin.Plugin.NewReleases/Sources/MusicBrainzSource.cs`. Do not add it to `IReleaseSource` (research R4). T006 passes. [U13] [U14] [U15] [U16]
- [ ] T016 [US1] Add the disambiguation step to `src/Jellyfin.Plugin.NewReleases/ScheduledTasks/RefreshNewReleasesTask.cs`, after the rotation loop and before ownership. Resolve the source with `_sources.OfType<MusicBrainzSource>()` and gate it on `IsEnabled` and `_http.IsAvailableAsync`. Handle failures as in research R4. T007 passes. [U17] [U18] [U19] [U20] [U21] [U22] [U23] [U24]
- [ ] T017 [US1] Add `string? Disambiguation` to `ArtistDto` in `src/Jellyfin.Plugin.NewReleases/Api/Dtos.cs`. In `GetArtistsAsync` in `src/Jellyfin.Plugin.NewReleases/Api/ReleasesController.cs`, group all artists by `TitleNormalizer.NormalizeName(name)` before the access filter. Emit the text only for groups of two or more and a non-empty text (research R3). T008, T009 and T012 pass. [A6] [U25] [U26] [U27] [U28] [U29] [U30]
- [ ] T018 [US1] In `src/Jellyfin.Plugin.NewReleases/Web/user-view.html`, replace `<select id="nr-f-artist">` with the `<input list>` + `<datalist id="nr-f-artist-list">` of [contracts/user-view.md](./contracts/user-view.md). Rewrite `loadArtists` to fill the datalist and build the `artistIndex` map. Make the `input` handler and the Clear button set or clear `artistId`. Expose `artistLabel` and `artistIndex` on `NewReleasesInternals`. T010 and T011 pass. [A1] [A2] [A3] [A4] [A5] [U40] [U41] [U42] [U43] [U44] [U45] [U46] [U47]
- [ ] T019 [US1] Amend the `GET /Artists` section of `specs/001-track-new-releases/contracts/http-api.md` with the `disambiguation` field and its rule from [contracts/http-api.md](./contracts/http-api.md).

- [ ] T042 [US1] Confirm that the acceptance test for US1-AS1 is green in the full suite before US1 counts as complete. [A1]
- [ ] T043 [US1] Confirm that the acceptance test for US1-AS2 is green in the full suite before US1 counts as complete. [A2]
- [ ] T044 [US1] Confirm that the acceptance test for US1-AS3 is green in the full suite before US1 counts as complete. [A3]
- [ ] T045 [US1] Confirm that the acceptance test for US1-AS4 is green in the full suite before US1 counts as complete. [A4]
- [ ] T046 [US1] Confirm that the acceptance test for US1-AS5 is green in the full suite before US1 counts as complete. [A5]
- [ ] T047 [US1] Confirm that the acceptance test for US1-AS6 is green in the full suite before US1 counts as complete. [A6]

**Checkpoint**: US1 complete. `dotnet test` and `node --test` are green.

---

## Phase 4: User Story 2 — Recognise a release by its cover (Priority: P2)

**Goal**: Each card shows a lazy, decorative cover. It tries Deezer, then Cover Art Archive,
then a placeholder.

**Independent Test**: Render one release with both sources and one with MusicBrainz only.
Make every image fail. Both cards end with the placeholder box and no `<img>`.

### Tests for User Story 2 (write first, observe red)

- [ ] T020 [P] [US2] Add a failing case to `tests/Jellyfin.Plugin.NewReleases.Tests/Storage/ReleaseRepositoryTests.cs`: each `ListedRelease.Sources` entry carries the `SourceReleaseId` stored in `source_entry`. [U31]
- [ ] T021 [P] [US2] Add failing cases to `tests/Jellyfin.Plugin.NewReleases.Tests/Api/ReleasesControllerTests.cs` for `GET Releases`:
  - A release with both sources gets `Covers` = `[https://api.deezer.com/album/{id}/image?size=medium, https://coverartarchive.org/release-group/{id}/front-250]`, in that order.
  - A single-source release gets only its own URL.
  - The IDs are URL-escaped.
  - `Sources` keeps its existing order. [U32] [U33] [U34] [U35] [U36]
- [ ] T022 [P] [US2] Add `covers` arrays to every item in `tests/fixtures/pages/releases.json`, `releases-filtered.json` and `releases-stale.json`. `ResponseNamingTests` now fails on the missing field. [U37]
- [ ] T023 [P] [US2] Extend `tests/web/fake-dom.js` so that `addEventListener(type, handler, options)` records `options`, and the element supports `remove()` (it detaches the element from its owner). Cover both with failing cases in `tests/web/fake-dom.test.js` first. [U38] [U39]
- [ ] T024 [US2] Add failing cases to `tests/web/render.test.js`:
  - Each row writes `<div class="nr-cover"><img …>` with `src` = `covers[0]`, `data-fallback` = the rest, `alt=""`, `loading="lazy"`, `decoding="async"`, `referrerpolicy="no-referrer"`, `width="64"` and `height="64"`.
  - A row with `covers: []` writes the box with no `<img>`.
  - Cover URLs are escaped with `esc`. [A10] [U48] [U49] [U50] [U51] [U52] [U53] [U54]
- [ ] T025 [US2] Add failing cases to a new `tests/web/cover-fallback.test.js`:
  - `nextCover` moves the first `data-fallback` URL into `src` and keeps the rest.
  - On the last failure, it removes the `<img>`.
  - The panel registers one `error` listener with capture. [A8] [A9] [U55] [U56] [U57]
- [ ] T026 [US2] Add `nextCover` to the exact set in `tests/web/exposure.test.js`. The test fails. [U58]
- [ ] T041 [P] [US2] Create failing `tests/web/styles.test.js`. It reads the `<style>` block of `src/Jellyfin.Plugin.NewReleases/Web/user-view.html` and asserts that `.nr-cover` declares a 64 × 64 box with a background and that `.nr-cover img` declares `object-fit: cover`. [U60] [U61]
- [ ] T027 [US2] Add a failing case to `tests/Jellyfin.Plugin.NewReleases.Tests/Acceptance/BrowseReleasesTests.cs`: after a refresh that stores a release at both sources, `GET Releases` returns its two cover URLs in Deezer-first order. [A7]

### Implementation for User Story 2

- [ ] T028 [US2] Add `SourceReleaseId` to `SourceLink` in `src/Jellyfin.Plugin.NewReleases/Model/StoredRecords.cs`. Select `source_release_id` in the `sources` JSON of the list query in `src/Jellyfin.Plugin.NewReleases/Storage/ReleaseRepository.cs`. T020 passes. [U31]
- [ ] T029 [US2] Add `IReadOnlyList<string> Covers` to `ReleaseDto` in `src/Jellyfin.Plugin.NewReleases/Api/Dtos.cs`. Build it in `ToDto` in `src/Jellyfin.Plugin.NewReleases/Api/ReleasesController.cs`, in the order and URL formats of research R6. T021, T022 and T027 pass. [A7] [U32] [U33] [U34] [U35] [U36] [U37]
- [ ] T030 [US2] In `src/Jellyfin.Plugin.NewReleases/Web/user-view.html`, change `render` to write the cover box as the first grid cell. Add `nextCover` and the panel's capture-phase `error` listener, and expose `nextCover`. Add the CSS rules `.nr-row { grid-template-columns: 64px 1fr auto }`, `.nr-cover` (64×64, neutral background, centred note glyph) and `.nr-cover img { object-fit: cover }`. T024, T025 and T026 pass. [A8] [A9] [A10] [U48] [U49] [U50] [U51] [U52] [U53] [U54] [U55] [U56] [U57] [U58] [U60] [U61]
- [ ] T031 [US2] Amend the `ReleaseDto` section of `specs/001-track-new-releases/contracts/http-api.md` with `covers` and its rules from [contracts/http-api.md](./contracts/http-api.md).

- [ ] T048 [US2] Confirm that the acceptance test for US2-AS1 is green in the full suite before US2 counts as complete. [A7]
- [ ] T049 [US2] Confirm that the acceptance test for US2-AS2 is green in the full suite before US2 counts as complete. [A8]
- [ ] T050 [US2] Confirm that the acceptance test for US2-AS3 is green in the full suite before US2 counts as complete. [A9]
- [ ] T051 [US2] Confirm that the acceptance test for US2-AS4 is green in the full suite before US2 counts as complete. [A10]

**Checkpoint**: US1 and US2 complete. Both suites are green.

---

## Phase 5: User Story 3 — Action buttons read as a pair (Priority: P3)

**Goal**: "Ignore", "Have it" and "Restore" share one style. The pair has equal widths and
shared edges, also on narrow screens.

**Independent Test**: The `<style>` block declares the stretch rules. In a browser, both buttons
have equal width and both edges aligned (quickstart §2.5).

- [ ] T032 [US3] Add failing cases to `tests/web/styles.test.js` (created by T041). It reads the `<style>` block of `src/Jellyfin.Plugin.NewReleases/Web/user-view.html` and asserts:
  - `.nr-actions` declares `align-items: stretch`.
  - `.nr-actions button` declares `width: 100%`.
  - A `@media (max-width: 600px)` block puts `.nr-actions` on its own row with equal columns (research R10). [A11] [U59]
- [ ] T040 [US3] Add a failing case to `tests/web/render.test.js`: an Archive-tab row writes its "Restore" button inside the same `.nr-actions` container as the List-tab buttons. [A12]
- [ ] T033 [US3] Change the `.nr-actions` rules and add the `600px` media query in `src/Jellyfin.Plugin.NewReleases/Web/user-view.html`. T032 passes. [A11] [A12] [U59]

- [ ] T052 [US3] Confirm that the acceptance test for US3-AS1 is green in the full suite before US3 counts as complete. [A11]
- [ ] T053 [US3] Confirm that the acceptance test for US3-AS2 is green in the full suite before US3 counts as complete. [A12]

**Checkpoint**: US3 complete.

---

## Phase 6: User Story 4 — Read the source link (Priority: P3)

**Goal**: The source link has a contrast of at least 4.5:1 on the dark card, and still reads as
a link.

**Independent Test**: The computed contrast of the declared link colour against `#1c1c1c` is at
least 4.5.

- [ ] T034 [US4] Add failing cases to `tests/web/styles.test.js`:
  - A rule for both `.nr-links a` and `.nr-links a:visited` declares a `color`.
  - That colour's WCAG contrast ratio against `#1c1c1c` is ≥ 4.5. Compute it in the test with the relative-luminance formula; no library.
  - No rule removes the link underline. [A13] [A14] [U62] [U63]
- [ ] T035 [US4] Add `.nr-links a, .nr-links a:visited { color: #00a4dc; }` to `src/Jellyfin.Plugin.NewReleases/Web/user-view.html`. T034 passes. [A13] [A14]

- [ ] T054 [US4] Confirm that the acceptance test for US4-AS1 is green in the full suite before US4 counts as complete. [A13]
- [ ] T055 [US4] Confirm that the acceptance test for US4-AS2 is green in the full suite before US4 counts as complete. [A14]

**Checkpoint**: All four stories complete.

---

## Phase 7: Polish & Cross-Cutting Concerns

- [ ] T036 Run `PATH=/opt/homebrew/opt/dotnet/bin:$PATH dotnet build --configuration Release`. The build has zero warnings (`TreatWarningsAsErrors`).
- [ ] T037 Run the full `dotnet test --configuration Release` and `node --test "tests/web/*.test.js"`. Both are green.
- [ ] T038 [P] Confirm that `HttpSurfaceTests.NoContractDocument_NamesARouteThePluginDoesNotServe` still passes with the amended `001` contract and this feature's `contracts/http-api.md`.
- [ ] T039 Commit to `main` and push. Verify that the CI run for the push is green (`gh run list --branch main`). The feature is done only then (constitution, Development Workflow).

The real-browser pass in [quickstart.md](./quickstart.md) §2 is the maintainer's own pass after
release. A defect found there becomes a new spec.

---

## Dependencies & Execution Order

- **Setup (T001–T003, T056)**: No dependencies. US1 uses T001–T003 only for T006. **T056 blocks every test task**: `/speckit-tdd-run` must not start on a red suite.
- **US1 (T004–T019)**: Depends on Setup. The order inside the phase is:
  1. T013 (migration).
  2. T014 (repository).
  3. T015 (source method).
  4. T016 (refresh step).
  5. T017 (endpoint).
  6. T018 (page).
- **US2 (T020–T031)**: Independent of US1. It touches different DTO fields, a different
  repository query and a different part of the card. It can start after Setup, or before.
- **US2 → US3 → US4** for the stylesheet: T041 (US2) creates `tests/web/styles.test.js`, and
  US3 and US4 add to it. All three edit the same `<style>` block, so run them in this order.
- **Shared files**: `user-view.html`, `Dtos.cs`, `ReleasesController.cs`, `StoredRecords.cs`
  and `exposure.test.js` are edited by more than one story. When stories run in parallel
  worktrees, merge them one at a time.
- **Acceptance gates**: T042–T055 close their story. Each one needs the story's implementation
  tasks to be done.
- **Polish**: Starts after every story you chose to deliver.

### Parallel opportunities

- Setup: T001 ∥ T002.
- US1 tests: T004 ∥ T005 ∥ T006 ∥ T008 ∥ T009 ∥ T010. These are different files. T007 waits for
  T006, because it uses the same lookup method signature.
- US2 tests: T020 ∥ T021 ∥ T022 ∥ T023 ∥ T041.
- Across stories: US1 and US2 can run in parallel worktrees, merged one at a time.

```text
# US1 red phase, together:
T004 DatabaseTests  ·  T005 ArtistRepositoryTests  ·  T006 MusicBrainzSourceTests
T008 ReleasesControllerTests (Artists)  ·  T009 artists.json  ·  T010 artist-filter.test.js

# US2 red phase, together:
T020 ReleaseRepositoryTests  ·  T021 ReleasesControllerTests (Releases)  ·  T022 releases*.json  ·  T023 fake-dom
```

## Implementation Strategy

1. **MVP = US1.** The filter is the P1 pain on large libraries. Ship it alone if needed. The
   migration and the lookup go with it.
2. **US2** next. It needs no migration, and covers appear for every stored release at once.
3. **US3 + US4** are small CSS changes. Do them together at the end.
4. After each checkpoint, the suites are green and the increment can be released as it is.

## Notes

- Every test is hermetic. No cover URL is fetched. The MusicBrainz lookup uses the stubbed
  handler and the T001/T002 fixtures.
- Do not edit a test in the same commit as the behaviour change it covers when refactoring
  (constitution II).
- Mark deliberate simplifications with a `ponytail:` comment that names the ceiling. One example
  is the duplicate-label rule "first in name order".
