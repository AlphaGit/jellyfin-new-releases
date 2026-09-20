# Real-server install, version 0.1.0

The pass `spec.md` for `003-jellyfin-12-compat` deliberately placed outside that feature:
"Verification on a running Jellyfin 12 server. That pass happens outside this feature. Anything it
finds becomes its own specification."

This is the record of that pass. **The install succeeded.** Two defects were found that no test in
the suite could have caught, and three facts about Jellyfin 12 that are worth writing down.

## What was done

| | |
| --- | --- |
| Date | 2026-09-20 |
| Server | Jellyfin **12.1.0** (`jellyfin-server` 12.1+ubu2404, native apt, Ubuntu 24.04 on AWS) |
| Plugin | 0.1.0.0, built against Jellyfin **12.0.0**, `targetAbi 12.0.0.0` |
| Method | The catalogue, as `FR-013` intends: add the repository, install from it, restart |
| Sideloading | None. No file was copied into the plugins directory by hand |

The release chain ran for the first time for real: tag `v0.1.0` → JPRM build → `jprm repo add` →
commit `repo/` to `main` → deploy to GitHub Pages.

Before anything was changed on the server, `/etc/jellyfin/system.xml` was copied to
`system.xml.pre-newreleases`. The only server-side changes were: one repository added to the
plugin repository list, the plugin installed, and one service restart.

## What worked

- **The published repository is correct end to end.** The manifest is served over HTTPS, the
  `sourceUrl` it names returns HTTP 200, and the MD5 in the manifest **matches the bytes of the
  downloaded package**.
- **A 12.1 server accepts a `targetAbi` of 12.0.0.0.** The plugin appeared in the catalogue
  alongside 36 others, which is what `SC-004` asserts: Jellyfin offers a version to any server at
  or above the declared ABI.
- **A plugin built against 12.0.0 loads on 12.1.0.** `Loaded plugin: New Releases 0.1.0.0`. This
  was the largest open risk and it did not materialise; .NET bound the 12.1 host assemblies to the
  plugin's 12.0.0 references without complaint.
- **The package needs no manual file step** (`FR-009`). All six declared artefacts extracted:
  the plugin DLL, the four SQLite assemblies and `runtimes/linux-x64/native/libe_sqlite3.so`.
- **The optional-integration degradation works in production.** Plugin Pages is not installed on
  this server, and the log carries exactly the line `U12` specifies, once, at Information level:
  `Plugin Pages is not available, so the New Releases page is not in the menu. Everything else works.`
- **Everything not depending on Plugin Pages works** (`A7`): the plugin reports `Active`, the
  configuration page is served, the scheduled task registered as "Refresh new releases" under the
  "New Releases" category with its daily trigger set, and `api/admin/status` and `api/status`
  both answer 200.
- Nothing else on the server was disturbed: all ten plugins `Active`, all four libraries intact,
  no fatal log entries.

## Finding 1 — an API-key caller gets 400 and a logged error, not 401

**Severity: low. It does not affect any user path.** This was first written up as
"real, user-visible", which was wrong, and is corrected here.

**It cannot happen to a user of the plugin.** The New Releases page calls this API from the
browser with the signed-in user's token, so the claim holds a real user id, `AccessOf` resolves a
real user, and per-user library filtering works — which is what `US1-AS4` asks and what the
install confirmed. The failure was reached only because this pass drove the API with an API key
over SSH, having no browser session; an API key is a server-to-server credential with no user
behind it.

`GET /Plugins/NewReleases/api/releases` and `.../artists` both return **400** with an `[ERR]` in
the server log:

```
System.ArgumentException: Guid can't be empty (Parameter 'id')
   at Jellyfin.Server.Implementations.Users.UserManager.GetUserById(Guid id)
   at Jellyfin.Plugin.NewReleases.Api.ReleasesController.AccessOf(Guid userId)
   at Jellyfin.Plugin.NewReleases.Api.ReleasesController.GetReleasesAsync(...)
```

**Cause.** A caller authenticated by API key has no user. Jellyfin's `User.GetUserId()` returns
`Guid.Empty` for such a caller rather than null, so the controller's "no user id claim" guard does
not fire, and `Guid.Empty` reaches `UserManager.GetUserById`, which throws.

**Why no test caught it.** `ReleasesControllerTests::GetReleases_WithoutTheUserIdClaim_Is401`
builds a `ControllerContext` with **no claim at all** and asserts 401. The real server supplies a
claim whose value is empty. The test's double and the host disagree about how "no user" is
represented, and the double is the one that is wrong.

**What is actually worth fixing.** Two small things, neither urgent:

1. An operator scripting against the plugin's API with a key — a legitimate thing to do — gets an
   unhandled exception, a 400 and a stack trace in the server log instead of a clean 401. Only the
   two user-scoped endpoints are affected; `api/admin/status` and `api/status` both answered 200
   with the same key.
