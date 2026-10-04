'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const { loadPageDom } = require('./load-page.js');
const { declaredIds } = require('./fake-dom.js');

// What each page actually asks the server for, captured from a recording `ApiClient` while the page
// runs. This is the paths as sent, not as written in the source: 005 dropped the source-scanning
// behaviour (U24) in favour of these. See specs/005-page-json-casing/tdd/cycle-log.md, cycle 16.

/** Loads a page with an `ApiClient` that records every request instead of answering one. */
function recordRequests(page, body = {}, overrides = {}) {
    const requests = [];
    const loaded = loadPageDom(page, {
        ApiClient: {
            ajax: options => {
                requests.push(options.type + ' ' + options.url);
                return Promise.resolve(body);
            },
        },
        ...overrides,
    });

    return { requests, ...loaded };
}

/** Lets every pending promise callback run; the pages chain their loads through `.then`. */
const settled = () => new Promise(resolve => setImmediate(resolve));

test('the New Releases view asks for the artist filter and the list', async () => {
    const { requests } = recordRequests('user-view.html', { items: [], hasStoredReleases: false });

    await settled();

    assert.deepEqual(requests, ['GET Plugins/NewReleases/Artists', 'GET Plugins/NewReleases/Releases']);
});

test('the administrator page asks for its status and posts its actions', async () => {
    // The page puts every destructive action behind `Dashboard.confirm`; answering yes is what
    // makes it send the request at all.
    const { requests, document } = recordRequests('admin.html', {}, {
        Dashboard: { confirm: (text, title, answer) => answer(true) },
    });
    const page = document.getElementById('newreleasesConfigPage');

    // The fake records listeners and never fires them, so a test fires the ones it means to drive.
    // No event object is synthesized and nothing bubbles; see contracts/page-sandbox.md.
    page.listeners.pageshow[0]();
    await settled();

    for (const id of ['nr-run-now', 'nr-purge', 'nr-clear-archive']) {
        document.getElementById(id).listeners.click[0]();
        await settled();
    }

    assert.deepEqual([...new Set(requests)], [
        'GET Plugins/NewReleases/Admin/Status',
        'POST Plugins/NewReleases/Admin/RunNow',
        'POST Plugins/NewReleases/Admin/Purge',
        'POST Plugins/NewReleases/Admin/ClearArchive',
    ]);
});

// U71, characterization (maintainer decision T083): the view's listeners predate 007. A listener that
// stops after its first event (`once`) or can be cut off (`signal`) sends no request the next time.
// The stand-in records options but ignores them, so they are read here.

/** Whether a listener registered with `options` hears every event: no `once`, no `signal`. */
function keepsListening(options) {
    return typeof options !== 'object' || (!options.once && options.signal === undefined);
}

for (const [options, expected] of [
    [undefined, true],
    [true, true],
    [false, true],
    [{ capture: true }, true],
    [{ passive: true, once: false }, true],
    [{ once: true }, false],
    [{ capture: true, once: true }, false],
    [{ signal: {} }, false],
]) {
    test(`U71 helper: ${JSON.stringify(options)} ${expected ? 'keeps' : 'stops'} listening`, () => {
        assert.equal(keepsListening(options), expected);
    });
}

test('U71: every listener the New Releases view registers keeps listening', () => {
    const { document } = recordRequests('user-view.html', { items: [], hasStoredReleases: false });
    const stopping = [...declaredIds('user-view.html')].flatMap(id => Object.entries(document.getElementById(id).listenerOptions)
        .flatMap(([type, all]) => all.filter(options => !keepsListening(options)).map(() => id + ' ' + type)));

    assert.deepEqual(stopping, []);
});
