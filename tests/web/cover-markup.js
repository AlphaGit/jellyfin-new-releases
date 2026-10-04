'use strict';

// A release row and its cover box (007 US2), read from the panel's markup as a string. Shared by
// render.test.js, which checks what a row writes (its cover, links, buttons and badges), and
// cover-fallback.test.js, which builds the `<img>` a browser would hold from it, so both read the
// markup by one rule.

/** The markup of the row with `id` in `panel`, from its `<article`, or an empty string when there is none. */
function rowOf(panel, id) {
    return panel.split('<article').find(r => r.includes('data-id="' + id + '"')) || '';
}

/** The markup inside the `nr-cover` box of the row with `id` in `panel`, or null when the row has no box. */
function coverBox(panel, id) {
    const box = /<div class="nr-cover">(.*?)<\/div>/.exec(rowOf(panel, id));
    return box ? box[1] : null;
}

/** The raw (still escaped) value of `name` on the first `<img>` in `markup`, or undefined. Names ignore case and the first of two duplicates wins, as in a browser. */
function imgAttribute(markup, name) {
    const img = /<img [^>]*>/i.exec(markup || '');
    const attr = img && new RegExp('\\s' + name + '="([^"]*)"', 'i').exec(img[0]);
    return attr ? attr[1] : undefined;
}

/** `value` with the entities the page's `esc` writes turned back into text, as a browser reads an attribute. */
function decoded(value) {
    return value === undefined ? undefined : value
        .replace(/&quot;/g, '"').replace(/&#39;/g, "'").replace(/&lt;/g, '<').replace(/&gt;/g, '>').replace(/&amp;/g, '&');
}

module.exports = { rowOf, coverBox, imgAttribute, decoded };
