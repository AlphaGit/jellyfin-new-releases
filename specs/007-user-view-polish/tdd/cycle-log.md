# Cycle Log: Polish the New Releases view

Append only. Newest last. Every entry's `red` block is the evidence that the test
existed and failed before the implementation.

## Baseline

- suite (dotnet): `dotnet test --configuration Release` -> 301 passed, 2 failed, 303 total
- suite (node): `node --test "tests/web/*.test.js"` -> 64 passed, 0 failed
- commit: `a3b3579`
- recorded: cycle 0, before any change

## Notes and deviations

- Red at baseline, predating this feature:
  `Packaging.RepositoryManifestTests.Manifest_EverySourceUrlSharesOneSiteRoot_AndNamesItsOwnVersion`
  and `Packaging.RepositoryManifestTests.Manifest_EveryVersionCarriesItsDownloadChecksumTimestampAndJellyfin12`
  -> "repo/manifest.json lists 2 version(s), expected 1". The 0.1.1 release published a second
  version and `PublishedVersionsToday` was not raised. Not touched by this feature; the loop must
  not start until it is fixed.

## Baseline, re-recorded before cycle 1

- suite (dotnet): `dotnet test --configuration Release` -> 310 passed, 0 failed, 310 total
- suite (node): `node --test "tests/web/*.test.js"` -> 79 passed, 0 failed
- commit: `e242057`
- recorded: 2026-10-02, before any change. The red baseline above was fixed outside this feature
  by `fb956af` (T056), so the loop may start.

## Setup: T001–T003, the recorded artist lookup

- `tests/fixtures/musicbrainz/artist_lookup.json` recorded 2026-10-02 from
  `GET https://musicbrainz.org/ws/2/artist/9dae8dff-0c54-4019-a6f2-667890ad9878?fmt=json`, scrubbed
  (`isnis`, `ipis` dropped). `artist_lookup_empty.json` is the same body with `disambiguation: ""`.
  Both recorded in `tests/fixtures/README.md`.
- No behaviour, no test change. Suite unchanged.
- commit: `e60275c`
- notes: T001–T003 carry no behaviour id, so this loop leaves them unticked in `tasks.md`.

## Cycle 1: A6 a refresh over two tagged homonyms gives each its text — BLOCKED

- test: `Acceptance/ConfigureAndRunTests.cs::A6_AfterARefreshOverTwoTaggedHomonyms_ArtistsCarriesEachTextAndTheUniqueNameNone`
  (new, not committed). Library: "Desire" tagged `9dae8dff-…`, "Desire" tagged `c931de1d-…`,
  "Chromatics" tagged; Deezer off; MusicBrainz catalogue and artist lookups stubbed. Needed
  `ArtistDto.Disambiguation` as a minimal declaration to compile.
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~ConfigureAndRunTests.A6_AfterARefreshOverTwoTaggedHomonyms" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: Collections differ`
  Expected `[("Chromatics", null), ("Desire", "Gothic Metal, Finland"), ("Desire", "with Johnny Jewel on …")]`
  Actual `[("Chromatics", null), ("Desire", null)]` (1 failed)
- finding: the red is not only the missing behaviour. **The library holds one "Desire", not two.**
  `LibraryScanner.Scan` groups albums by album-artist name with `StringComparer.OrdinalIgnoreCase`
  before it reads any MBID, and takes the first `MusicArtist` of that name. Jellyfin also keys
  artists by name, so two bands named "Desire" share one `jellyfinId`. Exact homonyms therefore
  never reach `library_artist` as two rows. Only names that `TitleNormalizer.NormalizeName` folds
  together but the scanner keeps apart (accents, punctuation: "Björk" / "Bjork") can collide.
  US1-AS6, FR-005a and FR-005b assume two rows for two same-name artists.
- decision: the maintainer chose to stop the loop and fix the specification first. A6 is
  `BLOCKED`. The test and its minimal declaration were removed from the tree, because a
  red test cannot be committed and the tree must build. Suite back at
  310 passed, 0 failed; node 79 passed.
- commit: none

## Re-plan, 2026-10-03

The 2026-10-03 clarification removed the homonym disambiguation. `tdd/test-list.md` was refreshed
at `d88c618`: A6, U1–U30, U40, U41 and U43 are `DROPPED`, and `tasks.md` was regenerated with new
task ids (`5a26d24`). Baseline at `5a26d24`: dotnet 310 passed, node 79 passed.

