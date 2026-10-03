'use strict';

// One fixed instant and the arithmetic around it, shared by every test that feeds an age to a
// page helper. Both pages compute an age as `now - checkedAt`, so the tests only ever need a
// stable `now` and a way to say "this long before it". It never moves, so no assertion can depend
// on when the suite runs. It sits 8 h 45 min after the page fixtures' `releasesLastCheckedAt`
// (2026-09-19T03:15Z), inside one 24 h interval, so a render of those fixtures takes the interval
// rule, not the clock-correction clamp that an instant later than `now` would take.

const HOUR = 3600 * 1000;
const DAY = 24 * HOUR;
const NOW = Date.parse('2026-09-19T12:00:00Z');

/** An ISO instant `ms` milliseconds before NOW. */
const ago = ms => new Date(NOW - ms).toISOString();

/** An ISO instant `ms` milliseconds after NOW — only a server clock correction produces one. */
const ahead = ms => new Date(NOW + ms).toISOString();

module.exports = { HOUR, DAY, NOW, ago, ahead };
