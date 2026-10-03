---
feature: 007-user-view-polish
loop: outside-in
profile: .specify/memory/tdd-profile.md
spec_criteria: 13
planned_at: a3b3579
updated_at: d88c618
suite_baseline: green
---

# Test List: Polish the New Releases view

**Refreshed 2026-10-03 at `d88c618`.** The 2026-10-03 clarification removed the homonym
disambiguation (spec FR-005; research R3–R5): two library artists never share a name, because
the library scan and Jellyfin keep one artist per name (cycle log, cycle 1). A6, U1–U30, U40,
U41 and U43 are `DROPPED`. A1–A5, U42 and U44–U47 now speak of names, not labels. The red
baseline recorded at planning was fixed outside this feature by `fb956af`; the suite is green
(dotnet 310, node 79).

**Acceptance level.** The profile has no end-to-end runner. Server criteria run through
`AcceptanceRig`: the real controllers, repositories and refresh task over a temporary database
and stubbed HTTP. Page criteria run through `tests/web/load-page.js`: the real `user-view.html`
script in a `node:vm` sandbox with the string-capturing fake DOM. The committed fixtures in
`tests/fixtures/pages/` connect the two sides, as in `005`. Layout criteria (US3) and real lazy
loading cannot be observed by a fake DOM that does no layout. Their acceptance lines assert the
declarations that produce them, and the real-browser pass in `quickstart.md` §2 checks the pixels.

## Outer loop: acceptance behaviors

| id  | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| A1  | With artists "ASP", "Aspen" and "Wasp", the page's suggestion list offers all three names | US1-AS1, FR-001 | example | DONE | `tests/web/artist-filter.test.js::A1: with artists ASP, Aspen and Wasp the suggestion list offers all three names` |
| A2  | When the field text becomes the name "ASP", the page requests releases with ASP's `artistId` | US1-AS2, FR-002, FR-005 | example | DONE | `tests/web/artist-filter.test.js::A2: when the field text becomes the name ASP the page requests releases with ASP's artistId` |
| A3  | After an artist is applied, emptying the field or pressing Clear requests releases with no `artistId` | US1-AS3, FR-003 | example | DONE | `tests/web/artist-filter.test.js::A3: after ASP is applied, emptying the field … / pressing Clear requests releases with no artistId` |
| A4  | Text that equals no artist name requests releases with no `artistId` | US1-AS4, FR-002 | example | DONE | `tests/web/artist-filter.test.js::A4: text that equals no artist name, even one differing only in case, requests releases with no artistId` |
| A5  | The Artist control is a native text input bound to the suggestion list by `list`, labelled "Artist", with no page key handling | US1-AS5, FR-004 | example | DONE | `tests/web/artist-filter.test.js::A5: the Artist control is a text input labelled "Artist" … / the page adds no key handling to the Artist control` |
| A6  | After a refresh over two tagged artists named "Desire" and one "Chromatics", `GET Artists` returns both "Desire" with their texts and "Chromatics" with `null` | US1-AS6 (removed) | example | DROPPED: US1-AS6 and FR-005a/b were removed on 2026-10-03; two library artists never share a name (cycle log, cycle 1) | |
| A7  | After a refresh stores one release at both sources, `GET Releases` returns its Deezer cover URL, then its Cover Art Archive URL | US2-AS1, FR-006, FR-006a | example | DONE | `tests/Jellyfin.Plugin.NewReleases.Tests/Acceptance/BrowseReleasesTests.cs::A7_AReleaseStoredAtBothSources_ListsItsDeezerCoverThenItsCoverArtArchiveCover` |
| A8  | On a card for a release with both sources, an `error` on the Deezer image puts the Cover Art Archive URL in its `src` | US2-AS2, FR-006a | example | DONE | `tests/web/cover-fallback.test.js::A8: on a card for a release at both sources, an error on the Deezer image puts the Cover Art Archive URL in its src` |
| A9  | On a card whose every cover URL fails, the cover box remains and holds no `<img>` | US2-AS3, FR-007 | example | DONE | `tests/web/cover-fallback.test.js::A9: on a card whose every cover URL fails, the cover box remains and holds no image` |
| A10 | A rendered cover image has empty alt text | US2-AS4 | example | DONE | `tests/web/render.test.js::A10: a rendered cover image has empty alt text, so a screen reader skips it` |
| A11 | The stylesheet makes the List-tab buttons fill one shared column: `.nr-actions` stretches, and its buttons are `width: 100%` | US3-AS1, FR-009, SC-003 | example | DONE | `tests/web/styles.test.js::A11: the List-tab buttons fill one shared column: .nr-actions stretches its buttons, and each is full width` |
| A12 | An Archive-tab card renders "Restore" inside `.nr-actions`, under the same button rule as "Ignore" and "Have it" | US3-AS2, FR-009 | example | DONE | `tests/web/render.test.js::A12: an Archive-tab row writes Restore inside .nr-actions, under the same rule as Ignore and Have it` |
| A13 | The declared source-link colour has a WCAG contrast of at least 4.5:1 against the card background `#1c1c1c` | US4-AS1, FR-010, SC-004 | example | PENDING | |
| A14 | The source link keeps its underline, its `:visited` state has the same colour, and focus shows the outline | US4-AS2, FR-010 | example | PENDING | |

