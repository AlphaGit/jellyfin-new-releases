using System.Reflection;
using System.Text.RegularExpressions;
using Jellyfin.Extensions.Json;
using Jellyfin.Plugin.NewReleases.Api;
using Jellyfin.Plugin.NewReleases.Tests.Support;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Xunit;

namespace Jellyfin.Plugin.NewReleases.Tests.Api;

/// <summary>
/// The plugin's HTTP surface as a whole: what it is named, and what it states about the responses
/// it returns. The convention these enforce is
/// <c>specs/005-page-json-casing/contracts/http-surface.md</c>, copied to <c>docs/http-surface.md</c>.
/// </summary>
public class HttpSurfaceTests
{
    [Fact]
    public void ReleasesController_IsServedUnderThePluginRoutesBase()
    {
        Assert.Equal(PluginRoutes.Base, ControllerRoute(typeof(ReleasesController)));
    }

    [Fact]
    public void ReleasesController_ServesTheListAtReleases()
    {
        Assert.Contains("GET Plugins/NewReleases/Releases", RoutesOf(typeof(ReleasesController)));
    }

    [Fact]
    public void ReleasesController_ServesTheArtistFilterAtArtists()
    {
        Assert.Contains("GET Plugins/NewReleases/Artists", RoutesOf(typeof(ReleasesController)));
    }

    [Fact]
    public void ReleasesController_ServesTheSmallStatusAtStatus()
    {
        Assert.Contains("GET Plugins/NewReleases/Status", RoutesOf(typeof(ReleasesController)));
    }

    [Fact]
    public void ReleasesController_ServesTheDecisionsUnderTheReleaseTheyDecide()
    {
        var routes = RoutesOf(typeof(ReleasesController));

        Assert.Contains("POST Plugins/NewReleases/Releases/{id:long}/Ignore", routes);
        Assert.Contains("POST Plugins/NewReleases/Releases/{id:long}/HaveIt", routes);
        Assert.Contains("POST Plugins/NewReleases/Releases/{id:long}/Restore", routes);
    }

    [Fact]
    public void AdminController_IsServedUnderThePluginRoutesAdmin()
    {
        Assert.Equal(PluginRoutes.Admin, ControllerRoute(typeof(AdminController)));
    }

    [Fact]
    public void AdminController_ServesItsStatusAtStatus()
    {
        Assert.Contains("GET Plugins/NewReleases/Admin/Status", RoutesOf(typeof(AdminController)));
    }

    [Fact]
    public void AdminController_ServesItsActionsAsPascalCaseSegments()
    {
        var routes = RoutesOf(typeof(AdminController));

        Assert.Contains("POST Plugins/NewReleases/Admin/RunNow", routes);
        Assert.Contains("POST Plugins/NewReleases/Admin/Purge", routes);
        Assert.Contains("POST Plugins/NewReleases/Admin/ClearArchive", routes);
    }

    [Fact]
    public void UserViewController_IsServedAtThePluginRoutesUserView()
    {
        Assert.Equal(PluginRoutes.UserView, ControllerRoute(typeof(UserViewController)));
    }

