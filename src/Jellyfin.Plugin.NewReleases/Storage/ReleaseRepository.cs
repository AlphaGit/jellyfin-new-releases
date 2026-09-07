using System.Globalization;
using System.Text.Json;
using Jellyfin.Plugin.NewReleases.Matching;
using Jellyfin.Plugin.NewReleases.Model;
using Microsoft.Data.Sqlite;

namespace Jellyfin.Plugin.NewReleases.Storage;

/// <summary>Data access for <c>release</c>, <c>source_entry</c>, <c>edition</c> and the ownership columns. Parameterized SQL, no ORM.</summary>
public sealed class ReleaseRepository
{
    private readonly PluginDatabase _db;

    public ReleaseRepository(PluginDatabase db)
    {
        _db = db;
    }

    /// <summary>
    /// Merges one source's listing into the release identified by (artist, normalized title) (FR-006a) and
    /// records the source entry for this run. Returns the release id.
    /// </summary>
    public async Task<long> UpsertFromSourceAsync(long artistId, string source, CatalogueItem item, long runId, DateTimeOffset now, CancellationToken ct)
    {
        var normalizedTitle = TitleNormalizer.NormalizeAlbum(item.Title);
        var timestamp = now.ToString("O");
        await using var connection = await _db.OpenAsync(ct).ConfigureAwait(false);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(ct).ConfigureAwait(false);

        long releaseId;
        await using (var release = connection.CreateCommand())
        {
            release.Transaction = transaction;
            release.CommandText = """
                INSERT INTO release (library_artist_id, normalized_title, title, canonical_source, canonical_source_id, primary_type, secondary_types, release_date, first_seen_at, last_seen_at, ownership_state)
                VALUES (@artistId, @normalizedTitle, @title, @source, @sourceReleaseId, @primary, @secondaries, @date, @now, @now, 'Missing')
                ON CONFLICT (library_artist_id, normalized_title) DO UPDATE SET last_seen_at = excluded.last_seen_at
                RETURNING id
                """;
            release.Parameters.AddWithValue("@artistId", artistId);
            release.Parameters.AddWithValue("@normalizedTitle", normalizedTitle);
            release.Parameters.AddWithValue("@title", item.Title);
            release.Parameters.AddWithValue("@source", source);
            release.Parameters.AddWithValue("@sourceReleaseId", item.SourceReleaseId);
            release.Parameters.AddWithValue("@primary", item.PrimaryType.ToString());
            release.Parameters.AddWithValue("@secondaries", JsonSerializer.Serialize(item.SecondaryTypes.Select(t => t.ToString())));
            release.Parameters.AddWithValue("@date", (object?)item.Date ?? DBNull.Value);
            release.Parameters.AddWithValue("@now", timestamp);
            releaseId = (long)(await release.ExecuteScalarAsync(ct).ConfigureAwait(false))!;
        }

        await using (var stale = connection.CreateCommand())
        {
            // The same source release id may have moved to another title at the source; one entry per source id.
            stale.Transaction = transaction;
            stale.CommandText = "DELETE FROM source_entry WHERE source = @source AND source_release_id = @sourceReleaseId AND release_id <> @releaseId";
            stale.Parameters.AddWithValue("@source", source);
            stale.Parameters.AddWithValue("@sourceReleaseId", item.SourceReleaseId);
            stale.Parameters.AddWithValue("@releaseId", releaseId);
            await stale.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }

        await using (var entry = connection.CreateCommand())
        {
            entry.Transaction = transaction;
            entry.CommandText = """
                INSERT INTO source_entry (release_id, source, source_release_id, url, source_title, source_primary_type, source_secondary_types, source_date, last_seen_run_id)
                VALUES (@releaseId, @source, @sourceReleaseId, @url, @title, @primary, @secondaries, @date, @runId)
                ON CONFLICT (release_id, source) DO UPDATE SET
                    source_release_id = excluded.source_release_id,
                    url = excluded.url,
                    source_title = excluded.source_title,
                    source_primary_type = excluded.source_primary_type,
                    source_secondary_types = excluded.source_secondary_types,
                    source_date = excluded.source_date,
                    last_seen_run_id = excluded.last_seen_run_id
                """;
            entry.Parameters.AddWithValue("@releaseId", releaseId);
            entry.Parameters.AddWithValue("@source", source);
            entry.Parameters.AddWithValue("@sourceReleaseId", item.SourceReleaseId);
            entry.Parameters.AddWithValue("@url", item.Url);
            entry.Parameters.AddWithValue("@title", item.Title);
            entry.Parameters.AddWithValue("@primary", item.PrimaryType.ToString());
            entry.Parameters.AddWithValue("@secondaries", JsonSerializer.Serialize(item.SecondaryTypes.Select(t => t.ToString())));
            entry.Parameters.AddWithValue("@date", (object?)item.Date ?? DBNull.Value);
            entry.Parameters.AddWithValue("@runId", runId);
            await entry.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }

        await RecomputeCanonicalAsync(connection, transaction, releaseId, ct).ConfigureAwait(false);
        await transaction.CommitAsync(ct).ConfigureAwait(false);
        return releaseId;
    }

