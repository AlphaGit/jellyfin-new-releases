namespace Jellyfin.Plugin.NewReleases.Model;

/// <summary>Result of <c>IReleaseSource.MatchArtistAsync</c> (FR-002). Never a guess: low confidence is <see cref="Unmatched"/> with a reason.</summary>
public sealed record ArtistMatch(MatchStatus Status, string? SourceArtistId, string? Reason)
{
    public static ArtistMatch Matched(string sourceArtistId) => new(MatchStatus.Matched, sourceArtistId, null);

    public static ArtistMatch Unmatched(string reason) => new(MatchStatus.Unmatched, null, reason);
}
