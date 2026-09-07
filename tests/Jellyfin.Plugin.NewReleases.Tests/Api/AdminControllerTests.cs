using Jellyfin.Plugin.NewReleases.Api;
using Jellyfin.Plugin.NewReleases.Configuration;
using Jellyfin.Plugin.NewReleases.Tests.Support;
using MediaBrowser.Common.Api;
using MediaBrowser.Model.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.NewReleases.Tests.Api;

public sealed class AdminControllerTests : IAsyncLifetime
{
    private readonly TimeProviderStub _clock = new(new DateTimeOffset(2026, 9, 6, 12, 0, 0, TimeSpan.Zero));
    private readonly PluginConfiguration _configuration = new();
    private readonly ITaskManager _tasks = Substitute.For<ITaskManager>();
    private TestDatabase _db = null!;

    public async Task InitializeAsync() => _db = await TestDatabase.CreateAsync(_clock);

    public async Task DisposeAsync() => await _db.DisposeAsync();

    private AdminController Controller() => new(_db.Artists, _db.Releases, _db.Archive, _db.SourceState, _tasks, _clock, NullLogger<AdminController>.Instance, () => _configuration);

    /// <summary>The pipeline enforces the policy; the test pins the declaration (FR-013: admin-only actions).</summary>
    [Fact]
    public void Controller_RequiresElevation()
    {
        var authorize = typeof(AdminController).GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).Cast<AuthorizeAttribute>();

        Assert.Contains(authorize, a => a.Policy == Policies.RequiresElevation);
    }
}
