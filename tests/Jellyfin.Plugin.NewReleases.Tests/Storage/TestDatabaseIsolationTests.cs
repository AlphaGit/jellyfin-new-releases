using Jellyfin.Plugin.NewReleases.Tests.Support;
using Xunit;

namespace Jellyfin.Plugin.NewReleases.Tests.Storage;

/// <summary>
/// Guards the fix for an intermittent <c>ObjectDisposedException: 'SQLitePCL.sqlite3'</c> that
/// failed roughly one full run in ten: two teardowns called the process-global
/// <c>SqliteConnection.ClearAllPools()</c> while xunit runs collections in parallel, so one
/// test's teardown could dispose a pooled handle another test was opening.
/// <para>
/// <b>The race itself cannot be pinned by a test.</b> After a global clear the pool is merely
/// empty and the next open builds a fresh connection, so nothing throws; the fault needs a
/// concurrent take. Two earlier attempts at a behavioural guard passed identically before and
/// after the fix and were removed rather than kept as false assurance. What is left guards the
/// call, which is the thing that can actually be reintroduced.
/// </para>
/// </summary>
[Collection(ProcessGlobalStateCollection.Name)]
public class TestDatabaseIsolationTests
{
    /// <summary>
    /// U37: no teardown may call the process-global pool clear. Fails the moment a teardown is
    /// "simplified" back to it, which is exactly how the flake was introduced.
    /// </summary>
    [Fact]
    public void NoSourceFile_CallsTheProcessGlobalPoolClear()
    {
        Assert.Empty(CallersOfTheGlobalClear());
    }

    /// <summary>
    /// U42: each database owns its pool because each owns its file. If two ever shared a
    /// connection string, clearing one would legitimately reach the other and the scoping the
    /// fix relies on would be void.
    /// </summary>
    [Fact]
    public async Task TwoDatabases_DoNotShareAConnectionString()
    {
        await using var first = await TestDatabase.CreateAsync();
        await using var second = await TestDatabase.CreateAsync();

        Assert.NotEqual(first.Database.ConnectionString, second.Database.ConnectionString);
    }

    /// <summary>
    /// Scans source rather than behaviour, deliberately — see the class remark. Normalises
    /// whitespace and strips comments so a reformatted or aliased call cannot slip past, and
    /// covers <c>src/</c> as well as <c>tests/</c>: the call is process-global either way.
    /// </summary>
    private static IReadOnlyList<string> CallersOfTheGlobalClear()
    {
        // Assembled at runtime so this file does not match the thing it forbids.
        var method = "Clear" + "AllPools";
        var offenders = new List<string>();

        foreach (var root in new[] { "src", "tests" })
        {
            var directory = Path.Combine(RepositoryFiles.Root.FullName, root);
            foreach (var path in Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
            {
                if (path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                {
                    continue;
                }

                var code = StripComments(File.ReadAllText(path));

                // Preceded by anything that is not an identifier character, so `.Clear…(`,
                // a bare `Clear…()` behind a `using static`, and a call split across lines all
                // match — while a *method name* ending in those letters does not.
                if (System.Text.RegularExpressions.Regex.IsMatch(
                        code, $@"(?<![A-Za-z0-9_]){method}\s*\("))
                {
                    offenders.Add(Path.GetRelativePath(RepositoryFiles.Root.FullName, path));
                }
            }
        }

        return offenders;
    }

    private static string StripComments(string code)
    {
        var withoutBlocks = System.Text.RegularExpressions.Regex.Replace(
            code, @"/\*.*?\*/", string.Empty, System.Text.RegularExpressions.RegexOptions.Singleline);

        return string.Join(
            '\n',
            withoutBlocks.Split('\n').Select(line =>
            {
                var comment = line.IndexOf("//", StringComparison.Ordinal);
                return comment < 0 ? line : line[..comment];
            }));
    }
}
