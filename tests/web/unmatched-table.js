'use strict';

const { loadPageDom } = require('./load-page.js');

// 008: readers for the administrator page's Unmatched artists table, shared by the page's unit
// tests (render-status.test.js) and its acceptance tests (unmatched-acceptance.test.js).

/** Renders one admin status through admin.html and returns the unmatched rows, one string of cell markup per row. */
function unmatchedRows(status) {
    const { internals, document } = loadPageDom('admin.html');
    internals.renderStatus(status);
    return document.getElementById('nr-unmatched').querySelector('tbody').innerHTML.split('</tr>').filter(r => r.includes('<tr>'));
}

/** A row's Reason lines as markup, one per source. */
function reasonMarkup(row) {
    return [...row.matchAll(/<div class="nr-reason">([\s\S]*?)<\/div>/g)].map(m => m[1]);
}

/** A row's fix paragraphs as text, markup removed. */
function fixes(row) {
    return [...row.matchAll(/<p class="nr-fix">([\s\S]*?)<\/p>/g)].map(m => m[1].replace(/<[^>]*>/g, ''));
}

/** A trail line as a sighted reader sees it: screen-reader words removed, then every tag. */
const visible = line => line.replace(/<span class="nr-sr">[^<]*<\/span>/g, '').replace(/<[^>]*>/g, '');

module.exports = { unmatchedRows, reasonMarkup, fixes, visible };
