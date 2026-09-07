using Jellyfin.Plugin.NewReleases.Library;
using Jellyfin.Plugin.NewReleases.ScheduledTasks;
using Jellyfin.Plugin.NewReleases.Sources;
using Jellyfin.Plugin.NewReleases.Storage;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
        // Storage: one database (lazy one-time migration, R13) and four repositories by aggregate (R14).
        serviceCollection.AddSingleton<PluginDatabase>();
        serviceCollection.AddSingleton<ArtistRepository>();
        serviceCollection.AddSingleton<ReleaseRepository>();
        serviceCollection.AddSingleton<ArchiveRepository>();
        serviceCollection.AddSingleton<SourceStateRepository>();

        // Clock: the host may already provide one; tests substitute it.
        serviceCollection.TryAddSingleton(TimeProvider.System);

        // Outbound HTTP: one named client with a response-size cap, wrapped by the politeness client (FR-011, FR-018).
        serviceCollection.AddHttpClient(SourceHttpClient.ClientName, client => client.MaxResponseContentBufferSize = SourceLimits.MaxResponseBytes);
        serviceCollection.AddSingleton<SourceHttpClient>();

        // Library reader and the two sources (IEnumerable<IReleaseSource> in the task).
        serviceCollection.AddSingleton<LibraryScanner>();
        serviceCollection.AddSingleton<IReleaseSource, MusicBrainzSource>();
        serviceCollection.AddSingleton<IReleaseSource, DeezerSource>();

        // The Refresh; Jellyfin picks up IScheduledTask implementations from the container.
        serviceCollection.AddSingleton<IScheduledTask, RefreshNewReleasesTask>();
    }
}
