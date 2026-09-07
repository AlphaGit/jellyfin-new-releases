using Xunit;

namespace Jellyfin.Plugin.NewReleases.Tests.Acceptance;

/// <summary>User Story 1 acceptance scenarios (spec.md US1-AS1…AS8) through the real task and controller.</summary>
public sealed class BrowseReleasesTests : IAsyncLifetime
{
    private AcceptanceRig _rig = null!;

    public async Task InitializeAsync() => _rig = await AcceptanceRig.CreateAsync();

    public async Task DisposeAsync() => await _rig.DisposeAsync();

    private static readonly string[] Discovery = ["One More Time", "Aerodynamic", "Digital Love"];
    private static readonly string[] Homework = ["Daftendirekt", "Da Funk", "Around the World"];

    /// <summary>Library: A with X, Y. Source lists X, Y, Z. Deezer only (MusicBrainz disabled) so the run stays fast.</summary>
    private void LibraryHasXY_SourceListsXYZ()
    {
        _rig.Harness.Configuration.MusicBrainzEnabled = false;
        _rig.Library_.Artist("Daft Punk");
        _rig.Library_.Album("Discovery", "Daft Punk", AcceptanceRig.Library, trackTitles: Discovery);
        _rig.Library_.Album("Homework", "Daft Punk", AcceptanceRig.Library, trackTitles: Homework);
        _rig.DeezerArtist(27, "Daft Punk", (1, "Discovery", "album", "2001-03-07"), (2, "Homework", "album", "1997-01-16"), (3, "Alive 2007", "album", "2007-11-16"))
            .DeezerAlbum(1, "Discovery", Discovery)
            .DeezerAlbum(2, "Homework", Homework)
            .DeezerAlbum(3, "Alive 2007", "Robot Rock / Oh Yeah", "Touch It / Technologic");
    }

    [Fact]
    public async Task A1_ZIsListedUnderA_XAndYAreNot()
    {
        LibraryHasXY_SourceListsXYZ();

        await _rig.RunAsync();
        var list = await _rig.ListAsync();

        var only = Assert.Single(list.Items);
        Assert.Equal(("Alive 2007", "Daft Punk", "Missing"), (only.Title, only.ArtistName, only.State));
    }
}
