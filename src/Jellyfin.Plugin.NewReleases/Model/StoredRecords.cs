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

/// <summary>Row of <c>release</c>.</summary>
public sealed record Release(
    long Id,
    long LibraryArtistId,
    string NormalizedTitle,
    string Title,
    string CanonicalSource,
    string CanonicalSourceId,
    ReleaseType PrimaryType,
    IReadOnlyList<ReleaseType> SecondaryTypes,
    string? ReleaseDate,
    string? DateSort,
    DateTimeOffset FirstSeenAt,
    DateTimeOffset LastSeenAt,
    OwnershipState OwnershipState,
    string? MatchMethod,
    Guid? LibraryAlbumId,
    long? ComparedEditionId,
    IReadOnlyList<string> MissingTracks,
    DateTimeOffset? OwnershipCheckedAt);

/// <summary>Row of <c>decision</c>: an Ignore or Have-it decision keyed by the natural release key (R8).</summary>
public sealed record Decision(string ArtistKey, string NormalizedTitle, DecisionKind Kind, Guid UserId, DateTimeOffset DecidedAt);

/// <summary>Read-time query for the list and the Archive. Type set, released-since and today come from configuration and the clock (R7).</summary>
public sealed record ReleaseFilter(
    ISet<ReleaseType> EnabledTypes,
    DateOnly Today,
    DateOnly? ReleasedSince = null,
    Guid? ArtistJellyfinId = null,
    ReleaseType? Type = null,
    ListState? State = null,
    DateOnly? From = null,
    DateOnly? To = null,
    bool Archived = false);

/// <summary>One row of the list or the Archive, joined with its artist, source links, compared edition and decision.</summary>
public sealed record ListedRelease(
    long Id,
    string ArtistName,
    Guid ArtistJellyfinId,
    IReadOnlyList<Guid> ArtistLibraryIds,
    string Title,
    ReleaseType Type,
    string? Date,
    string? DateSort,
    ListState State,
    IReadOnlyList<string> MissingTracks,
    ComparedEdition? ComparedEdition,
    IReadOnlyList<SourceLink> Sources,
    Decision? Archived);

public sealed record SourceLink(string Source, string Url);

public sealed record ComparedEdition(string Source, string Title);

/// <summary>Row of <c>edition</c>: one Official version of a release with its normalized track titles.</summary>
public sealed record Edition(long Id, long ReleaseId, string Source, string SourceEditionId, string Title, IReadOnlyList<string> Tracks, DateTimeOffset FetchedAt);

/// <summary>Row of <c>source_state</c>: health, budget counter and cooldown of one source (FR-011, FR-012).</summary>
public sealed record SourceState(
    string Source,
    int ConsecutiveFailures,
    DateTimeOffset? CooldownUntil,
    int CallsToday,
    DateOnly? CallsDay,
    DateTimeOffset? NextAllowedAt,
    string? LastError,
    DateTimeOffset? LastSuccessAt);

/// <summary>Row of <c>refresh_run</c> (FR-012, FR-015).</summary>
public sealed record RefreshRun(
    long Id,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt,
    int ArtistsProcessed,
    int ReleasesFound,
    int EditionsFetched,
    int Errors,
    string? Outcome);
