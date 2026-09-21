using System.Text.Json;
using System.Text.Json.Nodes;
using Jellyfin.Extensions.Json;
using Jellyfin.Plugin.NewReleases.Api;
using Jellyfin.Plugin.NewReleases.Tests.Support;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Jellyfin.Plugin.NewReleases.Tests.Api;

/// <summary>
/// The response a page receives, not the record a controller returns. Jellyfin offers each
/// response in either naming and picks by what the caller asks for, so these tests resolve the
/// serializer options from the endpoint's own <see cref="ProducesAttribute"/> and never reach for
/// <c>JsonDefaults.CamelCaseOptions</c> directly. Reaching for it would be green against an
/// endpoint that declares nothing, which is the defect this feature exists to remove
/// (specs/005-page-json-casing, FR-003, FR-006).
/// </summary>
public class ResponseNamingTests
{
    [Fact]
    public void ListResponse_AsTheReleasesEndpointDeclaresIt_CarriesTheNamesTheListPageReads()
    {
        var written = Serialize(typeof(ReleasesController), PopulatedListResponse());

        Assert.Equal(NamesByPath(Fixture("releases.json")), NamesByPath(written));
    }

    /// <summary>
    /// A <see cref="ListResponse"/> covering every name the list page reads: one row of each state,
    /// one carrying missing tracks and a compared edition, one undated, one archived. Fields the
    /// serializer omits when null (<c>DefaultIgnoreCondition = WhenWritingNull</c>) therefore
    /// appear on at least one row, which is what makes the union of names complete.
    /// </summary>
    private static ListResponse PopulatedListResponse()
    {
        var chromatics = Guid.NewGuid();
        var desire = Guid.NewGuid();
        var sources = new[] { new SourceLinkDto("musicbrainz", "https://musicbrainz.org/release-group/1") };

        return new ListResponse(
            new[]
            {
                new ReleaseDto(101, "Chromatics", chromatics, "Closer to Grey", "Album", "2019-10-02", "Day", "Missing", null, null, sources, null),
                new ReleaseDto(102, "Chromatics", chromatics, "Kill for Love", "Album", "2012-03-26", "Day", "Incomplete", new[] { "Into the Black" }, new ComparedEditionDto("musicbrainz", "Kill for Love (deluxe edition)"), sources, null),
                new ReleaseDto(103, "Desire", desire, "Escape", "Single", "2027-01-15", "Day", "Upcoming", null, null, sources, null),
                new ReleaseDto(104, "Desire", desire, "II", "Album", null, "None", "Missing", null, null, sources, new ArchivedDto("HaveIt", DateTimeOffset.UnixEpoch)),
            },
            4,
            true,
            DateTimeOffset.UnixEpoch,
            24,
            "2026-09-20");
    }

    /// <summary>
    /// Serializes <paramref name="value"/> the way the host writes it for an endpoint on
    /// <paramref name="controller"/>: the options are chosen by the controller's declared media
    /// type, and an endpoint that declares no JSON type gets the host default, whatever that is.
    /// </summary>
    private static string Serialize(Type controller, object value)
        => JsonSerializer.Serialize(value, OptionsDeclaredBy(controller));

    private static JsonSerializerOptions OptionsDeclaredBy(Type controller)
    {
        var declared = controller.GetCustomAttributes(typeof(ProducesAttribute), inherit: true)
            .Cast<ProducesAttribute>()
            .SelectMany(a => a.ContentTypes)
            .ToList();

        if (declared.Contains(JsonDefaults.CamelCaseMediaType, StringComparer.OrdinalIgnoreCase))
        {
            return JsonDefaults.CamelCaseOptions;
        }

        return declared.Contains(JsonDefaults.PascalCaseMediaType, StringComparer.OrdinalIgnoreCase)
            ? JsonDefaults.PascalCaseOptions
            : JsonDefaults.Options; // the host default an endpoint inherits when it states nothing
    }

    private static string Fixture(string name) => FixtureLoader.LoadText("pages/" + name);

    /// <summary>
    /// The property names a document carries, keyed by the path they sit at (<c>""</c> for the
    /// root, <c>"items[]"</c> for every element of the root's <c>items</c> array). Names are
    /// unioned across array elements, because a row whose value is null omits that name entirely.
    /// Values are not compared: the fixture's values are written by hand and only its names are
    /// the contract.
    /// </summary>
    private static SortedDictionary<string, SortedSet<string>> NamesByPath(string json)
    {
        var found = new SortedDictionary<string, SortedSet<string>>(StringComparer.Ordinal);
        Walk(JsonNode.Parse(json), string.Empty, found);
        return found;
    }

    private static void Walk(JsonNode? node, string path, SortedDictionary<string, SortedSet<string>> found)
    {
        switch (node)
        {
            case JsonObject obj:
                if (!found.TryGetValue(path, out var names))
                {
                    found[path] = names = new SortedSet<string>(StringComparer.Ordinal);
                }

                foreach (var (name, child) in obj)
                {
                    names.Add(name);
                    Walk(child, path.Length == 0 ? name : path + "." + name, found);
                }

                break;

            case JsonArray array:
                foreach (var element in array)
                {
                    Walk(element, path + "[]", found);
                }

                break;
        }
    }
}
