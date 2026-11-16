using System.Globalization;
using Microsoft.Data.Sqlite;

namespace Jellyfin.Plugin.NewReleases.Storage;

/// <summary>
/// Data access for <c>unfollowed_artist</c> (spec 011). An artist is followed unless it has a row. Keyed by the artist key, with no
/// link to <c>library_artist</c>, so a row outlives the artist's presence in the library, a purge and a Clear Archive (data-model).
/// </summary>
public sealed class UnfollowedArtistRepository
{
    private readonly PluginDatabase _db;

    public UnfollowedArtistRepository(PluginDatabase db)
    {
        _db = db;
    }

    /// <summary>Unfollow: one row per artist; a second call replaces the user and time (last write wins).</summary>
    public async Task SetAsync(string artistKey, Guid userId, DateTimeOffset now, CancellationToken ct)
    {
        await using var connection = await _db.OpenAsync(ct).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO unfollowed_artist (artist_key, user_id, decided_at) VALUES (@key, @user, @now)
            ON CONFLICT (artist_key) DO UPDATE SET user_id = excluded.user_id, decided_at = excluded.decided_at
            """;
        command.Parameters.AddWithValue("@key", artistKey);
        command.Parameters.AddWithValue("@user", userId.ToString("D"));
        command.Parameters.AddWithValue("@now", now.ToString("O", CultureInfo.InvariantCulture));
        await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Follow: deletes the row. Nothing happens for an artist that is already followed.</summary>
    public async Task RemoveAsync(string artistKey, CancellationToken ct)
    {
        await using var connection = await _db.OpenAsync(ct).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM unfollowed_artist WHERE artist_key = @key";
        command.Parameters.AddWithValue("@key", artistKey);
        await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Clear unfollowed artists: deletes every row, including those of artists that are not in the library now.</summary>
    public async Task ClearAsync(CancellationToken ct)
    {
        await using var connection = await _db.OpenAsync(ct).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM unfollowed_artist";
        await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    /// <summary>The key of every unfollowed artist; Refresh reads it once per run.</summary>
    public async Task<IReadOnlySet<string>> GetKeysAsync(CancellationToken ct)
    {
        await using var connection = await _db.OpenAsync(ct).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT artist_key FROM unfollowed_artist";
        var keys = new HashSet<string>(StringComparer.Ordinal);
        await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            keys.Add(reader.GetString(0));
        }

        return keys;
    }
}
