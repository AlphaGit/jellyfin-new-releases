'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const { loadPage } = require('./load-page.js');
const { FakeElement } = require('./fake-dom.js');

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
