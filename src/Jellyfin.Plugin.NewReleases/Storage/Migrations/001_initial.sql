-- Track New Releases: initial schema. Vocabulary and columns from specs/001-track-new-releases/data-model.md.
-- Timestamps are ISO-8601 UTC text; Jellyfin ids are GUID text. schema_version is created by PluginDatabase.

CREATE TABLE library_artist (
    id                INTEGER PRIMARY KEY,
    artist_key        TEXT    NOT NULL UNIQUE,
    jellyfin_id       TEXT    NOT NULL,
    name              TEXT    NOT NULL,
    mbid              TEXT,
    library_ids       TEXT    NOT NULL,
    album_count       INTEGER NOT NULL,
    last_refreshed_at TEXT
);
CREATE INDEX ix_library_artist_rotation ON library_artist (last_refreshed_at, name);

CREATE TABLE artist_source (
    library_artist_id INTEGER NOT NULL REFERENCES library_artist(id) ON DELETE CASCADE,
    source            TEXT    NOT NULL CHECK (source IN ('musicbrainz', 'deezer')),
    status            TEXT    NOT NULL CHECK (status IN ('Pending', 'Matched', 'Unmatched')),
    source_artist_id  TEXT,
    unmatched_reason  TEXT,
    resume_offset     INTEGER NOT NULL DEFAULT 0,
    pass_run_id       INTEGER,
    last_outcome      TEXT    CHECK (last_outcome IN ('Complete', 'Partial', 'Failed')),
    last_complete_at  TEXT,
    last_error        TEXT,
    PRIMARY KEY (library_artist_id, source)
);

CREATE TABLE release (
    id                   INTEGER PRIMARY KEY,
    library_artist_id    INTEGER NOT NULL REFERENCES library_artist(id) ON DELETE CASCADE,
    normalized_title     TEXT    NOT NULL,
    title                TEXT    NOT NULL,
    canonical_source     TEXT    NOT NULL CHECK (canonical_source IN ('musicbrainz', 'deezer')),
    canonical_source_id  TEXT    NOT NULL,
    primary_type         TEXT    NOT NULL CHECK (primary_type IN ('Album', 'EP', 'Single', 'Compilation', 'Other')),
    secondary_types      TEXT    NOT NULL,
    release_date         TEXT,
    date_sort            TEXT,
    first_seen_at        TEXT    NOT NULL,
    last_seen_at         TEXT    NOT NULL,
    ownership_state      TEXT    NOT NULL CHECK (ownership_state IN ('Missing', 'Incomplete', 'Owned')),
    match_method         TEXT    CHECK (match_method IN ('Identifier', 'Title')),
    library_album_id     TEXT,
    compared_edition_id  INTEGER REFERENCES edition(id) ON DELETE SET NULL,
    missing_tracks       TEXT    NOT NULL DEFAULT '[]',
    ownership_checked_at TEXT,
    UNIQUE (library_artist_id, normalized_title)
);
CREATE INDEX ix_release_artist ON release (library_artist_id);
CREATE INDEX ix_release_sort   ON release (date_sort DESC);

CREATE TABLE source_entry (
    release_id             INTEGER NOT NULL REFERENCES release(id) ON DELETE CASCADE,
    source                 TEXT    NOT NULL CHECK (source IN ('musicbrainz', 'deezer')),
    source_release_id      TEXT    NOT NULL,
    url                    TEXT    NOT NULL,
    source_title           TEXT    NOT NULL,
    source_primary_type    TEXT,
    source_secondary_types TEXT,
    source_date            TEXT,
    last_seen_run_id       INTEGER NOT NULL,
    PRIMARY KEY (release_id, source),
    UNIQUE (source, source_release_id)
);

CREATE TABLE edition (
    id                INTEGER PRIMARY KEY,
    release_id        INTEGER NOT NULL REFERENCES release(id) ON DELETE CASCADE,
    source            TEXT    NOT NULL CHECK (source IN ('musicbrainz', 'deezer')),
    source_edition_id TEXT    NOT NULL,
    title             TEXT    NOT NULL,
    status            TEXT    NOT NULL CHECK (status = 'Official'),
    tracks            TEXT    NOT NULL,
    fetched_at        TEXT    NOT NULL,
    UNIQUE (source, source_edition_id)
);

CREATE TABLE decision (
    artist_key       TEXT NOT NULL,
    normalized_title TEXT NOT NULL,
    kind             TEXT NOT NULL CHECK (kind IN ('Ignore', 'HaveIt')),
    user_id          TEXT NOT NULL,
    decided_at       TEXT NOT NULL,
    PRIMARY KEY (artist_key, normalized_title)
);

CREATE TABLE source_state (
    source               TEXT    PRIMARY KEY CHECK (source IN ('musicbrainz', 'deezer')),
    consecutive_failures INTEGER NOT NULL DEFAULT 0,
    cooldown_until       TEXT,
    calls_today          INTEGER NOT NULL DEFAULT 0,
    calls_day            TEXT,
    next_allowed_at      TEXT,
    last_error           TEXT,
    last_success_at      TEXT
);

CREATE TABLE refresh_run (
    id                INTEGER PRIMARY KEY,
    started_at        TEXT    NOT NULL,
    ended_at          TEXT,
    artists_processed INTEGER NOT NULL DEFAULT 0,
    releases_found    INTEGER NOT NULL DEFAULT 0,
    editions_fetched  INTEGER NOT NULL DEFAULT 0,
    errors            INTEGER NOT NULL DEFAULT 0,
    outcome           TEXT    CHECK (outcome IN ('Completed', 'Cancelled', 'Failed'))
);
