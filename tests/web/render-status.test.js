'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const { loadPageDom, pageSource } = require('./load-page.js');
const { fixture } = require('./fixtures.js');
const { unmatchedRows, reasonMarkup, fixes, visible } = require('./unmatched-table.js');
const { HOUR, DAY, ago } = require('./fixed-clock.js');

// The administrator page showed a dash for every value on Jellyfin 12. These capture what
// `renderStatus` already does against a response of the shape the server really produces, so the
// same silence cannot return. Characterization tests: green against untouched code.

const DASH = '–';

/** Renders one status response and returns the text each element now holds, by id. */
function rendered(status) {
    const { internals, document } = loadPageDom('admin.html');

    internals.renderStatus(status);

    const text = id => document.getElementById(id).textContent;
    return { text, document };
}

test('after a completed refresh the last refresh shows its instant and outcome, not a dash', () => {
    const { text } = rendered(fixture('admin-status.json'));

    assert.equal(text('nr-last-refresh'), '9/19/2026, 3:15:00 AM (Completed, 2 errors)');
});

test('after a completed refresh the releases-last-checked value is a sentence, not a dash', () => {
    const { text } = rendered(fixture('admin-status.json'));

    assert.equal(text('nr-last-checked'), 'Releases last checked 8 hours ago.');
});

// 002 SC-005: the last refresh and the data age are separate values. The fixture gives them the
// same instant, so this response separates them: one cannot be shown for the other.
test('the releases-last-checked value states the data age, not the last refresh', () => {
    const base = fixture('admin-status.json');

    const { text } = rendered({ ...base, lastRun: { ...base.lastRun, endedAt: ago(HOUR) }, releasesLastCheckedAt: ago(3 * DAY) });

    assert.equal(text('nr-last-checked'), 'Releases last checked 3 days ago.');
});

test('the next refresh shows its instant when no refresh is running', () => {
    const { text } = rendered(fixture('admin-status.json'));

    assert.equal(text('nr-next-refresh'), '9/21/2026, 3:00:00 AM');
});

test('the artists processed and releases found show their counts, not a dash', () => {
    const { text } = rendered(fixture('admin-status.json'));

    assert.equal(text('nr-artists-processed'), '79 of 83');
    assert.equal(text('nr-releases-found'), '812');
});

test('each source shows its health, calls today and daily budget', () => {
    const { text } = rendered(fixture('admin-status.json'));

    assert.equal(text('nr-health-musicbrainz'), 'Ok · 412 / 1000 requests today');
});

test('a cooling-down source shows the instant it resumes and its last error', () => {
    const { text } = rendered(fixture('admin-status.json'));

    assert.equal(
        text('nr-health-deezer'),
        'CoolingDown until 9/20/2026, 6:00:00 PM — Quota exceeded · 200 / 200 requests today');
});

test('a healthy source hides its last error', () => {
    const { text } = rendered(fixture('admin-status.json'));

    assert.doesNotMatch(text('nr-health-musicbrainz'), /503/);
});

/** The source each Reason line of a row starts with. */
function reasonLineSources(row) {
    return [...row.matchAll(/<div class="nr-reason">([^:<]+): /g)].map(m => m[1]);
}

// 008 FR-001: the table's columns are Artist, Reason, How to fix.
test('the unmatched table is headed Artist, Reason, How to fix', () => {
    const head = pageSource('admin.html').match(/<table[^>]*id="nr-unmatched"[\s\S]*?<thead>([\s\S]*?)<\/thead>/)[1];

    assert.deepEqual([...head.matchAll(/<th>([^<]*)<\/th>/g)].map(m => m[1]), ['Artist', 'Reason', 'How to fix']);
});

// 008 FR-001, FR-002: one Reason line per enabled source, led by the name the Sources section shows.
test('each unmatched artist has one Reason line per reasons entry, led by the source display name', () => {
    const rows = unmatchedRows(fixture('admin-status.json'));

    assert.deepEqual(rows.map(reasonLineSources), [['MusicBrainz', 'Deezer'], ['MusicBrainz', 'Deezer'], ['MusicBrainz', 'Deezer']]);
});

/** A row's Reason lines as text, markup removed. */
function reasonLines(row) {
    return [...row.matchAll(/<div class="nr-reason">([\s\S]*?)<\/div>/g)].map(m => m[1].replace(/<[^>]*>/g, ''));
}

