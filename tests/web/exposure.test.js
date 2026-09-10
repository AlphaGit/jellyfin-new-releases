'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const { loadPage } = require('./load-page.js');

// 002 FR-013/FR-016: each page exposes the helpers that can be computed without a page, so they
// can be tested without a browser and without shipping a second file.
//
// **These are exact-set assertions on purpose, and they are meant to fail on a new helper.**
// No requirement states the set, so the rule they enforce is stated here instead: a helper added
// to `NewReleasesInternals` arrives with a behaviour on `tdd/test-list.md` and a test of its own.
// That is not pedantry — it is the gate the project has now missed twice. `T029` put `checkedText`
// on `admin.html` with no behaviour on the list, and both TDD audits found the same consequence:
// first three of its four ladder boundaries pinned on one side only, then its clock-correction
// clamp with no test at all. Updating the array below is the cheap half of adding a helper; the
// list entry and the test are the half that matters.
//
// Callability needs no assertion here — every name below is called as a function by
// `staleness.test.js`, `checked.test.js`, `esc.test.js` or `page-helpers.test.js`.

test('user-view.html exposes its pure helpers', () => {
    const internals = loadPage('user-view.html');

    assert.deepEqual(Object.keys(internals).sort(), ['artistLink', 'esc', 'groupOf', 'stalenessText']);
});

test('admin.html exposes its pure helpers', () => {
    const internals = loadPage('admin.html');

    assert.deepEqual(Object.keys(internals).sort(), ['checkedText', 'esc', 'healthText']);
});
