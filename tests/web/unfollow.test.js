'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const { loadPageDom, settled, renderedList } = require('./load-page.js');
const { FakeElement } = require('./fake-dom.js');
const { fixture } = require('./fixtures.js');

// Spec 011: the artist-level Unfollow and Follow button, the Unfollowed artists tab, and the rule that
// the artist button is never mistaken for, or handled as, a Release action.

const RELEASES = fixture('releases.json');
const ARTISTS = fixture('unfollowed-artists.json');

/** Loads the view with an `ApiClient` that records each request and answers `answer(url)`. */
async function loadedView(answer = () => ({ items: [], hasStoredReleases: true }), overrides = {}) {
    const requests = [];
    const loaded = loadPageDom('user-view.html', {
        ApiClient: { ajax: options => { requests.push(options.type + ' ' + options.url); return Promise.resolve(answer(options.url)); } },
        ...overrides,
    });
    await settled();
    return { requests, ...loaded };
}

/** The artist button of a row as a browser builds it from the markup the view writes: a button carrying `data-artist-*`. */
function artistButton({ id = '6f3a9c1d7b0e4f2a8c5d1e9b3a7f6c04', name = 'Chromatics', action = 'Unfollow' } = {}) {
    const button = new FakeElement('artist-button');
    button.tagName = 'BUTTON';
    button.setAttribute('data-artist-action', action);
    Object.assign(button.dataset, { artistAction: action, artistId: id, artistName: name });
    return button;
}

/** The `.nr-artist` line of the row with `id` (up to the next `nr-meta` block). */
const artistLineOf = (panel, id) => new RegExp(`data-id="${id}"[\\s\\S]*?<div class="nr-artist">([\\s\\S]*?)</div>`).exec(panel)[1];

/** The `.nr-actions` block of the row with `id`. */
const actionsOf = (panel, id) => new RegExp(`data-id="${id}"[\\s\\S]*?<div class="nr-actions">([\\s\\S]*?)</div></article>`).exec(panel)[1];

test('011 FR-002, FR-003: every row of the List, Want it and Archive tabs has the artist button in the artist line, apart from the Release actions', () => {
    for (const options of [{}, { wanted: true }, { archive: true }]) {
        const panel = renderedList(RELEASES, options).panel;
        for (const id of [101, 102, 103, 104]) {
            const line = artistLineOf(panel, id);
            const actions = actionsOf(panel, id);

            assert.match(line, /<button [^>]*class="nr-artist-action"[^>]*data-artist-action="(Unfollow|Follow)"/);
            assert.doesNotMatch(line, /data-action=/);
            assert.doesNotMatch(actions, /data-artist-action/);
            assert.doesNotMatch(actions, /Unfollow|Follow/);
        }
    }
});

test('011 FR-013: the button reads Unfollow, or Follow when the artist is already unfollowed, and its label names the artist', () => {
    const panel = renderedList(RELEASES, { archive: true }).panel; // fixture: row 104 (Desire) is unfollowed, the others are not

    assert.match(artistLineOf(panel, 104), /data-artist-action="Follow"[^>]*aria-label="Follow Desire">Follow</);
    assert.match(artistLineOf(panel, 101), /data-artist-action="Unfollow"[^>]*aria-label="Unfollow Chromatics">Unfollow</);
    assert.match(artistLineOf(panel, 101), /data-artist-id="6f3a9c1d7b0e4f2a8c5d1e9b3a7f6c04"/);
});

for (const action of ['Unfollow', 'Follow']) {
    test(`011 FR-002: the ${action} button posts ${action} for its artist at once, with no confirmation, then reloads and announces it`, async () => {
        let confirmed = 0;
        const { requests, document } = await loadedView(undefined, { Dashboard: { confirm: () => { confirmed++; return Promise.resolve(true); } } });
        const before = requests.length;

        document.getElementById('nr-panel').listeners.click[0]({ target: artistButton({ action }) });
        await settled();

        assert.deepEqual(requests.slice(before), [
            `POST Plugins/NewReleases/Artists/6f3a9c1d7b0e4f2a8c5d1e9b3a7f6c04/${action}`,
            'GET Plugins/NewReleases/Releases',
        ]);
        assert.equal(confirmed, 0);
        assert.equal(document.getElementById('nr-announce').textContent, (action === 'Unfollow' ? 'Unfollowed' : 'Followed') + ' Chromatics');
    });
}

test('011: a failed Unfollow re-enables the button and says so', async () => {
    const requests = [];
    const { document } = loadPageDom('user-view.html', {
        ApiClient: { ajax: options => { requests.push(options.type); return options.type === 'POST' ? Promise.reject(new Error('offline')) : Promise.resolve({ items: [], hasStoredReleases: true }); } },
    });
    await settled();
    const button = artistButton();

    document.getElementById('nr-panel').listeners.click[0]({ target: button });
    await settled();

    assert.equal(button.disabled, false);
    assert.equal(document.getElementById('nr-announce').textContent, 'Could not update Chromatics');
});