    /// <summary>
    /// The one endpoint the naming rule exempts, and the only entry in the exceptions table of
    /// <c>docs/http-surface.md</c>. It serves an HTML fragment, so a JSON profile would be false.
    /// </summary>
    [Fact]
    public void UserViewController_DeclaresTextHtmlAndNoJsonProfile()
    {
        var produced = ProducedContentTypes(typeof(UserViewController).GetMethod(nameof(UserViewController.GetUserView))!);

        Assert.Contains("text/html", produced);
        Assert.DoesNotContain(produced, type => type.StartsWith("application/json", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// What an action states it produces: its own <see cref="ProducesAttribute"/> when it has one,
    /// otherwise its controller's. This is the order the host resolves them in, and it is what lets
    /// a controller-level declaration cover an action added later.
    /// </summary>
    internal static IReadOnlyList<string> ProducedContentTypes(MethodInfo action)
    {
        var own = action.GetCustomAttributes<ProducesAttribute>().SelectMany(a => a.ContentTypes).ToList();

        return own.Count > 0
            ? own
            : action.DeclaringType!.GetCustomAttributes<ProducesAttribute>(inherit: true).SelectMany(a => a.ContentTypes).ToList();
    }

    /// <summary>
    /// The pages are static resources served with no build step, and <c>admin.html</c> never passes
    /// through plugin code, so neither can read <see cref="PluginRoutes"/> at load time. Each holds
    /// one derived literal and this is what holds it to the source (FR-013, SC-007).
    /// </summary>
    [Theory]
    [InlineData("user-view.html", PluginRoutes.Base + "/")]
    [InlineData("admin.html", PluginRoutes.Admin + "/")]
    public void EachEmbeddedPage_BuildsItsPathsFromThePrefixItsEndpointsAreServedUnder(string page, string expected)
    {
        Assert.Equal(expected, ApiPrefixOf(page));
    }

    /// <summary>The <c>API</c> literal an embedded page declares, read from the shipped resource.</summary>
    internal static string ApiPrefixOf(string page)
    {
        var html = RepositoryFiles.ReadAllText("src/Jellyfin.Plugin.NewReleases/Web/" + page);
        var match = Regex.Match(html, @"var API = '([^']*)';");

        Assert.True(match.Success, $"{page} declares no `var API = '...';` line; the prefix guard cannot read it.");
        return match.Groups[1].Value;
    }

    // ---- Rule 2: every endpoint that returns a body declares its field naming ----

    /// <summary>
    /// The rule, stated so a test can be written from it rather than from a list of today's
    /// endpoints: an action MUST have an effective declaration, and if that declaration names any
    /// <c>application/json</c> type it MUST be the camelCase profile. **Declaring nothing fails.**
    /// That last clause is the whole point and the first version of this rule was missing it: an
    /// endpoint that states nothing is an endpoint that inherits the host default, which is the
    /// defect this feature exists to remove. See tdd/cycle-log.md cycle 37.
    /// <para>
    /// The table comes first and the scan below applies it, because a predicate proved with one
    /// example chosen after the fact has cost this project three consecutive remediations.
    /// </para>
    /// </summary>
    [Theory]
    [InlineData(true, JsonDefaults.CamelCaseMediaType)]
    [InlineData(true, "text/html")]
    [InlineData(true, "text/html", JsonDefaults.CamelCaseMediaType)]
    [InlineData(false)] // declares nothing, so it inherits whatever the host does: the defect
    [InlineData(false, "application/json")]
    [InlineData(false, JsonDefaults.PascalCaseMediaType)]
    [InlineData(false, "text/html", "application/json")]
    [InlineData(false, "application/json; charset=utf-8")]
    public void TheNamingRule_AcceptsOnlyJsonThatCarriesTheCamelCaseProfile(bool expected, params string[] produced)
    {
        Assert.Equal(expected, DeclaresItsNaming(produced));
    }

    [Fact]
    public void AnActionWithNoProducesOfItsOwn_InheritsItsControllersDeclaration()
    {
        var action = typeof(ReleasesController).GetMethod(nameof(ReleasesController.GetReleasesAsync))!;

        Assert.Empty(action.GetCustomAttributes<ProducesAttribute>());
        Assert.Contains(JsonDefaults.CamelCaseMediaType, ProducedContentTypes(action));
    }

    [Fact]
    public void EveryActionThatReturnsJson_DeclaresTheCamelCaseProfile()
    {
        var offenders = PluginActions()
            .Where(action => !DeclaresItsNaming(ProducedContentTypes(action)))
            .Select(action => action.DeclaringType!.Name + "." + action.Name)
            .ToList();

        Assert.True(offenders.Count == 0, string.Join("  |  ", offenders));
    }

    /// <summary>The rule of <c>docs/http-surface.md</c> rule 2, as a predicate.</summary>
    private static bool DeclaresItsNaming(IReadOnlyList<string> produced)
        => produced.Count > 0
           && (produced.Contains(JsonDefaults.CamelCaseMediaType, StringComparer.OrdinalIgnoreCase)
               || !produced.Any(type => type.StartsWith("application/json", StringComparison.OrdinalIgnoreCase)));

    // ---- Rule 1: route segments are PascalCase, concatenated, with no api segment ----

    [Theory]
    [InlineData(true, "Releases")]
    [InlineData(true, "ClearArchive")]
    [InlineData(true, "UserView")]
    [InlineData(true, "{id:long}")] // a route parameter is not a name the convention governs
    [InlineData(false, "releases")]
    [InlineData(false, "clear-archive")]
    [InlineData(false, "api")]
    [InlineData(false, "Api")]
    [InlineData(false, "run_now")]
    public void TheCasingRule_AcceptsOnlyPascalCaseSegmentsAndNoApiSegment(bool expected, string segment)
    {
        Assert.Equal(expected, IsConventionalSegment(segment));
    }

    [Fact]
    public void EveryRouteThePluginServes_UsesPascalCaseSegmentsAndNoApiSegment()
    {
        var offenders = PluginControllers()
            .SelectMany(RoutesOf)
            .Where(route => route.Split(' ')[1].Split('/').Any(segment => !IsConventionalSegment(segment)))
            .ToList();

        Assert.True(offenders.Count == 0, string.Join("  |  ", offenders));
    }

    /// <summary>The rule of <c>docs/http-surface.md</c> rule 1, as a predicate.</summary>
    private static bool IsConventionalSegment(string segment)
        => segment.StartsWith('{')
           || (segment.Length > 0
               && char.IsAsciiLetterUpper(segment[0])
               && segment.All(char.IsAsciiLetterOrDigit)
               && !segment.Equals("api", StringComparison.OrdinalIgnoreCase));

    /// <summary>Every controller the plugin ships.</summary>
    internal static IEnumerable<Type> PluginControllers()
        => typeof(PluginRoutes).Assembly.GetTypes().Where(type => typeof(ControllerBase).IsAssignableFrom(type) && !type.IsAbstract);

    /// <summary>Every action on every controller the plugin ships.</summary>
    internal static IEnumerable<MethodInfo> PluginActions()
        => PluginControllers().SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(action => action.GetCustomAttributes<HttpMethodAttribute>().Any());

    // ---- No specification or contract names a route the plugin does not serve (FR-015, SC-008) ----

    /// <summary>
    /// Contracts write a path either in full or relative to the base they state, and they write
    /// route parameters without the constraint the code carries. The rule is therefore about the
    /// path after normalisation, and the table states both sides of it before the scan exists.
    /// </summary>
    [Theory]
    [InlineData(true, "/Plugins/NewReleases/Releases")]
    [InlineData(true, "/Releases")]
    [InlineData(true, "/Admin/RunNow")]
    [InlineData(true, "/Releases/{id}/HaveIt")]
    [InlineData(true, "/Plugins/NewReleases/UserView")]
    [InlineData(false, "/api/releases")]
    [InlineData(false, "/Plugins/NewReleases/api/admin/run-now")]
    [InlineData(false, "/Releases/{id}/have-it")]
    [InlineData(false, "/Admin/Rebuild")]
    public void TheDocumentRule_AcceptsOnlyPathsThePluginActuallyServes(bool expected, string written)
    {
        Assert.Equal(expected, ServedRoutes().Contains(Normalise(written)));
    }

    [Fact]
    public void NoContractDocument_NamesARouteThePluginDoesNotServe()
    {
        var served = ServedRoutes();
        var offenders = new List<string>();

        foreach (var contract in Directory.EnumerateFiles(Path.Combine(RepositoryFiles.Root.FullName, "specs"), "*.md", SearchOption.AllDirectories)
                     .Where(file => file.Contains(Path.DirectorySeparatorChar + "contracts" + Path.DirectorySeparatorChar, StringComparison.Ordinal)))
        {
            foreach (Match written in PathInProse.Matches(File.ReadAllText(contract)))
            {
                var path = Normalise(written.Groups[1].Value);

                // A document may state the base the paths below it are relative to. That is a
                // prefix, not a route, and nothing serves it.
                if (path == PluginRoutes.Base)
                {
                    continue;
                }

                if (!served.Contains(path))
                {
                    offenders.Add(Path.GetRelativePath(RepositoryFiles.Root.FullName, contract) + ": " + written.Groups[1].Value);
                }
            }
        }

        Assert.True(offenders.Count == 0, string.Join("  |  ", offenders));
    }

    /// <summary>A path written in a contract: backticked, absolute, and not prose.</summary>
    private static readonly Regex PathInProse = new(@"`(/(?:api|Plugins/NewReleases|Releases|Artists|Status|Admin)[^`\s]*)`", RegexOptions.Compiled);

    /// <summary>
    /// The comparable form of a path: no leading slash, the stated base restored, route constraints
    /// dropped, and any trailing query or punctuation removed. Case is preserved — it is the whole
    /// point of the convention.
    /// </summary>
    private static string Normalise(string written)
    {
        var path = written.Trim('/', '.', ',');
        path = Regex.Replace(path, @"\{(\w+):\w+\}", "{$1}");
        path = path.Split('?')[0];

        if (path.StartsWith("api/", StringComparison.Ordinal))
        {
            path = PluginRoutes.Base + "/" + path;
        }

        return path.StartsWith(PluginRoutes.Base, StringComparison.Ordinal) ? path : PluginRoutes.Base + "/" + path;
    }

    /// <summary>Every path the plugin registers, normalised the same way.</summary>
    private static HashSet<string> ServedRoutes()
        => PluginControllers()
            .SelectMany(RoutesOf)
            .Select(route => Normalise(route.Split(' ')[1]))
            .ToHashSet(StringComparer.Ordinal);

    /// <summary>The <c>[Route]</c> template a controller declares.</summary>
    private static string ControllerRoute(Type controller)
        => controller.GetCustomAttribute<RouteAttribute>()?.Template
           ?? throw new InvalidOperationException($"{controller.Name} declares no [Route].");

    /// <summary>
    /// The full route of every action on <paramref name="controller"/>, as
    /// <c>"GET Plugins/NewReleases/Releases"</c>. Actions carry an
    /// <see cref="HttpMethodAttribute"/> whose template is relative to the controller's.
    /// </summary>
    internal static IReadOnlyList<string> RoutesOf(Type controller)
    {
        var prefix = ControllerRoute(controller);
        var routes = new List<string>();

        foreach (var action in controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            foreach (var http in action.GetCustomAttributes<HttpMethodAttribute>())
            {
                var method = string.Join('/', http.HttpMethods);
                var template = string.IsNullOrEmpty(http.Template) ? prefix : prefix + "/" + http.Template;
                routes.Add(method + " " + template);
            }
        }

        return routes;
    }
}
