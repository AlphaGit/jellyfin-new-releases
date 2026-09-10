using Xunit;

namespace Jellyfin.Plugin.NewReleases.Tests.Acceptance;

/// <summary>User Story 1 acceptance scenarios (spec.md US1-AS1…AS8) through the real task and controller.</summary>
public sealed class BrowseReleasesTests : IAsyncLifetime
{
    private AcceptanceRig _rig = null!;

    public async Task InitializeAsync() => _rig = await AcceptanceRig.CreateAsync();

    public async Task DisposeAsync() => await _rig.DisposeAsync();

    private static readonly string[] Discovery = ["One More Time", "Aerodynamic", "Digital Love"];
    private static readonly string[] Homework = ["Daftendirekt", "Da Funk", "Around the World"];

    /// <summary>Library: A with X, Y. Source lists X, Y, Z. Deezer only (MusicBrainz disabled) so the run stays fast.</summary>
    private void LibraryHasXY_SourceListsXYZ()
    {
        _rig.Harness.Configuration.MusicBrainzEnabled = false;
        _rig.Library_.Artist("Daft Punk");
        _rig.Library_.Album("Discovery", "Daft Punk", AcceptanceRig.Library, trackTitles: Discovery);
        _rig.Library_.Album("Homework", "Daft Punk", AcceptanceRig.Library, trackTitles: Homework);
        _rig.DeezerArtist(27, "Daft Punk", (1, "Discovery", "album", "2001-03-07"), (2, "Homework", "album", "1997-01-16"), (3, "Alive 2007", "album", "2007-11-16"))
            .DeezerAlbum(1, "Discovery", Discovery)
            .DeezerAlbum(2, "Homework", Homework)
            .DeezerAlbum(3, "Alive 2007", "Robot Rock / Oh Yeah", "Touch It / Technologic");
    }

    [Fact]
    public async Task A1_ZIsListedUnderA_XAndYAreNot()
    {
        LibraryHasXY_SourceListsXYZ();

        await _rig.RunAsync();
        var list = await _rig.ListAsync();

        var only = Assert.Single(list.Items);
        Assert.Equal(("Alive 2007", "Daft Punk", "Missing"), (only.Title, only.ArtistName, only.State));
    }

    [Fact]
    public async Task A2_SeveralYears_NewestFirst_UndatedLast_EachCarryingItsDate()
    {
        _rig.Harness.Configuration.MusicBrainzEnabled = false;
        _rig.Library_.Artist("Daft Punk");
        _rig.Library_.Album("Homework", "Daft Punk", AcceptanceRig.Library, trackTitles: Homework);
        _rig.DeezerArtist(27, "Daft Punk",
            (2, "Homework", "album", "1997-01-16"),
            (4, "Alive 1997", "album", "2001-10-01"),
            (3, "Alive 2007", "album", "2007-11-16"),
            (5, "Random Access Memories", "album", "2013-05-17"),
            (6, "Lost Tapes", "album", "0000-00-00"))
            .DeezerAlbum(2, "Homework", Homework);

        await _rig.RunAsync();
        var list = await _rig.ListAsync();

        Assert.Equal(
            [("Random Access Memories", "2013-05-17", "Day"), ("Alive 2007", "2007-11-16", "Day"), ("Alive 1997", "2001-10-01", "Day"), ("Lost Tapes", null, "None")],
            list.Items.Select(i => (i.Title, i.Date, i.DatePrecision)));
    }

    [Fact]
    public async Task A3_FortyReleases_ArtistFilterNarrows_ClearingRestoresAll()
    {
        _rig.Harness.Configuration.MusicBrainzEnabled = false;
        _rig.Library_.Artist("Daft Punk"); _rig.Library_.Album("Homework", "Daft Punk", AcceptanceRig.Library, trackTitles: Homework);
        _rig.Library_.Artist("Justice"); _rig.Library_.Album("Cross", "Justice", AcceptanceRig.Library, trackTitles: ["Genesis"]);
        var daftPunkAlbums = Enumerable.Range(1, 25).Select(i => ((long)(100 + i), $"Daft Album {i:00}", "album", $"20{i:00}-01-01")).Prepend((2L, "Homework", "album", "1997-01-16")).ToArray();
        var justiceAlbums = Enumerable.Range(1, 15).Select(i => ((long)(200 + i), $"Justice Album {i:00}", "album", $"20{i:00}-06-01")).Prepend((7L, "Cross", "album", "2007-06-11")).ToArray();
        _rig.DeezerArtist(27, "Daft Punk", daftPunkAlbums).DeezerAlbum(2, "Homework", Homework);
        _rig.DeezerArtist(28, "Justice", justiceAlbums).DeezerAlbum(7, "Cross", "Genesis");

        await _rig.RunAsync();
        var all = await _rig.ListAsync();
        var daftPunkId = (await _rig.ControllerFor(AcceptanceRig.Alice).GetArtistsAsync(CancellationToken.None)).Value!.Items.Single(a => a.Name == "Daft Punk").JellyfinId;
        var filtered = await _rig.ListAsync(artistId: daftPunkId);
        var cleared = await _rig.ListAsync();

        Assert.Equal(40, all.Total);
        Assert.Equal(25, filtered.Total);
        Assert.All(filtered.Items, i => Assert.Equal("Daft Punk", i.ArtistName));
        Assert.Equal(40, cleared.Total);
    }

