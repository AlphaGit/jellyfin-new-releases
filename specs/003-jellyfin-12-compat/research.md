# Phase 0 research: Run on Jellyfin 12

All findings below were verified against published artefacts on 2026-09-12 and 2026-09-13, not
recalled. Where a claim came from a web article it was re-checked against the source code or the
package itself before being written down.

## R1. Jellyfin 12 is published, and the plugin's host surface is unchanged

**Decision**: Retarget to `Jellyfin.Controller` / `Jellyfin.Model` `12.0.0`. Keep every host
integration the plugin already uses.

**Rationale**: `Jellyfin.Controller`, `Jellyfin.Model`, `Jellyfin.Common`, `Jellyfin.Data`,
`Jellyfin.Database.Implementations`, `Jellyfin.Extensions` and `Jellyfin.Naming` all published
`12.0.0` on 2026-09-08, shipping `lib/net10.0` only.

The public type and member surface of the 10.11.11 and 12.0.0 assemblies was dumped with
`System.Reflection.Metadata` and diffed. Every type the plugin binds to exists in 12.0.0 with the
same full name, and none carries `[Obsolete]`:

| Surface | 10.11.11 | 12.0.0 |
| --- | --- | --- |
| `MediaBrowser.Common.Plugins.BasePlugin\`1` | present | unchanged |
| `MediaBrowser.Model.Plugins.IHasWebPages`, `PluginPageInfo` | present | unchanged |
| `MediaBrowser.Controller.Plugins.IPluginServiceRegistrator` | present | unchanged |
| `MediaBrowser.Controller.Library.ILibraryManager` | present | additive only |
| `MediaBrowser.Controller.Entities.InternalItemsQuery` | present | additive only |
| `MediaBrowser.Controller.Library.IUserManager` | present | identical |
| `MediaBrowser.Model.Tasks.IScheduledTask`, `ITaskManager` | present | identical |
| `MediaBrowser.Common.Api.Policies` | present | identical |
| `MediaBrowser.Common.Configuration.IApplicationPaths` | present | identical |
| `Jellyfin.Data.Enums.BaseItemKind` | present | unchanged |
| `Jellyfin.Database.Implementations.Entities.User` + permission enums | present | identical |

`ILibraryManager.GetItemList(InternalItemsQuery)` has a byte-identical signature in both versions
and is still synchronous. The "`GetItems` is now asynchronous" change reported in release coverage
is on `IItemRepository`, which this plugin does not use.

The breaking changes Jellyfin names for plugin authors — `ISearchEngine` removed,
`IAuthenticationProvider.HasPassword`, parts of `IItemRepository`, some `IUserManager` members, and
alternate versions and playlist contents moving out of the parent item — touch nothing this plugin
calls. The only `IUserManager` member used is `GetUserById`, whose members diff identically.

**Alternatives considered**: a multi-ABI build serving 10.11 and 12 from one source. Rejected by the
specification: the plugin has never been released, so a second package taxes every future release
for nobody.

## R2. `Jellyfin.Data` and `Jellyfin.Database.Implementations` become explicit references

**Decision**: Add both as direct `PackageReference` entries at `12.0.0`, with
`ExcludeAssets=runtime` and `PrivateAssets=all`, matching the existing Jellyfin references.

**Rationale**: the plugin uses `Jellyfin.Data.Enums.BaseItemKind` (library scanning) and
`Jellyfin.Database.Implementations.Entities.User` with `PermissionKind` / `PreferenceKind`
(per-user library access). Today those arrive transitively: the 10.11.11 lock file resolves
`Jellyfin.Data` and `Jellyfin.Database.Implementations` as transitive dependencies of
`Jellyfin.Controller`. The `Jellyfin.Controller` 12.0.0 nuspec depends only on `Jellyfin.Naming`,
`Jellyfin.Common`, `Jellyfin.Model`, `Jellyfin.MediaEncoding.Keyframes` and
`Microsoft.Extensions.Configuration.Binder`. Both packages still publish `12.0.0`, so the fix is a
direct reference, not a code change.

**Alternatives considered**: rewriting the code to avoid those types. Rejected — `BaseItemKind` is
the only way to ask the library for artists, albums and tracks, and `User` with its permission
enums is how per-user library access is decided.

## R3. Runtime and toolchain

**Decision**: `net10.0`. Install the SDK with `brew install dotnet`, which is the `dotnet@10` alias
and currently provides SDK `10.0.400`.

**Rationale**: Jellyfin 12.0.0 ships `lib/net10.0` only. .NET 10 is the active LTS channel
(supported to 2028-11-14, latest SDK `10.0.401`). The runtime the plugin targets today leaves
support on 2026-11-10. Homebrew already manages `dotnet@8` and `dotnet@9` on this machine, so
`dotnet@10` installs beside them and nothing else breaks. `~/.zshenv` currently prefers
`dotnet@9`; the project selects the SDK it needs the same way it does today.

