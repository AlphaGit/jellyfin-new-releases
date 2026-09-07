using System.Globalization;
using Jellyfin.Plugin.NewReleases.Model;
using Microsoft.Data.Sqlite;

namespace Jellyfin.Plugin.NewReleases.Storage;

/// <summary>Data access for <c>decision</c> — the Archive (FR-013, FR-016). Keyed by the natural release key so decisions survive a purge (R8).</summary>
public sealed class ArchiveRepository
{
    private readonly PluginDatabase _db;

    public ArchiveRepository(PluginDatabase db)
    {
        _db = db;
    }

    /// <summary>Ignore or Have it: one decision per release; a new decision replaces the old.</summary>
    public async Task SetAsync(string artistKey, string normalizedTitle, DecisionKind kind, Guid userId, DateTimeOffset now, CancellationToken ct)
    {
        await using var connection = await _db.OpenAsync(ct).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO decision (artist_key, normalized_title, kind, user_id, decided_at) VALUES (@key, @title, @kind, @user, @now)
            ON CONFLICT (artist_key, normalized_title) DO UPDATE SET kind = excluded.kind, user_id = excluded.user_id, decided_at = excluded.decided_at
            """;
        AddKey(command, artistKey, normalizedTitle);
        command.Parameters.AddWithValue("@kind", kind.ToString());
        command.Parameters.AddWithValue("@user", userId.ToString("D"));
        command.Parameters.AddWithValue("@now", now.ToString("O"));
        await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    public async Task<Decision?> GetAsync(string artistKey, string normalizedTitle, CancellationToken ct)
    {
        await using var connection = await _db.OpenAsync(ct).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT artist_key, normalized_title, kind, user_id, decided_at FROM decision WHERE artist_key = @key AND normalized_title = @title";
        AddKey(command, artistKey, normalizedTitle);
        await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
        return await reader.ReadAsync(ct).ConfigureAwait(false)
            ? new Decision(reader.GetString(0), reader.GetString(1), Enum.Parse<DecisionKind>(reader.GetString(2)), Guid.Parse(reader.GetString(3)), DateTimeOffset.Parse(reader.GetString(4), CultureInfo.InvariantCulture))
            : null;
    }

    private static void AddKey(SqliteCommand command, string artistKey, string normalizedTitle)
    {
        command.Parameters.AddWithValue("@key", artistKey);
        command.Parameters.AddWithValue("@title", normalizedTitle);
    }
}
