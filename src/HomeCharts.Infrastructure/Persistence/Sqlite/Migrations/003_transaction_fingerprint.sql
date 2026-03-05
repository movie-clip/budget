ALTER TABLE transactions ADD COLUMN transaction_fingerprint TEXT NOT NULL DEFAULT '';

UPDATE transactions
SET transaction_fingerprint =
    booking_date || '|' ||
    COALESCE(value_date, '') || '|' ||
    normalized_description || '|' ||
    CAST(amount AS TEXT) || '|' ||
    '0' || '|' ||
    COALESCE(external_reference, '') || '|' ||
    COALESCE(source_account, '')
WHERE transaction_fingerprint = '';

CREATE INDEX IF NOT EXISTS ix_transactions_fingerprint ON transactions(transaction_fingerprint);
