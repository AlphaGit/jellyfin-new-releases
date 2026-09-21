namespace Jellyfin.Plugin.NewReleases.Api;

/// <summary>
/// The single authoritative source for the plugin's route prefix (specs/005-page-json-casing,
/// FR-013). Every <c>[Route]</c> and the Plugin Pages registration payload read it from here, and a
/// server-absolute path is derived by concatenation rather than written again.
/// <para>
/// The members are <c>const</c> because <c>[Route]</c> takes a compile-time constant, and constant
/// concatenation is what lets the derived paths compose without restating the prefix.
/// </para>
/// <para>
/// The two embedded pages cannot read this: they are static resources served with no build step,
/// and <c>admin.html</c> is streamed by Jellyfin without passing through plugin code. Their one
/// derived literal each is held to this file by a test instead.
/// </para>
/// </summary>
internal static class PluginRoutes
{
    /// <summary>The prefix every plugin endpoint is served under.</summary>
    public const string Base = "Plugins/NewReleases";

    /// <summary>The administrator endpoints, which additionally require elevation.</summary>
    public const string Admin = Base + "/Admin";

    /// <summary>The HTML fragment Plugin Pages fetches.</summary>
    public const string UserView = Base + "/UserView";

    /// <summary>The same path in the server-absolute form the Plugin Pages payload requires.</summary>
    public const string UserViewAbsolute = "/" + UserView;
}
