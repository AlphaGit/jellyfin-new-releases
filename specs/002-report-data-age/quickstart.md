# Quickstart: Report the age of the data, not the age of the run

**Feature**: `002-report-data-age` | **Date**: 2026-09-08

How to prove this feature works. Steps 1–2 are automated and run on any machine. Steps 3–6 need a
Jellyfin server and cover what the suite cannot reach: the page copy.

## Prerequisites

- .NET 9 SDK. On this Mac the default `dotnet` is SDK 8, so prefix commands with
  `PATH=/opt/homebrew/opt/dotnet@9/bin:$PATH DOTNET_ROOT=/opt/homebrew/opt/dotnet@9/libexec`.
- For steps 3–6: Jellyfin 10.11.11 with the Plugin Pages plugin, a music library with at least
  two artists, and the built plugin installed.

## 1. Build and run the suite

```bash
dotnet build --configuration Release   # must report zero warnings
dotnet test --configuration Release
```

Expected: green, with the suite larger than `001`'s 178 by the behaviours this feature adds.

## 2. Prove the datapoint, not the run

The load-bearing test. Run it alone:

```bash
dotnet test --configuration Release \
  --filter "FullyQualifiedName~A20_" \
  -- RunConfiguration.TreatNoTestsAsError=true
```

Expected: green, and asserting that after a refresh which reached no source the stated instant is
still the *first* run's completed fetch. Before this feature the same test asserted the opposite.

Then confirm the test has teeth — this is the check that would have caught the original defect:

```bash
# Temporarily point the read back at the last completed run instead of the last completed fetch.
# The A20 test MUST fail. Restore the change and re-run the full suite.
```

## 3. See the wording change

1. Open **New Releases** from the Jellyfin side menu.
2. Stop both sources reaching the network (disconnect the server, or set both to a state where
   they fail) and wait past one refresh interval, or set the interval to 1 hour to shorten it.
3. Expected above the list: `Releases last checked <n> hours ago.`
4. Expected absent: any sentence containing "refresh", "run", "scan" or "update" (`SC-004`).

## 4. See that a no-op run does not reset it

1. Note the age from step 3.
2. In the plugin's admin page, press **Run now**. The run completes; no source is reachable.
3. Reload **New Releases**.
4. Expected: the age is unchanged or larger — never smaller (`SC-001`). Before this feature it
   reset to "just now".
5. Expected on the admin page: the last run shows a fresh end time while the data age still shows
   the old instant. The two visibly diverge (`SC-005`).

## 5. See the empty state after a purge

1. With releases listed, press **Purge release data** on the admin page.
2. Reload **New Releases**.
3. Expected: `No data yet. New Releases is waiting for its first refresh.` and no age line, even
   though refresh runs are still on record (`FR-008`).

## 6. See a source being disabled age the data

1. With both sources enabled and a recent fetch, note the age.
2. Untick the source that has been doing the fetching, save, reload.
3. Expected: the age jumps to the older remaining value, or the line disappears if no enabled
   source has ever completed a fetch (`FR-002`).

## What this guide does not cover

- The unit ladder above two days. Weeks, months and `over a year ago` are covered by tests rather
  than by hand, because reproducing them live means waiting or editing the database.
- Keyboard operation and screen-reader announcement of the line, which `001` already leaves to a
  manual accessibility pass.

## Results

Record the outcome of steps 3–6 here when the manual pass is run, one line each, with the
Jellyfin version and date.
