# Quickstart: validating `006-upgrade-replaces-old-version`

Two passes. The suite proves the name agrees and the cleanup picks the right directories. Only a
real server proves an upgrade leaves one copy running — and this defect hid for nine days behind a
restart coin flip, so the server pass is the one that matters.

---

## Prerequisites

- .NET SDK 10. A shell started before 2026-09-20 may resolve `dotnet@9` and fail with `NETSDK1045`;
  prefix `PATH=/opt/homebrew/opt/dotnet/bin:$PATH DOTNET_ROOT=/opt/homebrew/opt/dotnet/libexec`.
- Node 22 for the page suite (unchanged by this feature, but it must stay green).
- No network and no Jellyfin server for pass 1.

## Pass 1 — the suite

```bash
dotnet build --configuration Release    # zero warnings
dotnet test  --configuration Release
node --test "tests/web/*.test.js"
```

### Scenario 1 — the names must agree (`SC-004`)

Change `name` in `build.yaml` to anything else, run the suite, change it back.

```bash
cp build.yaml /tmp/build.yaml.bak
# edit name: "New Releases" -> "Something Else"
dotnet test --configuration Release      # MUST fail
cp /tmp/build.yaml.bak build.yaml
cmp -s /tmp/build.yaml.bak build.yaml && echo restored
```

Restore from the copy, never `git checkout --`: it reverts the whole file to `HEAD` and takes
uncommitted work with it. This has cost work twice on this project.

### Scenario 2 — the cleanup removes only what it should

Covered by the directory-selection tests against a temp tree. Confirm all six cases appear in the
run: a stale-name copy removed; an older same-name copy removed; the running directory kept; a newer
copy kept; another GUID kept; an unreadable record kept.

```bash
dotnet test --configuration Release --filter "FullyQualifiedName~StaleCopy" \
  -- RunConfiguration.TreatNoTestsAsError=true
```

The `--` argument is mandatory. Without it a filter matching nothing exits 0 — verified: exit 1 with
it, exit 0 without.

### Scenario 3 — the cleanup is still wired up (`FR-005b`)

Remove the hosted-service registration from `PluginServiceRegistrator`, run the suite, restore from
a file copy. It MUST fail. Correct code nothing calls is the defect shape a green suite hides.

## Pass 2 — a real Jellyfin 12 server

`SC-001`, `SC-002`, `SC-003` and `SC-005` are only met here.

**Start from the broken state on purpose.** The point is to prove the upgrade repairs a server that
already carries two copies — which is the state every existing install is in.

1. Install the current release, then hand-place a second copy so two directories exist under two
   names, as the defect produced. Confirm `ls /var/lib/jellyfin/plugins/` shows both.
2. Build and publish the new release:
   `jprm plugin build . --version X.Y.Z --output ./artifacts`, tag, let CI publish.
3. Update from the catalogue and restart.
4. **`SC-005`**: `ls /var/lib/jellyfin/plugins/` shows exactly one copy of this plugin, the newest.
   The operator deleted nothing.
5. **`SC-003`**: no command was run on the server beyond the restart the host asked for.
6. **`SC-002`**: restart five times. After each, the log shows one `Loaded plugin:` line for this
   plugin and no `AmbiguousMatchException`.
7. **`SC-001`**: repeat from the oldest published release straight to the newest, skipping the ones
   between.
8. Confirm the removals were logged by directory name, and that the plugin still works: open the New
   Releases view, open the administrator page, confirm one refresh task is registered — not two.
9. **Rollback (`FR-009`)**: reinstall an earlier published version from the catalogue and confirm it
   comes back.

Record the pass in `docs/`, as `003`'s and `0.1.0`'s were. Anything it finds becomes its own
specification.

## References

- What the plugin reads and the exact removal rule: [`contracts/installed-copy-record.md`](./contracts/installed-copy-record.md)
- Identity and on-disk states: [`data-model.md`](./data-model.md)
- Why each decision was taken: [`research.md`](./research.md)
