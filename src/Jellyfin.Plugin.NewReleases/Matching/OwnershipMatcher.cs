using Jellyfin.Plugin.NewReleases.Library;
using Jellyfin.Plugin.NewReleases.Model;

namespace Jellyfin.Plugin.NewReleases.Matching;

/// <summary>Ownership algorithm from data-model.md (FR-005, FR-005a): library album candidate → best-overlapping edition → Owned / Incomplete / Missing.</summary>
public static class OwnershipMatcher
{
    public static OwnershipResult Decide(Release release, IReadOnlyList<Edition> editions, IReadOnlyList<LibraryAlbumSnapshot> albums)
    {
        var (candidate, method) = FindCandidate(release, editions, albums);
        if (candidate is null)
        {
            return OwnershipResult.Missing;
        }

        var edition = editions[0];
        var missing = edition.Tracks.Except(candidate.NormalizedTrackTitles, StringComparer.Ordinal).ToArray();
        return new OwnershipResult(missing.Length == 0 ? OwnershipState.Owned : OwnershipState.Incomplete, method, candidate.JellyfinId, edition.Id, missing);
    }

    /// <summary>Step 1: identifier match (canonical MusicBrainz release group, or a stored MusicBrainz edition id), else nothing yet.</summary>
    private static (LibraryAlbumSnapshot? Album, string? Method) FindCandidate(Release release, IReadOnlyList<Edition> editions, IReadOnlyList<LibraryAlbumSnapshot> albums)
    {
        var editionIds = editions.Where(e => e.Source == "musicbrainz").Select(e => e.SourceEditionId).ToHashSet(StringComparer.Ordinal);
        var byIdentifier = albums.FirstOrDefault(a =>
            (release.CanonicalSource == "musicbrainz" && a.MusicBrainzReleaseGroupId == release.CanonicalSourceId)
            || (a.MusicBrainzReleaseId is not null && editionIds.Contains(a.MusicBrainzReleaseId)));
        return byIdentifier is not null ? (byIdentifier, "Identifier") : (null, null);
    }
}
