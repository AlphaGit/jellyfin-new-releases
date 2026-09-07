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
}
