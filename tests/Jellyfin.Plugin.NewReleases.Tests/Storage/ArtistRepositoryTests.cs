using Jellyfin.Plugin.NewReleases.Library;
using Jellyfin.Plugin.NewReleases.Model;
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

    [Fact]
    public async Task GetRotationAsync_NeverRefreshedFirstThenOldestThenName()
    {
        var old = await _db.Artists.UpsertAsync(Artist("name:old", "Old"), CancellationToken.None);
        var recent = await _db.Artists.UpsertAsync(Artist("name:recent", "Recent"), CancellationToken.None);
        await _db.Artists.UpsertAsync(Artist("name:zed never", "Zed Never"), CancellationToken.None);
        await _db.Artists.UpsertAsync(Artist("name:amy never", "Amy Never"), CancellationToken.None);
        await _db.ExecuteAsync($"UPDATE library_artist SET last_refreshed_at = '2026-01-01T00:00:00Z' WHERE id = {old}; UPDATE library_artist SET last_refreshed_at = '2026-06-01T00:00:00Z' WHERE id = {recent};");

        var rotation = await _db.Artists.GetRotationAsync(CancellationToken.None);

        Assert.Equal(["Amy Never", "Zed Never", "Old", "Recent"], rotation.Select(a => a.Name));
    }

    [Fact]
    public async Task ArtistSource_UpsertStoresMatchAndOutcome_CompleteResetsOffset()
    {
        var now = new DateTimeOffset(2026, 9, 6, 3, 0, 0, TimeSpan.Zero);
        var id = await _db.Artists.UpsertAsync(Artist("name:a", "A"), CancellationToken.None);

        await _db.Artists.SetMatchAsync(id, "musicbrainz", ArtistMatch.Matched("mbid-1"), CancellationToken.None);
        await _db.Artists.SetFetchOutcomeAsync(id, "musicbrainz", FetchOutcome.Partial, resumeOffset: 100, error: null, now, CancellationToken.None);
        var partial = await _db.Artists.GetSourceStateAsync(id, "musicbrainz", CancellationToken.None);

        Assert.Equal(new ArtistSourceState(id, "musicbrainz", MatchStatus.Matched, "mbid-1", null, 100, FetchOutcome.Partial, null, null), partial);

        await _db.Artists.SetMatchAsync(id, "musicbrainz", ArtistMatch.Unmatched("no result"), CancellationToken.None);
        await _db.Artists.SetFetchOutcomeAsync(id, "musicbrainz", FetchOutcome.Complete, resumeOffset: 100, error: null, now, CancellationToken.None);
        var complete = await _db.Artists.GetSourceStateAsync(id, "musicbrainz", CancellationToken.None);

        Assert.Equal(new ArtistSourceState(id, "musicbrainz", MatchStatus.Unmatched, null, "no result", 0, FetchOutcome.Complete, now, null), complete);
    }

    [Fact]
    public async Task GetCountsAsync_ReportsTotalsMatchedPerSourceAndUnmatchedReasons()
    {
        var both = Artist("name:both", "Both");
        var mbOnly = Artist("name:mb only", "MB Only");
        var neither = Artist("name:neither", "Neither");
        var bothId = await _db.Artists.UpsertAsync(both, CancellationToken.None);
        var mbOnlyId = await _db.Artists.UpsertAsync(mbOnly, CancellationToken.None);
        var neitherId = await _db.Artists.UpsertAsync(neither, CancellationToken.None);
        await _db.Artists.SetMatchAsync(bothId, "musicbrainz", ArtistMatch.Matched("mb-1"), CancellationToken.None);
        await _db.Artists.SetMatchAsync(bothId, "deezer", ArtistMatch.Matched("dz-1"), CancellationToken.None);
        await _db.Artists.SetMatchAsync(mbOnlyId, "musicbrainz", ArtistMatch.Matched("mb-2"), CancellationToken.None);
        await _db.Artists.SetMatchAsync(mbOnlyId, "deezer", ArtistMatch.Unmatched("no corroborating album"), CancellationToken.None);
        await _db.Artists.SetMatchAsync(neitherId, "musicbrainz", ArtistMatch.Unmatched("ambiguous (score 90 vs 85)"), CancellationToken.None);
        await _db.Artists.SetMatchAsync(neitherId, "deezer", ArtistMatch.Unmatched("no result"), CancellationToken.None);

        var counts = await _db.Artists.GetCountsAsync(CancellationToken.None);

        Assert.Equal(3, counts.LibraryArtists);
        Assert.Equal(2, counts.MatchedBySource["musicbrainz"]);
        Assert.Equal(1, counts.MatchedBySource["deezer"]);
        // Records holding lists compare by reference; flatten to (artist, source, reason) rows.
        Assert.Equal(
            [
                (mbOnly.JellyfinId, "MB Only", "deezer", "no corroborating album"),
                (neither.JellyfinId, "Neither", "deezer", "no result"),
                (neither.JellyfinId, "Neither", "musicbrainz", "ambiguous (score 90 vs 85)"),
            ],
            counts.Unmatched.SelectMany(u => u.Sources.OrderBy(s => s.Source).Select(s => (u.JellyfinId, u.Name, s.Source, s.Reason))));
    }
}
