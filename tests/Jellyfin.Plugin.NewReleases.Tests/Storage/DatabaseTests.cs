using Jellyfin.Plugin.NewReleases.Storage;
using MediaBrowser.Common.Configuration;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.NewReleases.Tests.Storage;

/// <summary>Real SQLite file in a temp directory that the test deletes (constitution III).</summary>
public sealed class DatabaseTests : IDisposable
{
    private readonly string _dataPath = Path.Combine(Path.GetTempPath(), "nr_test_" + Guid.NewGuid().ToString("N"));

    private PluginDatabase NewDatabase()
    {
        var paths = Substitute.For<IApplicationPaths>();
        paths.DataPath.Returns(_dataPath);
        return new PluginDatabase(paths);
    }

    [Fact]
    public async Task OpenAsync_UsesNewReleasesFileUnderDataPathAndCreatesTheDirectory()
    {
        var db = NewDatabase();

        Assert.Equal(Path.Combine(_dataPath, "newreleases", "newreleases.db"), db.DatabasePath);

        await using var _ = await db.OpenAsync(CancellationToken.None);

        Assert.True(Directory.Exists(Path.Combine(_dataPath, "newreleases")));
    }

    /// <summary>The newest embedded migration: 003_source_entry_credit (spec 009). A fresh database ends at it.</summary>
    private const long LatestSchema = 3;

    private static readonly string[] SchemaTables =
        ["library_artist", "artist_source", "release", "source_entry", "edition", "decision", "source_state", "refresh_run", "schema_version"];

    [Fact]
    public async Task OpenAsync_FirstOpenAppliesTheInitialMigration()
    {
        await using var connection = await NewDatabase().OpenAsync(CancellationToken.None);

        await using var tables = connection.CreateCommand();
        tables.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table'";
        var names = new List<string>();
        await using (var reader = await tables.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                names.Add(reader.GetString(0));
            }
        }

        Assert.Superset(SchemaTables.ToHashSet(), names.ToHashSet());

