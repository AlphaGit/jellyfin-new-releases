'use strict';

const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');
const assert = require('node:assert/strict');
const { loadPageDom, settled } = require('./load-page.js');
const { declaredIds, keepsListening } = require('./fake-dom.js');

// The New Releases view's own controls: what its static markup declares and how its controls are
// wired. 007 T091 characterizes the parts that predate 007 (BASELINE), so a change to them fails here.
// The page source is read as text only for the static markup, as the profile permits.

const PAGE = fs.readFileSync(path.join(__dirname, '..', '..', 'src', 'Jellyfin.Plugin.NewReleases', 'Web', 'user-view.html'), 'utf8');

test('U75: the Type filter offers all types, then Album, EP, Single, Compilation, Live, Remix and Soundtrack', () => {
    const select = /<select id="nr-f-type">([\s\S]*?)<\/select>/.exec(PAGE)[1];

    assert.deepEqual([...select.matchAll(/<option(?: value="([^"]*)")?>([^<]*)<\/option>/g)].map(([, value, text]) => [value ?? text, text]), [
        ['', 'All types'], ['Album', 'Album'], ['EP', 'EP'], ['Single', 'Single'], ['Compilation', 'Compilation'],
        ['Live', 'Live'], ['Remix', 'Remix'], ['Soundtrack', 'Soundtrack'],
    ]);
});

// 007 T091 group C, characterization (BASELINE): a screen reader and keyboard get it right.

/** The value of `name` on the static element with `id`, or undefined. */
function staticAttribute(id, name) {
    const tag = new RegExp(`<[a-z]+ [^>]*\\bid="${id}"[^>]*>`).exec(PAGE)?.[0] || '';
    return new RegExp(`\\s${name}="([^"]*)"`).exec(tag)?.[1];
}

test('U85: the List tab is selected on open, and switching tabs moves the selection and the panel\'s label', async () => {
    const { document } = loadPageDom('user-view.html', { ApiClient: { ajax: () => Promise.resolve({ items: [], hasStoredReleases: true }) } });
    await settled();
    // What the loaded page holds: what its script wrote, or else what its markup declares.
    const now = (id, name) => document.getElementById(id).getAttribute(name) ?? staticAttribute(id, name);
    const state = () => [now('nr-tab-list', 'aria-selected'), now('nr-tab-archive', 'aria-selected'), now('nr-panel', 'aria-labelledby')];
    const opened = state();
    document.getElementById('nr-tab-archive').listeners.click[0]();
    const archive = state();
    document.getElementById('nr-tab-list').listeners.click[0]();

    assert.deepEqual([opened, archive, state()], [
        ['true', 'false', 'nr-tab-list'],
        ['false', 'true', 'nr-tab-archive'],
        ['true', 'false', 'nr-tab-list'],
    ]);
});

test('U86: the status line is announced politely', () => {
    assert.equal(staticAttribute('nr-announce', 'aria-live'), 'polite');
});

test('U87: each filter label belongs to its own field, and From and To are date fields', () => {
    const pairs = [...PAGE.matchAll(/<label for="([^"]*)">[^<]*<\/label>\s*<(?:input|select) [^>]*?id="([^"]*)"/g)].map(([, label, field]) => [label, field]);

    assert.deepEqual([pairs, staticAttribute('nr-f-from', 'type'), staticAttribute('nr-f-to', 'type')], [
        [['nr-f-artist', 'nr-f-artist'], ['nr-f-type', 'nr-f-type'], ['nr-f-state', 'nr-f-state'], ['nr-f-from', 'nr-f-from'], ['nr-f-to', 'nr-f-to']],
        'date', 'date',
    ]);
});

// U71, characterization (maintainer decision T083): the view's listeners predate 007. A listener that
// stops after its first event (`once`) or can be cut off (`signal`) sends no request the next time.
// The stand-in records options but ignores them, so they are read here.

for (const [options, expected] of [
    [undefined, true],
    [null, true],
    [true, true],
    [false, true],
    [{ capture: true }, true],
    [{ passive: true, once: false }, true],
    [{ once: true }, false],
    [{ capture: true, once: true }, false],
    [{ signal: {} }, false],
]) {
    test(`U71 helper: ${JSON.stringify(options)} ${expected ? 'keeps' : 'stops'} listening`, () => {
        assert.equal(keepsListening(options), expected);
    });
}

test('U71: every listener the New Releases view registers keeps listening', () => {
    const { document } = loadPageDom('user-view.html', { ApiClient: { ajax: () => Promise.resolve({ items: [], hasStoredReleases: false }) } });
    const stopping = [...declaredIds('user-view.html')].flatMap(id => Object.entries(document.getElementById(id).listenerOptions)
        .flatMap(([type, all]) => all.filter(options => !keepsListening(options)).map(() => id + ' ' + type)));

    assert.deepEqual(stopping, []);
});