Commit convention for the page cycles below: the acceptance behaviours A1–A5 have no unit layer
beneath them that is not the page itself, so each is driven directly, one test and one smallest
change per cycle, and committed at green.

## Cycle 2: A1 the suggestion list offers every artist name

- test: `tests/web/artist-filter.test.js::A1: with artists ASP, Aspen and Wasp the suggestion list offers all three names` (new file)
- red: `node --test tests/web/artist-filter.test.js` (the profile has no node single-test command; the file holds this one test)
  -> `Expected values to be strictly deep-equal: + [] - ['ASP', 'Aspen', 'Wasp']` (1 failed)
- green: `user-view.html` declares `<datalist id="nr-f-artist-list">` beside the existing
  `<select>`, and `loadArtists` writes one `<option value="{name}">` per artist into it. Names are
  not escaped yet: U44 drives that. Suite -> node 80 passed, dotnet 310 passed
- refactor: none needed

## Cycle 3: A2 the exact name applies that artist's filter

- test: `tests/web/artist-filter.test.js::A2: when the field text becomes the name ASP the page requests releases with ASP's artistId` (new), with the `type` helper that sets the field and fires its `input` listeners
- red: `node --test tests/web/artist-filter.test.js`
  -> `Expected values to be strictly equal: + 'GET Plugins/NewReleases/Releases' - 'GET Plugins/NewReleases/Releases?artistId=a5a5a5a5a5a5a5a5a5a5a5a5a5a5a5a5'` (1 failed)
- green: `<select id="nr-f-artist">` replaced by `<input id="nr-f-artist" type="text" list="nr-f-artist-list" autocomplete="off" placeholder="All artists">`.
  `loadArtists` builds a name → `jellyfinId` map instead of appending `<option>` children; `query()`
  sends the mapped id; the field reloads on `input`. Suite -> node 81 passed, dotnet 310 passed
- refactor: `query()` read the map twice; the lookup moved into one local. Suite re-run green (node 81, dotnet 310)
- commit of cycle 2: `cd52744`

## Cycle 4: A3 emptying the field or pressing Clear removes the artist filter

- test: `tests/web/artist-filter.test.js::A3: after ASP is applied, emptying the field requests releases with no artistId`
  and `…::A3: after ASP is applied, pressing Clear requests releases with no artistId` (new; the
  behaviour names two ways, so it has one test for each)
- red: **both passed on the first run.** Cycle 3's map lookup already yields no id for `""`, and
  the existing Clear handler already empties every filter. Deliberate mutants per the playbook,
  each applied to a file copy of `user-view.html` and restored with `cp`, verified with `cmp -s`:
  - M1, `query()` keeps the last applied id when the text maps to nothing -> both tests fail,
    `+ 'GET Plugins/NewReleases/Releases?artistId=a5a5a5a5a5a5a5a5a5a5a5a5a5a5a5a5'` (2 failed)
  - M2, Clear skips the Artist field -> the Clear test fails with the same line (1 failed)
- green: no production change. Suite -> node 83 passed, dotnet 310 passed
- refactor: none needed
- notes: test-after in the strict sense, for the same reason as `005` cycles 2 and 3; the mutants
  stand in for the red
- commit of cycle 3: `3791558`

## Cycle 5: A4 text that equals no name applies no filter

- test: `tests/web/artist-filter.test.js::A4: text that equals no artist name, even one differing only in case, requests releases with no artistId` (new).
  `asp` is chosen because the browser's suggestion list matches it to "ASP" (case-insensitive),
  while FR-002 applies only an exact name
- red: **passed on the first run**: cycle 3's map lookup is exact. Deliberate mutant M3, a
  case-insensitive lookup in `query()`, on a file copy -> `+ 'GET Plugins/NewReleases/Releases?artistId=a5a5a5a5a5a5a5a5a5a5a5a5a5a5a5a5'` (1 failed).
  Restored with `cp`, verified with `cmp -s`
- green: no production change. Suite -> node 84 passed, dotnet 310 passed
- refactor: none needed
- commit of cycle 4: `0730cb8`

## Cycle 6: A5 the Artist control is the native, labelled suggestion input

- test: `tests/web/artist-filter.test.js::A5: the Artist control is a text input labelled "Artist" and bound to the suggestion list`
  (reads the page source: the fake DOM does not model markup attributes) and
  `…::A5: the page adds no key handling to the Artist control, so the browser keeps its own` (new)
