CREATE TABLE IF NOT EXISTS utility_prefix_filters (
    id TEXT PRIMARY KEY,
    prefix TEXT NOT NULL,
    created_utc TEXT NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_utility_prefix_filters_prefix
ON utility_prefix_filters(prefix COLLATE NOCASE);