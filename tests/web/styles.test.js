'use strict';

const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');
const assert = require('node:assert/strict');

// 007 US2–US4: the stylesheet rules the view's layout and contrast depend on. The fake DOM does no
// layout (specs/005-page-json-casing/contracts/page-sandbox.md), so these assert the declarations
// in the page's own `<style>` block. Pixels are checked in the real-browser pass, quickstart.md §2.

const PAGE = fs.readFileSync(path.join(__dirname, '..', '..', 'src', 'Jellyfin.Plugin.NewReleases', 'Web', 'user-view.html'), 'utf8');
const STYLE = /<style>([\s\S]*?)<\/style>/.exec(PAGE)[1];
const SCOPE = '#nr-user-view ';

/** Every `property: value` declared for `selector` by a top-level rule whose selector list names it. Later rules win, as in CSS. */
function declarations(selector, css = STYLE.replace(/@media[^{]*\{(?:[^{}]*\{[^{}]*\})*[^{}]*\}/g, '')) {
    const found = {};
    for (const [, selectors, body] of css.matchAll(/([^{}]+)\{([^{}]*)\}/g)) {
        if (!selectors.split(',').map(s => s.trim()).includes(SCOPE + selector)) continue;
        for (const declaration of body.split(';')) {
            const at = declaration.indexOf(':');
            if (at > 0) found[declaration.slice(0, at).trim()] = declaration.slice(at + 1).trim();
        }
    }
    return found;
}

test('U60: .nr-cover declares a 64 by 64 box with a background', () => {
    const cover = declarations('.nr-cover');

    assert.deepEqual([cover.width, cover.height, Boolean(cover.background)], ['64px', '64px', true]);
});

test('U61: a cover image fills the box without stretching', () => {
    assert.equal(declarations('.nr-cover img')['object-fit'], 'cover');
});

test('U66: a card lays out the 64 px cover, then the details, then the actions', () => {
    assert.equal(declarations('.nr-row')['grid-template-columns'], '64px 1fr auto');
});

test('A11: the List-tab buttons fill one shared column: .nr-actions stretches its buttons, and each is full width', () => {
    assert.deepEqual([declarations('.nr-actions')['align-items'], declarations('.nr-actions button').width], ['stretch', '100%']);
});
