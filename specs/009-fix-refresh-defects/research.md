# Research: Fix four refresh defects found on the live server

Facts were checked against the code at `afb9b69`, the live MusicBrainz and Deezer APIs
(2026-10-10), and the live server's plugin database.

## R1. Which identifiers are placeholders

- **Decision**: a fixed set in one static class, `Sources/PlaceholderArtists.cs`.
  - MusicBrainz (13): Various Artists `89ad4ac3-39f7-470e-963a-56509c546377`, [unknown]
    `125ec42a-7229-4250-afc5-e057484327fe`, [anonymous] `f731ccc4-e22a-43af-a747-64213329e088`,
    [no artist] `eec63d3c-3b81-4ad4-b1e4-7c147d4d2b61`, [traditional]
    `9be7f096-97ec-4615-8957-8d40b5dcbc41`, [data] `33cf029c-63b0-41a0-9855-be2a3665fb3b`,
    [dialogue] `314e1c25-dde7-4e4d-b2f4-0a7b9f7c56dc`, [language instruction]
    `80a8851f-444c-4539-892b-ad2a49292aa9`, [Disney] `66ea0139-149f-4a0c-8fbf-5ea9ec4a6e49`,
    [theatre] `a0ef7e1d-44ff-4039-9435-7d5fefdeecc9`, [church chimes]
    `90068d37-bae7-4292-be4a-704c145bd616`, [unknown animal]
    `459a4aa6-b6b7-4c89-a42b-03acddbe1f80`, MusicBrainz Test Artist
    `7e84f845-ac16-41fe-9ff8-df12eb32af55`.
  - Deezer (1): Various Artists `5080`.
- **Rationale**: the MusicBrainz search `comment:"special purpose"` returns the twelve bracketed
  artists and the test artist; Various Artists has an empty disambiguation and is added by its
  well-known MBID. "No Artist" (`954f3f5b-…`) is excluded: its disambiguation says it is NOT a
  special-purpose artist. Deezer `artist/5080` is "Various Artists". FR-002 asks for a fixed set.
- **Alternatives rejected**: asking MusicBrainz per artist whether the disambiguation says
  "special purpose" (one request per artist, and it relies on free text); a configurable list
  (constitution VI: no configuration for a value that never changes).

## R2. Where the placeholder rule runs

- **Decision**: in `RefreshNewReleasesTask`, in two places, both before any catalogue request.
  1. Before the source loop: when every MBID on the snapshot is a placeholder, the artist is a
     placeholder. No source is asked anything (US1 scenario 1).
  2. After `MatchArtistAsync` (or the stored match) at a source: when the matched source ID is a
     placeholder for that source, the artist is a placeholder. The loop stops for this artist.
  In both cases every enabled source is set to `Unmatched` with the reason
  `MatchReasons.Placeholder` (`"placeholder artist"`). The artist counts as attempted at
  every source, so the rotation moves on (`SetLastRefreshedAsync`).
- **Rationale**: one place decides, and it covers stored matches from earlier runs (the live
  "Various Artists" row is stored `Matched` to `89ad4ac3-…`). Sources are registered MusicBrainz
  first, so a MusicBrainz verdict stops the Deezer name search (Clarifications: placeholder at any
  source means every source).
- **Alternatives rejected**: a check inside each source (two copies, and a source cannot set
  the state of the other source).

## R3. The Placeholder step on the administrator page

- **Fact**: feature `008` removed the per-row `hint`. `AdminController.ToReason` parses each
  stored reason with `MatchReasons.Parse(source, reason)` into a failed step, and
  `MatchReasons.Steps` builds the trail from a fixed step order per source. `admin.html`
  `fixFor` writes one fix per entry from `source` and `failedStep`, and joins them with no
  de-duplication. An unknown reason gives `failedStep` null, and the page prints the raw text.
- **Decision**:
  - `MatchReasons.Placeholder = "placeholder artist"`, beside `NoResult` and
    `NoCorroboratingAlbum`. R2 writes it.
  - `Parse` maps it to `FailedStep("Placeholder", [])` for both sources.
  - `Steps` returns the single step `Placeholder` `Failed` for it, outside each source's step
    order (Clarifications: one step only).
  - The placeholder row stores no `match_detail`, so `candidates` is null.
  - `admin.html` `fixFor` gains one branch, `failedStep === 'Placeholder'`, with the placeholder
    fix: "Placeholder artist, such as Various Artists. If this is a real artist, correct its
    MusicBrainz ID in Jellyfin's metadata editor or artist.nfo." The row's fixes are
    de-duplicated, so the fix shows once (`008` FR-007).
- **Rationale**: the wire shape of `008` is unchanged. Only a new value of `failedStep` and of
  `steps[].name` appears. The trail and fix wording stay in the page, as `008` research R5 set.

## R4. Joint artists: identifiers and the catalogue

- **Fact**: `LibraryScanner` stores the provider value as is, so `Mbid` and `ArtistKey` are
  `75c016da-…;a9930a14-…` for "Seven Lions + Kill the Noise". `MusicBrainzSource.MatchArtistAsync`
  returns it unchanged, and the browse sends `release?artist=<a;b>` → HTTP 400.
