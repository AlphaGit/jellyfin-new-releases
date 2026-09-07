# Quickstart: validating Track New Releases

Runnable checks that prove the feature end to end. Every acceptance scenario in `spec.md` maps
to a hermetic test at a real entry point (constitution II), plus one manual smoke run against a
Jellyfin server. Data shapes are in [data-model.md](data-model.md); endpoints in
[contracts/http-api.md](contracts/http-api.md).

## Prerequisites

- .NET SDK 9 (`dotnet --version` → 9.x). Build: `dotnet build --configuration Release` (zero
  warnings; `TreatWarningsAsErrors` is on).
- Tests: `dotnet test --configuration Release`. Single test:
  `dotnet test --configuration Release --filter "FullyQualifiedName~<Class>.<Method>" -- RunConfiguration.TreatNoTestsAsError=true`
  (from `.specify/memory/tdd-profile.md`).
- No network is needed for any test. Fixtures live in `tests/fixtures/{musicbrainz,deezer}/`.

## Recording fixtures (developer, once, live network — never inside a test)

```bash
UA='JellyfinNewReleases/0.1.0 ( https://github.com/alphagit/jellyfin-new-releases )'
curl -sH "User-Agent: $UA" 'https://musicbrainz.org/ws/2/artist?query=artist:%22Daft%20Punk%22&limit=5&fmt=json' > tests/fixtures/musicbrainz/artist_search_confident.json
curl -sH "User-Agent: $UA" 'https://musicbrainz.org/ws/2/release?artist=056e4f3e-d505-4dad-8ec1-d04f521cbb56&status=official&inc=release-groups&limit=100&offset=0&fmt=json' > tests/fixtures/musicbrainz/releases_page1.json
curl -s 'https://api.deezer.com/search/artist?q=Daft%20Punk&limit=25' > tests/fixtures/deezer/search_artist_exact.json
curl -s 'https://api.deezer.com/artist/27/albums?index=0&limit=100' > tests/fixtures/deezer/artist_albums_page1.json
grep -rn "apikey\|api_key\|access_token\|Authorization" tests/fixtures/   # must print nothing
```

Trim large pages by hand to the rows a scenario needs; keep the envelope fields
(`release-count`, `total`, `next`) consistent with the trimmed content.

## Scenario → test map

Entry points: **Task** = `RefreshNewReleasesTask.ExecuteAsync` with `TestDatabase`,
`LibraryFakes` (substituted `ILibraryManager` returning `MusicArtist`/`MusicAlbum`/`Audio`
items), `StubHttpMessageHandler` + fixtures, `TimeProviderStub`. **Releases API** =
`ReleasesController` with `TestDatabase` and a `ClaimsPrincipal` carrying `Jellyfin-UserId` and a
substituted `IUserManager` returning an in-memory `User`. **Admin API** = `AdminController` with a
substituted `ITaskManager`.

