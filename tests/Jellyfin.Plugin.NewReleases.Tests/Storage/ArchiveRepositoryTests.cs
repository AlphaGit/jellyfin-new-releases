using Jellyfin.Plugin.NewReleases.Model;
using Jellyfin.Plugin.NewReleases.Tests.Support;
using Xunit;

namespace Jellyfin.Plugin.NewReleases.Tests.Storage;

public sealed class ArchiveRepositoryTests : IAsyncLifetime
{
    private static readonly Guid Alice = Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111");
    private static readonly Guid Bob = Guid.Parse("bbbbbbbb-2222-2222-2222-222222222222");
    private static readonly DateTimeOffset T1 = new(2026, 9, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset T2 = new(2026, 9, 2, 10, 0, 0, TimeSpan.Zero);

    private TestDatabase _db = null!;

    public async Task InitializeAsync() => _db = await TestDatabase.CreateAsync();

    public async Task DisposeAsync() => await _db.DisposeAsync();

    [Fact]
    public async Task SetAsync_InsertsThenReplacesKindUserAndTime()
    {
        await _db.Archive.SetAsync("name:a", "album", DecisionKind.Ignore, Alice, T1, CancellationToken.None);
        Assert.Equal(new Decision("name:a", "album", DecisionKind.Ignore, Alice, T1), await _db.Archive.GetAsync("name:a", "album", CancellationToken.None));

        await _db.Archive.SetAsync("name:a", "album", DecisionKind.HaveIt, Bob, T2, CancellationToken.None);

        Assert.Equal(new Decision("name:a", "album", DecisionKind.HaveIt, Bob, T2), await _db.Archive.GetAsync("name:a", "album", CancellationToken.None));
        Assert.Equal(1L, await _db.ScalarAsync<long>("SELECT COUNT(*) FROM decision"));
    }

    [Fact]
    public async Task RemoveAsync_DeletesOnlyThatKey_ClearAsync_DeletesAll()
    {
        await _db.Archive.SetAsync("name:a", "one", DecisionKind.Ignore, Alice, T1, CancellationToken.None);
        await _db.Archive.SetAsync("name:a", "two", DecisionKind.HaveIt, Alice, T1, CancellationToken.None);
        await _db.Archive.SetAsync("name:b", "one", DecisionKind.Ignore, Bob, T1, CancellationToken.None);

        await _db.Archive.RemoveAsync("name:a", "one", CancellationToken.None);

        Assert.Null(await _db.Archive.GetAsync("name:a", "one", CancellationToken.None));
        Assert.NotNull(await _db.Archive.GetAsync("name:a", "two", CancellationToken.None));
        Assert.NotNull(await _db.Archive.GetAsync("name:b", "one", CancellationToken.None));

        await _db.Archive.ClearAsync(CancellationToken.None);

        Assert.Equal(0L, await _db.ScalarAsync<long>("SELECT COUNT(*) FROM decision"));
    }
}
