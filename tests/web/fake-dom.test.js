'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const { documentFor } = require('./fake-dom.js');

// The stand-in specified by specs/005-page-json-casing/contracts/page-sandbox.md. Its own contract
// is tested here because FR-017 and FR-018 make it a deliverable with stated limits, not a private
// helper: a later author must be able to tell what a passing page test did and did not exercise.

test('an element gives back what was written to it', () => {
    const document = documentFor('user-view.html');
    const panel = document.getElementById('nr-panel');

    panel.innerHTML = '<p>one</p>';
    panel.textContent = 'two';
    panel.hidden = true;
    panel.setAttribute('aria-labelledby', 'nr-tab-list');

    assert.equal(panel.innerHTML, '<p>one</p>');
    assert.equal(panel.textContent, 'two');
    assert.equal(panel.hidden, true);
    assert.equal(panel.getAttribute('aria-labelledby'), 'nr-tab-list');
});

test('insertAdjacentHTML beforeend appends rather than replacing', () => {
    const row = documentFor('admin.html').getElementById('nr-unmatched');

    row.innerHTML = '<tr>one</tr>';
    row.insertAdjacentHTML('beforeend', '<tr>two</tr>');

    assert.equal(row.innerHTML, '<tr>one</tr><tr>two</tr>');
});

test('an id the page does not declare answers null, so reaching for an unmodelled element fails loudly', () => {
    const document = documentFor('user-view.html');

    assert.notEqual(document.getElementById('nr-panel'), null);
    assert.equal(document.getElementById('nr-not-declared-by-the-page'), null);
});

test('a page loaded through the sandbox runs its initialization to completion', () => {
    const { loadPageDom } = require('./load-page.js');

    const { document } = loadPageDom('user-view.html');

    // The page sets this the moment it has found its root and claimed it; reaching it means every
    // element lookup above that point resolved.
    assert.equal(document.getElementById('nr-user-view').dataset.nrReady, '1');
});
