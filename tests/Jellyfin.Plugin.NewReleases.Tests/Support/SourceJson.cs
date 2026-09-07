using System.Text.Json;

namespace Jellyfin.Plugin.NewReleases.Tests.Support;

/// <summary>Bodies in the shape of the recorded fixtures (see tests/fixtures/*), with the values a scenario needs. Shapes are pinned by the source tests; these build controlled variants.</summary>
internal static class SourceJson
{
    private static string J(object o) => JsonSerializer.Serialize(o);

    public static class MusicBrainz
    {
        public static string Search(params (string Id, string Name, int Score)[] artists)
            => J(new { created = "2026-09-06T12:00:00Z", count = artists.Length, offset = 0, artists = artists.Select(a => new { id = a.Id, name = a.Name, score = a.Score }) });

        /// <summary>One Official release per release group; `first-release-date` as given (null → omitted).</summary>
        public static string Releases(params (string GroupId, string Title, string Primary, string[] Secondaries, string? Date)[] groups)
            => J(new Dictionary<string, object?>
            {
                ["release-count"] = groups.Length,
                ["release-offset"] = 0,
                ["releases"] = groups.Select(g => new Dictionary<string, object?>
                {
                    ["id"] = "rel-" + g.GroupId,
                    ["title"] = g.Title,
                    ["status"] = "Official",
                    ["release-group"] = new Dictionary<string, object?>
                    {
                        ["id"] = g.GroupId,
                        ["title"] = g.Title,
                        ["primary-type"] = g.Primary,
                        ["secondary-types"] = g.Secondaries,
                        ["first-release-date"] = g.Date ?? string.Empty,
                    },
                }),
            });

        public static string Editions(params (string ReleaseId, string Title, string[] Tracks)[] editions)
            => J(new Dictionary<string, object?>
            {
                ["release-count"] = editions.Length,
                ["release-offset"] = 0,
                ["releases"] = editions.Select(e => new Dictionary<string, object?>
                {
                    ["id"] = e.ReleaseId,
                    ["title"] = e.Title,
                    ["status"] = "Official",
                    ["media"] = new[] { new { format = "CD", position = 1, tracks = e.Tracks.Select((t, i) => new { position = i + 1, title = t }) } },
                }),
            });
    }

    public static class Deezer
    {
        public static string Search(params (long Id, string Name)[] artists)
            => J(new { data = artists.Select(a => new { id = a.Id, name = a.Name, link = $"https://www.deezer.com/artist/{a.Id}", nb_album = 1, type = "artist" }), total = artists.Length });

        public static string Albums(params (long Id, string Title, string RecordType, string Date)[] albums)
            => J(new { data = albums.Select(a => new { id = a.Id, title = a.Title, link = $"https://www.deezer.com/album/{a.Id}", record_type = a.RecordType, release_date = a.Date, type = "album" }), total = albums.Length });

        public static string Album(long id, string title)
            => J(new { id, title, link = $"https://www.deezer.com/album/{id}", record_type = "album", release_date = "2001-01-01", nb_tracks = 0, type = "album" });

        public static string Tracks(params string[] titles)
            => J(new { data = titles.Select((t, i) => new { id = i + 1, title = t, title_short = t, track_position = i + 1, type = "track" }), total = titles.Length });

        public static string Empty => J(new { data = Array.Empty<object>(), total = 0 });
    }
}