// 008 US2-AS3 (A9, U54): a matched source reads "<Source>: Matched", with no trail.
test('a matched source reads "<Source>: Matched" and nothing else', () => {
    const rows = unmatchedRows(fixture('admin-status.json'));

    assert.equal(reasonLines(rows[0])[0], 'MusicBrainz: Matched');
});

// 008 edge case: an enabled source that has not run for the artist reads "Not checked".
test('a source not yet checked reads "<Source>: Not checked"', () => {
    const rows = unmatchedRows(fixture('admin-status.json'));

    assert.equal(reasonLines(rows[2])[1], 'Deezer: Not checked');
});

/** The fixture with one more artist whose only reason is a text no released version writes. */
function withUnrecognizedReason(text) {
    const base = fixture('admin-status.json');
    const odd = { jellyfinId: '0b8f0e7c2d4a4f6e9a1c3e5f7a9b1d3f', name: 'Odd', reasons: [{ source: 'musicbrainz', state: 'Unmatched', steps: [], reason: text }] };
    return { ...base, unmatched: [...base.unmatched, odd] };
}

// 008 edge case: a reason with no known failed step shows its stored text, escaped.
test('an entry with no failed step shows its stored reason text, escaped', () => {
    const rows = unmatchedRows(withUnrecognizedReason('rate <limited> & retried'));

    assert.match(rows[3], /<div class="nr-reason">MusicBrainz: rate &lt;limited&gt; &amp; retried<\/div>/);
});

// 008 SC-001: the fixed hint that sent every failure to the MusicBrainz artist ID is gone.
test('the old fixed hint sentence appears nowhere on the page', () => {
    const rows = unmatchedRows(fixture('admin-status.json'));

    assert.equal(rows.filter(row => fixes(row).length > 0).length, 3); // every row has its fixes to read
    assert.doesNotMatch(rows.join(''), /picks it up on the next refresh/);
    assert.doesNotMatch(pageSource('admin.html'), /picks it up on the next refresh/);
});

/** The fixture with one reasons entry replaced: `row` and `source` index into unmatched[] and reasons[]. */
function withReason(row, source, reason) {
    const base = fixture('admin-status.json');
    const unmatched = base.unmatched.map((u, i) => i !== row ? u : { ...u, reasons: u.reasons.map((r, j) => j !== source ? r : reason) });
    return { ...base, unmatched };
}

const ARTIST_ID_FIX = "Set the MusicBrainz artist ID in Jellyfin's metadata editor or artist.nfo; the plugin uses it on the next refresh.";

// 008 FR-008: a MusicBrainz search that found nobody needs the artist ID, and has no candidates to offer.
test('a MusicBrainz Search failure gives the artist-ID sentence and no candidates', () => {
    const search = { source: 'musicbrainz', state: 'Unmatched', failedStep: 'Search', steps: [], reason: 'no result', candidates: [] };

    const row = unmatchedRows(withReason(2, 0, search))[2];

    assert.deepEqual(fixes(row), [ARTIST_ID_FIX]);
});

// 008 FR-008, FR-016: an ambiguous MusicBrainz failure lists its candidates, each with its score and a copyable ID.
test('a MusicBrainz Unique failure with candidates gives the sentence, then each name (score) with its ID in a <code>', () => {
    const row = unmatchedRows(fixture('admin-status.json'))[1];
    const markup = row.match(/<p class="nr-fix">([\s\S]*?)<\/p>/)[1];

    assert.equal(
        markup,
        ARTIST_ID_FIX + ' Candidates: Nirvana (100) <code class="nr-id">5b11f4ce-a62d-471e-81fc-a69a8278c7da</code>, ' +
            'Nirvana (96) <code class="nr-id">9282c8b4-ca0b-4c6b-b7e3-4f7762dfc4d6</code>, ' +
            'Approaching Nirvana (74) <code class="nr-id">c3a64a25-251b-4d03-afba-1471440245b8</code>');
});

// 008 FR-016, edge case: a row from before the upgrade has no candidates, so the fix is the sentence alone.
test('a MusicBrainz Score failure without stored candidates gives the sentence only', () => {
    const row = unmatchedRows(fixture('admin-status.json'))[2];

    assert.deepEqual(fixes(row), [ARTIST_ID_FIX]);
});

