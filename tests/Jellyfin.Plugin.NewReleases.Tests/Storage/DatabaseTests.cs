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