**Alternatives considered**: Microsoft's official installer (two package managers on one machine);
CI-only builds (one push and one CI run per red-green cycle, which the TDD loop cannot use).

## R4. Dependency pins

Every pin below is the newest version at least 7 days old, except the Jellyfin packages, which are
adopted under the explicit exception recorded in the specification.

| Package | From | To | Published | Note |
| --- | --- | --- | --- | --- |
| `Jellyfin.Controller` | 10.11.11 | **12.0.0** | 2026-09-08 | recorded exception; no older stable 12.x exists |
| `Jellyfin.Model` | 10.11.11 | **12.0.0** | 2026-09-08 | same |
| `Jellyfin.Data` | transitive | **12.0.0** | 2026-09-08 | now direct, see R2 |
| `Jellyfin.Database.Implementations` | transitive | **12.0.0** | 2026-09-08 | now direct, see R2 |
| `Microsoft.Data.Sqlite` | 9.0.19 | **10.0.11** | 2026-08-11 | matches the runtime line; 10.0.12 is only 5 days old |
| `Microsoft.NET.Test.Sdk` | 17.13.0 | **18.9.0** | 2026-08-14 | 18.10.0 is 4 days old |
| `xunit.runner.visualstudio` | 2.8.2 | **3.1.5** | 2025-09-27 | runs xUnit v1/v2/v3, `build/net8.0` applies to net10 |
| `xunit` | 2.9.3 | **2.9.3** | 2025-01-08 | no change needed |
| `NSubstitute` | 5.3.0 | **5.3.0** | — | targets netstandard2.0/net6.0, runs on net10 |

`Microsoft.Data.Sqlite` 10.0.11 targets `netstandard2.0` and depends on the same
`SQLitePCLRaw` 2.1.12 chain the project already resolves, so the native artefact list in
`build.yaml` — `SQLitePCLRaw.*.dll` and `runtimes/linux-x64/native/libe_sqlite3.so` — does not
change.

Both `packages.lock.json` files must be regenerated (`dotnet restore --force-evaluate`).

**Alternatives considered**: migrating to xUnit v3. Rejected as out of scope — the constitution
names xunit and NSubstitute, v2 runs fine on net10, and a framework migration is not a
compatibility change.

## R5. Plugin Pages: move to the supported registration interface

**Decision**: Stop writing `Jellyfin.Plugin.PluginPages/config.json`. Register through
`Jellyfin.Plugin.PluginPages.PluginInterface.RegisterPage`, and withdraw with `RemovePage`, reached
entirely by reflection so the plugin keeps no reference to Plugin Pages.

**Rationale**: Plugin Pages 3.0.0.0 (2026-09-08) ships a Jellyfin 12 build and adds a public API.
Its own source confirms the shape:

```csharp
public static class PluginInterface
{
    public static void RegisterPage(JObject payload);   // Newtonsoft.Json.Linq.JObject
    public static void RemovePage(string id);
}
```

`RegisterPage` deserialises the payload into its `PluginPage` model — `Id`, `Url`, `DisplayText`,
`Icon`, and optional `IsEnabledAssembly` / `IsEnabledClass` / `IsEnabledMethod`. There is no
`Version` field; the version number the plugin writes into `config.json` today is its own
invention for re-seeding and disappears with the file.

The maintainer marks the config-file path **legacy** in code — "at some point I want to remove this
but other plugins are using it so I'll leave it for a few releases" — and the release notes call the
API "the preferred integration path for dependent plugins going forward". That is exactly the case
the project's standing rule covers: take the supported path at migration time rather than twice.

**How, without a dependency**: the payload parameter is a Newtonsoft `JObject`, and Plugin Pages may
not be installed at all. Referencing either assembly would make an optional integration mandatory.
Instead:

1. Find the Plugin Pages assembly by scanning `AssemblyLoadContext.All.SelectMany(c => c.Assemblies)`.
   Jellyfin loads each plugin into its own `AssemblyLoadContext`, so `AppDomain.CurrentDomain.GetAssemblies()`
   is not reliable here. Plugin Pages itself scans `AssemblyLoadContext.All` for the same reason.
2. Resolve `Jellyfin.Plugin.PluginPages.PluginInterface` and its `RegisterPage` method.
3. Take the payload type from `RegisterPage`'s single parameter and call its static
   `Parse(string)` to build the argument from JSON the plugin composes itself. The Newtonsoft type
   is never named in our code.
4. Invoke. Wrap the whole thing so any failure — absent, older, or renamed — logs once and leaves
   the plugin running.

`RegisterPluginPage` ignores a duplicate `Id`, so repeated registration is safe.

**Where to call it**: not the `Plugin` constructor. `PluginInterface` goes through
`PluginPagesPlugin.Instance`, which that plugin sets in its own constructor; construction order
between two plugins is not guaranteed. Register from an `IHostedService` added in
`PluginServiceRegistrator`. `PluginManager.RegisterServices` contributes to the same
`IServiceCollection` before the container is built ("Note: DI is not yet instantiated yet"), so a
plugin-registered hosted service is started by the generic host, after all plugin objects exist.
`StartAsync` registers, `StopAsync` withdraws — which is precisely what FR-015 asks for.

