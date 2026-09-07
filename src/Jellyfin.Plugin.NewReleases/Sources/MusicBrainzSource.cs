using Jellyfin.Plugin.NewReleases.Library;
using Jellyfin.Plugin.NewReleases.Model;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.NewReleases.Sources;

/// <summary>MusicBrainz (research R2): identifier-based matching, release browse grouped by release group, Official editions with recordings.</summary>
public sealed class MusicBrainzSource : IReleaseSource
{
    private readonly SourceHttpClient _http;
    private readonly ILogger<MusicBrainzSource> _logger;

    public MusicBrainzSource(SourceHttpClient http, ILogger<MusicBrainzSource> logger)
    {
        _http = http;
        _logger = logger;
    }

    public string Id => SourceLimits.MusicBrainz;

    public string DisplayName => "MusicBrainz";

    public Task<ArtistMatch> MatchArtistAsync(LibraryArtistSnapshot artist, CancellationToken ct)
        => artist.Mbid is not null ? Task.FromResult(ArtistMatch.Matched(artist.Mbid)) : throw new NotImplementedException();

    public Task<CataloguePage> FetchCataloguePageAsync(string sourceArtistId, int offset, CancellationToken ct) => throw new NotImplementedException();

    public Task<IReadOnlyList<EditionTrackList>> FetchEditionsAsync(string sourceReleaseId, CancellationToken ct) => throw new NotImplementedException();
}