## Inner loop: unit behaviors

### `src/Jellyfin.Plugin.NewReleases/Storage/Migrations/002_artist_disambiguation.sql` (with `PluginDatabase`)

| id  | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U1  | A database at schema `001` gains `disambiguation` and `disambiguation_mbid`, `NULL` on every existing row | FR-005b, constitution IV | example | DROPPED: migration 002 removed with FR-005b (2026-10-03) | |
| U2  | A fresh first open reaches schema version 2 with two `schema_version` rows. This changes the baseline asserted by `DatabaseTests::OpenAsync_FirstOpenAppliesTheInitialMigration`, `…SecondOpenFromAFreshInstanceAppliesNothing` and `…TwoConcurrentFirstOpensRunTheMigrationOnce` | FR-005b | example | DROPPED: migration 002 removed with FR-005b (2026-10-03) | |

### `src/Jellyfin.Plugin.NewReleases/Storage/ArtistRepository.cs`

| id  | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U3  | A stored disambiguation text and its MBID read back unchanged | FR-005b | example | DROPPED: disambiguation storage and candidates removed with FR-005b (2026-10-03) | |
| U4  | A later library sync (`UpsertAsync`) of the same artist keeps the stored text and MBID | FR-005b | example | DROPPED: disambiguation storage and candidates removed with FR-005b (2026-10-03) | |
| U5  | A tagged artist whose name collides, with no text fetched yet, is a candidate under its tag MBID | FR-005b | example | DROPPED: disambiguation storage and candidates removed with FR-005b (2026-10-03) | |
| U6  | An untagged colliding artist with a `Matched` MusicBrainz source ID is a candidate under that ID | FR-005b | example | DROPPED: disambiguation storage and candidates removed with FR-005b (2026-10-03) | |
| U7  | An artist whose normalized name occurs once is not a candidate | FR-005b | example | DROPPED: disambiguation storage and candidates removed with FR-005b (2026-10-03) | |
| U8  | Two artists whose normalized name occurs exactly twice are both candidates (the other side of U7) | FR-005b | example | DROPPED: disambiguation storage and candidates removed with FR-005b (2026-10-03) | |
| U9  | Names that differ only in case or accents ("Björk", "bjork") count as a collision | FR-005a, FR-005b | example | DROPPED: disambiguation storage and candidates removed with FR-005b (2026-10-03) | |
| U10 | A colliding artist with no effective MBID is not a candidate | FR-005b | example | DROPPED: disambiguation storage and candidates removed with FR-005b (2026-10-03) | |
| U11 | A colliding artist already fetched for its current MBID is not a candidate | FR-005b | example | DROPPED: disambiguation storage and candidates removed with FR-005b (2026-10-03) | |
| U12 | A colliding artist whose effective MBID differs from the fetched one is a candidate again | FR-005b | example | DROPPED: disambiguation storage and candidates removed with FR-005b (2026-10-03) | |

