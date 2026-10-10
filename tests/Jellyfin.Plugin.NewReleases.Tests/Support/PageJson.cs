using System.Text.Json;
using Jellyfin.Extensions.Json;

namespace Jellyfin.Plugin.NewReleases.Tests.Support;

/// <summary>A response as the plugin's pages receive it: serialized under the camelCase profile every endpoint declares (docs/http-surface.md rule 2).</summary>
internal static class PageJson
{
    public static JsonElement Of(object response) => JsonSerializer.SerializeToElement(response, JsonDefaults.CamelCaseOptions);
}
