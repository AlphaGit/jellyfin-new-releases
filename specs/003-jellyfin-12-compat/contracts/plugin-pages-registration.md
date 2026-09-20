# Contract: Plugin Pages registration

Satisfies `FR-008`, `FR-015`, `FR-016` and User Story 1 scenario 6.

The plugin's user-facing view reaches the web client's menu through Plugin Pages, a separate
third-party plugin that may or may not be installed. This contract fixes what the plugin calls, what
it sends, when it calls it, and what must happen when the call cannot be made.

## The far side

Provided by Plugin Pages 3.0.0.0 and later:

```csharp
namespace Jellyfin.Plugin.PluginPages;

public static class PluginInterface
{
    public static void RegisterPage(JObject payload);  // Newtonsoft.Json.Linq.JObject
    public static void RemovePage(string id);
}
```

`RegisterPage` deserialises the payload into Plugin Pages' own model:

```csharp
public class PluginPage
{
    public string? Id { get; set; }
    public string? Url { get; set; }
    public string? DisplayText { get; set; }
    public string? Icon { get; set; }
    public string? IsEnabledAssembly { get; set; }
    public string? IsEnabledClass { get; set; }
    public string? IsEnabledMethod { get; set; }
}
```

Registrations are held in memory by Plugin Pages and rebuilt on every server start. A registration
whose `Id` is already present is ignored, so repeating the call is harmless.

## The payload

```json
{
  "Id": "Jellyfin.Plugin.NewReleases",
  "Url": "/Plugins/NewReleases/UserView",
  "DisplayText": "New Releases",
  "Icon": "new_releases"
}
```

The three `IsEnabled*` fields are omitted deliberately: the entry is shown to every authenticated
user, and per-user library filtering happens inside the view's own API calls.

`RemovePage` is called with the same `Id` string.

## How the call is made

The plugin **must not** reference the Plugin Pages assembly or Newtonsoft.Json. Both would turn an
optional integration into a hard requirement, and the plugin must load on a server where Plugin
Pages is absent. The call is therefore made by reflection:

1. **Find the assembly.** Scan `AssemblyLoadContext.All.SelectMany(c => c.Assemblies)` for
   `Jellyfin.Plugin.PluginPages`. Jellyfin loads each plugin into its own `AssemblyLoadContext`, so
   `AppDomain.CurrentDomain.GetAssemblies()` will not reliably see it. Plugin Pages scans
   `AssemblyLoadContext.All` for the same reason.
2. **Resolve the entry point.** Get type `Jellyfin.Plugin.PluginPages.PluginInterface`, then its
   public static `RegisterPage` and `RemovePage` methods.
3. **Build the argument without naming its type.** Take the single parameter type of `RegisterPage`
   and invoke that type's static `Parse(string)` with the JSON above. Newtonsoft.Json is never
   named in this project's source.
4. **Invoke.**

## When it happens

| Moment | Action |
| --- | --- |
| Host start | `IHostedService.StartAsync` registers the page. |
| Host stop | `IHostedService.StopAsync` withdraws it with `RemovePage`. |

The hosted service is added in `PluginServiceRegistrator`. Jellyfin's `PluginManager.RegisterServices`
contributes to the same `IServiceCollection` before the container is built, so the generic host
starts it — after every plugin object, including Plugin Pages', has been constructed.

Registration **must not** happen in the `Plugin` constructor. `PluginInterface` reaches Plugin Pages
through a static `Instance` set in that plugin's own constructor, and construction order between two
plugins is not guaranteed.

## Required behaviour when the integration is unavailable

Unavailable covers all of: Plugin Pages not installed, installed at a version without
`PluginInterface`, present but not yet initialised, or the call throwing for any other reason.

In every one of those cases:

- The plugin still loads. Nothing here may prevent it.
- The configuration page, the plugin API and the scheduled refresh all still work.
- The failure is logged **at most once** per server run, and not at error level — an absent optional
  integration is not a fault.
- Withdrawal on shutdown is equally guarded; a failed registration must not make shutdown noisy.

## What this replaces

`Plugin.TryRegisterPluginPagesEntry` and `Plugin.IsPluginPagesInstalled` are deleted. The plugin no
longer reads, creates or writes
`<plugin configurations>/Jellyfin.Plugin.PluginPages/config.json`, and no longer probes the plugins
directory for an installed copy.

Plugin Pages still reads that file — its maintainer calls it legacy and keeps it "for a few
releases" — so nothing breaks for an operator upgrading from an older build of this plugin. There is
no such operator in any case: the plugin has never been released. A stale entry left behind by a
development install is not cleaned up, because Plugin Pages ignores a duplicate `Id` and the
in-memory registration wins on identity.

## Testable statements

1. Given a stand-in exposing `RegisterPage(<payload type>)` and `RemovePage(string)`, when the
   hosted service starts, then `RegisterPage` is called exactly once with a payload whose `Id`,
   `Url`, `DisplayText` and `Icon` match the values above.
2. Given the same stand-in, when the hosted service stops, then `RemovePage` is called exactly once
   with `Jellyfin.Plugin.NewReleases`.
3. Given no Plugin Pages assembly is loaded, when the hosted service starts and stops, then neither
   throws, and one message is logged at start.
4. Given a stand-in whose `RegisterPage` throws, when the hosted service starts, then it does not
   throw and one message is logged.
5. Given the hosted service starts twice in one process, then no more than one message is logged.
6. The plugin project references neither `Jellyfin.Plugin.PluginPages` nor `Newtonsoft.Json`.
