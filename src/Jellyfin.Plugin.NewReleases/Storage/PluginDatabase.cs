using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using MediaBrowser.Common.Configuration;
using Microsoft.Data.Sqlite;

namespace Jellyfin.Plugin.NewReleases.Storage;

/// <summary>
/// Owns the plugin's private SQLite file: its path, connection string and the one-time schema
/// migration that runs lazily on first open. Repositories take this and call <see cref="OpenAsync"/>.
/// Named <c>PluginDatabase</c> because <c>Database</c> collides with the <c>Jellyfin.Database</c> namespace.
/// </summary>
public sealed partial class PluginDatabase
{
    private const string MigrationPrefix = "Jellyfin.Plugin.NewReleases.Storage.Migrations.";

    [GeneratedRegex(@"\.(\d{3})_[^.]+\.sql$")]
    private static partial Regex MigrationVersion();

    private readonly IApplicationPaths _paths;

    // ponytail: migration errors surface on first use (task or API), not at server start (research R13).
    private readonly Lazy<Task> _migration;

    public PluginDatabase(IApplicationPaths paths)
    {
        _paths = paths;
        _migration = new Lazy<Task>(MigrateAsync);
    }

    /// <summary>How many times the migration routine has run on this instance; the test seam for the once-only guarantee.</summary>
    internal int MigrationRuns => _migrationRuns;

    private int _migrationRuns;

    public string DirectoryPath => Path.Combine(_paths.DataPath, "newreleases");

    public string DatabasePath => Path.Combine(DirectoryPath, "newreleases.db");

    public string ConnectionString => $"Data Source={DatabasePath};Foreign Keys=True";

    public async Task<SqliteConnection> OpenAsync(CancellationToken ct)
    {
        await _migration.Value.ConfigureAwait(false);
        return await OpenRawAsync(ct).ConfigureAwait(false);
    }

    private async Task<SqliteConnection> OpenRawAsync(CancellationToken ct)
    {
        Directory.CreateDirectory(DirectoryPath);
        var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync(ct).ConfigureAwait(false);
        return connection;
    }

    private async Task MigrateAsync()
    {
        Interlocked.Increment(ref _migrationRuns);
        await using var connection = await OpenRawAsync(CancellationToken.None).ConfigureAwait(false);
        await ExecuteAsync(connection, "CREATE TABLE IF NOT EXISTS schema_version (version INTEGER PRIMARY KEY, applied_at TEXT NOT NULL)").ConfigureAwait(false);

        await using var current = connection.CreateCommand();
        current.CommandText = "SELECT COALESCE(MAX(version), 0) FROM schema_version";
        var applied = Convert.ToInt32(await current.ExecuteScalarAsync().ConfigureAwait(false), CultureInfo.InvariantCulture);

        var assembly = typeof(PluginDatabase).Assembly;
        var pending = assembly.GetManifestResourceNames()
            .Select(name => (Name: name, Match: MigrationVersion().Match(name)))
            .Where(x => x.Name.StartsWith(MigrationPrefix, StringComparison.Ordinal) && x.Match.Success)
            .Select(x => (x.Name, Version: int.Parse(x.Match.Groups[1].Value, CultureInfo.InvariantCulture)))
            .Where(x => x.Version > applied)
            .OrderBy(x => x.Version);

        foreach (var (name, version) in pending)
        {
            await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync().ConfigureAwait(false);
            await ExecuteAsync(connection, ReadResource(assembly, name), transaction).ConfigureAwait(false);
            await using var record = connection.CreateCommand();
            record.Transaction = transaction;
            record.CommandText = "INSERT INTO schema_version (version, applied_at) VALUES (@version, @now)";
            record.Parameters.AddWithValue("@version", version);
            record.Parameters.AddWithValue("@now", DateTimeOffset.UtcNow.ToString("O"));
            await record.ExecuteNonQueryAsync().ConfigureAwait(false);
            await transaction.CommitAsync().ConfigureAwait(false);
        }
    }

    private static async Task ExecuteAsync(SqliteConnection connection, string sql, SqliteTransaction? transaction = null)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    private static string ReadResource(Assembly assembly, string name)
    {
        using var stream = assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException($"Missing embedded migration {name}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
