using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.NewReleases.Api;

/// <summary>
/// Serves the user-facing HTML fragment consumed by the Plugin Pages (IAmParadox27) integration.
/// Plugin Pages fetches this URL from the Jellyfin SPA and injects the response into the page, so
/// the response must be an HTML fragment (no &lt;html&gt;/&lt;body&gt;) with an inline script that
/// self-initializes on insertion.
/// </summary>
[ApiController]
[Authorize]
[Route("Plugins/NewReleases/UserView")]
public sealed class UserViewController : ControllerBase
{
    private const string ResourceName = "Jellyfin.Plugin.NewReleases.Web.user-view.html";

    /// <summary>
    /// Returns the embedded HTML fragment for the user-facing new-releases view.
    /// </summary>
    /// <returns>HTML stream.</returns>
    [HttpGet]
    [Produces("text/html")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult GetUserView()
    {
        var stream = typeof(UserViewController).Assembly.GetManifestResourceStream(ResourceName);
        return stream is null ? NotFound() : File(stream, "text/html; charset=utf-8");
    }
}
