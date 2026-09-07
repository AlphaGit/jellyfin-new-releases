namespace Jellyfin.Plugin.NewReleases.Sources;

/// <summary>`User-Agent` for every outbound request (FR-018): product, version and, when configured, the operator's contact.</summary>
public static class UserAgentBuilder
{
    public const string ProductName = "JellyfinNewReleases";

    public static string Build(string version, string? contact) => $"{ProductName}/{version}";
}
