CREATE TABLE IF NOT EXISTS categories (
    id TEXT PRIMARY KEY,
    name TEXT NOT NULL,
    color_hex TEXT NOT NULL,
    is_system INTEGER NOT NULL,
    created_utc TEXT NOT NULL,
    updated_utc TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS categorization_rules (
    id TEXT PRIMARY KEY,
    name TEXT NOT NULL,
    pattern TEXT NOT NULL,
    match_type INTEGER NOT NULL,
    priority INTEGER NOT NULL,
    category_id TEXT NOT NULL,
    is_active INTEGER NOT NULL,
    created_utc TEXT NOT NULL,
    updated_utc TEXT NOT NULL,
    FOREIGN KEY(category_id) REFERENCES categories(id)
);

CREATE TABLE IF NOT EXISTS import_batches (
    id TEXT PRIMARY KEY,
    source_name TEXT NOT NULL,
    file_hash TEXT NOT NULL,
    imported_at_utc TEXT NOT NULL,
    imported_count INTEGER NOT NULL,
    skipped_count INTEGER NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_import_batches_file_hash ON import_batches(file_hash);

CREATE TABLE IF NOT EXISTS transactions (
    id TEXT PRIMARY KEY,
    booking_date TEXT NOT NULL,
    value_date TEXT NULL,
    description TEXT NOT NULL,
    amount REAL NOT NULL,
    currency TEXT NOT NULL,
    source_account TEXT NULL,
    counterparty TEXT NULL,
    external_reference TEXT NULL,
    category_id TEXT NULL,
    is_deleted INTEGER NOT NULL,
    created_utc TEXT NOT NULL,
    updated_utc TEXT NOT NULL,
    FOREIGN KEY(category_id) REFERENCES categories(id)
);

CREATE INDEX IF NOT EXISTS ix_transactions_booking_date ON transactions(booking_date);
CREATE INDEX IF NOT EXISTS ix_transactions_category_id ON transactions(category_id);

CREATE TABLE IF NOT EXISTS manual_overrides (
    id TEXT PRIMARY KEY,
    transaction_id TEXT NOT NULL,
    category_id TEXT NOT NULL,
    reason TEXT NULL,
    created_utc TEXT NOT NULL,
    FOREIGN KEY(transaction_id) REFERENCES transactions(id),
    FOREIGN KEY(category_id) REFERENCES categories(id)
);

CREATE INDEX IF NOT EXISTS ix_manual_overrides_transaction_id ON manual_overrides(transaction_id);
