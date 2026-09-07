namespace Jellyfin.Plugin.NewReleases.Sources;

/// <summary>`User-Agent` for every outbound request (FR-018): product, version and, when configured, the operator's contact.</summary>
public static class UserAgentBuilder
{
    public const string ProductName = "JellyfinNewReleases";

    /// <summary>`JellyfinNewReleases/&lt;version&gt;` or `JellyfinNewReleases/&lt;version&gt; ( &lt;contact&gt; )`; never a hard-coded contact.</summary>
    public static string Build(string version, string? contact)
    {
        var trimmed = contact?.Trim();
        return string.IsNullOrEmpty(trimmed) ? $"{ProductName}/{version}" : $"{ProductName}/{version} ( {trimmed} )";
    }
}
