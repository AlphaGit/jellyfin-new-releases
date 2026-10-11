# Quickstart: validate the four fixes

## 1. Suite gate (no network, no server)

```bash
dotnet build --configuration Release   # zero warnings
dotnet test
node --test tests/web/
```

Each scenario below has at least one test that goes through the real entry point (the refresh
task, `AdminController`, `admin.html`, or `SourceHttpClient`), with stubbed HTTP and recorded fixtures.

| Spec item | What the test proves |
| --- | --- |
| US1-1 | A snapshot artist with MBID `89ad4ac3-…`: zero requests to either source; both sources `Unmatched("placeholder artist")`. |
| US1-2 | An artist with no MBID whose MusicBrainz search returns Various Artists: no catalogue request, no Deezer request. Same for a Deezer match to `5080` with MusicBrainz disabled. |
| US1-3 | Admin status: each enabled source of the placeholder row has `failedStep` `Placeholder` and the one-step trail. Page (`node --test`): the placeholder fix shows once; another unmatched row keeps its `008` trail and fixes. |
| US1-4, FR-009 | A real artist next to a placeholder is fetched with the same requests as before, plus `artist-credits` in the browse. |
| US2-1..3 | A `a;b` artist with `releases_joint_artist.json`: one ID per request; only releases credited to both are stored; outcome `Complete`. |
| US2-5, US2-6 | Ownership: a release found for the joint artist is `Owned` by a library album filed under a credited member; a same-title album of an uncredited artist is not compared. |
| US3-1..4 | Retry delays: hint 0 → backoff; hint > backoff → hint and floor; past date → backoff; no hint → backoff. |
| US4-1..2 | Host start closes an open run with `Interrupted` and the start time; finished runs unchanged; a run that started after the start instant stays open. |
| FR-008 | On a database whose check rejects `Interrupted`, start logs one warning and does not throw. |
| Migration | A database at schema `002` migrates to `003`; existing `source_entry` rows read `credit = []`. |

## 2. Real-server pass (maintainer)

Prerequisites, done on 2026-10-10: placeholder releases deleted, `refresh_run` outcome check
widened by hand. The task's daily trigger is removed until the new version is installed.

1. Install the new version and restart Jellyfin.
2. Admin page: last refresh shows `(Interrupted)` for run 1.
3. Run "Refresh new releases" by hand.
4. Server log: no catalogue page for Various Artists or Soundtrack; no `answered 400` for
   "Seven Lions + Kill the Noise"; no `retrying in 00:00:00`.
5. Admin page: "Various Artists" and "Soundtrack" listed as unmatched, trail `Placeholder ✗` per source, with the placeholder fix once.
6. Release list, artist "Seven Lions + Kill the Noise": only releases credited to both.
7. Add the daily trigger back.
