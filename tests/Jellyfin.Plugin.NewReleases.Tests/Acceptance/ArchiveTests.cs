using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Jellyfin.Plugin.NewReleases.Tests.Acceptance;

/// <summary>User Story 3 acceptance scenarios (spec.md US3-AS1…AS4) through the real controller and task.</summary>
public sealed class ArchiveTests : IAsyncLifetime
{
    private static readonly string[] Homework = ["Daftendirekt", "Da Funk", "Around the World"];
    private static readonly string[] HumanAfterAll = ["Human After All", "The Prime Time of Your Life", "Robot Rock", "Steam Machine"];
    private AcceptanceRig _rig = null!;

    public async Task InitializeAsync() => _rig = await AcceptanceRig.CreateAsync();

    public async Task DisposeAsync() => await _rig.DisposeAsync();

    /// <summary>Deezer-only: library owns Homework, holds 3 of Human After All's 4 tracks (Incomplete), and lacks Alive 2007 (Missing).</summary>
    private MediaBrowser.Controller.Entities.Audio.MusicAlbum Scenario()
    {
        _rig.Harness.Configuration.MusicBrainzEnabled = false;
        _rig.Library_.Artist("Daft Punk");
        _rig.Library_.Album("Homework", "Daft Punk", AcceptanceRig.Library, trackTitles: Homework);
        var humanAfterAll = _rig.Library_.Album("Human After All", "Daft Punk", AcceptanceRig.Library, trackTitles: HumanAfterAll.Take(3).ToArray());
        _rig.DeezerArtist(27, "Daft Punk", (2, "Homework", "album", "1997-01-16"), (3, "Alive 2007", "album", "2007-11-16"), (4, "Human After All", "album", "2005-03-14"))
            .DeezerAlbum(2, "Homework", Homework)
            .DeezerAlbum(4, "Human After All", HumanAfterAll);
        return humanAfterAll;
    }

    private async Task<long> IdOf(string title, bool archived = false) => (await _rig.ListAsync(archived: archived)).Items.Single(i => i.Title == title).Id;

    [Fact]
    public async Task A16_IgnoreRemovesTheReleaseAtOnce_StillAbsentAfterARun_PresentInTheArchiveAsIgnore()
    {
        Scenario();
        await _rig.RunAsync();
        var alive = await IdOf("Alive 2007");

        Assert.IsType<NoContentResult>(await _rig.ControllerFor(AcceptanceRig.Alice).IgnoreAsync(alive, CancellationToken.None));
        Assert.DoesNotContain((await _rig.ListAsync()).Items, i => i.Title == "Alive 2007");

        await _rig.RunAsync();

        Assert.DoesNotContain((await _rig.ListAsync()).Items, i => i.Title == "Alive 2007");
        var archived = (await _rig.ListAsync(archived: true)).Items.Single(i => i.Title == "Alive 2007");
        Assert.Equal("Ignore", archived.Archived!.Kind);
    }

    [Fact]
    public async Task A17_RestoreOnAnArchivedRelease_PutsItBackInTheListAtItsDatePosition()
    {
        Scenario();
        await _rig.RunAsync();
        var controller = _rig.ControllerFor(AcceptanceRig.Alice);
        await controller.IgnoreAsync(await IdOf("Human After All"), CancellationToken.None);
        Assert.Equal(["Alive 2007"], (await _rig.ListAsync()).Items.Select(i => i.Title));

        Assert.IsType<NoContentResult>(await controller.RestoreAsync(await IdOf("Human After All", archived: true), CancellationToken.None));

        Assert.Equal(["Alive 2007", "Human After All"], (await _rig.ListAsync()).Items.Select(i => i.Title)); // 2007 before 2005
        Assert.Empty((await _rig.ListAsync(archived: true)).Items);
    }

    [Fact]
    public async Task A18_HaveItOnAnIncompleteRelease_StaysArchivedAfterARunThatStillFindsTracksMissing()
    {
        Scenario();
        await _rig.RunAsync();
        var human = (await _rig.ListAsync()).Items.Single(i => i.Title == "Human After All");
        Assert.Equal("Incomplete", human.State);

        await _rig.ControllerFor(AcceptanceRig.Alice).HaveItAsync(human.Id, CancellationToken.None);
        await _rig.RunAsync();

        Assert.DoesNotContain((await _rig.ListAsync()).Items, i => i.Title == "Human After All");
        var archived = (await _rig.ListAsync(archived: true)).Items.Single(i => i.Title == "Human After All");
        Assert.Equal(("HaveIt", "Incomplete"), (archived.Archived!.Kind, archived.State)); // the automatic check still says Incomplete
    }

    [Fact]
    public async Task A19_HaveItRelease_WhoseLibraryAlbumLaterGainsEveryTrack_StaysInTheArchive()
    {
        var humanAfterAll = Scenario();
        await _rig.RunAsync();
        await _rig.ControllerFor(AcceptanceRig.Alice).HaveItAsync(await IdOf("Human After All"), CancellationToken.None);

        _rig.Library_.SetTracks(humanAfterAll, HumanAfterAll); // the library gains the fourth track
        await _rig.RunAsync();

        Assert.Equal("Owned", await _rig.Harness.Db.ScalarAsync<string>("SELECT ownership_state FROM release WHERE title = 'Human After All'"));
        var archived = (await _rig.ListAsync(archived: true)).Items.Single(i => i.Title == "Human After All");
        Assert.Equal("HaveIt", archived.Archived!.Kind);
        Assert.DoesNotContain((await _rig.ListAsync()).Items, i => i.Title == "Human After All");
    }
}
