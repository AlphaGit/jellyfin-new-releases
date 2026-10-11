using Jellyfin.Plugin.NewReleases.Library;
using Jellyfin.Plugin.NewReleases.Matching;
using Jellyfin.Plugin.NewReleases.Model;
using Jellyfin.Plugin.NewReleases.Sources;
using Jellyfin.Plugin.NewReleases.Tests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Plugin.NewReleases.Tests.Sources;

public sealed class MusicBrainzSourceTests : IAsyncLifetime
{
    private const string DaftPunkMbid = "056e4f3e-d505-4dad-8ec1-d04f521cbb56";
    private SourceHarness _h = null!;

    public async Task InitializeAsync() => _h = await SourceHarness.CreateAsync();

    public async Task DisposeAsync() => await _h.DisposeAsync();

    private MusicBrainzSource Source() => new(_h.HttpClient, NullLogger<MusicBrainzSource>.Instance);

    private static LibraryArtistSnapshot Artist(string name, string? mbid = null)
        => new(mbid ?? "name:" + name.ToLowerInvariant(), Guid.NewGuid(), name, mbid, [], []);

    [Fact]
    public async Task MatchArtistAsync_SnapshotWithMbid_IsMatchedWithoutAnyRequest()
    {
        var match = await Source().MatchArtistAsync(Artist("Daft Punk", DaftPunkMbid), CancellationToken.None);

        Assert.Equal(ArtistMatch.Matched(DaftPunkMbid), match);
        Assert.Empty(_h.RequestedUrls);
    }

    private const string ArtistSearch = @"musicbrainz\.org/ws/2/artist\?query=";

    /// <summary>Serves a search body with the given (id, name, score) rows, for the score rules the recorded fixtures do not cover.</summary>
    private void SearchReturns(params (string Id, string Name, int Score)[] artists)
        => _h.Http.OnUrlPattern(ArtistSearch, System.Net.HttpStatusCode.OK, SourceJson.MusicBrainz.Search(artists));

    [Fact]
    public async Task MatchArtistAsync_ConfidentTopResult_IsMatched()
    {
        _h.Fixture(ArtistSearch, "musicbrainz/artist_search_confident.json"); // Daft Punk 100, runner-up 66

        var match = await Source().MatchArtistAsync(Artist("Daft Punk"), CancellationToken.None);

        Assert.Equal(ArtistMatch.Matched(DaftPunkMbid), match);
    }

    [Fact]
    public async Task MatchArtistAsync_RunnerUpWithinFivePoints_IsUnmatchedAsAmbiguous()
    {
        _h.Fixture(ArtistSearch, "musicbrainz/artist_search_ambiguous.json"); // Nirvana 100 vs Nirvana 96

        var match = await Source().MatchArtistAsync(Artist("Nirvana"), CancellationToken.None);

        Assert.Equal(MatchStatus.Unmatched, match.Status);
        Assert.Equal("ambiguous (score 100 vs 96)", match.Reason);
    }

    [Fact]
    public async Task MatchArtistAsync_RunnerUpSixPointsBehind_IsMatched()
    {
        SearchReturns(("mbid-1", "Blur", 90), ("mbid-2", "Blur Tribute", 84));

        Assert.Equal(ArtistMatch.Matched("mbid-1"), await Source().MatchArtistAsync(Artist("Blur"), CancellationToken.None));
    }

    /// <summary>U130: the ambiguity gap is inclusive, so a runner-up exactly 5 points behind is still ambiguous. `U76` sits at 4 points and `U77` at 6; this is the boundary between them.</summary>
    [Fact]
    public async Task MatchArtistAsync_RunnerUpExactlyFivePointsBehind_IsUnmatchedAsAmbiguous()
    {
        SearchReturns(("mbid-1", "Blur", 90), ("mbid-2", "Blur Tribute", 85));

        var match = await Source().MatchArtistAsync(Artist("Blur"), CancellationToken.None);

        Assert.Equal(MatchStatus.Unmatched, match.Status);
        Assert.Equal("ambiguous (score 90 vs 85)", match.Reason);
    }