**Alternatives considered**:
- Keep writing `config.json`. Works today and the maintainer still reads it, but it is explicitly
  legacy and reaches into another plugin's stored state.
- Register from a startup-triggered `IScheduledTask`, which is what Plugin Pages does for its own
  work. Rejected: it puts a second entry in Dashboard → Scheduled Tasks that has nothing to do with
  refreshing releases, and gives no shutdown hook.
- Resolve Plugin Pages' `IPluginPagesManager` out of the DI container by reflection. Rejected: it
  bypasses the public API the vendor asked plugins to use.

## R6. Publishing: packages and repository on GitHub Pages

**Decision**: A `repo/` directory in the repository holds `manifest.json` and the published zips.
The release workflow builds the package with JPRM, adds it to the manifest with `jprm repo add`,
commits `repo/` back to `main`, and deploys that directory to GitHub Pages.

**Rationale**: JPRM 1.1.0 already does the manifest work. `jprm repo add` produces exactly the
entry FR-013 requires:

```json
{ "version": "…", "changelog": "…", "targetAbi": "12.0.0.0",
  "sourceUrl": "{repo_url}/{slug}/{slug}_{version}.zip",
  "checksum": "<md5>", "timestamp": "…" }
```

and `update_plugin_manifest` merges new versions into an existing manifest rather than replacing
it. `targetAbi` is copied through verbatim with no validation, so `12.0.0.0` needs nothing special.
`framework` drives `dotnet publish --framework=` **and rewrites `<TargetFramework>` in the
csproj during the build**, so `build.yaml` must say `net10.0` and the project must contain exactly
one `<TargetFramework>` element — it does.

Keeping the published site in git is what makes FR-013's "every published version" true: an
artifact-only Pages deployment replaces the whole site each run, which would drop older zips. A
committed directory keeps them for free and needs no third-party action.

GitHub Pages cannot be served from an arbitrary branch directory — only the repository root or
`/docs` — and `docs/` already holds `domain_knowledge/`. So the deployment is the Actions path:
`actions/configure-pages@v6`, `actions/upload-pages-artifact@v5` pointed at `repo/`, and
`actions/deploy-pages@v5`. All three are at least 7 days old (v6.0.0 2026-03-25, v5.0.0 2026-04-10,
v5.0.1 2026-09-01). `actions/setup-dotnet@v6` gains `dotnet-version: '10.0.x'`.

JPRM 1.1.0 itself was published 2024-03-29 and is already pinned in `package.yml`.

**Alternatives considered**: a `gh-pages` branch pushed by a third-party action (another dependency
for the same result); moving `docs/domain_knowledge` so Pages could serve `/docs` directly
(reorganising the repository to suit a deployment mechanism).

**Not built**: no version is tagged by this feature. The repository correctly serves a manifest
with an empty `versions` list until the maintainer cuts a release.

## R7. The web pages need no change

**Decision**: Leave `admin.html` and `user-view.html` alone.

**Rationale**: checked against `jellyfin-web` at tag `v12.0`. `src/utils/dashboard.js` still ends
with `window.Dashboard = Dashboard;` and still exports every function the pages call —
`processPluginConfigurationUpdateResult`, `alert`, `confirm`, `showLoadingMsg`, `hideLoadingMsg` —
plus `getPluginUrl` and `getConfigurationResourceUrl`. `window.ApiClient` is still installed, and
`getPluginConfiguration`, `updatePluginConfiguration`, `serverId`, `ajax` and `getUrl` all still
exist. The page-injection rework in Jellyfin 12 is inside Plugin Pages, which ships a 12 build.

The Node page tests under `tests/web/` use the standard library only and are unaffected by the
retarget.

## R8. Not adopted

- **Search providers** (`ISearchProvider`, `IInternalSearchProvider`, `IExternalSearchProvider`,
  `ISearchManager`), new in Jellyfin 12 replacing the removed `ISearchEngine`. Contributing release
  results to Jellyfin's search is a new user-visible capability, which the specification freezes out.
- **`IHasEmbeddedImage`**, also new. Its own documentation restricts it: "This interface is intended
  for plugins compiled into the server. External plugins should continue to declare their image via
  the `imagePath` field in `meta.json`." A catalogue icon stays Outstanding by the maintainer's
  choice; JPRM supports it through `image` / `imageUrl` in `build.yaml` when it is wanted.
- **Similar-items providers**, unrelated to this plugin.

## R9. Open risk

The constitution pins the project to Jellyfin 10.11.x and `net9.0`. This feature contradicts both.
Its own Governance section says it is reviewed when the Jellyfin target major version changes, so
the amendment is due — see Complexity Tracking in `plan.md`. It must go through
`/speckit-constitution`, not an edit from this feature.
