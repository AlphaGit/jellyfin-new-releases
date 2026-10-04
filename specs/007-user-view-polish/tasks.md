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

- [X] T040 [US1] Finding 1 (HIGH): in `tests/web/artist-filter.test.js:92-98`, replace A4's single example with a table of texts that equal no artist name with case ignored (`As`, `ASP `, `constructor`, `toString`, `hasOwnProperty`, `__proto__`), each typed and then left. Then drive A15: a name typed in any case applies that artist (maintainer decision 2026-10-03; U42 changes with it). Observe each red, then make the name lookup own-keys only and case-blind in `src/Jellyfin.Plugin.NewReleases/Web/user-view.html:75, 101`. Done when `node --test tests/web/artist-filter.test.js` is green and the A4 table fails against the old lookup. [A4] [A15] [U42]
- [X] T041 [US4] Finding 2 (HIGH): make A14's underline test in `tests/web/styles.test.js:102-106` reject `text-decoration-line: none` and a rule broader than `.nr-links a` that reaches the link. Done when mutant N9 (`text-decoration-line: none` on `.nr-links a`) fails `node --test tests/web/styles.test.js`. [A14]
- [X] T042 [US2] Finding 3 (HIGH): replace `Boolean(cover.background)` in `tests/web/styles.test.js:32` with an assertion that the background is a visible colour (reject `none`, `transparent`, zero alpha). Done when mutant N6 (`background: none` on `.nr-cover`) fails `node --test tests/web/styles.test.js`. [U60]
- [X] T043 Finding 4 (HIGH, blocking): append a cycle-log entry that labels A3, A4, A5, A12, A14, U33, U34, U36, U45, U62, U64 and U65 as test-after, with the evidence the audit cites, and records the maintainer's dated decision to accept them or not. Do not edit past entries. New tests cannot fix this. Done when the entry exists, names all twelve, and a fresh `/speckit-tdd-verify` grades them.
- [X] T044 [US4] Finding 5 (MED): add a behaviour to `tdd/test-list.md` for US4-AS2's "keeps the same contrast" on hover and focus, then drive its test in `tests/web/styles.test.js`. Done when mutant N10 (`.nr-links a:hover { color: #0000ee }`) fails `node --test tests/web/styles.test.js`. [A16]
- [X] T045 Finding 6 (MED): maintainer decision. Either amend the page-side conventions in `.specify/memory/tdd-profile.md` to permit reading markup attributes and the `<style>` block as text (with the reason), or extend `tests/web/fake-dom.js` so A5 and `styles.test.js` stop reading source text. Done when the profile and `tests/web/artist-filter.test.js:101-105` and `tests/web/styles.test.js:12-27` agree.
- [X] T046 Finding 7 (MED): maintainer decision on `decoding="async"` and the centred note glyph named in T009 and T016. Either add them to `tdd/test-list.md` and drive them, or remove them from `contracts/user-view.md:34` and from T009/T016's text. Done when `tasks.md`, the contract and `user-view.html` agree.
- [X] T047 [US1] Finding 8 (LOW): in `tests/web/artist-filter.test.js:95-97, 133-136`, assert on the requests sent after typing (`requests.slice(before)`), not on the initial load. Add a `change` (leave the field) case for US1-AS4. Done when `node --test tests/web/artist-filter.test.js` is green and R3 still fails A4.
- [X] T048 Finding 9 (LOW): make `tests/web/cover-fallback.test.js:54-69` share one cover-markup reader with `tests/web/render.test.js:186-200`, with one decoding rule. Done when `node --test "tests/web/*.test.js"` is green and mutants N3 and N4 still fail.
- [X] T049 Finding 10 (LOW): let U57 in `tests/web/cover-fallback.test.js:46` accept either `true` or `{ capture: true }`. Done when mutant N5 (`false`) still fails it.
- [X] T050 Finding 11 (LOW): after the maintainer's push, verify the CI run with `gh run list --branch main` and record it, or untick T026 until then. Done: `main` pushed at `8875456` on 2026-10-04; CI run `37167992270` (`build`) passed.

