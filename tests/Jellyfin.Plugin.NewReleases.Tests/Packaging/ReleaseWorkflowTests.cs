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
    private static readonly string Workflow =
        RepositoryFiles.ReadAllText(".github/workflows/package.yml");

    /// <summary>
    /// The workflow with its comment lines removed. Ordering must be judged on the steps that
    /// run, not on prose that happens to name a command.
    /// </summary>
    private static readonly string Steps = string.Join(
        '\n',
        Workflow.Split('\n').Where(line => !line.TrimStart().StartsWith('#')));

    private static int IndexOf(string fragment)
    {
        var at = Steps.IndexOf(fragment, StringComparison.Ordinal);
        Assert.True(at >= 0, $"the release workflow has no step containing: {fragment}");
        return at;
    }

    /// <summary>
    /// U27: the chain has to run end to end from the tag. A missing link leaves the published
    /// repository behind the versions that exist, which FR-014 forbids.
    /// </summary>
    [Fact]
    public void ReleaseWorkflow_BuildsAddsToTheManifestCommitsAndDeploys_InThatOrder()
    {
        var build = IndexOf("jprm plugin build");
        var add = IndexOf("jprm repo add");
        var commit = IndexOf("git commit");
        var deploy = IndexOf("actions/deploy-pages");

        Assert.True(build < add, "the version is added to the manifest before the package is built");
        Assert.True(add < commit, "repo/ is committed before the version is added to the manifest");
        Assert.True(commit < deploy, "the site is deployed before repo/ is committed");
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
