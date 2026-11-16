-- Unfollow artist (spec 011): an artist-level, server-wide decision. Keyed by artist_key like decision, with no foreign key,
-- so a row outlives the artist's presence in the library, Purge release data and Clear Archive. Adds a table only; no row is rewritten.

CREATE TABLE unfollowed_artist (
    artist_key TEXT PRIMARY KEY,
    user_id    TEXT NOT NULL,
    decided_at TEXT NOT NULL
);
