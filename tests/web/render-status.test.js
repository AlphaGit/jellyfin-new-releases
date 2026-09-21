'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const { loadPageDom } = require('./load-page.js');
const { fixture } = require('./fixtures.js');

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

    assert.match(text('nr-last-checked'), /^Releases last checked .+\.$/);
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

test('every artist no source matched is listed with its reasons and the hint', () => {
    const { document } = rendered(fixture('admin-status.json'));
    const rows = document.getElementById('nr-unmatched').querySelector('tbody').innerHTML;

    assert.equal(rows.match(/<tr>/g).length, 2);
    assert.match(rows, /Various Artists/);
    assert.match(rows, /musicbrainz: NoConfidentMatch<br>deezer: NoResults/);
    assert.match(rows, /metadata editor or artist\.nfo/);
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