- red: **both passed on the first run**: cycle 3 already wrote the markup. Deliberate mutants on a
  file copy, restored with `cp` and verified with `cmp -s`:
  - M4, the `list` attribute removed -> the markup test fails (1 failed)
  - M5, a `keydown` listener added to the field -> `+ 'keydown'` (1 failed)
- green: no production change. Suite -> node 86 passed, dotnet 310 passed
- refactor: none needed
- notes: keyboard operation itself (FR-004) is the browser's; the hermetic suite can only pin that
  the page does not take it over. The real-browser pass, `quickstart.md` §2.1, checks it
- commit of cycle 5: `369f8e6`

## Cycle 7: U47 the view exposes exactly its helpers plus artistIndex

- order: taken before U42 and the other US1 units, and before A7. Exposing `artistIndex` for U42
  would turn this exact-set test red as a side effect, so the set change is its own behaviour first.
  The US1 units are finished before US2's outer loop opens, so no story's acceptance test is left
  open across another story's commits
- test: `tests/web/exposure.test.js::user-view.html exposes exactly its testable helpers` (changed
  baseline: `artistIndex` added to the expected set, as U47 states)
- red: `node --test tests/web/exposure.test.js` -> `Expected values to be strictly deep-equal` with `-   'artistIndex',` (1 failed)
- green: `user-view.html` declares `function artistIndex() { return {}; }` and exposes it. The body
  is a fake on purpose: U42 drives the mapping. Suite -> node 86 passed, dotnet 310 passed
- refactor: none needed
- commit of cycle 6: `0ed6363`

## Cycle 8: U42 artistIndex maps each name to its jellyfinId

- test: `tests/web/artist-filter.test.js::U42: artistIndex maps each name to its jellyfinId` (new)
- red: `node --test tests/web/artist-filter.test.js` -> `+ {} - { ASP: 'a5a5a5a5a5a5a5a5a5a5a5a5a5a5a5a5', Aspen: …` (1 failed)
- green: `artistIndex(artists)` builds the name → `jellyfinId` object. Suite -> node 87 passed, dotnet 310 passed
- refactor: `loadArtists` built the same map inline; it now assigns `artistIndex(data.items)`, so
  the tested helper is the one the filter uses. Suite re-run green (node 87, dotnet 310)
- commit of cycle 7: `5dcac35`

## Cycle 9: U44 each option value is written escaped

- test: `tests/web/artist-filter.test.js::U44: a name with markup characters is written into its option escaped` (new)
- red: `node --test tests/web/artist-filter.test.js` -> `+ '<option value="Guns "N" <Roses>">' - '<option value="Guns &quot;N&quot; &lt;Roses&gt;">'` (1 failed)
- green: the option value goes through the page's existing `esc`. Suite -> node 88 passed, dotnet 310 passed
- refactor: none needed
- notes: the browser decodes the entity in `value`, so the field text after a pick is the raw name
  and the exact lookup still finds it
- commit of cycle 8: `a0d0208`

## Cycle 10: U45 a failed artist list leaves a usable, unfiltered field

- test: `tests/web/artist-filter.test.js::U45: when the Artists request fails, typing in the field still requests releases with no artistId` (new); `loadView` now rejects the Artists request when handed an `Error`
- red: **passed on the first run**: the existing `.catch` in `loadArtists` leaves the empty map in
  place and the list still loads. Deliberate mutant M6 on a file copy: the `.catch` sets the map to
  `null` -> `TypeError` when the field is typed into (1 failed). Restored with `cp`, verified with `cmp -s`
- green: no production change. Suite -> node 89 passed, dotnet 310 passed
- refactor: none needed
- commit of cycle 9: `f146cb6`

## Test correction before cycle 11: the U45 test over-specified a reload

- what: cycle 10's U45 test asserted that typing "ASP" after a failed Artists request sends a new
  unfiltered request. `contracts/user-view.md` says the page reloads only when the applied artist
  **changes**; with no artists loaded, typing changes nothing (none to none), so U46 and U64 will
  forbid exactly that request. The test asserted more than U45 states.
- change: the test is renamed to the U45 line and asserts that typing does not throw and that every
  releases request is unfiltered. Taken as its own step, before U46's implementation, per the playbook.
