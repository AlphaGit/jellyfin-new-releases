using Jellyfin.Plugin.NewReleases.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.NewReleases.ScheduledTasks;

/// <summary>
/// Closes, at host start, every refresh run a restart left open (spec 009 FR-008, research R8). No refresh can be
/// running before the plugin has loaded, so every open run started before this instant was interrupted.
/// </summary>
public sealed class InterruptedRunCloser : IHostedService
{
    private readonly SourceStateRepository _sourceState;
    private readonly TimeProvider _clock;
    private readonly ILogger<InterruptedRunCloser> _logger;

    public InterruptedRunCloser(SourceStateRepository sourceState, TimeProvider clock, ILogger<InterruptedRunCloser> logger)
    {
        _sourceState = sourceState;
        _clock = clock;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _sourceState.CloseOpenRunsAsync(_clock.GetUtcNow(), cancellationToken).ConfigureAwait(false);
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19) // SQLITE_CONSTRAINT: an outcome check from before 009
        {
            _logger.LogWarning("Runs left open by a restart stay open: this database does not accept the outcome Interrupted.");
        }
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
