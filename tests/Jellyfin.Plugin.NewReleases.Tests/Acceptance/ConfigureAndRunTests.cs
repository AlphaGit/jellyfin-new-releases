using Jellyfin.Plugin.NewReleases.Model;
using Jellyfin.Plugin.NewReleases.ScheduledTasks;
using Jellyfin.Plugin.NewReleases.Tests.Support;
using MediaBrowser.Model.Tasks;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.NewReleases.Tests.Acceptance;

/// <summary>User Story 2 acceptance scenarios (spec.md US2-AS3, AS4, AS5, AS7) through the real task and controllers.</summary>
public sealed class ConfigureAndRunTests : IAsyncLifetime
{
    private static readonly string[] Homework = ["Daftendirekt", "Da Funk", "Around the World"];
    private AcceptanceRig _rig = null!;

    public async Task InitializeAsync() => _rig = await AcceptanceRig.CreateAsync();

    public async Task DisposeAsync() => await _rig.DisposeAsync();

    /// <summary>Deezer-only library of one artist owning Homework; the source also lists Alive 2007 and Human After All.</summary>
    private void DeezerScenario()
    {
        _rig.Harness.Configuration.MusicBrainzEnabled = false;
        _rig.Library_.Artist("Daft Punk");
        _rig.Library_.Album("Homework", "Daft Punk", AcceptanceRig.Library, trackTitles: Homework);
        _rig.DeezerArtist(27, "Daft Punk", (2, "Homework", "album", "1997-01-16"), (3, "Alive 2007", "album", "2007-11-16"), (4, "Human After All", "album", "2005-03-14"))
            .DeezerAlbum(2, "Homework", Homework);
    }

    [Fact]
    public async Task A11_RunNowQueuesTheTask_StatusAfterACompletedRunShowsEndCountsAndReleasesFound()
    {
        DeezerScenario();
        var worker = Substitute.For<IScheduledTaskWorker>();
        worker.ScheduledTask.Returns(_rig.Task);
        worker.State.Returns(TaskState.Idle);
        _rig.Tasks.ScheduledTasks.Returns([worker]);

        Assert.IsType<AcceptedResult>(_rig.AdminController().RunNow());
        _rig.Tasks.Received(1).QueueScheduledTask<RefreshNewReleasesTask>();

        await _rig.RunAsync(); // what Jellyfin does with the queued task
        var status = await _rig.AdminStatusAsync();

        Assert.NotNull(status.LastRun);
        Assert.Equal((_rig.Harness.Clock.GetUtcNow(), 1, 3, "Completed"), (status.LastRun.EndedAt, status.LastRun.ArtistsProcessed, status.LastRun.ReleasesFound, status.LastRun.Outcome));
    }

    [Fact]
    public async Task A13_PurgeEmptiesTheList_AReleaseArchivedBeforeThePurgeIsStillArchivedAfterTheNextRun()
    {
        DeezerScenario();
        await _rig.RunAsync();
        await _rig.Harness.Db.Archive.SetAsync("name:daft punk", "alive 2007", DecisionKind.Ignore, AcceptanceRig.Alice, _rig.Harness.Clock.GetUtcNow(), CancellationToken.None);
        Assert.Equal(["Human After All"], (await _rig.ListAsync()).Items.Select(i => i.Title));

        Assert.IsType<NoContentResult>(await _rig.AdminController().PurgeAsync(CancellationToken.None));
        Assert.Empty((await _rig.ListAsync()).Items);
        Assert.Empty((await _rig.ListAsync(archived: true)).Items);

        await _rig.RunAsync();

        Assert.Equal(["Human After All"], (await _rig.ListAsync()).Items.Select(i => i.Title));
        var archived = Assert.Single((await _rig.ListAsync(archived: true)).Items);
        Assert.Equal(("Alive 2007", "Ignore"), (archived.Title, archived.Archived!.Kind));
    }

    [Fact]
    public async Task A15_ClearArchiveEmptiesIt_ArchivedReleasesReturnToTheList_StoredReleasesUnchanged()
    {
        DeezerScenario();
        await _rig.RunAsync();
        await _rig.Harness.Db.Archive.SetAsync("name:daft punk", "alive 2007", DecisionKind.Ignore, AcceptanceRig.Alice, _rig.Harness.Clock.GetUtcNow(), CancellationToken.None);
        await _rig.Harness.Db.Archive.SetAsync("name:daft punk", "human after all", DecisionKind.HaveIt, AcceptanceRig.Alice, _rig.Harness.Clock.GetUtcNow(), CancellationToken.None);
        var storedBefore = await _rig.Harness.Db.ScalarAsync<long>("SELECT COUNT(*) FROM release");
        Assert.Empty((await _rig.ListAsync()).Items);

        Assert.IsType<NoContentResult>(await _rig.AdminController().ClearArchiveAsync(CancellationToken.None));

        Assert.Empty((await _rig.ListAsync(archived: true)).Items);
        Assert.Equal(["Alive 2007", "Human After All"], (await _rig.ListAsync()).Items.Select(i => i.Title));
        Assert.Equal(storedBefore, await _rig.Harness.Db.ScalarAsync<long>("SELECT COUNT(*) FROM release"));
    }

