namespace Jellyfin.Plugin.NewReleases.Api;

// Wire shapes from specs/001-track-new-releases/contracts/http-api.md. Jellyfin serializes camelCase.

public sealed record ListResponse(
    IReadOnlyList<ReleaseDto> Items,
    int Total,
    bool HasCompletedRefresh,
    DateTimeOffset? LastRefreshedAt,
    int RefreshIntervalHours,
    string ServerToday);

public sealed record ReleaseDto(
    long Id,
    string ArtistName,
    Guid ArtistJellyfinId,
    string Title,
    string Type,
    string? Date,
    string DatePrecision,
    string State,
    IReadOnlyList<string>? MissingTracks,
    ComparedEditionDto? ComparedEdition,
    IReadOnlyList<SourceLinkDto> Sources,
    ArchivedDto? Archived);

public sealed record ComparedEditionDto(string Source, string Title);

public sealed record SourceLinkDto(string Source, string Url);

public sealed record ArchivedDto(string Kind, DateTimeOffset DecidedAt);

public sealed record ArtistDto(Guid JellyfinId, string Name);

public sealed record ArtistsResponse(IReadOnlyList<ArtistDto> Items);

public sealed record StatusResponse(bool HasCompletedRefresh, DateTimeOffset? LastRefreshedAt, int RefreshIntervalHours, bool IsRunning);

// Admin status (contracts/http-api.md, GET api/admin/status).

public sealed record AdminStatusResponse(
    IReadOnlyList<SourceStatusDto> Sources,
    RunDto? LastRun,
    DateTimeOffset? NextRunAt,
    bool IsRunning,
    int LibraryArtists,
    IReadOnlyDictionary<string, int> MatchedArtists,
    IReadOnlyList<UnmatchedArtistDto> Unmatched);

public sealed record SourceStatusDto(string Id, string DisplayName, bool Enabled, string Health, string? LastError, int CallsToday, int DailyBudget, DateTimeOffset? CooldownUntil, DateTimeOffset? LastSuccessAt);

public sealed record RunDto(DateTimeOffset StartedAt, DateTimeOffset? EndedAt, string? Outcome, int ArtistsProcessed, int ReleasesFound, int EditionsFetched, int Errors);

public sealed record UnmatchedArtistDto(Guid JellyfinId, string Name, IReadOnlyList<UnmatchedSourceDto> Sources, string Hint);

public sealed record UnmatchedSourceDto(string Source, string Reason);
