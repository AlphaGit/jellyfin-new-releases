'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const { loadPage } = require('./load-page.js');

// Characterization (002 SC-008, US3-AS2). Release titles and artist names arrive from
// MusicBrainz and Deezer and are concatenated into HTML by `row()` in about ten places.
// `esc` is the only thing between them and the DOM, and it had no test.

for (const page of ['user-view.html', 'admin.html']) {
    const { esc } = loadPage(page);

    test(`${page}: escapes markup in text that came from a source`, () => {
        assert.equal(esc('<script>alert(1)</script>'), '&lt;script&gt;alert(1)&lt;/script&gt;');
        assert.equal(esc('Rock & Roll'), 'Rock &amp; Roll');
        assert.equal(esc('She said "hi"'), 'She said &quot;hi&quot;');
        assert.equal(esc("O'Brien"), 'O&#39;Brien');
        assert.equal(esc('" onerror="x'), '&quot; onerror=&quot;x');
    });

    test(`${page}: leaves ordinary text alone and turns nothing into the empty string`, () => {
        assert.equal(esc('Discovery'), 'Discovery');
        assert.equal(esc(null), '');
        assert.equal(esc(undefined), '');
        assert.equal(esc(0), '0');
    });
}