- **Fact**: MusicBrainz credits the duo's releases to both IDs: "The Blood" is
  "Seven Lions + Kill The Noise", "Cold Hearted" is "Seven Lions & Kill the Noise", and "Horizon"
  adds Tritonal. Seven Lions alone has 168 official releases.
- **Decision**:
  - One parser, `MusicBrainzIds.Parse(string?)`: split on `;`, trim, drop empties and
    placeholders, distinct, keep order. `ArtistKey` is not changed (it keys stored rows and
    decisions).
  - `MatchArtistAsync` returns `Matched(string.Join(';', ids))`. A stored `a;b` is the same value.
  - `FetchCataloguePageAsync` parses the stored ID. It browses the **first** ID with
    `inc=release-groups+artist-credits`. With more than one ID it keeps only releases whose
    `artist-credit[].artist.id` set contains every ID. Paging (`offset`, `release-count`) stays
    on the first ID's browse, so a resume offset keeps its meaning.
- **Rationale**: one request per page, exactly one ID per request (FR-005). Filtering on the
  release credit is what "album-artist credit" means at MusicBrainz.
- **Alternatives rejected**: browsing every ID and intersecting (more pages for the same result);
  choosing the ID with the fewest releases (an extra request to learn the counts).

## R5. Release credit for the ownership rule

- **Fact**: Deezer's `artist/<id>/albums` items carry no artist or contributor fields (keys:
  `cover*`, `explicit_lyrics`, `fans`, `genre_id`, `id`, `link`, `md5_image`, `record_type`,
  `release_date`, `title`, `tracklist`, `type`).
- **Decision**: `CatalogueItem` gains `Credit`: a list of `(Mbid?, Name)`. MusicBrainz fills it
  from the release's `artist-credit`. Deezer leaves it empty. A missing `artist-credit` (older
  recorded fixtures) also gives an empty list.
- **Decision**: stored per source entry, in a new column `source_entry.credit` (JSON, default
  `'[]'`), added by migration `003_source_entry_credit.sql`. Every catalogue page upserts every
  entry, so a complete pass fills the column for existing rows.
- **Rationale**: the ownership step runs after all fetches and reads stored releases, so the
  credit must be stored. An empty credit means "only the artist the release was found for", which
  is today's behaviour.

## R6. Ownership against credited library artists

- **Decision**: once per run, after the source loop, build an index of the snapshot's library
  artists: by each parsed MBID, and by normalized name. For each release, its credit is the union
  of its entries' credits. Candidate albums are the found-for artist's albums first, then the
  albums of every other library artist that is credited:
  - its MBIDs (any of them, for a joint artist) intersect the credit's MBIDs, or
  - the library artist has no MBID, or the credit entry has none, and the normalized names are
    equal.
  `OwnershipMatcher.Decide` is unchanged; it receives the longer album list. The found-for
  artist's albums come first, so its `FirstOrDefault` keeps today's preference.
- **Rationale**: FR-005a with the smallest change to the matcher. An uncredited artist's album
  is never in the list, so a same-title album cannot match (US2 scenario 6).

## R7. Retry delay

- **Decision**: in `SourceHttpClient.GetStringAsync`, `delay = max(hint, RetryBackoffs[attempt])`.
  A date hint in the past gives a negative hint, so the backoff wins. The source's next-allowed
  floor is set only when the hint is longer than the backoff.
- **Rationale**: FR-006 and FR-007 in one line. Today a hint of zero replaces the backoff.
- **Note**: `SourceLimits.DefaultRetryAfter` is unused before and after this change. It is not
  removed here (out of scope); it is reported for a later clean-up.

## R8. Closing interrupted runs at start

- **Decision**: a hosted service, `ScheduledTasks/InterruptedRunCloser`, registered with
  `AddHostedService`. `StartAsync` calls `SourceStateRepository.CloseOpenRunsAsync(before: now)`:
  `UPDATE refresh_run SET ended_at = @now, outcome = 'Interrupted' WHERE ended_at IS NULL AND
  started_at < @before`.
- **Rationale**: hosted services start once per host start, so the end time is close to the
  restart (spec Assumptions). The `started_at < @before` guard keeps a run that starts in the same
  instant open. The plugin already uses a hosted service for Plugin Pages.
- **Decision**: `001_initial.sql` widens the `outcome` check to include `'Interrupted'`, for new
  installs. No migration (Clarifications). An install whose check rejects the value logs one
  warning and starts normally; `CloseOpenRunsAsync` catches the constraint error.
- **Alternatives rejected**: closing at the next refresh (decided against); closing inside the
  lazy migration (the end time would be the first use, possibly hours after the restart).

## R9. Fixtures

- **Decision**: record one new MusicBrainz fixture, `tests/fixtures/musicbrainz/releases_joint_artist.json`:
  the Seven Lions browse with `inc=release-groups+artist-credits`, trimmed to a few releases
  (one with both IDs, one with both plus Tritonal, one Seven Lions alone). Existing fixtures stay;
  their missing `artist-credit` parses to an empty credit.
- **Rationale**: constitution III. Public catalogue data; nothing to scrub.
