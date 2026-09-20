using Jellyfin.Plugin.NewReleases.Tests.Support;
using Xunit;

namespace Jellyfin.Plugin.NewReleases.Tests.Packaging;

/// <summary>
/// A proxy for spec US2 scenario 4 and FR-006. What an operator needs before installing cannot
/// be read out of the code, so the README has to say it. These assert that it does; whether the
/// prose reads well is a human judgement and not tested here.
/// </summary>
public class DocumentationTests
{
    private static readonly string Readme = RepositoryFiles.ReadAllText("README.md");

    /// <summary>
    /// One markdown section's body. Asserting against the whole README lets prose elsewhere
    /// satisfy a check about a section that may not even exist.
    /// </summary>
    private static string SectionOf(string heading)
    {
        var start = Readme.IndexOf(heading, StringComparison.Ordinal);
        Assert.True(start >= 0, $"README.md has no {heading} section");

        var next = Readme.IndexOf("\n## ", start + heading.Length, StringComparison.Ordinal);
        return next < 0 ? Readme[start..] : Readme[start..next];
    }

    /// <summary>
    /// U29: an operator on 10.11.x must not read this and expect the plugin to work. Support for
    /// it ended with this feature, and the old claim is worse than no claim.
    /// </summary>
    [Fact]
    public void Readme_StatesJellyfin12_AndNoLongerClaims1011()
    {
        var requirements = SectionOf("## Requirements");

        Assert.Contains("Jellyfin 12", requirements, StringComparison.Ordinal);
        Assert.DoesNotContain("10.11", requirements, StringComparison.Ordinal);
    }

    /// <summary>
    /// U30: the repository address is the only way to install the plugin from a catalogue. An
    /// operator who cannot find it has to sideload, which FR-009 exists to avoid.
    /// </summary>
    [Fact]
    public void Readme_GivesTheRepositoryUrlAndWhereToAddIt()
    {
        var install = SectionOf("## Install");

        Assert.Matches(@"https://\S+/manifest\.json", install);
        Assert.Contains("Dashboard", install, StringComparison.Ordinal);
        Assert.Contains("Repositories", install, StringComparison.Ordinal);
    }

    /// <summary>
    /// U31: the page entry now goes through Plugin Pages' registration interface, which older
    /// builds of it do not have. Without the minimum stated, the menu entry silently never
    /// appears and nothing explains why.
    /// </summary>
    [Fact]
    public void Readme_StatesTheMinimumPluginPagesVersion()
    {
        var requirements = SectionOf("## Requirements");

        Assert.Contains("Plugin Pages", requirements, StringComparison.Ordinal);
        Assert.Contains("3.0.0.0", requirements, StringComparison.Ordinal);
    }
}
