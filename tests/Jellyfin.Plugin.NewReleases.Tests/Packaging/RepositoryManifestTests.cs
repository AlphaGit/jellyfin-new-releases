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
    private const string Jellyfin12Abi = TargetVersions.JellyfinAbi;

    /// <summary>
    /// How many versions the published repository lists right now. Zero is correct until the
    /// maintainer tags the first release: `FR-014` builds the publishing chain, and running it is
    /// a separate act. Raising this is the deliberate step that turns the per-entry checks below
    /// from vacuous into binding.
    /// </summary>
    private const int PublishedVersionsToday = 0;

    /// <summary>
    /// The package slug JPRM derives from the plugin's name. Read from <c>build.yaml</c> rather
    /// than written down, so a fork that renames the plugin still passes.
    /// </summary>
    private static readonly string Slug =
        RepositoryFiles.Scalar(RepositoryFiles.ReadAllText("build.yaml"), "name")!
            .ToLowerInvariant()
            .Replace(' ', '-');

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
        var versions = Versions();

        // Pinned, not skipped: `Assert.All` over an empty list asserts nothing and reports green,
        // so the count is stated first. This line is what fails the day the release chain adds a
        // version, which is exactly when these checks must start being read.
        AssertPublishedVersionCount(versions);
        Assert.All(versions, AssertInstallable);
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

        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertInstallable(entry.RootElement));
    }

    /// <summary>
    /// U25: the package must come from the same site as the document, under the name the version
    /// says, or the entry points at bytes that are not that version. The site root is taken from
    /// the document itself, never written down here: this repository and every fork of it publish
    /// to their own Pages site, and the invariant is that all entries share one.
    /// </summary>
    [Fact]
    public void Manifest_EverySourceUrlSharesOneSiteRoot_AndNamesItsOwnVersion()
    {
        var versions = Versions();

        AssertPublishedVersionCount(versions);

        if (versions.Count > 0)
        {
            var siteRoot = SiteRootOf(versions[0]);
            Assert.All(versions, version => AssertSourceUrlNamesItsOwnVersion(version, siteRoot));
        }
    }

    /// <summary>
    /// U26: the other side of U25. A source location under a different site, or naming a
    /// different version, points a server at bytes that are not the version it asked for.
    /// </summary>
    [Theory]
    [InlineData("1.0.0.0", "https://elsewhere.invalid/x/jellyfin-new-releases_1.0.0.0.zip")]
    [InlineData("1.0.0.0", ExampleSiteRoot + "jellyfin-new-releases_2.0.0.0.zip")]
    [InlineData("1.0.0.0", ExampleSiteRoot + "jellyfin-new-releases.zip")]
    public void ASourceUrlOffTheSiteOrNamingAnotherVersion_IsRejected(string number, string sourceUrl)
    {
        using var entry = JsonDocument.Parse(
            JsonSerializer.Serialize(new Dictionary<string, string>
            {
                ["version"] = number,
                ["sourceUrl"] = sourceUrl,
            }));

        Assert.ThrowsAny<Xunit.Sdk.XunitException>(
            () => AssertSourceUrlNamesItsOwnVersion(entry.RootElement, ExampleSiteRoot));
    }

    private static void AssertInstallable(JsonElement version)
    {
        // TryGetProperty, not GetProperty: a missing field must fail as an assertion about the
        // rule, not as a KeyNotFoundException that the negative theories would accept either way.
        Assert.False(string.IsNullOrWhiteSpace(Field(version, "version")), "version is missing or blank");
        Assert.False(string.IsNullOrWhiteSpace(Field(version, "sourceUrl")), "sourceUrl is missing or blank");
        Assert.False(string.IsNullOrWhiteSpace(Field(version, "checksum")), "checksum is missing or blank");
        Assert.False(string.IsNullOrWhiteSpace(Field(version, "timestamp")), "timestamp is missing or blank");
        Assert.Equal(Jellyfin12Abi, Field(version, "targetAbi"));
    }

    private static string? Field(JsonElement version, string name)
        => version.TryGetProperty(name, out var value) ? value.GetString() : null;

    /// <summary>A stand-in site root for the rejecting cases. Test data, not this project's URL.</summary>
    private const string ExampleSiteRoot = "https://example.invalid/repo/plugin/";

    /// <summary>
    /// Fails when the published repository gains or loses a version, so neither per-entry check
    /// above can sit green over an empty list without anyone noticing.
    /// </summary>
    private static void AssertPublishedVersionCount(IReadOnlyCollection<JsonElement> versions)
        => Assert.True(
            versions.Count == PublishedVersionsToday,
            $"repo/manifest.json lists {versions.Count} version(s), expected {PublishedVersionsToday}. "
            + "If the release chain has published one, raise PublishedVersionsToday — the per-entry "
            + "checks in this class only start binding once it is above zero.");

    private static string SiteRootOf(JsonElement version)
    {
        var sourceUrl = version.GetProperty("sourceUrl").GetString()!;
        Assert.StartsWith("https://", sourceUrl, StringComparison.Ordinal);
        return sourceUrl[..(sourceUrl.LastIndexOf('/') + 1)];
    }

    private static void AssertSourceUrlNamesItsOwnVersion(JsonElement version, string siteRoot)
    {
        var number = version.GetProperty("version").GetString();
        var sourceUrl = version.GetProperty("sourceUrl").GetString()!;

        Assert.StartsWith(siteRoot, sourceUrl, StringComparison.Ordinal);
        Assert.EndsWith($"{Slug}_{number}.zip", sourceUrl, StringComparison.Ordinal);
    }
}
