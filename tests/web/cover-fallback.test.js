'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const { loadPage, loadPageDom } = require('./load-page.js');
const { FakeElement } = require('./fake-dom.js');
const { fixture } = require('./fixtures.js');

// 007 US2: what the view does when a cover image fails to load (FR-006a, FR-007, research R7).
// The browser delivers `error` to the image; these tests stand in for it with a FakeElement
// `<img>` inside a FakeElement cover box. Nothing here loads an image.

const DEEZER = 'https://api.deezer.com/album/101/image?size=medium';
const CAA = 'https://coverartarchive.org/release-group/00000000-0000-0000-0000-000000000101/front-250';
const THIRD = 'https://example.test/third.jpg';

/** An `<img>` holding `src` with `fallbacks`, appended to its own cover box. */
function coverImage(src, fallbacks) {
    const box = new FakeElement('nr-cover');
    const img = box.appendChild(new FakeElement('img'));
    img.tagName = 'IMG';
    img.src = src;
    img.dataset.fallback = fallbacks.join(' ');
    return { box, img };
}

test('U55: nextCover with two fallbacks puts the first in src and keeps the second', () => {
    const { img } = coverImage(DEEZER, [CAA, THIRD]);

    loadPage('user-view.html').nextCover(img);

    assert.deepEqual([img.src, img.dataset.fallback], [CAA, THIRD]);
});

test('U56: nextCover with no fallback left removes the image, and the cover box stays as the placeholder', () => {
    const { box, img } = coverImage(CAA, []);

    loadPage('user-view.html').nextCover(img);

    assert.deepEqual(box.children, []);
});

test('U57: the panel has one error listener, registered for the capture phase', () => {
    const { document } = loadPageDom('user-view.html');

    assert.deepEqual(document.getElementById('nr-panel').listenerOptions.error, [true]);
});

// Acceptance, through the real `render`: the `<img>` a browser would build from the written markup,
// with `error` delivered to the panel's listeners as the browser's capture phase does.

const BOTH_SOURCES = 101; // releases.json: Closer to Grey, at MusicBrainz and Deezer

/** Renders `body` and returns the panel and the markup inside the `nr-cover` box of the row with `id`. */
function renderedCover(body, id) {
    const { internals, document } = loadPageDom('user-view.html', { ApiClient: { ajax: () => Promise.resolve(body) } });
    internals.render(body);
    const panel = document.getElementById('nr-panel');
    const row = panel.innerHTML.split('<article').find(r => r.includes('data-id="' + id + '"')) || '';
    const box = /<div class="nr-cover">(.*?)<\/div>/.exec(row);
    return { panel, markup: box ? box[1] : null };
}

/** The decoded value of `name` on the first `<img>` in `markup`, or undefined. */
function imgAttribute(markup, name) {
    const img = /<img [^>]*>/.exec(markup || '');
    const attr = img && new RegExp('\\s' + name + '="([^"]*)"').exec(img[0]);
    return attr ? attr[1].replace(/&amp;/g, '&') : undefined;
}

/** Delivers an image `error` to the panel's listeners, with the image as target. */
function fail(panel, img) {
    (panel.listeners.error || []).forEach(handler => handler({ target: img }));
}

test('A8: on a card for a release at both sources, an error on the Deezer image puts the Cover Art Archive URL in its src', () => {
    const body = fixture('releases.json');
    const { panel, markup } = renderedCover(body, BOTH_SOURCES);
    const { img } = coverImage(imgAttribute(markup, 'src'), [imgAttribute(markup, 'data-fallback')]);

    fail(panel, img);

    assert.equal(img.src, body.items.find(i => i.id === BOTH_SOURCES).covers[1]);
});

test('A9: on a card whose every cover URL fails, the cover box remains and holds no image', () => {
    const { panel, markup } = renderedCover(fixture('releases.json'), BOTH_SOURCES);
    const { box, img } = coverImage(imgAttribute(markup, 'src'), [imgAttribute(markup, 'data-fallback')]);

    fail(panel, img);
    fail(panel, img);

    assert.deepEqual([markup !== null, box.children.length], [true, 0]);
});
