'use strict';

// Loads an embedded page's script into a sandbox so its pure helpers can be tested
// without a browser. Standard library only: no package.json, no install step (FR-014).
//
// Each page assigns its helpers to `NewReleasesInternals` as the first statement of its
// IIFE, before it touches the DOM. Function declarations hoist, so the assignment sees
// them all. Everything after that point wires the page to elements that do not exist
// here and is expected to fail; the catch below is deliberate, not defensive.

const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

const WEB_DIR = path.join(__dirname, '..', '..', 'src', 'Jellyfin.Plugin.NewReleases', 'Web');

/** The one `<script>` body in a page. Two would mean the page changed shape; fail loudly. */
function extractScript(html, fileName) {
    const blocks = [...html.matchAll(/<script[^>]*>([\s\S]*?)<\/script>/g)].map(m => m[1]);
    if (blocks.length !== 1) {
        throw new Error(`${fileName}: expected exactly one <script> block, found ${blocks.length}`);
    }

    return blocks[0];
}

/**
 * The browser and Jellyfin globals the pages reach for. Every element lookup answers null.
 * `overrides` merges one level down, so a test that pins `ApiClient.serverId` keeps the rest of
 * `ApiClient` rather than restating it.
 */
function sandboxGlobals(overrides) {
    const none = () => null;
    const defaults = {
        console,
        document: { getElementById: none, querySelector: none, querySelectorAll: () => [] },
        ApiClient: {
            ajax: () => Promise.resolve({}),
            getUrl: p => p,
            serverId: () => 'server-1',
            getPluginConfiguration: () => Promise.resolve({}),
            updatePluginConfiguration: () => Promise.resolve({}),
        },
        Dashboard: { alert: () => {}, confirm: () => Promise.resolve(true), processPluginConfigurationUpdateResult: () => {} },
        // The pages pass `undefined` as the locale on purpose, so a Jellyfin user reads the
        // sentence in their own language. That makes the runtime's ambient locale an input, and
        // the assertions are English: without this the suite is green on an English machine and
        // red on any other. Pinned here, in the harness, so production keeps its behaviour.
        // `Object.create` rather than a spread: Intl's constructors are non-enumerable.
        Intl: Object.create(Intl, {
            RelativeTimeFormat: {
                value: function (locale, options) { return new Intl.RelativeTimeFormat(locale ?? 'en', options); },
            },
        }),
        // Same reason, second channel: `when()` in admin.html calls `Date.prototype.toLocaleString()`,
        // which reads the machine's locale *and* its timezone. Both are pinned here so a test can
        // assert the exact sentence; production still passes nothing and renders in the user's own
        // format. Everything else on Date is inherited.
        Date: class extends Date {
            toLocaleString(locale, options) {
                return super.toLocaleString(locale ?? 'en-US', { timeZone: 'UTC', ...options });
            }
        },
    };

    const merged = { ...defaults };
    for (const [name, value] of Object.entries(overrides)) {
        const base = defaults[name];
        const mergeable = base && value && typeof base === 'object' && typeof value === 'object';
        merged[name] = mergeable ? { ...base, ...value } : value;
    }

    return merged;
}

/**
 * Returns the helpers `fileName` exposes. `overrides` merges into the sandbox globals, so a test
 * can pin what `artistLink` reads from `ApiClient` without restating the rest of it.
 */
function loadPage(fileName, overrides = {}) {
    const html = fs.readFileSync(path.join(WEB_DIR, fileName), 'utf8');
    const sandbox = sandboxGlobals(overrides);

    try {
        vm.runInNewContext(extractScript(html, fileName), sandbox, { filename: fileName });
    } catch (error) {
        if (!sandbox.NewReleasesInternals) throw error; // failed before exposing: a real problem
    }

    if (!sandbox.NewReleasesInternals) {
        throw new Error(`${fileName} exposed no NewReleasesInternals; see tests/web/load-page.js`);
    }

    return sandbox.NewReleasesInternals;
}

module.exports = { loadPage };
