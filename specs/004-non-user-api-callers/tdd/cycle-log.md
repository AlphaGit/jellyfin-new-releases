# Cycle Log: Answer a caller that has no user

Append only. Newest last. Every entry's `red` block is the evidence that the test existed and
failed before the implementation.

## Baseline

- suite: `dotnet test --configuration Release` -> 352 passed, 0 failed
- web suite: `node --test "tests/web/*.test.js"` -> 363 passed, 0 failed
- commit: `463373e`
- recorded: cycle 0, before any change
