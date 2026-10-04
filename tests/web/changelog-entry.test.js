'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { entryFor, withChangelog } = require('../../.github/scripts/changelog-entry.js');

// 003 T064: the catalogue text JPRM publishes is generated at release from CHANGELOG.md, so the
// two can no longer drift. These pin the extraction the release workflow depends on.

const CHANGELOG = [
    '# Changelog',
    '',
    '## 0.1.10 — 2026-11-01',
    '',
    '- Ten.',
    '',
    '## 0.1.1 — 2026-09-20',
    '',
    '### Fixed',
    '',
    '- One.',
    '',
    '## 0.1.0 — 2026-09-20',
    '',
    '- Zero.',
    '',
].join('\n');

test('the entry is the body of the version\'s own section, up to the next one', () => {
    assert.equal(entryFor(CHANGELOG, '0.1.1'), '### Fixed\n\n- One.');
});

// A release must not publish an empty catalogue entry, so a version with no section stops it.
test('a version with no section is an error, not an empty entry', () => {
    assert.throws(() => entryFor(CHANGELOG, '0.2.0'), /CHANGELOG\.md has no section for 0\.2\.0/);
});

test('a version whose section is empty is an error, not an empty entry', () => {
    const empty = '## 0.3.0 — 2026-12-01\n\n## 0.2.0 — 2026-11-15\n\n- Two.\n';
    assert.throws(() => entryFor(empty, '0.3.0'), /CHANGELOG\.md has an empty section for 0\.3\.0/);
});

// 003 T063: the next release cannot ship a placeholder or an empty catalogue entry. The version
// build.yaml declares is the one the next tag publishes, and CHANGELOG.md must already describe it.
test('CHANGELOG.md has an entry for the version build.yaml declares', () => {
    const root = path.join(__dirname, '..', '..');
    const version = /^version:\s*"([^"]+)"/m.exec(fs.readFileSync(path.join(root, 'build.yaml'), 'utf8'))[1];

    assert.doesNotThrow(() => entryFor(fs.readFileSync(path.join(root, 'CHANGELOG.md'), 'utf8'), version));
});

// JPRM reads build.yaml with a YAML parser and copies `changelog` into the published manifest. A
// JSON string is a valid YAML double-quoted scalar, so writing the entry as one keeps its line
// breaks, quotes and colons intact without a YAML library.
const BUILD_YAML = 'name: "New Releases"\nversion: "0.1.1"\nchangelog: "Initial scaffold."\nartifacts:\n  - "a.dll"\n';

test('the entry replaces build.yaml\'s changelog as one quoted scalar, and nothing else changes', () => {
    assert.equal(
        withChangelog(BUILD_YAML, '### Fixed\n\n- "One": done.'),
        'name: "New Releases"\nversion: "0.1.1"\nchangelog: "### Fixed\\n\\n- \\"One\\": done."\nartifacts:\n  - "a.dll"\n');
});

test('a build.yaml with no changelog line is an error, so the committed text is never published by accident', () => {
    assert.throws(() => withChangelog('version: "0.1.1"\n', '- One.'), /build\.yaml has no changelog line/);
});

// 006 T039: the release step writes the GitHub Release text through this script, so the text a
// release shows is the text the catalogue shows. Run as the workflow runs it, from a working
// directory holding a CHANGELOG.md and no build.yaml, so the order of its arguments is what is
// tested and the build.yaml path is not touched.
test('with --notes, the tagged version\'s section is written to the named file and build.yaml is not read', () => {
    const os = require('node:os');
    const { spawnSync } = require('node:child_process');
    const dir = fs.mkdtempSync(path.join(os.tmpdir(), 'changelog-notes-'));
    try {
        fs.writeFileSync(path.join(dir, 'CHANGELOG.md'), CHANGELOG);
        const script = path.join(__dirname, '..', '..', '.github', 'scripts', 'changelog-entry.js');

        const run = spawnSync(process.execPath, [script, '0.1.1', '--notes', 'notes.md'], { cwd: dir, encoding: 'utf8' });

        assert.equal(run.status, 0, run.stderr);
        assert.equal(fs.readFileSync(path.join(dir, 'notes.md'), 'utf8'), '### Fixed\n\n- One.');
    } finally {
        fs.rmSync(dir, { recursive: true, force: true });
    }
});

// 006 U22, U23 (FR-007): the renaming release's notes, read through the same entryFor the release
// and the catalogue publish them with, rather than through a copy of its rule (006 audit,
// findings 9 and 11).
const RAW_CATALOGUE = 'https://raw.githubusercontent.com/AlphaGit/jellyfin-new-releases/main/repo/manifest.json';
const section020 = () => entryFor(
    fs.readFileSync(path.join(__dirname, '..', '..', 'CHANGELOG.md'), 'utf8'),
    '0.2.0');

test('the 0.2.0 notes name the old-name folder, and say to remove it once and nothing else', () => {
    const notes = section020();

    assert.match(notes, /`Jellyfin New Releases_<version>`/);
    assert.match(notes, /\bonce\b/);
    assert.match(notes, /\bnothing else\b/);
});

test('the 0.2.0 notes give the raw catalogue address as the one that replaces the old address', () => {
    const notes = section020();

    assert.ok(notes.includes(RAW_CATALOGUE), `no ${RAW_CATALOGUE} in the 0.2.0 notes`);
    assert.match(notes, /\breplace\b/i);
});

// 006 U33: the removal step as one instruction — which folder, nothing else, and that no later
// upgrade needs it — so a heading that says "once" cannot stand in for it (006 third audit,
// finding 29).
test('the 0.2.0 removal instruction deletes the old-name folder and nothing else, and only this once', () => {
    assert.match(
        section020(),
        /delete\s+the `Jellyfin New Releases_<version>` folder and nothing else,[^.]*\.\s+No later\s+upgrade needs this\./);
});

// 006 U34: the replacement step as one instruction — the old address goes, the raw address takes
// its place — so the address merely appearing near the word "replace" cannot stand in for it.
test('the 0.2.0 replacement instruction swaps the old github.io address for the raw catalogue address', () => {
    const escaped = RAW_CATALOGUE.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');

    assert.match(section020(), new RegExp(`replace the old \`github\\.io\` address with\\s+\`${escaped}\``));
});