2. **The test disagrees with the host about what "no user" looks like**, and that is the part with
   a future cost. `ControllerContextFactory` models an unauthenticated caller as *no claim at
   all*; Jellyfin supplies a claim holding `Guid.Empty`. So the test asserts 401 for a case that
   cannot occur and misses the one that can. The next controller that reads a claim inherits the
   same blind spot.

The fix is one line — treat `Guid.Empty` as a missing claim — plus a double that matches the host.

**Not fixed here.** Per `spec.md` a real-server finding becomes its own specification.

## Finding 2 — the release chain cannot deploy from a tag under default Pages settings

**Severity: blocked the first real release. Fixed by a repository setting, not by code.**

The first run of `package.yml` on tag `v0.1.0` failed in one second, before any step executed:

```
Tag "v0.1.0" is not allowed to deploy to github-pages due to environment protection rules.
The deployment was rejected or didn't satisfy other protection rules.
```

**Cause.** Enabling GitHub Pages with "GitHub Actions" as the source creates a `github-pages`
environment whose deployment branch policy allows the default branch only. `package.yml` runs on
`push: tags: ['v*']`, so its deployment was rejected.

**Fix applied.** A deployment branch policy of type `tag`, pattern `v*`, added alongside the
existing `branch: main`. The re-run then completed every step green.

No test could have caught this. `ReleaseWorkflowTests` asserts the workflow's shape, and the
workflow's shape was correct; the blocker was in repository settings, which the suite cannot see.
It is recorded here because a fork, or a restored repository, will hit it again.

## Finding 3 — the user-facing page needs a two-plugin chain

**Severity: documentation only. The menu entry works once the chain is complete.**

The New Releases view reaches the web client's menu through Plugin Pages, and on Jellyfin 12 that
needs **two** third-party plugins, not one:

```
New Releases  ->  Plugin Pages 3.0.0.0+  ->  File Transformation
```

Plugin Pages serves its browser script at `/PluginPages/inject.js`, but nothing references it from
`index.html` on its own. **File Transformation is what patches the client to add the script tag**
(`[FileTransformation] Registering transformation for 'index.html'`). Without it the script is
served but never loaded, and no menu entry can appear. Neither Plugin Pages' catalogue description
nor its assembly metadata mentions this; it was found by reading the served HTML.

With both installed, the whole path works and was driven end to end in a signed-in browser:

- `GET /PluginPages/User` returns this plugin's entry with exactly the payload `data-model.md`
  fixes — `Id`, `Url`, `DisplayText`, `Icon`, and none of the three `IsEnabled*` fields.
- Plugin Pages injects into the **user avatar menu** (`div#app-user-menu`), after the
  `[href='#/mypreferencesmenu']` anchor — its Jellyfin 12 layout path, not the legacy drawer.
- Clicking the entry navigates to `#/userpluginsettings.html?pageUrl=/Plugins/NewReleases/UserView`
  and renders `user-view.html`: both tabs, every filter, and the "No data yet" empty state.

**This closes `A6` end to end**, the one acceptance behaviour no test could reach.

### A correction, recorded because the first version of this document was wrong

This finding first claimed Plugin Pages 3.0.1.0 was broken on Jellyfin 12 and that the entry could
never render, on the reasoning that its `inject.js` initialises only when it finds
`.mainDrawer-scrollContainer`, which appeared absent. **That was measured on the login page.** In a
signed-in client the element is present, the gate passes, and Plugin Pages takes its new-layout
branch correctly. Two intermediate hypotheses were also wrong and are recorded so the method is
visible: that `window.PluginPages` being undefined indicated failure (it is declared `const`, so it
is never a global), and that jQuery was absent from the 12 client (it is bundled and `window.$` is
a function).

The lesson is the same one the test suite keeps teaching in this project: a negative observation
taken in the wrong context is worse than no observation, because it is acted upon. No issue was
filed upstream; there was no upstream bug.

## Finding 4 — both embedded pages are blank on Jellyfin 12: the API's JSON casing changed

**Severity: high, user-visible, and the reason to hold 1.0.0. This is a defect in this plugin.**

With 812 releases stored and the API returning them correctly, **the New Releases view shows
"No data yet. New Releases is waiting for its first refresh." and the administrator page shows a
dash for every status value.** Neither page can display anything on Jellyfin 12.

**Cause.** The two embedded pages read the API response in camelCase; the Jellyfin 12 host
serialises it in PascalCase.

| The pages read | Jellyfin 12 returns |
| --- | --- |
| `data.items` | `Items` |
| `data.hasStoredReleases` | `HasStoredReleases` |
| `data.releasesLastCheckedAt` | `ReleasesLastCheckedAt` |
| `data.refreshIntervalHours` | `RefreshIntervalHours` |
| `.sources`, `.lastRun`, `.libraryArtists` | `Sources`, `LastRun`, `LibraryArtists` |

