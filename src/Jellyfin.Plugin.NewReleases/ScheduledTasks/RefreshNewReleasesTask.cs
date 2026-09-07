using Jellyfin.Plugin.NewReleases.Configuration;
using Jellyfin.Plugin.NewReleases.Library;
using Jellyfin.Plugin.NewReleases.Sources;
using Jellyfin.Plugin.NewReleases.Storage;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.NewReleases.ScheduledTasks;

/// <summary>
/// The Refresh (FR-009): scan the library, sync artists, rotate artists × enabled sources (match, page, upsert, prune on
/// Complete), recompute ownership, record the run. Outline in plan.md "Refresh run outline".
/// </summary>
public sealed class RefreshNewReleasesTask : IScheduledTask
{
    private readonly LibraryScanner _scanner;
    private readonly ArtistRepository _artists;
    private readonly ReleaseRepository _releases;
    private readonly SourceStateRepository _sourceState;
    private readonly SourceHttpClient _http;
    private readonly IReadOnlyList<IReleaseSource> _sources;
    private readonly TimeProvider _clock;
    private readonly Func<PluginConfiguration> _configuration;
    private readonly ILogger<RefreshNewReleasesTask> _logger;

    public RefreshNewReleasesTask(
        LibraryScanner scanner,
        ArtistRepository artists,
        ReleaseRepository releases,
        SourceStateRepository sourceState,
        SourceHttpClient http,
        IEnumerable<IReleaseSource> sources,
        TimeProvider clock,
        ILogger<RefreshNewReleasesTask> logger)
        : this(scanner, artists, releases, sourceState, http, sources, clock, logger, () => Plugin.Instance?.Configuration ?? new PluginConfiguration())
    {
    }

    public RefreshNewReleasesTask(
        LibraryScanner scanner,
        ArtistRepository artists,
        ReleaseRepository releases,
        SourceStateRepository sourceState,
        SourceHttpClient http,
        IEnumerable<IReleaseSource> sources,
        TimeProvider clock,
        ILogger<RefreshNewReleasesTask> logger,
        Func<PluginConfiguration> configuration)
    {
        _scanner = scanner;
        _artists = artists;
        _releases = releases;
        _sourceState = sourceState;
        _http = http;
        _sources = sources.ToList();
        _clock = clock;
        _logger = logger;
        _configuration = configuration;
    }

    public string Name => "Refresh new releases";

    public string Key => "NewReleases.Refresh";

    public string Description => "Reads MusicBrainz and Deezer for releases by your library artists that the library does not contain yet.";

    public string Category => "New Releases";

    /// <summary>Daily at 03:00 server time; the interval is changed in Jellyfin's Scheduled Tasks, not here.</summary>
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() =>
    [
        new TaskTriggerInfo { Type = TaskTriggerInfoType.DailyTrigger, TimeOfDayTicks = TimeSpan.FromHours(3).Ticks },
    ];

    public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken) => throw new NotImplementedException();
}
