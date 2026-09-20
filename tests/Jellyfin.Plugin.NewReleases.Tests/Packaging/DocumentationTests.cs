using System.Text.RegularExpressions;
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
    /// <summary>
    /// Sentences that name 10.11 without saying it is gone. Whole-file: a stale claim in the
    /// intro misleads as much as one under Requirements. Sentence-scoped so the README may still
    /// say support ended — and split on sentence punctuation followed by a space, never on a bare
    /// full stop, which would cut "10.11" in half.
    /// </summary>
    internal static IReadOnlyList<string> SentencesClaimingSupportFor1011(string markdown)
        => Regex.Split(markdown, @"(?<=[.!?])\s|\n")
            .Where(sentence => sentence.Contains("10.11", StringComparison.Ordinal))
            .Where(sentence => !Regex.IsMatch(
                sentence,
                @"no longer|dropped|not supported|unsupported|ended",
                RegexOptions.IgnoreCase))
            .ToList();

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
        Assert.Contains("Jellyfin 12", SectionOf("## Requirements"), StringComparison.Ordinal);
        Assert.DoesNotContain("10.11", SectionOf("## Requirements"), StringComparison.Ordinal);
        Assert.Empty(SentencesClaimingSupportFor1011(Readme));
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
    /// U38: the rule above is a predicate, and a predicate needs a table. These cases were
    /// written from the requirement — "the README must not claim support for 10.11, but may say
    /// it was dropped" — before the predicate existed. An earlier attempt at this check passed a
    /// single hand-picked example and was strictly weaker than what it replaced.
    /// </summary>
    [Theory]
    [InlineData("Jellyfin 10.11 or newer.")]
    [InlineData("Requires Jellyfin 10.11.")]
    [InlineData("requires Jellyfin 10.11")]
    [InlineData("Supports 10.11 and 12.")]
    [InlineData("- Jellyfin 10.11+")]
    [InlineData("Works with Jellyfin 10.11.x and later.")]
    [InlineData("Minimum: Jellyfin 10.11")]
    public void AClaimOfSupportFor1011_IsCaught(string sentence)
    {
        Assert.NotEmpty(SentencesClaimingSupportFor1011(sentence));
    }

    /// <summary>U38, the other side: saying support ended must remain sayable.</summary>
    [Theory]
    [InlineData("Jellyfin 10.11 is no longer supported.")]
    [InlineData("Support for Jellyfin 10.11 was dropped in this release.")]
    [InlineData("no longer supports 10.11")]
    [InlineData("Jellyfin 12. Older servers are not supported.")]
    public void SayingSupportFor1011Ended_IsAllowed(string sentence)
    {
        Assert.Empty(SentencesClaimingSupportFor1011(sentence));
    }

    /// <summary>
    /// U44: the menu entry needs a chain of two third-party plugins, not one, and on Jellyfin 12
    /// it does not render at all. The real-server pass found the README stating only half of
    /// that. An operator who reads "needs Plugin Pages" and installs it alone is left with a
    /// missing menu item and no explanation.
    /// </summary>
    [Fact]
    public void Readme_StatesTheWholePluginPagesChain_AndThatTheMenuEntryDoesNotRenderOn12()
    {
        var requirements = SectionOf("## Requirements");

        Assert.Contains("Plugin Pages", requirements, StringComparison.Ordinal);
        Assert.Contains("File Transformation", requirements, StringComparison.Ordinal);
        Assert.Contains("does not", requirements, StringComparison.Ordinal);
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
        Assert.Contains(TargetVersions.MinimumPluginPages, requirements, StringComparison.Ordinal);
    }
}
