using System.Text.Json;
using System.Reflection;
using Jellyfin.Plugin.NewReleases.Integration;
using Jellyfin.Plugin.NewReleases.Tests.Support;
using Microsoft.Extensions.Logging;
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

    /// <summary>
    /// U9: the four fields are what the web client renders. The three IsEnabled* fields are
    /// omitted on purpose — the entry is shown to every authenticated user, and per-user library
    /// filtering happens inside the view's own API calls.
    /// </summary>
    [Fact]
    public async Task StartAsync_SendsThePageEntryFromTheDataModel_AndNoIsEnabledFields()
    {
        var service = ServiceOver(WithPluginPages());

        await service.StartAsync(CancellationToken.None);

        var payload = JsonSerializer.Deserialize<Dictionary<string, string>>(
            Assert.Single(FakePluginPages.Registered).Json);

        Assert.NotNull(payload);
        Assert.Equal("Jellyfin.Plugin.NewReleases", payload["Id"]);
        Assert.Equal("/Plugins/NewReleases/UserView", payload["Url"]);
        Assert.Equal("New Releases", payload["DisplayText"]);
        Assert.Equal("new_releases", payload["Icon"]);
        Assert.Equal(["Id", "Url", "DisplayText", "Icon"], payload.Keys);
    }

    /// <summary>
    /// U10: Plugin Pages holds registrations in memory and keys them by id, so a shutdown that
    /// does not withdraw leaves a menu entry pointing at a plugin that has stopped.
    /// </summary>
    [Fact]
    public async Task StopAsync_WithTheIntegrationPresent_CallsRemovePageOnceWithThePluginId()
    {
        var service = ServiceOver(WithPluginPages());
        await service.StartAsync(CancellationToken.None);

        await service.StopAsync(CancellationToken.None);

        Assert.Equal("Jellyfin.Plugin.NewReleases", Assert.Single(FakePluginPages.Removed));
    }

    /// <summary>
    /// U11: an operator may run Jellyfin 12 without Plugin Pages at all. Nothing here may stop
    /// the plugin loading, so neither host callback may throw.
    /// </summary>
    [Fact]
    public async Task WithNoIntegrationAssembly_StartingAndStoppingBothSucceed()
    {
        var service = ServiceOver([]);

        await service.StartAsync(CancellationToken.None);
        await service.StopAsync(CancellationToken.None);

        Assert.Empty(FakePluginPages.Registered);
        Assert.Empty(FakePluginPages.Removed);
    }

    /// <summary>
    /// U12: an absent optional integration is not a fault. It is said once, so an operator who
    /// wonders why the menu entry is missing can find out, and below error level so it does not
    /// read as a defect in a log the operator is scanning for real ones.
    /// </summary>
    [Fact]
    public async Task WithNoIntegrationAssembly_LogsExactlyOnce_BelowErrorLevel()
    {
        var logger = new RecordingLogger<PluginPagesRegistrationService>();
        var service = new PluginPagesRegistrationService(new PluginPagesGateway(() => []), logger);

        await service.StartAsync(CancellationToken.None);

        var entry = Assert.Single(logger.Entries);
        Assert.True(entry.Level < LogLevel.Error, $"logged at {entry.Level}, which reads as a fault");
    }

    /// <summary>
    /// U13: Plugin Pages may be installed but not yet initialised, in which case its own static
    /// entry point throws. A hosted service that lets that escape stops the server starting.
    /// </summary>
    [Fact]
    public async Task WhenRegisterPageThrows_StartingDoesNotThrow_AndLogsOnce()
    {
        FakePluginPages.RegisterThrows = new InvalidOperationException("Plugin Pages is not ready");
        var logger = new RecordingLogger<PluginPagesRegistrationService>();
        var service = new PluginPagesRegistrationService(new PluginPagesGateway(WithPluginPages), logger);

        await service.StartAsync(CancellationToken.None);

        Assert.Single(logger.Entries);
        Assert.Empty(FakePluginPages.Registered);
    }

    /// <summary>
    /// U14: the message is worth saying once. Said on every start attempt it becomes noise in
    /// the log of an operator who has simply chosen not to install Plugin Pages.
    /// </summary>
    [Fact]
    public async Task StartingTwiceWithNoIntegration_LogsAtMostOnce()
    {
        var logger = new RecordingLogger<PluginPagesRegistrationService>();
        var service = new PluginPagesRegistrationService(new PluginPagesGateway(() => []), logger);

        await service.StartAsync(CancellationToken.None);
        await service.StartAsync(CancellationToken.None);

        Assert.Single(logger.Entries);
    }

    /// <summary>
    /// U15: a registration that failed must not make shutdown noisy. The operator has already
    /// been told once; repeating it while the server stops adds nothing.
    /// </summary>
    [Fact]
    public async Task AfterAFailedRegistration_StoppingDoesNotThrow_AndLogsNothingFurther()
    {
        var logger = new RecordingLogger<PluginPagesRegistrationService>();
        var service = new PluginPagesRegistrationService(new PluginPagesGateway(() => []), logger);
        await service.StartAsync(CancellationToken.None);

        await service.StopAsync(CancellationToken.None);

        Assert.Single(logger.Entries);
    }
}
