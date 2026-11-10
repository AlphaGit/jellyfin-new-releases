-- Want it list (spec 010): decision.kind also allows 'WantIt'. SQLite cannot alter a CHECK, so the table is rebuilt and every row is kept.

CREATE TABLE decision_new (
    artist_key       TEXT NOT NULL,
    normalized_title TEXT NOT NULL,
    kind             TEXT NOT NULL CHECK (kind IN ('Ignore', 'HaveIt', 'WantIt')),
    user_id          TEXT NOT NULL,
    decided_at       TEXT NOT NULL,
    PRIMARY KEY (artist_key, normalized_title)
);

INSERT INTO decision_new (artist_key, normalized_title, kind, user_id, decided_at)
    SELECT artist_key, normalized_title, kind, user_id, decided_at FROM decision;

DROP TABLE decision;

ALTER TABLE decision_new RENAME TO decision;
