# Real-server upgrade to 0.2.0

The `quickstart.md` pass 2 for `specs/006-upgrade-replaces-old-version/`, on 2026-10-04. It closes
task T026 and the acceptance behaviours A1–A4.

## Server

- Host: the maintainer's Jellyfin server, Jellyfin `12.1+ubu2404`.
- Before: one installed copy, `Jellyfin New Releases_0.1.1.0`. Its `meta.json` already gave the
  name `New Releases`, because the host had written the running plugin's name into it. The
  repository entry was `Jellyfin New Releases` at the old GitHub Pages address.

## What was done

1. `0.2.0` was released from tag `v0.2.0`. The GitHub Release has `new-releases.zip`, and the
   catalogue at
   `https://raw.githubusercontent.com/AlphaGit/jellyfin-new-releases/main/repo/manifest.json` lists
   `0.2.0.0` with that asset and its checksum (`c3b9a393f53a0640936d2c1dedfc1655`).
2. `/etc/jellyfin/system.xml` was backed up. The repository entry was changed to the name
   `New Releases` and the raw catalogue address, which is the step the `0.2.0` notes give.
3. With Jellyfin stopped, the release asset was checked against the catalogue checksum and
   extracted to `/var/lib/jellyfin/plugins/New Releases_0.2.0.0`. Jellyfin was then started.

## What was observed

- **The old copy removed itself.** After start-up, the plugin folder held only
  `New Releases_0.2.0.0`, in state `Active`. Both copies were filed under one name, so Jellyfin
  kept the newer one and deleted the older folder. This is the cleanup the fix depends on. The
  manual removal the `0.2.0` notes describe was not needed on this server.
- The start-up log has one `Loaded plugin: New Releases 0.2.0.0` line and one daily trigger for
  "Refresh new releases". It has no `AmbiguousMatchException` and no error.
- **The maintainer verified the rest by hand and accepted it:**
  - the New Releases view and the administrator page under the new name;
  - repeated restarts with one copy loaded each time;
  - the upgrade path through the catalogue.
- GitHub Pages was turned off afterwards. The old catalogue address returns 404.

## Result

| Criterion | Result |
| --- | --- |
| US1-AS1, SC-001: one release running after the upgrade | Met |
| US1-AS2, SC-002: the same single release on every restart | Met, by the maintainer's restarts |
| US1-AS3: the view renders | Met, by the maintainer's check |
| US1-AS4, SC-003: no manual step beyond the restart | Met. The only manual step was replacing the repository address, which the renaming release documents once (`FR-007`) |
| SC-005: one copy after the documented removal | Met without the removal: the host removed the old copy itself |
