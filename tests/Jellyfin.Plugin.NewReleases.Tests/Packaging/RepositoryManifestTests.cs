using System.Text.Json;
using Jellyfin.Plugin.NewReleases.Tests.Support;
using Xunit;

namespace Jellyfin.Plugin.NewReleases.Tests.Packaging;

/// <summary>
/// The document an operator points a Jellyfin server at. Produced by <c>jprm repo add</c> and
/// never hand-edited. Contract:
/// <c>specs/003-jellyfin-12-compat/contracts/plugin-repository-manifest.md</c>.
/// <para>
/// The version checks pass vacuously while <c>versions</c> is empty, which is correct until the
/// first tag, and bite the moment the release chain adds one.
/// </para>
/// </summary>
public class RepositoryManifestTests
{
    private const string ManifestPath = "repo/manifest.json";
    private const string SiteRoot = "https://alphagit.github.io/jellyfin-new-releases";

    private static JsonElement TheOnlyPlugin()
    {
        using var document = JsonDocument.Parse(RepositoryFiles.ReadAllText(ManifestPath));
        return Assert.Single(document.RootElement.EnumerateArray().ToList()).Clone();
    }

    private static List<JsonElement> Versions()
        => TheOnlyPlugin().GetProperty("versions").EnumerateArray().ToList();

    /// <summary>
    /// U21: a Jellyfin server reads this document to decide what to offer. The GUID is the
    /// plugin's identity; published under another, an install is filed as a different plugin.
    /// </summary>
    [Fact]
    public void Manifest_IsAnArrayOfOnePlugin_CarryingTheFrozenGuid()
    {
        Assert.Equal(Plugin.PluginGuid, TheOnlyPlugin().GetProperty("guid").GetString());
    }

    /// <summary>
    /// U22: before the first tag the document exists with no versions. A server pointed at it
    /// sees a repository with nothing to install, which is right — this feature builds the
    /// publishing chain; running it is the maintainer's act.
    /// </summary>
    [Fact]
    public void Manifest_MayListNoVersionsAtAll()
    {
        Assert.Equal(JsonValueKind.Array, TheOnlyPlugin().GetProperty("versions").ValueKind);
    }

    /// <summary>
    /// U23: every listed version must be installable. A missing checksum or source location is
    /// an entry a server will offer and then fail to fetch.
    /// </summary>
    [Fact]
    public void Manifest_EveryVersionCarriesItsDownloadChecksumTimestampAndJellyfin12()
    {
        foreach (var version in Versions())
        {
            AssertInstallable(version);
        }
    }

    /// <summary>
    /// U24: the check above runs over an empty list until the first tag, so on its own it proves
    /// nothing. This is its other side: the same check, applied to entries that must fail.
    /// </summary>
    [Theory]
    [InlineData(@"{""sourceUrl"":""u"",""checksum"":""c"",""timestamp"":""t"",""targetAbi"":""12.0.0.0""}")]
    [InlineData(@"{""version"":""1.0.0.0"",""checksum"":""c"",""timestamp"":""t"",""targetAbi"":""12.0.0.0""}")]
    [InlineData(@"{""version"":""1.0.0.0"",""sourceUrl"":""u"",""timestamp"":""t"",""targetAbi"":""12.0.0.0""}")]
    [InlineData(@"{""version"":""1.0.0.0"",""sourceUrl"":""u"",""checksum"":""c"",""targetAbi"":""12.0.0.0""}")]
    [InlineData(@"{""version"":""1.0.0.0"",""sourceUrl"":""u"",""checksum"":""c"",""timestamp"":""t"",""targetAbi"":""10.11.0.0""}")]
    [InlineData(@"{""version"":"" "",""sourceUrl"":""u"",""checksum"":""c"",""timestamp"":""t"",""targetAbi"":""12.0.0.0""}")]
    public void AnEntryMissingAFieldOrDeclaringAnotherAbi_IsRejected(string json)
    {
        using var entry = JsonDocument.Parse(json);

        Assert.ThrowsAny<Exception>(() => AssertInstallable(entry.RootElement));
    }

    /// <summary>
    /// U25: the package must come from the same site as the document, under the name the version
    /// says, or the entry points at bytes that are not that version.
    /// </summary>
    [Fact]
    public void Manifest_EverySourceUrlIsUnderTheSiteRoot_AndNamesItsOwnVersion()
    {
        foreach (var version in Versions())
        {
            AssertSourceUrlNamesItsOwnVersion(version);
        }
    }

    /// <summary>
    /// U26: the other side of U25. A source location under a different site, or naming a
    /// different version, points a server at bytes that are not the version it asked for.
    /// </summary>
    [Theory]
    [InlineData("1.0.0.0", "https://example.invalid/jellyfin-new-releases/jellyfin-new-releases_1.0.0.0.zip")]
    [InlineData("1.0.0.0", SiteRoot + "/jellyfin-new-releases/jellyfin-new-releases_2.0.0.0.zip")]
    [InlineData("1.0.0.0", SiteRoot + "/jellyfin-new-releases/jellyfin-new-releases.zip")]
    public void ASourceUrlOffTheSiteOrNamingAnotherVersion_IsRejected(string number, string sourceUrl)
    {
        using var entry = JsonDocument.Parse(
            JsonSerializer.Serialize(new Dictionary<string, string>
            {
                ["version"] = number,
                ["sourceUrl"] = sourceUrl,
            }));

        Assert.ThrowsAny<Exception>(() => AssertSourceUrlNamesItsOwnVersion(entry.RootElement));
    }

    private static void AssertInstallable(JsonElement version)
    {
        Assert.False(string.IsNullOrWhiteSpace(version.GetProperty("version").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(version.GetProperty("sourceUrl").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(version.GetProperty("checksum").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(version.GetProperty("timestamp").GetString()));
        Assert.Equal("12.0.0.0", version.GetProperty("targetAbi").GetString());
    }

    private static void AssertSourceUrlNamesItsOwnVersion(JsonElement version)
    {
        var number = version.GetProperty("version").GetString();
        var sourceUrl = version.GetProperty("sourceUrl").GetString()!;

        Assert.StartsWith(SiteRoot, sourceUrl, StringComparison.Ordinal);
        Assert.EndsWith($"jellyfin-new-releases_{number}.zip", sourceUrl, StringComparison.Ordinal);
    }
}
