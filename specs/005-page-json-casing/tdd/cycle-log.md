# Cycle Log: Make the pages read what the server actually sends

Append only. Newest last. Every entry's `red` block is the evidence that the test existed and
failed before the implementation.

## Baseline

- suite: `dotnet test --configuration Release` -> 257 passed, 0 failed, 0 skipped
- suite: `node --test "tests/web/*.test.js"` -> 33 passed, 0 failed
- commit: `af02934`
- recorded: cycle 0, before any change

### Measured before cycle 1, and the reason the naming behaviours are red

`Jellyfin.Extensions` 12.0.0, the version this plugin builds against:

```text
JsonDefaults.Options           -> {"Items":[1],"Total":1,"HasStoredReleases":true, ...}
JsonDefaults.CamelCaseOptions  -> {"items":[1],"total":1,"hasStoredReleases":true, ...}
JsonDefaults.PascalCaseOptions -> {"Items":[1],"Total":1,"HasStoredReleases":true, ...}
JsonDefaults.Options.PropertyNamingPolicy -> null (exact property names)
```

The host default an endpoint receives when it declares nothing writes `Items`, not `items`. That
is the production defect, reproducible in-process, and it is why `U1`, `U2`, `U3` and `U9` must
resolve the serializer options from the endpoint's own declaration rather than reaching for
`CamelCaseOptions` directly.

## Cycle 1: U1 the list response carries the names the list page reads

- test: `tests/Jellyfin.Plugin.NewReleases.Tests/Api/ResponseNamingTests.cs::ListResponse_AsTheReleasesEndpointDeclaresIt_CarriesTheNamesTheListPageReads` (new), with
  `tests/fixtures/pages/releases.json` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~ResponseNamingTests.ListResponse_AsTheReleasesEndpointDeclaresIt_CarriesTheNamesTheListPageReads" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: Collections differ`
  Expected `[""] = ["hasStoredReleases", "items", "refreshIntervalHours", "releasesLastCheckedAt", "serverToday", ···]`
  Actual `[""] = ["HasStoredReleases", "Items", "RefreshIntervalHours", "ReleasesLastCheckedAt", "ServerToday", ···]`
  (1 failed) — the production defect, reproduced in the suite
- green: `src/Jellyfin.Plugin.NewReleases/Api/ReleasesController.cs:20` added
  `[Produces(JsonDefaults.CamelCaseMediaType)]` at class level. Suite `dotnet test --configuration Release`
  -> 258 passed, 0 failed
- refactor: none needed. The options resolution and the name walk are used by one test so far; the
  duplication that would justify extracting them into `Support/` does not exist yet.
- commit: see below

### Note: what this cycle necessarily also satisfied

The declaration is per-controller, so the one attribute that made `U1` pass also makes `U2`
(`ArtistsResponse`) and `U3` (`StatusResponse`) pass — both are returned by the same controller.
Their cycles therefore cannot produce a first-run red, and the playbook's deliberate-mutant check
stands in for one. Recorded here so the audit does not read those cycles as test-after.

The smaller alternative — an action-level attribute per endpoint — was rejected: `FR-010` requires
every endpoint that returns a body to declare the naming, including ones added later, and a
class-level declaration is what makes a new action inherit it rather than silently omit it.

## Cycle 2: U2 the artists response carries the names the artist filter reads

- test: `Api/ResponseNamingTests.cs::ArtistsResponse_AsTheArtistsEndpointDeclaresIt_CarriesTheNamesTheArtistFilterReads` (new), with `tests/fixtures/pages/artists.json` (new)
- red: **passed on the first run**, as cycle 1's note predicted — the class-level declaration that
  made `U1` pass covers every response this controller returns. Deliberate-mutant check per the
  playbook: removed `[Produces(JsonDefaults.CamelCaseMediaType)]` from
  `src/Jellyfin.Plugin.NewReleases/Api/ReleasesController.cs` ->
  `Assert.Equal() Failure: Collections differ`,
  Expected `[""] = ["items"], ["items[]"] = ["jellyfinId", "name"]`,
  Actual `[""] = ["Items"], ["Items[]"] = ["JellyfinId", "Name"]` (1 failed)
- green: mutant restored from a file copy, verified with `cmp -s`. Suite
  `dotnet test --configuration Release` -> 259 passed, 0 failed. No production change in this cycle
- refactor: none needed
- commit: see below

## Cycle 3: U3 the small status response carries the names its contract records

- test: `Api/ResponseNamingTests.cs::StatusResponse_AsTheStatusEndpointDeclaresIt_CarriesTheNamesItsContractRecords` (new), with `tests/fixtures/pages/status.json` (new)
- red: passed on the first run, same reason as cycle 2. Deliberate mutant, same removal ->
  Expected `[""] = ["hasStoredReleases", "isRunning", "refreshIntervalHours", "releasesLastCheckedAt"]`,
  Actual `[""] = ["HasStoredReleases", "IsRunning", "RefreshIntervalHours", "ReleasesLastCheckedAt"]`
  (1 failed)
- green: mutant restored from a file copy, verified with `cmp -s`. No production change in this cycle
- refactor: none needed
- notes: no page reads this response. It is covered because `FR-010` is one rule over every endpoint
  that returns a body, deliberately without a judgement about which bodies matter
- commit: see below
