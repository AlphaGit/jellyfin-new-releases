'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const { documentFor, FakeElement, actionRow, keepsListening } = require('./fake-dom.js');

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

test('addEventListener keeps the options it was given readable', () => {
    const panel = documentFor('user-view.html').getElementById('nr-panel');
    const handler = () => {};

    panel.addEventListener('error', handler, true);

    assert.deepEqual(panel.listenerOptions.error, [true]);
});

test('remove detaches an element from the element it was appended to', () => {
    const box = new FakeElement('nr-cover');
    const img = box.appendChild(new FakeElement('img'));

    img.remove();

    assert.deepEqual(box.children, []);
});

for (const [selector, expected] of [
    ['button[data-action]', 'button'],
    ['.nr-actions', 'actions'],
    ['.nr-row', 'article'],
    ['article', 'article'],
    ['div[data-action]', null],
    ['.nr-missing', null],
]) {
    test(`closest("${selector}") from a row's button finds ${expected ?? 'nothing'}, itself included, walking up`, () => {
        const row = actionRow();

        assert.equal(row.button.closest(selector), expected && row[expected]);
    });
}


test('closest refuses a selector it does not model, rather than answering it wrongly', () => {
    assert.throws(() => actionRow().button.closest('.nr-row .nr-actions'), /not modelled/);
});

// keepsListening reads the options the stand-in records. U71 and U57 rely on it.
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
    test(`keepsListening(${JSON.stringify(options)}) ${expected ? 'keeps' : 'stops'} listening (U71, U57)`, () => {
        assert.equal(keepsListening(options), expected);
    });
}
