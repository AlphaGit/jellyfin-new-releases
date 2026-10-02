# Quickstart: validating `006-upgrade-replaces-old-version`

The suite proves one thing: the two statements of the name agree. Everything else is proved on a
real server, because this defect needs two copies in one host and hid for nine days behind a restart
coin flip.

---

## Prerequisites

- .NET SDK 10. A shell started before 2026-09-20 may resolve `dotnet@9` and fail with `NETSDK1045`;
  prefix `PATH=/opt/homebrew/opt/dotnet/bin:$PATH DOTNET_ROOT=/opt/homebrew/opt/dotnet/libexec`.
- A Jellyfin 12 server for pass 2.

## Pass 1 — the suite

```bash
dotnet build --configuration Release    # zero warnings
dotnet test  --configuration Release
node --test "tests/web/*.test.js"       # unchanged by this feature, must stay green
```

### Scenario 1 — the names must agree (`SC-004`)

```bash
cp build.yaml /tmp/build.yaml.bak
# change name: "New Releases" to anything else
dotnet test --configuration Release --filter "FullyQualifiedName~BuildManifestTests" \
  -- RunConfiguration.TreatNoTestsAsError=true      # MUST fail
cp /tmp/build.yaml.bak build.yaml
cmp -s /tmp/build.yaml.bak build.yaml && echo restored
```

Restore from the copy, never `git checkout --`: it reverts the whole file to `HEAD` and takes
uncommitted work with it. This has cost work twice on this project.

The `--` argument is mandatory — verified: exit 1 with it, exit 0 without.

## Pass 2 — a real Jellyfin 12 server

`SC-001`, `SC-002`, `SC-003` and `SC-005` are only met here.

**Start from the broken state on purpose**, because that is the state every existing install is in.

1. Confirm the server carries a copy under the old name:
   `ls /var/lib/jellyfin/plugins/` shows `Jellyfin New Releases_<version>`.
2. Publish the renaming release and update from the catalogue, then restart.
3. Confirm both directories now exist — `Jellyfin New Releases_<old>` and `New Releases_<new>` —
   and that the plugin works. This is the expected intermediate state, not a failure.
4. **`FR-007`**: follow the release notes' single step and remove the old-name directory. Restart.
5. **`SC-005`**: `ls /var/lib/jellyfin/plugins/` shows exactly one copy of this plugin.
6. **`SC-002`**: restart five times. After each, the log shows one `Loaded plugin:` line for this
   plugin and no `AmbiguousMatchException`. Confirm one refresh task is registered, not two.
7. **`SC-003` and `SC-001`**: publish a further release, update, restart. The old directory is gone
   with **no** manual step — this is the host's own cleanup working now that the names agree, and it
   is the proof the feature actually did its job. Repeat once more to be sure.
8. **`FR-009`**: reinstall an earlier published version from the catalogue and confirm it comes back.
9. Open the New Releases view and the administrator page; both still work under the new name.

Step 7 is the one that matters. Steps 4–6 only show the transition was survivable; step 7 shows the
defect cannot recur.

Record the pass in `docs/`, as `003`'s and `0.1.0`'s were. Anything it finds becomes its own
specification.

## References

- Why each decision was taken, and the host behaviour measured: [`research.md`](./research.md)
