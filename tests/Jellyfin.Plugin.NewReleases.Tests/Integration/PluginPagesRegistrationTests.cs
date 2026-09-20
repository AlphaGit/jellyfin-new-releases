using System.Reflection;
using Jellyfin.Plugin.NewReleases.Integration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using FakePluginPages = Jellyfin.Plugin.PluginPages.PluginInterface;

namespace Jellyfin.Plugin.NewReleases.Tests.Integration;

/// <summary>
/// The page entry reaches the web client's menu through Plugin Pages, which may not be installed.
/// Contract: <c>specs/003-jellyfin-12-compat/contracts/plugin-pages-registration.md</c>.
/// The stand-in is static, like the real <c>PluginInterface</c>, so these tests share it and run
/// in one collection.
/// </summary>
[Collection(nameof(PluginPagesRegistrationTests))]
[CollectionDefinition(nameof(PluginPagesRegistrationTests), DisableParallelization = true)]
public class PluginPagesRegistrationTests
{
    /// <summary>The assemblies a server with Plugin Pages installed would offer the gateway.</summary>
    private static IEnumerable<Assembly> WithPluginPages() => [typeof(FakePluginPages).Assembly];

    private static PluginPagesRegistrationService ServiceOver(IEnumerable<Assembly> assemblies)
        => new(
            new PluginPagesGateway(() => assemblies),
            NullLogger<PluginPagesRegistrationService>.Instance);

    public PluginPagesRegistrationTests() => FakePluginPages.Reset();

    /// <summary>
    /// U8: the entry appears because the host start registers it, once. Registering on every
    /// start of a restarted host is fine; registering twice in one start is not.
    /// </summary>
    [Fact]
    public async Task StartAsync_WithTheIntegrationPresent_CallsRegisterPageExactlyOnce()
    {
        var service = ServiceOver(WithPluginPages());

        await service.StartAsync(CancellationToken.None);

        Assert.Single(FakePluginPages.Registered);
    }
}
