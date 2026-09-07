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

    [Fact]
    public void MapMusicBrainz_MapsSecondaryTypesByNameAndUnknownToOther()
    {
        var (_, secondaries) = ReleaseTypeMapper.MapMusicBrainz(
            "Album", ["Compilation", "Live", "Remix", "Soundtrack", "DJ-mix", "Mixtape/Street", "Demo", "Audiobook"]);

        Assert.Equal(
            [ReleaseType.Compilation, ReleaseType.Live, ReleaseType.Remix, ReleaseType.Soundtrack, ReleaseType.Other, ReleaseType.Other, ReleaseType.Other, ReleaseType.Other],
            secondaries);
    }

    [Theory]
    [InlineData("album", ReleaseType.Album)]
    [InlineData("ep", ReleaseType.EP)]
    [InlineData("single", ReleaseType.Single)]
    [InlineData("compile", ReleaseType.Compilation)]
    [InlineData("mixtape", ReleaseType.Other)]
    public void MapDeezer_MapsRecordTypeAsPrimaryWithNoSecondaries(string recordType, ReleaseType expected)
    {
        var (primary, secondaries) = ReleaseTypeMapper.MapDeezer(recordType);

        Assert.Equal(expected, primary);
        Assert.Empty(secondaries);
    }

    private static readonly HashSet<ReleaseType> DefaultSelection = [ReleaseType.Album, ReleaseType.EP];

    [Fact]
    public void IsIncluded_EpWithNoSecondaries_IsIncludedByDefault()
    {
        Assert.True(ReleaseTypeMapper.IsIncluded(ReleaseType.EP, [], DefaultSelection));
    }
}
