# ADR-0001: Ownership is decided per Release by track list, with user override

**Status**: Accepted — 2026-09-06

## Context

The plugin must decide whether the library "already has" a release a source reports. Jellyfin
groups files into albums, but an album folder may hold a subset of the tracks (a stray single,
an unfinished download) or a different edition than the one a source describes.

## Decision

The unit of ownership is the whole Release. A Release is In library only when the library album
matched to it contains every track of the best-overlapping Edition's track list, compared by
normalized title. Anything less is Incomplete and stays listed with its missing tracks and the
Edition and Source compared against. A user may mark a Release Have it; that decision supersedes
the automatic result in both directions and is stored separately from release data.

## Rationale

Album-level presence (the simplest rule) would hide half-downloaded albums, which is precisely
the gap the owner wants to see. Track-count-only comparison would flag every deluxe or regional
variant as missing. Full title comparison against the best-matching Edition catches real gaps
while tolerating edition variance, and the manual override settles the cases no heuristic can.

## Consequences

- Track lists are fetched only for Releases that have a library album candidate, so the extra
  requests scale with the library, not the sources' catalogues.
- A third visible state, Incomplete, and a user decision store (the Archive) exist because of
  this rule.
