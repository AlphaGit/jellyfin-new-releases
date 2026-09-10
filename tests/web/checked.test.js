'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const { loadPage } = require('./load-page.js');
const { HOUR, DAY, NOW, ago, ahead } = require('./fixed-clock.js');

// 002 T029 (FR-009, FR-010, contracts/staleness-line.md "Administrator page"): the administrator
// page states the data age beside the last run, in the same units as the New Releases page. It is
// an operational view, so it shows the age whenever the instant is known, not only when stale.
//
// `checkedText` is the twin of `stalenessText` in `staleness.test.js`: the same ladder, stated
// twice because the two pages are separate embedded resources with no way to share code. Change
// one ladder and you must change the other; these two files sit side by side so that is visible.

const { checkedText } = loadPage('admin.html');

test('no instant yields a dash, not a sentence', () => {
    assert.equal(checkedText(null, NOW), '–');
});

// 002 FR-010: a stated age is never in the future. Only a server clock correction can leave the
// stored instant later than now. `stalenessText` needs no test of its own clamp, because FR-006
// hides its line below one refresh interval; this view has no such gate and shows the age whenever
// the instant is known, so the clamp is the only thing holding the rule here.
test('an instant later than now counts as the present moment, never a future age', () => {
    assert.equal(checkedText(ahead(5 * HOUR), NOW), 'Releases last checked 0 hours ago.');
});

// Each unit gives way at two of the next, so every changeover is asserted on both sides. A
// threshold with one test pins nothing. This view differs from the New Releases page in one way
// only — it shows an age under an hour, which that page never reaches, so the bottom rung is
// asserted here and not there.

const ladder = [
    ['under an hour renders as zero hours, not "this hour"', 0, '0 hours'],
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
        assert.equal(checkedText(ago(age), NOW), `Releases last checked ${expected} ago.`);
    });
}

test('365 days renders as over a year, not a count', () => {
    assert.equal(checkedText(ago(365 * DAY), NOW), 'Releases last checked over a year ago.');
    assert.equal(checkedText(ago(700 * DAY), NOW), 'Releases last checked over a year ago.');
});
