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

        if (editions.Count == 0)
        {
            // Step 2: the caller fetches editions (all sources listing the release), then calls again.
            return new OwnershipResult(OwnershipState.Missing, method, candidate.JellyfinId, null, [], NeedsEditions: true);
        }

        // Step 3: the edition whose track list overlaps the library album most; deterministic tie-breaks.
        var owned = candidate.NormalizedTrackTitles.ToHashSet(StringComparer.Ordinal);
        var (edition, missing) = editions
            .Select(e => (Edition: e, Missing: e.Tracks.Where(t => !owned.Contains(t)).ToArray()))
            .OrderByDescending(x => x.Edition.Tracks.Count - x.Missing.Length)
            .ThenBy(x => x.Missing.Length)
            .ThenBy(x => x.Edition.Source == "musicbrainz" ? 0 : 1)
            .ThenBy(x => x.Edition.SourceEditionId, StringComparer.Ordinal)
            .First();
        return new OwnershipResult(missing.Length == 0 ? OwnershipState.Owned : OwnershipState.Incomplete, method, candidate.JellyfinId, edition.Id, missing);
    }

    /// <summary>Step 1: identifier match (canonical MusicBrainz release group, or a stored MusicBrainz edition id), else nothing yet.</summary>
    private static (LibraryAlbumSnapshot? Album, string? Method) FindCandidate(Release release, IReadOnlyList<Edition> editions, IReadOnlyList<LibraryAlbumSnapshot> albums)
    {
        var editionIds = editions.Where(e => e.Source == "musicbrainz").Select(e => e.SourceEditionId).ToHashSet(StringComparer.Ordinal);
        var byIdentifier = albums.FirstOrDefault(a =>
            (release.CanonicalSource == "musicbrainz" && a.MusicBrainzReleaseGroupId == release.CanonicalSourceId)
            || (a.MusicBrainzReleaseId is not null && editionIds.Contains(a.MusicBrainzReleaseId)));
        if (byIdentifier is not null)
        {
            return (byIdentifier, "Identifier");
        }

        var byTitle = albums.FirstOrDefault(a => a.NormalizedTitle == release.NormalizedTitle);
        return byTitle is not null ? (byTitle, "Title") : (null, null);
    }
}
