namespace Jellyfin.Plugin.NewReleases.Model;

/// <summary>Release types the plugin knows. <see cref="Other"/> marks a source type outside the list and always excludes.</summary>
public enum ReleaseType
{
    Album,
    EP,
    Single,
    Compilation,
    Live,
    Remix,
    Soundtrack,
    Other,
}

/// <summary>Result of the automatic ownership check. <c>Owned</c> is shown to users as "In library" (never listed).</summary>
public enum OwnershipState
{
    Missing,
    Incomplete,
    Owned,
}

/// <summary>A user's decision on a release; both send it to the Archive.</summary>
public enum DecisionKind
{
    Ignore,
    HaveIt,
}

/// <summary>Outcome of one (artist, source) catalogue fetch. Only <c>Complete</c> may prune source entries (FR-014).</summary>
public enum FetchOutcome
{
    Complete,
    Partial,
    Failed,
}

/// <summary>Match state of a library artist at one source (FR-002).</summary>
public enum MatchStatus
{
    Pending,
    Matched,
    Unmatched,
}

/// <summary>State a listed release shows to users (FR-007, FR-008). <c>Owned</c> releases are never listed.</summary>
public enum ListState
{
    Missing,
    Incomplete,
    Upcoming,
}