`user-view.html` renders the empty state from `if (!data.hasStoredReleases && !archived)`. That
property is `undefined` on Jellyfin 12, so the branch is always taken no matter how much data
exists. `admin.html` fails the same way for every status field.

The plugin never configures a naming policy for its API; it relies on the host's serializer. The
pages were written against Jellyfin 10.11, where that produced camelCase. That the casing differed
on 10.11 is inferred from the pages having been written that way and having worked, not measured
against a 10.11 server.

**Confirmed against the running server, not deduced.** The page's own request was traced in the
browser: `HTTP 200 /Plugins/NewReleases/api/releases`, a full `Items` array in the response, and
the empty state rendered anyway. The administrator page was checked the same way.

**Why no test caught it, which is the part worth acting on.** The gap is exact and was documented
as a limitation all along:

- The C# tests assert the *DTO shape* the controllers return — a C# object graph, before the host
  serialises it. They never see the JSON the browser receives.
- The page tests under `tests/web/` cover only pure helpers. `002`'s test list says so explicitly:
  "Anything that reads or writes elements (`row`, `render`, `refreshStatus`, `read`, `fill`,
  `query`) needs a simulated browser this project does not have; those stay manual."

`render` is exactly the function that reads `data.hasStoredReleases`. The one function that
consumes the response is the one function deliberately left untested, and the retarget changed the
response's shape underneath it. `003`'s `FR-002` — "every behaviour specified in `001` and `002`
MUST hold on Jellyfin 12, unchanged" — is not met, and the suite reports green.

**Not fixed here.** Per `spec.md` a real-server finding becomes its own specification. Seeded in
`specs/005-page-json-casing/`.

## Three facts about Jellyfin 12 worth keeping

1. **The legacy API authentication headers are gone.** On 12.1, both of these return **401**:
   - `X-Emby-Token: <key>`
   - `?api_key=<key>`

   Only the full authorization header works:
   `Authorization: MediaBrowser Token="<key>", Client="…", Device="…", DeviceId="…", Version="…"`.
   Any script or document that still uses the short forms needs updating.

2. **Upgrading to 12 removes third-party plugins but keeps their configuration.** This server had
   `Jellyfin.Plugin.PluginPages.xml` and `Jellyfin.Plugin.ConcertRadar.xml` under
   `plugins/configurations/` with no corresponding plugin directories — the plugins were removed
   during the upgrade, as Jellyfin's own upgrade guidance instructs, and their settings survived.
   A reinstall therefore picks up the old configuration.

3. **The web client is a different application, but it still carries jQuery.** Jellyfin 12
   replaced the client with React and MUI (`#reactRoot`, `@mui/material`, `@tanstack/react-query`)
   — and still bundles jQuery and exposes `window.$`, which is how Plugin Pages' script continues
   to work. The signed-in DOM also still provides `.mainDrawer-scrollContainer`. Probing the
   **login page** shows none of the signed-in structure and will mislead anyone who measures there.

4. **The API's JSON casing changed.** A plugin controller's response reaches the browser in
   PascalCase on 12 where 10.11 produced camelCase. Any embedded page that reads response fields
   by name needs checking; nothing in a C# test will reveal it, because the change happens in the
   host's serializer after the controller returns.

5. **First-party plugins track the server version.** TMDb, OMDb, MusicBrainz, Studio Images and
   AudioDB all report `12.1.0.0` on a 12.1 server, while third-party ones keep their own numbering
   (AniList 15.0.0.0, Open Subtitles 25.0.0.0, TVmaze 14.0.0.0).

## Cosmetic issue, not worth its own finding

The catalogue entry shows `"changelog": "Initial scaffold."`, because `build.yaml`'s `changelog`
field was never updated while `CHANGELOG.md` gained the real Jellyfin 12 entries. `jprm repo add`
copies that field verbatim into the published manifest, so it is what an operator reads in the
catalogue. Worth correcting before any version that is meant to be installed by someone else.

## State left on the server

The plugin is **installed and running**, ready for the manual review. To undo it completely:

1. Dashboard → Plugins → New Releases → uninstall, or
   `rm -rf "/var/lib/jellyfin/plugins/Jellyfin New Releases_0.1.0.0"`
2. Remove the "Jellyfin New Releases" entry from Dashboard → Plugins → Repositories.
3. `sudo cp /etc/jellyfin/system.xml.pre-newreleases /etc/jellyfin/system.xml` restores the
   repository list as it was before this pass.
4. `sudo systemctl restart jellyfin`.
5. The plugin's own data lives at `<data>/newreleases/newreleases.db`; delete that directory to
   remove it. Nothing else on the server was touched.

The API key created for this pass should be revoked in Dashboard → API Keys when the review is
finished.
