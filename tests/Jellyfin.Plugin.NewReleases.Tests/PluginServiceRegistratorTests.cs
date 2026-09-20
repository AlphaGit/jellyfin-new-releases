using Jellyfin.Plugin.NewReleases.Integration;
using Jellyfin.Plugin.NewReleases.Library;
using Jellyfin.Plugin.NewReleases.ScheduledTasks;
using Jellyfin.Plugin.NewReleases.Sources;
using Jellyfin.Plugin.NewReleases.Storage;
using Jellyfin.Plugin.NewReleases.Tests.Support;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.NewReleases.Tests;

/// <summary>
/// Jellyfin builds one container and hands the plugin an <see cref="IServiceCollection"/> to add
/// to. A service that registers but cannot resolve fails at the moment the server first needs it,
/// which is long after the plugin reports itself loaded.
/// </summary>
[Collection(ProcessGlobalStateCollection.Name)]
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

        // GetRequiredService throws, naming the type, when a registration is missing — the call
        // is the assertion. Wrapping each in NotNull only gave the test eleven reasons to fail.
        foreach (var service in new[]
                 {
                     typeof(PluginDatabase), typeof(ArtistRepository), typeof(ReleaseRepository),
                     typeof(ArchiveRepository), typeof(SourceStateRepository), typeof(TimeProvider),
                     typeof(IHttpClientFactory), typeof(SourceHttpClient), typeof(LibraryScanner),
                     typeof(IScheduledTask),
                 })
        {
            provider.GetRequiredService(service);
        }

        Assert.NotEmpty(provider.GetRequiredService<IEnumerable<IReleaseSource>>());
        Assert.Same(TimeProvider.System, provider.GetRequiredService<TimeProvider>());
    }

    /// <summary>
    /// U5: the refresh takes <c>IEnumerable&lt;IReleaseSource&gt;</c>. Registering one source
    /// twice, or dropping one, still satisfies A2's NotEmpty but halves what the refresh reads.
    /// </summary>
    [Fact]
    public async Task RegisterServices_BothReleaseSourcesAreRegistered_NotOneOfThemTwice()
    {
        await using var provider = BuildContainerAsTheHostWould();

        var sources = provider.GetRequiredService<IEnumerable<IReleaseSource>>();

        Assert.Collection(
            sources,
            source => Assert.IsType<MusicBrainzSource>(source),
            source => Assert.IsType<DeezerSource>(source));
    }

    /// <summary>
    /// U6: Jellyfin discovers scheduled tasks by resolving <see cref="IScheduledTask"/> from the
    /// container. Registered as the wrong type, the refresh never appears in the dashboard.
    /// </summary>
    [Fact]
    public async Task RegisterServices_TheScheduledTaskResolvesAsTheRefreshTask()
    {
        await using var provider = BuildContainerAsTheHostWould();

        Assert.IsType<RefreshNewReleasesTask>(provider.GetRequiredService<IScheduledTask>());
    }

    /// <summary>
    /// U7: the page registration runs as a hosted service so it happens after every plugin
    /// object exists, which the plugin constructor cannot guarantee. Unregistered, the page
    /// never reaches the menu and nothing says so.
    /// </summary>
    [Fact]
    public async Task RegisterServices_ThePageRegistrationRunsAsAHostedService()
    {
        await using var provider = BuildContainerAsTheHostWould();

        Assert.Contains(
            provider.GetRequiredService<IEnumerable<IHostedService>>(),
            service => service is PluginPagesRegistrationService);
    }
}
