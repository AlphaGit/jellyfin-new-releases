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
}
