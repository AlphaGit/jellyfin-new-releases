using Jellyfin.Plugin.NewReleases.ScheduledTasks;
using Jellyfin.Plugin.NewReleases.Tests.Support;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Plugin.NewReleases.Tests.ScheduledTasks;

/// <summary>009 User Story 4: a run left open by a restart is closed when the plugin starts.</summary>
public sealed class InterruptedRunCloserTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Restart = new(2026, 10, 10, 21, 30, 0, TimeSpan.Zero);

    private readonly TimeProviderStub _clock = new(Restart - TimeSpan.FromHours(2));
    private TestDatabase _db = null!;

    public async Task InitializeAsync() => _db = await TestDatabase.CreateAsync(_clock);

    public async Task DisposeAsync() => await _db.DisposeAsync();

    /// <summary>US4-AS1, AS3: the open run gets the start instant as its end and the outcome Interrupted.</summary>
    [Fact]
    public async Task StartAsync_ClosesTheOpenRun_WithTheStartInstant()
    {
        var run = await _db.SourceState.StartRunAsync(CancellationToken.None);
        _clock.Advance(TimeSpan.FromHours(2));

        await new InterruptedRunCloser(_db.SourceState, _clock, NullLogger<InterruptedRunCloser>.Instance).StartAsync(CancellationToken.None);

        var closed = (await _db.SourceState.GetLatestRunAsync(CancellationToken.None))!;
        Assert.Equal((run, "Interrupted", (DateTimeOffset?)Restart), (closed.Id, closed.Outcome, closed.EndedAt));
    }

    /// <summary>FR-008: an install whose outcome check predates Interrupted logs one warning and starts normally.</summary>
    [Fact]
    public async Task StartAsync_OnAnOldOutcomeCheck_LogsOneWarning_AndDoesNotThrow()
    {
        await _db.ExecuteAsync("""
            DROP TABLE refresh_run;
            CREATE TABLE refresh_run (
                id INTEGER PRIMARY KEY, started_at TEXT NOT NULL, ended_at TEXT,
                artists_processed INTEGER NOT NULL DEFAULT 0, releases_found INTEGER NOT NULL DEFAULT 0,
                editions_fetched INTEGER NOT NULL DEFAULT 0, errors INTEGER NOT NULL DEFAULT 0,
                outcome TEXT CHECK (outcome IN ('Completed', 'Cancelled', 'Failed')));
            """);
        await _db.SourceState.StartRunAsync(CancellationToken.None);
        _clock.Advance(TimeSpan.FromHours(2));
        var logger = new RecordingLogger<InterruptedRunCloser>();

        await new InterruptedRunCloser(_db.SourceState, _clock, logger).StartAsync(CancellationToken.None);

        Assert.Equal(LogLevel.Warning, Assert.Single(logger.Entries).Level);
        Assert.Null((await _db.SourceState.GetLatestRunAsync(CancellationToken.None))!.Outcome);
    }

    [Fact]
    public async Task StopAsync_DoesNothing()
    {
        await new InterruptedRunCloser(_db.SourceState, _clock, NullLogger<InterruptedRunCloser>.Instance).StopAsync(CancellationToken.None);
    }
}
