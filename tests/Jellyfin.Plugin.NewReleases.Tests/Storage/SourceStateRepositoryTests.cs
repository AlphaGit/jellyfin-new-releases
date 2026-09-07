using Jellyfin.Plugin.NewReleases.Tests.Support;
using Xunit;

namespace Jellyfin.Plugin.NewReleases.Tests.Storage;

public sealed class SourceStateRepositoryTests : IAsyncLifetime
{
    private static readonly DateTimeOffset LateEvening = new(2026, 9, 6, 23, 59, 0, TimeSpan.Zero);
    private const string MusicBrainz = "musicbrainz";

    private readonly TimeProviderStub _clock = new(LateEvening);
    private TestDatabase _db = null!;

    public async Task InitializeAsync() => _db = await TestDatabase.CreateAsync(_clock);

    public async Task DisposeAsync() => await _db.DisposeAsync();

    [Fact]
    public async Task RecordCallAsync_IncrementsCallsToday_AndRestartsAtOneOnANewUtcDay()
    {
        await _db.SourceState.RecordCallAsync(MusicBrainz, CancellationToken.None);
        await _db.SourceState.RecordCallAsync(MusicBrainz, CancellationToken.None);
        Assert.Equal((2, new DateOnly(2026, 9, 6)), ((await _db.SourceState.GetAsync(MusicBrainz, CancellationToken.None))!.CallsToday, (await _db.SourceState.GetAsync(MusicBrainz, CancellationToken.None))!.CallsDay));

        _clock.Advance(TimeSpan.FromMinutes(2)); // 00:01 UTC next day
        await _db.SourceState.RecordCallAsync(MusicBrainz, CancellationToken.None);

        var state = (await _db.SourceState.GetAsync(MusicBrainz, CancellationToken.None))!;
        Assert.Equal((1, new DateOnly(2026, 9, 7)), (state.CallsToday, state.CallsDay));
    }

    [Fact]
    public async Task GetRemainingBudgetAsync_IsBudgetMinusCallsToday_NeverBelowZero()
    {
        Assert.Equal(3, await _db.SourceState.GetRemainingBudgetAsync(MusicBrainz, 3, CancellationToken.None));

        for (var i = 0; i < 4; i++)
        {
            await _db.SourceState.RecordCallAsync(MusicBrainz, CancellationToken.None);
        }

        Assert.Equal(0, await _db.SourceState.GetRemainingBudgetAsync(MusicBrainz, 3, CancellationToken.None));
        Assert.Equal(6, await _db.SourceState.GetRemainingBudgetAsync(MusicBrainz, 10, CancellationToken.None));

        _clock.Advance(TimeSpan.FromMinutes(2)); // new UTC day: yesterday's calls no longer count
        Assert.Equal(3, await _db.SourceState.GetRemainingBudgetAsync(MusicBrainz, 3, CancellationToken.None));
    }
}
