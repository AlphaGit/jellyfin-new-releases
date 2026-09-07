# ADR-0002: v1 sources are MusicBrainz and Deezer; Bandcamp deferred

**Status**: Accepted — 2026-09-06

## Context

The constitution allows official or open APIs by default and HTML scraping only behind a
per-source terms-of-service opt-in. A first release needs enough coverage to be useful without
credentials the operator must obtain.

## Decision

Ship two credential-free sources: MusicBrainz (identifier-based artist and release matching,
using the MusicBrainz IDs Jellyfin already stores on artists and albums) and Deezer (name-based
matching; explicit release types, dates, track lists, documented request limit). Listings of the
same Release from both are merged; MusicBrainz identity and type are canonical when present.
Bandcamp is wanted but excluded until Bandcamp answers the permission request already sent; it
becomes its own feature then.

## Rationale

MusicBrainz alone under-represents streaming-era and independent releases. Among open commercial
catalogues, iTunes Search was rejected because it reports every collection as "Album", forcing
type inference from title suffixes, and depends on a storefront country. Deezer's record type
maps directly onto the albums-and-EPs default.

## Consequences

- Two adapters, two rate-limit budgets, two fixture sets from the start.
- A cross-source merge rule (artist plus normalized title) is part of the core model.
