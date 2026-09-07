using System.Security.Claims;
using Jellyfin.Data;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Plugin.NewReleases.Api;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Jellyfin.Plugin.NewReleases.Tests.Support;

/// <summary>Builds the request context a controller sees: a principal carrying `Jellyfin-UserId` and an <see cref="IUserManager"/> that knows the in-memory user.</summary>
internal static class ControllerContextFactory
{
    public static ControllerContext ForUser(Guid? userId)
    {
        var identity = new ClaimsIdentity(userId is { } id ? [new Claim(ReleasesController.UserIdClaim, id.ToString("N"))] : [], "Test");
        return new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) } };
    }

    /// <summary>An in-memory Jellyfin user with either access to every library or to the given folders only.</summary>
    public static User User(Guid id, bool allFolders, params Guid[] enabledFolders)
    {
        var user = new User("tester", "Jellyfin.Server.Implementations.Users.DefaultAuthenticationProvider", "Jellyfin.Server.Implementations.Users.DefaultPasswordResetProvider") { Id = id };
        user.SetPermission(PermissionKind.EnableAllFolders, allFolders);
        user.SetPreference(PreferenceKind.EnabledFolders, enabledFolders);
        return user;
    }

    public static IUserManager UserManager(params User[] users)
    {
        var manager = Substitute.For<IUserManager>();
        manager.GetUserById(Arg.Any<Guid>()).Returns(call => users.FirstOrDefault(u => u.Id == call.Arg<Guid>()));
        return manager;
    }
}
