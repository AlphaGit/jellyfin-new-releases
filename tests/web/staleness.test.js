'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const { loadPage } = require('./load-page.js');
const { HOUR, DAY, NOW, ago, ahead } = require('./fixed-clock.js');

// The twin of this file is `checked.test.js`: `admin.html` states the same ladder for its own
// operational view. Change one and you must change the other.

const { stalenessText } = loadPage('user-view.html');

const INTERVAL_HOURS = 24;

// 002 FR-006/FR-008/FR-010: when there is a sentence at all.

test('no instant yields no sentence', () => {
    assert.equal(stalenessText(null, NOW, INTERVAL_HOURS), null);
    assert.equal(stalenessText(undefined, NOW, INTERVAL_HOURS), null);
});

test('an instant later than now counts as an age of zero, so no sentence', () => {
    assert.equal(stalenessText(ahead(5 * HOUR), NOW, INTERVAL_HOURS), null);
});

test('an age of exactly one refresh interval yields no sentence', () => {
    assert.equal(stalenessText(ago(INTERVAL_HOURS * HOUR), NOW, INTERVAL_HOURS), null);
});

test('an age one second past the interval yields a sentence', () => {
    assert.equal(stalenessText(ago(INTERVAL_HOURS * HOUR + 1000), NOW, INTERVAL_HOURS), 'Releases last checked 24 hours ago.');
});

// 002 FR-012 / SC-007: the unit ladder. Each unit gives way at two of the next, so every
// changeover is asserted on both sides — a threshold with one test pins nothing.

const ladder = [
    ['47 hours renders in hours', 47 * HOUR, '47 hours'],
    ['48 hours renders in days', 48 * HOUR, '2 days'],
    ['13 days renders in days', 13 * DAY, '13 days'],
    ['14 days renders in weeks', 14 * DAY, '2 weeks'],
    ['60 days renders in weeks', 60 * DAY, '8 weeks'],
    ['61 days renders in months', 61 * DAY, '2 months'],
    ['364 days renders in months', 364 * DAY, '11 months'],
];

for (const [name, age, expected] of ladder) {
    test(name, () => {
        assert.equal(stalenessText(ago(age), NOW, INTERVAL_HOURS), `Releases last checked ${expected} ago.`);
    });
}

test('365 days renders as over a year, not a count', () => {
    assert.equal(stalenessText(ago(365 * DAY), NOW, INTERVAL_HOURS), 'Releases last checked over a year ago.');
    assert.equal(stalenessText(ago(700 * DAY), NOW, INTERVAL_HOURS), 'Releases last checked over a year ago.');
});

// 002 FR-007 / SC-004: the sentence is about the releases, never about the refresh job.

test('the sentence names the releases and no word for the refresh job', () => {
    const sentence = stalenessText(ago(3 * DAY), NOW, INTERVAL_HOURS);

    // The equality carries the requirement: no word for the refresh job can be in the sentence
    // if the sentence is exactly this. A separate loop over "refresh"/"run"/"scan"/"update"
    // could never fail on its own.
    assert.equal(sentence, 'Releases last checked 3 days ago.');
});
