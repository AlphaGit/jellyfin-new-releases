'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const { loadPageDom } = require('./load-page.js');

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
