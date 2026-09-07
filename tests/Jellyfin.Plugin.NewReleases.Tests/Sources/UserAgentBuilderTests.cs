using Jellyfin.Plugin.NewReleases.Sources;
using Xunit;

namespace Jellyfin.Plugin.NewReleases.Tests.Sources;

public class UserAgentBuilderTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Build_WithoutContact_IsProductAndVersion(string? contact)
    {
        Assert.Equal("JellyfinNewReleases/0.1.0", UserAgentBuilder.Build("0.1.0", contact));
    }

    [Fact]
    public void Build_WithContact_AppendsTrimmedContactInParentheses()
    {
        Assert.Equal("JellyfinNewReleases/0.1.0 ( me@example.org )", UserAgentBuilder.Build("0.1.0", " me@example.org "));
    }
}
