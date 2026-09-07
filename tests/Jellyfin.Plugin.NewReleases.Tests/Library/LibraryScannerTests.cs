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
}
