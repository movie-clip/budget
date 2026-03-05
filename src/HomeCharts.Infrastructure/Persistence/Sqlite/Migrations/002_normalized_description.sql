ALTER TABLE transactions ADD COLUMN normalized_description TEXT NOT NULL DEFAULT '';

UPDATE transactions
SET normalized_description = UPPER(TRIM(REPLACE(REPLACE(description, CHAR(9), ' '), CHAR(10), ' ')))
WHERE normalized_description = '';

CREATE INDEX IF NOT EXISTS ix_transactions_normalized_description ON transactions(normalized_description);
