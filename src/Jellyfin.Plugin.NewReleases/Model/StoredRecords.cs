namespace Jellyfin.Plugin.NewReleases.Model;

/// <summary>Row of <c>library_artist</c>.</summary>
public sealed record LibraryArtist(
    long Id,
    string ArtistKey,
    Guid JellyfinId,
    string Name,
    string? Mbid,
    IReadOnlyList<Guid> LibraryIds,
    int AlbumCount,
    DateTimeOffset? LastRefreshedAt);
