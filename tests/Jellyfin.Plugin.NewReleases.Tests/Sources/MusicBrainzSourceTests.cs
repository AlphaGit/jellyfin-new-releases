using Jellyfin.Plugin.NewReleases.Library;
using Jellyfin.Plugin.NewReleases.Model;
using Jellyfin.Plugin.NewReleases.Sources;
using Jellyfin.Plugin.NewReleases.Tests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Plugin.NewReleases.Tests.Sources;

public sealed class MusicBrainzSourceTests : IAsyncLifetime
{
    private const string DaftPunkMbid = "056e4f3e-d505-4dad-8ec1-d04f521cbb56";
    private SourceHarness _h = null!;

    public async Task InitializeAsync() => _h = await SourceHarness.CreateAsync();

    public async Task DisposeAsync() => await _h.DisposeAsync();

    private MusicBrainzSource Source() => new(_h.HttpClient, NullLogger<MusicBrainzSource>.Instance);

    private static LibraryArtistSnapshot Artist(string name, string? mbid = null)
        => new(mbid ?? "name:" + name.ToLowerInvariant(), Guid.NewGuid(), name, mbid, [], []);

    [Fact]
    public async Task MatchArtistAsync_SnapshotWithMbid_IsMatchedWithoutAnyRequest()
    {
        var match = await Source().MatchArtistAsync(Artist("Daft Punk", DaftPunkMbid), CancellationToken.None);

        Assert.Equal(ArtistMatch.Matched(DaftPunkMbid), match);
        Assert.Empty(_h.RequestedUrls);
    }

    private const string ArtistSearch = @"musicbrainz\.org/ws/2/artist\?query=";

    private static string Search(params (string Name, int Score)[] artists)
        => System.Text.Json.JsonSerializer.Serialize(new
        {
            created = "2026-09-06T12:00:00Z",
            count = artists.Length,
            offset = 0,
            artists = artists.Select((a, i) => new { id = $"mbid-{i + 1}", name = a.Name, score = a.Score }),
        });

    [Fact]
    public async Task MatchArtistAsync_ConfidentTopResult_IsMatched()
    {
        _h.Fixture(ArtistSearch, "musicbrainz/artist_search_confident.json"); // Daft Punk 100, runner-up 66

        var match = await Source().MatchArtistAsync(Artist("Daft Punk"), CancellationToken.None);

        Assert.Equal(ArtistMatch.Matched(DaftPunkMbid), match);
    }

    [Fact]
    public async Task MatchArtistAsync_RunnerUpWithinFivePoints_IsUnmatchedAsAmbiguous()
    {
        _h.Fixture(ArtistSearch, "musicbrainz/artist_search_ambiguous.json"); // Nirvana 100 vs Nirvana 96

        var match = await Source().MatchArtistAsync(Artist("Nirvana"), CancellationToken.None);

        Assert.Equal(MatchStatus.Unmatched, match.Status);
        Assert.Equal("ambiguous (score 100 vs 96)", match.Reason);
    }

    [Fact]
    public async Task MatchArtistAsync_RunnerUpSixPointsBehind_IsMatched()
    {
        _h.Http.OnUrlPattern(ArtistSearch, System.Net.HttpStatusCode.OK, Search(("Blur", 90), ("Blur Tribute", 84)));

        Assert.Equal(ArtistMatch.Matched("mbid-1"), await Source().MatchArtistAsync(Artist("Blur"), CancellationToken.None));
    }

    [Fact]
    public async Task MatchArtistAsync_TopScoreBelow85_IsUnmatchedAsLowScore()
    {
        _h.Fixture(ArtistSearch, "musicbrainz/artist_search_low_score.json"); // 84 and 40

        var match = await Source().MatchArtistAsync(Artist("Daft Punkk Orchestra Zzz"), CancellationToken.None);

        Assert.Equal(MatchStatus.Unmatched, match.Status);
        Assert.Equal("low score (84)", match.Reason);
    }

    [Fact]
    public async Task MatchArtistAsync_NoResults_IsUnmatchedAsNoResult()
    {
        _h.Fixture(ArtistSearch, "musicbrainz/artist_search_empty.json");

        var match = await Source().MatchArtistAsync(Artist("zzqxjv nonexistent artist qqq"), CancellationToken.None);

        Assert.Equal(ArtistMatch.Unmatched("no result"), match);
    }
}
