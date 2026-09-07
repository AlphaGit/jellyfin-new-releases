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
    }

    public PluginDatabase Database { get; }

    public TimeProvider Clock { get; }

    public ArtistRepository Artists { get; }

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
