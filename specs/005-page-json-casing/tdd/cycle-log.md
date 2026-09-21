# Cycle Log: Make the pages read what the server actually sends

Append only. Newest last. Every entry's `red` block is the evidence that the test existed and
failed before the implementation.

## Baseline

- suite: `dotnet test --configuration Release` -> 257 passed, 0 failed, 0 skipped
- suite: `node --test "tests/web/*.test.js"` -> 33 passed, 0 failed
- commit: `af02934`
- recorded: cycle 0, before any change

### Measured before cycle 1, and the reason the naming behaviours are red

`Jellyfin.Extensions` 12.0.0, the version this plugin builds against:

```text
JsonDefaults.Options           -> {"Items":[1],"Total":1,"HasStoredReleases":true, ...}
JsonDefaults.CamelCaseOptions  -> {"items":[1],"total":1,"hasStoredReleases":true, ...}
JsonDefaults.PascalCaseOptions -> {"Items":[1],"Total":1,"HasStoredReleases":true, ...}
JsonDefaults.Options.PropertyNamingPolicy -> null (exact property names)
```

The host default an endpoint receives when it declares nothing writes `Items`, not `items`. That
is the production defect, reproducible in-process, and it is why `U1`, `U2`, `U3` and `U9` must
resolve the serializer options from the endpoint's own declaration rather than reaching for
`CamelCaseOptions` directly.
