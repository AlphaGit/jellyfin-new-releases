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
