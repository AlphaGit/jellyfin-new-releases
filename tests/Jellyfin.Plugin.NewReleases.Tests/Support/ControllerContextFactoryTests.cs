using Jellyfin.Plugin.NewReleases.Api;
using Xunit;

namespace Jellyfin.Plugin.NewReleases.Tests.Support;

/// <summary>
/// The request context the suite builds must be the one the host builds (004 FR-006). Jellyfin's
/// `CustomAuthenticationHandler` writes the `Jellyfin-UserId` claim for every authenticated request,
/// and writes `Guid.Empty` in `N` format when there is no user: an API key, or a deleted user's token.
/// </summary>
public class ControllerContextFactoryTests
{
    [Fact]
    public void ACallerWithoutAUser_CarriesTheEmptyUserIdTheHostSends()
    {
        var principal = ControllerContextFactory.ForCallerWithoutUser().HttpContext.User;

        var claim = Assert.Single(principal.FindAll(ReleasesController.UserIdClaim));
        Assert.Equal("00000000000000000000000000000000", claim.Value);
    }
}
