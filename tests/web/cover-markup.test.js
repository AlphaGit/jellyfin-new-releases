'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const { imgAttribute } = require('./cover-markup.js');

// The cover-markup reader's own contract. render.test.js and cover-fallback.test.js read the cover
// box through it, so it must read an attribute as a browser does: names without case, first of two
// duplicates wins.

for (const [markup, name, expected] of [
    ['<img LOADING="eager" loading="lazy">', 'loading', 'eager'],
    ['<img data-src="b" src="a">', 'src', 'a'],
    ['<img src="a" alt="">', 'alt', ''],
    ['<img src="a">', 'alt', undefined],
]) {
    test(`imgAttribute reads ${name} from ${JSON.stringify(markup)} as a browser does`, () => {
        assert.equal(imgAttribute(markup, name), expected);
    });
}