    /// <summary>U129: 85 is the inclusive minimum, so a top score of exactly 85 matches. `U78` sits at 84; this is the boundary above it.</summary>
    [Fact]
    public async Task MatchArtistAsync_TopScoreExactly85_IsMatched()
    {
        SearchReturns(("mbid-1", "Blur", 85), ("mbid-2", "Blur Tribute", 79));

        Assert.Equal(ArtistMatch.Matched("mbid-1"), await Source().MatchArtistAsync(Artist("Blur"), CancellationToken.None));
    }

    [Fact]
    public async Task MatchArtistAsync_TopScoreBelow85_IsUnmatchedAsLowScore()
    {
        _h.Fixture(ArtistSearch, "musicbrainz/artist_search_low_score.json"); // 84 and 40

        var match = await Source().MatchArtistAsync(Artist("Daft Punkk Orchestra Zzz"), CancellationToken.None);

        Assert.Equal(MatchStatus.Unmatched, match.Status);
        Assert.Equal("low score (84)", match.Reason);
    }

    [Fact]
    public async Task MatchArtistAsync_NoResults_IsUnmatchedAsNoResult()
    {
        _h.Fixture(ArtistSearch, "musicbrainz/artist_search_empty.json");

        var match = await Source().MatchArtistAsync(Artist("zzqxjv nonexistent artist qqq"), CancellationToken.None);

        // 008 FR-015 adds the attempt detail, compared in its own test; everything else is the record a bare Unmatched has.
        Assert.Equal(ArtistMatch.Unmatched("no result") with { Detail = match.Detail }, match);
    }

    /// <summary>Spec 008 FR-015: an ambiguous result keeps the candidates the search returned, best first, with score and ID.</summary>
    [Fact]
    public async Task MatchArtistAsync_Ambiguous_KeepsTheCandidatesInScoreOrderWithScoreAndId()
    {
        _h.Fixture(ArtistSearch, "musicbrainz/artist_search_ambiguous.json"); // Nirvana 100, Nirvana 96, Approaching Nirvana 74

        var match = await Source().MatchArtistAsync(Artist("Nirvana"), CancellationToken.None);

        Assert.Equal(
            [
                new MatchCandidate("Nirvana", 100, "5b11f4ce-a62d-471e-81fc-a69a8278c7da"),
                new MatchCandidate("Nirvana", 96, "9282c8b4-ca0b-4c6b-b7e3-4f7762dfc4d6"),
                new MatchCandidate("Approaching Nirvana", 74, "c3a64a25-251b-4d03-afba-1471440245b8"),
            ],
            match.Detail?.Candidates ?? []);
    }

    /// <summary>Spec 008 FR-016: a fix shows at most 3 candidates, so no more than 3 are kept.</summary>
    [Fact]
    public async Task MatchArtistAsync_FiveResultsScoringBelow85_KeepsOnlyTheFirstThree()
    {
        SearchReturns(("mbid-1", "Blurt", 80), ("mbid-2", "Blurr", 70), ("mbid-3", "Blue", 60), ("mbid-4", "Bluo", 50), ("mbid-5", "Blum", 40));

        var match = await Source().MatchArtistAsync(Artist("Blur"), CancellationToken.None);

        Assert.Equal(["mbid-1", "mbid-2", "mbid-3"], match.Detail?.Candidates.Select(c => c.Id) ?? []);
    }

    /// <summary>Spec 008 FR-015: a search that found nobody records that, as an empty candidate list.</summary>
    [Fact]
    public async Task MatchArtistAsync_NoResults_RecordsAnEmptyCandidateList()
    {
        _h.Fixture(ArtistSearch, "musicbrainz/artist_search_empty.json");

        var match = await Source().MatchArtistAsync(Artist("zzqxjv nonexistent artist qqq"), CancellationToken.None);

        Assert.NotNull(match.Detail);
        Assert.Empty(match.Detail.Candidates);
    }

