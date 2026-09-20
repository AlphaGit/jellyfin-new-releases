using Jellyfin.Plugin.NewReleases.Tests.Support;
using Xunit;

namespace Jellyfin.Plugin.NewReleases.Tests.Packaging;

/// <summary>
/// <c>build.yaml</c> is what JPRM turns into the package and its catalogue entry. Contract:
/// <c>specs/003-jellyfin-12-compat/contracts/plugin-repository-manifest.md</c>.
/// </summary>
public class BuildManifestTests
{
    private static readonly string Manifest = RepositoryFiles.ReadAllText("build.yaml");

    /// <summary>
    /// U17: Jellyfin offers a version only to a server at or above its targetAbi. This is the
    /// single value that keeps the plugin off servers that cannot load it.
    /// </summary>
    [Fact]
    public void BuildManifest_DeclaresJellyfin12AsTheTargetAbi()
    {
        Assert.Equal(TargetVersions.JellyfinAbi, RepositoryFiles.Scalar(Manifest, "targetAbi"));
    }

    /// <summary>
    /// U18: JPRM rewrites the project's TargetFramework from this value while building, so a
    /// stale one silently packages the wrong framework.
    /// </summary>
    [Fact]
    public void BuildManifest_DeclaresNet10AsTheFramework()
    {
        Assert.Equal(TargetVersions.Framework, RepositoryFiles.Scalar(Manifest, "framework"));
    }

    /// <summary>
    /// U35: build.yaml and the project file must name the same framework. JPRM rewrites the
    /// project's TargetFramework from build.yaml, so a divergence packages one framework while
    /// the repository builds another, and nothing else in the suite compares the two.
    /// </summary>
    [Fact]
    public void BuildManifest_FrameworkMatchesTheProjectFile()
    {
        var csproj = RepositoryFiles.ReadAllText(
            "src/Jellyfin.Plugin.NewReleases/Jellyfin.Plugin.NewReleases.csproj");

        Assert.Contains(
            $"<TargetFramework>{RepositoryFiles.Scalar(Manifest, "framework")}</TargetFramework>",
            csproj,
            StringComparison.Ordinal);
    }

    /// <summary>
    /// U19: Jellyfin keys an install by this GUID. If the manifest and the assembly disagree,
    /// the catalogue offers an install that the server files under a different identity.
    /// </summary>
    [Fact]
    public void BuildManifest_GuidMatchesTheOneCompiledIntoThePlugin()
    {
        Assert.Equal(Plugin.PluginGuid, RepositoryFiles.Scalar(Manifest, "guid"));
    }

    /// <summary>
    /// U20: FR-009 — a catalogue install must need no manual file step, so every file the plugin
    /// needs at runtime has to be listed here or it is simply absent from the package.
    /// </summary>
    [Fact]
    public void BuildManifest_ShipsThePluginItsSqliteAssembliesAndTheNativeLibrary()
    {
        Assert.Equal(
            new SortedSet<string>(
            [
                "Jellyfin.Plugin.NewReleases.dll",
                "Microsoft.Data.Sqlite.dll",
                "SQLitePCLRaw.batteries_v2.dll",
                "SQLitePCLRaw.core.dll",
                "SQLitePCLRaw.provider.e_sqlite3.dll",
                "runtimes/linux-x64/native/libe_sqlite3.so",
            ], StringComparer.Ordinal),
            new SortedSet<string>(RepositoryFiles.Sequence(Manifest, "artifacts"), StringComparer.Ordinal));
    }
}
