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

/** Every rule in `css`, `@media` blocks included, once per selector in its list. */
function rules(css = STYLE) {
    const found = [];
    for (const [, selectors, body] of css.matchAll(/([^{}]+)\{([^{}]*)\}/g)) {
        const declared = {};
        for (const declaration of body.split(';')) {
            const at = declaration.indexOf(':');
            if (at > 0) declared[declaration.slice(0, at).trim()] = declaration.slice(at + 1).trim();
        }
        for (const selector of selectors.split(',')) found.push({ selector: selector.trim(), declared });
    }
    return found;
}

/** Every `property: value` declared for `selector` by a top-level rule whose selector list names it. Later rules win, as in CSS. */
function declarations(selector, css = STYLE.replace(/@media[^{]*\{(?:[^{}]*\{[^{}]*\})*[^{}]*\}/g, '')) {
    return Object.assign({}, ...rules(css).filter(rule => rule.selector === SCOPE + selector).map(rule => rule.declared));
}

/** The `background` shorthand's tokens that are not a colour: images, positions, sizes, repeats, boxes, and `!important`. */
const BACKGROUND_KEYWORDS = /^(none|!important|url\(.*\)|repeat(-[xy])?|no-repeat|space|round|scroll|fixed|local|center|top|bottom|left|right|(border|padding|content)-box|text|auto|cover|contain|-?[\d.]+(%|[a-z]+)?)$/;

/**
 * Whether a declared background shows a colour: once its image, position, size and repeat tokens are set
 * aside, exactly one token is left, and it is a visible colour. ponytail: a function with nested
 * parentheses (a gradient) is not read.
 */
function isVisibleColour(value) {
    const tokens = ((value || '').trim().toLowerCase().match(/[\w-]+\([^)]*\)|[^\s/]+/g) || []).filter(token => !BACKGROUND_KEYWORDS.test(token));
    return tokens.length === 1 && isVisibleColourToken(tokens[0]);
}

/** Whether one colour token can be seen: not `transparent`, not a CSS-wide keyword (which leaves a background transparent or the parent's), and not a zero alpha in any notation. */
function isVisibleColourToken(colour) {
    if (['transparent', 'initial', 'inherit', 'unset', 'revert', 'revert-layer'].includes(colour)) return false;
    const call = /^\w+\(([^)]*)\)$/.exec(colour);
    if (call) {
        const parts = call[1].split(/[\s,/]+/).filter(Boolean);
        return parts.length < 4 || parseFloat(parts[3]) > 0;
    }
    const hex = /^#([0-9a-f]{4}|[0-9a-f]{8})$/.exec(colour);
    return !hex || !/^0+$/.test(hex[1].slice(hex[1].length / 4 * 3));
}

for (const [value, expected] of [
    [undefined, false],
    ['', false],
    ['none', false],
    ['TRANSPARENT', false],
    ['rgba(127,127,127,0)', false],
    ['rgba(127, 127, 127, 0.0)', false],
    ['rgb(127 127 127 / 0%)', false],
    ['hsla(0,0%,50%,0)', false],
    ['#7f7f7f00', false],
    ['#7770', false],
    ['initial', false],
    ['inherit', false],
    ['unset', false],
    ['revert', false],
    ['REVERT-LAYER', false],
    ['transparent none', false],
    ['none transparent', false],
    ['rgba(0,0,0,0) none', false],
    ['transparent no-repeat', false],
    ['url(cover.png)', false],
    ['rgba(127,127,127,.18)', true],
    ['rgb(0,0,0)', true],
    ['#3a3a3a', true],
    ['#333', true],
    ['#7f7f7f80', true],
    ['grey', true],
    ['#3a3a3a no-repeat', true],
    ['rgba(127,127,127,.18) !important', true],
    ['rgb(0 0 0 / .5)', true],
]) {
    test(`U60 helper: ${JSON.stringify(value)} ${expected ? 'is' : 'is not'} a visible colour`, () => {
        assert.equal(isVisibleColour(value), expected);
    });
}