---

## Phase 9: TDD remediation

From the second [`tdd/verification.md`](./tdd/verification.md) (verdict **FAIL**, audited at
`ad2b277`). **The feature is not done until T051–T054 are cleared.** Each test change is proven by a
red observed before the fix, or by the named mutant. Apply each mutant to a file copy, then restore
it and check the restore with `cmp -s`. Never use `git checkout`. Add a table row before you change a
predicate (profile: "a predicate needs a table").

- [X] T051 [US4] Finding 1 (HIGH): add accepting rows to the `reachesSourceLink` table in `tests/web/styles.test.js:163-179`: `#nr-user-view .nr-row a`, `#nr-user-view .nr-list a:hover`, `#nr-user-view .nr-row div a`, `#nr-user-view .nr-links a[href]`, `#nr-user-view .nr-links :any-link`, `#nr-user-view .nr-links *`. Keep `.nr-artist a` as a rejecting row. Observe the red, then fix the predicate at `:152-157`. Done when mutants P1, P2 and P3 each fail `node --test tests/web/styles.test.js`. [A14] [A16]
- [X] T052 [US2] Finding 2 (HIGH): make U57 in `tests/web/cover-fallback.test.js:44-51` reject a registration with `once`. Put `true` and `{ capture: true }` as accepting rows, and `false`, `{ capture: true, once: true }` and `{}` as rejecting rows. Done when mutant E2 (`}, { capture: true, once: true });` at `user-view.html:270`) fails `node --test tests/web/cover-fallback.test.js`, and the page rewritten to `{ capture: true }` passes it. [U57]
- [X] T053 Finding 3 (HIGH, blocking): append a cycle-log entry that records the maintainer's dated decision on A16 (cycle 54, test-after, N10 and N8 caught). Do not edit past entries. Done when the entry exists and a fresh `/speckit-tdd-verify` grades A16 `TEST_AFTER_ACCEPTED`, or when A16 is re-driven with a red. [A16]
- [X] T054 [US2] Finding 4 (HIGH): add rejecting rows `initial`, `inherit`, `unset`, `revert` and `revert-layer` to the `isVisibleColour` table in `tests/web/styles.test.js:42-62`. Observe the red, then fix the predicate at `:30-40`. Done when mutant P4 (`background: initial` on `.nr-cover`) fails `node --test tests/web/styles.test.js`. [U60]
- [X] T055 [US4] Finding 5 (MED): add a behaviour to `tdd/test-list.md` for US4-AS2: no rule that reaches a source link lowers its contrast through `opacity` (or `filter`). Then drive its test in `tests/web/styles.test.js`. Done when mutant P9 (`#nr-user-view .nr-links a:hover { opacity: .3 }`) fails `node --test tests/web/styles.test.js`. [A17]
- [X] T056 Finding 6 (LOW): append a cycle-log entry that maps cycles 49–54 and the two refactor entries to their commits (`64ff918`, `6495435`, `3e4bc76`, `352f5d8`, `baff78c`, `6880ddd`, `7767c12`, `ad2b277`). Do not edit past entries. Done when each of those cycles has a named commit in `tdd/cycle-log.md`.
- [X] T057 Finding 7 (LOW): make `declarations()` and `rules()` in `tests/web/styles.test.js:17-27, 138-149` share one CSS rule parser. Do not change behaviour. Done when `node --test tests/web/styles.test.js` stays green, and N9 and N10 still fail it.

---

## Phase 10: TDD remediation

