'use strict';

// One fixed instant and the arithmetic around it, shared by every test that feeds an age to a
// page helper. Both pages compute an age as `now - checkedAt`, so the tests only ever need a
// stable `now` and a way to say "this long before it". The instant itself is arbitrary; what
// matters is that it never moves, so no assertion can depend on when the suite runs.

const HOUR = 3600 * 1000;
const DAY = 24 * HOUR;
const NOW = Date.parse('2026-09-09T12:00:00Z');

/** An ISO instant `ms` milliseconds before NOW. */
const ago = ms => new Date(NOW - ms).toISOString();

/** An ISO instant `ms` milliseconds after NOW — only a server clock correction produces one. */
const ahead = ms => new Date(NOW + ms).toISOString();

module.exports = { HOUR, DAY, NOW, ago, ahead };
