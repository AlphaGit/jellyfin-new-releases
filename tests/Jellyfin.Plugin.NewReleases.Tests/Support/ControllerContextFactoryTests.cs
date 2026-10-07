using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Jellyfin.Plugin.NewReleases.Tests.Support;

/// <summary>
/// The request context the suite builds must be the one the host builds (004 FR-006). Jellyfin's
/// `CustomAuthenticationHandler` writes the `Jellyfin-UserId` claim for every authenticated request,
/// and writes `Guid.Empty` in `N` format when there is no user: an API key, or a deleted user's token.
/// </summary>
public class ControllerContextFactoryTests
{
    /// <summary>The claim type the host writes (`InternalClaimTypes.UserId`), stated here rather than read from the plugin, so a wrong constant cannot agree with itself.</summary>
    private const string HostUserIdClaim = "Jellyfin-UserId";

    [Fact]
    public void ACallerWithoutAUser_CarriesTheEmptyUserIdTheHostSends()
    {
        var principal = ControllerContextFactory.ForCallerWithoutUser().HttpContext.User;

        var claim = Assert.Single(principal.FindAll(HostUserIdClaim));
        Assert.Equal("00000000000000000000000000000000", claim.Value);
    }

    /// <summary>004 FR-007: one way to build a caller with no user, and one for a caller with a user, so a future per-user endpoint is tested the same way.</summary>
    [Fact]
    public void TheFactory_HasOneBuilderForEachKindOfCaller()
    {
        var builders = typeof(ControllerContextFactory).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.ReturnType == typeof(ControllerContext))
            .Select(m => m.Name)
            .Order();

        Assert.Equal(["ForCallerWithoutUser", "ForUser"], builders);
    }

    /// <summary>The host's `UserManager.GetUserById` throws for an empty id (`UserManager.cs:125`); a double that returned null would hide a missing guard.</summary>
    [Fact]
    public void TheUserManager_RejectsAnEmptyId_AsTheHostDoes()
    {
        var users = ControllerContextFactory.UserManager();

        Assert.Throws<ArgumentException>(() => users.GetUserById(Guid.Empty));
    }
}
