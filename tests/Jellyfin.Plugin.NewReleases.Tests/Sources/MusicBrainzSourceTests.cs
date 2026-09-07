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
}