### `src/Jellyfin.Plugin.NewReleases/Sources/MusicBrainzSource.cs`

| id  | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U13 | The lookup requests `artist/{mbid}?fmt=json` with the MBID URL-escaped | FR-005b, contracts/http-api.md | example | DROPPED: MusicBrainz artist lookup removed with FR-005b (2026-10-03) | |
| U14 | The lookup returns the `disambiguation` of `artist_lookup.json` | FR-005b | example | DROPPED: MusicBrainz artist lookup removed with FR-005b (2026-10-03) | |
| U15 | The lookup returns `""` for `artist_lookup_empty.json` | FR-005b | example | DROPPED: MusicBrainz artist lookup removed with FR-005b (2026-10-03) | |
| U16 | With the MusicBrainz budget exhausted, the lookup throws `DailyBudgetExhaustedException` and sends nothing | FR-005b, constitution V | example | DROPPED: MusicBrainz artist lookup removed with FR-005b (2026-10-03) | |

### `src/Jellyfin.Plugin.NewReleases/ScheduledTasks/RefreshNewReleasesTask.cs`

| id  | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U17 | A run over two homonyms and one unique artist sends exactly two lookups | FR-005b | example | DROPPED: refresh disambiguation step removed with FR-005b (2026-10-03) | |
| U18 | Each looked-up text is stored with the MBID it was fetched for | FR-005b | example | DROPPED: refresh disambiguation step removed with FR-005b (2026-10-03) | |
| U19 | A second run with an unchanged library sends no lookup | FR-005b | example | DROPPED: refresh disambiguation step removed with FR-005b (2026-10-03) | |
| U20 | An artist first matched by search in this run is looked up in the same run | FR-005b | example | DROPPED: refresh disambiguation step removed with FR-005b (2026-10-03) | |
| U21 | MusicBrainz disabled in configuration: the run sends no lookup | FR-005b | example | DROPPED: refresh disambiguation step removed with FR-005b (2026-10-03) | |
| U22 | MusicBrainz cooling down: the run sends no lookup | FR-005b, constitution V | example | DROPPED: refresh disambiguation step removed with FR-005b (2026-10-03) | |
| U23 | A budget exhausted mid-step stops the remaining lookups, and the run ends `Completed` | FR-005b | example | DROPPED: refresh disambiguation step removed with FR-005b (2026-10-03) | |
| U24 | A lookup that fails with another exception adds one error, and the next candidate is still looked up | FR-005b | example | DROPPED: refresh disambiguation step removed with FR-005b (2026-10-03) | |

### `src/Jellyfin.Plugin.NewReleases/Api/ReleasesController.cs`: `GET Artists`

| id  | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U25 | Two artists with the same normalized name and stored texts each get their own text | FR-005a | example | DROPPED: `ArtistDto.disambiguation` removed with FR-005a (2026-10-03) | |
| U26 | A unique name gets `null`, even with a stale stored text | FR-005a | example | DROPPED: `ArtistDto.disambiguation` removed with FR-005a (2026-10-03) | |
| U27 | A colliding artist with a stored `""` gets `null` | FR-005a | example | DROPPED: `ArtistDto.disambiguation` removed with FR-005a (2026-10-03) | |
| U28 | A caller who sees one of two homonyms gets that artist's text | FR-005a | example | DROPPED: `ArtistDto.disambiguation` removed with FR-005a (2026-10-03) | |
| U29 | A caller who sees one of two homonyms does not get the hidden one in `items` | FR-005a | example | DROPPED: `ArtistDto.disambiguation` removed with FR-005a (2026-10-03) | |
| U30 | `ArtistsResponse`, serialized through the endpoint's declaration, carries the names in `tests/fixtures/pages/artists.json`, `disambiguation` included | contracts/http-api.md | contract | DROPPED: `ArtistDto.disambiguation` removed with FR-005a (2026-10-03) | |

