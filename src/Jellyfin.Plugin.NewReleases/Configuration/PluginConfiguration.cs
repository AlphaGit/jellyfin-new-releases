using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.NewReleases.Configuration;

/// <summary>
/// Server-global plugin configuration. Persisted as XML by Jellyfin's built-in serializer.
/// Collection properties must use <see cref="System.Collections.Generic.List{T}"/> —
/// <c>HashSet&lt;T&gt;</c> and <c>Dictionary&lt;K,V&gt;</c> do not survive
/// <see cref="System.Xml.Serialization.XmlSerializer"/>. Do not seed lists in the constructor
/// (the serializer calls <c>Add</c> on round-trip and duplicates entries).
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    // Settings are defined by the spec (see specs/). Intentionally empty until then.
}
