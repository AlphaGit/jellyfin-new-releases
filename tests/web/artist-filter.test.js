'use strict';

const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');
const assert = require('node:assert/strict');
const { loadPageDom, settled } = require('./load-page.js');

// 007 US1: the Artist filter is a native suggestion list (`<input list>` + `<datalist>`). The
// browser does the matching (FR-001), so these tests assert what the page writes into the list
// and what it asks the server for, never which suggestions a browser would show.

const ARTISTS = {
    items: [
        { jellyfinId: 'a5a5a5a5a5a5a5a5a5a5a5a5a5a5a5a5', name: 'ASP' },
        { jellyfinId: 'a6a6a6a6a6a6a6a6a6a6a6a6a6a6a6a6', name: 'Aspen' },
        { jellyfinId: 'a7a7a7a7a7a7a7a7a7a7a7a7a7a7a7a7', name: 'Wasp' },
    ],
};


/** Loads the view with an `ApiClient` that answers Artists with `artists` (an `Error` rejects it) and records every request. */
async function loadView(artists = ARTISTS) {
    const requests = [];
    const loaded = loadPageDom('user-view.html', {
        ApiClient: {
            ajax: options => {
                requests.push(options.type + ' ' + options.url);
                if (!options.url.endsWith('/Artists')) return Promise.resolve({ items: [], hasStoredReleases: false });
                return artists instanceof Error ? Promise.reject(artists) : Promise.resolve(artists);
            },
        },
    });
    await settled();
    return { requests, ...loaded };
}

/** The `value` of every `<option>` the page wrote into the suggestion list, in order. */
function suggestions(document) {
    const list = document.getElementById('nr-f-artist-list');
    return list ? [...list.innerHTML.matchAll(/<option value="([^"]*)">/g)].map(m => m[1]) : [];
}

test('A1: with artists ASP, Aspen and Wasp the suggestion list offers all three names', async () => {
    const { document } = await loadView();

    assert.deepEqual(suggestions(document), ['ASP', 'Aspen', 'Wasp']);
});

/** Types `text` into the Artist field: sets its value, then fires the `input` listeners the page registered. */
async function type(document, text) {
    const field = document.getElementById('nr-f-artist');
    field.value = text;
    (field.listeners.input || []).forEach(handler => handler());
    await settled();
}

test('A2: when the field text becomes the name ASP the page requests releases with ASP\'s artistId', async () => {
    const { document, requests } = await loadView();

    await type(document, 'ASP');

    assert.equal(requests.at(-1), 'GET Plugins/NewReleases/Releases?artistId=a5a5a5a5a5a5a5a5a5a5a5a5a5a5a5a5');
});

const ASP_FILTER = 'GET Plugins/NewReleases/Releases?artistId=a5a5a5a5a5a5a5a5a5a5a5a5a5a5a5a5';
const UNFILTERED = 'GET Plugins/NewReleases/Releases';

test('A3: after ASP is applied, emptying the field requests releases with no artistId', async () => {
    const { document, requests } = await loadView();
    await type(document, 'ASP');
    assert.equal(requests.at(-1), ASP_FILTER);

    await type(document, '');

    assert.equal(requests.at(-1), UNFILTERED);
});

test('A3: after ASP is applied, pressing Clear requests releases with no artistId', async () => {
    const { document, requests } = await loadView();
    await type(document, 'ASP');
    assert.equal(requests.at(-1), ASP_FILTER);

    document.getElementById('nr-f-clear').listeners.click[0]();
    await settled();

    assert.equal(requests.at(-1), UNFILTERED);
});

/** Leaves the Artist field: fires the `change` listeners the page registered, as a browser does on blur. */
async function leave(document) {
    (document.getElementById('nr-f-artist').listeners.change || []).forEach(handler => handler());
    await settled();
}

// FR-002, with case ignored (maintainer decision, 2026-10-03). None of these is a name. The last four
// are keys every plain JavaScript object inherits, so a lookup that reads them would invent a match.
for (const text of ['As', 'ASP ', 'constructor', 'toString', 'hasOwnProperty', '__proto__']) {
    test(`A4: "${text}" equals no artist name, so typing it and leaving the field request releases with no artistId`, async () => {
        const { document, requests } = await loadView();
        const before = requests.length;

        await type(document, text);
        await leave(document);

        assert.deepEqual(requests.slice(before), [UNFILTERED]);
    });
}

// FR-002, with case ignored (maintainer decision, 2026-10-03). The library scan merges names that
// differ only in case, so at most one artist can match. The last row is a name that is also an
// inherited object key, which only an own-key lookup can hold.
const PROTO_ARTIST = { items: [{ jellyfinId: 'a9a9a9a9a9a9a9a9a9a9a9a9a9a9a9a9', name: '__proto__' }] };
// Accented names, as a browser hands them over from the suggestion list (precomposed, NFC).
const ACCENTED = { items: [{ jellyfinId: 'b1b1b1b1b1b1b1b1b1b1b1b1b1b1b1b1', name: 'Björk' }, { jellyfinId: 'b2b2b2b2b2b2b2b2b2b2b2b2b2b2b2b2', name: 'Sigur Rós' }] };

