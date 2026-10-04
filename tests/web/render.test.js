'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const { loadPageDom, renderedList, settled } = require('./load-page.js');
const { fixture } = require('./fixtures.js');
const { HOUR, DAY, ago } = require('./fixed-clock.js');
const { rowOf, coverBox, imgAttribute, decoded } = require('./cover-markup.js');

// `render` is the one function that consumes a server response, and until now the one function with
// no test. These capture what it already does, against a response of the shape the server really
// produces. They are characterization tests: they pass against untouched code, and their value is
// that they fail when the page and the server stop agreeing.
//
// They assert on the string the page wrote. Nothing here parses markup; see
// specs/005-page-json-casing/contracts/page-sandbox.md for what that does and does not prove.

const WAITING = 'No data yet. New Releases is waiting for its first refresh.';

test('with stored releases the panel holds one row per item', () => {
    const body = fixture('releases.json');

    const { panel } = renderedList(body);

    assert.equal(panel.match(/class="nr-row"/g).length, body.items.length);
});

test('with stored releases the panel does not hold the waiting message', () => {
    assert.doesNotMatch(renderedList(fixture('releases.json')).panel, new RegExp(WAITING));
});

test('with no stored releases the panel holds the waiting message', () => {
    assert.match(renderedList(fixture('releases-empty.json')).panel, new RegExp(WAITING));
});

test('with no stored releases the panel holds no row', () => {
    assert.doesNotMatch(renderedList(fixture('releases-empty.json')).panel, /class="nr-row"/);
});

test('with stored releases but nothing in this selection the panel says so', () => {
    const body = { ...fixture('releases.json'), items: [] };

    assert.match(renderedList(body).panel, /Nothing missing for this selection\./);
});

