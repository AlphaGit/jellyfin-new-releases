using Jellyfin.Plugin.NewReleases.Tests.Support;
using Xunit;

namespace Jellyfin.Plugin.NewReleases.Tests.Storage;

/// <summary>
/// The suite runs xunit collections in parallel, so one test's teardown must never disturb
/// another test's open database. This pins that: it is the regression test for an intermittent
/// <c>ObjectDisposedException: 'SQLitePCL.sqlite3'</c> seen roughly once in ten full runs.
/// </summary>
public class TestDatabaseIsolationTests
{
    /// <summary>
    /// Disposing one <see cref="TestDatabase"/> must not break a different one that is in use.
    /// </summary>
    [Fact]
    public async Task DisposingOneDatabase_DoesNotDisturbAnotherThatIsStillOpening()
    {
        await using var inUse = await TestDatabase.CreateAsync();
        var keepUsing = true;
        Exception? observed = null;

        var users = Enumerable.Range(0, 8).Select(_ => Task.Run(async () =>
        {
            while (Volatile.Read(ref keepUsing))
            {
                try
                {
                    await using var connection = await inUse.Database.OpenAsync(CancellationToken.None);
                    await using var command = connection.CreateCommand();
                    command.CommandText = "SELECT COUNT(*) FROM schema_version";
                    await command.ExecuteScalarAsync();
                }
                catch (Exception ex)
                {
                    observed = ex;
                    return;
                }
            }
        })).ToArray();

        for (var i = 0; i < 300 && observed is null; i++)
        {
            var throwaway = await TestDatabase.CreateAsync();
            await throwaway.DisposeAsync();
        }

        Volatile.Write(ref keepUsing, false);
        await Task.WhenAll(users);

        Assert.Null(observed);
    }
}
