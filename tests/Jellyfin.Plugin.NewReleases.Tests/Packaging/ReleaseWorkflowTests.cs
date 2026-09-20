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

    private static int IndexOf(string fragment)
    {
        var at = Workflow.IndexOf(fragment, StringComparison.Ordinal);
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
    /// put it back. If it does not, the commit that follows would carry that edit onto main.
    /// </summary>
    [Fact]
    public void ReleaseWorkflow_FailsTheRunIfPackagingLeftTheProjectFileModified()
    {
        Assert.Contains(
            "git diff --quiet -- src/Jellyfin.Plugin.NewReleases/Jellyfin.Plugin.NewReleases.csproj",
            Workflow,
            StringComparison.Ordinal);
    }

    /// <summary>
    /// U27, second half: the run must be triggered by the tag and need no one to edit a file.
    /// </summary>
    [Fact]
    public void ReleaseWorkflow_RunsOnAVersionTag_AndDerivesTheVersionFromIt()
    {
        Assert.Contains("tags: ['v*']", Workflow, StringComparison.Ordinal);
        Assert.Contains("GITHUB_REF_NAME#v", Workflow, StringComparison.Ordinal);
    }
}