    /// <summary>Spec 008 FR-015: keeping the candidates sends nothing new: an unmatched attempt is still the one search.</summary>
    [Fact]
    public async Task MatchArtistAsync_Ambiguous_SendsOnlyTheSearch()
    {
        _h.Fixture(ArtistSearch, "musicbrainz/artist_search_ambiguous.json");
        _h.Http.OnUrlPattern(@"musicbrainz\.org/ws/2/artist/", System.Net.HttpStatusCode.OK, "{}"); // answers any lookup a change might add

        await Source().MatchArtistAsync(Artist("Nirvana"), CancellationToken.None);

        Assert.Equal(["https://musicbrainz.org/ws/2/artist?query=artist:%22Nirvana%22&limit=5&fmt=json"], _h.RequestedUrls);
    }

    /// <summary>Spec 008 FR-011: every reason the match writes names its failed step, so the admin page can show the trail.</summary>
    [Theory]
    [InlineData("musicbrainz/artist_search_empty.json", "Search")]
    [InlineData("musicbrainz/artist_search_low_score.json", "Score")]
    [InlineData("musicbrainz/artist_search_ambiguous.json", "Unique")]
    public async Task MatchArtistAsync_EveryUnmatchedReason_ParsesToItsFailedStep(string fixture, string step)
    {
        _h.Fixture(ArtistSearch, fixture);

        var match = await Source().MatchArtistAsync(Artist("Nirvana"), CancellationToken.None);

        Assert.Equal(step, MatchReasons.Parse(Source().Id, match.Reason)?.Step);
    }

    private const string ReleaseBrowse = @"musicbrainz\.org/ws/2/release\?artist=";

    [Fact]
    public async Task FetchCataloguePageAsync_GroupsByReleaseGroupMapsTypesKeepsPartialDatesAndLinksTheReleaseGroup()
    {
        _h.Fixture(ReleaseBrowse, "musicbrainz/releases_page1.json"); // 8 releases in 7 release groups

        var page = await Source().FetchCataloguePageAsync(DaftPunkMbid, 0, CancellationToken.None);

        Assert.Equal(7, page.Items.Count);
        var discovery = page.Items.Single(i => i.Title == "Discovery");
        Assert.Equal(("48117b90-a16e-34ca-a514-19c702df1158", "https://musicbrainz.org/release-group/48117b90-a16e-34ca-a514-19c702df1158", ReleaseType.Album, "2001-02-26"),
            (discovery.SourceReleaseId, discovery.Url, discovery.PrimaryType, discovery.Date));
        Assert.Empty(discovery.SecondaryTypes);
        var alive = page.Items.Single(i => i.Title == "Alive 1997");
        Assert.Equal((ReleaseType.Album, "1998"), (alive.PrimaryType, alive.Date));
        Assert.Equal([ReleaseType.Live], alive.SecondaryTypes);
        Assert.Equal([ReleaseType.Other], page.Items.Single(i => i.Title.StartsWith("Generic Radio Interview")).SecondaryTypes);
        Assert.Equal(ReleaseType.Single, page.Items.Single(i => i.Title == "Da Funk").PrimaryType);
    }

    [Fact]
    public async Task FetchCataloguePageAsync_NextOffsetAdvancesBy100WhileTheCountExceedsIt_NullOnTheLastPage()
    {
        _h.Fixture(ReleaseBrowse + ".*offset=0&", "musicbrainz/releases_page1.json");   // release-count 110
        _h.Fixture(ReleaseBrowse + ".*offset=100&", "musicbrainz/releases_page2.json"); // release-count 110
        var source = Source();

        var first = await source.FetchCataloguePageAsync(DaftPunkMbid, 0, CancellationToken.None);
        var last = await source.FetchCataloguePageAsync(DaftPunkMbid, 100, CancellationToken.None);

        Assert.Equal((100, 110), (first.NextOffset, first.Total));
        Assert.Equal(((int?)null, 4), (last.NextOffset, last.Items.Count));
    }

