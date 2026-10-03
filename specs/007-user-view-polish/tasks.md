---

description: "Task list for 007-user-view-polish"
---

# Tasks: Polish the New Releases view

**Input**: Design documents from `/specs/007-user-view-polish/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md),
[data-model.md](./data-model.md), [contracts/](./contracts/), [quickstart.md](./quickstart.md)

**Tests**: **Mandatory** (constitution II). Each test task comes before the implementation task
that makes it pass, and is observed failing first. Every behavioural task carries the ids of
the behaviours it covers in brackets (`[U31]`, `[A1]`), from [`tdd/test-list.md`](./tdd/test-list.md).
`/speckit-tdd-run` ticks a task only through these ids. A task with no id is infrastructure or
documentation. T027–T039 were added by `/speckit-tdd-plan` and continue the ID sequence: one
outer-loop gate per acceptance scenario.

**Regenerated 2026-10-03** after the clarification that removed the homonym disambiguation
(spec FR-005, research R3–R5). The earlier list's migration, MusicBrainz lookup, refresh step and
Artists-response tasks are gone. Task IDs restart at T001.

**Organization**: One phase per user story, in spec priority order. The stories are
independent: each one touches a different DTO field, a different part of the card, or different
CSS rules. US2, US3 and US4 share `styles.test.js` and the `<style>` block, so run them one after
the other.

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

**Purpose**: Remove what the dropped scope left in the tree.

- [X] T001 Delete `tests/fixtures/musicbrainz/artist_lookup.json` and `tests/fixtures/musicbrainz/artist_lookup_empty.json`, and remove their entry from `tests/fixtures/README.md`. No test reads them (plan, Technical Context). Run the full `dotnet test --configuration Release` and confirm 0 failed.

---

## Phase 2: Foundational (Blocking Prerequisites)

None. No story depends on another story's code.

---

## Phase 3: User Story 1 — Find an artist by typing (Priority: P1) 🎯 MVP

**Goal**: A native suggestion list of artist names replaces the Artist dropdown. The filter
applies only when the field text equals a name. The server does not change.

**Independent Test**: Load the view with many artists, type a fragment, and pick a suggestion.
The list shows only that artist's releases. Free text, an empty field and Clear show all artists.

### Tests for User Story 1 (write first, observe red)

- [X] T002 [P] [US1] Write failing `tests/web/artist-filter.test.js`, loading `src/Jellyfin.Plugin.NewReleases/Web/user-view.html` through `tests/web/load-page.js`:
  - `artistIndex` maps each `name` to its `jellyfinId`.
  - After the artists load from `tests/fixtures/pages/artists.json`, the `#nr-f-artist-list` datalist holds one escaped `<option value>` per name.
  - An `input` event whose text equals a name requests `Releases?artistId={id}` through the recording `ApiClient`.
  - Free text, empty text and the Clear button each request releases with no `artistId`.
  - Typing the same applied name again sends no second request.
  - When the Artists request fails, the field stays usable and releases are requested with no `artistId`.
  - The control is `<input id="nr-f-artist" list="nr-f-artist-list">` labelled "Artist", and the page adds no key handler to it. [A1] [A2] [A3] [A4] [A5] [U42] [U44] [U45] [U46]
- [X] T003 [US1] Add `artistIndex` to the exact member set asserted in `tests/web/exposure.test.js`. The test fails. [U47]

### Implementation for User Story 1

- [X] T004 [US1] In `src/Jellyfin.Plugin.NewReleases/Web/user-view.html`, replace `<select id="nr-f-artist">` with the `<input list>` + `<datalist id="nr-f-artist-list">` of [contracts/user-view.md](./contracts/user-view.md). Rewrite `loadArtists` to fill the datalist and build the `artistIndex` map. Make the `input` handler and the Clear button set or clear `artistId`. Expose `artistIndex` on `NewReleasesInternals`. T002 and T003 pass. [A1] [A2] [A3] [A4] [A5] [U42] [U44] [U45] [U46] [U47]

