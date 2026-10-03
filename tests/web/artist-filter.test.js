'use strict';

const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');
const assert = require('node:assert/strict');
const { loadPageDom } = require('./load-page.js');

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

/** Lets every pending promise callback run; the page chains its loads through `.then`. */
const settled = () => new Promise(resolve => setImmediate(resolve));

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

test('A4: text that equals no artist name, even one differing only in case, requests releases with no artistId', async () => {
    const { document, requests } = await loadView();

    await type(document, 'asp');

    assert.equal(requests.at(-1), UNFILTERED);
});

/** The page source, for the declarations the fake DOM does not model. */
const PAGE = fs.readFileSync(path.join(__dirname, '..', '..', 'src', 'Jellyfin.Plugin.NewReleases', 'Web', 'user-view.html'), 'utf8');

test('A5: the Artist control is a text input labelled "Artist" and bound to the suggestion list', () => {
    assert.match(PAGE, /<label for="nr-f-artist">Artist<\/label><input id="nr-f-artist" type="text" list="nr-f-artist-list"[^>]*><datalist id="nr-f-artist-list"><\/datalist>/);
});

test('A5: the page adds no key handling to the Artist control, so the browser keeps its own', async () => {
    const { document } = await loadView();

    assert.deepEqual(Object.keys(document.getElementById('nr-f-artist').listeners).filter(type => type.startsWith('key')), []);
});

test('U42: artistIndex maps each name to its jellyfinId', async () => {
    const { internals } = await loadView();

    // Spread into this realm: the page's object comes from the sandbox's own `Object`.
    assert.deepEqual({ ...internals.artistIndex(ARTISTS.items) }, {
        ASP: 'a5a5a5a5a5a5a5a5a5a5a5a5a5a5a5a5',
        Aspen: 'a6a6a6a6a6a6a6a6a6a6a6a6a6a6a6a6',
        Wasp: 'a7a7a7a7a7a7a7a7a7a7a7a7a7a7a7a7',
    });
});

test('U44: a name with markup characters is written into its option escaped', async () => {
    const { document } = await loadView({ items: [{ jellyfinId: 'a8a8a8a8a8a8a8a8a8a8a8a8a8a8a8a8', name: 'Guns "N" <Roses>' }] });

    assert.equal(document.getElementById('nr-f-artist-list').innerHTML, '<option value="Guns &quot;N&quot; &lt;Roses&gt;">');
});

test('U45: when the Artists request fails, the field stays usable and releases are requested with no artistId', async () => {
    const { document, requests } = await loadView(new Error('500'));

    await type(document, 'ASP'); // throws if the failure left the field unusable

    const releases = requests.filter(r => r.includes('/Releases'));
    assert.deepEqual([releases.length > 0, releases.every(r => r === UNFILTERED)], [true, true]);
});

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
