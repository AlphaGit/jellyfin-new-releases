using Jellyfin.Plugin.NewReleases.Library;
using Jellyfin.Plugin.NewReleases.Tests.Support;
using Xunit;

namespace Jellyfin.Plugin.NewReleases.Tests.Storage;

public sealed class ArtistRepositoryTests : IAsyncLifetime
{
    private static readonly Guid LibraryA = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid LibraryB = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");

    private TestDatabase _db = null!;

    public async Task InitializeAsync() => _db = await TestDatabase.CreateAsync();

    public async Task DisposeAsync() => await _db.DisposeAsync();

    internal static LibraryArtistSnapshot Artist(string key, string name, string? mbid = null, int albums = 1, params Guid[] libraries)
        => new(
            key,
            Guid.NewGuid(),
            name,
            mbid,
            libraries.Length == 0 ? [LibraryA] : libraries,
            Enumerable.Range(1, albums).Select(i => new LibraryAlbumSnapshot(Guid.NewGuid(), $"Album {i}", $"album {i}", null, null, [])).ToArray());

    [Fact]
    public async Task UpsertAsync_SameKeyKeepsIdAndUpdatesFields()
    {
        var first = await _db.Artists.UpsertAsync(Artist("name:daft punk", "Daft Punk", albums: 1), CancellationToken.None);

        var second = await _db.Artists.UpsertAsync(
            Artist("name:daft punk", "DAFT PUNK", mbid: "056e4f3e-d505-4dad-8ec1-d04f521cbb56", albums: 3, LibraryA, LibraryB), CancellationToken.None);

        Assert.Equal(first, second);
        var stored = Assert.Single(await _db.Artists.GetAllAsync(CancellationToken.None));
        Assert.Equal("DAFT PUNK", stored.Name);
        Assert.Equal("056e4f3e-d505-4dad-8ec1-d04f521cbb56", stored.Mbid);
        Assert.Equal([LibraryA, LibraryB], stored.LibraryIds);
        Assert.Equal(3, stored.AlbumCount);
    }
}
