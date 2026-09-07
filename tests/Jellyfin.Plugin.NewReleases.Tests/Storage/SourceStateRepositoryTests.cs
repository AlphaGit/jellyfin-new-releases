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
}
