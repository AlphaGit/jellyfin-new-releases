using System.Globalization;
using Jellyfin.Data;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Plugin.NewReleases.Configuration;
using Jellyfin.Plugin.NewReleases.Model;
using Jellyfin.Plugin.NewReleases.ScheduledTasks;
using Jellyfin.Plugin.NewReleases.Storage;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.NewReleases.Api;

/// <summary>User-facing endpoints (any authenticated user): the list, the Archive, the artist filter, status, and decisions (FR-007, FR-008, FR-015, FR-016).</summary>
[ApiController]
[Authorize]
[Route("Plugins/NewReleases/api")]
public sealed class ReleasesController : ControllerBase
{
    /// <summary>Claim Jellyfin's authentication handler puts on the principal (`Jellyfin.Api.Constants.InternalClaimTypes.UserId`, not on NuGet — R5).</summary>
    public const string UserIdClaim = "Jellyfin-UserId";

    private readonly ReleaseRepository _releases;
    private readonly ArtistRepository _artists;
    private readonly ArchiveRepository _archive;
    private readonly SourceStateRepository _runs;
    private readonly IUserManager _users;
    private readonly ITaskManager _tasks;
    private readonly TimeProvider _clock;
    private readonly Func<PluginConfiguration> _configuration;
    private readonly ILogger<ReleasesController> _logger;

    public ReleasesController(ReleaseRepository releases, ArtistRepository artists, ArchiveRepository archive, SourceStateRepository runs, IUserManager users, ITaskManager tasks, TimeProvider clock, ILogger<ReleasesController> logger)
        : this(releases, artists, archive, runs, users, tasks, clock, logger, () => Plugin.Instance?.Configuration ?? new PluginConfiguration())
    {
    }

    public ReleasesController(ReleaseRepository releases, ArtistRepository artists, ArchiveRepository archive, SourceStateRepository runs, IUserManager users, ITaskManager tasks, TimeProvider clock, ILogger<ReleasesController> logger, Func<PluginConfiguration> configuration)
    {
        _releases = releases;
        _artists = artists;
        _archive = archive;
        _runs = runs;
        _users = users;
        _tasks = tasks;
        _clock = clock;
        _logger = logger;
        _configuration = configuration;
    }

