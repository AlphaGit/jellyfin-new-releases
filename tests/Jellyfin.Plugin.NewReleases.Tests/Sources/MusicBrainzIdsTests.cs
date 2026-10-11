using Jellyfin.Plugin.NewReleases.Sources;
using Xunit;

namespace Jellyfin.Plugin.NewReleases.Tests.Sources;

/// <summary>Joint-artist IDs as Jellyfin stores them (spec 009 FR-005, edge cases; research R4).</summary>
public class MusicBrainzIdsTests
{
    private const string SevenLions = "75c016da-48e4-4992-8a10-b7128189b272";
    private const string KillTheNoise = "a9930a14-8fbb-435a-ad25-f4b6cae71523";
    private const string VariousArtists = "89ad4ac3-39f7-470e-963a-56509c546377";

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ; ")]
    public void NoId_GivesNothing(string? value)
    {
        Assert.Empty(MusicBrainzIds.Parse(value));
    }

    [Fact]
    public void OneId_GivesThatId()
    {
        Assert.Equal([SevenLions], MusicBrainzIds.Parse(SevenLions));
    }

    [Theory]
    [InlineData(SevenLions + ";" + KillTheNoise)]
    [InlineData(" " + SevenLions + " ; " + KillTheNoise + " ")]
    public void JointIds_AreSplitAndTrimmedInOrder(string value)
    {
        Assert.Equal([SevenLions, KillTheNoise], MusicBrainzIds.Parse(value));
    }

    [Fact]
    public void DuplicateId_IsKeptOnce()
    {
        Assert.Equal([SevenLions], MusicBrainzIds.Parse(SevenLions + ";" + SevenLions.ToUpperInvariant()));
    }

    [Fact]
    public void PlaceholderId_IsDropped()
    {
        Assert.Equal([SevenLions], MusicBrainzIds.Parse(SevenLions + ";" + VariousArtists));
    }

    [Fact]
    public void OnlyPlaceholders_GiveNothing()
    {
        Assert.Empty(MusicBrainzIds.Parse(VariousArtists + ";125ec42a-7229-4250-afc5-e057484327fe"));
    }
}
