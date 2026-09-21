'use strict';

const fs = require('node:fs');
const path = require('node:path');

// A stand-in for the page's surroundings, written here because FR-017 forbids a third-party
// library and 002 recorded that constraint deliberately. See
// specs/005-page-json-casing/contracts/page-sandbox.md for the full statement of what it covers.
//
// It captures what a page WRITES as a string. The pages only ever write markup, so nothing here
// parses any. A test asserting on `panel.innerHTML` proves what the page wrote, never what a
// browser would render from it.

/** One element. Every write is a plain property the test can read back. */
class FakeElement {
    constructor(id) {
        this.id = id;
        this.innerHTML = '';
        this.textContent = '';
        this.hidden = false;
        this.value = '';
        this.checked = false;
        this.dataset = {};
        this.attributes = {};
        this.children = [];
        this.listeners = {};
        this.owned = new Map();
        this.owner = null;
    }

    setAttribute(name, value) { this.attributes[name] = String(value); }

    getAttribute(name) { return Object.hasOwn(this.attributes, name) ? this.attributes[name] : null; }

    /** The only position the pages use. `beforeend` is an append, so the string grows. */
    insertAdjacentHTML(position, html) {
        if (position !== 'beforeend') {
            throw new Error(`fake-dom: insertAdjacentHTML('${position}') is not modelled; only 'beforeend' is.`);
        }

        this.innerHTML += html;
    }

    addEventListener(type, handler) { (this.listeners[type] ||= []).push(handler); }

    appendChild(child) { this.children.push(child); return child; }

    /**
     * `#id` resolves through the document that owns this element; anything else is treated as a
     * child this element owns, created once and reused. There is no tag or class matching here:
     * `querySelector('tbody')` answers "the tbody of this element", which is all the pages ask.
     */
    querySelector(selector) {
        if (selector.startsWith('#')) { return this.owner ? this.owner(selector.slice(1)) : null; }
        if (!this.owned.has(selector)) { this.owned.set(selector, new FakeElement(selector)); }
        return this.owned.get(selector);
    }

    /** Always empty: no test below needs a descendant list, and guessing one would mislead. */
    querySelectorAll() { return []; }
}

const WEB_DIR = path.join(__dirname, '..', '..', 'src', 'Jellyfin.Plugin.NewReleases', 'Web');

/**
 * The element ids a page's own markup declares. Modelling exactly these is what makes a page
 * reaching for something it does not ship fail loudly rather than silently write into a phantom.
 * Ids built at runtime inside a JS string (`id="' + item.id + '"`) are not declarations and are
 * excluded by the identifier shape.
 */
function declaredIds(fileName) {
    const html = fs.readFileSync(path.join(WEB_DIR, fileName), 'utf8');
    return new Set([...html.matchAll(/\sid="([A-Za-z][\w-]*)"/g)].map(m => m[1]));
}

/** A fake `document` modelling one page's declared elements and nothing else. */
function documentFor(fileName) {
    const ids = declaredIds(fileName);
    const byId = new Map();
    const element = id => {
        if (!ids.has(id)) { return null; }
        if (!byId.has(id)) {
            const created = new FakeElement(id);
            created.owner = element;
            byId.set(id, created);
        }

        return byId.get(id);
    };

    return {
        getElementById: element,
        querySelector: selector => (selector.startsWith('#') ? element(selector.slice(1)) : null),
        querySelectorAll: () => [],
        createElement: tag => new FakeElement(tag),
    };
}

module.exports = { documentFor, FakeElement };
