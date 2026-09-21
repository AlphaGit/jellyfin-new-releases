using System.Reflection;
using Jellyfin.Plugin.NewReleases.Api;
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
