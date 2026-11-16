using Jellyfin.Plugin.NewReleases.Model;
using Jellyfin.Plugin.NewReleases.Tests.Support;
using Xunit;

namespace Jellyfin.Plugin.NewReleases.Tests.Storage;

public sealed class UnfollowedArtistRepositoryTests : IAsyncLifetime
{
    private static readonly Guid Alice = Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111");
    private static readonly Guid Bob = Guid.Parse("bbbbbbbb-2222-2222-2222-222222222222");
    private static readonly DateTimeOffset T1 = new(2026, 11, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset T2 = new(2026, 11, 16, 10, 0, 0, TimeSpan.Zero);

    private TestDatabase _db = null!;

    public async Task InitializeAsync() => _db = await TestDatabase.CreateAsync();

    public async Task DisposeAsync() => await _db.DisposeAsync();

    [Fact]
    public async Task SetAsync_InsertsThenReplacesUserAndTime_LastWriteWins()
    {
        await _db.Unfollowed.SetAsync("name:a", Alice, T1, CancellationToken.None);
        await _db.Unfollowed.SetAsync("name:a", Bob, T2, CancellationToken.None);

        Assert.Equal(1L, await _db.ScalarAsync<long>("SELECT COUNT(*) FROM unfollowed_artist"));
        Assert.Equal(Bob.ToString("D"), await _db.ScalarAsync<string>("SELECT user_id FROM unfollowed_artist WHERE artist_key = 'name:a'"));
    }

    [Fact]
    public async Task GetKeysAsync_ReturnsEveryKey_RemoveAsyncDeletesOnlyThatKey_ClearAsyncDeletesAll()
    {
        await _db.Unfollowed.SetAsync("name:a", Alice, T1, CancellationToken.None);
        await _db.Unfollowed.SetAsync("name:b", Alice, T1, CancellationToken.None);
        Assert.Equal(new[] { "name:a", "name:b" }, (await _db.Unfollowed.GetKeysAsync(CancellationToken.None)).Order());

        await _db.Unfollowed.RemoveAsync("name:a", CancellationToken.None);
        await _db.Unfollowed.RemoveAsync("name:never", CancellationToken.None); // already followed: nothing happens

        Assert.Equal(new[] { "name:b" }, await _db.Unfollowed.GetKeysAsync(CancellationToken.None));

        await _db.Unfollowed.ClearAsync(CancellationToken.None);

        Assert.Empty(await _db.Unfollowed.GetKeysAsync(CancellationToken.None));
    }

    /// <summary>FR-014, FR-015: the row has no link to the library artist, the releases or the Archive, so none of these removes it.</summary>
    [Fact]
    public async Task ARow_SurvivesTheArtistLeavingTheLibrary_PurgeReleaseData_AndClearArchive()
    {
        var snapshot = ArtistRepositoryTests.Artist("name:gone", "Gone");
        await _db.Artists.UpsertAsync(snapshot, CancellationToken.None);
        await _db.Unfollowed.SetAsync("name:gone", Alice, T1, CancellationToken.None);
        await _db.Archive.SetAsync("name:gone", "album", DecisionKind.Ignore, Alice, T1, CancellationToken.None);

        await _db.Releases.PurgeAsync(CancellationToken.None);
        await _db.Archive.ClearAsync(CancellationToken.None);
        await _db.Artists.DeleteMissingAsync([], CancellationToken.None);

        Assert.Empty(await _db.Artists.GetAllAsync(CancellationToken.None));
        Assert.Equal(new[] { "name:gone" }, await _db.Unfollowed.GetKeysAsync(CancellationToken.None));

        // The artist returns: it is the same key, so it is unfollowed again with no action (FR-014).
        await _db.Artists.UpsertAsync(snapshot, CancellationToken.None);
        Assert.Contains("name:gone", await _db.Unfollowed.GetKeysAsync(CancellationToken.None));
    }
}
