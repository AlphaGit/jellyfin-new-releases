'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const { loadPageDom } = require('./load-page.js');
const { fixture } = require('./fixtures.js');

// `render` is the one function that consumes a server response, and until now the one function with
// no test. These capture what it already does, against a response of the shape the server really
// produces. They are characterization tests: they pass against untouched code, and their value is
// that they fail when the page and the server stop agreeing.
//
// They assert on the string the page wrote. Nothing here parses markup; see
// specs/005-page-json-casing/contracts/page-sandbox.md for what that does and does not prove.

/** Loads the view and renders one response through it, returning what the panel now holds. */
function rendered(body, { archive = false } = {}) {
    const { internals, document } = loadPageDom('user-view.html', {
        ApiClient: { ajax: () => Promise.resolve(body) },
    });

    if (archive) {
        document.getElementById('nr-tab-archive').listeners.click[0]();
    }

    internals.render(body);

    return {
        panel: document.getElementById('nr-panel').innerHTML,
        staleness: document.getElementById('nr-staleness'),
    };
}

const WAITING = 'No data yet. New Releases is waiting for its first refresh.';

test('with stored releases the panel holds one row per item', () => {
    const body = fixture('releases.json');

    const { panel } = rendered(body);

    assert.equal(panel.match(/class="nr-row"/g).length, body.items.length);
});

test('with stored releases the panel does not hold the waiting message', () => {
    assert.doesNotMatch(rendered(fixture('releases.json')).panel, new RegExp(WAITING));
});

test('with no stored releases the panel holds the waiting message', () => {
    assert.match(rendered(fixture('releases-empty.json')).panel, new RegExp(WAITING));
});

test('with no stored releases the panel holds no row', () => {
    assert.doesNotMatch(rendered(fixture('releases-empty.json')).panel, /class="nr-row"/);
});

test('with stored releases but nothing in this selection the panel says so', () => {
    const body = { ...fixture('releases.json'), items: [] };

    assert.match(rendered(body).panel, /Nothing missing for this selection\./);
});

test('a rendered row carries its artist, title, type, date and state', () => {
    const { panel } = rendered(fixture('releases.json'));

    assert.match(panel, /Closer to Grey/);
    assert.match(panel, /Chromatics/);
    assert.match(panel, /<span class="nr-badge">Album<\/span>/);
    assert.match(panel, /<span>2019-10-02<\/span>/);
    assert.match(panel, /nr-badge-state" role="status">Missing</);
});

test('an Incomplete row carries its missing tracks and the edition they were compared with', () => {
    const { panel } = rendered(fixture('releases.json'));

    assert.match(panel, /2 missing tracks/);
    assert.match(panel, /<li>Into the Black<\/li>/);
    assert.match(panel, /compared with Kill for Love \(deluxe edition\) from musicbrainz/);
});

test('a rendered row carries one link per source', () => {
    const body = fixture('releases.json');

    const { panel } = rendered(body);

    const links = body.items.reduce((total, item) => total + item.sources.length, 0);
    assert.equal(panel.match(/ target="_blank"/g).length, links);
});

test('an undated row is grouped and printed as Undated', () => {
    const { panel } = rendered(fixture('releases.json'));

    assert.match(panel, /<h2>Undated<\/h2>/);
    assert.match(panel, /<span>Undated<\/span>/);
});

test('on the Archive tab a row carries its archived badge and the kind of the decision', () => {
    const { panel } = rendered(fixture('releases.json'), { archive: true });

    assert.match(panel, /<span class="nr-badge">Have it, In library<\/span>/);
});

test('with no instant on record the staleness line is empty and the list still renders', () => {
    const body = { ...fixture('releases.json'), releasesLastCheckedAt: null };

    const { panel, staleness } = rendered(body);

    assert.equal(staleness.textContent, '');
    assert.equal(staleness.hidden, true);
    assert.match(panel, /class="nr-row"/);
});

test('with an instant older than the refresh interval the staleness sentence appears', () => {
    const { staleness } = rendered(fixture('releases-stale.json'));

    assert.match(staleness.textContent, /^Releases last checked .+ ago\.$/);
    assert.equal(staleness.hidden, false);
});

// The click handler reads a row's `data-action` and joins it into the path it posts. The join
// itself needs `closest`, which the stand-in does not model, so what is pinned here is the half a
// test can see: the values in the markup are the action segments the plugin registers. The join is
// driven only by the real-server pass.
test('a rendered row offers the actions under the names the decision routes are served under', () => {
    const { panel } = rendered(fixture('releases.json'));
    const archived = rendered(fixture('releases.json'), { archive: true }).panel;

    assert.deepEqual([...new Set([...panel.matchAll(/data-action="(\w+)"/g)].map(m => m[1]))], ['Ignore', 'HaveIt']);
    assert.deepEqual([...new Set([...archived.matchAll(/data-action="(\w+)"/g)].map(m => m[1]))], ['Restore']);
});

test('a narrowed response lists only what it carries', () => {
    const body = fixture('releases-filtered.json');

    const { panel } = rendered(body);

    assert.equal(panel.match(/class="nr-row"/g).length, 1);
    assert.equal(body.items.length, 1);
    assert.doesNotMatch(panel, /Kill for Love/);
});