    /// <summary>SC-007: with no source reachable the list still shows the last stored data and reports when it was last refreshed.</summary>
    [Fact]
    public async Task A20_WithEverySourceInCooldown_TheListStillShowsTheStoredDataAndItsAge()
    {
        DeezerScenario();
        await _rig.RunAsync();
        Assert.Equal(["Alive 2007", "Human After All"], (await _rig.ListAsync()).Items.Select(i => i.Title));
        var callsWhileReachable = _rig.Harness.Http.CallCount;

        foreach (var source in new[] { "musicbrainz", "deezer" })
        {
            for (var i = 0; i < Jellyfin.Plugin.NewReleases.Sources.SourceLimits.FailureThreshold; i++)
            {
                await _rig.Harness.Db.SourceState.RecordFailureAsync(source, "503", Jellyfin.Plugin.NewReleases.Sources.SourceLimits.FailureThreshold, Jellyfin.Plugin.NewReleases.Sources.SourceLimits.Cooldown, CancellationToken.None);
            }
        }

        _rig.Harness.Clock.Advance(TimeSpan.FromHours(1)); // still inside the 6 h cooldown
        await _rig.RunAsync();
        var list = await _rig.ListAsync();

        Assert.Equal(callsWhileReachable, _rig.Harness.Http.CallCount); // no source was contacted
        Assert.Equal(["Alive 2007", "Human After All"], list.Items.Select(i => i.Title));
        // 002 FR-003: the second run completed no catalogue fetch, so the reported instant stays
        // at the first run's fetch. Before 002 this asserted the second run's end instead.
        Assert.Equal((true, SourceHarness.Start), (list.HasCompletedRefresh, list.LastRefreshedAt));
    }

    /// <summary>MusicBrainz fails on every call (four failures already on record from earlier runs), Deezer works: the failing source is isolated.</summary>
    [Fact]
    public async Task A12_MusicBrainz503OnEveryCall_WhileDeezerSucceeds_StatusShowsCoolingDownWithLastError_DeezerReleasesListed()
    {
        const string mbid = "056e4f3e-d505-4dad-8ec1-d04f521cbb56";
        _rig.Library_.Artist("Daft Punk", mbid);
        _rig.Library_.Album("Homework", "Daft Punk", AcceptanceRig.Library, trackTitles: Homework);
        _rig.Harness.Http.OnUrlPattern(@"musicbrainz\.org", System.Net.HttpStatusCode.ServiceUnavailable, "{\"error\":\"busy\"}");
        _rig.DeezerArtist(27, "Daft Punk", (2, "Homework", "album", "1997-01-16"), (3, "Alive 2007", "album", "2007-11-16")).DeezerAlbum(2, "Homework", Homework);
        for (var i = 0; i < Jellyfin.Plugin.NewReleases.Sources.SourceLimits.FailureThreshold - 1; i++)
        {
            await _rig.Harness.Db.SourceState.RecordFailureAsync("musicbrainz", "503", Jellyfin.Plugin.NewReleases.Sources.SourceLimits.FailureThreshold, Jellyfin.Plugin.NewReleases.Sources.SourceLimits.Cooldown, CancellationToken.None);
        }

        await _rig.Harness.RunAdvancingAsync(_rig.RunAsync(), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30));
        var status = await _rig.AdminStatusAsync();
        var list = await _rig.ListAsync();

        var musicBrainz = status.Sources.Single(s => s.Id == "musicbrainz");
        Assert.Equal("CoolingDown", musicBrainz.Health);
        Assert.Contains("503", musicBrainz.LastError);
        Assert.Equal("Ok", status.Sources.Single(s => s.Id == "deezer").Health);
        Assert.Equal(["Alive 2007"], list.Items.Select(i => i.Title));
        Assert.Equal("Completed", status.LastRun!.Outcome);
        Assert.Equal(1, status.LastRun.Errors);
    }
}
