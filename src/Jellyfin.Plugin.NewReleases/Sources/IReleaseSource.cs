using Jellyfin.Plugin.NewReleases.Library;
using Jellyfin.Plugin.NewReleases.Model;

namespace Jellyfin.Plugin.NewReleases.Sources;

/// <summary>
/// A release database the plugin reads (contracts/release-source.md). Both implementations go through
/// <see cref="SourceHttpClient"/>. Any exception is a Failed outcome for the (artist, source) pair; the caller never removes data on it.
/// </summary>
public interface IReleaseSource
{
    /// <summary>`musicbrainz` | `deezer` — stored in every source-scoped row.</summary>
    string Id { get; }

    string DisplayName { get; }

    /// <summary>FR-002. Never guesses: returns Unmatched with a reason when confidence is low.</summary>
    Task<ArtistMatch> MatchArtistAsync(LibraryArtistSnapshot artist, CancellationToken ct);

    /// <summary>FR-003. One page of the artist's catalogue starting at <paramref name="offset"/>. NextOffset null = last page.</summary>
    Task<CataloguePage> FetchCataloguePageAsync(string sourceArtistId, int offset, CancellationToken ct);

    /// <summary>FR-005. Official editions with normalized track lists for one source release id.</summary>
    Task<IReadOnlyList<EditionTrackList>> FetchEditionsAsync(string sourceReleaseId, CancellationToken ct);
}
