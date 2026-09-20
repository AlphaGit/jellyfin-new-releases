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

    /// <summary>
    /// U2: the host renders whatever <c>GetPages</c> returns. A second entry would put an
    /// unintended page in the dashboard; a wrong resource path renders an empty one.
    /// </summary>
    [Fact]
    public void GetPages_OffersExactlyOnePage_TheEmbeddedAdminPage()
    {
        var plugin = new Plugin(Substitute.For<IApplicationPaths>(), Substitute.For<IXmlSerializer>());

        var page = Assert.Single(plugin.GetPages());

        Assert.Equal("newreleases", page.Name);
        Assert.Equal("Jellyfin.Plugin.NewReleases.Web.admin.html", page.EmbeddedResourcePath);
    }

    /// <summary>
    /// U3: the page entry is registered through Plugin Pages' own interface now. Reaching into
    /// another plugin's stored configuration is what that replaces, so constructing this plugin
    /// must leave the configurations tree untouched.
    /// </summary>
    [Fact]
    public void Constructing_WritesNothingIntoThePluginConfigurationsTree()
    {
        var root = Directory.CreateTempSubdirectory(nameof(Constructing_WritesNothingIntoThePluginConfigurationsTree));
        try
        {
            // Plugin Pages installed and its configuration already present: the arrangement in
            // which the deleted writer would have edited another plugin's file.
            var plugins = root.CreateSubdirectory("plugins");
            plugins.CreateSubdirectory("Jellyfin.Plugin.PluginPages_3.0.0.0");
            var configurations = root.CreateSubdirectory("configurations");

            var paths = Substitute.For<IApplicationPaths>();
            paths.PluginConfigurationsPath.Returns(configurations.FullName);
            paths.PluginsPath.Returns(plugins.FullName);

            _ = new Plugin(paths, Substitute.For<IXmlSerializer>());

            Assert.Empty(configurations.GetFileSystemInfos());
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }

    /// <summary>
    /// U16: Plugin Pages is optional, and a reference to it — or to the Newtonsoft type its
    /// entry point takes — would make it mandatory, so the plugin would fail to load without it.
    /// The gateway reaches both by reflection precisely to keep this list clean.
    /// </summary>
    [Fact]
    public void ThePluginAssembly_ReferencesNeitherPluginPagesNorNewtonsoft()
    {
        var referenced = typeof(Plugin).Assembly
            .GetReferencedAssemblies()
            .Select(assembly => assembly.Name!)
            .ToList();

        Assert.DoesNotContain("Jellyfin.Plugin.PluginPages", referenced);
        Assert.DoesNotContain("Newtonsoft.Json", referenced);
    }
}
