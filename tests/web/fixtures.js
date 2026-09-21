'use strict';

const fs = require('node:fs');
const path = require('node:path');

const DIR = path.join(__dirname, '..', 'fixtures', 'pages');

/**
 * One committed response, parsed fresh on every call so a test that edits it cannot affect another.
 * These are the same files the C# side asserts the server produces, which is what makes the two
 * languages meet without a running host (specs/005-page-json-casing/research.md R5).
 */
function fixture(name) {
    return JSON.parse(fs.readFileSync(path.join(DIR, name), 'utf8'));
}

module.exports = { fixture };
