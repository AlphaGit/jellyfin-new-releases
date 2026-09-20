using Xunit;

namespace Jellyfin.Plugin.NewReleases.Tests.Support;

/// <summary>
/// Tests that touch state shared by the whole process, and so must not run beside each other or
/// beside anything that reads it. Two such states exist:
/// <list type="bullet">
/// <item>the static <c>Plugin.Instance</c>, which every <c>new Plugin(...)</c> assigns and the
/// convenience constructors of the task, the controllers and the HTTP client read;</item>
/// <item>the static <c>Jellyfin.Plugin.PluginPages.PluginInterface</c> stand-in, which the
/// production gateway can reach because it scans every loaded assembly.</item>
/// </list>
/// The stack profile requires this grouping once a second such test appears.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ProcessGlobalStateCollection
{
    public const string Name = "process-global plugin state";
}
