using Jellyfin.Plugin.NewReleases.Library;
using Jellyfin.Plugin.NewReleases.Matching;
using Jellyfin.Plugin.NewReleases.Model;
using Jellyfin.Plugin.NewReleases.Sources;
using Jellyfin.Plugin.NewReleases.Tests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Plugin.NewReleases.Tests.Sources;

public sealed class DeezerSourceTests : IAsyncLifetime
{
    private const string Search = @"api\.deezer\.com/search/artist\?q=";
    private SourceHarness _h = null!;

    public async Task InitializeAsync() => _h = await SourceHarness.CreateAsync();

    public async Task DisposeAsync() => await _h.DisposeAsync();

    private DeezerSource Source() => new(_h.HttpClient, NullLogger<DeezerSource>.Instance);

    private const string EmptyPage = "{\"data\":[],\"total\":0}";

    private static LibraryArtistSnapshot Artist(string name, params string[] albumTitles)
        => new("name:" + TitleNormalizer.NormalizeName(name), Guid.NewGuid(), name, null, [],
            albumTitles.Select(t => new LibraryAlbumSnapshot(Guid.NewGuid(), t, TitleNormalizer.NormalizeAlbum(t), null, null, [])).ToArray());

    [Fact]
    public async Task MatchArtistAsync_ExactNameWhoseFirstAlbumsPageContainsALibraryAlbum_IsMatched()
    {
        _h.Fixture(Search, "deezer/search_artist_exact.json");                       // Daft Punk = id 27, plus near-misses
        _h.Fixture(@"api\.deezer\.com/artist/27/albums\?", "deezer/artist_albums_page1.json"); // contains Discovery

        var match = await Source().MatchArtistAsync(Artist("Daft Punk", "Discovery (Deluxe Edition)"), CancellationToken.None);

        Assert.Equal(ArtistMatch.Matched("27"), match);
    }

    [Fact]
    public async Task MatchArtistAsync_ExactNameWithoutACorroboratingAlbum_IsUnmatchedWithReason()
    {
        _h.Fixture(Search, "deezer/search_artist_exact.json"); // exact-name candidates: 27 and 412557421
        _h.Fixture(@"api\.deezer\.com/artist/27/albums\?", "deezer/artist_albums_page1.json");
        _h.Http.OnUrlPattern(@"api\.deezer\.com/artist/412557421/albums\?", System.Net.HttpStatusCode.OK, EmptyPage);

        var match = await Source().MatchArtistAsync(Artist("Daft Punk", "Some Album The Library Made Up"), CancellationToken.None);

        Assert.Equal(ArtistMatch.Unmatched("no corroborating album"), match);
    }

    [Fact]
    public async Task MatchArtistAsync_TwoExactNameHomonyms_OnlyTheSecondCorroborated_MatchesTheSecond()
    {
        _h.Fixture(Search, "deezer/search_artist_homonyms.json"); // exact "Nirvana": 415 then 278793911
        _h.Http.OnUrlPattern(@"api\.deezer\.com/artist/415/albums\?", System.Net.HttpStatusCode.OK, EmptyPage);
        _h.Http.OnUrlPattern(@"api\.deezer\.com/artist/278793911/albums\?", System.Net.HttpStatusCode.OK,
            "{\"data\":[{\"id\":1,\"title\":\"Nevermind\",\"link\":\"https://www.deezer.com/album/1\",\"record_type\":\"album\",\"release_date\":\"1991-09-24\"}],\"total\":1}");

        var match = await Source().MatchArtistAsync(Artist("Nirvana", "Nevermind"), CancellationToken.None);

        Assert.Equal(ArtistMatch.Matched("278793911"), match);
    }

    [Fact]
    public async Task MatchArtistAsync_NoSearchResults_IsUnmatched()
    {
        _h.Fixture(Search, "deezer/search_artist_empty.json");

        var match = await Source().MatchArtistAsync(Artist("zzqxjv nonexistent artist qqq", "Anything"), CancellationToken.None);

        Assert.Equal(MatchStatus.Unmatched, match.Status);
        Assert.DoesNotContain(_h.RequestedUrls, u => u.Contains("/albums", StringComparison.Ordinal));
    }

    private const string Albums27 = @"api\.deezer\.com/artist/27/albums\?";

    [Fact]
    public async Task FetchCataloguePageAsync_MapsRecordTypeAndTakesNextOffsetFromNext()
    {
        _h.Fixture(Albums27 + "index=0", "deezer/artist_albums_page1.json");  // next = …index=25, total 39
        _h.Fixture(Albums27 + "index=25", "deezer/artist_albums_page2.json"); // no next
        var source = Source();

        var first = await source.FetchCataloguePageAsync("27", 0, CancellationToken.None);
        var last = await source.FetchCataloguePageAsync("27", 25, CancellationToken.None);

        Assert.Equal((25, 39), (first.NextOffset, first.Total));
        Assert.Null(last.NextOffset);
        Assert.Equal(ReleaseType.Album, first.Items.Single(i => i.Title == "Discovery").PrimaryType);
        Assert.Equal(ReleaseType.EP, first.Items.Single(i => i.Title == "Revolution 909").PrimaryType);
        Assert.Equal(ReleaseType.Single, first.Items.Single(i => i.Title.StartsWith("Get Lucky")).PrimaryType);
        Assert.All(first.Items, i => Assert.Empty(i.SecondaryTypes));
    }

    [Fact]
    public async Task FetchCataloguePageAsync_UnknownDateBecomesNull_LinkBecomesTheUrl()
    {
        _h.Fixture(Albums27, "deezer/artist_albums_unknown_date.json"); // first row release_date 0000-00-00

        var page = await Source().FetchCataloguePageAsync("27", 0, CancellationToken.None);

        Assert.Null(page.Items[0].Date);
        Assert.Equal("2010-03-15", page.Items[1].Date);
        Assert.All(page.Items, i => Assert.Equal($"https://www.deezer.com/album/{i.SourceReleaseId}", i.Url));
    }

    [Fact]
    public async Task FetchEditionsAsync_TracksAcrossTwoPages_ComeBackAsOneEditionTitledAsTheAlbum()
    {
        _h.Fixture(@"api\.deezer\.com/album/302127$", "deezer/album.json");
        _h.Fixture(@"api\.deezer\.com/album/302127/tracks\?(?!.*index=8)", "deezer/album_tracks_page1.json"); // next → index=8
        _h.Fixture(@"api\.deezer\.com/album/302127/tracks\?.*index=8", "deezer/album_tracks_page2.json");

        var editions = await Source().FetchEditionsAsync("302127", CancellationToken.None);

        var edition = Assert.Single(editions);
        Assert.Equal(("302127", "Discovery", 14), (edition.SourceEditionId, edition.Title, edition.NormalizedTrackTitles.Count));
        Assert.Equal("one more time", edition.NormalizedTrackTitles[0]);
        Assert.Equal("too long", edition.NormalizedTrackTitles[13]);
    }
}
