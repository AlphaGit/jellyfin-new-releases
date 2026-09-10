'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const { loadPage } = require('./load-page.js');

// Characterization (002 US3-AS2): helpers this feature does not change, pinned so the rename
// that follows, and anything later, cannot break them silently.

const { groupOf, artistLink } = loadPage('user-view.html', {
    ApiClient: { serverId: () => 'srv-42', ajax: () => Promise.resolve({}), getUrl: p => p },
});
const { healthText } = loadPage('admin.html');

test('groupOf buckets by Upcoming, then undated, then year', () => {
    assert.equal(groupOf({ state: 'Upcoming', date: '2027-01-01' }), 'Upcoming');
    assert.equal(groupOf({ state: 'Missing', date: null }), 'Undated');
    assert.equal(groupOf({ state: 'Missing', date: '2013-05-17' }), '2013');
    assert.equal(groupOf({ state: 'Incomplete', date: '2013' }), '2013');
});

test('artistLink builds the Jellyfin artist deep link, escaping both ids', () => {
    assert.equal(
        artistLink({ artistJellyfinId: 'a b&c' }),
        '#/details?id=a%20b%26c&serverId=srv-42');
});

test('healthText names the health, the reason when there is one, and the day\'s requests', () => {
    const base = { callsToday: 3, dailyBudget: 10000, cooldownUntil: null, lastError: null };

    assert.equal(healthText({ ...base, health: 'Ok' }), 'Ok · 3 / 10000 requests today');
    assert.equal(
        healthText({ ...base, health: 'Failing', lastError: '503 Service Unavailable' }),
        'Failing — 503 Service Unavailable · 3 / 10000 requests today');
    assert.equal(
        healthText({ ...base, health: 'Disabled', lastError: 'ignored while disabled' }),
        'Disabled · 3 / 10000 requests today');
    assert.match(healthText({ ...base, health: 'CoolingDown', cooldownUntil: '2026-09-06T18:00:00Z' }), /^CoolingDown until .+ · 3 \/ 10000 requests today$/);
});
