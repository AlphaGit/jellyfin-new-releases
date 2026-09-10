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

/** The browser and Jellyfin globals the pages reach for. Every element lookup answers null. */
function sandboxGlobals(overrides) {
    const none = () => null;
    return Object.assign({
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
    }, overrides);
}

/**
 * Returns the helpers `fileName` exposes. `overrides` replaces sandbox globals, so a test
 * can pin what `artistLink` reads from `ApiClient`.
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
