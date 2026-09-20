# Seed: the embedded pages read camelCase; Jellyfin 12 sends PascalCase

Found by the real-server install of `0.1.0` on Jellyfin 12.1 — see
[`docs/real-server-install-0.1.0.md`](../../docs/real-server-install-0.1.0.md) finding 4.
This is the seed for a specification, not the specification. Run `/speckit-specify`.

## The defect

**Both embedded pages are unusable on Jellyfin 12.** With 812 releases stored and the API
returning them, `user-view.html` shows "No data yet. New Releases is waiting for its first
refresh." and `admin.html` shows a dash for every status value.

The pages read camelCase (`data.items`, `data.hasStoredReleases`, `.sources`, `.lastRun`,
`.libraryArtists`, `data.releasesLastCheckedAt`, `data.refreshIntervalHours`); the Jellyfin 12
host serialises the same DTOs in PascalCase (`Items`, `HasStoredReleases`, ...). Verified in the
browser: the page's own request returns `HTTP 200` with a populated `Items` array, and the empty
state renders anyway.

## Why the suite is green

- The C# tests assert the **DTO shape** the controllers return, before the host serialises it.
- The page tests cover **pure helpers only**. `002`'s test list records that `render`,
  `refreshStatus` and the rest "need a simulated browser this project does not have".

`render` is the function that reads `data.hasStoredReleases`. The single function that consumes a
real response is the single function with no test.

## Questions for the specification

- **Fix on which side?** Make the pages tolerant of either casing, pin the serialization to
  camelCase in the plugin's own controllers, or rewrite the pages to PascalCase. Pinning is the
  only option that cannot drift again with a host change, but it diverges from whatever the host
  does elsewhere.
- **How is this tested so it cannot recur?** This is the real question. A test that asserts the
  DTO shape will not catch it. Options: assert the serialized JSON through the host's own
  serializer configuration, or give the page tests a response fixture captured from a real server
  and run `render` against it in the existing `node:vm` sandbox.
- **Does the same mismatch affect anything else?** Every field both pages read should be
  enumerated and checked, not just the ones observed failing.
- **Is `002`'s staleness sentence affected?** It reads `releasesLastCheckedAt`, so probably yes,
  and `002`'s acceptance criteria are therefore also unmet on Jellyfin 12.
