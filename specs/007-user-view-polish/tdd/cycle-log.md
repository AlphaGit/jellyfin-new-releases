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