    /// <summary>Canonical identity, types and date come from the MusicBrainz entry when one exists, else from Deezer (R17).</summary>
    private static async Task RecomputeCanonicalAsync(SqliteConnection connection, SqliteTransaction transaction, long releaseId, CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            UPDATE release SET
                canonical_source = e.source,
                canonical_source_id = e.source_release_id,
                title = e.source_title,
                primary_type = COALESCE(e.source_primary_type, 'Other'),
                secondary_types = COALESCE(e.source_secondary_types, '[]'),
                release_date = e.source_date
            FROM (SELECT * FROM source_entry WHERE release_id = @id ORDER BY CASE source WHEN 'musicbrainz' THEN 0 ELSE 1 END LIMIT 1) AS e
            WHERE release.id = @id
            """;
        command.Parameters.AddWithValue("@id", releaseId);
        await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    public async Task<Release?> GetAsync(long releaseId, CancellationToken ct)
    {
        await using var connection = await _db.OpenAsync(ct).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = ReleaseColumns + " FROM release WHERE id = @id";
        command.Parameters.AddWithValue("@id", releaseId);
        await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
        return await reader.ReadAsync(ct).ConfigureAwait(false) ? ReadRelease(reader) : null;
    }

    private const string ReleaseColumns =
        "SELECT id, library_artist_id, normalized_title, title, canonical_source, canonical_source_id, primary_type, secondary_types, release_date, date_sort, " +
        "first_seen_at, last_seen_at, ownership_state, match_method, library_album_id, compared_edition_id, missing_tracks, ownership_checked_at";

    private static Release ReadRelease(SqliteDataReader r) => new(
        r.GetInt64(0),
        r.GetInt64(1),
        r.GetString(2),
        r.GetString(3),
        r.GetString(4),
        r.GetString(5),
        Enum.Parse<ReleaseType>(r.GetString(6)),
        (JsonSerializer.Deserialize<string[]>(r.GetString(7)) ?? []).Select(Enum.Parse<ReleaseType>).ToArray(),
        r.IsDBNull(8) ? null : r.GetString(8),
        r.IsDBNull(9) ? null : r.GetString(9),
        DateTimeOffset.Parse(r.GetString(10), CultureInfo.InvariantCulture),
        DateTimeOffset.Parse(r.GetString(11), CultureInfo.InvariantCulture),
        Enum.Parse<OwnershipState>(r.GetString(12)),
        r.IsDBNull(13) ? null : r.GetString(13),
        r.IsDBNull(14) ? null : Guid.Parse(r.GetString(14)),
        r.IsDBNull(15) ? null : r.GetInt64(15),
        JsonSerializer.Deserialize<string[]>(r.GetString(16)) ?? [],
        r.IsDBNull(17) ? null : DateTimeOffset.Parse(r.GetString(17), CultureInfo.InvariantCulture));
}
