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

    [Fact]
    public async Task DeleteMissingAsync_RemovesArtistsAbsentFromTheSnapshotWithTheirRows()
    {
        var keep = await _db.Artists.UpsertAsync(Artist("name:keep", "Keep"), CancellationToken.None);
        var gone = await _db.Artists.UpsertAsync(Artist("name:gone", "Gone"), CancellationToken.None);
        foreach (var id in new[] { keep, gone })
        {
            await _db.ExecuteAsync($"""
                INSERT INTO artist_source (library_artist_id, source, status) VALUES ({id}, 'musicbrainz', 'Pending');
                INSERT INTO release (id, library_artist_id, normalized_title, title, canonical_source, canonical_source_id, primary_type, secondary_types, first_seen_at, last_seen_at, ownership_state)
                    VALUES ({id * 100}, {id}, 'x', 'X', 'musicbrainz', 'rg-{id}', 'Album', '[]', '2026-01-01T00:00:00Z', '2026-01-01T00:00:00Z', 'Missing');
                INSERT INTO source_entry (release_id, source, source_release_id, url, source_title, last_seen_run_id) VALUES ({id * 100}, 'musicbrainz', 'rg-{id}', 'https://musicbrainz.org/release-group/rg-{id}', 'X', 1);
                INSERT INTO edition (release_id, source, source_edition_id, title, status, tracks, fetched_at) VALUES ({id * 100}, 'musicbrainz', 'rel-{id}', 'X', 'Official', '[]', '2026-01-01T00:00:00Z');
                """);
        }

        await _db.Artists.DeleteMissingAsync(["name:keep"], CancellationToken.None);

        Assert.Equal("Keep", Assert.Single(await _db.Artists.GetAllAsync(CancellationToken.None)).Name);
        foreach (var table in new[] { "artist_source", "release", "source_entry", "edition" })
        {
            Assert.Equal(1L, await _db.ScalarAsync<long>($"SELECT COUNT(*) FROM {table}"));
        }
    }
}