// 008 FR-008: Deezer matches by exact name only, so its near names are what the administrator compares.
test('a Deezer Name failure with names lists them comma-separated and says Deezer matches by name only', () => {
    const row = unmatchedRows(fixture('admin-status.json'))[1];

    assert.equal(
        fixes(row)[1],
        'Deezer lists Nirvana (UK), Approaching Nirvana. If one is this artist, its name on Deezer differs from the name in Jellyfin; Deezer matches by name only.');
});

// 008 FR-008: a Deezer search that found nobody at all may mean Deezer does not list the artist.
test('a Deezer Name failure with an empty candidate list says Deezer may not list this artist', () => {
    const nobody = { source: 'deezer', state: 'Unmatched', failedStep: 'Name', steps: [], reason: 'no result', candidates: [] };

    const row = unmatchedRows(withReason(1, 1, nobody))[1];

    assert.equal(fixes(row)[1], 'Deezer may not list this artist.');
});

// 008 FR-008, edge case: a Deezer Name failure from before the upgrade has no candidates recorded.
test('a Deezer Name failure with no stored candidates says Deezer may not list this artist', () => {
    const old = { source: 'deezer', state: 'Unmatched', failedStep: 'Name', steps: [], reason: 'no result' };

    const row = unmatchedRows(withReason(1, 1, old))[1];

    assert.equal(fixes(row)[1], 'Deezer may not list this artist.');
});

const ALBUM_RULE = 'None has the same title as an album in the library, ignoring case, punctuation, accents and one trailing edition tag such as (Deluxe Edition). Only the first 100 albums of each artist are compared.';

// 008 FR-008: an album check that found nothing in common says what it compared.
test('a Deezer Album failure with detail gives both counts', () => {
    const row = unmatchedRows(fixture('admin-status.json'))[0];

    assert.deepEqual(fixes(row), ['Checked 2 Deezer artists with this name and 37 of their albums. ' + ALBUM_RULE]);
});

// 008 FR-008, edge case: an album failure from before the upgrade has no counts, so the fix starts at the rule.
test('a Deezer Album failure without detail starts at "No Deezer album has the same title"', () => {
    const old = { source: 'deezer', state: 'Unmatched', failedStep: 'Album', steps: [], reason: 'no corroborating album' };

    const row = unmatchedRows(withReason(0, 1, old))[0];

    assert.deepEqual(fixes(row), [ALBUM_RULE.replace('None has', 'No Deezer album has')]);
});

// 008 SC-001, FR-006: a row whose only failure is at Deezer never points at the MusicBrainz artist ID.
test('a row whose only failure is at Deezer never mentions the MusicBrainz artist ID', () => {
    const row = unmatchedRows(fixture('admin-status.json'))[0];

    assert.match(fixes(row)[0], /^Checked 2 Deezer artists/); // the Deezer fix is there
    assert.doesNotMatch(row, /MusicBrainz artist ID/);
});

// Every value the page writes passes through esc: candidate names and IDs come from a third-party search.
test('candidate names and IDs are written escaped', () => {
    const musicBrainz = { source: 'musicbrainz', state: 'Unmatched', failedStep: 'Unique', steps: [], reason: 'ambiguous (score 90 vs 88)', candidates: [{ name: '<b>Blur</b>', score: 90, id: 'id<1>' }] };
    const deezer = { source: 'deezer', state: 'Unmatched', failedStep: 'Name', steps: [], reason: 'no result', candidates: [{ name: 'Tom & <i>Jerry</i>' }] };

    const status = withReason(1, 1, deezer);
    status.unmatched[1].reasons[0] = musicBrainz;

    const row = unmatchedRows(status)[1];

    assert.doesNotMatch(row, /<b>|<i>|id<1>/);
    assert.match(row, /&lt;b&gt;Blur&lt;\/b&gt; \(90\) <code class="nr-id">id&lt;1&gt;<\/code>/);
    assert.match(row, /Deezer lists Tom &amp; &lt;i&gt;Jerry&lt;\/i&gt;\./);
});

// 008 FR-006: only a failed step gets a fix; a match or a source not yet checked gets none.
test('a matched or not-checked source gives no fix paragraph', () => {
    // Stale text and candidates on a source that matched or never ran would earn a fix if the page keyed on them.
    const matched = { source: 'musicbrainz', state: 'Matched', steps: [], reason: 'no result', candidates: [{ name: 'Daft Punk', score: 70, id: 'mbid-old' }] };
    const notChecked = { source: 'deezer', state: 'NotChecked', steps: [], reason: 'no result', candidates: [] };

    assert.deepEqual(fixes(unmatchedRows(withReason(0, 0, matched))[0]), ['Checked 2 Deezer artists with this name and 37 of their albums. ' + ALBUM_RULE]);
    assert.deepEqual(fixes(unmatchedRows(withReason(2, 1, notChecked))[2]), [ARTIST_ID_FIX]);
});

