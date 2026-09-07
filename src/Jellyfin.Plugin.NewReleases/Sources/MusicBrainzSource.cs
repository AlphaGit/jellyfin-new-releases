using System.Text.Json;
using Jellyfin.Plugin.NewReleases.Library;
using Jellyfin.Plugin.NewReleases.Matching;
using Jellyfin.Plugin.NewReleases.Model;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.NewReleases.Sources;

/// <summary>MusicBrainz (research R2): identifier-based matching, release browse grouped by release group, Official editions with recordings.</summary>
public sealed class MusicBrainzSource : IReleaseSource
{
    private const string Base = "https://musicbrainz.org/ws/2/";
    private const int MinimumScore = 85;
    private const int CataloguePageSize = 100;

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
        var candidates = json.RootElement.GetProperty("artists").EnumerateArray()
            .Select(a => (Id: a.GetProperty("id").GetString()!, Score: a.GetProperty("score").GetInt32()))
            .OrderByDescending(a => a.Score)
            .ToList();
        if (candidates.Count == 0)
        {
            return ArtistMatch.Unmatched("no result");
        }

        var top = candidates[0];
        if (top.Score < MinimumScore)
        {
            return ArtistMatch.Unmatched($"low score ({top.Score})");
        }

        if (candidates.Count > 1 && top.Score - candidates[1].Score <= 5)
        {
            return ArtistMatch.Unmatched($"ambiguous (score {top.Score} vs {candidates[1].Score})");
        }

        return ArtistMatch.Matched(top.Id);
    }

    /// <summary>Browses Official releases with their release groups and yields one item per release group (R2).</summary>
    public async Task<CataloguePage> FetchCataloguePageAsync(string sourceArtistId, int offset, CancellationToken ct)
    {
        var url = $"{Base}release?artist={Uri.EscapeDataString(sourceArtistId)}&status=official&inc=release-groups&limit={CataloguePageSize}&offset={offset}&fmt=json";
        var body = await _http.GetStringAsync(Id, url, ct).ConfigureAwait(false);
        using var json = JsonDocument.Parse(body);
        var items = new List<CatalogueItem>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var release in json.RootElement.GetProperty("releases").EnumerateArray())
        {
            var group = release.GetProperty("release-group");
            var groupId = group.GetProperty("id").GetString()!;
            if (!seen.Add(groupId))
            {
                continue;
            }

            var (primary, secondaries) = ReleaseTypeMapper.MapMusicBrainz(
                OptionalString(group, "primary-type"),
                group.TryGetProperty("secondary-types", out var secondary) ? secondary.EnumerateArray().Select(t => t.GetString()!) : []);
            items.Add(new CatalogueItem(groupId, group.GetProperty("title").GetString()!, $"https://musicbrainz.org/release-group/{groupId}", primary, secondaries, OptionalString(group, "first-release-date")));
        }

        var total = json.RootElement.GetProperty("release-count").GetInt32();
        return new CataloguePage(items, null, total);
    }

    /// <summary>A string property, or null when absent or empty (MusicBrainz sends `""` for an unknown date).</summary>
    private static string? OptionalString(JsonElement element, string name)
        => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String && value.GetString() is { Length: > 0 } text ? text : null;

    public Task<IReadOnlyList<EditionTrackList>> FetchEditionsAsync(string sourceReleaseId, CancellationToken ct) => throw new NotImplementedException();
}
