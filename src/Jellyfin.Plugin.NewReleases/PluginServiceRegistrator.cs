using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.NewReleases;

/// <summary>
/// Registers plugin services into the Jellyfin DI container.
/// Jellyfin discovers this class by scanning plugin assemblies for <see cref="IPluginServiceRegistrator"/>.
/// </summary>
public sealed class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        // Services (storage, release sources, scheduled task) are defined by the spec. Nothing to register yet.
    }
}