- strength re-checked: mutant M6 (the `.catch` sets the map to `null`) still fails it with
  `TypeError` (1 failed); restored with `cp`, verified with `cmp -s`. Suite -> node 89 passed
- list: U64 appended, the none-to-none half of "when it changes, reload", which U46's line does not cover

## Cycle 11: U46 the same applied name typed again sends no request

- test: `tests/web/artist-filter.test.js::U46: typing the applied name again sends no second releases request` (new)
- red: `node --test tests/web/artist-filter.test.js` -> `+ [ 'GET Plugins/NewReleases/Releases?artistId=a5a5a5a5a5a5a5a5a5a5a5a5a5a5a5a5' ] - []` (1 failed)
- green: `query()` records the artist each request applied (`appliedArtistId`), and the `input`
  handler reloads only when the mapped id differs from it. Recording it in `query()` rather than in
  the handler keeps Clear correct: Clear reloads, the reload records "none", and the name typed
  again afterwards applies. Suite -> node 90 passed, dotnet 310 passed
- refactor: one comment on the side effect in `query()`. Suite re-run green (node 90, dotnet 310)
- correction commit before this cycle: `b2720ed`; commit of cycle 10: `977f1df`

## Cycle 12: U64 typing that leaves no artist applied sends no request

- test: `tests/web/artist-filter.test.js::U64: typing part of a name while no artist is applied sends no releases request` (new)
- red: **passed on the first run**: cycle 11's comparison already covers none to none. Deliberate
  mutant M7 on a file copy, the handler also reloads whenever nothing is applied ->
  `+   'GET Plugins/NewReleases/Releases'` (1 failed). Restored with `cp`, verified with `cmp -s`
- green: no production change. Suite -> node 91 passed, dotnet 310 passed
- refactor: none needed
- commit of cycle 11: `e57fa95`

## Cycle 13: U65 after Clear, the same name applies again

- test: `tests/web/artist-filter.test.js::U65: after Clear removes ASP, typing ASP again applies it again` (new; U65 appended to the list in this cycle)
- red: **passed on the first run**: cycle 11 records the applied artist in `query()`, which Clear's
  reload also runs. Deliberate mutant M8 on a file copy, the applied artist recorded in the `input`
  handler instead -> `+ 'GET Plugins/NewReleases/Releases'` (1 failed). Restored with `cp`, verified with `cmp -s`
- green: no production change. Suite -> node 92 passed, dotnet 310 passed
- refactor: none needed
- outer loop: US1 closes here. A1–A5 are green in the full suite with every US1 unit `DONE`
  (U42, U44–U47, U64, U65). Tasks T002–T004 and the gates T027–T031 ticked
- commit of cycle 12: `4dd7bb4`

## Cycle 14: A7 opens US2's outer loop — RED, held open

- test: `Acceptance/BrowseReleasesTests.cs::A7_AReleaseStoredAtBothSources_ListsItsDeezerCoverThenItsCoverArtArchiveCover` (new).
  Daft Punk tagged; MusicBrainz lists "Alive 2007" as release group `48117b90-…`, Deezer as album 3;
  the release is read from its **serialized** form under `JsonDefaults.CamelCaseOptions`, because
  `covers` is the wire field the page reads, so no `ReleaseDto.Covers` declaration is needed to compile
- first run: `Assert.Single() Failure: The collection was empty` — **not a valid red**: the test's
  MusicBrainz group carried the `Live` secondary type, which the default type set hides. Fixed the
  test (no secondary type), re-ran
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~BrowseReleasesTests.A7_AReleaseStoredAtBothSources" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: Collections differ` Expected `["https://api.deezer.com/album/3/image?size=medium", "https://coverartarchive.org/release-group/48117b90"···]` Actual `[]` (1 failed)
- state: `RED`. The test stays **uncommitted** while U31–U37 run, so every unit commit is green;
  each unit cycle's full suite reports this one test failing and nothing else
- commit of cycle 13: `670aadc`

## Cycle 15: U31 each listed source link carries its source_release_id

- test: `Storage/ReleaseRepositoryTests.cs::ListAsync_EachSourceLinkCarriesTheSourceReleaseIdStoredForIt` (new),
  with `SourceLink.SourceReleaseId` declared as `= ""` so the test compiles
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~ReleaseRepositoryTests.ListAsync_EachSourceLinkCarriesTheSourceReleaseIdStoredForIt" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: Collections differ` Expected `[("deezer", "dz-disc"), ("musicbrainz", "rg-disc")]` Actual `[("deezer", ""), ("musicbrainz", "")]` (1 failed)
- green: the list query's `sources` JSON also selects `source_release_id`. Suite -> dotnet 311
  passed, 1 failed (A7, held open), node 92 passed
