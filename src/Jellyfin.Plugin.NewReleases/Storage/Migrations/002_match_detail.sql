-- Explain each unmatched artist step by step (specs/008-unmatched-artist-reasons/data-model.md):
-- the candidates and counts of an artist's last failed match at a source, as JSON. Null for a match
-- and for every row written before this migration.

ALTER TABLE artist_source ADD COLUMN match_detail TEXT;
