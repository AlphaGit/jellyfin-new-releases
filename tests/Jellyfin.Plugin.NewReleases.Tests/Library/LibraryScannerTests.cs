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

    [Fact]
    public void Scan_AlbumSnapshotsCarryIdentifiersAndNormalizedTrackTitles()
    {
        _library.Artist("Daft Punk");
        var album = _library.Album(
            "Discovery (Deluxe Edition)", "Daft Punk", Library,
            mbAlbum: "d073287b-d1bd-4f11-a933-a4386f8cf701", mbReleaseGroup: "48117b90-a16e-34ca-a514-19c702df1158",
            trackTitles: ["One More Time", "Digital Love (feat. Nobody)"]);
        _library.Album("Homework", "Daft Punk", Library, trackTitles: ["Da Funk"]);

        var artist = Assert.Single(Scan().Artists);

        Assert.Equal(2, artist.Albums.Count);
        var discovery = artist.Albums.Single(a => a.JellyfinId == album.Id);
        Assert.Equal(("Discovery (Deluxe Edition)", "discovery", "d073287b-d1bd-4f11-a933-a4386f8cf701", "48117b90-a16e-34ca-a514-19c702df1158"),
            (discovery.Title, discovery.NormalizedTitle, discovery.MusicBrainzReleaseId, discovery.MusicBrainzReleaseGroupId));
        Assert.Equal(["one more time", "digital love"], discovery.NormalizedTrackTitles);
        Assert.Equal(["da funk"], artist.Albums.Single(a => a.Title == "Homework").NormalizedTrackTitles);
    }

    [Fact]
    public void Scan_LibraryIdsAreTheDistinctCollectionFoldersOfTheArtistsAlbums()
    {
        var secondLibrary = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");
        _library.Artist("Daft Punk");
        _library.Album("Discovery", "Daft Punk", Library);
        _library.Album("Homework", "Daft Punk", Library);
        _library.Album("Alive 2007", "Daft Punk", secondLibrary);

        var artist = Assert.Single(Scan().Artists);

        Assert.Equal([Library, secondLibrary], artist.LibraryIds.OrderBy(id => id));
    }
}
