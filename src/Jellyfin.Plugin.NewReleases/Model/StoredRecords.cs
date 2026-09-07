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

/// <summary>Row of <c>artist_source</c>: match and fetch state of one artist at one source.</summary>
public sealed record ArtistSourceState(
    long LibraryArtistId,
    string Source,
    MatchStatus Status,
    string? SourceArtistId,
    string? UnmatchedReason,
    int ResumeOffset,
    FetchOutcome? LastOutcome,
    DateTimeOffset? LastCompleteAt,
    string? LastError);

/// <summary>Admin-page counts (FR-012).</summary>
public sealed record ArtistCounts(int LibraryArtists, IReadOnlyDictionary<string, int> MatchedBySource, IReadOnlyList<UnmatchedArtist> Unmatched);

/// <summary>A library artist Unmatched at one or more sources, with each source's reason.</summary>
public sealed record UnmatchedArtist(Guid JellyfinId, string Name, IReadOnlyList<UnmatchedAt> Sources);

public sealed record UnmatchedAt(string Source, string Reason);