### `src/Jellyfin.Plugin.NewReleases/Storage/ReleaseRepository.cs`

| id  | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U31 | Each listed source link carries the `source_release_id` stored for it | FR-006 | example | DONE | `tests/Jellyfin.Plugin.NewReleases.Tests/Storage/ReleaseRepositoryTests.cs::ListAsync_EachSourceLinkCarriesTheSourceReleaseIdStoredForIt` |

### `src/Jellyfin.Plugin.NewReleases/Api/ReleasesController.cs`: `GET Releases`

| id  | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U32 | A release at both sources gets the Deezer URL first, then the Cover Art Archive URL | FR-006, FR-006a | example | DONE | `tests/Jellyfin.Plugin.NewReleases.Tests/Api/ReleasesControllerTests.cs::GetReleases_AReleaseAtBothSources_ListsTheDeezerCoverThenTheCoverArtArchiveCover` |
| U33 | A Deezer-only release gets exactly `https://api.deezer.com/album/{id}/image?size=medium` | FR-006, FR-006a | example | DONE | `tests/Jellyfin.Plugin.NewReleases.Tests/Api/ReleasesControllerTests.cs::GetReleases_ADeezerOnlyRelease_ListsExactlyItsDeezerCover` |
| U34 | A MusicBrainz-only release gets exactly `https://coverartarchive.org/release-group/{id}/front-250` | FR-006, FR-006a | example | DONE | `tests/Jellyfin.Plugin.NewReleases.Tests/Api/ReleasesControllerTests.cs::GetReleases_AMusicBrainzOnlyRelease_ListsExactlyItsCoverArtArchiveCover` |
| U35 | A source ID containing URL-reserved characters is escaped in its cover URL | FR-006 | example | DONE | `tests/Jellyfin.Plugin.NewReleases.Tests/Api/ReleasesControllerTests.cs::GetReleases_ASourceIdWithReservedCharacters_IsEscapedInItsCoverUrl (deezer, musicbrainz)` |
| U36 | `sources` keeps its existing order when `covers` is added | FR-011 | example | DONE | `tests/Jellyfin.Plugin.NewReleases.Tests/Api/ReleasesControllerTests.cs::GetReleases_AddingCovers_KeepsTheSourcesInTheirExistingOrder` |
| U37 | `ListResponse`, serialized through the endpoint's declaration, carries the names in `tests/fixtures/pages/releases.json`, `covers` included | contracts/http-api.md | contract | DONE | `tests/Jellyfin.Plugin.NewReleases.Tests/Api/ResponseNamingTests.cs::ListResponse_AsTheReleasesEndpointDeclaresIt_CarriesTheNamesTheListPageReads` |

### `tests/web/fake-dom.js` (test infrastructure the US2 page tests need)

| id  | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U38 | `addEventListener(type, handler, options)` keeps `options` readable by a test | invariant: A8 and U53 need the capture flag | example | DONE | `tests/web/fake-dom.test.js::addEventListener keeps the options it was given readable` |
| U39 | `remove()` detaches an element from its owner | invariant: A9 and U52 need removal | example | DONE | `tests/web/fake-dom.test.js::remove detaches an element from the element it was appended to` |

### `src/Jellyfin.Plugin.NewReleases/Web/user-view.html`: Artist filter

