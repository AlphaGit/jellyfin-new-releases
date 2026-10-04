using System.Text.RegularExpressions;
using Jellyfin.Plugin.NewReleases.Tests.Support;
using Xunit;

namespace Jellyfin.Plugin.NewReleases.Tests.Packaging;

/// <summary>
/// A proxy for spec US2 scenario 1, "a tagged version publishes with no manual editing step".
/// A tag push cannot be exercised hermetically, so these assert the workflow's shape instead.
/// They bite when a step is deleted or reordered; they stay green if a real run fails for some
/// other reason, and the local dry run in <c>quickstart.md</c> is what covers that.
/// </summary>
public class ReleaseWorkflowTests
{
    /// <summary>
    /// The workflow with its comment lines removed. Ordering must be judged on the steps that
    /// run, not on prose that happens to name a command.
    /// </summary>
    private static readonly string Steps = RepositoryFiles.WorkflowSteps(".github/workflows/package.yml");

    private static int IndexOf(string fragment)
    {
        var at = Steps.IndexOf(fragment, StringComparison.Ordinal);
        Assert.True(at >= 0, $"the release workflow has no step containing: {fragment}");
        return at;
    }

    /// <summary>
    /// U27, restated by 006 U15: the chain has to run end to end from the tag. A missing link
    /// leaves the published repository behind the versions that exist, which FR-014 forbids. The
    /// release comes before the catalogue entry, so the catalogue never names an asset that does
    /// not exist yet (006 FR-011). Nothing is deployed: the catalogue is read from the branch.
    /// </summary>
    [Fact]
    public void ReleaseWorkflow_BuildsReleasesAddsToTheManifestAndCommits_InThatOrder()
    {
        var build = IndexOf("jprm plugin build");
        var release = IndexOf("gh release create");
        var add = IndexOf("jprm repo add");
        var commit = IndexOf("git commit");

        Assert.True(build < release, "the release is created before the package is built");
        Assert.True(release < add, "the catalogue names the release asset before the release exists");
        Assert.True(add < commit, "repo/ is committed before the version is added to the manifest");
    }

    /// <summary>
    /// 006 U28: the package can be moved only once JPRM has written it, and the release can
    /// upload it only once it has been moved. Out of order, the release step fails on a missing
    /// file, and only when a tag is pushed (006 second audit, survivor S1).
    /// </summary>
    [Fact]
    public void ReleaseWorkflow_MovesThePackageBetweenBuildingAndReleasingIt()
    {
        var move = IndexOf("mv \"./artifacts/");

        Assert.True(IndexOf("jprm plugin build") < move, "the package is moved before JPRM has written it");
        Assert.True(move < IndexOf("gh release create"), "the release uploads the package before it is moved");
    }

    /// <summary>
    /// 006 U16: packages are release assets and the catalogue is read from the branch, so no
    /// release depends on GitHub Pages (006 FR-012). A Pages step left behind would keep a second,
    /// stale copy of the catalogue answering at the old address.
    /// </summary>
    [Theory]
    [InlineData("actions/configure-pages")]
    [InlineData("actions/upload-pages-artifact")]
    [InlineData("actions/deploy-pages")]
    public void ReleaseWorkflow_DeploysNoPagesSite(string pagesAction)
    {
        Assert.DoesNotContain(pagesAction, Steps, StringComparison.Ordinal);
    }

    /// <summary>
    /// 006 U17: with no Pages deployment the job needs only `contents: write`, to create the
    /// release and push the catalogue. A token that can still publish a site or mint an identity
    /// token is a wider grant than the release uses.
    /// </summary>
    [Fact]
    public void ReleaseWorkflow_GrantsNoPagesPermission()
    {
        Assert.False(GrantsPagesOrIdToken(Steps), "the release job can still publish a site or mint an identity token");
    }

    /// <summary>
    /// The predicate above is a rule, and a rule needs a table (`tdd-profile.md`). Written from the
    /// ways a workflow can grant a permission — a key under `permissions:`, the `write-all`
    /// shorthand, the flow form — before the predicate was widened (006 second audit, finding 7).
    /// </summary>
    [Theory]
    [InlineData("permissions:\n  contents: write\n", false)]
    [InlineData("permissions: read-all\n", false)]
    [InlineData("permissions:\n  contents: write\n  pages: write\n", true)]
    [InlineData("permissions:\n  id-token: write\n", true)]
    [InlineData("permissions: write-all\n", true)]
    [InlineData("permissions: { contents: write, pages: write }\n", true)]
    [InlineData("permissions: { id-token: write }\n", true)]
    public void GrantsPagesOrIdToken_ReadsEveryWayOfGrantingThem(string permissions, bool grants)
    {
        Assert.Equal(grants, GrantsPagesOrIdToken(permissions));
    }