    private const string DaftPunkMbid = "056e4f3e-d505-4dad-8ec1-d04f521cbb56";

    /// <summary>Both sources list the same missing release; MusicBrainz is used here (1 req/s), so this test takes a few seconds.</summary>
    [Fact]
    public async Task A4_EveryListedReleaseCarriesOneLinkPerListingSource_HttpsOnTheSourceHosts()
    {
        _rig.Library_.Artist("Daft Punk", DaftPunkMbid);
        _rig.Library_.Album("Homework", "Daft Punk", AcceptanceRig.Library, trackTitles: Homework);
        _rig.MusicBrainzCatalogue(DaftPunkMbid, ("rg-home", "Homework", "Album", [], "1997-01-20"), ("rg-ram", "Random Access Memories", "Album", [], "2013-05-17"))
            .MusicBrainzEditions("rg-home", ("rel-home", "Homework", Homework));
        _rig.DeezerArtist(27, "Daft Punk", (2, "Homework", "album", "1997-01-16"), (5, "Random Access Memories", "album", "2013-05-17"), (3, "Alive 2007", "album", "2007-11-16"))
            .DeezerAlbum(2, "Homework", Homework);

        await _rig.RunAsync();
        var list = await _rig.ListAsync();

        var ram = list.Items.Single(i => i.Title == "Random Access Memories");
        Assert.Equal([("deezer", "https://www.deezer.com/album/5"), ("musicbrainz", "https://musicbrainz.org/release-group/rg-ram")], ram.Sources.Select(s => (s.Source, s.Url)));
        Assert.Equal([("deezer", "https://www.deezer.com/album/3")], list.Items.Single(i => i.Title == "Alive 2007").Sources.Select(s => (s.Source, s.Url)));
        Assert.All(list.Items.SelectMany(i => i.Sources), s =>
        {
            var uri = new Uri(s.Url);
            Assert.Equal("https", uri.Scheme);
            Assert.Contains(uri.Host, new[] { "musicbrainz.org", "www.deezer.com" });
        });
    }

    [Fact]
    public async Task A5_NoCompletedRun_ReportsNoStoredReleasesAndNoInstant()
    {
        var list = await _rig.ListAsync();

        Assert.False(list.HasStoredReleases);
        Assert.Null(list.ReleasesLastCheckedAt);
        Assert.Empty(list.Items);
    }

    [Fact]
    public async Task A6_ZListedAfterRunOne_ZAddedToTheLibrary_AbsentAfterRunTwo()
    {
        LibraryHasXY_SourceListsXYZ();
        await _rig.RunAsync();
        Assert.Equal(["Alive 2007"], (await _rig.ListAsync()).Items.Select(i => i.Title));

        _rig.Library_.Album("Alive 2007", "Daft Punk", AcceptanceRig.Library, trackTitles: ["Robot Rock / Oh Yeah", "Touch It / Technologic"]);
        await _rig.RunAsync();

        Assert.Empty((await _rig.ListAsync()).Items);
    }

    [Fact]
    public async Task A7_ReleaseDatedAfterToday_IsUpcoming_AndTheStateFilterReturnsOnlyIt()
    {
        _rig.Harness.Configuration.MusicBrainzEnabled = false;
        _rig.Library_.Artist("Daft Punk");
        _rig.Library_.Album("Homework", "Daft Punk", AcceptanceRig.Library, trackTitles: Homework);
        _rig.DeezerArtist(27, "Daft Punk", (2, "Homework", "album", "1997-01-16"), (3, "Alive 2007", "album", "2007-11-16"), (9, "Tomorrow", "album", "2026-09-07"))
            .DeezerAlbum(2, "Homework", Homework);

        await _rig.RunAsync();
        var list = await _rig.ListAsync();
        var upcoming = await _rig.ListAsync(state: "Upcoming");

        Assert.Equal("2026-09-06", list.ServerToday);
        Assert.Equal([("Tomorrow", "Upcoming"), ("Alive 2007", "Missing")], list.Items.Select(i => (i.Title, i.State)));
        Assert.Equal(["Tomorrow"], upcoming.Items.Select(i => i.Title));
    }

    [Fact]
    public async Task A8_LibraryAlbumWith8Of10Tracks_IsIncompleteWithTheTwoMissingTitlesAndTheComparedEdition()
    {
        _rig.Harness.Configuration.MusicBrainzEnabled = false;
        var ten = Enumerable.Range(1, 10).Select(i => $"Track {i}").ToArray();
        _rig.Library_.Artist("Daft Punk");
        _rig.Library_.Album("W", "Daft Punk", AcceptanceRig.Library, trackTitles: ten.Take(8).ToArray());
        _rig.DeezerArtist(27, "Daft Punk", (11, "W", "album", "2005-03-14")).DeezerAlbum(11, "W (Edition)", ten);

        await _rig.RunAsync();
        var w = Assert.Single((await _rig.ListAsync()).Items);

        Assert.Equal(("W", "Incomplete"), (w.Title, w.State));
        Assert.Equal(["track 9", "track 10"], w.MissingTracks);
        Assert.Equal(new Jellyfin.Plugin.NewReleases.Api.ComparedEditionDto("deezer", "W (Edition)"), w.ComparedEdition);
    }
}
