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

### Deviation: cycles 2 and 3 share one commit

The playbook asks for one commit per cycle. Cycles 2 and 3 added only tests, made no production
change, and were committed together as `test: pin the releases controller's response naming against
what the pages read`. Recorded rather than rewritten. Later cycles commit one to one.

## Cycle 4: U9 the administrator status response carries the names that page reads

- test: `Api/ResponseNamingTests.cs::AdminStatusResponse_AsTheAdminStatusEndpointDeclaresIt_CarriesTheNamesTheAdministratorPageReads` (new), with `tests/fixtures/pages/admin-status.json` (new)
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~ResponseNamingTests.AdminStatusResponse_AsTheAdminStatusEndpointDeclaresIt" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: Collections differ`
  Expected `["lastRun"] = ["artistsProcessed", "editionsFetched", "endedAt", "errors", "outcome", ···]`
  Actual `["LastRun"] = ["ArtistsProcessed", "EditionsFetched", "EndedAt", "Errors", "Outcome", ···]`
  (1 failed) — a real first-run red: `AdminController` is a different controller and declared nothing
- green: `src/Jellyfin.Plugin.NewReleases/Api/AdminController.cs:18` added
  `[Produces(JsonDefaults.CamelCaseMediaType)]` at class level. Suite -> 261 passed, 0 failed
- refactor: none needed
- notes: the red output also confirms `matchedArtists`' dictionary **keys** are untouched by the
  naming policy (`["matchedArtists"] = ["deezer", "musicbrainz"]` on both sides). Keys are data, not
  contract names, which is why the fixture can state them exactly
- commit: see below

## Cycle 5: U8 the releases controller is served under the PluginRoutes base

- test: `Api/HttpSurfaceTests.cs::ReleasesController_IsServedUnderThePluginRoutesBase` (new file),
  with `src/Jellyfin.Plugin.NewReleases/Api/PluginRoutes.cs` added as the minimal declaration the
  test needs to resolve the symbol, per the playbook's step 3
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~HttpSurfaceTests.ReleasesController_IsServedUnderThePluginRoutesBase" -- RunConfiguration.TreatNoTestsAsError=true`
  -> `Assert.Equal() Failure: Strings differ` Expected `"Plugins/NewReleases"` Actual `"Plugins/NewReleases/api"` (1 failed)
- green: `[Route("Plugins/NewReleases/api")]` -> `[Route(PluginRoutes.Base)]`. Suite -> 262 passed
- refactor: none needed

## Cycle 6: U4 the list is served at Releases

- test: `Api/HttpSurfaceTests.cs::ReleasesController_ServesTheListAtReleases`
- red: `Assert.Contains() Failure: Item not found in collection` Not found `"GET Plugins/NewReleases/Releases"` (1 failed)
- green: `[HttpGet("releases")]` -> `[HttpGet("Releases")]`. Suite -> 263 passed
- refactor: none needed

## Cycle 7: U5 the artist filter is served at Artists

- test: `Api/HttpSurfaceTests.cs::ReleasesController_ServesTheArtistFilterAtArtists`
- red: Not found `"GET Plugins/NewReleases/Artists"` (1 failed)
- green: `[HttpGet("artists")]` -> `[HttpGet("Artists")]`. Suite -> 264 passed
- refactor: none needed

## Cycle 8: U6 the small status is served at Status

- test: `Api/HttpSurfaceTests.cs::ReleasesController_ServesTheSmallStatusAtStatus`
- red: Not found `"GET Plugins/NewReleases/Status"` (1 failed)
- green: `[HttpGet("status")]` -> `[HttpGet("Status")]`. Suite -> 265 passed
- refactor: none needed

## Cycle 9: U7 the decisions are served under the release they decide

- test: `Api/HttpSurfaceTests.cs::ReleasesController_ServesTheDecisionsUnderTheReleaseTheyDecide`
- red: Not found `"POST Plugins/NewReleases/Releases/{id:long}/Ignore"` (1 failed)
- green: the three `[HttpPost]` templates became `Releases/{id:long}/Ignore`, `/HaveIt`, `/Restore`.
  Suite -> 266 passed
- refactor: none needed

## Cycle 10: U12 the administrator controller is served under the PluginRoutes admin prefix

- test: `Api/HttpSurfaceTests.cs::AdminController_IsServedUnderThePluginRoutesAdmin`
- red: Expected `"Plugins/NewReleases/Admin"` Actual `"Plugins/NewReleases/api/admin"` (1 failed)
- green: `[Route("Plugins/NewReleases/api/admin")]` -> `[Route(PluginRoutes.Admin)]`. Suite -> 267 passed
- refactor: none needed

## Cycle 11: U10 the administrator status is served at Status

