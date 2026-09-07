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
                release_date = COALESCE(e.source_date, (SELECT source_date FROM source_entry WHERE release_id = @id AND source_date IS NOT NULL ORDER BY source LIMIT 1))
            FROM (SELECT * FROM source_entry WHERE release_id = @id ORDER BY CASE source WHEN 'musicbrainz' THEN 0 ELSE 1 END LIMIT 1) AS e
            WHERE release.id = @id
            """;
        command.Parameters.AddWithValue("@id", releaseId);
        await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);

        // date_sort: partial dates padded with 00 so a year-only release sorts after that year's dated ones (R16).
        await using var sort = connection.CreateCommand();
        sort.Transaction = transaction;
        sort.CommandText = """
            UPDATE release SET date_sort = CASE length(release_date)
                WHEN 4 THEN release_date || '-00-00'
                WHEN 7 THEN release_date || '-00'
                ELSE release_date END
            WHERE id = @id
            """;
        sort.Parameters.AddWithValue("@id", releaseId);
        await sort.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// After a Complete fetch of (artist, source) in <paramref name="runId"/>: drops this pair's entries the run did not
    /// return and recomputes the canonical entry of the releases touched (FR-014).
    /// </summary>
    public async Task PruneEntriesAsync(long artistId, string source, long runId, CancellationToken ct)
    {
        await using var connection = await _db.OpenAsync(ct).ConfigureAwait(false);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(ct).ConfigureAwait(false);

        var touched = new List<long>();
        await using (var prune = connection.CreateCommand())
        {
            prune.Transaction = transaction;
            prune.CommandText = """
                DELETE FROM source_entry
                WHERE source = @source AND last_seen_run_id < @runId
                  AND release_id IN (SELECT id FROM release WHERE library_artist_id = @artistId)
                RETURNING release_id
                """;
            prune.Parameters.AddWithValue("@source", source);
            prune.Parameters.AddWithValue("@runId", runId);
            prune.Parameters.AddWithValue("@artistId", artistId);
            await using var reader = await prune.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await reader.ReadAsync(ct).ConfigureAwait(false))
            {
                touched.Add(reader.GetInt64(0));
            }
        }

        await using (var orphans = connection.CreateCommand())
        {
            orphans.Transaction = transaction;
            orphans.CommandText = "DELETE FROM release WHERE library_artist_id = @artistId AND NOT EXISTS (SELECT 1 FROM source_entry WHERE release_id = release.id)";
            orphans.Parameters.AddWithValue("@artistId", artistId);
            await orphans.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }

        foreach (var releaseId in touched)
        {
            await RecomputeCanonicalAsync(connection, transaction, releaseId, ct).ConfigureAwait(false);
        }

        await transaction.CommitAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// The list (or the Archive): joined rows in display order. Ordering is `date_sort` descending with undated rows
    /// last and title as tiebreak (R16). Read-time rules (types, released-since, state, Archive) are applied here.
    /// </summary>
    public async Task<IReadOnlyList<ListedRelease>> ListAsync(ReleaseFilter filter, CancellationToken ct)
    {
        await using var connection = await _db.OpenAsync(ct).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT r.id, a.name, a.jellyfin_id, a.library_ids, r.title, r.primary_type, r.secondary_types, r.release_date, r.date_sort,
                   r.ownership_state, r.missing_tracks, e.source, e.title,
                   a.artist_key, r.normalized_title, d.kind, d.user_id, d.decided_at,
                   (SELECT json_group_array(json_object('source', s.source, 'url', s.url))
                      FROM (SELECT source, url FROM source_entry WHERE release_id = r.id ORDER BY source) AS s) AS sources
            FROM release r
            JOIN library_artist a ON a.id = r.library_artist_id
            LEFT JOIN edition e ON e.id = r.compared_edition_id
            LEFT JOIN decision d ON d.artist_key = a.artist_key AND d.normalized_title = r.normalized_title
            WHERE r.ownership_state <> 'Owned'
            ORDER BY r.date_sort IS NULL, r.date_sort DESC, r.title
            """;

        var rows = new List<ListedRelease>();
        await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            var primary = Enum.Parse<ReleaseType>(reader.GetString(5));
            var secondaries = (JsonSerializer.Deserialize<string[]>(reader.GetString(6)) ?? []).Select(Enum.Parse<ReleaseType>).ToArray();
            if (!ReleaseTypeMapper.IsIncluded(primary, secondaries, filter.EnabledTypes))
            {
                continue;
            }

            var dateSort = reader.IsDBNull(8) ? null : reader.GetString(8);
            if (filter.ReleasedSince is { } since && dateSort is not null
                && string.CompareOrdinal(dateSort, since.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)) < 0)
            {
                continue; // "released since" never cuts undated releases (data-model: Included)
            }

            rows.Add(ReadListed(reader, primary, secondaries));
        }

        return rows;
    }

    private static ListedRelease ReadListed(SqliteDataReader r, ReleaseType primary, IReadOnlyList<ReleaseType> secondaries)
    {
        var ownership = Enum.Parse<OwnershipState>(r.GetString(9));
        var sources = (JsonSerializer.Deserialize<List<SourceLink>>(r.GetString(18), JsonWeb) ?? []).AsReadOnly();
        return new ListedRelease(
            r.GetInt64(0),
            r.GetString(1),
            Guid.Parse(r.GetString(2)),
            JsonSerializer.Deserialize<Guid[]>(r.GetString(3)) ?? [],
            r.GetString(4),
            ReleaseTypeMapper.DisplayType(primary, secondaries),
            r.IsDBNull(7) ? null : r.GetString(7),
            r.IsDBNull(8) ? null : r.GetString(8),
            ownership == OwnershipState.Incomplete ? ListState.Incomplete : ListState.Missing,
            JsonSerializer.Deserialize<string[]>(r.GetString(10)) ?? [],
            r.IsDBNull(11) ? null : new ComparedEdition(r.GetString(11), r.GetString(12)),
            sources,
            r.IsDBNull(15) ? null : new Decision(r.GetString(13), r.GetString(14), Enum.Parse<DecisionKind>(r.GetString(15)), Guid.Parse(r.GetString(16)), DateTimeOffset.Parse(r.GetString(17), CultureInfo.InvariantCulture)));
    }

    private static readonly JsonSerializerOptions JsonWeb = new(JsonSerializerDefaults.Web);

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
