using Jellyfin.Plugin.NewReleases.Api;
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
    private const string PageEntryJson = $$"""
        {
          "Id": "Jellyfin.Plugin.NewReleases",
          "Url": "{{PluginRoutes.UserViewAbsolute}}",
          "DisplayText": "New Releases",
          "Icon": "new_releases"
        }
        """;

    private readonly PluginPagesGateway _gateway;
    private readonly ILogger<PluginPagesRegistrationService> _logger;

    private bool _reportedUnavailable;

    public PluginPagesRegistrationService(PluginPagesGateway gateway, ILogger<PluginPagesRegistrationService> logger)
    {
        _gateway = gateway;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_gateway.TryRegisterPage(PageEntryJson))
        {
            ReportUnavailableOnce();
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _gateway.TryRemovePage(PageEntryId);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Plugin Pages is optional, so its absence is stated once per run and never as an error.
    /// </summary>
    private void ReportUnavailableOnce()
    {
        if (_reportedUnavailable)
        {
            return;
        }

        _reportedUnavailable = true;
        _logger.LogInformation(
            "Plugin Pages is not available, so the New Releases page is not in the menu. Everything else works.");
    }
}