test('U60: .nr-cover declares a 64 by 64 box with a background', () => {
    const cover = declarations('.nr-cover');

    assert.deepEqual([cover.width, cover.height, isVisibleColour(cover.background)], ['64px', '64px', true]);
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

// What can reach a source link, and what takes its underline off. Predicates, so each is pinned by a
// table of accepting and rejecting cases before the A14 test relies on it.

/** The ids and classes a source link sits inside, and the attributes it carries: `row()` in user-view.html. */
const LINK_ANCESTORS = ['#nr-user-view', '#nr-panel', '.nr-list', '.nr-row', '.nr-links'];
const LINK_ATTRIBUTES = ['href', 'target', 'rel'];

/**
 * Whether `selector` can match a source link: its last compound is `a`, `*` or bare pseudo-classes, with
 * no class or id and only the link's own attributes, and every id or class of an ancestor compound is one of
 * the link's ancestors. A compound before `~` or `+` is a sibling, so any element may stand there.
 * ponytail: splits on whitespace and commas, so `:is(…)` lists and quoted spaces are not read.
 */
function reachesSourceLink(selector) {
    const parts = selector.trim().split(/\s*([>+~])\s*|\s+/); // compound, combinator (undefined for a space), compound, …
    const subject = parts.pop();
    const ancestors = parts.filter((part, i) => i % 2 === 0 && !['+', '~'].includes(parts[i + 1]));
    return /^(a|\*)?(\[[\w-]+[^\]]*\]|:[\w-]+(\([^)]*\))?)*$/.test(subject) && subject !== ''
        && [...subject.matchAll(/\[([\w-]+)/g)].every(([, name]) => LINK_ATTRIBUTES.includes(name))
        && ancestors.every(compound => (compound.match(/[.#][\w-]+/g) || []).every(name => LINK_ANCESTORS.includes(name)));
}

/**
 * Whether `declared` turns the underline off or hides it: a `none` line, a `transparent` colour or a zero
 * thickness, through the shorthand or a longhand. ponytail: reads tokens, so a zero-alpha colour function is not read.
 */
function removesUnderline(declared) {
    return ['text-decoration', 'text-decoration-line', 'text-decoration-color', 'text-decoration-thickness']
        .flatMap(property => (declared[property] || '').trim().toLowerCase().split(/\s+/))
        .some(token => /^none(!|$)/.test(token) || token === 'transparent' || parseFloat(token) === 0);
}

for (const [selector, expected] of [
    ['#nr-user-view .nr-links a', true],
    ['#nr-user-view .nr-links a:visited', true],
    ['#nr-user-view .nr-links a:hover', true],
    ['#nr-user-view .nr-links > a', true],
    ['#nr-user-view a', true],
    ['a:focus-visible', true],
    ['#nr-user-view .nr-row a', true],
    ['#nr-user-view .nr-list a:hover', true],
    ['#nr-user-view .nr-row:hover a', true],
    ['#nr-user-view #nr-panel a', true],
    ['#nr-user-view .nr-row div a', true],
    ['#nr-user-view .nr-links a[href]', true],
    ['#nr-user-view .nr-links :any-link', true],
    ['#nr-user-view .nr-links *', true],
    ['#nr-user-view .nr-meta ~ .nr-links a', true],
    ['#nr-user-view .nr-title ~ .nr-links a:hover', true],
    ['#nr-user-view .nr-cover + div a', true],
    ['#nr-user-view #nr-filters ~ #nr-panel a', true],
    ['#nr-user-view .nr-title ~ .nr-artist a', false],
    ['#nr-user-view .nr-artist a', false],
    ['#nr-user-view .nr-filter a', false],
    ['#nr-user-view [role="tab"]', false],
    ['#nr-user-view .nr-links a[download]', false],
    ['#nr-user-view .nr-links', false],
    ['#nr-user-view .nr-links span', false],
    ['#nr-user-view .nr-links a.other', false],
    ['#nr-user-view .nr-links abbr', false],
]) {
    test(`A14 helper: "${selector}" ${expected ? 'can' : 'cannot'} reach a source link`, () => {
        assert.equal(reachesSourceLink(selector), expected);
    });
}

for (const [declared, expected] of [
    [{ 'text-decoration': 'none' }, true],
    [{ 'text-decoration-line': 'none' }, true],
    [{ 'text-decoration': 'none !important' }, true],
    [{ 'text-decoration': 'none solid red' }, true],
    [{ 'text-decoration-color': 'transparent' }, true],
    [{ 'text-decoration': 'underline transparent' }, true],
    [{ 'text-decoration-thickness': '0' }, true],
    [{ 'text-decoration-color': '#00a4dc' }, false],
    [{ 'text-decoration-thickness': '2px' }, false],
    [{}, false],
    [{ color: '#00a4dc' }, false],
    [{ 'text-decoration': 'underline' }, false],
    [{ 'text-decoration': 'underline dotted' }, false],
]) {
    test(`A14 helper: ${JSON.stringify(declared)} ${expected ? 'removes' : 'keeps'} the underline`, () => {
        assert.equal(removesUnderline(declared), expected);
    });
}

test('A16: every rule that reaches a source link and declares a colour keeps 4.5:1 against the card, so hover and focus keep it too', () => {
    const colours = rules().filter(rule => reachesSourceLink(rule.selector) && rule.declared.color !== undefined);
    const below = colours.filter(rule => !(contrast(rule.declared.color, CARD) >= 4.5)).map(rule => rule.selector + ' ' + rule.declared.color);

    assert.deepEqual([colours.length > 0, below], [true, []]);
});

/** Whether `declared` dims what it reaches: an `opacity` below 1, or any `filter` but `none`. */
function dimsText(declared) {
    const opacity = (declared.opacity || '').trim();
    return parseFloat(opacity) / (opacity.endsWith('%') ? 100 : 1) < 1 || !['', 'none'].includes((declared.filter || '').trim());
}

for (const [declared, expected] of [
    [{}, false],
    [{ color: '#00a4dc' }, false],
    [{ opacity: '1' }, false],
    [{ opacity: '100%' }, false],
    [{ filter: 'none' }, false],
    [{ opacity: '.3' }, true],
    [{ opacity: '0' }, true],
    [{ opacity: '30%' }, true],
    [{ filter: 'brightness(.5)' }, true],
]) {
    test(`A17 helper: ${JSON.stringify(declared)} ${expected ? 'dims' : 'does not dim'} the text`, () => {
        assert.equal(dimsText(declared), expected);
    });
}

test('A17: no rule that reaches a source link dims it, so hover and focus keep the contrast A16 measures', () => {
    assert.deepEqual(rules().filter(rule => reachesSourceLink(rule.selector) && dimsText(rule.declared)).map(rule => rule.selector), []);
});

/**
 * Whether `selector` can match an element around a source link: its subject is `div`, `article`, `*` or bare
 * pseudo-classes, names only the link's ancestors, and every ancestor compound before it names only the
 * link's ancestors. ponytail: a subject with an attribute selector (`[role="tabpanel"]`) is not read.
 */
function reachesLinkAncestor(selector) {
    const parts = selector.trim().split(/\s*([>+~])\s*|\s+/);
    const subject = parts.pop();
    const ancestors = parts.filter((part, i) => i % 2 === 0 && !['+', '~'].includes(parts[i + 1]));
    return !subject.includes('[') && ['', 'div', 'article', '*'].includes(/^[\w*]*/.exec(subject)[0])
        && [subject, ...ancestors].every(compound => (compound.match(/[.#][\w-]+/g) || []).every(name => LINK_ANCESTORS.includes(name)));
}

for (const [selector, expected] of [
    ['#nr-user-view', true],
    ['#nr-user-view #nr-panel', true],
    ['#nr-user-view .nr-list', true],
    ['#nr-user-view .nr-row', true],
    ['#nr-user-view .nr-row:hover .nr-links', true],
    ['#nr-user-view .nr-row > div', true],
    ['#nr-user-view .nr-cover + div', true],
    ['#nr-user-view article:hover', true],
    ['#nr-user-view :hover', true],
    ['#nr-user-view .nr-links a', false],
    ['#nr-user-view .nr-meta', false],
    ['#nr-user-view .nr-status', false],
    ['#nr-user-view .nr-artist', false],
    ['#nr-user-view .nr-filter label', false],
    ['#nr-user-view .nr-filters div', false],
    ['#nr-user-view h1', false],
]) {
    test(`A18 helper: "${selector}" ${expected ? 'can' : 'cannot'} reach an element around a source link`, () => {
        assert.equal(reachesLinkAncestor(selector), expected);
    });
}

test('A18: hover and focus keep the source link\'s background and do not dim the elements around it', () => {
    const backgrounds = rules().filter(rule => reachesSourceLink(rule.selector)
        && (isVisibleColour(rule.declared.background) || isVisibleColour(rule.declared['background-color'])));
    const dimmed = rules().filter(rule => reachesLinkAncestor(rule.selector) && dimsText(rule.declared));

    assert.deepEqual([backgrounds.map(rule => rule.selector), dimmed.map(rule => rule.selector)], [[], []]);
});

test('A14: a visited source link keeps the same colour', () => {
    assert.equal(declarations('.nr-links a:visited').color, declarations('.nr-links a').color);
});

test('A14: no rule takes the underline off a source link', () => {
    assert.deepEqual(rules().filter(rule => reachesSourceLink(rule.selector) && removesUnderline(rule.declared)).map(rule => rule.selector), []);
});

/**
 * Whether `declared` hides the focus outline: a `none` style, a zero width or a `transparent` colour, through
 * the shorthand or a longhand. ponytail: reads tokens, so a zero-alpha colour function is not read.
 */
function removesOutline(declared) {
    return ['outline', 'outline-style', 'outline-width', 'outline-color']
        .flatMap(property => (declared[property] || '').trim().toLowerCase().split(/\s+/))
        .some(token => token === 'none' || token === 'transparent' || parseFloat(token) === 0);
}

for (const [declared, expected] of [
    [{ outline: 'none' }, true],
    [{ outline: '0' }, true],
    [{ outline: 'none !important' }, true],
    [{ outline: '0px solid #52b54b' }, true],
    [{ outline: '2px solid transparent' }, true],
    [{ 'outline-style': 'none' }, true],
    [{ 'outline-width': '0' }, true],
    [{ 'outline-color': 'transparent' }, true],
    [{}, false],
    [{ color: '#00a4dc' }, false],
    [{ outline: '2px solid #52b54b' }, false],
    [{ 'outline-offset': '0' }, false],
]) {
    test(`A14 helper: ${JSON.stringify(declared)} ${expected ? 'removes' : 'keeps'} the focus outline`, () => {
        assert.equal(removesOutline(declared), expected);
    });
}

test('A14: a focused source link shows the focus outline', () => {
    const removed = rules().filter(rule => reachesSourceLink(rule.selector) && removesOutline(rule.declared)).map(rule => rule.selector);

    assert.deepEqual([/solid/.test(declarations(':focus-visible').outline || ''), removed], [true, []]);
});
