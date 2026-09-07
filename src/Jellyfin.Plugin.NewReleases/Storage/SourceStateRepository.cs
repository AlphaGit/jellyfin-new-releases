using System.Globalization;
using Jellyfin.Plugin.NewReleases.Model;
using Microsoft.Data.Sqlite;

namespace Jellyfin.Plugin.NewReleases.Storage;

/// <summary>Data access for <c>source_state</c> and <c>refresh_run</c>. The clock is injected so day rollover and cooldowns are testable.</summary>
public sealed class SourceStateRepository
{
    private readonly PluginDatabase _db;
    private readonly TimeProvider _clock;

    public SourceStateRepository(PluginDatabase db, TimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    /// <summary>Counts one request against today's (UTC) budget; the counter restarts when the stored day is not today (FR-011).</summary>
    public async Task RecordCallAsync(string source, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(_clock.GetUtcNow().UtcDateTime).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        await using var connection = await _db.OpenAsync(ct).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO source_state (source, calls_today, calls_day) VALUES (@source, 1, @today)
            ON CONFLICT (source) DO UPDATE SET
                calls_today = CASE WHEN source_state.calls_day = excluded.calls_day THEN source_state.calls_today + 1 ELSE 1 END,
                calls_day = excluded.calls_day
            """;
        command.Parameters.AddWithValue("@source", source);
        command.Parameters.AddWithValue("@today", today);
        await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Budget left today: budget minus today's calls, never negative. Yesterday's counter does not count.</summary>
    public async Task<int> GetRemainingBudgetAsync(string source, int dailyBudget, CancellationToken ct)
    {
        var state = await GetAsync(source, ct).ConfigureAwait(false);
        var today = DateOnly.FromDateTime(_clock.GetUtcNow().UtcDateTime);
        var spent = state is not null && state.CallsDay == today ? state.CallsToday : 0;
        return Math.Max(0, dailyBudget - spent);
    }

    /// <summary>One more consecutive failure; at <paramref name="threshold"/> the source cools down for <paramref name="cooldown"/> (FR-011).</summary>
    public async Task RecordFailureAsync(string source, string error, int threshold, TimeSpan cooldown, CancellationToken ct)
    {
        var now = _clock.GetUtcNow();
        await using var connection = await _db.OpenAsync(ct).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO source_state (source, consecutive_failures, cooldown_until, last_error)
            VALUES (@source, 1, CASE WHEN 1 >= @threshold THEN @cooldownUntil END, @error)
            ON CONFLICT (source) DO UPDATE SET
                consecutive_failures = source_state.consecutive_failures + 1,
                cooldown_until = CASE WHEN source_state.consecutive_failures + 1 >= @threshold THEN @cooldownUntil ELSE source_state.cooldown_until END,
                last_error = excluded.last_error
            """;
        command.Parameters.AddWithValue("@source", source);
        command.Parameters.AddWithValue("@threshold", threshold);
        command.Parameters.AddWithValue("@cooldownUntil", (now + cooldown).ToString("O"));
        command.Parameters.AddWithValue("@error", error);
        await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    /// <summary>A successful request closes the failure streak and any cooldown.</summary>
    public async Task RecordSuccessAsync(string source, CancellationToken ct)
    {
        await using var connection = await _db.OpenAsync(ct).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO source_state (source, consecutive_failures, cooldown_until, last_success_at) VALUES (@source, 0, NULL, @now)
            ON CONFLICT (source) DO UPDATE SET consecutive_failures = 0, cooldown_until = NULL, last_success_at = excluded.last_success_at
            """;
        command.Parameters.AddWithValue("@source", source);
        command.Parameters.AddWithValue("@now", _clock.GetUtcNow().ToString("O"));
        await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    /// <summary>True while <c>cooldown_until</c> lies strictly in the future.</summary>
    public async Task<bool> IsInCooldownAsync(string source, CancellationToken ct)
    {
        var state = await GetAsync(source, ct).ConfigureAwait(false);
        return state?.CooldownUntil is { } until && until > _clock.GetUtcNow();
    }

    public async Task<long> StartRunAsync(CancellationToken ct)
    {
        await using var connection = await _db.OpenAsync(ct).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO refresh_run (started_at) VALUES (@now) RETURNING id";
        command.Parameters.AddWithValue("@now", _clock.GetUtcNow().ToString("O"));
        return (long)(await command.ExecuteScalarAsync(ct).ConfigureAwait(false))!;
    }

    public async Task FinishRunAsync(long runId, int artistsProcessed, int releasesFound, int editionsFetched, int errors, string outcome, CancellationToken ct)
    {
        await using var connection = await _db.OpenAsync(ct).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE refresh_run SET ended_at = @now, artists_processed = @artists, releases_found = @releases,
                editions_fetched = @editions, errors = @errors, outcome = @outcome
            WHERE id = @id
            """;
        command.Parameters.AddWithValue("@id", runId);
        command.Parameters.AddWithValue("@now", _clock.GetUtcNow().ToString("O"));
        command.Parameters.AddWithValue("@artists", artistsProcessed);
        command.Parameters.AddWithValue("@releases", releasesFound);
        command.Parameters.AddWithValue("@editions", editionsFetched);
        command.Parameters.AddWithValue("@errors", errors);
        command.Parameters.AddWithValue("@outcome", outcome);
        await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    /// <summary>The run behind `lastRefreshedAt` (FR-015): latest with outcome Completed.</summary>
    public Task<RefreshRun?> GetLastCompletedRunAsync(CancellationToken ct) => QueryRunAsync("WHERE outcome = 'Completed' ORDER BY ended_at DESC, id DESC", ct);

    public Task<RefreshRun?> GetLatestRunAsync(CancellationToken ct) => QueryRunAsync("ORDER BY id DESC", ct);

    private async Task<RefreshRun?> QueryRunAsync(string tail, CancellationToken ct)
    {
        await using var connection = await _db.OpenAsync(ct).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, started_at, ended_at, artists_processed, releases_found, editions_fetched, errors, outcome FROM refresh_run " + tail + " LIMIT 1";
        await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
        return await reader.ReadAsync(ct).ConfigureAwait(false)
            ? new RefreshRun(
                reader.GetInt64(0),
                DateTimeOffset.Parse(reader.GetString(1), CultureInfo.InvariantCulture),
                Time(reader, 2),
                reader.GetInt32(3),
                reader.GetInt32(4),
                reader.GetInt32(5),
                reader.GetInt32(6),
                reader.IsDBNull(7) ? null : reader.GetString(7))
            : null;
    }

    public async Task<SourceState?> GetAsync(string source, CancellationToken ct)
    {
        await using var connection = await _db.OpenAsync(ct).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT source, consecutive_failures, cooldown_until, calls_today, calls_day, next_allowed_at, last_error, last_success_at FROM source_state WHERE source = @source";
        command.Parameters.AddWithValue("@source", source);
        await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
        return await reader.ReadAsync(ct).ConfigureAwait(false) ? ReadState(reader) : null;
    }

    private static SourceState ReadState(SqliteDataReader r) => new(
        r.GetString(0),
        r.GetInt32(1),
        Time(r, 2),
        r.GetInt32(3),
        r.IsDBNull(4) ? null : DateOnly.ParseExact(r.GetString(4), "yyyy-MM-dd", CultureInfo.InvariantCulture),
        Time(r, 5),
        r.IsDBNull(6) ? null : r.GetString(6),
        Time(r, 7));

    private static DateTimeOffset? Time(SqliteDataReader r, int ordinal)
        => r.IsDBNull(ordinal) ? null : DateTimeOffset.Parse(r.GetString(ordinal), CultureInfo.InvariantCulture);
}
