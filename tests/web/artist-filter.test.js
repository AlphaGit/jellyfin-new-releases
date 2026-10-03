'use strict';

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

/** Loads the view with an `ApiClient` that answers Artists with `artists` and records every request. */
async function loadView(artists = ARTISTS) {
    const requests = [];
    const loaded = loadPageDom('user-view.html', {
        ApiClient: {
            ajax: options => {
                requests.push(options.type + ' ' + options.url);
                return options.url.endsWith('/Artists') ? Promise.resolve(artists) : Promise.resolve({ items: [], hasStoredReleases: false });
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
