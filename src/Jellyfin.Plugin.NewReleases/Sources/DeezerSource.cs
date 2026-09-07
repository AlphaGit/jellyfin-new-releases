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
    private const int QuotaExceededCode = 4;
    private static readonly TimeSpan QuotaBackoff = TimeSpan.FromSeconds(5);

    private readonly SourceHttpClient _http;
    private readonly TimeProvider _clock;
    private readonly ILogger<DeezerSource> _logger;

    public DeezerSource(SourceHttpClient http, TimeProvider clock, ILogger<DeezerSource> logger)
    {
        _http = http;
        _clock = clock;
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

        return ArtistMatch.Unmatched(candidates.Count == 0 ? "no result" : "no corroborating album");
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
                album.TryGetProperty("release_date", out var date) && date.GetString() is { Length: > 0 } text && text != "0000-00-00" ? text : null);
        }).ToList();
        return new CataloguePage(items, NextIndex(json.RootElement), json.RootElement.TryGetProperty("total", out var total) ? total.GetInt32() : items.Count);
    }

    /// <summary>Deezer pages with a `next` URL; its `index` query value is the next offset. Absent `next` = last page.</summary>
    private static int? NextIndex(JsonElement page)
    {
        if (!page.TryGetProperty("next", out var next) || next.ValueKind != JsonValueKind.String || !Uri.TryCreate(next.GetString(), UriKind.Absolute, out var uri))
        {
            return null;
        }

        var index = uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split('=', 2))
            .FirstOrDefault(kv => kv[0] == "index" && kv.Length == 2);
        return index is not null && int.TryParse(index[1], NumberStyles.None, CultureInfo.InvariantCulture, out var value) ? value : null;
    }

    /// <summary>One edition per Deezer album: title from `album/&lt;id&gt;`, tracks from `album/&lt;id&gt;/tracks` following `next` (FR-005).</summary>
    public async Task<IReadOnlyList<EditionTrackList>> FetchEditionsAsync(string sourceReleaseId, CancellationToken ct)
    {
        var id = Uri.EscapeDataString(sourceReleaseId);
        string title;
        using (var album = await GetJsonAsync($"{Base}album/{id}", ct).ConfigureAwait(false))
        {
            title = album.RootElement.GetProperty("title").GetString() ?? sourceReleaseId;
        }

        var tracks = new List<string>();
        string? url = $"{Base}album/{id}/tracks?limit={AlbumPageSize}";
        while (url is not null)
        {
            using var page = await GetJsonAsync(url, ct).ConfigureAwait(false);
            tracks.AddRange(page.RootElement.GetProperty("data").EnumerateArray().Select(t => TitleNormalizer.NormalizeTrack(t.GetProperty("title").GetString() ?? string.Empty)));
            url = page.RootElement.TryGetProperty("next", out var next) && next.ValueKind == JsonValueKind.String ? next.GetString() : null;
        }

        return [new EditionTrackList(sourceReleaseId, title, tracks)];
    }

    /// <summary>Deezer answers HTTP 200 with an `error` envelope (R3): code 4 (quota) is transient and retried after a short backoff; anything else is a failure.</summary>
    private async Task<JsonDocument> GetJsonAsync(string url, CancellationToken ct)
    {
        for (var attempt = 0; ; attempt++)
        {
            var json = JsonDocument.Parse(await _http.GetStringAsync(Id, url, ct).ConfigureAwait(false));
            if (!json.RootElement.TryGetProperty("error", out var error))
            {
                return json;
            }

            var code = error.TryGetProperty("code", out var c) && c.ValueKind == JsonValueKind.Number ? c.GetInt32() : -1;
            var message = error.TryGetProperty("message", out var m) ? m.GetString() : null;
            json.Dispose();
            if (code != QuotaExceededCode || attempt >= SourceLimits.RetryBackoffs.Length)
            {
                throw new HttpRequestException($"Deezer error {code}: {message}");
            }

            _logger.LogWarning("Deezer quota exceeded; retrying in {Delay}.", QuotaBackoff);
            await Task.Delay(QuotaBackoff, _clock, ct).ConfigureAwait(false);
        }
    }
}
