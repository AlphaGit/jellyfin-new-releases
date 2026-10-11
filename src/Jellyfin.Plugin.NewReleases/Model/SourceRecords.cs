namespace Jellyfin.Plugin.NewReleases.Model;

/// <summary>Result of <c>IReleaseSource.MatchArtistAsync</c> (FR-002). Never a guess: low confidence is <see cref="Unmatched"/> with a reason.</summary>
public sealed record ArtistMatch(MatchStatus Status, string? SourceArtistId, string? Reason, MatchDetail? Detail = null)
{
    public static ArtistMatch Matched(string sourceArtistId) => new(MatchStatus.Matched, sourceArtistId, null);

    public static ArtistMatch Unmatched(string reason, MatchDetail? detail = null) => new(MatchStatus.Unmatched, null, reason, detail);
}

/// <summary>What a failed match found (spec 008 FR-015): up to 3 candidates and, for Deezer's album check, the counts it compared.</summary>
public sealed record MatchDetail(IReadOnlyList<MatchCandidate> Candidates, int? ExactNameCount = null, int? AlbumsCompared = null);

/// <summary>One artist a source's search returned. Score and ID are MusicBrainz's; Deezer gives only the name.</summary>
public sealed record MatchCandidate(string Name, int? Score = null, string? Id = null);

/// <summary>One release as one source lists it (FR-003). <paramref name="Date"/> is the source string: <c>YYYY</c>, <c>YYYY-MM</c>, <c>YYYY-MM-DD</c> or null.</summary>
public sealed record CatalogueItem(
    string SourceReleaseId,
    string Title,
    string Url,
    ReleaseType PrimaryType,
    IReadOnlyList<ReleaseType> SecondaryTypes,
    string? Date)
{
    /// <summary>The release's album-artist credit (spec 009 FR-005a). Empty when the source gives none, as Deezer's album lists do.</summary>
    public IReadOnlyList<CreditedArtist> Credit { get; init; } = [];
}

/// <summary>One artist credited on a release: MusicBrainz gives the MBID and name.</summary>
public sealed record CreditedArtist(string? Mbid, string Name);

/// <summary>One page of an artist's catalogue. <paramref name="NextOffset"/> null means last page.</summary>
public sealed record CataloguePage(IReadOnlyList<CatalogueItem> Items, int? NextOffset, int Total);

/// <summary>An Official edition with its track titles already normalized as tracks (FR-005).</summary>
public sealed record EditionTrackList(string SourceEditionId, string Title, IReadOnlyList<string> NormalizedTrackTitles);
