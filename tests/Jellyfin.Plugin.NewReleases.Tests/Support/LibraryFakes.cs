using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using NSubstitute;

namespace Jellyfin.Plugin.NewReleases.Tests.Support;

/// <summary>
/// An in-memory music library behind a substituted <see cref="ILibraryManager"/>: <c>GetItemList</c> answers by
/// <c>IncludeItemTypes</c> / <c>ParentId</c>, <c>GetCollectionFolders(album)</c> returns the folders given.
/// </summary>
internal sealed class LibraryFakes
{
    private readonly List<MusicArtist> _artists = [];
    private readonly List<MusicAlbum> _albums = [];
    private readonly List<Audio> _tracks = [];
    private readonly Dictionary<Guid, List<Folder>> _foldersByAlbum = [];

    public LibraryFakes()
    {
        Manager = Substitute.For<ILibraryManager>();
        Manager.GetItemList(Arg.Any<InternalItemsQuery>()).Returns(call => Answer(call.Arg<InternalItemsQuery>()));
        Manager.GetCollectionFolders(Arg.Any<BaseItem>()).Returns(call => _foldersByAlbum.GetValueOrDefault(call.Arg<BaseItem>().Id, []));
    }

    public ILibraryManager Manager { get; }

    public MusicArtist Artist(string name, string? mbid = null)
    {
        var artist = new MusicArtist { Id = Guid.NewGuid(), Name = name };
        if (mbid is not null)
        {
            artist.ProviderIds[MetadataProvider.MusicBrainzArtist.ToString()] = mbid;
        }

        _artists.Add(artist);
        return artist;
    }

    public MusicAlbum Album(string title, string albumArtist, Guid library, string? mbAlbum = null, string? mbReleaseGroup = null, string? mbAlbumArtist = null, params string[] trackTitles)
    {
        var album = new MusicAlbum { Id = Guid.NewGuid(), Name = title, AlbumArtists = [albumArtist] };
        if (mbAlbum is not null)
        {
            album.ProviderIds[MetadataProvider.MusicBrainzAlbum.ToString()] = mbAlbum;
        }

        if (mbReleaseGroup is not null)
        {
            album.ProviderIds[MetadataProvider.MusicBrainzReleaseGroup.ToString()] = mbReleaseGroup;
        }

        if (mbAlbumArtist is not null)
        {
            album.ProviderIds[MetadataProvider.MusicBrainzAlbumArtist.ToString()] = mbAlbumArtist;
        }

        _albums.Add(album);
        _foldersByAlbum[album.Id] = [new Folder { Id = library, Name = "Music " + library.ToString("N")[..4] }];
        foreach (var trackTitle in trackTitles)
        {
            _tracks.Add(new Audio { Id = Guid.NewGuid(), Name = trackTitle, ParentId = album.Id });
        }

        return album;
    }

    private List<BaseItem> Answer(InternalItemsQuery query)
    {
        var kinds = query.IncludeItemTypes ?? [];
        if (kinds.Contains(BaseItemKind.MusicArtist))
        {
            return [.. _artists];
        }

        if (kinds.Contains(BaseItemKind.MusicAlbum))
        {
            return [.. _albums];
        }

        if (kinds.Contains(BaseItemKind.Audio))
        {
            return [.. _tracks.Where(t => query.ParentId == Guid.Empty || t.ParentId == query.ParentId)];
        }

        return [];
    }
}
