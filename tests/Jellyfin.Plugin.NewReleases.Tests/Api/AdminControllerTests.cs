using Jellyfin.Plugin.NewReleases.Api;
using Jellyfin.Plugin.NewReleases.Configuration;
using Jellyfin.Plugin.NewReleases.Library;
using Jellyfin.Plugin.NewReleases.Model;
using Jellyfin.Plugin.NewReleases.ScheduledTasks;
using Jellyfin.Plugin.NewReleases.Sources;
using Microsoft.AspNetCore.Mvc;
using Jellyfin.Plugin.NewReleases.Tests.Support;
using MediaBrowser.Common.Api;
using MediaBrowser.Model.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.NewReleases.Tests.Api;

public sealed class AdminControllerTests : IAsyncLifetime
{
    private readonly TimeProviderStub _clock = new(new DateTimeOffset(2026, 9, 6, 12, 0, 0, TimeSpan.Zero));
    private readonly PluginConfiguration _configuration = new();
    private readonly ITaskManager _tasks = Substitute.For<ITaskManager>();
    private TestDatabase _db = null!;

    public async Task InitializeAsync() => _db = await TestDatabase.CreateAsync(_clock);

    public async Task DisposeAsync() => await _db.DisposeAsync();

    private AdminController Controller() => new(_db.Artists, _db.Releases, _db.Archive, _db.SourceState, _tasks, _clock, NullLogger<AdminController>.Instance, () => _configuration);

    /// <summary>The pipeline enforces the policy; the test pins the declaration (FR-013: admin-only actions).</summary>
    [Fact]
    public void Controller_RequiresElevation()
    {
        var authorize = typeof(AdminController).GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).Cast<AuthorizeAttribute>();

