# Seed: an upgrade can leave two releases running, and the page dies on some restarts

Found by the real-server pass of `0.1.1` on Jellyfin 12.1 (2026-10-01), the pass
`005-page-json-casing` placed inside itself. This is the seed for a specification, not the
specification.

## What was observed

The New Releases menu entry rendered a black page. The browser console showed
`GET /Plugins/NewReleases/UserView` returning **500**. Every other plugin route answered normally.

Read from the server log, not inferred:

```
Microsoft.AspNetCore.Routing.Matching.AmbiguousMatchException: The request matched multiple endpoints.
```

Both `0.1.0.0` and `0.1.1.0` were loaded into the same host. `/Plugins/NewReleases/UserView` is the
only route both versions spell identically — `005` renamed every other one — so it alone was
ambiguous.

## It is intermittent

The server restarts daily around 12:03 UTC. One week of the log:

| Date | Loaded |
| --- | --- |
| Sep 27 | `0.1.1.0` only |
| Sep 28 | `0.1.1.0` **and** `0.1.0.0` |
| Sep 29 | `Skipping disabled plugin 0.1.0.0`, then `0.1.1.0` only |
| Sep 30 | `0.1.1.0` **and** `0.1.0.0` |

The same URL answered `401` (healthy) on Sep 21 and `500` on Sep 30, same server, no operator
action in between.

## Root cause, measured before the old directory was deleted

```text
/var/lib/jellyfin/plugins/Jellyfin New Releases_0.1.0.0/meta.json
  name "New Releases"            status "Superseded"
/var/lib/jellyfin/plugins/Jellyfin New Releases_0.1.1.0/meta.json
  name "Jellyfin New Releases"   status "Active"
```

`build.yaml` declares `name: "Jellyfin New Releases"`. `src/Jellyfin.Plugin.NewReleases/Plugin.cs`
declares `Name => "New Releases"`. Jellyfin rewrites `meta.json` from `Plugin.Name` when it
supersedes a copy, so the superseded copy is filed under a different name than the active one. The
GUID is identical in both, so grouping is evidently not by GUID alone.

## The remedy applied, which is not an acceptable upgrade path

```bash
sudo rm -rf "/var/lib/jellyfin/plugins/Jellyfin New Releases_0.1.0.0"
sudo systemctl restart jellyfin
```

After it: `UserView` 401, old `api/…` routes 404, `Loaded plugin: New Releases 0.1.1.0` alone.
Constitution IV forbids a release that requires the operator to delete plugin data.

## Candidates for the grilling phase

- **(a) Align the two names** and guard the agreement with a test. Addresses the root cause.
- **(b) Never reuse an address between releases** — moves `UserView` off its shared path. Defence in
  depth: makes a stale copy harmless rather than preventing it. Safe because `003` established the
  page registration is rewritten at every server start.
- **(c) Delete superseded sibling directories from inside the plugin at startup.** The most direct
  reading of "remove the previous version on every update", but it has the plugin deleting files the
  host owns and defeats the host's rollback.
- **(d) Prune old versions from the published manifest.** Stops new installs of an old release;
  does nothing about one already on disk. Also removes rollback.

(a) and (b) are not alternatives to each other — (a) prevents the condition, (b) removes its
consequence — and the specification asks for both.

## What the publishing workflow cannot do

The ask that prompted this was "fix the deployment pipeline so the previous version is removed on
every update". The pipeline publishes a package; Jellyfin owns the directory it is installed into,
on a machine the workflow has never heard of. The outcome is reachable only through what the package
declares.

## Why no test caught it

The condition needs two copies of the plugin assembly in one running host. The suite has no way to
arrange that. What it can reach: the two names agreeing, and no two published releases sharing a
route.
