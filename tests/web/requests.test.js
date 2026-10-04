'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const { loadPageDom, settled } = require('./load-page.js');
const { declaredIds, actionRow } = require('./fake-dom.js');

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

// U72, characterization (maintainer decision T083): the action handler predates 007. A row is built as a
// browser builds it from the markup the view writes, and the click is handed to the panel's listener.
for (const action of ['Ignore', 'HaveIt', 'Restore']) {
    test(`U72: the ${action} button posts ${action} for the release of its row`, async () => {
        const { requests, document } = recordRequests('user-view.html', { items: [], hasStoredReleases: false });
        await settled();
        const { button } = actionRow({ id: '102', title: 'Kill for Love', action });

        document.getElementById('nr-panel').listeners.click[0]({ target: button });
        await settled();

        assert.deepEqual(requests.filter(request => request.startsWith('POST')), ['POST Plugins/NewReleases/Releases/102/' + action]);
    });
}

test('U72: a click inside a row but not on a button posts nothing', async () => {
    const { requests, document } = recordRequests('user-view.html', { items: [], hasStoredReleases: false });
    await settled();
    const { actions } = actionRow();

    document.getElementById('nr-panel').listeners.click[0]({ target: actions });
    await settled();

    assert.deepEqual(requests.filter(request => request.startsWith('POST')), []);
});


// 007 T091 group A, characterization (BASELINE): the list asks for the right releases. These pin what
// the view did before 007, so a change to what it asks the server for fails here.

/** Loads the view, lets it settle, and returns the recorder and the document. */
async function loadedView(body = { items: [], hasStoredReleases: true }) {
    const loaded = recordRequests('user-view.html', body);
    await settled();
    return loaded;
}

test('U73: the Archive tab asks for archived releases, and the List tab asks for the others', async () => {
    const { requests, document } = await loadedView();
    const before = requests.length;

    document.getElementById('nr-tab-archive').listeners.click[0]();
    await settled();
    document.getElementById('nr-tab-list').listeners.click[0]();
    await settled();

    assert.deepEqual(requests.slice(before), ['GET Plugins/NewReleases/Releases?archived=true', 'GET Plugins/NewReleases/Releases']);
});

for (const [id, value, sent] of [
    ['nr-f-type', 'EP', 'type=EP'],
    ['nr-f-state', 'Upcoming', 'state=Upcoming'],
    ['nr-f-from', '2026-01-01', 'from=2026-01-01'],
    ['nr-f-to', '2026-12-31', 'to=2026-12-31'],
]) {
    test(`U74: setting ${id} to ${value} asks for releases with ${sent}`, async () => {
        const { requests, document } = await loadedView();
        const before = requests.length;
        const field = document.getElementById(id);

        field.value = value;
        field.listeners.change[0]();
        await settled();

        assert.deepEqual(requests.slice(before), ['GET Plugins/NewReleases/Releases?' + sent]);
    });
}

test('U76: an action in the Archive tab reloads the Archive and stays on it', async () => {
    const { requests, document } = await loadedView();
    document.getElementById('nr-tab-archive').listeners.click[0]();
    await settled();
    const before = requests.length;

    document.getElementById('nr-panel').listeners.click[0]({ target: actionRow({ action: 'Restore' }).button });
    await settled();

    assert.deepEqual([requests.slice(before), document.getElementById('nr-tab-archive').getAttribute('aria-selected')],
        [['POST Plugins/NewReleases/Releases/101/Restore', 'GET Plugins/NewReleases/Releases?archived=true'], 'true']);
});
