# Cycle Log: Run on Jellyfin 12

Append only. Newest last. Every entry's `red` block is the evidence that the test existed and
failed before the implementation.

## Baseline

- suite: `dotnet test --configuration Release` -> 195 passed, 0 failed, 10 s
- page-side suite: `node --test "tests/web/*.test.js"` -> 33 passed, 0 failed, 133 ms
- commit: `a9f1ba4`
- recorded: cycle 0, before any change
- target at baseline: `net9.0`, Jellyfin 10.11.11. **This is the old target.** Every cycle that
  closes a behaviour on `test-list.md` must record its run as `net10.0` against Jellyfin 12.0.0;
  a green recorded against this baseline's target proves nothing for this feature.
- note: run with SDK 9 (`PATH=/opt/homebrew/opt/dotnet@9/bin:$PATH DOTNET_ROOT=/opt/homebrew/opt/dotnet@9/libexec`);
  the default `dotnet` in non-login shells is SDK 8 and fails with NETSDK1045
