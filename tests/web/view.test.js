'use strict';

const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');
const assert = require('node:assert/strict');

// The New Releases view's own controls: what its static markup declares and how its controls are
// wired. 007 T091 characterizes the parts that predate 007 (BASELINE), so a change to them fails here.
// The page source is read as text only for the static markup, as the profile permits.

const PAGE = fs.readFileSync(path.join(__dirname, '..', '..', 'src', 'Jellyfin.Plugin.NewReleases', 'Web', 'user-view.html'), 'utf8');

test('U75: the Type filter offers all types, then Album, EP, Single, Compilation, Live, Remix and Soundtrack', () => {
    const select = /<select id="nr-f-type">([\s\S]*?)<\/select>/.exec(PAGE)[1];

    assert.deepEqual([...select.matchAll(/<option(?: value="([^"]*)")?>([^<]*)<\/option>/g)].map(([, value, text]) => [value ?? text, text]), [
        ['', 'All types'], ['Album', 'Album'], ['EP', 'EP'], ['Single', 'Single'], ['Compilation', 'Compilation'],
        ['Live', 'Live'], ['Remix', 'Remix'], ['Soundtrack', 'Soundtrack'],
    ]);
});