// 008 edge case, FR-006: a reason with no known failed step gets no fix, since no step is known to fix.
test('an entry with no failed step gives no fix paragraph', () => {
    const row = unmatchedRows(withUnrecognizedReason('rate limited'))[3];

    assert.deepEqual(fixes(row), []);
});

/** Each step of a trail line as [short name, state class]. */
function trailSteps(line) {
    return [...line.matchAll(/<span class="nr-step nr-step-(\w+)">([^ <]+)/g)].map(m => [m[2], m[1]]);
}

// 008 FR-003: a trail shows every step of the source in its order, by short name, joined by " · ".
test('a trail shows every step in order by short name, ID for Id, joined by " · "', () => {
    const [musicBrainz] = reasonMarkup(unmatchedRows(fixture('admin-status.json'))[1]);

    assert.deepEqual(trailSteps(musicBrainz).map(([name]) => name), ['ID', 'Search', 'Score', 'Unique']);
    assert.equal(musicBrainz.split('</span> · <span class="nr-step').length, 4);
});

/** Each step of a trail line as [mark, words for a screen reader]. */
function marks(line) {
    return [...line.matchAll(/<span aria-hidden="true">([^<]*)<\/span><span class="nr-sr">([^<]*)<\/span>/g)].map(m => [m[1], m[2]]);
}

// 008 FR-003, FR-010: a passed step shows ✓, and says "passed" in words.
test('a passed step shows ✓ and the words "passed"', () => {
    const [, deezer] = reasonMarkup(unmatchedRows(fixture('admin-status.json'))[0]); // Name passed, Album failed

    assert.deepEqual(marks(deezer)[0], ['✓', 'passed']);
});

// 008 FR-003, FR-010: a failed step shows ✗, and says "failed" in words.
test('a failed step shows ✗ and the words "failed"', () => {
    const [, deezer] = reasonMarkup(unmatchedRows(fixture('admin-status.json'))[0]); // Name passed, Album failed

    assert.deepEqual(marks(deezer)[1], ['✗', 'failed']);
});

// 008 FR-003, FR-010: a step after the failed one shows –, says "not reached", and carries the dimming class.
test('a not-reached step shows –, the words "not reached", and the dimming class', () => {
    const [musicBrainz] = reasonMarkup(unmatchedRows(fixture('admin-status.json'))[2]); // ID, Search, Score failed, Unique not reached

    assert.deepEqual(marks(musicBrainz)[3], ['–', 'not reached']);
    assert.deepEqual(trailSteps(musicBrainz)[3], ['Unique', 'notreached']);
});

// 008 FR-010: a screen reader skips the mark and reads the words; the state is never in the mark or colour alone.
test('every step hides its mark from screen readers and states itself in visually hidden words', () => {
    const [musicBrainz] = reasonMarkup(unmatchedRows(fixture('admin-status.json'))[1]);
    const pairs = musicBrainz.split(' · ').map(step => (/<span aria-hidden="true">([✓✗–])<\/span><span class="nr-sr">([^<]*)<\/span>/.exec(step) || []).slice(1));

    assert.deepEqual(pairs, [['✗', 'failed'], ['✓', 'passed'], ['✓', 'passed'], ['✗', 'failed']]);
});

// 008 FR-004: a failed Score step shows the best score after its mark.
test('a failed Score step shows its score after the mark', () => {
    const [musicBrainz] = reasonMarkup(unmatchedRows(fixture('admin-status.json'))[2]);

    assert.match(visible(musicBrainz), / · Score ✗ 70 · /);
});

// 008 FR-004: a failed Unique step shows the two closest scores.
test('a failed Unique step shows both scores: "Unique ✗ 100 vs 96"', () => {
    const [musicBrainz] = reasonMarkup(unmatchedRows(fixture('admin-status.json'))[1]);

    assert.match(visible(musicBrainz), / · Unique ✗ 100 vs 96$/);
});

// 008 FR-005, SC-004 (research R7): the longest trail any source can write fits the one-line budget of 64 characters.
const ONE_LINE_BUDGET = 64;