    [HttpGet("releases")]
    [ProducesResponseType(typeof(ListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ListResponse>> GetReleasesAsync(
        [FromQuery] Guid? artistId = null,
        [FromQuery] string? type = null,
        [FromQuery] string? state = null,
        [FromQuery] string? from = null,
        [FromQuery] string? to = null,
        [FromQuery] bool archived = false,
        CancellationToken cancellationToken = default)
    {
        if (CallerId() is not { } userId)
        {
            return Unauthorized();
        }

        if (AccessOf(userId) is not { } access)
        {
            return Unauthorized();
        }

        var configuration = _configuration();
        var today = DateOnly.FromDateTime(_clock.GetUtcNow().UtcDateTime);
        var filter = new ReleaseFilter(
            configuration.EnabledReleaseTypes(),
            today,
            configuration.ReleasedSinceDate(),
            artistId,
            Enum.TryParse<ReleaseType>(type, ignoreCase: true, out var t) ? t : null,
            Enum.TryParse<ListState>(state, ignoreCase: true, out var st) ? st : null,
            ParseDate(from),
            ParseDate(to),
            archived);

        var rows = await _releases.ListAsync(filter, cancellationToken).ConfigureAwait(false);
        var visible = rows.Where(access.CanSee).Select(ToDto).ToList();
        var lastChecked = await _artists.GetReleasesLastCheckedAtAsync(configuration.EnabledSourceIds(), cancellationToken).ConfigureAwait(false);
        var hasStored = await _releases.HasAnyAsync(cancellationToken).ConfigureAwait(false);
        return new ListResponse(visible, visible.Count, hasStored, lastChecked, RefreshIntervalHours(), today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
    }

    [HttpGet("artists")]
    [ProducesResponseType(typeof(ArtistsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ArtistsResponse>> GetArtistsAsync(CancellationToken cancellationToken = default)
    {
        if (CallerId() is not { } userId || AccessOf(userId) is not { } access)
        {
            return Unauthorized();
        }

        var artists = await _artists.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return new ArtistsResponse(artists.Where(a => access.CanSee(a.LibraryIds)).Select(a => new ArtistDto(a.JellyfinId, a.Name)).ToList());
    }

    [HttpPost("releases/{id:long}/ignore")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<ActionResult> IgnoreAsync(long id, CancellationToken cancellationToken = default) => DecideAsync(id, DecisionKind.Ignore, cancellationToken);

    [HttpPost("releases/{id:long}/have-it")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<ActionResult> HaveItAsync(long id, CancellationToken cancellationToken = default) => DecideAsync(id, DecisionKind.HaveIt, cancellationToken);

    [HttpPost("releases/{id:long}/restore")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<ActionResult> RestoreAsync(long id, CancellationToken cancellationToken = default) => DecideAsync(id, null, cancellationToken);

    /// <summary>Ignore / Have it (upsert) or Restore (delete) on the release's natural key with the caller's id (FR-005b, FR-016). Visibility is checked before writing (FR-007).</summary>
    private async Task<ActionResult> DecideAsync(long id, DecisionKind? kind, CancellationToken ct)
    {
        if (CallerId() is not { } userId || AccessOf(userId) is not { } access)
        {
            return Unauthorized();
        }

        var release = await _releases.GetAsync(id, ct).ConfigureAwait(false);
        var artist = release is null ? null : await _artists.GetByIdAsync(release.LibraryArtistId, ct).ConfigureAwait(false);
        if (release is null || artist is null)
        {
            return NotFound();
        }

        if (!access.CanSee(artist.LibraryIds))
        {
            return Forbid();
        }

        if (kind is { } decision)
        {
            await _archive.SetAsync(artist.ArtistKey, release.NormalizedTitle, decision, userId, _clock.GetUtcNow(), ct).ConfigureAwait(false);
        }
        else
        {
            await _archive.RemoveAsync(artist.ArtistKey, release.NormalizedTitle, ct).ConfigureAwait(false);
        }

        return NoContent();
    }

    /// <summary>Small status for the fragment header (also embedded in the list response; kept for polling).</summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(StatusResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<StatusResponse>> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var lastChecked = await _artists.GetReleasesLastCheckedAtAsync(_configuration().EnabledSourceIds(), cancellationToken).ConfigureAwait(false);
        var hasStored = await _releases.HasAnyAsync(cancellationToken).ConfigureAwait(false);
        var latest = await _runs.GetLatestRunAsync(cancellationToken).ConfigureAwait(false);
        return new StatusResponse(hasStored, lastChecked, RefreshIntervalHours(), latest is { EndedAt: null });
    }

    /// <summary>Hours between refreshes from the task's triggers in Jellyfin (R15); 24 when no trigger is readable.</summary>
    private int RefreshIntervalHours()
    {
        var trigger = _tasks.ScheduledTasks?.FirstOrDefault(w => w.ScheduledTask is RefreshNewReleasesTask)?.Triggers?.FirstOrDefault();
        return trigger?.Type switch
        {
            TaskTriggerInfoType.IntervalTrigger when trigger.IntervalTicks is { } ticks && ticks > 0 => (int)Math.Max(1, Math.Round(TimeSpan.FromTicks(ticks).TotalHours)),
            TaskTriggerInfoType.WeeklyTrigger => 24 * 7,
            _ => 24,
        };
    }

    /// <summary>The libraries the caller may see (FR-007, R4); null when Jellyfin does not know the user.</summary>
    private LibraryAccess? AccessOf(Guid userId)
    {
        var user = _users.GetUserById(userId);
        return user is null ? null : new LibraryAccess(
            user.HasPermission(PermissionKind.EnableAllFolders),
            user.GetPreferenceValues<Guid>(PreferenceKind.EnabledFolders).ToHashSet());
    }

    private sealed record LibraryAccess(bool AllLibraries, ISet<Guid> Libraries)
    {
        public bool CanSee(ListedRelease release) => CanSee(release.ArtistLibraryIds);

        public bool CanSee(IReadOnlyList<Guid> libraryIds) => AllLibraries || libraryIds.Any(Libraries.Contains);
    }

    private static DateOnly? ParseDate(string? text)
        => DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date : null;

    private static ReleaseDto ToDto(ListedRelease r) => new(
        r.Id,
        r.ArtistName,
        r.ArtistJellyfinId,
        r.Title,
        r.Type.ToString(),
        r.Date,
        r.Date switch { null => "None", { Length: 4 } => "Year", { Length: 7 } => "Month", _ => "Day" },
        r.State.ToString(),
        r.State == ListState.Incomplete ? r.MissingTracks : null,
        r.State == ListState.Incomplete && r.ComparedEdition is { } edition ? new ComparedEditionDto(edition.Source, edition.Title) : null,
        r.Sources.Select(s => new SourceLinkDto(s.Source, s.Url)).ToList(),
        r.Archived is { } decision ? new ArchivedDto(decision.Kind.ToString(), decision.DecidedAt) : null);

    /// <summary>The requesting user's id from the `Jellyfin-UserId` claim, or null when the principal carries none (R5).</summary>
    private Guid? CallerId()
        => Guid.TryParse(User.FindFirst(UserIdClaim)?.Value, out var id) ? id : null;
}
