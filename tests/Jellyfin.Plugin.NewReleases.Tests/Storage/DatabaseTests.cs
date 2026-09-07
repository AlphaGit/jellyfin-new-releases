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
        Assert.Equal(1L, await version.ExecuteScalarAsync());
    }

    [Fact]
    public async Task OpenAsync_SecondOpenFromAFreshInstanceAppliesNothing()
    {
        await using (await NewDatabase().OpenAsync(CancellationToken.None))
        {
        }

        await using var connection = await NewDatabase().OpenAsync(CancellationToken.None);

        Assert.Equal(1L, await CountAsync(connection, "schema_version"));
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

    // Pooled connections keep the file open on Windows; harmless elsewhere.
    private static void SqliteConnectionPoolReset() => Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
}
