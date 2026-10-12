ALTER TABLE users ADD COLUMN IF NOT EXISTS external_subject text;

CREATE UNIQUE INDEX IF NOT EXISTS ix_users_external_subject
    ON users (organization_id, external_subject)
    WHERE external_subject IS NOT NULL;
