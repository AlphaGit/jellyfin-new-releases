using System;
using System.Collections.Generic;
using Jellyfin.Plugin.NewReleases.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.NewReleases;

/// <summary>
/// Jellyfin New Releases plugin entry point.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    /// <summary>Stable plugin identity. Never change: Jellyfin keys installs by this GUID.</summary>
    public const string PluginGuid = "b8a15db8-e368-42c4-9048-390faf0094db";

    /// <summary>
    /// Initializes a new instance of the <see cref="Plugin"/> class.
    /// </summary>
    /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
    /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    /// <inheritdoc />
    public override string Name => "New Releases";

    /// <inheritdoc />
    public override string Description =>
        "Tracks new releases by the artists in your Jellyfin music library that the library does not yet contain.";

    /// <inheritdoc />
    public override Guid Id => new Guid(PluginGuid);

    /// <summary>
    /// Gets the current plugin instance.
    /// </summary>
    public static Plugin? Instance { get; private set; }

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages() => new[]
    {
        new PluginPageInfo
        {
            Name = "newreleases",
            EmbeddedResourcePath = GetType().Namespace + ".Web.admin.html",
        },
    };
}
