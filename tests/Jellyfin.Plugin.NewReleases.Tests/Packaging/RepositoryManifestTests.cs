using System.Text.Json;
using System.Text.RegularExpressions;
using Jellyfin.Plugin.NewReleases.Tests.Support;
using Xunit;

namespace Jellyfin.Plugin.NewReleases.Tests.Packaging;

/// <summary>
/// The document an operator points a Jellyfin server at. Produced by <c>jprm repo add</c>, and
/// hand-edited exactly once, to clear the review releases (006 U4). Contract:
/// <c>specs/003-jellyfin-12-compat/contracts/plugin-repository-manifest.md</c>.
/// <para>
/// The version checks pass vacuously while <c>versions</c> is empty, which is correct until the
/// first tag, and bite the moment the release chain adds one.
/// </para>
/// </summary>
public class RepositoryManifestTests
{
    private const string ManifestPath = "repo/manifest.json";

    /// <summary>
    /// Read on use, not in the static initializer: a missing or unreadable workflow must fail the
    /// workflow tests alone, not every test in this class (006 audit, finding 12).
    /// </summary>
    private static string ReleaseWorkflowSteps => RepositoryFiles.WorkflowSteps(".github/workflows/package.yml");
    private const string Jellyfin12Abi = TargetVersions.JellyfinAbi;

    /// <summary>
    /// How many versions the published repository lists right now. 0.1.0.0 and 0.1.1.0 were
    /// review releases for the real-server passes, published under the old package slug, and were
    /// cleared on 2026-10-04 (006 U4). 0.2.0 is the first version published under the new name;
    /// until it is, the per-entry checks below run only against synthetic entries. Raise this
    /// with each release; the failure message says so.
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

    /// <summary>
    /// The workflow line that moves the package JPRM wrote, `{slug}_{four-part version}.zip`,
    /// up to the end of its source argument.
    /// </summary>
    private static readonly string MovesJprmsPackage =
        $@"(?m)^\s*mv\s+""?\./artifacts/{Regex.Escape(Slug)}_\$\{{\{{ steps\.ver\.outputs\.version4 }}}}\.zip""?";

