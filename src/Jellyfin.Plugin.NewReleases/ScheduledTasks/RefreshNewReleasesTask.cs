using Jellyfin.Plugin.NewReleases.Configuration;
using Jellyfin.Plugin.NewReleases.Library;
using Jellyfin.Plugin.NewReleases.Matching;
using Jellyfin.Plugin.NewReleases.Model;
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

    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var configuration = _configuration();
        var enabledSources = _sources.Where(s => IsEnabled(configuration, s.Id)).ToList();

        // 1–2. Scan the library and sync artists (FR-001, FR-014).
        var snapshot = _scanner.Scan();
        var artistIds = new Dictionary<string, long>(StringComparer.Ordinal);
        foreach (var artist in snapshot.Artists)
        {
            artistIds[artist.ArtistKey] = await _artists.UpsertAsync(artist, cancellationToken).ConfigureAwait(false);
        }

        await _artists.DeleteMissingAsync(artistIds.Keys.ToArray(), cancellationToken).ConfigureAwait(false);
        var snapshots = snapshot.Artists.ToDictionary(a => a.ArtistKey, StringComparer.Ordinal);
        var runId = await _sourceState.StartRunAsync(cancellationToken).ConfigureAwait(false);
        var counts = new RunCounts();

        try
        {
            // 3. Rotate artists × enabled sources.
            var rotation = await _artists.GetRotationAsync(cancellationToken).ConfigureAwait(false);
            var placeholders = new HashSet<long>();
            var done = 0;
            foreach (var stored in rotation)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var artist = snapshots[stored.ArtistKey];
                var everySourceAttempted = true;
                var anySourceAttempted = false;

                // 009 FR-001: an artist whose every MBID is a placeholder asks no source anything.
                var placeholder = !string.IsNullOrEmpty(artist.Mbid?.Trim(' ', ';')) && MusicBrainzIds.Parse(artist.Mbid).Count == 0;
                foreach (var source in enabledSources)
                {
                    if (placeholder)
                    {
                        break;
                    }

                    if (!await _http.IsAvailableAsync(source.Id, cancellationToken).ConfigureAwait(false))
                    {
                        everySourceAttempted = false;
                        continue;
                    }

                    anySourceAttempted = true;
                    try
                    {
                        placeholder = await RefreshArtistAtSourceAsync(stored.Id, artist, source, runId, counts, cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        // Failed: record, remove nothing, continue with the next source or artist (FR-014, contract failure semantics).
                        counts.Errors++;
                        _logger.LogWarning(ex, "Source {Source} failed for {Artist}.", source.Id, artist.Name);
                        var state = await _artists.GetSourceStateAsync(stored.Id, source.Id, cancellationToken).ConfigureAwait(false);
                        await _artists.SetFetchOutcomeAsync(stored.Id, source.Id, FetchOutcome.Failed, state?.ResumeOffset ?? 0, ex.Message, _clock.GetUtcNow(), cancellationToken).ConfigureAwait(false);
                    }
                }

                if (placeholder)
                {
                    // 009 FR-003: a placeholder at any source is one at every source; every source counts as attempted.
                    placeholders.Add(stored.Id);
                    everySourceAttempted = true;
                    foreach (var source in enabledSources)
                    {
                        await _artists.SetMatchAsync(stored.Id, source.Id, ArtistMatch.Unmatched(MatchReasons.Placeholder), cancellationToken).ConfigureAwait(false);
                    }
                }

                if (anySourceAttempted)
                {
                    counts.ArtistsProcessed++;
                }

                if (everySourceAttempted)
                {
                    await _artists.SetLastRefreshedAsync(stored.Id, _clock.GetUtcNow(), cancellationToken).ConfigureAwait(false);
                }

                progress.Report(100.0 * ++done / Math.Max(1, rotation.Count));
            }

            // 4. Ownership for every artist, from stored editions plus the fresh snapshot; editions fetched only for candidates (R11, EC-7).
            var credited = CreditIndex(snapshot.Artists);
            foreach (var stored in rotation.Where(s => !placeholders.Contains(s.Id)))
            {
                cancellationToken.ThrowIfCancellationRequested();
                await DecideOwnershipAsync(stored.Id, snapshots[stored.ArtistKey], credited, enabledSources, counts, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            await FinishAsync(runId, counts, "Cancelled").ConfigureAwait(false);
            throw;
        }
        catch (Exception)
        {
            await FinishAsync(runId, counts, "Failed").ConfigureAwait(false);
            throw;
        }

        // 5. Record the run (FR-012).
        await FinishAsync(runId, counts, "Completed", cancellationToken).ConfigureAwait(false);
    }

    private Task FinishAsync(long runId, RunCounts counts, string outcome, CancellationToken ct = default)
        => _sourceState.FinishRunAsync(runId, counts.ArtistsProcessed, counts.ReleasesFound, counts.EditionsFetched, counts.Errors, outcome, ct);

    private sealed class RunCounts
    {
        public int ArtistsProcessed { get; set; }

        public int ReleasesFound { get; set; }

        public int EditionsFetched { get; set; }

        public int Errors { get; set; }
    }

    /// <summary>
    /// 009 FR-005a, R6: the library artists a credit entry names: by MBID where both sides have one, otherwise by
    /// normalized name. Built once per run.
    /// </summary>
    private static Func<CreditedArtist, IEnumerable<LibraryArtistSnapshot>> CreditIndex(IReadOnlyList<LibraryArtistSnapshot> library)
    {
        var byMbid = library.SelectMany(a => MusicBrainzIds.Parse(a.Mbid).Select(id => (Id: id, Artist: a))).ToLookup(x => x.Id, x => x.Artist, StringComparer.OrdinalIgnoreCase);
        var byName = library.ToLookup(a => TitleNormalizer.NormalizeName(a.Name), StringComparer.Ordinal);
        return credit => credit.Mbid is null
            ? byName[TitleNormalizer.NormalizeName(credit.Name)]
            : byMbid[credit.Mbid].Concat(byName[TitleNormalizer.NormalizeName(credit.Name)].Where(a => MusicBrainzIds.Parse(a.Mbid).Count == 0));
    }

    /// <summary>The found-for artist's albums first, so the matcher keeps its preference, then those of every other credited library artist.</summary>
    private static IReadOnlyList<LibraryAlbumSnapshot> CandidateAlbums(Release release, LibraryArtistSnapshot artist, Func<CreditedArtist, IEnumerable<LibraryArtistSnapshot>> credited)
        => release.Credit.Count == 0
            ? artist.Albums
            : [.. artist.Albums, .. release.Credit.SelectMany(credited).Where(a => a.ArtistKey != artist.ArtistKey).DistinctBy(a => a.ArtistKey).SelectMany(a => a.Albums)];

    private async Task DecideOwnershipAsync(long artistId, LibraryArtistSnapshot artist, Func<CreditedArtist, IEnumerable<LibraryArtistSnapshot>> credited, IReadOnlyList<IReleaseSource> enabledSources, RunCounts counts, CancellationToken ct)
    {
        foreach (var release in await _releases.GetByArtistAsync(artistId, ct).ConfigureAwait(false))
        {
            var albums = CandidateAlbums(release, artist, credited);
            var editions = await _releases.GetEditionsAsync(release.Id, ct).ConfigureAwait(false);
            var result = OwnershipMatcher.Decide(release, editions, albums);
            if (result.NeedsEditions)
            {
                foreach (var (sourceId, sourceReleaseId) in await _releases.GetSourceEntriesAsync(release.Id, ct).ConfigureAwait(false))
                {
                    var source = enabledSources.FirstOrDefault(s => s.Id == sourceId);
                    if (source is null || !await _http.IsAvailableAsync(sourceId, ct).ConfigureAwait(false))
                    {
                        continue;
                    }

                    try
                    {
                        foreach (var edition in await source.FetchEditionsAsync(sourceReleaseId, ct).ConfigureAwait(false))
                        {
                            await _releases.UpsertEditionAsync(release.Id, sourceId, edition, _clock.GetUtcNow(), ct).ConfigureAwait(false);
                            counts.EditionsFetched++;
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _logger.LogWarning(ex, "Source {Source}: edition fetch failed for release {Release}.", sourceId, release.Title);
                    }
                }

                editions = await _releases.GetEditionsAsync(release.Id, ct).ConfigureAwait(false);
                result = OwnershipMatcher.Decide(release, editions, albums);
            }

            if (!result.NeedsEditions)
            {
                await _releases.WriteOwnershipAsync(release.Id, result, _clock.GetUtcNow(), ct).ConfigureAwait(false);
            }
        }
    }

    /// <summary>Refreshes one artist at one source. True when the match is that source's placeholder (009 R2): nothing is fetched or stored.</summary>
    private async Task<bool> RefreshArtistAtSourceAsync(long artistId, LibraryArtistSnapshot artist, IReleaseSource source, long runId, RunCounts counts, CancellationToken ct)
    {
        var state = await _artists.GetSourceStateAsync(artistId, source.Id, ct).ConfigureAwait(false);
        var match = state is { Status: MatchStatus.Matched, SourceArtistId: not null }
            ? ArtistMatch.Matched(state.SourceArtistId)
            : await source.MatchArtistAsync(artist, ct).ConfigureAwait(false);
        if (match.Status == MatchStatus.Matched && PlaceholderArtists.IsPlaceholder(source.Id, match.SourceArtistId))
        {
            return true;
        }

        await _artists.SetMatchAsync(artistId, source.Id, match, ct).ConfigureAwait(false);
        if (match.Status != MatchStatus.Matched)
        {
            return false;
        }

        var offset = state?.ResumeOffset ?? 0;
        var passStartedIn = await _artists.BeginPassAsync(artistId, source.Id, runId, ct).ConfigureAwait(false);
        for (int? next = offset; next is not null;)
        {
            CataloguePage page;
            try
            {
                page = await source.FetchCataloguePageAsync(match.SourceArtistId!, next.Value, ct).ConfigureAwait(false);
            }
            catch (DailyBudgetExhaustedException)
            {
                // Partial: keep the offset for the next run and remove nothing (FR-014, edge cases 5 and 8).
                _logger.LogInformation("Source {Source}: budget exhausted at offset {Offset} for {Artist}; resuming next run.", source.Id, next, artist.Name);
                await _artists.SetFetchOutcomeAsync(artistId, source.Id, FetchOutcome.Partial, next.Value, null, _clock.GetUtcNow(), ct).ConfigureAwait(false);
                return false;
            }

            foreach (var item in page.Items)
            {
                await _releases.UpsertFromSourceAsync(artistId, source.Id, item, runId, _clock.GetUtcNow(), ct).ConfigureAwait(false);
                counts.ReleasesFound++;
            }

            next = page.NextOffset;
        }

        // Complete: only now may this source's entries be dropped, and only those not seen since the paging pass began (FR-014).
        await _releases.PruneEntriesAsync(artistId, source.Id, passStartedIn, ct).ConfigureAwait(false);
        await _artists.SetFetchOutcomeAsync(artistId, source.Id, FetchOutcome.Complete, 0, null, _clock.GetUtcNow(), ct).ConfigureAwait(false);
        return false;
    }

    private static bool IsEnabled(PluginConfiguration configuration, string sourceId) => sourceId switch
    {
        SourceLimits.MusicBrainz => configuration.MusicBrainzEnabled,
        SourceLimits.Deezer => configuration.DeezerEnabled,
        _ => false,
    };
}
