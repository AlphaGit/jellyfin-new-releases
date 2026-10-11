-- Fix four refresh defects (specs/009-fix-refresh-defects/data-model.md): the release's album-artist
-- credit at that source, as a JSON array of { "mbid", "name" }. MusicBrainz fills it; Deezer's album
-- lists carry no artist, so its entries stay empty, as does every row written before this migration.

ALTER TABLE source_entry ADD COLUMN credit TEXT NOT NULL DEFAULT '[]';
