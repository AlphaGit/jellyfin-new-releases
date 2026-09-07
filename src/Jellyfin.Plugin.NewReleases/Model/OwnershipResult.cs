namespace Jellyfin.Plugin.NewReleases.Model;

/// <summary>Outcome of the ownership check for one release (FR-005, FR-005a). <paramref name="NeedsEditions"/> asks the caller to fetch editions first.</summary>
public sealed record OwnershipResult(
    OwnershipState State,
    string? MatchMethod,
    Guid? LibraryAlbumId,
    long? EditionId,
    IReadOnlyList<string> MissingTracks,
    bool NeedsEditions = false)
{
    public static OwnershipResult Missing { get; } = new(OwnershipState.Missing, null, null, null, []);
}
