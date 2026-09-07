using Jellyfin.Plugin.NewReleases.Storage;
using MediaBrowser.Common.Configuration;
using Microsoft.Data.Sqlite;
using NSubstitute;

namespace Jellyfin.Plugin.NewReleases.Tests.Support;

/// <summary>
/// Real SQLite file in a unique temp directory with the repositories wired to it. Disposal deletes
/// the directory (constitution III). Pass a <see cref="TimeProvider"/> for clock-sensitive repositories.
/// </summary>
internal sealed class TestDatabase : IAsyncDisposable
{
    private readonly string _dataPath;

    private TestDatabase(string dataPath, PluginDatabase database, TimeProvider clock)
    {
        _dataPath = dataPath;
        Database = database;
        Clock = clock;
        Artists = new ArtistRepository(database);
        Releases = new ReleaseRepository(database);
        Archive = new ArchiveRepository(database);
    }

    public PluginDatabase Database { get; }

    public TimeProvider Clock { get; }

    public ArtistRepository Artists { get; }

    public ReleaseRepository Releases { get; }

    public ArchiveRepository Archive { get; }

    public static Task<TestDatabase> CreateAsync() => CreateAsync(TimeProvider.System);

    public static async Task<TestDatabase> CreateAsync(TimeProvider clock)
    {
        var dataPath = Path.Combine(Path.GetTempPath(), "nr_test_" + Guid.NewGuid().ToString("N"));
        var paths = Substitute.For<IApplicationPaths>();
        paths.DataPath.Returns(dataPath);
        var database = new PluginDatabase(paths);
        await using (await database.OpenAsync(CancellationToken.None))
        {
        }

        return new TestDatabase(dataPath, database, clock);
    }

    /// <summary>Runs raw SQL against the file to seed rows that no repository writes yet.</summary>
    public async Task ExecuteAsync(string sql)
    {
        await using var connection = await Database.OpenAsync(CancellationToken.None);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>Reads the first column of every row.</summary>
    public async Task<List<T>> ColumnAsync<T>(string sql)
    {
        await using var connection = await Database.OpenAsync(CancellationToken.None);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var result = new List<T>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add((T)Convert.ChangeType(reader.GetValue(0), typeof(T), System.Globalization.CultureInfo.InvariantCulture));
        }

        return result;
    }

    /// <summary>Runs a scalar query against the file for assertions that no repository exposes.</summary>
    public async Task<T?> ScalarAsync<T>(string sql)
    {
        await using var connection = await Database.OpenAsync(CancellationToken.None);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var value = await command.ExecuteScalarAsync();
        return value is null or DBNull ? default : (T)Convert.ChangeType(value, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
    }

    public ValueTask DisposeAsync()
    {
        SqliteConnection.ClearAllPools();
        try
        {
            Directory.Delete(_dataPath, recursive: true);
        }
        catch (IOException)
        {
            // best effort
        }

        return ValueTask.CompletedTask;
    }
}
