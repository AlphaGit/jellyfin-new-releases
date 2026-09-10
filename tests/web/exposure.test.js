'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const { loadPage } = require('./load-page.js');

// 002 FR-013/FR-016: each page exposes the helpers that can be computed without a page, so they
// can be tested without a browser and without shipping a second file.

test('user-view.html exposes its pure helpers', () => {
    const internals = loadPage('user-view.html');

    assert.deepEqual(Object.keys(internals).sort(), ['artistLink', 'esc', 'groupOf', 'stalenessText']);
    for (const name of Object.keys(internals)) {
        assert.equal(typeof internals[name], 'function', `${name} should be a function`);
    }
});

test('admin.html exposes its pure helpers', () => {
    const internals = loadPage('admin.html');

    assert.deepEqual(Object.keys(internals).sort(), ['esc', 'healthText']);
    for (const name of Object.keys(internals)) {
        assert.equal(typeof internals[name], 'function', `${name} should be a function`);
    }
});