test('011: the artist button is not a Release action: a Release click handler cannot be reached by it', async () => {
    const { requests, document } = await loadedView();
    const before = requests.length;

    document.getElementById('nr-panel').listeners.click[0]({ target: artistButton() });
    await settled();

    assert.equal(requests.slice(before).filter(request => request.includes('/Releases/')).length, 0);
});

test('011 FR-010: the Unfollowed artists tab asks for Artists/Unfollowed, lists name and Follow, and hides the filters', async () => {
    const { requests, document } = await loadedView(url => (url.endsWith('Artists/Unfollowed') ? ARTISTS : { items: [], hasStoredReleases: true }));
    const before = requests.length;

    document.getElementById('nr-tab-unfollowed').listeners.click[0]();
    await settled();

    const panel = document.getElementById('nr-panel').innerHTML;
    assert.deepEqual(requests.slice(before), ['GET Plugins/NewReleases/Artists/Unfollowed']);
    assert.deepEqual([...panel.matchAll(/<a href="[^"]*">([^<]*)<\/a>/g)].map(m => m[1]), ['Chromatics', 'Desire']);
    assert.equal([...panel.matchAll(/data-artist-action="Follow"[^>]*>Follow</g)].length, 2);
    assert.doesNotMatch(panel, /Unfollow|data-action=/);
    assert.equal(document.getElementById('nr-filters').hidden, true);
    assert.equal(document.getElementById('nr-tab-unfollowed').getAttribute('aria-selected'), 'true');
    assert.equal(document.getElementById('nr-panel').getAttribute('aria-labelledby'), 'nr-tab-unfollowed');

    document.getElementById('nr-tab-list').listeners.click[0]();
    await settled();

    assert.equal(document.getElementById('nr-filters').hidden, false);
});

test('011 FR-012: with no unfollowed artist the tab explains how an artist gets there', async () => {
    const { document } = await loadedView(url => (url.endsWith('Artists/Unfollowed') ? { items: [] } : { items: [], hasStoredReleases: true }));

    document.getElementById('nr-tab-unfollowed').listeners.click[0]();
    await settled();

    assert.match(document.getElementById('nr-panel').innerHTML, /No unfollowed artists\. Choose Unfollow next to an artist name to add one\./);
});

test('011: Follow in the Unfollowed artists tab posts Follow, reloads that tab and stays on it', async () => {
    const { requests, document } = await loadedView(url => (url.endsWith('Artists/Unfollowed') ? ARTISTS : { items: [], hasStoredReleases: true }));
    document.getElementById('nr-tab-unfollowed').listeners.click[0]();
    await settled();
    const before = requests.length;

    document.getElementById('nr-panel').listeners.click[0]({ target: artistButton({ id: 'b21e7d4a9f3c48d6a0e5b8c2f7d1a930', name: 'Desire', action: 'Follow' }) });
    await settled();

    assert.deepEqual(requests.slice(before), ['POST Plugins/NewReleases/Artists/b21e7d4a9f3c48d6a0e5b8c2f7d1a930/Follow', 'GET Plugins/NewReleases/Artists/Unfollowed']);
    assert.equal(document.getElementById('nr-tab-unfollowed').getAttribute('aria-selected'), 'true');
    assert.equal(document.getElementById('nr-announce').textContent, 'Followed Desire');
});

test('011 FR-016: the Artist filter still takes its suggestions from every library artist', async () => {
    const { requests } = await loadedView();

    assert.equal(requests.filter(request => request === 'GET Plugins/NewReleases/Artists').length, 1);
    assert.equal(requests.filter(request => request.includes('Artists/Unfollowed')).length, 0);
});

// 011 FR-018: the admin button follows every artist again, behind the same confirmation as the other destructive actions.
test('011: Clear unfollowed artists asks for confirmation and sends nothing when the administrator declines', async () => {
    const requests = [];
    const { document } = loadPageDom('admin.html', {
        ApiClient: { ajax: options => { requests.push(options.type + ' ' + options.url); return Promise.resolve({}); } },
        Dashboard: { confirm: (text, title, answer) => answer(false) },
    });

    document.getElementById('nr-clear-unfollowed').listeners.click[0]();
    await settled();

    assert.deepEqual(requests.filter(request => request.startsWith('POST')), []);
});

test('011: the confirmation names what Clear unfollowed artists does', async () => {
    let asked;
    const { document } = loadPageDom('admin.html', { Dashboard: { confirm: (text, title, answer) => { asked = [text, title]; answer(false); } } });

    document.getElementById('nr-clear-unfollowed').listeners.click[0]();
    await settled();

    assert.deepEqual(asked, ['Follow every unfollowed artist again? Their Releases show in the list again.', 'Clear unfollowed artists']);
});
