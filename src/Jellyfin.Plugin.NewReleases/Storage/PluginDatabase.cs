using MediaBrowser.Common.Configuration;
using Microsoft.Data.Sqlite;

namespace Jellyfin.Plugin.NewReleases.Storage;

/// <summary>
/// Owns the plugin's private SQLite file: its path, connection string and the one-time schema
/// migration that runs lazily on first open. Repositories take this and call <see cref="OpenAsync"/>.
/// Named <c>PluginDatabase</c> because <c>Database</c> collides with the <c>Jellyfin.Database</c> namespace.
/// </summary>
public sealed class PluginDatabase
{
    private readonly IApplicationPaths _paths;

    public PluginDatabase(IApplicationPaths paths)
    {
        _paths = paths;
    }

    public string DirectoryPath => Path.Combine(_paths.DataPath, "newreleases");

    public string DatabasePath => Path.Combine(DirectoryPath, "newreleases.db");

    public string ConnectionString => $"Data Source={DatabasePath};Foreign Keys=True";

    public async Task<SqliteConnection> OpenAsync(CancellationToken ct)
    {
        Directory.CreateDirectory(DirectoryPath);
        var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync(ct).ConfigureAwait(false);
        return connection;
    }
}