From the third [`tdd/verification.md`](./tdd/verification.md) (verdict **FAIL**, audited at
`f68901c`). **The feature is not done until T058–T061 are cleared.** Take the decision in T062 before
you start them: it sets whether they are fixed with more table rows or with a closed-world check.
Each test change is proven by a red observed before the fix, or by the named mutant. Each mutant is a
rule added before the `@media` block of `src/Jellyfin.Plugin.NewReleases/Web/user-view.html`, unless
the task names a line. Apply each mutant to a file copy, then restore it and check the restore with
`cmp -s`. Never use `git checkout`. Add a table row before you change a predicate (profile: "a
predicate needs a table").

- [X] T058 [US4] Finding 1 (HIGH): make A14's focus test in `tests/web/styles.test.js:255-257` read every rule through `rules()` and `reachesSourceLink`. Add a predicate that finds a removed outline (`outline: none`, `outline: 0`, `outline-style: none`, `outline-width: 0`), with its own table, and keep the present `:focus-visible` check. Done when mutant Q4 (`#nr-user-view .nr-links a:focus-visible { outline: none; }`) fails `node --test tests/web/styles.test.js`, and R13b still fails it. [A14]
- [X] T059 [US4] Finding 2 (HIGH): add accepting rows `#nr-user-view .nr-meta ~ .nr-links a`, `#nr-user-view .nr-title ~ .nr-links a:hover`, `#nr-user-view .nr-cover + div a` and `#nr-user-view #nr-filters ~ #nr-panel a` to the `reachesSourceLink` table in `tests/web/styles.test.js:170-193`. Observe the red, then stop reading a compound before `~` or `+` as an ancestor at `:157-162`. Done when mutants Q1, Q2, Q3 and Q9 each fail `node --test tests/web/styles.test.js`, and P1, P2 and P3 still fail it. [A14] [A16]
- [X] T060 [US4] Finding 3 (HIGH): add accepting rows `{ 'text-decoration-color': 'transparent' }`, `{ 'text-decoration': 'underline transparent' }` and `{ 'text-decoration-thickness': '0' }` to the `removesUnderline` table in `tests/web/styles.test.js:199-208`. Observe the red, then fix the predicate at `:166-168`. Done when mutant Q5 (`#nr-user-view .nr-links a { text-decoration-color: transparent; }`) fails `node --test tests/web/styles.test.js`, and N9 and R13a still fail it. [A14]
- [X] T061 [US2] Finding 4 (HIGH): add rejecting rows `transparent none`, `none transparent` and `rgba(0,0,0,0) none` to the `isVisibleColour` table in `tests/web/styles.test.js:48-70`. Observe the red, then fix the predicate at `:36-46`. Done when mutant Q8 (`background: transparent none;` on `.nr-cover`, `user-view.html:15`) fails `node --test tests/web/styles.test.js`, and P4 and N6 still fail it. [U60]
- [X] T062 Finding 6 (MED): maintainer decision on how `styles.test.js` reads CSS. Either a closed-world check (pin the exact list of selectors whose subject can be an anchor or a focus state, so any new such rule fails until it is reviewed), or more table rows with a recorded ceiling and the remaining risk sent to `quickstart.md` §2. Done when a dated cycle-log entry records the decision. For a closed-world check, also done when mutants Q1–Q5 and Q9 each fail `node --test tests/web/styles.test.js`.
- [X] T063 [US4] Finding 5 (MED): add a behaviour to `tdd/test-list.md` for US4-AS2: hover and focus keep the link's background, and no rule dims an ancestor of the link. Then drive its test in `tests/web/styles.test.js`. Or record both as out of scope in the test list, with the maintainer's dated decision. Done when mutants Q6 (`#nr-user-view .nr-links a:hover { background: #00a4dc; }`) and Q7 (`#nr-user-view .nr-row:hover .nr-links { opacity: .3; }`) each fail `node --test tests/web/styles.test.js`, or when the test list records the decision. [A18]
- [X] T064 Finding 7 (LOW): link T044 to `A16` and T055 to `A17`. Add the ids to those tasks, or record the mapping in a new cycle-log entry if the task text must stay unchanged. Done when `grep -n 'A16\|A17' specs/007-user-view-polish/tasks.md specs/007-user-view-polish/tdd/cycle-log.md` shows each task with its id.

---

## Phase 11: TDD remediation

From the fourth [`tdd/verification.md`](./tdd/verification.md) (verdict **FAIL**, audited at
`8e7daf7`). **The feature is not done until T065–T068 are cleared.** Take the decision in T069 before
you start them. A closed-world check of the `<style>` block clears all four through one test. More
table rows clear them one input form at a time. Each mutant is a rule added before the `@media`
block of `src/Jellyfin.Plugin.NewReleases/Web/user-view.html`, with the `#nr-user-view ` prefix,
unless the task names a line. Apply each mutant to a file copy, then restore it and check the
restore. Never use `git checkout`. Add a table row before you change a predicate (profile: "a
predicate needs a table").

- [X] T065 [US2] Finding 1 (HIGH): make U60, U61, U66, A11 and A14's visited-colour test in `tests/web/styles.test.js:96-113, 336-338` see every rule that reaches their element, with each property read through its shorthand and longhands, or cover them by T069's closed-world check. Done when mutants S2 (`.nr-links a[href]:visited { color: #c58af9; }`), S10 (`.nr-cover { background-color: transparent; }`), X1 (`.nr-row { grid-template: auto / 1fr 64px auto; }`), X2 (`.nr-row .nr-cover img { object-fit: fill; }`) and X3 (`.nr-row .nr-actions button { width: auto; }`) each fail `node --test tests/web/styles.test.js`. [U60] [U61] [U66] [A11] [A14]
- [X] T066 [US4] Finding 2 (HIGH): make the value predicates in `tests/web/styles.test.js:43-58, 138-148, 201-205, 268-272, 328-334, 348-352` read `all`, an alpha in hex colours, a colour equal to the card, `!important` with or without a space, and `box-shadow` as a background, or cover them by T069's closed-world check. Done when mutants S1 (`.nr-links a:hover { color: #00a4dc66; }`), S3 (`.nr-links a { all: unset; }`), S5 (`.nr-links a:focus-visible { outline-color: #1c1c1c; }`), S6 (`.nr-links a:hover { text-decoration-color: #1c1c1c; }`), M2 (`.nr-links a:focus-visible { outline: none!important; }`), M3 (`background: transparent!important;` on `.nr-cover`, `user-view.html:15`), M4 (`.nr-links a:hover { opacity: 50% !important; }`) and S8 (`.nr-links a:hover { box-shadow: inset 0 0 0 2em #3a3a3a; }`) each fail `node --test tests/web/styles.test.js`. [A14] [A16] [A17] [A18] [U60]
- [X] T067 [US4] Finding 3 (HIGH): make `compounds`, `reachesSourceLink` and `reachesLinkAncestor` in `tests/web/styles.test.js:174-195, 299-304` ignore combinators inside brackets and parentheses, ignore names inside `:not()`, and compare type selectors without case, or cover them by T069's closed-world check. Add accepting rows `.nr-links a:not([download])`, `.nr-links a[rel~="noopener"]`, `.nr-links a:nth-child(2n+1)`, `ARTICLE:hover` and `.nr-row div:not(.nr-meta)`. Done when mutants S4 (`.nr-links a:not([download]) { text-decoration-line: none; }`), M1 (`.nr-links a[rel~="noopener"] { text-decoration: none; }`) and S9 (`ARTICLE:hover { opacity: .6; }`) each fail `node --test tests/web/styles.test.js`. [A14] [A17] [A18]
- [X] T068 Finding 4 (HIGH): clear the six recorded-ceiling survivors. Either T069's closed-world check catches C1–C5 and S7, or the maintainer adds a row for recorded-ceiling survivors to `.specify/templates/overrides/tdd-test-quality-rubric.md`, as on 2026-10-01 for `TEST_AFTER_ACCEPTED`. Done when mutants C1–C5 and S7 (`tdd/verification.md`, "Mutation results") each fail `node --test tests/web/styles.test.js`, or when the override row exists and a dated cycle-log entry records the decision.
- [X] T069 Finding 5 (MED): maintainer decision on how `styles.test.js` reads the stylesheet. Option one is a closed-world check: a reviewed, hand-written list of every rule in the `<style>` block, compared with the page, so any added or changed rule fails until the list is updated. Option two is more table rows for each input form. Also make the predicates pass correct CSS. Done when a dated cycle-log entry records the decision, and control K1 (`outline: 2px solid rgb(82 181 0);` on `:focus-visible`, `user-view.html:29`) passes `node --test tests/web/styles.test.js`.
- [X] T070 Finding 6 (MED): give `dimsText` in `tests/web/styles.test.js:268-272` a ceiling comment like the other predicates, and add the table rows `{ opacity: '50% !important' }` (dims) and `{ filter: 'none!important' }` (does not dim). Done when `node --test tests/web/styles.test.js` is green and mutant M4 fails it.
- [X] T071 Finding 7 (LOW): link T063 to `A18`. Add the id to that task, or record the mapping in a new cycle-log entry. Done when `grep -n 'T063' specs/007-user-view-polish/tasks.md` shows `[A18]`, or the cycle log names the mapping.

---

## Phase 12: TDD remediation

From the fifth [`tdd/verification.md`](./tdd/verification.md) (verdict **FAIL**, audited at
`78b0f4b`). **The feature is not done until T072–T074 are cleared.** Take the decision in T075 before
you start T073 and T074. Each mutant (W*, V*) is listed with its exact text in the report's "Mutation
results" and in the runner the audit used. Apply each mutant to a file copy, then restore it and
check the restore. Never use `git checkout`. Run the whole page-side suite for each one, because a
markup mutant can be caught by any page test.

- [X] T072 Finding 1 (HIGH): the maintainer reviews `STYLESHEET` in `tests/web/styles.test.js:53-82` against `src/Jellyfin.Plugin.NewReleases/Web/user-view.html` and records the review in a dated cycle-log entry that names the reviewed commit. Mark the 21 entries that predate 007 in the list's comment. Done when the entry exists and `node --test tests/web/styles.test.js` is green. [U67]
- [X] T073 [US4] Finding 2 (HIGH): make U67 in `tests/web/styles.test.js:13, 39-50` assert that the page has exactly one `<style>` element, that its only at-rule is the pinned `@media`, and that `stylesheet()` accounts for every non-whitespace character of the block. Observe each red first. Done when mutants W1, V1, V2 and V3 each fail `node --test "tests/web/*.test.js"`. [U67]
- [X] T074 [US1] Finding 3 (HIGH): close the markup. For each element template that `row()` and `cover()` write, and for the static Artist field and `#nr-panel`, assert the exact set of attribute names (lower case, no duplicates) and the exact class list, through `tests/web/load-page.js` and the static-attribute text read the profile permits. Assert that no `style` attribute is written or declared. Make `imgAttribute` in `tests/web/cover-markup.js` match names without case. Done when mutants W2–W13 and V4 each fail `node --test "tests/web/*.test.js"`. [A5] [A10] [A11] [A12] [A13] [A14] [A17] [A18] [U50] [U51] [U61] [U66]
- [X] T075 Finding 4 (MED): maintainer decision. Either extend the closed world to the markup (T073 and T074 as written), or record these forms as outside the hermetic suite, send them to `quickstart.md` §2, and add a rubric override row for them. Done when a dated cycle-log entry records the decision.
- [X] T076 Finding 5 (LOW): make `rules()`, `declarations()` and `narrowScreen()` in `tests/web/styles.test.js:17, 31, 168` read their rules through `stylesheet()`, keeping the `@media` condition. Name `calc()` and `var()` in `dimsText`'s ceiling comment at `:331-334`. Do not change behaviour. Done when `node --test "tests/web/*.test.js"` stays green and every earlier mutant (R*, N*, P*, Q*, S*, X*, C*, M*, E2) still fails it.
- [X] T077 Finding 6 (LOW): name `78b0f4b` for cycle 66 in a new cycle-log entry. Do not edit past entries. Done when `grep -n 78b0f4b specs/007-user-view-polish/tdd/cycle-log.md` finds it.
- [X] T078 Finding 7 (LOW): the maintainer confirms or rejects the loop's reading of T069's K1 check (K1 fails U67 only) in a dated cycle-log entry. Done when the entry exists.

---

## Phase 13: TDD remediation

From the sixth [`tdd/verification.md`](./tdd/verification.md) (verdict **FAIL**, audited at
`dbab71c`). **The feature is not done until T079–T081 are cleared.** Each mutant (Y*, Z*, G*) is listed
in the report's "Mutation results" and in the runner the audit used. Apply each mutant to a file copy,
then restore it and check the restore. Never use `git checkout`. Run the whole page-side suite for
each one.

- [X] T079 Finding 1 (HIGH): the maintainer reviews `STATIC` and `WRITTEN` in `tests/web/markup.test.js:57, 92` against `src/Jellyfin.Plugin.NewReleases/Web/user-view.html`, and the review goes into a dated cycle-log entry that names the reviewed commit. Done when the entry exists. [U68]
- [X] T080 [US2] Finding 2 (HIGH): replace the pooled `WRITTEN` set in `tests/web/markup.test.js:92-144` with the ordered list of shapes for each template that `row()`, `cover()`, `render()` and `loadArtists()` write. Render every `panel.innerHTML` branch, the error branch included. Observe each red first. Done when mutants Y1, Y2, Z1, Z2, G1 and G2 each fail `node --test "tests/web/*.test.js"`. [U68] [U59] [U66] [A16] [A18]
- [X] T081 [US1] Finding 3 (HIGH): after the first load, a tab switch, a filter change, Clear, and a render, assert that each declared element's attributes and `hidden` equal a reviewed state. Rename the test in `tests/web/markup.test.js:146-153` to name that scope. Done when mutants Y3, Y6, Y7 and Y10 each fail `node --test "tests/web/*.test.js"`. [U68] [A3] [A5] [A17] [A18]
- [X] T082 Finding 4 (MED): make `signatures()` in `tests/web/markup.test.js:20-41` give every opening tag a shape, or fail on one it cannot read. Add table rows for unquoted and single-quoted values, a comment, `<template>`, a self-closing tag and a duplicate `class`. Done when mutant G3 (unquoted `class=nr-badge style=opacity:.3` on the type badge) fails a U68 test.
- [X] T083 Finding 5 (MED): maintainer decision on Y4, Y8 and Y9, the survivors in behaviours that predate 007. Open a separate spec for them, or record why not. Done when a dated cycle-log entry records the decision.
- [X] T084 Finding 6 (MED): move the `imgAttribute` table out of `tests/web/markup.test.js:43-52`, to the file of its users or to a file for `cover-markup.js`. Done when `markup.test.js` holds only U68's tests and helpers and `node --test "tests/web/*.test.js"` is green.
- [X] T085 Finding 7 (LOW): keep one copy of `settled`, of the two-route `ApiClient` stub, and of the render-to-panel helper, in the page-side helpers. Done when `grep -n "const settled" tests/web/*.test.js` finds none and `node --test "tests/web/*.test.js"` is green.

---

## Phase 14: TDD remediation

From the seventh [`tdd/verification.md`](./tdd/verification.md) (verdict **FAIL**, audited at
`3f9061f`). **The feature is not done until T086–T090 are cleared.** Each mutant (H*, J*) is listed in
the report's "Mutation results" and in the runner the audit used. Apply each mutant to a file copy,
then restore it and check the restore. Never use `git checkout`. Run the whole page-side suite for
each one.

- [X] T086 Finding 1 (HIGH): the maintainer reviews `TEMPLATES` and `RUNTIME` in `tests/web/markup.test.js:129-145, 176-182` against `src/Jellyfin.Plugin.NewReleases/Web/user-view.html`, and the review goes into a dated cycle-log entry that names the reviewed commit. Done when the entry exists. [U68]
- [X] T087 [US1] Finding 2 (HIGH): add A15 rows for a precomposed non-ASCII name (`Björk`, `Sigur Rós`), a test with 1,000 artists whose last one is suggested and applies, and run A15 under `LANG=tr_TR.UTF-8` with a name that holds `I`. Add the Turkish run to the profile's locale check. Observe each red against its mutant. Done when mutants H1, H2, H3 and H5 each fail the page-side suite (H5 under `LANG=tr_TR.UTF-8 node --test "tests/web/*.test.js"`). [A1] [A2] [A15] Scope: accents and big libraries only; the Turkish-locale run is dropped (maintainer decision 2026-10-04, Q2).
- [X] T088 [US1] Finding 3 (HIGH): make A5 in `tests/web/artist-filter.test.js:136-138` assert that the Artist input carries exactly the attributes and values of `contracts/user-view.md:12`. Done when mutant H4 (`autocomplete="on"`) fails `node --test "tests/web/*.test.js"`. [A5]
- [X] T089 [US4] Finding 4 (HIGH): extend U68's runtime test in `tests/web/markup.test.js:184-206` with an action click (through `actionRow`), an Artist `input`, an image `error` and a failed load. Done when mutant J3 (`style` written on the panel on an action click) fails `node --test "tests/web/*.test.js"`. [U68] Dropped: maintainer decision 2026-10-04 (cycle log, grilling session Q3).
- [X] T090 Finding 5 (HIGH): split the `closest` test in `tests/web/fake-dom.test.js:70-77` into one table row per case, including an element that lacks the attribute, several classes, a digit in a tag, `*` and the empty selector. Give U72 in `tests/web/requests.test.js:97-107` a second action and a click outside any button. Correct the comment at `tests/web/render.test.js:119-121`. Done when mutants J5 and J4 each fail `node --test "tests/web/*.test.js"`. [U69] [U72]
- [X] T091 Finding 6 (MED): maintainer decision on the view's untested pre-007 behaviour (H6, H7, H9, H10, J1 and the probe's other survivors): a separate spec, or more characterization in 007. Done when a dated cycle-log entry records the decision. Decided 2026-10-04: fixed in 007 as the characterization tasks T095–T097 (cycle log, grilling session).
- [ ] T092 Finding 7 (MED): name in `tests/web/markup.test.js:131-145` the fixture facts the repeated shapes depend on (two sources for 101, two missing tracks for 102, two artists), or derive the counts from the fixture. Done when `node --test "tests/web/*.test.js"` is green and each repeated shape has its reason beside it.
- [ ] T093 Finding 8 (MED): move U71 out of `tests/web/requests.test.js:61-94` to a file whose subject is the view's listeners, and name it in the profile. Done when `node --test "tests/web/*.test.js"` is green and Y4 still fails it.
- [ ] T094 Finding 9 (LOW): share one `once`/`signal` rule between `keepsListening` and `capturesEveryError` (safe on `null`); rename `load-page.js`'s `rendered` and `markup.test.js`'s `DETAILS` to names that say what they hold; correct the U68 row of `tdd/test-list.md` and the header of `tests/web/cover-markup.js`. Done when `node --test "tests/web/*.test.js"` is green and E2 and Y4 still fail it.
- [X] T095 [US2] T091 group A (characterization, `BASELINE`): the list shows the right releases. The Archive tab asks for archived releases and the List tab does not; Type, State, From and To each send their own value under the name the server reads; the Type filter offers Album, EP, Single, Compilation, Live, Remix, Soundtrack; an action in the Archive tab reloads the Archive and stays on it; a release with three sources shows three links. Done when each test fails against a hand-made break of its behaviour and `node --test "tests/web/*.test.js"` is green. [U73] [U74] [U75] [U76] [U77]
- [X] T096 [US2] T091 group B (characterization, `BASELINE`): the page shows the right words. The failed-load sentence; the three empty states (the Archive one also before the first refresh); the button texts and their screen-reader labels; the state and archive badges; "1 missing track" and "2 missing tracks"; the source link labels; the status line after each action. Done as T095. [U78] [U79] [U80] [U81] [U82] [U83] [U84]
- [ ] T097 [US4] T091 group C (characterization, `BASELINE`): a screen reader and keyboard get it right. The List tab is selected on open, switching swaps the selection and the panel's label; status announcements are on; each label belongs to its own field, and From and To are date fields; source links open with `rel="noopener"`. Done as T095. [U85] [U86] [U87] [U88]
