ALTER TABLE users ADD COLUMN IF NOT EXISTS membership_status text NOT NULL DEFAULT 'Active';
ALTER TABLE users ADD COLUMN IF NOT EXISTS email_verified boolean NOT NULL DEFAULT true;
ALTER TABLE users ADD COLUMN IF NOT EXISTS email_verification_token text;
ALTER TABLE users ADD COLUMN IF NOT EXISTS email_verification_expires_at timestamptz;
CREATE UNIQUE INDEX IF NOT EXISTS ix_users_email_verification_token ON users(email_verification_token) WHERE email_verification_token IS NOT NULL;