- refactor: the stub default `= ""` removed from `SourceLink`; every row now carries the value.
  Suite re-run: same counts

## Cycle 16: U37 the list response carries `covers` under the endpoint's declared naming

- order: taken before U32–U36. Declaring `ReleaseDto.Covers` for U32 would turn this naming test
  red as a side effect, so the contract change is its own behaviour first (as U47 was for U42)
- test: `Api/ResponseNamingTests.cs::ListResponse_AsTheReleasesEndpointDeclaresIt_CarriesTheNamesTheListPageReads`
  (existing). Its contract input changed: every item in `tests/fixtures/pages/releases.json`,
  `releases-filtered.json` and `releases-stale.json` gained a `covers` array built from its own
  source ids, Deezer first (T007)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~ResponseNamingTests.ListResponse_AsTheReleasesEndpointDeclaresIt" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: Collections differ` Expected `["items[]"] = ["archived", "artistJellyfinId", "artistName", "comparedEdition", "covers", ···]` Actual `["items[]"] = ["archived", "artistJellyfinId", "artistName", "comparedEdition", "date", ···]` (1 failed)
- green: `ReleaseDto` gains `IReadOnlyList<string> Covers` as its last member; `ToDto` passes `[]`,
  a fake that U32–U35 replace. The test's `PopulatedListResponse` passes a one-URL `covers` to the
  new constructor parameter, the change needed to compile. Suite -> dotnet 311 passed, 1 failed (A7,
  held open), node 92 passed (the node suite reads the same fixtures and is unaffected)
- refactor: none needed
- commit of cycle 15: `7da7bce`

## Cycle 17: U32 a release at both sources lists Deezer's cover, then the Cover Art Archive's

- test: `Api/ReleasesControllerTests.cs::GetReleases_AReleaseAtBothSources_ListsTheDeezerCoverThenTheCoverArtArchiveCover` (new), with the `ListedWithSourcesAsync` helper
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~ReleasesControllerTests.GetReleases_AReleaseAtBothSources" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: Collections differ` Expected `["https://api.deezer.com/album/302127/image?size=med"···, "https://coverartarchive.org/release-group/rg-disc/"···]` Actual `[]` (1 failed)
- green: `ToDto` builds `Covers` with `CoversOf`: source links ordered Deezer first, each mapped to
  its URL format (research R6). IDs are not escaped yet: U35 drives that. Suite -> dotnet 313
  passed, 0 failed; node 92 passed. The held-open A7 now passes too; it is closed after U33–U36
- refactor: none needed
- commit of cycle 16: `6d6d8fa`

## Cycle 18: U33 a Deezer-only release lists exactly its Deezer cover

- test: `Api/ReleasesControllerTests.cs::GetReleases_ADeezerOnlyRelease_ListsExactlyItsDeezerCover` (new)
- red: **passed on the first run**: cycle 17's mapping already covers one source. Deliberate mutant
  M9 on a file copy, the URL-format branch inverted (`!=`) ->
  Expected `["https://api.deezer.com/album/302127/image?size=med"···]` Actual `["https://coverartarchive.org/release-group/302127/f"···]` (1 failed).
  Restored with `cp`, verified with `cmp -s`
- green: no production change. Suite -> dotnet 314 passed, 0 failed (A7 held open, passing)
- refactor: none needed
- commit of cycle 17: `bc52ad7`

## Cycle 19: U34 a MusicBrainz-only release lists exactly its Cover Art Archive cover

- test: `Api/ReleasesControllerTests.cs::GetReleases_AMusicBrainzOnlyRelease_ListsExactlyItsCoverArtArchiveCover` (new)
- red: **passed on the first run**, as U33. Deliberate mutant M10 on a file copy, `front-250` ->
  `front-500` -> Actual `["https://coverartarchive.org/release-group/48117b90"···]` against the
  exact expected URL (1 failed). Restored with `cp`, verified with `cmp -s`
- green: no production change. Suite -> dotnet 315 passed, 0 failed
- refactor: none needed
- commit of cycle 18: `40b34e5`
