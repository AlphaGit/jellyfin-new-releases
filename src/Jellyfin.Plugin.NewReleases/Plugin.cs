using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
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

    // Bump when the Plugin Pages entry schema changes; forces re-seed of the
    // Plugin Pages config.json so stale entries get replaced.
    private const int PluginPagesEntryVersion = 1;

    /// <summary>
    /// Initializes a new instance of the <see cref="Plugin"/> class.
    /// </summary>
    /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
    /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;

        TryRegisterPluginPagesEntry(applicationPaths);
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

    /// <summary>
    /// Writes a New Releases entry into the Plugin Pages (by IAmParadox27) config file so the
    /// user-facing view appears in the web client's hamburger menu, next to Concert Radar.
    /// No-op if Plugin Pages is not installed. Pattern shared with jellyfin-concert-radar.
    /// </summary>
    private static void TryRegisterPluginPagesEntry(IApplicationPaths applicationPaths)
    {
        try
        {
            var configDir = Path.Combine(applicationPaths.PluginConfigurationsPath, "Jellyfin.Plugin.PluginPages");
            var configFile = Path.Combine(configDir, "config.json");

            JsonObject root;
            if (File.Exists(configFile))
            {
                var text = File.ReadAllText(configFile);
                root = string.IsNullOrWhiteSpace(text)
                    ? new JsonObject()
                    : JsonNode.Parse(text) as JsonObject ?? new JsonObject();
            }
            else
            {
                if (!IsPluginPagesInstalled(applicationPaths))
                {
                    return;
                }

                Directory.CreateDirectory(configDir);
                root = new JsonObject();
            }

            if (root["pages"] is not JsonArray pages)
            {
                pages = new JsonArray();
                root["pages"] = pages;
            }

            JsonObject? existing = null;
            foreach (var node in pages)
            {
                if (node is JsonObject obj && (string?)obj["Id"] == "Jellyfin.Plugin.NewReleases")
                {
                    existing = obj;
                    break;
                }
            }

            if (existing is not null)
            {
                var storedVersion = (int?)existing["Version"] ?? 0;
                if (storedVersion >= PluginPagesEntryVersion)
                {
                    return;
                }

                pages.Remove(existing);
            }

            pages.Add(new JsonObject
            {
                ["Id"] = "Jellyfin.Plugin.NewReleases",
                ["Url"] = "/Plugins/NewReleases/UserView",
                ["DisplayText"] = "New Releases",
                ["Icon"] = "new_releases",
                ["Version"] = PluginPagesEntryVersion,
            });

            File.WriteAllText(configFile, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
            // Plugin Pages is an optional integration. The admin page, API and tasks must start regardless.
        }
    }

    private static bool IsPluginPagesInstalled(IApplicationPaths applicationPaths)
    {
        try
        {
            var pluginsDir = applicationPaths.PluginsPath;
            if (!Directory.Exists(pluginsDir))
            {
                return false;
            }

            foreach (var dir in Directory.EnumerateDirectories(pluginsDir))
            {
                var name = Path.GetFileName(dir);
                if (name.StartsWith("PluginPages", StringComparison.OrdinalIgnoreCase)
                    || name.StartsWith("Jellyfin.Plugin.PluginPages", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}