- test: `Api/HttpSurfaceTests.cs::AdminController_ServesItsStatusAtStatus`
- red: Not found `"GET Plugins/NewReleases/Admin/Status"` (1 failed)
- green: `[HttpGet("status")]` -> `[HttpGet("Status")]`. Suite -> 268 passed
- refactor: none needed

## Cycle 12: U11 the administrator actions are PascalCase segments

- test: `Api/HttpSurfaceTests.cs::AdminController_ServesItsActionsAsPascalCaseSegments`
- red: Not found `"POST Plugins/NewReleases/Admin/RunNow"` (1 failed)
- green: `run-now`, `purge`, `clear-archive` became `RunNow`, `Purge`, `ClearArchive`. Suite -> 269 passed
- refactor: none needed

## Cycle 13: U13 the user view is served at the PluginRoutes user view

- test: `Api/HttpSurfaceTests.cs::UserViewController_IsServedAtThePluginRoutesUserView`
- red: passed on the first run — the literal already spelled the same path, so only the *derivation*
  was missing and no assertion could see it. Deliberate mutant: `PluginRoutes.Base` changed to
  `"Plugins/NewReleasesX"` -> Expected `"Plugins/NewReleasesX/UserView"` Actual
  `"Plugins/NewReleases/UserView"` (1 failed). Restored from a file copy, verified with `cmp -s`
- green: `[Route("Plugins/NewReleases/UserView")]` -> `[Route(PluginRoutes.UserView)]`. Suite -> 271 passed
- refactor: none needed

## Cycle 14: U14 the user view declares text/html and no JSON profile

- test: `Api/HttpSurfaceTests.cs::UserViewController_DeclaresTextHtmlAndNoJsonProfile`
- red: passed on the first run. Deliberate mutant: the action's `[Produces("text/html")]` widened to
  `[Produces("text/html", "application/json")]` -> `Assert.DoesNotContain() Failure: Filter matched in collection`
  (1 failed). Restored from a file copy, verified with `cmp -s`
- green: no production change. This behaviour records an exemption that already holds, and the test
  is what stops it widening into an unstated JSON endpoint
- refactor: none needed

## Cycle 15: U28 the Plugin Pages entry derives its Url from PluginRoutes

- test: `Integration/PluginPagesRegistrationTests.cs::RegistersThePageEntry…` gained a second
  assertion against `PluginRoutes.UserViewAbsolute`; the existing literal assertion stays, so the
  payload is pinned both ways
- red: passed on the first run, same reason as cycle 13 — the literal already matched. The cycle 13
  mutant on `PluginRoutes.Base` is the same proof for this assertion
- green: the payload's raw string became a constant interpolated one carrying
  `{{PluginRoutes.UserViewAbsolute}}`, so the path is written once. Suite -> 271 passed
- refactor: none needed

### Deviation: cycles 5 to 15 are committed per controller, not per cycle

Eleven cycles, three commits: the releases routes, the administrator routes, and the user view with
its registration payload. Each cycle's red was observed and recorded before its own implementation;
only the commit boundary is coarser than the playbook's one-per-cycle. Recorded rather than hidden.

## Cycle 16: U23 each embedded page builds its paths from the prefix its endpoints are served under

- test: `Api/HttpSurfaceTests.cs::EachEmbeddedPage_BuildsItsPathsFromThePrefixItsEndpointsAreServedUnder`
  (a `[Theory]`, one row per page)
- list edit before the red: the behaviour read "begins with `PluginRoutes.Base`", which
  `'Plugins/NewReleases/api/'` already satisfied — an assertion that could not fail. Tightened to
  "is exactly the prefix its endpoints are served under" before writing the test. Recorded here
  because a behaviour reworded mid-loop must be visible to the audit
- red: `dotnet test --configuration Release --filter "FullyQualifiedName~HttpSurfaceTests.EachEmbeddedPage_BuildsItsPathsFromThePrefixItsEndpointsAreServedUnder" -- RunConfiguration.TreatNoTestsAsError=true`
  -> Expected `"Plugins/NewReleases/"` Actual `"Plugins/NewReleases/api/"`; Expected
  `"Plugins/NewReleases/Admin/"` Actual `"Plugins/NewReleases/api/admin/"` (2 failed)
- green: both pages' `var API = '...'` literals updated. Suite -> 273 passed, 0 failed
- refactor: none needed

### U24 dropped

`U24` ("every request path literal either page sends resolves to a route the plugin registers") is
dropped. Extracting those paths from the page source is unreliable: `user-view.html` completes a
decision path at runtime from a `data-action` attribute, and `admin.html` passes its action paths
through `confirmed(...)` rather than to `api(` directly, so a source scan would read a prefix and
guess the rest.

