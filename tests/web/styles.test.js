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

/** The body of the `@media (max-width: 600px)` block, or an empty string when the page has none. */
function narrowScreen() {
    const at = STYLE.indexOf('@media (max-width: 600px)');
    if (at < 0) return '';
    let depth = 0;
    for (let i = STYLE.indexOf('{', at); i < STYLE.length; i++) {
        if (STYLE[i] === '{') depth++;
        if (STYLE[i] === '}' && --depth === 0) return STYLE.slice(STYLE.indexOf('{', at) + 1, i);
    }
    return '';
}

test('U59: below 600 px the actions take their own row, in equal columns', () => {
    const actions = declarations('.nr-actions', narrowScreen());

    assert.deepEqual([actions['grid-column'], actions.display, actions['grid-auto-flow'], actions['grid-auto-columns']], ['1 / -1', 'grid', 'column', '1fr']);
});

// US4: WCAG 2 contrast, computed here from relative luminance; no library (FR-010, SC-004).

/** The card background the spec measures against (FR-010, contracts/user-view.md). */
const CARD = '#1c1c1c';

/** WCAG 2 relative luminance of a `#rrggbb` colour. */
function luminance(hex) {
    const [r, g, b] = [1, 3, 5]
        .map(i => parseInt(hex.slice(i, i + 2), 16) / 255)
        .map(c => (c <= 0.03928 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4));
    return 0.2126 * r + 0.7152 * g + 0.0722 * b;
}

/** WCAG 2 contrast ratio of two `#rrggbb` colours, lighter over darker. Not rounded: WCAG compares the exact ratio with 4.5. */
function contrast(a, b) {
    const [light, dark] = [luminance(a), luminance(b)].sort((x, y) => y - x);
    return (light + 0.05) / (dark + 0.05);
}

test('U63: the contrast formula rates white on black at 21, its upper bound', () => {
    assert.equal(contrast('#ffffff', '#000000'), 21);
});

test('U62: the contrast formula rates the browser\'s default link blue on the card below 4.5, the defect the spec reports', () => {
    assert.ok(contrast('#0000ee', CARD) < 4.5, `#0000ee on ${CARD} rated ${contrast('#0000ee', CARD)}`);
});

test('A13: the declared source-link colour has a contrast of at least 4.5:1 against the card', () => {
    const color = declarations('.nr-links a').color;

    assert.ok(color !== undefined && contrast(color, CARD) >= 4.5, `.nr-links a declares ${color}`);
});