        Assert.Contains(authorize, a => a.Policy == Policies.RequiresElevation);
    }

    private void RefreshWorkerIs(TaskState state, params TaskTriggerInfo[] triggers)
    {
        var worker = Substitute.For<IScheduledTaskWorker>();
        worker.ScheduledTask.Returns(new RefreshNewReleasesTask(null!, null!, null!, null!, null!, [], null!, null!));
        worker.State.Returns(state);
        worker.Triggers.Returns(triggers);
        _tasks.ScheduledTasks.Returns([worker]);
    }

    [Fact]
    public void RunNow_QueuesTheTaskAndReturns202_Returns409WithoutQueueingWhileRunning()
    {
        RefreshWorkerIs(TaskState.Idle);
        Assert.IsType<AcceptedResult>(Controller().RunNow());
        _tasks.Received(1).QueueScheduledTask<RefreshNewReleasesTask>();

        RefreshWorkerIs(TaskState.Running);
        var conflict = Assert.IsType<ConflictObjectResult>(Controller().RunNow());

        Assert.Equal(409, conflict.StatusCode);
        _tasks.Received(1).QueueScheduledTask<RefreshNewReleasesTask>(); // still exactly one
    }

    private static readonly Guid Library = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private const string Hint = "Set the MusicBrainz artist ID in Jellyfin's metadata editor or artist.nfo; the plugin picks it up on the next refresh.";

    [Fact]
    public async Task Status_ReportsSourceHealthRunTriggerArtistCountsAndUnmatchedArtistsWithHint()
    {
        _configuration.DeezerEnabled = false;
        var blurId = Guid.NewGuid();
        var daft = await _db.Artists.UpsertAsync(new LibraryArtistSnapshot("mbid-1", Guid.NewGuid(), "Daft Punk", "mbid-1", [Library], []), CancellationToken.None);
        var blur = await _db.Artists.UpsertAsync(new LibraryArtistSnapshot("name:blur", blurId, "Blur", null, [Library], []), CancellationToken.None);
        await _db.Artists.SetMatchAsync(daft, "musicbrainz", ArtistMatch.Matched("mbid-1"), CancellationToken.None);
        await _db.Artists.SetMatchAsync(blur, "musicbrainz", ArtistMatch.Unmatched("ambiguous (score 100 vs 97)"), CancellationToken.None);
        for (var i = 0; i < 3; i++)
        {
            await _db.SourceState.RecordCallAsync("musicbrainz", CancellationToken.None);
        }

        for (var i = 0; i < SourceLimits.FailureThreshold; i++)
        {
            await _db.SourceState.RecordFailureAsync("musicbrainz", "503 Service Unavailable", SourceLimits.FailureThreshold, SourceLimits.Cooldown, CancellationToken.None);
        }

        var run = await _db.SourceState.StartRunAsync(CancellationToken.None);
        _clock.Advance(TimeSpan.FromMinutes(10));
        await _db.SourceState.FinishRunAsync(run, 2, 340, 5, 1, "Completed", CancellationToken.None);
        RefreshWorkerIs(TaskState.Idle, new TaskTriggerInfo { Type = TaskTriggerInfoType.DailyTrigger, TimeOfDayTicks = TimeSpan.FromHours(3).Ticks });

        var status = (await Controller().GetStatusAsync(CancellationToken.None)).Value!;

        var musicBrainz = status.Sources.Single(s => s.Id == "musicbrainz");
        Assert.Equal(("MusicBrainz", true, "CoolingDown", "503 Service Unavailable", 3, 10_000), (musicBrainz.DisplayName, musicBrainz.Enabled, musicBrainz.Health, musicBrainz.LastError, musicBrainz.CallsToday, musicBrainz.DailyBudget));
        Assert.Equal(_clock.GetUtcNow() - TimeSpan.FromMinutes(10) + SourceLimits.Cooldown, musicBrainz.CooldownUntil);
        Assert.Equal(("Disabled", 20_000), (status.Sources.Single(s => s.Id == "deezer").Health, status.Sources.Single(s => s.Id == "deezer").DailyBudget));
        Assert.Equal((2, 340, 5, 1, "Completed", _clock.GetUtcNow()), (status.LastRun!.ArtistsProcessed, status.LastRun.ReleasesFound, status.LastRun.EditionsFetched, status.LastRun.Errors, status.LastRun.Outcome, status.LastRun.EndedAt));
        Assert.NotNull(status.NextRunAt);
        Assert.Equal(new TimeSpan(3, 0, 0), status.NextRunAt!.Value.TimeOfDay);
        Assert.False(status.IsRunning);
        Assert.Equal((2, 1), (status.LibraryArtists, status.MatchedArtists["musicbrainz"]));
        var unmatched = Assert.Single(status.Unmatched);
        Assert.Equal((blurId, "Blur", Hint), (unmatched.JellyfinId, unmatched.Name, unmatched.Hint));
        Assert.Equal([("musicbrainz", "ambiguous (score 100 vs 97)")], unmatched.Sources.Select(s => (s.Source, s.Reason)));
    }

    /// <summary>002 FR-009/SC-005: an operator sees the run and the data age side by side, and sees them diverge.</summary>
    [Fact]
    public async Task Status_ReportsTheLastRunAndTheDataAge_WhichDivergeAfterARunThatCompletedNoFetch()
    {
        var fetchedAt = _clock.GetUtcNow();
        var artist = await _db.Artists.UpsertAsync(new LibraryArtistSnapshot("name:daft punk", Guid.NewGuid(), "Daft Punk", null, [Library], []), CancellationToken.None);
        await _db.Artists.SetFetchOutcomeAsync(artist, "musicbrainz", FetchOutcome.Complete, 0, null, fetchedAt, CancellationToken.None);
        // A stored release, because the age is reported only for data that exists (U35).
        await _db.Releases.UpsertFromSourceAsync(artist, "musicbrainz", new CatalogueItem("rg-1", "Discovery", "https://musicbrainz.org/release-group/rg-1", ReleaseType.Album, [], "2001-03-12"), 1, fetchedAt, CancellationToken.None);

        _clock.Advance(TimeSpan.FromHours(6));
        var run = await _db.SourceState.StartRunAsync(CancellationToken.None);
        await _db.SourceState.FinishRunAsync(run, 0, 0, 0, 0, "Completed", CancellationToken.None);
        RefreshWorkerIs(TaskState.Idle);

        var status = (await Controller().GetStatusAsync(CancellationToken.None)).Value!;

        Assert.Equal(_clock.GetUtcNow(), status.LastRun!.EndedAt);
        Assert.Equal(fetchedAt, status.ReleasesLastCheckedAt);
        Assert.NotEqual(status.LastRun.EndedAt, status.ReleasesLastCheckedAt);
    }

    /// <summary>002 U35 (FR-008, FR-011): with nothing stored the administrator view reports no age either.
    /// The list side of FR-011 is covered by ReleasesControllerTests.GetReleases_ListAndStatusReportTheSameInstant.</summary>
    [Fact]
    public async Task Status_WithNothingStored_ReportsNoInstantEitherThoughTheFetchTimestampSurvives()
    {
        var artist = await _db.Artists.UpsertAsync(new LibraryArtistSnapshot("name:daft punk", Guid.NewGuid(), "Daft Punk", null, [Library], []), CancellationToken.None);
        await _db.Artists.SetFetchOutcomeAsync(artist, "musicbrainz", FetchOutcome.Complete, 0, null, _clock.GetUtcNow(), CancellationToken.None);
        RefreshWorkerIs(TaskState.Idle);

        // No release rows were ever stored, or they were purged: the fetch timestamp outlives them.
        var status = (await Controller().GetStatusAsync(CancellationToken.None)).Value!;

        Assert.Null(status.ReleasesLastCheckedAt);
    }

    private async Task<long> SeedReleaseDataAsync()
    {
        var artist = await _db.Artists.UpsertAsync(new LibraryArtistSnapshot("name:daft punk", Guid.NewGuid(), "Daft Punk", null, [Library], []), CancellationToken.None);
        await _db.Artists.SetMatchAsync(artist, "musicbrainz", ArtistMatch.Matched("mbid-1"), CancellationToken.None);
        await _db.Artists.SetFetchOutcomeAsync(artist, "musicbrainz", FetchOutcome.Partial, 100, null, _clock.GetUtcNow(), CancellationToken.None);
        var release = await _db.Releases.UpsertFromSourceAsync(artist, "musicbrainz", new CatalogueItem("rg-1", "Discovery", "https://musicbrainz.org/release-group/rg-1", ReleaseType.Album, [], "2001-03-12"), 1, _clock.GetUtcNow(), CancellationToken.None);
        await _db.Releases.UpsertEditionAsync(release, "musicbrainz", new EditionTrackList("rel-1", "Discovery", ["one more time"]), _clock.GetUtcNow(), CancellationToken.None);
        await _db.Archive.SetAsync("name:daft punk", "discovery", DecisionKind.Ignore, Guid.NewGuid(), _clock.GetUtcNow(), CancellationToken.None);
        return artist;
    }

    [Fact]
    public async Task Purge_EmptiesReleaseData_KeepsDecisionsAndArtists_ResetsResumeOffsets()
    {
        var artist = await SeedReleaseDataAsync();

        Assert.IsType<NoContentResult>(await Controller().PurgeAsync(CancellationToken.None));

        foreach (var emptied in new[] { "release", "source_entry", "edition" })
        {
            Assert.Equal(0L, await _db.ScalarAsync<long>($"SELECT COUNT(*) FROM {emptied}"));
        }

        Assert.Equal(1L, await _db.ScalarAsync<long>("SELECT COUNT(*) FROM decision"));
        var state = (await _db.Artists.GetSourceStateAsync(artist, "musicbrainz", CancellationToken.None))!;
        Assert.Equal((MatchStatus.Matched, "mbid-1", 0), (state.Status, state.SourceArtistId, state.ResumeOffset));
    }

    [Fact]
    public async Task ClearArchive_EmptiesDecisions_LeavesReleaseRowsUntouched()
    {
        await SeedReleaseDataAsync();

        Assert.IsType<NoContentResult>(await Controller().ClearArchiveAsync(CancellationToken.None));

        Assert.Equal(0L, await _db.ScalarAsync<long>("SELECT COUNT(*) FROM decision"));
        Assert.Equal((1L, 1L, 1L), (await _db.ScalarAsync<long>("SELECT COUNT(*) FROM release"), await _db.ScalarAsync<long>("SELECT COUNT(*) FROM source_entry"), await _db.ScalarAsync<long>("SELECT COUNT(*) FROM edition")));
    }
}
