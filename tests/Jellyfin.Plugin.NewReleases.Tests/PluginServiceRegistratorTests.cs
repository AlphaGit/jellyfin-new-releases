using Jellyfin.Plugin.NewReleases.Library;
using Jellyfin.Plugin.NewReleases.Sources;
using Jellyfin.Plugin.NewReleases.Storage;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.NewReleases.Tests;

/// <summary>
/// Jellyfin builds one container and hands the plugin an <see cref="IServiceCollection"/> to add
/// to. A service that registers but cannot resolve fails at the moment the server first needs it,
/// which is long after the plugin reports itself loaded.
/// </summary>
public class PluginServiceRegistratorTests
{
    /// <summary>
    /// The host services the plugin expects Jellyfin to have registered already, plus the
    /// plugin's own registrations on top. Nothing here is real but the plugin's own types.
    /// </summary>
    private static ServiceProvider BuildContainerAsTheHostWould()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddSingleton(Substitute.For<IApplicationPaths>());
        services.AddSingleton(Substitute.For<ILibraryManager>());

        new PluginServiceRegistrator().RegisterServices(services, Substitute.For<IServerApplicationHost>());

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// A2: resolve every type the registrator adds, from a container that holds only what the
    /// Jellyfin 12 host provides. This is the whole of the plugin's wiring in one assertion.
    /// </summary>
    [Fact]
    public async Task RegisterServices_EveryServiceThePluginRegisters_ResolvesFromTheHostContainer()
    {
        // SourceHttpClient is IAsyncDisposable only, so the container needs DisposeAsync.
        await using var provider = BuildContainerAsTheHostWould();

        Assert.NotNull(provider.GetRequiredService<PluginDatabase>());
        Assert.NotNull(provider.GetRequiredService<ArtistRepository>());
        Assert.NotNull(provider.GetRequiredService<ReleaseRepository>());
        Assert.NotNull(provider.GetRequiredService<ArchiveRepository>());
        Assert.NotNull(provider.GetRequiredService<SourceStateRepository>());
        Assert.NotNull(provider.GetRequiredService<TimeProvider>());
        Assert.NotNull(provider.GetRequiredService<IHttpClientFactory>());
        Assert.NotNull(provider.GetRequiredService<SourceHttpClient>());
        Assert.NotNull(provider.GetRequiredService<LibraryScanner>());
        Assert.NotEmpty(provider.GetRequiredService<IEnumerable<IReleaseSource>>());
        Assert.NotNull(provider.GetRequiredService<IScheduledTask>());
    }
}
