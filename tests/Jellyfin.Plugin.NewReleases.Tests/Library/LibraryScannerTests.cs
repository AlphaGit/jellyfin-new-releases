using Jellyfin.Plugin.NewReleases.Library;
using Jellyfin.Plugin.NewReleases.Tests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Plugin.NewReleases.Tests.Library;

public class LibraryScannerTests
{
    private static readonly Guid Library = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");

    private readonly LibraryFakes _library = new();

    private LibrarySnapshot Scan() => new LibraryScanner(_library.Manager, NullLogger<LibraryScanner>.Instance).Scan();

    [Fact]
    public void Scan_AlbumArtistsBecomeLibraryArtists_FeaturedOnlyArtistsDoNot()
    {
        _library.Artist("Daft Punk");
        _library.Artist("Pharrell Williams"); // exists as an artist item through a featured credit; owns no album
        _library.Album("Random Access Memories", "Daft Punk", Library, trackTitles: ["Get Lucky (feat. Pharrell Williams)"]);

        var snapshot = Scan();

        Assert.Equal(["Daft Punk"], snapshot.Artists.Select(a => a.Name));
    }

    [Fact]
    public void Scan_MbidFromArtistThenAlbumArtistProviderId_ElseNameKey()
    {
        _library.Artist("Daft Punk", mbid: "056e4f3e-d505-4dad-8ec1-d04f521cbb56");
        _library.Album("Discovery", "Daft Punk", Library);
        _library.Artist("Justice");
        _library.Album("Cross", "Justice", Library, mbAlbumArtist: "f6ccbf37-4a3d-4b6b-9eef-ea5ef0dc4b2d");
        _library.Artist("Sigur Rós");
        _library.Album("Ágætis byrjun", "Sigur Rós", Library);

        var snapshot = Scan();

        Assert.Equal(
            [("Daft Punk", "056e4f3e-d505-4dad-8ec1-d04f521cbb56", "056e4f3e-d505-4dad-8ec1-d04f521cbb56"),
             ("Justice", "f6ccbf37-4a3d-4b6b-9eef-ea5ef0dc4b2d", "f6ccbf37-4a3d-4b6b-9eef-ea5ef0dc4b2d"),
             ("Sigur Rós", null, "name:sigur ros")],
            snapshot.Artists.Select(a => (a.Name, a.Mbid, a.ArtistKey)));
    }
}