The guard is not lost. `U39` and `U50` assert the **actual** URLs each page requests, captured from a
recording `ApiClient` while the page runs, which is stronger extraction than any regex over the
source; `U23` pins the prefix; and `U4`-`U7`, `U10`-`U12` pin what the plugin registers. A path that
exists on neither side still fails one of those.

What the drop does give up: a page could request a well-formed path that no controller registers, and
only the real-server pass would notice. Recorded as the accepted cost.

## Cycle 17: U52 an element gives back what was written to it

- test: `tests/web/fake-dom.test.js::an element gives back what was written to it` (new), with
  `tests/web/fake-dom.js` added as a stub that throws
- red: `node --test tests/web/fake-dom.test.js`
  -> `error: 'fake-dom: documentFor is not implemented'`, `# fail 1` — a deliberate
  not-implemented signal from a stub the test drives
- green: `FakeElement` with `innerHTML`, `textContent`, `hidden`, `setAttribute`/`getAttribute`,
  and a `documentFor` that hands one out per id. Node suite -> 34 passed
- refactor: none needed

## Cycle 18: U53 insertAdjacentHTML beforeend appends rather than replacing

- test: `tests/web/fake-dom.test.js::insertAdjacentHTML beforeend appends rather than replacing`
- red: `error: 'row.insertAdjacentHTML is not a function'`, `# fail 1`
- green: `insertAdjacentHTML` appends to `innerHTML`, and throws for any position but `beforeend`
  rather than silently modelling one it does not have
- refactor: none needed

## Cycle 19: U54 an id the page does not declare answers null

- test: `tests/web/fake-dom.test.js::an id the page does not declare answers null, so reaching for an unmodelled element fails loudly`
- red: `# fail 1` — `getElementById` created an element for any id, so the assertion that an
  undeclared id answers `null` failed with an object where `null` was expected
- green: `declaredIds` reads the page's own markup and the lookup answers `null` outside that set.
  Ids built at runtime inside a JS string (`id="' + item.id + '"`) are excluded by the identifier
  shape, so they cannot widen the model by accident. Node suite -> 36 passed
- refactor: none needed

## Cycle 20: U55 a page loaded through the sandbox runs its initialization to completion

- test: `tests/web/fake-dom.test.js::a page loaded through the sandbox runs its initialization to completion`
- red: `error: 'loadPageDom is not a function'`, `# fail 1`
- green: element `querySelector` resolves `#id` through the owning document and treats any other
  selector as a child it owns; `load-page.js` builds its sandbox `document` from `documentFor` and
  gained `loadPageDom`, which returns the document alongside the helpers. `loadPage` keeps its
  signature, so every existing page test is untouched. Node suite -> 37 passed
- refactor: `load-page.js`'s header still said "Every element lookup answers null" and that the page
  "is expected to fail" after exposing its helpers. Both were now false. Rewritten to describe the
  stand-in and why the catch stays. Node suite re-run green after the edit

## Cycle 21: U39 the New Releases view asks for the artist filter and the list

- test: `tests/web/requests.test.js::the New Releases view asks for the artist filter and the list`
  (new file), driving the page with an `ApiClient` that records requests instead of answering them
- red: `node --test tests/web/requests.test.js` ->
  `+ 'GET Plugins/NewReleases/artists', + 'GET Plugins/NewReleases/releases'` against
  `- 'GET Plugins/NewReleases/Artists', - 'GET Plugins/NewReleases/Releases'`, `# fail 1`
- green: `user-view.html` now requests `Releases` and `Artists`, posts to
  `Releases/{id}/{action}`, and its three `data-action` values became `Ignore`, `HaveIt`,
  `Restore` — the announcement text reads the same values, so it moved with them.
  Node suite -> 38 passed
- refactor: none needed

## Cycle 22: U50 the administrator page asks for its status and posts its actions

- test: `tests/web/requests.test.js::the administrator page asks for its status and posts its actions`
- red: `Expected values to be strictly deep-equal` — actual `['GET Plugins/NewReleases/Admin/status']`
  against the four expected requests, `# fail 1`. The lower-case `status` is the defect; the three
  missing posts are the test not yet answering `Dashboard.confirm`
- green: `admin.html` now requests `Status` and posts `RunNow`, `Purge`, `ClearArchive`.
  `load-page.js` gained `Dashboard.showLoadingMsg`/`hideLoadingMsg` stubs, without which the page's
  `pageshow` handler cannot run at all. Node suite -> 39 passed
- notes: **the stand-in's stated limits moved.** A test may now invoke a listener the fake recorded,
  which is how `pageshow` and the three action buttons are driven. Nothing is dispatched, nothing
  bubbles, and no event object is synthesized. `contracts/page-sandbox.md` is updated to say so
- refactor: none needed
