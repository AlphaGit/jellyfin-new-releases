using Jellyfin.Plugin.NewReleases.Configuration;
using Jellyfin.Plugin.NewReleases.ScheduledTasks;
using Jellyfin.Plugin.NewReleases.Sources;
using Jellyfin.Plugin.NewReleases.Storage;
using MediaBrowser.Common.Api;
using MediaBrowser.Model.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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

    /// <summary>Run now (FR-009). Jellyfin refuses to start a task that is already running; report that as 409 instead of queueing.</summary>
    [HttpPost("run-now")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public ActionResult RunNow()
    {
        if (RefreshWorker() is { State: TaskState.Running })
        {
            return Conflict(new { message = "A refresh is already running." });
        }

        _tasks.QueueScheduledTask<RefreshNewReleasesTask>();
        return Accepted();
    }

    [HttpGet("status")]
    [ProducesResponseType(typeof(AdminStatusResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminStatusResponse>> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var configuration = _configuration();
        var now = _clock.GetUtcNow();
        var sources = new List<SourceStatusDto>();
        foreach (var (id, displayName, enabled) in new[] { (SourceLimits.MusicBrainz, "MusicBrainz", configuration.MusicBrainzEnabled), (SourceLimits.Deezer, "Deezer", configuration.DeezerEnabled) })
        {
            var state = await _sourceState.GetAsync(id, cancellationToken).ConfigureAwait(false);
            var health = !enabled ? "Disabled"
                : state?.CooldownUntil > now ? "CoolingDown"
                : state?.ConsecutiveFailures > 0 ? "Failing"
                : "Ok";
            var today = DateOnly.FromDateTime(now.UtcDateTime);
            sources.Add(new SourceStatusDto(
                id,
                displayName,
                enabled,
                health,
                state?.LastError,
                state is not null && state.CallsDay == today ? state.CallsToday : 0,
                SourceLimits.BySource[id].DailyBudget,
                state?.CooldownUntil,
                state?.LastSuccessAt));
        }

        var lastRun = await _sourceState.GetLatestRunAsync(cancellationToken).ConfigureAwait(false);
        var counts = await _artists.GetCountsAsync(cancellationToken).ConfigureAwait(false);
        var worker = RefreshWorker();
        return new AdminStatusResponse(
            sources,
            lastRun is null ? null : new RunDto(lastRun.StartedAt, lastRun.EndedAt, lastRun.Outcome, lastRun.ArtistsProcessed, lastRun.ReleasesFound, lastRun.EditionsFetched, lastRun.Errors),
            NextRunAt(worker, now),
            worker is { State: TaskState.Running },
            counts.LibraryArtists,
            counts.MatchedBySource,
            counts.Unmatched.Select(u => new UnmatchedArtistDto(u.JellyfinId, u.Name, u.Sources.Select(s => new UnmatchedSourceDto(s.Source, s.Reason)).ToList(), UnmatchedHint)).ToList());
    }

    [HttpPost("purge")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    /// <summary>Purge release data (FR-013): releases, entries, editions and ownership go; decisions, artists and match state stay; paging passes restart.</summary>
    public async Task<ActionResult> PurgeAsync(CancellationToken cancellationToken = default)
    {
        await _releases.PurgeAsync(cancellationToken).ConfigureAwait(false);
        await _artists.ResetResumeOffsetsAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogWarning("Release data purged by an administrator.");
        return NoContent();
    }

    [HttpPost("clear-archive")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    /// <summary>Clear Archive (FR-013): every Ignore and Have-it decision; release rows untouched.</summary>
    public async Task<ActionResult> ClearArchiveAsync(CancellationToken cancellationToken = default)
    {
        await _archive.ClearAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogWarning("Archive cleared by an administrator.");
        return NoContent();
    }

    /// <summary>FR-012: the fix is made in Jellyfin, never in the plugin.</summary>
    public const string UnmatchedHint = "Set the MusicBrainz artist ID in Jellyfin's metadata editor or artist.nfo; the plugin picks it up on the next refresh.";

    /// <summary>Next start from the task's first trigger, as Jellyfin fires them (server local time); null when no trigger is set.</summary>
    private static DateTimeOffset? NextRunAt(IScheduledTaskWorker? worker, DateTimeOffset now)
    {
        var trigger = worker?.Triggers?.FirstOrDefault();
        var local = now.ToLocalTime();
        switch (trigger?.Type)
        {
            case TaskTriggerInfoType.DailyTrigger when trigger.TimeOfDayTicks is { } ticks:
                var today = new DateTimeOffset(local.Date, local.Offset) + TimeSpan.FromTicks(ticks);
                return today > local ? today : today.AddDays(1);
            case TaskTriggerInfoType.IntervalTrigger when trigger.IntervalTicks is { } interval:
                return (worker!.LastExecutionResult?.EndTimeUtc is { } end ? new DateTimeOffset(end, TimeSpan.Zero).ToLocalTime() : local) + TimeSpan.FromTicks(interval);
            default:
                return null;
        }
    }

    private IScheduledTaskWorker? RefreshWorker() => _tasks.ScheduledTasks?.FirstOrDefault(w => w.ScheduledTask is RefreshNewReleasesTask);
}
