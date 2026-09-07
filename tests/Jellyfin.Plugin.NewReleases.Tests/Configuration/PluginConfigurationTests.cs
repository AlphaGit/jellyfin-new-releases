using Jellyfin.Plugin.NewReleases.Configuration;
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
}