    /// <summary>
    /// A well-formed entry, shaped exactly as `jprm repo add --plugin-url` writes one: its package
    /// is the asset of the version's own GitHub Release (006 FR-010). The published manifest lists
    /// no versions until the first tag, so without this the helpers below would only ever run over
    /// an empty list and could reject everything without a test noticing.
    /// </summary>
    private static JsonDocument WellFormedEntry(
        string version = "1.2.3.0",
        string? sourceUrl = null,
        string targetAbi = TargetVersions.JellyfinAbi)
        => JsonDocument.Parse(JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["version"] = version,
            ["changelog"] = "anything",
            ["targetAbi"] = targetAbi,
            ["sourceUrl"] = sourceUrl ?? $"{ExampleReleaseRoot}v{version}/{Slug}.zip",
            ["checksum"] = "0123456789abcdef0123456789abcdef",
            ["timestamp"] = "2026-09-20T00:00:00Z",
        }));

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
    /// U39, restated by 006 U13: the slug is derived here from `build.yaml`, and used on both
    /// sides of every source-URL assertion — so a wrong derivation agrees with itself. This
    /// anchors it to the one place the asset is really named: the file the release workflow
    /// uploads with `gh release create`. Matched whole, from a path, space or quote boundary to
    /// one, because a bare substring check let a stale `jellyfin-new-releases_…zip` satisfy
    /// `new-releases_…zip` (006 cycle 3). Comment lines are not steps and are skipped.
    /// </summary>
    [Fact]
    public void TheDerivedSlug_MatchesTheFilenameTheReleaseWorkflowBuilds()
    {
        Assert.Matches(
            $@"gh release create(?:[^\n]*\\\n)*[^\n]*[\s/""']{Regex.Escape(Slug)}\.zip(?=[\s""']|$)",
            ReleaseWorkflowSteps);
    }

    /// <summary>
    /// 006 U25: JPRM writes the package as `{slug}_{four-part version}.zip`, and the workflow
    /// renames it before uploading. A source spelt any other way names a file that does not
    /// exist, and the release fails only when a tag is pushed. Restores the half of U39 that
    /// tied the slug to JPRM's own output name (006 audit, finding 1).
    /// </summary>
    [Fact]
    public void TheReleaseWorkflow_MovesTheFileJprmWrites()
    {
        Assert.Matches(MovesJprmsPackage + @"\s", ReleaseWorkflowSteps);
    }

    /// <summary>
    /// 006 U29: JPRM writes the package into the folder its `--output` names. The move must read
    /// from that folder, or it looks for a file JPRM never wrote (006 second audit, survivor S2).
    /// </summary>
    [Fact]
    public void TheReleaseWorkflow_MovesThePackageFromTheFolderJprmWritesTo()
    {
        var output = Regex.Match(ReleaseWorkflowSteps, @"jprm plugin build[^\n]*--output\s+""?([^\s""]+?)/?""?(?=\s|$)");
        // Any package file name: the name is U25's to pin, the folder is this test's.
        var moved = Regex.Match(ReleaseWorkflowSteps, @"(?m)^\s*mv\s+""?([^\s""]*)/[^/\s""]+_\$\{\{");
        Assert.True(output.Success, "jprm plugin build names no --output folder");
        Assert.True(moved.Success, "the workflow does not move JPRM's package");

        Assert.Equal(output.Groups[1].Value, moved.Groups[1].Value);
    }

    /// <summary>
    /// 006 U26: the release uploads the file the package was renamed to. Upload anything else
    /// and the release step fails on a missing file, after the package was built (006 audit,
    /// finding 1).
    /// </summary>
    [Fact]
    public void TheReleaseWorkflow_UploadsTheFileItMovedThePackageTo()
    {
        var uploaded = Regex.Match(ReleaseWorkflowSteps, @"gh release create\s+\S+\s+""?([^\s""]+)""?");
        Assert.True(uploaded.Success, "gh release create uploads no file");

        // Found by its destination, not by its source, so this test fails for one reason only:
        // what U25 pins about the source is U25's to report (006 second audit, finding 21).
        Assert.Matches($@"(?m)^\s*mv\s+.+\s""?{Regex.Escape(uploaded.Groups[1].Value)}""?\s*$", ReleaseWorkflowSteps);
    }

    /// <summary>
    /// 006 U14: JPRM writes a `sourceUrl` under its own repository folder unless told otherwise.
    /// The catalogue must instead name the asset of the release the tag just created, or a server
    /// is sent to a file that was never published.
    /// </summary>
    [Fact]
    public void TheReleaseWorkflow_PointsTheCatalogueAtTheReleaseAsset()
    {
        Assert.Matches(
            $@"jprm repo add(?:[^\n]*\\\n)*[^\n]*--plugin-url\s+""?https://github\.com/\$\{{\{{ github\.repository }}}}/releases/download/\$\{{?GITHUB_REF_NAME}}?/{Regex.Escape(Slug)}\.zip(?=[\s""']|$)",
            ReleaseWorkflowSteps);
    }

    /// <summary>
    /// U40: the release-root rule must reject a second entry published somewhere else. Built from
    /// two different literal roots, so neither side of the comparison is derived from the other.
    /// </summary>
    [Fact]
    public void EntriesFromTwoDifferentSites_AreRejected()
    {
        using var here = WellFormedEntry("1.0.0.0", sourceUrl: $"https://one.invalid/o/r/releases/download/v1.0.0.0/{Slug}.zip");
        using var elsewhere = WellFormedEntry("2.0.0.0", sourceUrl: $"https://two.invalid/o/r/releases/download/v2.0.0.0/{Slug}.zip");

        var releaseRoot = ReleaseRootOf(here.RootElement);

        AssertSourceUrlNamesItsOwnVersion(here.RootElement, releaseRoot);
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(
            () => AssertSourceUrlNamesItsOwnVersion(elsewhere.RootElement, releaseRoot));
    }

    /// <summary>
    /// U41, restated by 006 U24: `ReleaseRootOf` must return the address above the entry's tag
    /// directory, read from the entry, not something it constructed. Every version is published
    /// under its own tag, so the directory holding the file differs per version and cannot be what
    /// all entries share. Fed a literal this class did not build.
    /// </summary>
    [Fact]
    public void ReleaseRootOf_ReturnsTheAddressAboveTheEntrysTagDirectory()
    {
        using var entry = WellFormedEntry(
            "3.0.0.0",
            sourceUrl: "https://host.invalid/owner/repo/releases/download/v3.0.0/pkg.zip");

        Assert.Equal("https://host.invalid/owner/repo/releases/download/", ReleaseRootOf(entry.RootElement));
    }

    /// <summary>
    /// U23, the accepting side. Without this the rule below could reject every entry ever
    /// written and no test would fail, because the published list is empty until the first tag.
    /// </summary>
    [Fact]
    public void AWellFormedEntry_IsAccepted()
    {
        using var entry = WellFormedEntry();

        AssertInstallable(entry.RootElement);
    }

    /// <summary>
    /// U25, the accepting side, for the same reason.
    /// </summary>
    [Fact]
    public void AWellFormedEntry_SourceUrlIsAccepted()
    {
        using var entry = WellFormedEntry();

        AssertSourceUrlNamesItsOwnVersion(entry.RootElement, ExampleReleaseRoot);
    }

    /// <summary>
    /// 006 U8: the release is created from the tag, `v1.2.3`, while JPRM writes the version in four
    /// parts, `1.2.3.0`. Both name the same release, so the address must be accepted for it.
    /// </summary>
    [Fact]
    public void AReleaseTaggedInThreeParts_IsAcceptedForItsFourPartVersion()
    {
        using var entry = WellFormedEntry("1.2.3.0", sourceUrl: $"{ExampleReleaseRoot}v1.2.3/{Slug}.zip");

        AssertSourceUrlNamesItsOwnVersion(entry.RootElement, ExampleReleaseRoot);
    }

    /// <summary>
    /// 006 T045: the other short form, `v1.2`, names the release JPRM writes as `1.2.0.0`.
    /// </summary>
    [Fact]
    public void AReleaseTaggedInTwoParts_IsAcceptedForItsFourPartVersion()
    {
        using var entry = WellFormedEntry("1.2.0.0", sourceUrl: $"{ExampleReleaseRoot}v1.2/{Slug}.zip");

        AssertSourceUrlNamesItsOwnVersion(entry.RootElement, ExampleReleaseRoot);
    }

    /// <summary>
    /// 006 T045: an address with no tag directory names no release, so it has no root to share.
    /// Reading one from it would let a malformed first entry set the root every other entry is
    /// judged against.
    /// </summary>
    [Fact]
    public void ReleaseRootOf_RejectsAnAddressWithNoTagDirectory()
    {
        using var entry = WellFormedEntry("3.0.0.0", sourceUrl: "https://host.invalid/owner/repo/releases/download/pkg.zip");

        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => ReleaseRootOf(entry.RootElement));
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
    /// U25: every package must be the asset of its own version's release, under one release root,
    /// or the entry points at bytes that are not that version. The release root is taken from the
    /// document itself, never written down here: this repository and every fork of it publish to
    /// their own releases, and the invariant is that all entries share one (006 FR-010).
    /// </summary>
    [Fact]
    public void Manifest_EverySourceUrlSharesOneSiteRoot_AndNamesItsOwnVersion()
    {
        var versions = Versions();

        AssertPublishedVersionCount(versions);

        // No branch: the root is read only when an entry exists, so an empty catalogue asserts the
        // count alone. The rule binds once a release raises PublishedVersionsToday with its entry;
        // until then the count above fails first (006 audit, findings 3 and 25).
        Assert.All(versions, entry => AssertSourceUrlNamesItsOwnVersion(entry, ReleaseRootOf(versions[0])));
    }

    /// <summary>
    /// U26: the other side of U25. A source location under a different release root, or naming a
    /// different version, points a server at bytes that are not the version it asked for.
    /// </summary>
    [Theory]
    [MemberData(nameof(RejectedSourceUrls))]
    public void ASourceUrlOffTheSiteOrNamingAnotherVersion_IsRejected(string number, string sourceUrl)
    {
        using var entry = JsonDocument.Parse(
            JsonSerializer.Serialize(new Dictionary<string, string>
            {
                ["version"] = number,
                ["sourceUrl"] = sourceUrl,
            }));

        Assert.ThrowsAny<Xunit.Sdk.XunitException>(
            () => AssertSourceUrlNamesItsOwnVersion(entry.RootElement, ExampleReleaseRoot));
    }

    /// <summary>
    /// The rejecting rows, built from <see cref="Slug"/> so a fork that renames the plugin keeps
    /// rejecting each row for its stated reason (006 audit, findings 6, 13 and 26). Each row
    /// differs from a valid address in its stated reason only, so each is the row that fails when
    /// the check for that reason is removed.
    /// </summary>
    public static TheoryData<string, string> RejectedSourceUrls => new()
    {
        // Another release root, of the same length, so only the root check can reject it.
        { "1.0.0.0", $"{AnotherReleaseRoot}v1.0.0.0/{Slug}.zip" },

        // Another version's release.
        { "1.0.0.0", $"{ExampleReleaseRoot}v2.0.0.0/{Slug}.zip" },

        // No tag directory, so no version at all.
        { "1.0.0.0", $"{ExampleReleaseRoot}{Slug}.zip" },

        // The other side of the three-part boundary U8 accepts.
        { "1.2.3.1", $"{ExampleReleaseRoot}v1.2.3/{Slug}.zip" },

        // A five-part tag, which no four-part version can name.
        { "1.2.3.4", $"{ExampleReleaseRoot}v1.2.3.4.0/{Slug}.zip" },

        // The Pages layout: a slug folder, and the version in the file name.
        { "1.0.0.0", $"{ExampleReleaseRoot}{Slug}/{Slug}_1.0.0.0.zip" },

        // JPRM's own file name, carrying the version, under the right tag.
        { "1.0.0.0", $"{ExampleReleaseRoot}v1.0.0.0/{Slug}_1.0.0.0.zip" },

        // Another asset of the same length, so only the asset-name check can reject it.
        { "1.0.0.0", $"{ExampleReleaseRoot}v1.0.0.0/{(Slug[0] == 'x' ? 'y' : 'x')}{Slug[1..]}.zip" },
    };

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

    /// <summary>A stand-in release root for the rejecting cases. Test data, not this project's URL.</summary>
    private const string ExampleReleaseRoot = "https://example.invalid/owner/repo/releases/download/";

    /// <summary>A second stand-in release root, the same length as <see cref="ExampleReleaseRoot"/>.</summary>
    private const string AnotherReleaseRoot = "https://another.invalid/owner/repo/releases/download/";

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

    private static string ReleaseRootOf(JsonElement version)
    {
        var sourceUrl = version.GetProperty("sourceUrl").GetString()!;
        Assert.StartsWith("https://", sourceUrl, StringComparison.Ordinal);
        var tagDirectory = sourceUrl[..sourceUrl.LastIndexOf('/')];
        Assert.Matches("/v[^/]+$", tagDirectory);
        return tagDirectory[..(tagDirectory.LastIndexOf('/') + 1)];
    }

    private static void AssertSourceUrlNamesItsOwnVersion(JsonElement version, string releaseRoot)
    {
        var number = version.GetProperty("version").GetString();
        var sourceUrl = version.GetProperty("sourceUrl").GetString()!;

        var tagStart = $"{releaseRoot}v";
        var asset = $"/{Slug}.zip";

        Assert.StartsWith(tagStart, sourceUrl, StringComparison.Ordinal);
        Assert.EndsWith(asset, sourceUrl, StringComparison.Ordinal);
        Assert.Equal(number, InFourParts(sourceUrl[tagStart.Length..^asset.Length]));
    }

    /// <summary>
    /// A tag's version padded to the four parts JPRM writes, as the release workflow pads it.
    /// </summary>
    private static string InFourParts(string tag)
    {
        while (tag.Count(c => c == '.') < 3)
        {
            tag += ".0";
        }

        return tag;
    }
}
