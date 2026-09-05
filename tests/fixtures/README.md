# Test Fixtures

Recorded HTTP response bodies used by contract and unit tests. Tests make no live network calls.

**No API keys, session tokens, or PII may appear in any committed fixture.** Scrub before commit:

```bash
grep -rn "apikey\|api_key\|access_token\|Authorization" tests/fixtures/
```

Layout: one directory per external source (`musicbrainz/`, …), one file per recorded scenario
(`happy_path.json`, `empty_results.json`, `error_5xx.json`). Refresh a fixture when a contract
test fails or the fixture is older than 6 months. Do not suppress a failing contract test.