    private static bool GrantsPagesOrIdToken(string steps)
        => Regex.IsMatch(steps, @"(?m)^\s*(pages|id-token)\s*:")
           || Regex.IsMatch(steps, @"(?m)^\s*permissions\s*:\s*write-all\b")
           || Regex.IsMatch(steps, @"(?m)^\s*permissions\s*:\s*\{[^}]*\b(pages|id-token)\s*:");

    /// <summary>
    /// 006 U18: the release says what the catalogue says — the tagged version's section of
    /// CHANGELOG.md, written by the same script that writes the catalogue text, in its `--notes`
    /// mode, whose argument order `tests/web/changelog-entry.test.js` pins (006 FR-011, T039).
    /// The notes file `gh release create` reads must be the one that step writes.
    /// </summary>
    [Fact]
    public void ReleaseWorkflow_GivesTheReleaseTheTaggedVersionsChangelogSection()
    {
        var notes = Regex.Match(Steps, @"gh release create(?:[^\n]*\\\n)*[^\n]*--notes-file\s+""?([^\s""]+)");
        Assert.True(notes.Success, "gh release create is given no --notes-file");

        Assert.Matches(
            $@"node \.github/scripts/changelog-entry\.js ""\$\{{\{{ steps\.ver\.outputs\.version }}}}"" --notes ""?{Regex.Escape(notes.Groups[1].Value)}""?(?=\s|$)",
            Steps);
    }

    /// <summary>
    /// 006 U19: `gh release create` reads its notes file when it runs. Written afterwards, the
    /// file does not exist yet and the release fails, or a stale one from an earlier step is read.
    /// </summary>
    [Fact]
    public void ReleaseWorkflow_WritesTheReleaseNotesBeforeCreatingTheRelease()
    {
        Assert.True(
            IndexOf("changelog-entry.js \"${{ steps.ver.outputs.version }}\" --notes") < IndexOf("gh release create"),
            "the release is created before its notes are written");
    }

    /// <summary>
    /// U28: JPRM rewrites TargetFramework in the project file while packaging and is expected to
    /// put it back. Contract statement 6 is about that element specifically, and a local dry run
    /// confirmed JPRM restores it — while leaving the rewritten &lt;Version&gt; behind. A whole-file
    /// check would therefore fail every release; this guards what the contract actually says.
    /// </summary>
    [Fact]
    public void ReleaseWorkflow_FailsTheRunIfPackagingDidNotRestoreTheTargetFramework()
    {
        // One unit: the grep, its pattern and its file, across the shell line continuation.
        // Asserted separately, `git checkout -- <csproj>` two lines later satisfies the path half.
        Assert.Matches(
            $@"grep -q '<TargetFramework>{Regex.Escape(TargetVersions.Framework)}</TargetFramework>'\s*\\?\s*"
            + @"src/Jellyfin\.Plugin\.NewReleases/Jellyfin\.Plugin\.NewReleases\.csproj",
            Steps);
    }

    /// <summary>
    /// U51 (T064): the catalogue text is generated from CHANGELOG.md for the tagged version, so it
    /// has to be written into build.yaml before JPRM reads build.yaml to build the package.
    /// </summary>
    [Fact]
    public void ReleaseWorkflow_WritesTheChangelogEntryForTheTaggedVersionBeforePackaging()
    {
        var write = IndexOf("node .github/scripts/changelog-entry.js \"${{ steps.ver.outputs.version }}\"");

        Assert.True(write < IndexOf("jprm plugin build"), "the package is built before its changelog is written into build.yaml");
    }

    /// <summary>
    /// U33: JPRM normalises a three-part version to four parts and names the package for the
    /// normalised one, so a tag v1.0.0 produces jellyfin-new-releases_1.0.0.0.zip. A workflow
    /// that interpolates the tag's own version points at a file that does not exist.
    /// </summary>
    [Fact]
    public void ReleaseWorkflow_NamesThePackageByTheFourPartVersionJprmWrites()
    {
        Assert.Contains("${{ steps.ver.outputs.version4 }}.zip", Steps, StringComparison.Ordinal);
    }

    /// <summary>
    /// U27, second half: the run must be triggered by the tag and need no one to edit a file.
    /// </summary>
    [Fact]
    public void ReleaseWorkflow_RunsOnAVersionTag_AndDerivesTheVersionFromIt()
    {
        // Quote style is the author's choice; that the trigger is a v-prefixed tag is not.
        Assert.Matches(@"tags:\s*(\[\s*|-\s*)?['""]?v\*['""]?", Steps);
        Assert.Contains("GITHUB_REF_NAME#v", Steps, StringComparison.Ordinal);
    }
}
