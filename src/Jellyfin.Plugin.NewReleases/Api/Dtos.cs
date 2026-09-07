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