test('the longest possible trail is at most 64 characters as a reader sees it', () => {
    const longest = {
        source: 'musicbrainz', state: 'Unmatched', failedStep: 'Unique', reason: 'ambiguous (score 100 vs 95)',
        steps: [{ name: 'Id', state: 'Failed' }, { name: 'Search', state: 'Passed' }, { name: 'Score', state: 'Passed' }, { name: 'Unique', state: 'Failed', values: [100, 95] }],
    };

    const [line] = reasonMarkup(unmatchedRows(withReason(1, 0, longest))[1]);

    assert.equal(visible(line), 'MusicBrainz: ID ✗ · Search ✓ · Score ✓ · Unique ✗ 100 vs 95');
    assert.ok(visible(line).length <= ONE_LINE_BUDGET, `${visible(line).length} characters`);
});

// The Artist cell links the artist's Jellyfin page, with the name written escaped.
test("each unmatched row's Artist cell links the artist's page with the name escaped", () => {
    const base = fixture('admin-status.json');
    const status = { ...base, unmatched: [{ ...base.unmatched[0], name: 'Tom & <Jerry>' }] };

    const [row] = unmatchedRows(status);

    assert.match(row, /^<tr><td><a is="emby-linkbutton" class="button-link" href="#\/details\?id=5f2a9c1e7b3d4e8fa6c0d9b14e72a3c8&serverId=[^"]+">Tom &amp; &lt;Jerry&gt;<\/a><\/td>/);
});

test('with no unmatched artists the table is hidden and the empty line is shown', () => {
    const { document } = rendered(fixture('admin-status-quiet.json'));

    assert.equal(document.getElementById('nr-unmatched').hidden, true);
    assert.equal(document.getElementById('nr-unmatched-empty').hidden, false);
});

test('with no instant on record the releases-last-checked value is a dash', () => {
    const { text } = rendered(fixture('admin-status-quiet.json'));

    assert.equal(text('nr-last-checked'), DASH);
});

test('while a refresh is running the next refresh says so', () => {
    const { text } = rendered(fixture('admin-status-quiet.json'));

    assert.equal(text('nr-next-refresh'), 'Running now');
});

const PLACEHOLDER_FIX = 'Placeholder artist, such as Various Artists. If this is a real artist, correct its MusicBrainz ID in Jellyfin\'s metadata editor or artist.nfo.';

/** The fixture with one more artist, a placeholder at both sources (009 contracts/http-api.md). */
function withPlaceholder() {
    const base = fixture('admin-status.json');
    const entry = source => ({ source, state: 'Unmatched', failedStep: 'Placeholder', steps: [{ name: 'Placeholder', state: 'Failed' }], reason: 'placeholder artist' });
    const various = { jellyfinId: '7d1e5b3a9c2f4e6a8b0d1c3e5f7a9b2c', name: 'Various Artists', reasons: [entry('musicbrainz'), entry('deezer')] };
    return { ...base, unmatched: [...base.unmatched, various] };
}

// 009 US1-AS3, FR-003a: a placeholder's trail is the one Placeholder step at every source.
test('a placeholder reads "<Source>: Placeholder ✗" at every source', () => {
    const row = unmatchedRows(withPlaceholder())[3];

    assert.deepEqual(reasonMarkup(row).map(visible), ['MusicBrainz: Placeholder ✗', 'Deezer: Placeholder ✗']);
});

// 009 FR-003a, 008 FR-007: the placeholder fix shows once, though both sources fail at that step.
test('a placeholder has the placeholder fix once, then Not tracked', () => {
    const row = unmatchedRows(withPlaceholder())[3];

    assert.deepEqual(fixes(row), [PLACEHOLDER_FIX]);
    assert.match(row, /<p class="nr-tracking">Not tracked\.<\/p>/);
});

// 009 FR-003a: the other rows keep 008's trails and fixes.
test('a placeholder row leaves the other rows as they were', () => {
    const before = unmatchedRows(fixture('admin-status.json'));

    assert.deepEqual(unmatchedRows(withPlaceholder()).slice(0, 3), before);
});

// 009 US4: a run closed at start reads "(Interrupted)".
test('a run closed as interrupted shows its outcome in brackets', () => {
    const base = fixture('admin-status.json');
    const { text } = rendered({ ...base, lastRun: { ...base.lastRun, outcome: 'Interrupted', errors: 0 } });

    assert.equal(text('nr-last-refresh'), '9/19/2026, 3:15:00 AM (Interrupted)');
});