- [X] T027 [US1] Confirm that the acceptance test for US1-AS1 is green in the full suite before US1 counts as complete. [A1]
- [X] T028 [US1] Confirm that the acceptance test for US1-AS2 is green in the full suite before US1 counts as complete. [A2]
- [X] T029 [US1] Confirm that the acceptance test for US1-AS3 is green in the full suite before US1 counts as complete. [A3]
- [X] T030 [US1] Confirm that the acceptance test for US1-AS4 is green in the full suite before US1 counts as complete. [A4]
- [X] T031 [US1] Confirm that the acceptance test for US1-AS5 is green in the full suite before US1 counts as complete. [A5]

**Checkpoint**: US1 complete. `node --test` is green, and `dotnet test` is unchanged.

---

## Phase 4: User Story 2 — Recognise a release by its cover (Priority: P2)

**Goal**: Each card shows a lazy, decorative cover. It tries Deezer, then Cover Art Archive,
then a placeholder.

**Independent Test**: Render one release with both sources and one with MusicBrainz only.
Make every image fail. Both cards end with the placeholder box and no `<img>`.

### Tests for User Story 2 (write first, observe red)

- [X] T005 [P] [US2] Add a failing case to `tests/Jellyfin.Plugin.NewReleases.Tests/Storage/ReleaseRepositoryTests.cs`: each `ListedRelease.Sources` entry carries the `SourceReleaseId` stored in `source_entry`. [U31]
- [X] T006 [P] [US2] Add failing cases to `tests/Jellyfin.Plugin.NewReleases.Tests/Api/ReleasesControllerTests.cs` for `GET Releases`:
  - A release with both sources gets `Covers` = `[https://api.deezer.com/album/{id}/image?size=medium, https://coverartarchive.org/release-group/{id}/front-250]`, in that order.
  - A Deezer-only release gets only its Deezer URL. A MusicBrainz-only release gets only its Cover Art Archive URL.
  - A source ID with URL-reserved characters is escaped.
  - `Sources` keeps its existing order. [U32] [U33] [U34] [U35] [U36]
- [X] T007 [P] [US2] Add a `covers` array to every item in `tests/fixtures/pages/releases.json`, `releases-filtered.json` and `releases-stale.json`. `ResponseNamingTests` in `tests/Jellyfin.Plugin.NewReleases.Tests/Api/ResponseNamingTests.cs` now fails on the missing field. [U37]
- [X] T008 [P] [US2] Extend `tests/web/fake-dom.js` so that `addEventListener(type, handler, options)` keeps `options` readable by a test, and an element supports `remove()` (it detaches the element from its owner). Cover both with failing cases in `tests/web/fake-dom.test.js` first. [U38] [U39]
- [X] T009 [US2] Add failing cases to `tests/web/render.test.js`:
  - Each row writes `<div class="nr-cover"><img …>` with `src` = `covers[0]`, `data-fallback` = the rest in order, `alt=""`, `loading="lazy"`, `referrerpolicy="no-referrer"`, `width="64"` and `height="64"`.
  - A row with `covers: []` writes the box with no `<img>`.
  - Cover URLs are written through `esc`. [A10] [U48] [U49] [U50] [U51] [U52] [U53] [U54]
- [X] T010 [US2] Add failing cases to a new `tests/web/cover-fallback.test.js`:
  - `nextCover` moves the first `data-fallback` URL into `src` and keeps the rest.
  - With no fallback left, it removes the `<img>`, and the cover box stays.
  - The panel registers one `error` listener for the capture phase. [A8] [A9] [U55] [U56] [U57]
- [X] T011 [US2] Add `nextCover` to the exact member set in `tests/web/exposure.test.js`. The test fails. [U58]
- [X] T012 [P] [US2] Create failing `tests/web/styles.test.js`. It reads the `<style>` block of `src/Jellyfin.Plugin.NewReleases/Web/user-view.html` and asserts that `.nr-cover` declares a 64 × 64 box with a background and that `.nr-cover img` declares `object-fit: cover`. [U60] [U61]
- [X] T013 [US2] Add a failing case to `tests/Jellyfin.Plugin.NewReleases.Tests/Acceptance/BrowseReleasesTests.cs`, using `AcceptanceRig`: after a refresh that stores a release at both sources, `GET Releases` returns its two cover URLs in Deezer-first order. [A7]

