using Jellyfin.Plugin.NewReleases.Model;
using Jellyfin.Plugin.NewReleases.ScheduledTasks;
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
}
