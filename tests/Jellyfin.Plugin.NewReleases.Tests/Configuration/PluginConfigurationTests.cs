using System.Xml.Serialization;
using Jellyfin.Plugin.NewReleases.Configuration;
using Jellyfin.Plugin.NewReleases.Model;
using Xunit;

namespace Jellyfin.Plugin.NewReleases.Tests.Configuration;

/// <summary>Defaults and round-trip per <c>contracts/plugin-configuration.md</c> (US2 scenarios 1–2, constitution IV).</summary>
public class PluginConfigurationTests
{
    [Fact]
    public void Defaults_BothSourcesEnabled_ExactlyAlbumsAndEpsIncluded_NoCutoffNoContact()
    {
        var configuration = new PluginConfiguration();

        Assert.True(configuration.MusicBrainzEnabled);
        Assert.True(configuration.DeezerEnabled);
        Assert.Equal(
            (true, true, false, false, false, false, false),
            (configuration.IncludeAlbums, configuration.IncludeEps, configuration.IncludeSingles, configuration.IncludeCompilations, configuration.IncludeLive, configuration.IncludeRemixes, configuration.IncludeSoundtracks));
        Assert.Equal(string.Empty, configuration.ReleasedSince);
        Assert.Equal(string.Empty, configuration.UserAgentContact);
    }

    private static PluginConfiguration RoundTrip(PluginConfiguration configuration)
    {
        var serializer = new XmlSerializer(typeof(PluginConfiguration));
        using var buffer = new MemoryStream();
        serializer.Serialize(buffer, configuration);
        buffer.Position = 0;
        return (PluginConfiguration)serializer.Deserialize(buffer)!;
    }

    private static object Fields(PluginConfiguration c) => (
        c.MusicBrainzEnabled, c.DeezerEnabled, c.IncludeAlbums, c.IncludeEps, c.IncludeSingles, c.IncludeCompilations,
        c.IncludeLive, c.IncludeRemixes, c.IncludeSoundtracks, c.ReleasedSince, c.UserAgentContact);

    [Fact]
    public void XmlRoundTrip_FullyChangedConfiguration_IsEqualFieldByField()
    {
        var changed = new PluginConfiguration
        {
            MusicBrainzEnabled = false,
            DeezerEnabled = false,
            IncludeAlbums = false,
            IncludeEps = false,
            IncludeSingles = true,
            IncludeCompilations = true,
            IncludeLive = true,
            IncludeRemixes = true,
            IncludeSoundtracks = true,
            ReleasedSince = "2020-01-01",
            UserAgentContact = "admin@example.org",
        };

        Assert.Equal(Fields(changed), Fields(RoundTrip(changed)));
    }

    [Fact]
    public void EnabledReleaseTypes_AlbumAndEpByDefault_ReflectsEachToggle()
    {
        Assert.Equal(new HashSet<ReleaseType> { ReleaseType.Album, ReleaseType.EP }, new PluginConfiguration().EnabledReleaseTypes());

        var flipped = new PluginConfiguration
        {
            IncludeAlbums = false, IncludeEps = false, IncludeSingles = true, IncludeCompilations = true, IncludeLive = true, IncludeRemixes = true, IncludeSoundtracks = true,
        };

        Assert.Equal(
            new HashSet<ReleaseType> { ReleaseType.Single, ReleaseType.Compilation, ReleaseType.Live, ReleaseType.Remix, ReleaseType.Soundtrack },
            flipped.EnabledReleaseTypes());
    }
}
