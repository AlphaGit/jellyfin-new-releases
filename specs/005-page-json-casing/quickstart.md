# Quickstart: validating `005-page-json-casing`

Two passes. The suite proves the mismatch cannot return unnoticed; the server pass proves the pages
work. **Neither substitutes for the other** — this defect shipped past a green suite, which is why
`spec.md` puts a real server inside the feature rather than after it.

---

## Prerequisites

- .NET SDK 10. A shell started before 2026-09-20 may resolve `dotnet@9` and fail with `NETSDK1045`;
  prefix `PATH=/opt/homebrew/opt/dotnet/bin:$PATH DOTNET_ROOT=/opt/homebrew/opt/dotnet/libexec`.
- Node 22 or later. No `npm install`: the page tests use `node:test`, `node:assert` and `node:vm`.
- No network and no Jellyfin server are needed for pass 1 (constitution III).

## Pass 1 — the suite

```bash
dotnet build --configuration Release    # zero warnings; TreatWarningsAsErrors is on
dotnet test  --configuration Release
node --test "tests/web/*.test.js"
LANG=de_DE.UTF-8 node --test "tests/web/*.test.js"   # the pages pass undefined as the locale
```

Expected: all three green, with the new tests visible in the run.

### Scenario 1 — the naming declaration is enforced (`SC-004`, `FR-006`, `FR-010`)

Remove `[Produces(JsonDefaults.CamelCaseMediaType)]` from `ReleasesController`, run the suite,
put it back.

```bash
cp src/Jellyfin.Plugin.NewReleases/Api/ReleasesController.cs /tmp/rc.bak
# delete the attribute line
dotnet test --configuration Release      # MUST fail
cp /tmp/rc.bak src/Jellyfin.Plugin.NewReleases/Api/ReleasesController.cs
cmp -s /tmp/rc.bak src/Jellyfin.Plugin.NewReleases/Api/ReleasesController.cs && echo restored
```

Restore from the copy, never with `git checkout --`: that reverts the whole file to `HEAD` and takes
uncommitted work with it. This has cost work twice on this project.

### Scenario 2 — a renamed response fails the suite (`US3-AS1`)

Rename one DTO property — `ListResponse.HasStoredReleases` → `HasStoredRelease` — and run
`dotnet test`. The serialization-contract test MUST fail against
`tests/fixtures/pages/releases.json`. Restore from a copy.

### Scenario 3 — a page reading an absent field fails the suite (`US3-AS2`, `FR-007`)

In `src/Jellyfin.Plugin.NewReleases/Web/user-view.html`, change `data.hasStoredReleases` to
`data.hasStoredRelease` and run `node --test tests/web/render.test.js`. It MUST fail: the page falls
to the empty state against a fixture that has releases. Restore from a copy.

### Scenario 4 — the list-versus-empty decision (`US3-AS3`, `FR-008`, `US1-AS1`, `US1-AS2`)

Already covered by `tests/web/render.test.js`, which drives `render` against
`tests/fixtures/pages/releases.json` for both cases. Confirm both assertions appear in the run:
releases present → rows rendered and no "waiting for its first refresh"; `hasStoredReleases: false`
→ that message and no rows.

### Scenario 5 — no contract names a route the plugin does not serve (`SC-008`)

```bash
dotnet test --configuration Release --filter "FullyQualifiedName~HttpSurfaceTests" \
  -- RunConfiguration.TreatNoTestsAsError=true
```

The `--` argument is mandatory; without it a filter matching nothing exits 0 and every red reads as
green.

### Scenario 6 — the page literals track the prefix (`SC-007`, `FR-013`)

Change `PluginRoutes.Base` to `Plugins/NewReleasesX` and run `dotnet test`. The guard test MUST
fail, naming both pages. Restore from a copy.

## Pass 2 — a running Jellyfin 12 server

`SC-001`, `SC-002`, `SC-003` and `SC-005` are all stated "against a real server". This pass is JD's
own; the suite going green is not evidence for any of them.

Build and install:

```bash
jprm plugin build . --version 0.1.1 --output ./artifacts
```

Install through the catalogue as `FR-013` of `003` intends — add the repository, install, restart —
or copy the package for a pre-release check. Record which was used.

Then, signed in as a normal user:

1. **The view lists releases** (`SC-001`). Open New Releases with releases stored. Expected: the
   list, grouped by year. Not expected: "No data yet. New Releases is waiting for its first
   refresh."
2. **Every row is populated** (`US1-AS4`, `SC-003`). Pick one row of each state. Expected: artist,
   title, type, date, state, and source links all carry values. For an `Incomplete` row, expect the
   missing track titles and the compared edition.
3. **Filters narrow the list** (`US1-AS3`). Artist, type, state, from/to — each in turn.
4. **The empty state is still reachable** (`US1-AS2`, `FR-004`). Purge release data from the
   administrator page, reopen the view. Expected: the "waiting for its first refresh" message.
   Then run a refresh and confirm the list returns.

As an administrator:

5. **Every status value is real** (`SC-002`, `US2-AS1`). Open the plugin's page after a completed
   refresh. Expected: last refresh, releases last checked, next run, artists processed, releases
   found — **no dash anywhere**.
6. **Source health** (`US2-AS2`). Expected: each source's health, calls today, daily budget,
   cooldown and last error.
7. **Unmatched artists** (`US2-AS3`). Expected: each listed with its reason and the hint sentence.
8. **The staleness wording** (`US2-AS4`, `SC-005`, `FR-005`). With data older than one refresh
   interval, expect `002`'s sentence on both pages. With no fetch completed yet, expect `002`'s "no
   age reported" behaviour and **not** a dash from an unreadable field — that is the first edge case
   in `spec.md` and the two must be told apart by observation, not assumed.
9. **The Archive tab** (`spec.md` edge case). Open it. Expected: archived rows with their Ignore /
   Have it badge; an empty Archive shows "The Archive is empty." Not assumed to share the list's
   fate.
10. **Actions still work on the renamed routes.** Ignore, Have it and Restore one release each; Run
    now, Purge release data, Clear Archive from the administrator page. Expected: each succeeds and
    the server log carries no `[ERR]` from this plugin.

Record the pass in `docs/` as `003`'s was. Anything it finds becomes its own specification, not a
patch inside this one.

## References

- Route surface, the naming rule, and the exceptions list: [`contracts/http-surface.md`](./contracts/http-surface.md)
- What the page stand-in does and does not cover: [`contracts/page-sandbox.md`](./contracts/page-sandbox.md)
- The 37 field names and the DTO property behind each: [`data-model.md`](./data-model.md)
- Why each decision was taken: [`research.md`](./research.md)