### Implementation for User Story 2

- [X] T014 [US2] Add `SourceReleaseId` to `SourceLink` in `src/Jellyfin.Plugin.NewReleases/Model/StoredRecords.cs`. Select `source_release_id` in the `sources` JSON of the list query in `src/Jellyfin.Plugin.NewReleases/Storage/ReleaseRepository.cs`. T005 passes. [U31]
- [X] T015 [US2] Add `IReadOnlyList<string> Covers` to `ReleaseDto` in `src/Jellyfin.Plugin.NewReleases/Api/Dtos.cs`. Build it in `ToDto` in `src/Jellyfin.Plugin.NewReleases/Api/ReleasesController.cs`, in the order and URL formats of research R6. T006, T007 and T013 pass. [A7] [U32] [U33] [U34] [U35] [U36] [U37]
- [X] T016 [US2] In `src/Jellyfin.Plugin.NewReleases/Web/user-view.html`, change `render` to write the cover box as the first grid cell. Add `nextCover` and the panel's capture-phase `error` listener, and expose `nextCover`. Add the CSS rules `.nr-row { grid-template-columns: 64px 1fr auto }`, `.nr-cover` (64×64, neutral background) and `.nr-cover img { object-fit: cover }`. T009, T010, T011 and T012 pass. [A8] [A9] [A10] [U48] [U49] [U50] [U51] [U52] [U53] [U54] [U55] [U56] [U57] [U58] [U60] [U61]
- [X] T017 [US2] Amend the `ReleaseDto` section of `specs/001-track-new-releases/contracts/http-api.md` with `covers` and its rules from [contracts/http-api.md](./contracts/http-api.md).

- [X] T032 [US2] Confirm that the acceptance test for US2-AS1 is green in the full suite before US2 counts as complete. [A7]
- [X] T033 [US2] Confirm that the acceptance test for US2-AS2 is green in the full suite before US2 counts as complete. [A8]
- [X] T034 [US2] Confirm that the acceptance test for US2-AS3 is green in the full suite before US2 counts as complete. [A9]
- [X] T035 [US2] Confirm that the acceptance test for US2-AS4 is green in the full suite before US2 counts as complete. [A10]

**Checkpoint**: US1 and US2 complete. Both suites are green.

---

## Phase 5: User Story 3 — Action buttons read as a pair (Priority: P3)

**Goal**: "Ignore", "Have it" and "Restore" share one style. The pair has equal widths and
shared edges, also on narrow screens.

**Independent Test**: The `<style>` block declares the stretch rules. In a browser, both buttons
have equal width and both edges aligned (quickstart §2.4).

- [X] T018 [US3] Add failing cases to `tests/web/styles.test.js` (created by T012). It asserts:
  - `.nr-actions` declares `align-items: stretch`.
  - `.nr-actions button` declares `width: 100%`.
  - A `@media (max-width: 600px)` block puts `.nr-actions` on its own row with equal columns (research R10). [A11] [U59]
- [X] T019 [US3] Add a case to `tests/web/render.test.js`: an Archive-tab row writes its "Restore" button inside the same `.nr-actions` container as the List-tab buttons. [A12]
- [X] T020 [US3] Change the `.nr-actions` rules and add the `600px` media query in `src/Jellyfin.Plugin.NewReleases/Web/user-view.html`. T018 passes. [A11] [A12] [U59]

- [X] T036 [US3] Confirm that the acceptance test for US3-AS1 is green in the full suite before US3 counts as complete. [A11]
- [X] T037 [US3] Confirm that the acceptance test for US3-AS2 is green in the full suite before US3 counts as complete. [A12]

**Checkpoint**: US3 complete.

---

## Phase 6: User Story 4 — Read the source link (Priority: P3)

**Goal**: The source link has a contrast of at least 4.5:1 on the dark card, and still reads as
a link.

**Independent Test**: The computed contrast of the declared link colour against `#1c1c1c` is at
least 4.5.

