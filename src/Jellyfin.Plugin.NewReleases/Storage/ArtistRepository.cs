using System.Globalization;
using System.Text.Json;
using Jellyfin.Plugin.NewReleases.Library;
using Jellyfin.Plugin.NewReleases.Model;
using Microsoft.Data.Sqlite;

namespace Jellyfin.Plugin.NewReleases.Storage;

/// <summary>Data access for <c>library_artist</c> and <c>artist_source</c>. Parameterized SQL, no ORM.</summary>
public sealed class ArtistRepository
{
    private readonly PluginDatabase _db;

    public ArtistRepository(PluginDatabase db)
    {
        _db = db;
    }

    /// <summary>Inserts or updates by <c>artist_key</c>; the surrogate id is stable across upserts.</summary>
    public async Task<long> UpsertAsync(LibraryArtistSnapshot artist, CancellationToken ct)
    {
        await using var connection = await _db.OpenAsync(ct).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO library_artist (artist_key, jellyfin_id, name, mbid, library_ids, album_count)
            VALUES (@key, @jellyfinId, @name, @mbid, @libraryIds, @albumCount)
            ON CONFLICT (artist_key) DO UPDATE SET
                jellyfin_id = excluded.jellyfin_id,
                name = excluded.name,
                mbid = excluded.mbid,
                library_ids = excluded.library_ids,
                album_count = excluded.album_count
            RETURNING id
            """;
        command.Parameters.AddWithValue("@key", artist.ArtistKey);
        command.Parameters.AddWithValue("@jellyfinId", artist.JellyfinId.ToString("D"));
        command.Parameters.AddWithValue("@name", artist.Name);
        command.Parameters.AddWithValue("@mbid", (object?)artist.Mbid ?? DBNull.Value);
        command.Parameters.AddWithValue("@libraryIds", JsonSerializer.Serialize(artist.LibraryIds));
        command.Parameters.AddWithValue("@albumCount", artist.Albums.Count);
        return (long)(await command.ExecuteScalarAsync(ct).ConfigureAwait(false))!;
    }

    /// <summary>Deletes artists whose key is not in the snapshot; <c>ON DELETE CASCADE</c> removes their source, release, entry and edition rows (FR-014).</summary>
    public async Task DeleteMissingAsync(IReadOnlyCollection<string> presentKeys, CancellationToken ct)
    {
        await using var connection = await _db.OpenAsync(ct).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM library_artist WHERE artist_key NOT IN (SELECT value FROM json_each(@keys))";
        command.Parameters.AddWithValue("@keys", JsonSerializer.Serialize(presentKeys));
        await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Refresh order: never refreshed first, then oldest first, ties by name (edge case: rotation, no starvation).</summary>
    public Task<IReadOnlyList<LibraryArtist>> GetRotationAsync(CancellationToken ct)
        => QueryArtistsAsync("ORDER BY last_refreshed_at IS NOT NULL, last_refreshed_at, name", ct);

    public Task<IReadOnlyList<LibraryArtist>> GetAllAsync(CancellationToken ct) => QueryArtistsAsync("ORDER BY name", ct);

    private async Task<IReadOnlyList<LibraryArtist>> QueryArtistsAsync(string orderBy, CancellationToken ct)
    {
        await using var connection = await _db.OpenAsync(ct).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, artist_key, jellyfin_id, name, mbid, library_ids, album_count, last_refreshed_at FROM library_artist " + orderBy;
        var result = new List<LibraryArtist>();
        await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            result.Add(ReadArtist(reader));
        }

        return result;
    }

    private static LibraryArtist ReadArtist(SqliteDataReader reader) => new(
        reader.GetInt64(0),
        reader.GetString(1),
        Guid.Parse(reader.GetString(2)),
        reader.GetString(3),
        reader.IsDBNull(4) ? null : reader.GetString(4),
        JsonSerializer.Deserialize<Guid[]>(reader.GetString(5)) ?? [],
        reader.GetInt32(6),
        reader.IsDBNull(7) ? null : DateTimeOffset.Parse(reader.GetString(7), CultureInfo.InvariantCulture));
}
