using Jellyfin.Data.Enums;
using Jellyfin.Plugin.NewReleases.Matching;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
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
            var mbid = ProviderId(item, MetadataProvider.MusicBrainzArtist)
                ?? group.Select(x => ProviderId(x.Album, MetadataProvider.MusicBrainzAlbumArtist)).FirstOrDefault(id => id is not null);
            artists.Add(new LibraryArtistSnapshot(
                mbid ?? "name:" + TitleNormalizer.NormalizeName(group.Key),
                item?.Id ?? Guid.Empty,
                item?.Name ?? group.Key,
                mbid,
                [],
                group.Select(x => SnapshotAlbum(x.Album)).ToArray()));
        }

        _logger.LogInformation("Library scan: {Artists} library artists across {Albums} albums.", artists.Count, albums.Count);
        return new LibrarySnapshot(artists);
    }

    private LibraryAlbumSnapshot SnapshotAlbum(MusicAlbum album)
    {
        // Audio children by ParentId rather than MusicAlbum.Tracks, which needs the static BaseItem.LibraryManager (R6).
        var tracks = _library.GetItemList(new InternalItemsQuery { IncludeItemTypes = [BaseItemKind.Audio], ParentId = album.Id, Recursive = true })
            .Select(track => TitleNormalizer.NormalizeTrack(track.Name))
            .ToArray();
        return new LibraryAlbumSnapshot(
            album.Id,
            album.Name,
            TitleNormalizer.NormalizeAlbum(album.Name),
            ProviderId(album, MetadataProvider.MusicBrainzAlbum),
            ProviderId(album, MetadataProvider.MusicBrainzReleaseGroup),
            tracks);
    }

    /// <summary>First value of a provider id (Jellyfin may store several, comma-separated), or null.</summary>
    private static string? ProviderId(BaseItem? item, MetadataProvider provider)
    {
        if (item is null || !item.ProviderIds.TryGetValue(provider.ToString(), out var raw) || string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var first = raw.Split(',', 2)[0].Trim();
        return first.Length == 0 ? null : first;
    }
}
