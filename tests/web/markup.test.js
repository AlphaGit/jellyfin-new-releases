'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { loadPageDom, settled, rendered } = require('./load-page.js');
const { declaredIds, FakeElement } = require('./fake-dom.js');
const { fixture } = require('./fixtures.js');

// 007 U68, the closed world over the markup (T075): every element the view declares or writes carries
// exactly the reviewed attribute names and classes. An inline style, an added or renamed class, an extra
// attribute, or a duplicate in capital letters fails here until the lists below are reviewed. Attribute
// values are left to the behaviour tests; this file pins the shape.

const PAGE = fs.readFileSync(path.join(__dirname, '..', '..', 'src', 'Jellyfin.Plugin.NewReleases', 'Web', 'user-view.html'), 'utf8');

/** An attribute value: double-quoted, single-quoted, or unquoted. */
const VALUE = String.raw`"[^"]*"|'[^']*'|[^\s"'=<>\x60]+`;

/**
 * Every opening tag in `html` as `name#id.classes[attribute names]`: names in lower case and sorted, duplicates
 * kept, and `id` and the classes taken from the first of two duplicates. Comments are skipped. A tag it cannot
 * read fails it, so nothing goes unread.
 */
function signatures(html) {
    const markup = html.replace(/<!--[\s\S]*?-->/g, '');
    const tags = [...markup.matchAll(new RegExp(String.raw`<([a-z][\w-]*)((?:\s+[^\s"'=<>\/]+(?:\s*=\s*(?:${VALUE}))?)*)\s*\/?>`, 'gi'))];
    if (tags.length !== (markup.match(/<[a-z]/gi) || []).length) throw new Error(`signatures: a tag could not be read in ${JSON.stringify(html)}`);
    return tags.map(([, name, rest]) => {
        const attributes = [...rest.matchAll(new RegExp(String.raw`([^\s"'=<>\/]+)(?:\s*=\s*(${VALUE}))?`, 'g'))]
            .map(([, key, value = '']) => [key.toLowerCase(), value.replace(/^(["'])(.*)\1$/, '$2')]);
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
    ['<span class=nr-badge style=opacity:.3>', ['span.nr-badge[class style]']],
    ["<a href='x' title='y z'>", ['a[href title]']],
    ['<!-- <p class="x"> --><p>', ['p[]']],
    ['<template><p></p></template>', ['template[]', 'p[]']],
    ['<br/><img src="a" />', ['br[]', 'img[src]']],
    ['<div class="a" class="b">', ['div.a[class class]']],
]) {
    test(`U68 helper: ${JSON.stringify(html)} has the shapes ${JSON.stringify(expected)}`, () => {
        assert.deepEqual(signatures(html), expected);
    });
}

test('U68 helper: a tag that signatures cannot read fails it, rather than going unread', () => {
    assert.throws(() => signatures('<p><div class="a>'), /could not be read/);
});

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

test('U68: the static markup holds exactly the reviewed elements', () => {
    assert.deepEqual(signatures(PAGE.replace(/<(style|script)>[\s\S]*?<\/\1>/g, '')), STATIC);
});

/** What the view writes into the panel for `body`, in the List tab or the Archive tab. */
const panelFor = (body, archive = false) => rendered(body, { archive }).panel;

/** What the view writes into `id` once it has loaded with `ajax` answering its requests. */
async function loadedInto(id, ajax) {
    const { document } = loadPageDom('user-view.html', { ApiClient: { ajax } });
    await settled();
    return document.getElementById(id).innerHTML;
}

const RELEASES = fixture('releases.json');

/** An `ajax` that answers the Artists request with `artists.json` and every other with `releases.json`. */
const artistsAndReleases = options => Promise.resolve(fixture(options.url.includes('Artists') ? 'artists.json' : 'releases.json'));
const only = (id, changes = {}) => ({ ...RELEASES, items: [{ ...RELEASES.items.find(item => item.id === id), ...changes }] });

// The reviewed shapes of each template the view writes, in order. A template is one branch of `render`,
// `load` or `loadArtists`, or one kind of row. Change a list only together with a review of the markup
// change it records.
const LIST = ['div.nr-list[class]', 'h2[]'];
const COVER = ['div.nr-cover[class]', 'img[alt data-fallback height loading referrerpolicy src width]'];
const DETAILS = ['div[]', 'div.nr-title[class]', 'div.nr-artist[class]', 'a[href]', 'div.nr-meta[class]', 'span.nr-badge[class]', 'span[]', 'span.nr-badge.nr-badge-state[class role]'];
const LINK = 'a[href rel target]';
const BUTTON = 'button[aria-label data-action type]';
const EMPTY = ['p.nr-empty[class]'];

const TEMPLATES = [
    // Release 101 has two sources (two links); the List tab offers two buttons, Ignore and Have it.
    ['a Missing release at both sources, in the List tab', () => panelFor(only(101)),
        [...LIST, 'article.nr-row[class data-id data-title]', ...COVER, ...DETAILS, 'div.nr-links[class]', LINK, LINK, 'div.nr-actions[class]', BUTTON, BUTTON]],
    // Release 102 has two missing tracks (two list items), a compared edition (the paragraph) and one source.
    ['an Incomplete release with a compared edition, in the List tab', () => panelFor(only(102)),
        [...LIST, 'article.nr-row[class data-id data-title]', ...COVER, ...DETAILS, 'details[]', 'summary[]', 'ul[]', 'li[]', 'li[]', 'p[]',
            'div.nr-links[class]', LINK, 'div.nr-actions[class]', BUTTON, BUTTON]],
    // Release 104 is archived (the extra badge) and has one source; the Archive tab offers one button, Restore.
    ['an archived release, in the Archive tab', () => panelFor(only(104), true),
        [...LIST, 'article.nr-row[class data-id data-title]', ...COVER, ...DETAILS, 'span.nr-badge[class]', 'div.nr-links[class]', LINK, 'div.nr-actions[class]', BUTTON]],
    ['a release with no cover', () => panelFor(only(101, { covers: [] })),
        [...LIST, 'article.nr-row[class data-id data-title]', 'div.nr-cover[class]', ...DETAILS, 'div.nr-links[class]', LINK, LINK, 'div.nr-actions[class]', BUTTON, BUTTON]],
    ['no stored releases', () => panelFor(fixture('releases-empty.json')), EMPTY],
    ['an empty selection', () => panelFor({ ...RELEASES, items: [] }), EMPTY],
    ['an empty Archive', () => panelFor(fixture('releases-empty.json'), true), EMPTY],
    ['a list that fails to load', () => loadedInto('nr-panel', () => Promise.reject(new Error('offline'))), EMPTY],
    // artists.json holds two artists, so two suggestions.
    ['the suggestion list', () => loadedInto('nr-f-artist-list', artistsAndReleases),
        ['option[value]', 'option[value]']],
];

for (const [name, markup, expected] of TEMPLATES) {
    test(`U68: ${name} writes exactly the reviewed shapes, in order`, async () => {
        assert.deepEqual(signatures(await markup()), expected);
    });
}

/** What the view has written on `element` beyond the content the stand-in models: attribute names, `hidden`, and any property the stand-in does not have. */
function writesOn(element) {
    return [...Object.keys(element.attributes), ...(element.hidden ? ['hidden'] : []), ...Object.keys(element).filter(key => !MODELLED.has(key))];
}

/** The properties a fresh stand-in element has; a write to any other one is a property the stand-in does not model. */
const MODELLED = new Set(Object.keys(new FakeElement('x')));

for (const [label, write, expected] of [
    ['nothing', () => {}, []],
    ['an attribute', element => element.setAttribute('aria-selected', 'true'), ['aria-selected']],
    ['a style attribute', element => element.setAttribute('style', 'opacity:.3'), ['style']],
    ['hidden', element => { element.hidden = true; }, ['hidden']],
    ['a class name', element => { element.className = 'nr-status'; }, ['className']],
    ['content and value', element => { element.innerHTML = '<p>'; element.textContent = 'x'; element.value = 'x'; }, []],
]) {
    test(`U68 helper: writing ${label} on an element reads back as ${JSON.stringify(expected)}`, () => {
        const element = new FakeElement('x');
        write(element);
        assert.deepEqual(writesOn(element), expected);
    });
}

/** The reviewed runtime writes: the tabs' selection and the panel's label. `#nr-staleness` toggles `hidden`. */
const RUNTIME = {
    'nr-staleness': ['hidden'],
    'nr-tab-list': ['aria-selected'],
    'nr-tab-archive': ['aria-selected'],
    'nr-panel': ['aria-labelledby'],
};

test('U68: through load, both tabs, a filter change, Clear and a render, the view writes on its elements only what is reviewed', async () => {
    const { internals, document } = loadPageDom('user-view.html', {
        ApiClient: { ajax: artistsAndReleases },
    });
    const unreviewed = () => [...declaredIds('user-view.html')]
        .flatMap(id => writesOn(document.getElementById(id)).filter(write => !(RUNTIME[id] || []).includes(write)).map(write => id + ' ' + write));
    const steps = {
        load: () => {},
        'the Archive tab': () => document.getElementById('nr-tab-archive').listeners.click[0](),
        'the List tab': () => document.getElementById('nr-tab-list').listeners.click[0](),
        'a filter change': () => document.getElementById('nr-f-type').listeners.change[0](),
        Clear: () => document.getElementById('nr-f-clear').listeners.click[0](),
        'a render': () => internals.render(RELEASES),
    };
    const found = {};
    for (const [step, act] of Object.entries(steps)) {
        act();
        await settled();
        found[step] = unreviewed();
    }

    assert.deepEqual(found, Object.fromEntries(Object.keys(steps).map(step => [step, []])));
});

