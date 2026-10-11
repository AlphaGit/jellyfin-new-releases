using Jellyfin.Plugin.NewReleases.Sources;
using Xunit;

namespace Jellyfin.Plugin.NewReleases.Tests.Sources;

/// <summary>The fixed placeholder set (spec 009 FR-002, research R1).</summary>
public class PlaceholderArtistsTests
{
    [Theory]
    [InlineData("89ad4ac3-39f7-470e-963a-56509c546377")]
    [InlineData("125ec42a-7229-4250-afc5-e057484327fe")]
    [InlineData("f731ccc4-e22a-43af-a747-64213329e088")]
    [InlineData("eec63d3c-3b81-4ad4-b1e4-7c147d4d2b61")]
    [InlineData("9be7f096-97ec-4615-8957-8d40b5dcbc41")]
    [InlineData("33cf029c-63b0-41a0-9855-be2a3665fb3b")]
    [InlineData("314e1c25-dde7-4e4d-b2f4-0a7b9f7c56dc")]
    [InlineData("80a8851f-444c-4539-892b-ad2a49292aa9")]
    [InlineData("66ea0139-149f-4a0c-8fbf-5ea9ec4a6e49")]
    [InlineData("a0ef7e1d-44ff-4039-9435-7d5fefdeecc9")]
    [InlineData("90068d37-bae7-4292-be4a-704c145bd616")]
    [InlineData("459a4aa6-b6b7-4c89-a42b-03acddbe1f80")]
    [InlineData("7e84f845-ac16-41fe-9ff8-df12eb32af55")]
    [InlineData("89AD4AC3-39F7-470E-963A-56509C546377")]
    public void MusicBrainzSpecialPurposeArtist_IsAPlaceholder(string mbid)
    {
        Assert.True(PlaceholderArtists.IsPlaceholder(SourceLimits.MusicBrainz, mbid));
    }

    [Fact]
    public void DeezerVariousArtists_IsAPlaceholder()
    {
        Assert.True(PlaceholderArtists.IsPlaceholder(SourceLimits.Deezer, "5080"));
    }

    [Theory]
    [InlineData(SourceLimits.MusicBrainz, "5080")]
    [InlineData(SourceLimits.Deezer, "89ad4ac3-39f7-470e-963a-56509c546377")]
    [InlineData(SourceLimits.MusicBrainz, "75c016da-48e4-4992-8a10-b7128189b272")]
    [InlineData(SourceLimits.Deezer, "27")]
    [InlineData(SourceLimits.MusicBrainz, null)]
    public void OtherIdentity_IsNotAPlaceholder(string source, string? id)
    {
        Assert.False(PlaceholderArtists.IsPlaceholder(source, id));
    }
}
