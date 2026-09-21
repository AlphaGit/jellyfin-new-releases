# Contract: the page-test stand-in, and what it does not cover

`FR-017` requires a stand-in for the page's surroundings, written in this repository with no
third-party library. `FR-018` requires its partiality to be recorded, so a later author does not
read a passing test as proof of something it never exercised. This is that record.

---

## What it is

An extension of `tests/web/load-page.js`: a fake DOM whose elements **capture what the page writes
as a string**. The pages only write markup; nothing under test reads back parsed structure.
Assertions match against the captured string.

**There is no HTML parser, and none is to be added.**

## What it supports

| Surface | Behaviour |
| --- | --- |
| `document.getElementById`, `document.querySelector` | returns a fake element for a known selector, `null` otherwise |
| `document.createElement` | a fake element with `value`, `textContent` |
| `element.querySelector`, `element.querySelectorAll` | same lookup, scoped; `querySelectorAll` returns an array |
| `element.innerHTML`, `element.textContent` | plain string properties, last write wins |
| `element.insertAdjacentHTML('beforeend', …)` | appends to `innerHTML` |
| `element.appendChild` | records the child |
| `element.setAttribute`, `element.hidden`, `element.dataset` | plain property stores |
| `element.addEventListener` | recorded, never fired |

## What it does **not** cover

- **No parsing.** `panel.innerHTML` is the string the page wrote. A test asserting on it proves what
  the page *wrote*, never what a browser would *render*. Malformed markup passes.
- **No events.** Listeners are recorded and never invoked, so tab switching, filter changes, form
  submission and the Ignore / Have it / Restore buttons are not exercised here.
- **No traversal.** `closest`, `parentNode` and sibling access are absent. The click handler in
  `user-view.html` depends on `closest` and stays outside this stand-in.
- **No layout, no CSS, no focus, no accessibility tree.** Nothing about how the page looks or reads
  to assistive technology is asserted by any test using this fake.
- **No network.** `ApiClient.ajax` is already stubbed by `load-page.js` and stays stubbed.

## What it is for

Exactly two behaviours, and it is extended no further than they need:

1. `render(data)` on `user-view.html` — the list-versus-empty-state decision and the field reads
   inside a row (`FR-008`, `US3-AS3`).
2. `renderStatus(status)` on `admin.html` — every status value reading a real value rather than a
   dash (`US2-AS1`, `US2-AS2`, `US2-AS3`).

`row`, `refreshStatus`, `read`, `fill` and `query` stay outside it unless the work happens to reach
them, as `spec.md`'s Out of Scope states.

## Consequence for `exposure.test.js`

Both pages must expose `render` / `renderStatus` on `NewReleasesInternals` for a test to reach them.
`tests/web/exposure.test.js` asserts that set exactly and **will fail**, by design. Each newly
exposed function arrives with a behaviour on `specs/005-page-json-casing/tdd/test-list.md` and a
test of its own — the rule `exposure.test.js` states in its own header.
