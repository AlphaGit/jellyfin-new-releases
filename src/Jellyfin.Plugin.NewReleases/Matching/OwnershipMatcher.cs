using Jellyfin.Plugin.NewReleases.Library;
using Jellyfin.Plugin.NewReleases.Model;

namespace Jellyfin.Plugin.NewReleases.Matching;

/// <summary>Ownership algorithm from data-model.md (FR-005, FR-005a): library album candidate → best-overlapping edition → Owned / Incomplete / Missing.</summary>
public static class OwnershipMatcher
{
    public static OwnershipResult Decide(Release release, IReadOnlyList<Edition> editions, IReadOnlyList<LibraryAlbumSnapshot> albums)
    {
        var (candidate, method) = FindCandidate(release, albums);
        if (candidate is null)
        {
            return OwnershipResult.Missing;
        }

        var edition = editions[0];
        var missing = edition.Tracks.Except(candidate.NormalizedTrackTitles, StringComparer.Ordinal).ToArray();
        return new OwnershipResult(missing.Length == 0 ? OwnershipState.Owned : OwnershipState.Incomplete, method, candidate.JellyfinId, edition.Id, missing);
    }

    /// <summary>Step 1: identifier match first (MusicBrainz release group), else nothing yet.</summary>
    private static (LibraryAlbumSnapshot? Album, string? Method) FindCandidate(Release release, IReadOnlyList<LibraryAlbumSnapshot> albums)
    {
        if (release.CanonicalSource == "musicbrainz")
        {
            var byReleaseGroup = albums.FirstOrDefault(a => a.MusicBrainzReleaseGroupId == release.CanonicalSourceId);
            if (byReleaseGroup is not null)
            {
                return (byReleaseGroup, "Identifier");
            }
        }

        return (null, null);
    }
}