| id  | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U40 | `artistLabel` of an artist with a disambiguation is `name — disambiguation` | FR-005a, FR-005c | example | DROPPED: labels with a disambiguation removed with FR-005a (2026-10-03) | |
| U41 | `artistLabel` of an artist with `disambiguation: null` is the name alone | FR-005a | example | DROPPED: `artistLabel` removed: the label is the name (2026-10-03) | |
| U42 | `artistIndex` maps each name to its `jellyfinId` | FR-002 | example | DONE | `tests/web/artist-filter.test.js::U42: artistIndex maps each name to its jellyfinId` |
| U43 | `artistIndex` keeps the first artist when two share a label | FR-002 | example | DROPPED: two library artists never share a name, so there is no duplicate to resolve (2026-10-03) | |
| U44 | Loading artists writes one escaped `<option value>` per name into `#nr-f-artist-list` | FR-001, FR-005 | example | DONE | `tests/web/artist-filter.test.js::U44: a name with markup characters is written into its option escaped` |
| U45 | When the Artists request fails, the field stays usable, and releases are requested with no `artistId` | spec edge case "artist list fails to load" | example | DONE | `tests/web/artist-filter.test.js::U45: when the Artists request fails, the field stays usable and releases are requested with no artistId` |
| U46 | Typing the same applied name again sends no second releases request | contracts/user-view.md ("when it changes, reload") | example | DONE | `tests/web/artist-filter.test.js::U46: typing the applied name again sends no second releases request` |
| U47 | `NewReleasesInternals` exposes exactly the existing members plus `artistIndex` (changes the baseline of `exposure.test.js`) | contracts/user-view.md | example | DONE | `tests/web/exposure.test.js::user-view.html exposes exactly its testable helpers` |
| U64 | Typing text that leaves the applied artist unchanged (none to none, as while typing a name) sends no releases request | contracts/user-view.md ("when it changes, reload"); discovered in cycle 11 | example | DONE | `tests/web/artist-filter.test.js::U64: typing part of a name while no artist is applied sends no releases request` |
| U65 | After Clear removes an applied artist, typing that name again applies it again | FR-002, FR-003; discovered in cycle 11 (where the applied artist is recorded decides this) | example | DONE | `tests/web/artist-filter.test.js::U65: after Clear removes ASP, typing ASP again applies it again` |

### `src/Jellyfin.Plugin.NewReleases/Web/user-view.html`: release card cover