for (const [text, artists, id] of [
    ['asp', ARTISTS, 'a5a5a5a5a5a5a5a5a5a5a5a5a5a5a5a5'],
    ['aSP', ARTISTS, 'a5a5a5a5a5a5a5a5a5a5a5a5a5a5a5a5'],
    ['WASP', ARTISTS, 'a7a7a7a7a7a7a7a7a7a7a7a7a7a7a7a7'],
    ['__PROTO__', PROTO_ARTIST, 'a9a9a9a9a9a9a9a9a9a9a9a9a9a9a9a9'],
    ['Björk', ACCENTED, 'b1b1b1b1b1b1b1b1b1b1b1b1b1b1b1b1'],
    ['sigur rós', ACCENTED, 'b2b2b2b2b2b2b2b2b2b2b2b2b2b2b2b2'],
]) {
    test(`A15: typing "${text}" applies the artist whose name it equals with case ignored`, async () => {
        const { document, requests } = await loadView(artists);
        const before = requests.length;

        await type(document, text);

        assert.deepEqual(requests.slice(before), ['GET Plugins/NewReleases/Releases?artistId=' + id]);
    });
}

// SC-001 names a library of 1,000 artists. Every one is offered, and the last one applies like the first.
const LIBRARY = { items: Array.from({ length: 1000 }, (_, i) => ({ jellyfinId: 'c' + String(i + 1).padStart(31, '0'), name: 'Artist ' + (i + 1) })) };

test('A1, A2: in a library of 1,000 artists every name is suggested, and the last one applies', async () => {
    const { document, requests } = await loadView(LIBRARY);
    const before = requests.length;

    await type(document, 'Artist 1000');

    assert.deepEqual([suggestions(document).length, suggestions(document).at(-1), requests.slice(before)],
        [1000, 'Artist 1000', ['GET Plugins/NewReleases/Releases?artistId=c' + '1000'.padStart(31, '0')]]);
});

/** The page source, for the declarations the fake DOM does not model. */
const PAGE = fs.readFileSync(path.join(__dirname, '..', '..', 'src', 'Jellyfin.Plugin.NewReleases', 'Web', 'user-view.html'), 'utf8');

test('A5: the Artist control is a text input labelled "Artist" and bound to the suggestion list', () => {
    // Exactly as contracts/user-view.md writes it: `autocomplete="off"` keeps the browser's own history out of the suggestions.
    assert.equal(/<label for="nr-f-artist">[\s\S]*?<\/datalist>/.exec(PAGE)?.[0],
        '<label for="nr-f-artist">Artist</label><input id="nr-f-artist" type="text" list="nr-f-artist-list" autocomplete="off" placeholder="All artists"><datalist id="nr-f-artist-list"></datalist>');
});

test('A5: the page adds no key handling to the Artist control, so the browser keeps its own', async () => {
    const { document } = await loadView();

    assert.deepEqual(Object.keys(document.getElementById('nr-f-artist').listeners).filter(type => type.startsWith('key')), []);
});

test('U42: artistIndex maps each name, in lower case, to its jellyfinId', async () => {
    const { internals } = await loadView();

    // Spread into this realm: the page's object comes from the sandbox's own `Object`.
    assert.deepEqual({ ...internals.artistIndex(ARTISTS.items) }, {
        asp: 'a5a5a5a5a5a5a5a5a5a5a5a5a5a5a5a5',
        aspen: 'a6a6a6a6a6a6a6a6a6a6a6a6a6a6a6a6',
        wasp: 'a7a7a7a7a7a7a7a7a7a7a7a7a7a7a7a7',
    });
});

test('U44: a name with markup characters is written into its option escaped', async () => {
    const { document } = await loadView({ items: [{ jellyfinId: 'a8a8a8a8a8a8a8a8a8a8a8a8a8a8a8a8', name: 'Guns "N" <Roses>' }] });

    assert.equal(document.getElementById('nr-f-artist-list').innerHTML, '<option value="Guns &quot;N&quot; &lt;Roses&gt;">');
});

// With no artist list, nothing is a name: neither a real one nor an inherited object key.
for (const text of ['ASP', 'constructor']) {
    test(`U45: when the Artists request fails, typing "${text}" and leaving the field request releases with no artistId`, async () => {
        const { document, requests } = await loadView(new Error('500'));
        const before = requests.length;

        await type(document, text); // throws if the failure left the field unusable
        await leave(document);

        assert.deepEqual(requests.slice(before), [UNFILTERED]);
    });
}

test('U46: typing the applied name again sends no second releases request', async () => {
    const { document, requests } = await loadView();
    await type(document, 'ASP');
    const before = requests.length;

    await type(document, 'ASP');

    assert.deepEqual(requests.slice(before), []);
});

test('U64: typing part of a name while no artist is applied sends no releases request', async () => {
    const { document, requests } = await loadView();
    const before = requests.length;

    await type(document, 'As');

    assert.deepEqual(requests.slice(before), []);
});

test('U65: after Clear removes ASP, typing ASP again applies it again', async () => {
    const { document, requests } = await loadView();
    await type(document, 'ASP');
    document.getElementById('nr-f-clear').listeners.click[0]();
    await settled();

    await type(document, 'ASP');

    assert.equal(requests.at(-1), ASP_FILTER);
});