test('a rendered row carries its artist, title, type, date and state', () => {
    const { panel } = renderedList(fixture('releases.json'));

    assert.match(panel, /Closer to Grey/);
    assert.match(panel, /Chromatics/);
    assert.match(panel, /<span class="nr-badge">Album<\/span>/);
    assert.match(panel, /<span>2019-10-02<\/span>/);
    assert.match(panel, /nr-badge-state" role="status">Missing</);
});

test('an Incomplete row carries its missing tracks and the edition they were compared with', () => {
    const { panel } = renderedList(fixture('releases.json'));

    assert.match(panel, /2 missing tracks/);
    assert.match(panel, /<li>Into the Black<\/li>/);
    assert.match(panel, /compared with Kill for Love \(deluxe edition\) from musicbrainz/);
});

test('a rendered row carries one link per source', () => {
    const body = fixture('releases.json');

    const { panel } = renderedList(body);

    const links = body.items.reduce((total, item) => total + item.sources.length, 0);
    assert.equal(panel.match(/ target="_blank"/g).length, links);
});

test('an undated row is grouped and printed as Undated', () => {
    const { panel } = renderedList(fixture('releases.json'));

    assert.match(panel, /<h2>Undated<\/h2>/);
    assert.match(panel, /<span>Undated<\/span>/);
});

test('on the Archive tab a row carries its archived badge and the kind of the decision', () => {
    const { panel } = renderedList(fixture('releases.json'), { archive: true });

    assert.match(panel, /<span class="nr-badge">Have it, In library<\/span>/);
});

test('with no instant on record the staleness line is empty and the list still renders', () => {
    const body = { ...fixture('releases.json'), releasesLastCheckedAt: null };

    const { panel, staleness } = renderedList(body);

    assert.equal(staleness.textContent, '');
    assert.equal(staleness.hidden, true);
    assert.match(panel, /class="nr-row"/);
});

test('with an instant older than the refresh interval the staleness sentence appears', () => {
    const { staleness } = renderedList(fixture('releases-stale.json'));

    assert.equal(staleness.textContent, 'Releases last checked 7 weeks ago.');
    assert.equal(staleness.hidden, false);
});

// 002 FR-006: the page hands the rule the interval the response carries, not a fixed day. Every
// fixture serves 24, so these two set it on either side.
test('a weekly interval keeps a two-day-old instant quiet', () => {
    const { staleness } = renderedList({ ...fixture('releases.json'), releasesLastCheckedAt: ago(2 * DAY), refreshIntervalHours: 168 });

    assert.equal(staleness.textContent, '');
    assert.equal(staleness.hidden, true);
});

test('a six-hour interval states a twelve-hour-old instant', () => {
    const { staleness } = renderedList({ ...fixture('releases.json'), releasesLastCheckedAt: ago(12 * HOUR), refreshIntervalHours: 6 });

    assert.equal(staleness.textContent, 'Releases last checked 12 hours ago.');
    assert.equal(staleness.hidden, false);
});

// The click handler reads a row's `data-action` and joins it into the path it posts. This pins the
// half in the markup: the values are the action segments the plugin registers. The join itself is
// pinned by U72 in requests.test.js, one test per action, since the stand-in models `closest` (007 cycle 72).
test('a rendered row offers the actions under the names the decision routes are served under', () => {
    const { panel } = renderedList(fixture('releases.json'));
    const archived = renderedList(fixture('releases.json'), { archive: true }).panel;

    assert.deepEqual([...new Set([...panel.matchAll(/data-action="(\w+)"/g)].map(m => m[1]))], ['Ignore', 'HaveIt']);
    assert.deepEqual([...new Set([...archived.matchAll(/data-action="(\w+)"/g)].map(m => m[1]))], ['Restore']);
});

test('a narrowed response lists only what it carries', () => {
    const body = fixture('releases-filtered.json');

    const { panel } = renderedList(body);

    assert.equal(panel.match(/class="nr-row"/g).length, 1);
    assert.equal(body.items.length, 1);
    assert.doesNotMatch(panel, /Kill for Love/);
});

// 002 A9 / SC-008: a title arrives from MusicBrainz or Deezer and is concatenated into the row's
// markup. `esc.test.js` pins the helper; these pin that the row actually uses it.
const MARKUP_TITLE = '<img src=x onerror=alert(1)>';

function renderedWithTitle(title) {
    const body = fixture('releases.json');
    return renderedList({ ...body, items: [{ ...body.items[0], title }] }).panel;
}

test('a title containing markup is written into the row as text', () => {
    assert.match(renderedWithTitle(MARKUP_TITLE), /<div class="nr-title">&lt;img src=x onerror=alert\(1\)&gt;<\/div>/);
});

test('a title containing markup appears nowhere in the row unescaped', () => {
    assert.equal(renderedWithTitle(MARKUP_TITLE).indexOf(MARKUP_TITLE), -1);
});

// The title is also written into three attributes (`data-title` and both buttons' `aria-label`),
// where a quote, not markup, is what breaks out. Four places, each escaped in full.
const QUOTED_TITLE = `"'><img src=x onerror=alert(1)>`;

test('a title containing quotes is escaped in every place the row writes it', () => {
    assert.equal((renderedWithTitle(QUOTED_TITLE).match(/&quot;&#39;&gt;&lt;img src=x onerror=alert\(1\)&gt;/g) ?? []).length, 4);
});

// 007 US2: the cover box each row opens with. The markup is read as a string; what a browser does
// with a failing image is in cover-fallback.test.js.

const BOTH_SOURCES = 101; // releases.json: Closer to Grey, at MusicBrainz and Deezer

test('U48: a row\'s cover image src is its first cover URL', () => {
    const body = fixture('releases.json');

    const box = coverBox(renderedList(body).panel, BOTH_SOURCES);

    assert.equal(imgAttribute(box, 'src'), body.items.find(i => i.id === BOTH_SOURCES).covers[0]);
});

test('U49: a row\'s cover image lists the remaining cover URLs, in order, as its fallbacks', () => {
    const body = fixture('releases.json');

    const box = coverBox(renderedList(body).panel, BOTH_SOURCES);

    assert.equal(imgAttribute(box, 'data-fallback'), body.items.find(i => i.id === BOTH_SOURCES).covers.slice(1).join(' '));
});

test('U50: a row\'s cover image loads lazily', () => {
    assert.equal(imgAttribute(coverBox(renderedList(fixture('releases.json')).panel, BOTH_SOURCES), 'loading'), 'lazy');
});

test('U51: a row\'s cover image sends no referrer', () => {
    assert.equal(imgAttribute(coverBox(renderedList(fixture('releases.json')).panel, BOTH_SOURCES), 'referrerpolicy'), 'no-referrer');
});

test('U52: a row\'s cover image declares a 64 by 64 box', () => {
    const box = coverBox(renderedList(fixture('releases.json')).panel, BOTH_SOURCES);

    assert.deepEqual([imgAttribute(box, 'width'), imgAttribute(box, 'height')], ['64', '64']);
});

test('U53: a row with no cover URL writes the cover box with no image', () => {
    const body = fixture('releases.json');
    body.items.find(i => i.id === BOTH_SOURCES).covers = [];

    assert.equal(coverBox(renderedList(body).panel, BOTH_SOURCES), '');
});

test('U54: cover URLs are written escaped, in the src and in the fallbacks', () => {
    const body = fixture('releases.json');
    body.items.find(i => i.id === BOTH_SOURCES).covers = ['https://x.test/a"><script>', 'https://x.test/b?c=1&d=2'];

    const box = coverBox(renderedList(body).panel, BOTH_SOURCES);

    assert.deepEqual([imgAttribute(box, 'src'), imgAttribute(box, 'data-fallback')], ['https://x.test/a&quot;&gt;&lt;script&gt;', 'https://x.test/b?c=1&amp;d=2']);
});

test('A10: a rendered cover image has empty alt text, so a screen reader skips it', () => {
    assert.equal(imgAttribute(coverBox(renderedList(fixture('releases.json')).panel, BOTH_SOURCES), 'alt'), '');
});

test('A12: an Archive-tab row writes Restore inside .nr-actions, under the same rule as Ignore and Have it', () => {
    const { panel } = renderedList(fixture('releases.json'), { archive: true });

    assert.match(panel, /<div class="nr-actions"><button type="button" data-action="Restore"/);
});

// U70, characterization (maintainer decision T083): the source links predate 007. This pins what they
// already do, so a change to where they point fails here.
test('U70: each source link of a row points at its source URL, in order', () => {
    const body = fixture('releases.json');
    const { panel } = renderedList(body);
    const hrefs = item => [...rowOf(panel, item.id).matchAll(/<a href="([^"]*)" target="_blank"/g)].map(([, href]) => decoded(href));

    assert.deepEqual(body.items.map(hrefs), body.items.map(item => item.sources.map(source => source.url)));
});

// 007 T091 group A, characterization (BASELINE).
test('U77: a release with three sources shows three links, in order', () => {
    const body = fixture('releases.json');
    const three = { ...body.items[0], sources: [...body.items[0].sources, { source: 'musicbrainz', url: 'https://musicbrainz.org/release-group/00000000-0000-0000-0000-000000000199' }] };
    const { panel } = renderedList({ ...body, items: [three] });

    assert.deepEqual([...rowOf(panel, three.id).matchAll(/<a href="([^"]*)" target="_blank"/g)].map(([, href]) => decoded(href)), three.sources.map(source => source.url));
});

// 007 T091 group B, characterization (BASELINE): the page shows the right words. Only the sentences no
// earlier test reads are pinned here.

test('U78: a list that fails to load says it could not load', async () => {
    const { document } = loadPageDom('user-view.html', { ApiClient: { ajax: () => Promise.reject(new Error('offline')) } });
    await settled();

    assert.equal(document.getElementById('nr-panel').innerHTML, '<p class="nr-empty">Could not load New Releases.</p>');
});

for (const [label, body] of [
    ['with stored releases', { ...fixture('releases.json'), items: [] }],
    ['before the first refresh', fixture('releases-empty.json')],
]) {
    test(`U79: an empty Archive says the Archive is empty, ${label}`, () => {
        assert.equal(renderedList(body, { archive: true }).panel, '<p class="nr-empty">The Archive is empty.</p>');
    });
}

/** Each button of the row with `id`, as `[screen-reader label, visible text]`. */
const buttonsOf = (panel, id) => [...rowOf(panel, id).matchAll(/<button [^>]*aria-label="([^"]*)">([^<]*)<\/button>/g)].map(([, label, text]) => [label, text]);

test('U80: the buttons read Ignore, Have it and Restore, and their screen-reader labels name the release', () => {
    const body = fixture('releases.json');

    assert.deepEqual([buttonsOf(renderedList(body).panel, 101), buttonsOf(renderedList(body, { archive: true }).panel, 104)], [
        [['Ignore Closer to Grey', 'Ignore'], ['Mark Closer to Grey as Have it', 'Have it']],
        [['Restore II', 'Restore']],
    ]);
});

test('U81: an Upcoming release\'s state badge reads "Upcoming, not yet released"', () => {
    const badge = /nr-badge-state" role="status">([^<]*)</.exec(rowOf(renderedList(fixture('releases.json')).panel, 103));

    assert.equal(badge?.[1], 'Upcoming, not yet released');
});

test('U81: in the Archive tab, an ignored release\'s badge reads "Ignored"', () => {
    const body = fixture('releases.json');
    const ignored = { ...body, items: [{ ...body.items[3], archived: { kind: 'Ignore', decidedAt: '2026-09-18T11:04:00+00:00' } }] };

    assert.equal(/<span class="nr-badge">(Ignored|Have it, In library)<\/span>/.exec(renderedList(ignored, { archive: true }).panel)?.[1], 'Ignored');
});


test('U82: one missing track is counted in the singular', () => {
    const body = fixture('releases.json');
    const one = { ...body, items: [{ ...body.items[1], missingTracks: ['Into the Black'] }] };

    assert.match(renderedList(one).panel, /<summary>1 missing track<\/summary>/);
});

test('U83: the source links read MusicBrainz and Deezer', () => {
    assert.deepEqual([...rowOf(renderedList(fixture('releases.json')).panel, 101).matchAll(/target="_blank"[^>]*>([^<]*)<\/a>/g)].map(([, text]) => text), ['MusicBrainz', 'Deezer']);
});

// 007 T091 group C, characterization (BASELINE).
test('U88: every source link opens in a new tab without access to the page', () => {
    const body = fixture('releases.json');
    const { panel } = renderedList(body);
    const links = [...panel.matchAll(/<a href="[^"]*"( [^>]*)>/g)].map(([, rest]) => rest).filter(rest => rest.includes('target'));

    assert.deepEqual(links, body.items.flatMap(item => item.sources.map(() => ' target="_blank" rel="noopener"')));
});
