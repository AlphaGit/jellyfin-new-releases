# HTTP API delta: Explain each unmatched artist step by step

This document amends `specs/001-track-new-releases/contracts/http-api.md`. No route is added or
renamed, so the route table in `docs/http-surface.md` does not change. When this feature is
implemented, apply the change below to the `001` contract, as `005` and `007` did.

## GET `Plugins/NewReleases/Admin/Status`: `unmatched[]` is reshaped

Each entry's `sources` and `hint` are removed. `reasons` replaces them.

```json
"unmatched": [
  { "jellyfinId": "…", "name": "Nirvana",
    "reasons": [
      { "source": "musicbrainz", "state": "Unmatched", "failedStep": "Unique",
        "steps": [
          { "name": "Id",     "state": "Failed" },
          { "name": "Search", "state": "Passed" },
          { "name": "Score",  "state": "Passed" },
          { "name": "Unique", "state": "Failed", "values": [100, 96] }
        ],
        "reason": "ambiguous (score 100 vs 96)",
        "candidates": [
          { "name": "Nirvana", "score": 100, "id": "5b11f4ce-a62d-471e-81fc-a69a8278c7da" },
          { "name": "Nirvana", "score": 96,  "id": "9282c8b4-ca0b-4c6b-b7e3-4f7762dfc4d6" },
          { "name": "Approaching Nirvana", "score": 74, "id": "c3a64a25-251b-4d03-afba-1471440245b8" }
        ],
        "exactNameCount": null, "albumsCompared": null },
      { "source": "deezer", "state": "Matched", "failedStep": null, "steps": [],
        "reason": null, "candidates": null, "exactNameCount": null, "albumsCompared": null }
    ] }
]
```

| Field | Rule |
| --- | --- |
| `reasons` | One entry per **enabled** source, in the order of the response's `sources` array. Never empty. |
| `source` | The source ID, as in `sources[].id`. The page shows the matching `displayName`. |
| `state` | `Matched` \| `Unmatched` \| `NotChecked`. |
| `failedStep` | For `Unmatched`: `Id`, `Search`, `Score`, `Unique` (MusicBrainz) or `Name`, `Album` (Deezer). `null` for the other states, and for an unrecognized reason. |
| `steps` | For `Unmatched` with a known `failedStep`: every step of the source, in order. Otherwise empty. |
| `steps[].state` | `Passed` \| `Failed` \| `NotReached`. |
| `steps[].values` | Present only on the failed step, when the reason holds numbers: `[n]` for `Score`, `[a, b]` for `Unique`. |
| `reason` | The stored reason text for `Unmatched`, otherwise `null`. The page shows it only when `failedStep` is `null`. |
| `candidates` | `null` when the row has no attempt detail (a match, or a row from before `002`). Otherwise up to 3 items in source order; `score` and `id` are `null` for Deezer. |
| `exactNameCount`, `albumsCompared` | Deezer `Album` failures with attempt detail; otherwise `null`. |

An artist is in `unmatched` only when at least one entry has `state` `Unmatched` (FR-014).

The fields are camelCase under the existing `[Produces(JsonDefaults.CamelCaseMediaType)]`
(`docs/http-surface.md` rule 2). Update `tests/fixtures/pages/admin-status.json` in the same
change, because `ResponseNamingTests` holds the DTOs to it.

## `admin.html`: Unmatched artists table

Replace, in the `001` contract's `admin.html` section, "sources and reasons, the fix hint
sentence" with:

- Columns **Artist**, **Reason**, **How to fix**.
- **Reason**: one line per entry of `reasons`, `<displayName>: ` then the trail, `Matched`, or
  `Not checked`. The trail and the fix wording are specified in
  [admin-page.md](./admin-page.md).
- **How to fix**: one fix per `Unmatched` entry with a known `failedStep`, then the tracking line.