| Spec item | Entry point | Check |
| --- | --- | --- |
| US1-1 Z listed, X and Y not | Task → Releases API | library has X, Y; fixtures list X, Y, Z → list contains only Z |
| US1-2 newest first, grouped by year | Releases API | order of `items[].date`; client grouping is by `date[0..4]` (manual smoke) |
| US1-3 filter by artist | Releases API | `?artistId=` returns only that artist; without it all 40 |
| US1-4 source links | Releases API | each item has one `sources[]` entry per listing source with an https URL on the allowed host |
| US1-5 empty state | Releases API | no `refresh_run` → `hasCompletedRefresh=false`, `items=[]` (fragment text is a manual check) |
| US1-6 added to library disappears | Task ×2 | second run with album Z in the snapshot → Z `Owned`, absent from list |
| US1-7 Upcoming | Releases API | `date > serverToday` → `state=Upcoming`; `?state=Upcoming` filters |
| US1-8 Incomplete with missing tracks | Task → Releases API | album with 8/10 tracks → `state=Incomplete`, `missingTracks` has 2 titles, `comparedEdition` set |
| US2-1 defaults | `PluginConfigurationTests` | new config: both sources on, Album+EP on, others off |
| US2-2 persist | `PluginConfigurationTests` | XmlSerializer round-trip equality |
| US2-3 Run now + status | Admin API | `run-now` → `QueueScheduledTask<RefreshNewReleasesTask>()` received, `202`; `status.lastRun` from `refresh_run` |
| US2-4 failing source isolated | Task | MusicBrainz stub 503 ×N, Deezer OK → Deezer releases stored, `source_state.musicbrainz` Failing/CoolingDown |
| US2-5 purge keeps Archive | Admin API → Task → Releases API | decision survives purge; re-fetch → release in `?archived=true` |
| US2-6 terms acceptance | — | not applicable in v1 (research R12); recorded, no test |
| US2-7 Clear Archive | Admin API → Releases API | decisions gone, releases back in list, release rows untouched |
| US3-1 Ignore persists across refresh | Releases API → Task → Releases API | `POST ignore` → absent; after run still absent; present in Archive |
| US3-2 Restore | Releases API | `POST restore` → back in list in date position |
| US3-3 Have it on Incomplete | Releases API → Task | archived despite missing tracks after another run |
| US3-4 Have it stays after library completes | Task | album gains tracks → `Owned`; decision still present, row in Archive |
| Edge: homonyms / low confidence | `MusicBrainzSourceTests`, `DeezerSourceTests` | ambiguous fixture → `Unmatched(reason)`; Deezer homonyms without corroboration → `Unmatched` |
| Edge: unmatched everywhere | Admin API | `unmatched[]` has the artist with Jellyfin id and hint |
| Edge: edition-titled library album | `TitleNormalizerTests`, `OwnershipMatcherTests` | `Album (Deluxe Edition)` ≡ `Album`; best-overlap edition chosen |
| Edge: year-only / undated | `ReleaseRepositoryTests` | `date_sort` padding; order undated last |
| Edge: source unavailable mid-run | Task | Partial outcome → no entries removed, `resume_offset` kept |
| Edge: rotation | Task | `last_refreshed_at` ordering; second run starts with the artist not yet processed |
| Edge: track lists only for candidates | Task | edition requests count == candidates, not catalogue size |
| Edge: artist removed | Task | artist absent from snapshot → its releases deleted |
| Edge: duplicate artist entries | `LibraryScannerTests` | two `MusicArtist` items sharing an MBID → one library artist |
| Edge: no library permission | Releases API | user with `EnabledFolders` excluding the library → empty list, `403` on ignore |
| FR-011 rate limit, budget, cooldown | `SourceHttpClientTests`, `SourceStateRepositoryTests` | token bucket spacing with `TimeProviderStub`; budget exhaustion throws; 5 failures → cooldown |
| FR-017/018 outbound content | `SourceHttpClientTests` | `User-Agent` = `JellyfinNewReleases/<v> ( contact )`; request URLs contain only names/ids |
| FR-019 accessibility | manual | keyboard-only walkthrough below |
| SC-005 | `ReleaseRepositoryTests` | 500 rows: list query < 500 ms on the dev machine (soft assertion, logged) |

## Manual smoke run (Jellyfin 10.11.11, Plugin Pages installed)

1. `dotnet build --configuration Release`; copy `src/…/bin/Release/net9.0/Jellyfin.Plugin.NewReleases.dll`
   and the SQLite DLLs/native lib listed in `build.yaml` into the server's `plugins/NewReleases/`;
   restart Jellyfin.
2. Dashboard → Plugins → New Releases: defaults shown (both sources, Album + EP). Set a contact.
   Save, reload, confirm persisted.
3. Click **Run now**. Dashboard → Scheduled Tasks shows "Refresh new releases" under
   "New Releases" running. When done, the plugin page shows last refresh, artists processed,
   releases found, unmatched artists with working links.
4. Web client → hamburger menu → **New Releases**: groups Upcoming / years / Undated; filters
   work; source links open in a new tab; artist links open the artist page.
5. Keyboard only (FR-019, SC-008): Tab through filters, a row's links and buttons; Enter on
   **Ignore** → announcement heard in a screen reader, row gone; Tab to **Archive**, **Restore**.
6. Stop network access, open the page: list still shows; after > 24 h it shows "Last refreshed
   … ago." (SC-007).
7. Verify CI after pushing to `main`: `gh run list --branch main` green (constitution).