- [X] T021 [US4] Add failing cases to `tests/web/styles.test.js`:
  - A rule for both `.nr-links a` and `.nr-links a:visited` declares a `color`.
  - That colour's WCAG contrast ratio against `#1c1c1c` is ≥ 4.5. Compute it in the test with the relative-luminance formula; no library. Pin the formula with `#0000ee` on `#1c1c1c` (below 4.5) and `#ffffff` on `#000000` (21).
  - No rule removes the link underline, and focus keeps its outline. [A13] [A14] [U62] [U63]
- [X] T022 [US4] Add `.nr-links a, .nr-links a:visited { color: #00a4dc; }` to `src/Jellyfin.Plugin.NewReleases/Web/user-view.html`. T021 passes. [A13] [A14]

- [X] T038 [US4] Confirm that the acceptance test for US4-AS1 is green in the full suite before US4 counts as complete. [A13]
- [X] T039 [US4] Confirm that the acceptance test for US4-AS2 is green in the full suite before US4 counts as complete. [A14]

**Checkpoint**: All four stories complete.

---

## Phase 7: Polish & Cross-Cutting Concerns

- [X] T023 Run `PATH=/opt/homebrew/opt/dotnet/bin:$PATH dotnet build --configuration Release`. The build has zero warnings (`TreatWarningsAsErrors`).
- [X] T024 Run the full `dotnet test --configuration Release` and `node --test "tests/web/*.test.js"`, and the node suite again with `LANG=de_DE.UTF-8`. All are green.
- [X] T025 [P] Confirm that `HttpSurfaceTests.NoContractDocument_NamesARouteThePluginDoesNotServe` in `tests/Jellyfin.Plugin.NewReleases.Tests/Api/HttpSurfaceTests.cs` still passes with the amended `001` contract and this feature's `contracts/http-api.md`.
- [X] T026 Commit to `main`. Push only when the maintainer asks, then verify that the CI run for the push is green (`gh run list --branch main`).

The real-browser pass in [quickstart.md](./quickstart.md) §2 is the maintainer's own pass after
release. A defect found there becomes a new spec.

---

## Dependencies & Execution Order

- **Setup (T001)**: No dependencies. It only removes files no test reads.
- **US1 (T002–T004)**: Page only. Independent of every other story.
- **US2 (T005–T017)**: Independent of US1. Server order inside the phase: T014 (repository), then
  T015 (DTO and controller). The page work (T016) needs T008 (fake DOM) first.
- **US2 → US3 → US4** for the stylesheet: T012 creates `tests/web/styles.test.js`, and T018 and
  T021 add to it. All three edit the same `<style>` block, so run them in this order.
- **Shared files**: `user-view.html` and `exposure.test.js` are edited by US1 and US2. When the
  stories run in parallel worktrees, merge them one at a time.
- **Acceptance gates**: T027–T039 close their story. Each one needs the story's implementation
  tasks to be done.
- **Polish**: Starts after every story you chose to deliver.

### Parallel opportunities

- US1 tests: T002 alone (T003 shares `exposure.test.js` with T011).
- US2 tests: T005 ∥ T006 ∥ T007 ∥ T008 ∥ T012. Different files.
- Across stories: US1 and US2 can run in parallel worktrees, merged one at a time.

```text
# US2 red phase, together:
T005 ReleaseRepositoryTests  ·  T006 ReleasesControllerTests  ·  T007 releases*.json
T008 fake-dom  ·  T012 styles.test.js
```

## Implementation Strategy

1. **MVP = US1.** The filter is the P1 pain on large libraries. It is page-only now, and it can
   ship alone.
2. **US2** next. It needs no migration, and covers appear for every stored release at once.
3. **US3 + US4** are small CSS changes. Do them together at the end.
4. After each checkpoint, the suites are green and the increment can be released as it is.

## Notes

- Every test is hermetic. No cover URL is fetched.
- Do not edit a test in the same commit as the behaviour change it covers when refactoring
  (constitution II).
- Mark deliberate simplifications with a `ponytail:` comment that names the ceiling.

---

## Phase 8: TDD remediation