        await using var version = connection.CreateCommand();
        version.CommandText = "SELECT MAX(version) FROM schema_version";
        Assert.Equal(LatestSchema, await version.ExecuteScalarAsync());
    }

    [Fact]
    public async Task OpenAsync_SecondOpenFromAFreshInstanceAppliesNothing()
    {
        await using (await NewDatabase().OpenAsync(CancellationToken.None))
        {
        }

        await using var connection = await NewDatabase().OpenAsync(CancellationToken.None);

        Assert.Equal(LatestSchema, await CountAsync(connection, "schema_version"));
    }

    [Fact]
    public async Task OpenAsync_TwoConcurrentFirstOpensRunTheMigrationOnce()
    {
        var db = NewDatabase();

        // Task.Run: the SQLite driver is synchronous, so two plain awaits would never overlap.
        var connections = await Task.WhenAll(
            Enumerable.Range(0, 4).Select(_ => Task.Run(() => db.OpenAsync(CancellationToken.None))));

        Assert.Equal(1, db.MigrationRuns);
        Assert.Equal(LatestSchema, await CountAsync(connections[0], "schema_version"));
        foreach (var connection in connections)
        {
            await connection.DisposeAsync();
        }
    }

    /// <summary>
    /// 008 / constitution IV: a database left by a released version (schema 001, every release so far)
    /// migrates forward, keeping its rows; the new column starts empty.
    /// </summary>
    [Fact]
    public async Task OpenAsync_ADatabaseAtSchema001_MigratesTo002_KeepingItsArtistSourceRows()
    {
        var db = NewDatabase();
        Directory.CreateDirectory(Path.GetDirectoryName(db.DatabasePath)!);
        await using (var old = new Microsoft.Data.Sqlite.SqliteConnection(db.ConnectionString))
        {
            await old.OpenAsync();
            using var reader = new StreamReader(typeof(PluginDatabase).Assembly.GetManifestResourceStream("Jellyfin.Plugin.NewReleases.Storage.Migrations.001_initial.sql")!);
            await using var setup = old.CreateCommand();
            setup.CommandText = await reader.ReadToEndAsync() + """
                CREATE TABLE schema_version (version INTEGER PRIMARY KEY, applied_at TEXT NOT NULL);
                INSERT INTO schema_version VALUES (1, '2026-09-06T00:00:00Z');
                INSERT INTO library_artist (id, artist_key, jellyfin_id, name, library_ids, album_count) VALUES (1, 'name:blur', '00000000-0000-0000-0000-000000000001', 'Blur', '[]', 1);
                INSERT INTO artist_source (library_artist_id, source, status, unmatched_reason) VALUES (1, 'musicbrainz', 'Unmatched', 'low score (70)');
                """;
            await setup.ExecuteNonQueryAsync();
        }

        await using var connection = await db.OpenAsync(CancellationToken.None);
        await using var row = connection.CreateCommand();
        row.CommandText = "SELECT unmatched_reason, match_detail IS NULL, (SELECT MAX(version) FROM schema_version) FROM artist_source";
        await using var read = await row.ExecuteReaderAsync();
        Assert.True(await read.ReadAsync());

        Assert.Equal(("low score (70)", 1L, LatestSchema), (read.GetString(0), read.GetInt64(1), read.GetInt64(2)));
    }

    /// <summary>009 data-model: a database at schema 002 migrates to 003; an existing entry reads an empty credit.</summary>
    [Fact]
    public async Task OpenAsync_ADatabaseAtSchema002_MigratesTo003_ExistingEntriesHaveAnEmptyCredit()
    {
        var db = NewDatabase();
        Directory.CreateDirectory(Path.GetDirectoryName(db.DatabasePath)!);
        await using (var old = new Microsoft.Data.Sqlite.SqliteConnection(db.ConnectionString))
        {
            await old.OpenAsync();
            string Script(string name)
            {
                using var reader = new StreamReader(typeof(PluginDatabase).Assembly.GetManifestResourceStream($"Jellyfin.Plugin.NewReleases.Storage.Migrations.{name}")!);
                return reader.ReadToEnd();
            }

            await using var setup = old.CreateCommand();
            setup.CommandText = Script("001_initial.sql") + Script("002_match_detail.sql") + """
                CREATE TABLE schema_version (version INTEGER PRIMARY KEY, applied_at TEXT NOT NULL);
                INSERT INTO schema_version VALUES (1, '2026-09-06T00:00:00Z'), (2, '2026-10-01T00:00:00Z');
                INSERT INTO library_artist (id, artist_key, jellyfin_id, name, library_ids, album_count) VALUES (1, 'name:blur', '00000000-0000-0000-0000-000000000001', 'Blur', '[]', 1);
                INSERT INTO release (id, library_artist_id, normalized_title, title, canonical_source, canonical_source_id, primary_type, secondary_types, first_seen_at, last_seen_at, ownership_state)
                    VALUES (1, 1, 'parklife', 'Parklife', 'musicbrainz', 'rg-1', 'Album', '[]', '2026-10-01T00:00:00Z', '2026-10-01T00:00:00Z', 'Missing');
                INSERT INTO source_entry (release_id, source, source_release_id, url, source_title, source_primary_type, source_secondary_types, last_seen_run_id)
                    VALUES (1, 'musicbrainz', 'rg-1', 'https://musicbrainz.org/release-group/rg-1', 'Parklife', 'Album', '[]', 1);
                """;
            await setup.ExecuteNonQueryAsync();
        }

        await using var connection = await db.OpenAsync(CancellationToken.None);
        await using var row = connection.CreateCommand();
        row.CommandText = "SELECT credit, (SELECT MAX(version) FROM schema_version) FROM source_entry";
        await using var read = await row.ExecuteReaderAsync();
        Assert.True(await read.ReadAsync());

        Assert.Equal(("[]", 3L), (read.GetString(0), read.GetInt64(1)));
    }

    private static async Task<long> CountAsync(Microsoft.Data.Sqlite.SqliteConnection connection, string table)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM {table}";
        return (long)(await command.ExecuteScalarAsync())!;
    }

    public void Dispose()
    {
        SqliteConnectionPoolReset();
        if (Directory.Exists(_dataPath))
        {
            Directory.Delete(_dataPath, recursive: true);
        }
    }

    // Pooled connections keep the file open on Windows; harmless elsewhere. Scoped to this
    // class's own database file: `ClearAllPools()` is process-global and xunit runs collections
    // in parallel, so it could dispose a pooled handle another test was opening.
    private void SqliteConnectionPoolReset()
    {
        using var pooled = new Microsoft.Data.Sqlite.SqliteConnection(NewDatabase().ConnectionString);
        Microsoft.Data.Sqlite.SqliteConnection.ClearPool(pooled);
    }
}
