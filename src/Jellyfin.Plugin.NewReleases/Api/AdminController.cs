using Jellyfin.Plugin.NewReleases.Configuration;
using Jellyfin.Plugin.NewReleases.Storage;
using MediaBrowser.Common.Api;
using MediaBrowser.Model.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.NewReleases.Api;

/// <summary>Administrator endpoints (FR-009, FR-012, FR-013): status, Run now, Purge release data, Clear Archive.</summary>
[ApiController]
[Authorize(Policy = Policies.RequiresElevation)]
[Route("Plugins/NewReleases/api/admin")]
public sealed class AdminController : ControllerBase
{
    private readonly ArtistRepository _artists;
    private readonly ReleaseRepository _releases;
    private readonly ArchiveRepository _archive;
    private readonly SourceStateRepository _sourceState;
    private readonly ITaskManager _tasks;
    private readonly TimeProvider _clock;
    private readonly Func<PluginConfiguration> _configuration;
    private readonly ILogger<AdminController> _logger;

    public AdminController(ArtistRepository artists, ReleaseRepository releases, ArchiveRepository archive, SourceStateRepository sourceState, ITaskManager tasks, TimeProvider clock, ILogger<AdminController> logger)
        : this(artists, releases, archive, sourceState, tasks, clock, logger, () => Plugin.Instance?.Configuration ?? new PluginConfiguration())
    {
    }

    public AdminController(ArtistRepository artists, ReleaseRepository releases, ArchiveRepository archive, SourceStateRepository sourceState, ITaskManager tasks, TimeProvider clock, ILogger<AdminController> logger, Func<PluginConfiguration> configuration)
    {
        _artists = artists;
        _releases = releases;
        _archive = archive;
        _sourceState = sourceState;
        _tasks = tasks;
        _clock = clock;
        _logger = logger;
        _configuration = configuration;
    }
}
