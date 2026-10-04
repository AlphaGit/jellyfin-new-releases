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
        this.listenerOptions = {};
        this.owned = new Map();
        this.owner = null;
        this.parentNode = null;
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

    /** `options` (a capture flag or an options object) is kept beside the handler, at the same index. */
    addEventListener(type, handler, options) {
        (this.listeners[type] ||= []).push(handler);
        (this.listenerOptions[type] ||= []).push(options);
    }

    appendChild(child) { this.children.push(child); child.parentNode = this; return child; }

    /** Detaches this element from the element it was appended to; a no-op when it has none. */
    remove() {
        if (this.parentNode) { this.parentNode.children.splice(this.parentNode.children.indexOf(this), 1); }
        this.parentNode = null;
    }

    /**
     * The nearest of this element and the elements it was appended to that `selector` names. A selector is
     * one compound of a tag, classes and attribute names (`button[data-action]`, `.nr-row`); anything else
     * throws. The tag is the `tagName` a test gives the element, the classes its `class` attribute.
     */
    closest(selector) {
        const parts = /^([a-z]*)((?:\.[\w-]+)*)((?:\[[\w-]+\])*)$/i.exec(selector);
        if (!parts || !selector) { throw new Error(`fake-dom: closest('${selector}') is not modelled; only one compound of a tag, classes and attributes is.`); }
        const [, tag, classes, attributes] = parts;
        const matches = element => (!tag || (element.tagName || '').toLowerCase() === tag.toLowerCase())
            && classes.split('.').filter(Boolean).every(name => (element.getAttribute('class') || '').split(/\s+/).includes(name))
            && [...attributes.matchAll(/\[([\w-]+)\]/g)].every(([, name]) => element.getAttribute(name) !== null);
        for (let element = this; element; element = element.parentNode) {
            if (matches(element)) { return element; }
        }
        return null;
    }

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

/**
 * A row's elements as a browser builds them from the markup the New Releases view writes,
 * `article.nr-row > div.nr-actions > button[data-action]`, joined by `appendChild` so `closest` can walk them.
 */
function actionRow({ id = '101', title = 'Closer to Grey', action = 'Ignore' } = {}) {
    const article = new FakeElement('row');
    article.tagName = 'ARTICLE';
    article.setAttribute('class', 'nr-row');
    Object.assign(article.dataset, { id, title });
    const actions = article.appendChild(new FakeElement('actions'));
    actions.tagName = 'DIV';
    actions.setAttribute('class', 'nr-actions');
    const button = actions.appendChild(new FakeElement('button'));
    button.tagName = 'BUTTON';
    button.setAttribute('data-action', action);
    button.dataset.action = action;
    return { article, actions, button };
}

/**
 * Whether a listener registered with `options` hears every event, as the stand-in records them: no
 * options, a capture flag, or an options object with neither `once` nor a `signal`.
 */
function keepsListening(options) {
    return options === null || typeof options !== 'object' || (!options.once && options.signal === undefined);
}

module.exports = { documentFor, FakeElement, declaredIds, actionRow, keepsListening };