| id  | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U48 | A row's image `src` is `covers[0]` | FR-007 | example | DONE | `tests/web/render.test.js::U48: a row's cover image src is its first cover URL` |
| U49 | A row's `data-fallback` lists `covers[1..]` in order | FR-006a | example | DONE | `tests/web/render.test.js::U49: a row's cover image lists the remaining cover URLs, in order, as its fallbacks` |
| U50 | A row's image declares `loading="lazy"` | FR-007a | example | DONE | `tests/web/render.test.js::U50: a row's cover image loads lazily` |
| U51 | A row's image declares `referrerpolicy="no-referrer"` | FR-008, constitution V | example | DONE | `tests/web/render.test.js::U51: a row's cover image sends no referrer` |
| U52 | A row's image declares `width="64" height="64"` | FR-007 | example | DONE | `tests/web/render.test.js::U52: a row's cover image declares a 64 by 64 box` |
| U53 | A row with `covers: []` writes the cover box with no `<img>` | FR-007 | example | DONE | `tests/web/render.test.js::U53: a row with no cover URL writes the cover box with no image` |
| U54 | Cover URLs are written through `esc` | invariant: page markup is string-built, so an unescaped URL is an injection | example | DONE | `tests/web/render.test.js::U54: cover URLs are written escaped, in the src and in the fallbacks` |
| U55 | `nextCover` with two fallbacks puts the first in `src` and keeps the second | FR-006a | example | DONE | `tests/web/cover-fallback.test.js::U55: nextCover with two fallbacks puts the first in src and keeps the second` |
| U56 | `nextCover` with no fallback removes the image | FR-007 | example | DONE | `tests/web/cover-fallback.test.js::U56: nextCover with no fallback left removes the image, and the cover box stays as the placeholder` |
| U57 | The panel has one `error` listener registered for the capture phase | FR-006a | example | DONE | `tests/web/cover-fallback.test.js::U57: the panel has one error listener, registered for the capture phase` |
| U58 | `NewReleasesInternals` also exposes `nextCover` | contracts/user-view.md | example | DONE | `tests/web/exposure.test.js::user-view.html exposes exactly its testable helpers` |

### `src/Jellyfin.Plugin.NewReleases/Web/user-view.html`: stylesheet (`tests/web/styles.test.js`)

| id  | behavior | traces | kind | state | test |
| --- | --- | --- | --- | --- | --- |
| U59 | `@media (max-width: 600px)` puts `.nr-actions` on its own row, with equal-width columns | FR-009, spec edge case "narrow screen" | example | DONE | `tests/web/styles.test.js::U59: below 600 px the actions take their own row, in equal columns` |
| U60 | `.nr-cover` declares a 64 × 64 box with a background | FR-007 | example | DONE | `tests/web/styles.test.js::U60: .nr-cover declares a 64 by 64 box with a background` |
| U61 | `.nr-cover img` declares `object-fit: cover` | spec edge case "not square" | example | DONE | `tests/web/styles.test.js::U61: a cover image fills the box without stretching` |
| U62 | The test's contrast function rates `#0000ee` on `#1c1c1c` below 4.5, which pins it against the defect the spec reports | FR-010 | example | PENDING | |
| U63 | The test's contrast function rates `#ffffff` on `#000000` at 21, the formula's upper bound | FR-010 | example | PENDING | |
| U66 | `.nr-row` declares three columns, the 64 px cover first, then the details, then the actions | US2-AS1 ("beside the release details"), FR-007; discovered in cycle 40 (named by T016, missing from the list) | example | DONE | `tests/web/styles.test.js::U66: a card lays out the 64 px cover, then the details, then the actions` |

## Invariants and edge cases still to place

None. Every edge case in `spec.md` is placed above or named in "Out of scope".

## Out of scope

- **Homonym disambiguation** (MusicBrainz text beside same-name artists): removed on 2026-10-03.
  Jellyfin and the library scan keep one artist per name, so there is nothing to tell apart.
  Needs its own specification.
- **Matching rules and number of suggestions**: the browser's native list decides them
  (FR-001, Clarifications 2026-10-01). No test can, or should, pin a browser's matching.
- **Typing stays responsive for more than 1,000 artists**: the browser filters natively, and
  the page does an O(1) map lookup per keystroke. There is no measurable requirement, so there
  is no test.
- **Pixel-equal button widths, rendered contrast, real lazy-load timing**: no layout in the
  fake DOM. Checked in the real-browser pass, `quickstart.md` §2.
- **Admin page**: unchanged (spec Assumptions).
- **Light themes**: contrast is specified for the default dark theme only (spec Assumptions).
- **Fetching or proxying images on the server**: forbidden by FR-008, so the server has no
  behavior to test. U32–U35 assert only the URL strings.

## Verification commands

Copied verbatim from `.specify/memory/tdd-profile.md` (detected at `b1b4c7e`):

- Single test (dotnet): `dotnet test --configuration Release --filter "FullyQualifiedName~{name}" -- RunConfiguration.TreatNoTestsAsError=true`
- Full suite (dotnet): `dotnet test --configuration Release`
- File (node): `node --test tests/web/{file}`
- Full suite (node): `node --test "tests/web/*.test.js"`
- Watch (node): `node --test --watch "tests/web/*.test.js"`
- Coverage (node): `node --test --experimental-test-coverage "tests/web/*.test.js"`
- Mutation: none in the profile. Use a deliberate-mutant spot check (constitution II).
- Shell note: prefix `PATH=/opt/homebrew/opt/dotnet/bin:$PATH DOTNET_ROOT=/opt/homebrew/opt/dotnet/libexec` when `dotnet` resolves to SDK 9.