    /// <summary>U132: a page past the end of the catalogue yields no items and stops the paging pass.</summary>
    [Fact]
    public async Task FetchCataloguePageAsync_EmptyPage_YieldsNoItemsAndStopsPaging()
    {
        _h.Fixture(ReleaseBrowse, "musicbrainz/releases_empty.json"); // offset 100 000, release-count 296, no releases

        var page = await Source().FetchCataloguePageAsync(DaftPunkMbid, 100_000, CancellationToken.None);

        Assert.Empty(page.Items);
        Assert.Null(page.NextOffset);
        Assert.Equal(296, page.Total);
    }

    [Fact]
    public async Task FetchEditionsAsync_OneEditionPerOfficialReleaseWithAllMediaTracksNormalized()
    {
        _h.Fixture(@"musicbrainz\.org/ws/2/release\?release-group=", "musicbrainz/editions_two_official.json");

        var editions = await Source().FetchEditionsAsync("48117b90-a16e-34ca-a514-19c702df1158", CancellationToken.None);

        Assert.Equal(2, editions.Count);
        var france = editions.Single(e => e.SourceEditionId == "d073287b-d1bd-4f11-a933-a4386f8cf701");
        var japan = editions.Single(e => e.SourceEditionId == "51467269-3122-3d7e-92b2-0f0a694d30c1");
        Assert.Equal(("Discovery", 14), (france.Title, france.NormalizedTrackTitles.Count));
        Assert.Equal("harder better faster stronger", france.NormalizedTrackTitles[3]);
        Assert.Equal(15, japan.NormalizedTrackTitles.Count);
        Assert.Equal("one more time", japan.NormalizedTrackTitles[14][..13]); // "(Romanthony's Unplugged)" is not a feat. segment and stays
        Assert.Equal("one more time romanthonys unplugged", japan.NormalizedTrackTitles[14]);
    }

    /// <summary>FR-017: only artist names and public identifiers leave the server; the URLs are the contract's, nothing more.</summary>
    [Fact]
    public async Task Requests_UseExactlyTheContractEndpointsWithOnlyNamesAndIdsInTheQuery()
    {
        _h.Fixture(ArtistSearch, "musicbrainz/artist_search_confident.json");
        _h.Fixture(ReleaseBrowse, "musicbrainz/releases_page1.json");
        _h.Fixture(@"release\?release-group=", "musicbrainz/editions_two_official.json");
        var source = Source();

        await source.MatchArtistAsync(Artist("Daft Punk"), CancellationToken.None);
        await source.FetchCataloguePageAsync(DaftPunkMbid, 0, CancellationToken.None);
        await source.FetchEditionsAsync("48117b90-a16e-34ca-a514-19c702df1158", CancellationToken.None);

        Assert.Equal(
            [
                "https://musicbrainz.org/ws/2/artist?query=artist:%22Daft%20Punk%22&limit=5&fmt=json",
                $"https://musicbrainz.org/ws/2/release?artist={DaftPunkMbid}&status=official&inc=release-groups+artist-credits&limit=100&offset=0&fmt=json",
                "https://musicbrainz.org/ws/2/release?release-group=48117b90-a16e-34ca-a514-19c702df1158&status=official&inc=recordings+media&limit=25&offset=0&fmt=json",
            ],
            _h.RequestedUrls);
    }

    /// <summary>A failing source is a Failed outcome for the pair (FR-014), never a silent Unmatched or an empty catalogue.</summary>
    [Fact]
    public async Task PersistentServiceUnavailable_SurfacesAsAnException()
    {
        _h.Fixture(".*", "musicbrainz/error_503_retry_after.txt", System.Net.HttpStatusCode.ServiceUnavailable);
        var source = Source();

        var matching = source.MatchArtistAsync(Artist("Daft Punk"), CancellationToken.None);
        await Assert.ThrowsAsync<HttpRequestException>(() => _h.RunAdvancingAsync(matching, TimeSpan.FromMilliseconds(500)));

        var paging = source.FetchCataloguePageAsync(DaftPunkMbid, 0, CancellationToken.None);
        await Assert.ThrowsAsync<HttpRequestException>(() => _h.RunAdvancingAsync(paging, TimeSpan.FromMilliseconds(500)));
    }

