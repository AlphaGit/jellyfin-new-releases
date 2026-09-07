using System.Text.Json;
using Jellyfin.Plugin.NewReleases.Library;
using Jellyfin.Plugin.NewReleases.Model;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.NewReleases.Sources;

/// <summary>MusicBrainz (research R2): identifier-based matching, release browse grouped by release group, Official editions with recordings.</summary>
public sealed class MusicBrainzSource : IReleaseSource
{
    private const string Base = "https://musicbrainz.org/ws/2/";
    private const int MinimumScore = 85;

    private readonly SourceHttpClient _http;
    private readonly ILogger<MusicBrainzSource> _logger;

    public MusicBrainzSource(SourceHttpClient http, ILogger<MusicBrainzSource> logger)
    {
        _http = http;
        _logger = logger;
    }

    public string Id => SourceLimits.MusicBrainz;

    public string DisplayName => "MusicBrainz";

    public async Task<ArtistMatch> MatchArtistAsync(LibraryArtistSnapshot artist, CancellationToken ct)
    {
        if (artist.Mbid is not null)
        {
            return ArtistMatch.Matched(artist.Mbid);
        }

        // Only the artist name leaves the server (FR-017). Quotes inside the name are escaped for the Lucene query.
        var query = Uri.EscapeDataString("\"" + artist.Name.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal) + "\"");
        var body = await _http.GetStringAsync(Id, $"{Base}artist?query=artist:{query}&limit=5&fmt=json", ct).ConfigureAwait(false);
        using var json = JsonDocument.Parse(body);
        var top = json.RootElement.GetProperty("artists").EnumerateArray().First();
        return ArtistMatch.Matched(top.GetProperty("id").GetString()!);
    }

    public Task<CataloguePage> FetchCataloguePageAsync(string sourceArtistId, int offset, CancellationToken ct) => throw new NotImplementedException();

    public Task<IReadOnlyList<EditionTrackList>> FetchEditionsAsync(string sourceReleaseId, CancellationToken ct) => throw new NotImplementedException();
}
