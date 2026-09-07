using System.Globalization;
using System.Text.Json;
using Jellyfin.Plugin.NewReleases.Library;
using Jellyfin.Plugin.NewReleases.Matching;
using Jellyfin.Plugin.NewReleases.Model;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.NewReleases.Sources;

/// <summary>Deezer (research R3): name matching corroborated by a library album title, album pages with `next`, one edition per album.</summary>
public sealed class DeezerSource : IReleaseSource
{
    private const string Base = "https://api.deezer.com/";
    private const int AlbumPageSize = 100;

    private readonly SourceHttpClient _http;
    private readonly ILogger<DeezerSource> _logger;

    public DeezerSource(SourceHttpClient http, ILogger<DeezerSource> logger)
    {
        _http = http;
        _logger = logger;
    }

    public string Id => SourceLimits.Deezer;

    public string DisplayName => "Deezer";

    /// <summary>FR-002: exact normalized name, and at least one album on the candidate's first page matches a library album title.</summary>
    public async Task<ArtistMatch> MatchArtistAsync(LibraryArtistSnapshot artist, CancellationToken ct)
    {
        var wanted = TitleNormalizer.NormalizeName(artist.Name);
        var libraryTitles = artist.Albums.Select(a => a.NormalizedTitle).ToHashSet(StringComparer.Ordinal);
        using var search = await GetJsonAsync($"{Base}search/artist?q={Uri.EscapeDataString(artist.Name)}&limit=25", ct).ConfigureAwait(false);
        var candidates = search.RootElement.GetProperty("data").EnumerateArray()
            .Where(a => TitleNormalizer.NormalizeName(a.GetProperty("name").GetString() ?? string.Empty) == wanted)
            .Select(a => a.GetProperty("id").GetInt64().ToString(CultureInfo.InvariantCulture))
            .ToList();

        foreach (var candidate in candidates)
        {
            var page = await FetchCataloguePageAsync(candidate, 0, ct).ConfigureAwait(false);
            if (page.Items.Any(item => libraryTitles.Contains(TitleNormalizer.NormalizeAlbum(item.Title))))
            {
                return ArtistMatch.Matched(candidate);
            }
        }

        return ArtistMatch.Unmatched("no result");
    }

    public async Task<CataloguePage> FetchCataloguePageAsync(string sourceArtistId, int offset, CancellationToken ct)
    {
        using var json = await GetJsonAsync($"{Base}artist/{Uri.EscapeDataString(sourceArtistId)}/albums?index={offset}&limit={AlbumPageSize}", ct).ConfigureAwait(false);
        var items = json.RootElement.GetProperty("data").EnumerateArray().Select(album =>
        {
            var (primary, secondaries) = ReleaseTypeMapper.MapDeezer(album.GetProperty("record_type").GetString());
            return new CatalogueItem(
                album.GetProperty("id").GetInt64().ToString(CultureInfo.InvariantCulture),
                album.GetProperty("title").GetString() ?? string.Empty,
                album.GetProperty("link").GetString() ?? string.Empty,
                primary,
                secondaries,
                album.TryGetProperty("release_date", out var date) ? date.GetString() : null);
        }).ToList();
        return new CataloguePage(items, null, json.RootElement.TryGetProperty("total", out var total) ? total.GetInt32() : items.Count);
    }

    public Task<IReadOnlyList<EditionTrackList>> FetchEditionsAsync(string sourceReleaseId, CancellationToken ct) => throw new NotImplementedException();

    private async Task<JsonDocument> GetJsonAsync(string url, CancellationToken ct)
        => JsonDocument.Parse(await _http.GetStringAsync(Id, url, ct).ConfigureAwait(false));
}
