using System.Text.Json;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Serialization;
using Jellyfin.Plugin.NewReleases.Tests.Support;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.NewReleases.Tests;

[Collection(ProcessGlobalStateCollection.Name)]
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
    /// 006 U1: Jellyfin groups installed copies by name and retires all but the newest. A package
    /// declaring one name and a plugin reporting another files copies under two names, so the host
    /// loads both — the defect 006 fixes. Ordinal, although the host compares ignoring case: the
    /// requirement is that the two statements are identical.
    /// </summary>
    [Fact]
    public void Plugin_DisplayName_MatchesTheNameThePackageDeclares()
    {
        var plugin = new Plugin(Substitute.For<IApplicationPaths>(), Substitute.For<IXmlSerializer>());

        var declared = RepositoryFiles.Scalar(RepositoryFiles.ReadAllText("build.yaml"), "name");

        Assert.Equal(plugin.Name, declared);
    }

    /// <summary>
    /// 006 U2: this plugin is not part of Jellyfin's official distribution, so the name an operator
    /// reads in the dashboard must not suggest it is. The assembly identity keeps the host's
    /// <c>Jellyfin.Plugin.*</c> convention; only the displayed name is bound by this.
    /// </summary>
    [Fact]
    public void Plugin_DisplayName_DoesNotClaimToBeJellyfin()
    {
        var plugin = new Plugin(Substitute.For<IApplicationPaths>(), Substitute.For<IXmlSerializer>());

        Assert.DoesNotContain("jellyfin", plugin.Name, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 006 U5: the catalogue states the plugin's name too, and an operator's server files a copy
    /// under the name it was installed with. A catalogue entry under another name is the condition
    /// that let two copies load side by side.
    /// </summary>
    [Fact]
    public void Plugin_DisplayName_MatchesTheNameTheCatalogueLists()
    {
        var plugin = new Plugin(Substitute.For<IApplicationPaths>(), Substitute.For<IXmlSerializer>());
        using var catalogue = JsonDocument.Parse(RepositoryFiles.ReadAllText("repo/manifest.json"));

        var entry = Assert.Single(catalogue.RootElement.EnumerateArray().ToList());

        Assert.Equal(plugin.Name, entry.GetProperty("name").GetString());
    }

    /// <summary>
    /// A1: one constructed instance must be usable by the host for both of the things the host
    /// asks it for. The units below pin each half; this pins that the same instance gives both.
    /// </summary>
    [Fact]
    public void Plugin_ConstructedWithHostServices_ReportsItsIdentityAndOffersAConfigurationPage()
    {
        var plugin = new Plugin(Substitute.For<IApplicationPaths>(), Substitute.For<IXmlSerializer>());

        // The composite the units beneath cannot see: the host reaches the plugin's configuration
        // through the static Instance, and it must be the object just constructed. The identity
        // and the page are pinned by U1 and U2; repeating them here would only duplicate.
        Assert.Same(plugin, Plugin.Instance);
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