    private const string SevenLions = "75c016da-48e4-4992-8a10-b7128189b272";
    private const string KillTheNoise = "a9930a14-8fbb-435a-ad25-f4b6cae71523";

    /// <summary>009 FR-005: a joint artist's IDs are parsed and joined, with no request.</summary>
    [Fact]
    public async Task MatchArtistAsync_JointIds_MatchedToTheParsedIds_WithNoRequest()
    {
        var match = await Source().MatchArtistAsync(Artist("Seven Lions + Kill the Noise", $" {SevenLions} ; {KillTheNoise} "), CancellationToken.None);

        Assert.Equal(ArtistMatch.Matched($"{SevenLions};{KillTheNoise}"), match);
        Assert.Empty(_h.RequestedUrls);
    }

    /// <summary>009 US2-AS1: one request, one ID: the first.</summary>
    [Fact]
    public async Task FetchCataloguePageAsync_JointIds_BrowsesTheFirstIdOnly_WithArtistCredits()
    {
        _h.Fixture(ReleaseBrowse, "musicbrainz/releases_joint_artist.json");

        await Source().FetchCataloguePageAsync($"{SevenLions};{KillTheNoise}", 0, CancellationToken.None);

        Assert.Equal([$"https://musicbrainz.org/ws/2/release?artist={SevenLions}&status=official&inc=release-groups+artist-credits&limit=100&offset=0&fmt=json"], _h.RequestedUrls);
    }

    /// <summary>009 US2-AS2: only releases credited to every ID are kept; another credited artist is allowed.</summary>
    [Fact]
    public async Task FetchCataloguePageAsync_JointIds_KeepsOnlyReleasesCreditedToEveryId()
    {
        _h.Fixture(ReleaseBrowse, "musicbrainz/releases_joint_artist.json"); // Ophelia Volume 1 (solo), The Blood (both), Horizon (both + others); release-count 158

        var page = await Source().FetchCataloguePageAsync($"{SevenLions};{KillTheNoise}", 0, CancellationToken.None);

        Assert.Equal(["The Blood", "Horizon"], page.Items.Select(i => i.Title));
        Assert.Equal((100, 158), (page.NextOffset, page.Total));
    }

    /// <summary>009 US2-AS4: one ID keeps every release, as before, and each item carries its credit.</summary>
    [Fact]
    public async Task FetchCataloguePageAsync_OneId_KeepsEveryRelease_WithItsCredit()
    {
        _h.Fixture(ReleaseBrowse, "musicbrainz/releases_joint_artist.json");

        var page = await Source().FetchCataloguePageAsync(SevenLions, 0, CancellationToken.None);

        Assert.Equal(["Ophelia Volume 1", "The Blood", "Horizon"], page.Items.Select(i => i.Title));
        Assert.Equal([new CreditedArtist(SevenLions, "Seven Lions"), new CreditedArtist(KillTheNoise, "Kill the Noise")], page.Items[1].Credit); // the artist's own name, not the credited spelling
        Assert.Equal(4, page.Items[2].Credit.Count);
    }

    /// <summary>009 R5: a recorded page without artist-credit gives empty credits.</summary>
    [Fact]
    public async Task FetchCataloguePageAsync_NoArtistCredit_GivesEmptyCredits()
    {
        _h.Fixture(ReleaseBrowse, "musicbrainz/releases_page1.json");

        var page = await Source().FetchCataloguePageAsync(DaftPunkMbid, 0, CancellationToken.None);

        Assert.All(page.Items, i => Assert.Empty(i.Credit));
    }
}
