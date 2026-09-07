namespace Jellyfin.Plugin.NewReleases.Library;

/// <summary>Read-only picture of the music library taken by <c>LibraryScanner</c> at the start of a run. Not persisted.</summary>
public sealed record LibrarySnapshot(IReadOnlyList<LibraryArtistSnapshot> Artists);

/// <summary>A library artist (album artist of at least one album, FR-001). <paramref name="ArtistKey"/> is the MBID, else <c>name:&lt;normalized name&gt;</c>.</summary>
public sealed record LibraryArtistSnapshot(
    string ArtistKey,
    Guid JellyfinId,
    string Name,
    string? Mbid,
    IReadOnlyList<Guid> LibraryIds,
    IReadOnlyList<LibraryAlbumSnapshot> Albums);

/// <summary>One library album with the identifiers and normalized track titles the ownership check compares.</summary>
public sealed record LibraryAlbumSnapshot(
    Guid JellyfinId,
    string Title,
    string NormalizedTitle,
    string? MusicBrainzReleaseId,
    string? MusicBrainzReleaseGroupId,
    IReadOnlyList<string> NormalizedTrackTitles);
