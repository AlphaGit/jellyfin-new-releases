'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { loadPageDom } = require('./load-page.js');
const { declaredIds } = require('./fake-dom.js');
const { fixture } = require('./fixtures.js');
const { imgAttribute } = require('./cover-markup.js');

// 007 U68, the closed world over the markup (T075): every element the view declares or writes carries
// exactly the reviewed attribute names and classes. An inline style, an added or renamed class, an extra
// attribute, or a duplicate in capital letters fails here until the lists below are reviewed. Attribute
// values are left to the behaviour tests; this file pins the shape.

const PAGE = fs.readFileSync(path.join(__dirname, '..', '..', 'src', 'Jellyfin.Plugin.NewReleases', 'Web', 'user-view.html'), 'utf8');
const settled = () => new Promise(resolve => setImmediate(resolve));

/** Every opening tag in `html` as `name#id.classes[attribute names]`: names in lower case and sorted, duplicates kept. */
function signatures(html) {
    return [...html.matchAll(/<([a-z][\w-]*)((?:\s+[^\s"'=<>\/]+(?:\s*=\s*"[^"]*")?)*)\s*\/?>/gi)].map(([, name, rest]) => {
        const attributes = [...rest.matchAll(/([^\s"'=<>\/]+)(?:\s*=\s*"([^"]*)")?/g)].map(([, key, value]) => [key.toLowerCase(), value]);
        const value = key => (attributes.find(([found]) => found === key) || [])[1];
        const classes = (value('class') || '').split(/\s+/).filter(Boolean).map(name => '.' + name).join('');
        return name.toLowerCase() + (value('id') ? '#' + value('id') : '') + classes + '[' + attributes.map(([key]) => key).sort().join(' ') + ']';
    });
}

for (const [html, expected] of [
    ['<div class="a b" id="x">', ['div#x.a.b[class id]']],
    ['<IMG SRC="u" alt="">', ['img[alt src]']],
    ['<img LOADING="eager" loading="lazy">', ['img[loading loading]']],
    ['<p>text</p><span hidden>', ['p[]', 'span[hidden]']],
    ['<a href="x>y" style="color: red">', ['a[href style]']],
    ['</div>', []],
]) {
    test(`U68 helper: ${JSON.stringify(html)} has the shapes ${JSON.stringify(expected)}`, () => {
        assert.deepEqual(signatures(html), expected);
    });
}

for (const [markup, name, expected] of [
    ['<img LOADING="eager" loading="lazy">', 'loading', 'eager'],
    ['<img data-src="b" src="a">', 'src', 'a'],
    ['<img src="a" alt="">', 'alt', ''],
    ['<img src="a">', 'alt', undefined],
]) {
    test(`U68 helper: imgAttribute reads ${name} from ${JSON.stringify(markup)} as a browser does`, () => {
        assert.equal(imgAttribute(markup, name), expected);
    });
}

// The reviewed shapes. Change a list only together with a review of the markup change it records.

/** The page's static markup, in source order, outside its `<style>` and `<script>`. */
const STATIC = [
    'div#nr-user-view[id]',
    'h1[]',
    'p#nr-staleness.nr-status[class hidden id]',
    'div[aria-label role]',
    'button#nr-tab-list[aria-controls aria-selected id role type]',
    'button#nr-tab-archive[aria-controls aria-selected id role type]',
    'form#nr-filters.nr-filters[aria-label class id role]',
    'div.nr-filter[class]',
    'label[for]',
    'input#nr-f-artist[autocomplete id list placeholder type]',
    'datalist#nr-f-artist-list[id]',
    'div.nr-filter[class]',
    'label[for]',
    'select#nr-f-type[id]',
    'option[value]',
    ...Array(7).fill('option[]'),
    'div.nr-filter[class]',
    'label[for]',
    'select#nr-f-state[id]',
    'option[value]',
    ...Array(3).fill('option[]'),
    'div.nr-filter[class]',
    'label[for]',
    'input#nr-f-from[id type]',
    'div.nr-filter[class]',
    'label[for]',
    'input#nr-f-to[id type]',
    'div.nr-filter[class]',
    'button#nr-f-clear[id type]',
    'div#nr-panel[aria-labelledby id role]',
    'div#nr-announce.nr-visually-hidden[aria-live class id]',
];

/** Every distinct shape the view writes into the panel and the suggestion list, sorted. */
const WRITTEN = [
    'a[href rel target]',
    'a[href]',
    'article.nr-row[class data-id data-title]',
    'button[aria-label data-action type]',
    'details[]',
    'div.nr-actions[class]',
    'div.nr-artist[class]',
    'div.nr-cover[class]',
    'div.nr-links[class]',
    'div.nr-list[class]',
    'div.nr-meta[class]',
    'div.nr-title[class]',
    'div[]',
    'h2[]',
    'img[alt data-fallback height loading referrerpolicy src width]',
    'li[]',
    'option[value]',
    'p.nr-empty[class]',
    'p[]',
    'span.nr-badge.nr-badge-state[class role]',
    'span.nr-badge[class]',
    'span[]',
    'summary[]',
    'ul[]',
];

test('U68: the static markup holds exactly the reviewed elements', () => {
    assert.deepEqual(signatures(PAGE.replace(/<(style|script)>[\s\S]*?<\/\1>/g, '')), STATIC);
});

/** What the view writes for `body`, in the List tab or the Archive tab. */
function panelFor(body, archive) {
    const { internals, document } = loadPageDom('user-view.html', { ApiClient: { ajax: () => Promise.resolve(body) } });
    if (archive) document.getElementById('nr-tab-archive').listeners.click[0]();
    internals.render(body);
    return document.getElementById('nr-panel').innerHTML;
}

test('U68: every element the view writes has a reviewed shape', async () => {
    const releases = fixture('releases.json');
    const coverless = { ...releases, items: releases.items.map(item => ({ ...item, covers: [] })) };
    const { document } = loadPageDom('user-view.html', {
        ApiClient: { ajax: options => Promise.resolve(fixture(options.url.includes('Artists') ? 'artists.json' : 'releases.json')) },
    });
    await settled();
    const markup = [
        panelFor(releases, false), panelFor(releases, true), panelFor(coverless, false), panelFor(fixture('releases-empty.json'), false),
        document.getElementById('nr-f-artist-list').innerHTML,
    ].join('');

    assert.deepEqual([...new Set(signatures(markup))].sort(), WRITTEN);
});

test('U68: no element of the view carries a style attribute once it has loaded', async () => {
    const { document } = loadPageDom('user-view.html', {
        ApiClient: { ajax: options => Promise.resolve(fixture(options.url.includes('Artists') ? 'artists.json' : 'releases.json')) },
    });
    await settled();

    assert.deepEqual([...declaredIds('user-view.html')].filter(id => document.getElementById(id).getAttribute('style') !== null), []);
});
