'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const { loadPageDom } = require('./load-page.js');
const { fixture } = require('./fixtures.js');

// 008 acceptance scenarios for the Unmatched artists table, through the page's own rendering of the
// committed admin status (tests/fixtures/pages/admin-status.json, held to the server records by
// ResponseNamingTests). Rows: 0 Daft Punk (MusicBrainz matched, Deezer Album), 1 Nirvana
// (MusicBrainz Unique, Deezer Name), 2 The Blue Nile (MusicBrainz Score from before the upgrade,
// Deezer not checked).

/** The rendered unmatched rows, each as { reason, fix } cell text with markup removed, plus the fix markup. */
function rows(status) {
    const { internals, document } = loadPageDom('admin.html');
    internals.renderStatus(status);
    const body = document.getElementById('nr-unmatched').querySelector('tbody').innerHTML;
    return body.split('</tr>').filter(r => r.includes('<tr>')).map(r => {
        const cells = [...r.matchAll(/<td>([\s\S]*?)<\/td>/g)].map(m => m[1]);
        return { reason: cells[1].replace(/<[^>]*>/g, ''), fix: cells[2].replace(/<[^>]*>/g, ' ').replace(/\s+/g, ' ').trim(), fixMarkup: cells[2] };
    });
}

test('A1 (US1-AS1): an ambiguous MusicBrainz row tells the administrator to set the artist ID and lists its three candidates', () => {
    const { fix, fixMarkup } = rows(fixture('admin-status.json'))[1];

    assert.match(fix, /Set the MusicBrainz artist ID in Jellyfin's metadata editor or artist\.nfo/);
    assert.match(fixMarkup, /Nirvana \(100\)[\s\S]*<code[^>]*>5b11f4ce-a62d-471e-81fc-a69a8278c7da<\/code>[\s\S]*Nirvana \(96\)[\s\S]*<code[^>]*>9282c8b4-ca0b-4c6b-b7e3-4f7762dfc4d6<\/code>[\s\S]*Approaching Nirvana \(74\)[\s\S]*<code[^>]*>c3a64a25-251b-4d03-afba-1471440245b8<\/code>/);
});

test('A2 (US1-AS2): a Deezer album failure gives both counts and never the MusicBrainz artist ID', () => {
    const { fix } = rows(fixture('admin-status.json'))[0];

    assert.match(fix, /Checked 2 Deezer artists with this name and 37 of their albums/);
    assert.doesNotMatch(fix, /MusicBrainz artist ID/);
});

test('A3 (US1-AS3): a Deezer name failure lists the near names and says Deezer matches by name only', () => {
    const { fix } = rows(fixture('admin-status.json'))[1];

    assert.match(fix, /Deezer lists Nirvana \(UK\), Approaching Nirvana\./);
    assert.match(fix, /Deezer matches by name only/);
});

test('A4 (US1-AS4): a row matched at one source ends by saying it is tracked through that source', () => {
    const { fix } = rows(fixture('admin-status.json'))[0];

    assert.match(fix, /Tracked through MusicBrainz\.$/);
});

test('A5 (US1-AS5): a row unmatched at every enabled source ends with Not tracked', () => {
    const { fix } = rows(fixture('admin-status.json'))[1];

    assert.match(fix, /Not tracked\.$/);
});

test('A6 (US1-AS6): a row failing at both sources has one fix per failed source and no fix twice', () => {
    const { fixMarkup } = rows(fixture('admin-status.json'))[1];
    const fixes = [...fixMarkup.matchAll(/<p class="nr-fix">([\s\S]*?)<\/p>/g)].map(m => m[1]);

    assert.equal(fixes.length, 2);
    assert.notEqual(fixes[0], fixes[1]);
});
