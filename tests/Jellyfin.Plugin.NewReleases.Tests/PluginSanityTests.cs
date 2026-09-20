using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Serialization;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.NewReleases.Tests;

public class PluginSanityTests
{
    /// <summary>
    /// The GUID is the plugin's identity key in Jellyfin. Changing it orphans existing installs.
    /// </summary>
    [Fact]
    public void Plugin_Guid_IsStable()
    {
        var plugin = new Plugin(Substitute.For<IApplicationPaths>(), Substitute.For<IXmlSerializer>());

        Assert.Equal(new Guid("b8a15db8-e368-42c4-9048-390faf0094db"), plugin.Id);
    }

    /// <summary>
    /// A1: one constructed instance must be usable by the host for both of the things the host
    /// asks it for. The units below pin each half; this pins that the same instance gives both.
    /// </summary>
    [Fact]
    public void Plugin_ConstructedWithHostServices_ReportsItsIdentityAndOffersAConfigurationPage()
    {
        var plugin = new Plugin(Substitute.For<IApplicationPaths>(), Substitute.For<IXmlSerializer>());

        Assert.Equal(new Guid(Plugin.PluginGuid), plugin.Id);
        Assert.NotEmpty(plugin.GetPages());
    }
}
