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
    private const int EditionPageSize = 25;

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
        var ids = MusicBrainzIds.Parse(artist.Mbid);
        if (ids.Count > 0)
        {
            return ArtistMatch.Matched(string.Join(';', ids)); // 009 FR-005: a joint artist keeps every ID
        }

        // Only the artist name leaves the server (FR-017). Quotes inside the name are escaped for the Lucene query.
        var query = Uri.EscapeDataString("\"" + artist.Name.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal) + "\"");
        var body = await _http.GetStringAsync(Id, $"{Base}artist?query=artist:{query}&limit=5&fmt=json", ct).ConfigureAwait(false);
        using var json = JsonDocument.Parse(body);
        var candidates = json.RootElement.GetProperty("artists").EnumerateArray()
            .Select(a => (Id: a.GetProperty("id").GetString()!, Name: a.GetProperty("name").GetString() ?? string.Empty, Score: a.GetProperty("score").GetInt32()))
            .OrderByDescending(a => a.Score)
            .ToList();
        if (candidates.Count == 0)
        {
            return ArtistMatch.Unmatched(MatchReasons.NoResult, new MatchDetail([]));
        }

        // FR-016: a fix shows at most 3 candidates, so no more are kept.
        var detail = new MatchDetail(candidates.Take(3).Select(c => new MatchCandidate(c.Name, c.Score, c.Id)).ToArray());
        var top = candidates[0];
        if (top.Score < MinimumScore)
        {
            return ArtistMatch.Unmatched(MatchReasons.LowScore(top.Score), detail);
        }

        if (candidates.Count > 1 && top.Score - candidates[1].Score <= 5)
        {
            return ArtistMatch.Unmatched(MatchReasons.Ambiguous(top.Score, candidates[1].Score), detail);
        }

        return ArtistMatch.Matched(top.Id);
    }

    /// <summary>
    /// Browses Official releases with their release groups and credits and yields one item per release group (R2).
    /// A joint artist browses its first ID and keeps only releases credited to every ID (009 R4).
    /// </summary>
    public async Task<CataloguePage> FetchCataloguePageAsync(string sourceArtistId, int offset, CancellationToken ct)
    {
        var ids = MusicBrainzIds.Parse(sourceArtistId);
        var url = $"{Base}release?artist={Uri.EscapeDataString(ids[0])}&status=official&inc=release-groups+artist-credits&limit={CataloguePageSize}&offset={offset}&fmt=json";
        var body = await _http.GetStringAsync(Id, url, ct).ConfigureAwait(false);
        using var json = JsonDocument.Parse(body);
        var items = new List<CatalogueItem>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var release in json.RootElement.GetProperty("releases").EnumerateArray())
        {
            var credit = Credit(release);
            if (ids.Count > 1 && !ids.All(id => credit.Any(c => string.Equals(c.Mbid, id, StringComparison.OrdinalIgnoreCase))))
            {
                continue;
            }

            var group = release.GetProperty("release-group");
            var groupId = group.GetProperty("id").GetString()!;
            if (!seen.Add(groupId))
            {
                continue;
            }

            var (primary, secondaries) = ReleaseTypeMapper.MapMusicBrainz(
                OptionalString(group, "primary-type"),
                group.TryGetProperty("secondary-types", out var secondary) ? secondary.EnumerateArray().Select(t => t.GetString()!) : []);
            items.Add(new CatalogueItem(groupId, group.GetProperty("title").GetString()!, $"https://musicbrainz.org/release-group/{groupId}", primary, secondaries, OptionalString(group, "first-release-date")) { Credit = credit });
        }

        var total = json.RootElement.GetProperty("release-count").GetInt32();
        var next = offset + CataloguePageSize;
        return new CataloguePage(items, next < total ? next : null, total);
    }

    /// <summary>A release's <c>artist-credit</c> as (MBID, name); empty when absent.</summary>
    private static IReadOnlyList<CreditedArtist> Credit(JsonElement release)
        => release.TryGetProperty("artist-credit", out var credit) && credit.ValueKind == JsonValueKind.Array
            ? credit.EnumerateArray()
                .Where(c => c.TryGetProperty("artist", out _))
                .Select(c => c.GetProperty("artist"))
                .Select(a => new CreditedArtist(OptionalString(a, "id"), OptionalString(a, "name") ?? string.Empty))
                .ToArray()
            : [];

    /// <summary>A string property, or null when absent or empty (MusicBrainz sends `""` for an unknown date).</summary>
    private static string? OptionalString(JsonElement element, string name)
        => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String && value.GetString() is { Length: > 0 } text ? text : null;

    /// <summary>Official releases of a release group with recordings; every `media[].tracks[].title` becomes a normalized track (FR-005).</summary>
    public async Task<IReadOnlyList<EditionTrackList>> FetchEditionsAsync(string sourceReleaseId, CancellationToken ct)
    {
        var editions = new List<EditionTrackList>();
        for (var offset = 0; ;)
        {
            var url = $"{Base}release?release-group={Uri.EscapeDataString(sourceReleaseId)}&status=official&inc=recordings+media&limit={EditionPageSize}&offset={offset}&fmt=json";
            var body = await _http.GetStringAsync(Id, url, ct).ConfigureAwait(false);
            using var json = JsonDocument.Parse(body);
            foreach (var release in json.RootElement.GetProperty("releases").EnumerateArray())
            {
                var tracks = release.TryGetProperty("media", out var media)
                    ? media.EnumerateArray()
                        .SelectMany(m => m.TryGetProperty("tracks", out var t) ? t.EnumerateArray() : Enumerable.Empty<JsonElement>())
                        .Select(t => TitleNormalizer.NormalizeTrack(t.GetProperty("title").GetString() ?? string.Empty))
                        .ToArray()
                    : [];
                editions.Add(new EditionTrackList(release.GetProperty("id").GetString()!, release.GetProperty("title").GetString()!, tracks));
            }

            offset += EditionPageSize;
            if (offset >= json.RootElement.GetProperty("release-count").GetInt32())
            {
                return editions;
            }
        }
    }
}
