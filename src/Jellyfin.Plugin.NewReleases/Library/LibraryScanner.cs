using Jellyfin.Data.Enums;
using Jellyfin.Plugin.NewReleases.Matching;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.NewReleases.Library;

/// <summary>
/// Reads the music library into a <see cref="LibrarySnapshot"/> (FR-001, FR-002, FR-005, FR-007). Read-only: it never
/// calls <c>ILibraryManager.GetArtist(string)</c>, which would create items (R6).
/// </summary>
public sealed class LibraryScanner
{
    private readonly ILibraryManager _library;
    private readonly ILogger<LibraryScanner> _logger;

    public LibraryScanner(ILibraryManager library, ILogger<LibraryScanner> logger)
    {
        _library = library;
        _logger = logger;
    }

    public LibrarySnapshot Scan()
    {
        var artistItems = _library.GetItemList(new InternalItemsQuery { IncludeItemTypes = [BaseItemKind.MusicArtist], Recursive = true })
            .OfType<MusicArtist>()
            .ToList();
        var albums = _library.GetItemList(new InternalItemsQuery { IncludeItemTypes = [BaseItemKind.MusicAlbum], Recursive = true })
            .OfType<MusicAlbum>()
            .ToList();

        var artists = new List<LibraryArtistSnapshot>();
        foreach (var group in albums.SelectMany(album => album.AlbumArtists.Select(name => (Name: name, Album: album))).GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
        {
            var item = artistItems.FirstOrDefault(a => string.Equals(a.Name, group.Key, StringComparison.OrdinalIgnoreCase));
            artists.Add(new LibraryArtistSnapshot(
                "name:" + TitleNormalizer.NormalizeName(group.Key),
                item?.Id ?? Guid.Empty,
                item?.Name ?? group.Key,
                null,
                [],
                []));
        }

        _logger.LogInformation("Library scan: {Artists} library artists across {Albums} albums.", artists.Count, albums.Count);
        return new LibrarySnapshot(artists);
    }
}