From [`tdd/verification.md`](./tdd/verification.md) (verdict **FAIL**, audited at `5846c02`). **The
feature is not done until T040–T043 are cleared.** Each test change is proven by a red observed
before the fix, or by the named mutant, applied from a file copy and restored with a `cmp -s` check,
never `git checkout`.

- [ ] T040 [US1] Finding 1 (HIGH): in `tests/web/artist-filter.test.js:92-98`, replace A4's single example with a table of texts that equal no artist name with case ignored (`As`, `ASP `, `constructor`, `toString`, `hasOwnProperty`, `__proto__`), each typed and then left. Then drive A15: a name typed in any case applies that artist (maintainer decision 2026-10-03; U42 changes with it). Observe each red, then make the name lookup own-keys only and case-blind in `src/Jellyfin.Plugin.NewReleases/Web/user-view.html:75, 101`. Done when `node --test tests/web/artist-filter.test.js` is green and the A4 table fails against the old lookup. [A4] [A15] [U42]
- [ ] T041 [US4] Finding 2 (HIGH): make A14's underline test in `tests/web/styles.test.js:102-106` reject `text-decoration-line: none` and a rule broader than `.nr-links a` that reaches the link. Done when mutant N9 (`text-decoration-line: none` on `.nr-links a`) fails `node --test tests/web/styles.test.js`. [A14]
- [ ] T042 [US2] Finding 3 (HIGH): replace `Boolean(cover.background)` in `tests/web/styles.test.js:32` with an assertion that the background is a visible colour (reject `none`, `transparent`, zero alpha). Done when mutant N6 (`background: none` on `.nr-cover`) fails `node --test tests/web/styles.test.js`. [U60]
- [X] T043 Finding 4 (HIGH, blocking): append a cycle-log entry that labels A3, A4, A5, A12, A14, U33, U34, U36, U45, U62, U64 and U65 as test-after, with the evidence the audit cites, and records the maintainer's dated decision to accept them or not. Do not edit past entries. New tests cannot fix this. Done when the entry exists, names all twelve, and a fresh `/speckit-tdd-verify` grades them.
- [ ] T044 [US4] Finding 5 (MED): add a behaviour to `tdd/test-list.md` for US4-AS2's "keeps the same contrast" on hover and focus, then drive its test in `tests/web/styles.test.js`. Done when mutant N10 (`.nr-links a:hover { color: #0000ee }`) fails `node --test tests/web/styles.test.js`.
- [X] T045 Finding 6 (MED): maintainer decision. Either amend the page-side conventions in `.specify/memory/tdd-profile.md` to permit reading markup attributes and the `<style>` block as text (with the reason), or extend `tests/web/fake-dom.js` so A5 and `styles.test.js` stop reading source text. Done when the profile and `tests/web/artist-filter.test.js:101-105` and `tests/web/styles.test.js:12-27` agree.
- [X] T046 Finding 7 (MED): maintainer decision on `decoding="async"` and the centred note glyph named in T009 and T016. Either add them to `tdd/test-list.md` and drive them, or remove them from `contracts/user-view.md:34` and from T009/T016's text. Done when `tasks.md`, the contract and `user-view.html` agree.
- [ ] T047 [US1] Finding 8 (LOW): in `tests/web/artist-filter.test.js:95-97, 133-136`, assert on the requests sent after typing (`requests.slice(before)`), not on the initial load. Add a `change` (leave the field) case for US1-AS4. Done when `node --test tests/web/artist-filter.test.js` is green and R3 still fails A4.
- [ ] T048 Finding 9 (LOW): make `tests/web/cover-fallback.test.js:54-69` share one cover-markup reader with `tests/web/render.test.js:186-200`, with one decoding rule. Done when `node --test "tests/web/*.test.js"` is green and mutants N3 and N4 still fail.
- [ ] T049 Finding 10 (LOW): let U57 in `tests/web/cover-fallback.test.js:46` accept either `true` or `{ capture: true }`. Done when mutant N5 (`false`) still fails it.
- [ ] T050 Finding 11 (LOW): after the maintainer's push, verify the CI run with `gh run list --branch main` and record it, or untick T026 until then.
