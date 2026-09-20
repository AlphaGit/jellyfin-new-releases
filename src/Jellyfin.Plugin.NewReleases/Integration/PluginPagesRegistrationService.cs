using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.NewReleases.Integration;

/// <summary>
/// Registers the New Releases page with Plugin Pages at host start and withdraws it at stop.
/// </summary>
public sealed class PluginPagesRegistrationService : IHostedService
{
    /// <summary>The entry's identity. `RemovePage` takes this string.</summary>
    private const string PageEntryId = "Jellyfin.Plugin.NewReleases";

    /// <summary>The page entry, exactly as `data-model.md` fixes it.</summary>
    private const string PageEntryJson = """
        {
          "Id": "Jellyfin.Plugin.NewReleases",
          "Url": "/Plugins/NewReleases/UserView",
          "DisplayText": "New Releases",
          "Icon": "new_releases"
        }
        """;

    private readonly PluginPagesGateway _gateway;
    private readonly ILogger<PluginPagesRegistrationService> _logger;

    public PluginPagesRegistrationService(PluginPagesGateway gateway, ILogger<PluginPagesRegistrationService> logger)
    {
        _gateway = gateway;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _gateway.TryRegisterPage(PageEntryJson);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _gateway.TryRemovePage(PageEntryId);
        return Task.CompletedTask;
    }
}
