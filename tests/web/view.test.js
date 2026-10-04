'use strict';

const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');
const assert = require('node:assert/strict');
const { loadPageDom, settled } = require('./load-page.js');

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
    const state = () => ['nr-tab-list', 'nr-tab-archive'].map(id => document.getElementById(id).getAttribute('aria-selected'))
        .concat(document.getElementById('nr-panel').getAttribute('aria-labelledby'));
    const opened = [staticAttribute('nr-tab-list', 'aria-selected'), staticAttribute('nr-tab-archive', 'aria-selected'), staticAttribute('nr-panel', 'aria-labelledby')];
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
