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

    /// <summary>Records the match result for (artist, source); creates the row when missing.</summary>
    public async Task SetMatchAsync(long artistId, string source, ArtistMatch match, CancellationToken ct)
    {
        await using var connection = await _db.OpenAsync(ct).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO artist_source (library_artist_id, source, status, source_artist_id, unmatched_reason)
            VALUES (@artistId, @source, @status, @sourceArtistId, @reason)
            ON CONFLICT (library_artist_id, source) DO UPDATE SET
                status = excluded.status,
                source_artist_id = excluded.source_artist_id,
                unmatched_reason = excluded.unmatched_reason
            """;
        command.Parameters.AddWithValue("@artistId", artistId);
        command.Parameters.AddWithValue("@source", source);
        command.Parameters.AddWithValue("@status", match.Status.ToString());
        command.Parameters.AddWithValue("@sourceArtistId", (object?)match.SourceArtistId ?? DBNull.Value);
        command.Parameters.AddWithValue("@reason", (object?)match.Reason ?? DBNull.Value);
        await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Records a catalogue fetch outcome. <see cref="FetchOutcome.Complete"/> resets the resume offset and stamps <c>last_complete_at</c>.</summary>
    public async Task SetFetchOutcomeAsync(long artistId, string source, FetchOutcome outcome, int resumeOffset, string? error, DateTimeOffset now, CancellationToken ct)
    {
        var complete = outcome == FetchOutcome.Complete;
        await using var connection = await _db.OpenAsync(ct).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO artist_source (library_artist_id, source, status, resume_offset, last_outcome, last_complete_at, last_error)
            VALUES (@artistId, @source, 'Pending', @offset, @outcome, @completeAt, @error)
            ON CONFLICT (library_artist_id, source) DO UPDATE SET
                resume_offset = excluded.resume_offset,
                last_outcome = excluded.last_outcome,
                last_complete_at = COALESCE(excluded.last_complete_at, artist_source.last_complete_at),
                last_error = excluded.last_error
            """;
        command.Parameters.AddWithValue("@artistId", artistId);
        command.Parameters.AddWithValue("@source", source);
        command.Parameters.AddWithValue("@offset", complete ? 0 : resumeOffset);
        command.Parameters.AddWithValue("@outcome", outcome.ToString());
        command.Parameters.AddWithValue("@completeAt", complete ? now.ToString("O") : DBNull.Value);
        command.Parameters.AddWithValue("@error", (object?)error ?? DBNull.Value);
        await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    public async Task<ArtistSourceState?> GetSourceStateAsync(long artistId, string source, CancellationToken ct)
    {
        await using var connection = await _db.OpenAsync(ct).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT library_artist_id, source, status, source_artist_id, unmatched_reason, resume_offset, last_outcome, last_complete_at, last_error
            FROM artist_source WHERE library_artist_id = @artistId AND source = @source
            """;
        command.Parameters.AddWithValue("@artistId", artistId);
        command.Parameters.AddWithValue("@source", source);
        await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
        return await reader.ReadAsync(ct).ConfigureAwait(false) ? ReadSourceState(reader) : null;
    }

    private static ArtistSourceState ReadSourceState(SqliteDataReader reader) => new(
        reader.GetInt64(0),
        reader.GetString(1),
        Enum.Parse<MatchStatus>(reader.GetString(2)),
        reader.IsDBNull(3) ? null : reader.GetString(3),
        reader.IsDBNull(4) ? null : reader.GetString(4),
        reader.GetInt32(5),
        reader.IsDBNull(6) ? null : Enum.Parse<FetchOutcome>(reader.GetString(6)),
        reader.IsDBNull(7) ? null : DateTimeOffset.Parse(reader.GetString(7), CultureInfo.InvariantCulture),
        reader.IsDBNull(8) ? null : reader.GetString(8));

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
