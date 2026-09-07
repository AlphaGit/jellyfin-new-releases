using Jellyfin.Plugin.NewReleases.Matching;
using Jellyfin.Plugin.NewReleases.Model;
using Xunit;

namespace Jellyfin.Plugin.NewReleases.Tests.Matching;

/// <summary>Mapping table and examples from <c>docs/domain_knowledge/release-types.md</c>.</summary>
public class ReleaseTypeMapperTests
{
    [Theory]
    [InlineData("Album", ReleaseType.Album)]
    [InlineData("EP", ReleaseType.EP)]
    [InlineData("Single", ReleaseType.Single)]
    [InlineData("Broadcast", ReleaseType.Other)]
    [InlineData("Other", ReleaseType.Other)]
    [InlineData(null, ReleaseType.Other)]
    public void MapMusicBrainz_MapsPrimaryType(string? primary, ReleaseType expected)
    {
        var (mapped, secondaries) = ReleaseTypeMapper.MapMusicBrainz(primary, []);

        Assert.Equal(expected, mapped);
        Assert.Empty(secondaries);
    }
}
