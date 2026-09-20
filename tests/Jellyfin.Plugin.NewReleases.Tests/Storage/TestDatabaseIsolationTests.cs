using Jellyfin.Plugin.NewReleases.Tests.Support;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Jellyfin.Plugin.NewReleases.Tests.Storage;

/// <summary>
/// The suite runs xunit collections in parallel, so one test's teardown must never disturb
/// another test's database. This is the regression test for an intermittent
/// <c>ObjectDisposedException: 'SQLitePCL.sqlite3'</c> that failed roughly one full run in ten,
/// caused by teardowns calling the process-global <c>SqliteConnection.ClearAllPools()</c>.
/// <para>
/// It asserts the property directly rather than racing for the symptom: an earlier version
/// hammered two databases from eight threads and could pass having done no work at all.
/// </para>
/// </summary>
[Collection(ProcessGlobalStateCollection.Name)]
public class TestDatabaseIsolationTests
{
    /// <summary>
    /// Disposing one database must leave another's pooled connection usable. Restoring
    /// <c>ClearAllPools()</c> in <see cref="TestDatabase.DisposeAsync"/> fails this.
    /// </summary>
    [Fact]
    public async Task DisposingOneDatabase_LeavesAnotherDatabasesPooledConnectionUsable()
    {
        await using var survivor = await TestDatabase.CreateAsync();

        // Put a connection in the survivor's pool, then return it there by closing it.
        await using (var warmed = await survivor.Database.OpenAsync(CancellationToken.None))
        {
            Assert.Equal(System.Data.ConnectionState.Open, warmed.State);
        }

        var throwaway = await TestDatabase.CreateAsync();
        await throwaway.DisposeAsync();

        // The pooled handle must still be alive. With a process-global clear it is disposed, and
        // Open() throws ObjectDisposedException from inside SQLitePCL.
        await using var afterOtherDisposal = await survivor.Database.OpenAsync(CancellationToken.None);
        await using var command = afterOtherDisposal.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM schema_version";

        Assert.NotNull(await command.ExecuteScalarAsync());
    }

    /// <summary>
    /// Each database owns its pool because each owns its file. If two ever shared a connection
    /// string, clearing one would legitimately reach the other and the fix above would be void.
    /// </summary>
    [Fact]
    public async Task TwoDatabases_DoNotShareAConnectionString()
    {
        await using var first = await TestDatabase.CreateAsync();
        await using var second = await TestDatabase.CreateAsync();

        Assert.NotEqual(first.Database.ConnectionString, second.Database.ConnectionString);
    }

    /// <summary>
    /// The one check that actually catches a regression here. The race itself cannot be pinned
    /// deterministically — after a global clear the pool is simply empty, so the next open makes
    /// a fresh connection and nothing throws; the fault needs a concurrent take, which is why it
    /// surfaced as a one-in-ten flake rather than a failing test. So the guard is on the call:
    /// <c>ClearAllPools()</c> is process-global and must not appear in this suite at all.
    /// </summary>
    [Fact]
    public void NoTestTeardown_CallsTheProcessGlobalClearAllPools()
    {
        // Built at runtime so this test's own source does not match the thing it forbids.
        const string GlobalClear = ".Clear" + "AllPools(";

        var offenders = Directory
            .EnumerateFiles(Path.Combine(RepositoryFiles.Root.FullName, "tests"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(path => File.ReadLines(path).Any(line =>
                line.Contains(GlobalClear, StringComparison.Ordinal)
                && !line.TrimStart().StartsWith("//", StringComparison.Ordinal)))
            .Select(path => Path.GetRelativePath(RepositoryFiles.Root.FullName, path))
            .ToList();

        Assert.Empty(offenders);
    }

    /// <summary>
    /// Clearing a pool must be addressable per connection string; this is the API the teardown
    /// relies on, and the guard against someone "simplifying" it back to ClearAllPools.
    /// </summary>
    [Fact]
    public async Task ClearingOnePool_IsScopedToItsOwnConnectionString()
    {
        await using var survivor = await TestDatabase.CreateAsync();
        await using (await survivor.Database.OpenAsync(CancellationToken.None))
        {
        }

        await using var other = await TestDatabase.CreateAsync();
        using (var otherHandle = new SqliteConnection(other.Database.ConnectionString))
        {
            SqliteConnection.ClearPool(otherHandle);
        }

        await using var stillUsable = await survivor.Database.OpenAsync(CancellationToken.None);

        Assert.Equal(System.Data.ConnectionState.Open, stillUsable.State);
    }
}
