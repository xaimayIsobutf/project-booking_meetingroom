ALTER TABLE users ADD COLUMN IF NOT EXISTS invitation_token text;
ALTER TABLE users ADD COLUMN IF NOT EXISTS invitation_expires_at timestamptz;
CREATE UNIQUE INDEX IF NOT EXISTS ix_users_invitation_token ON users(invitation_token) WHERE invitation_token IS NOT NULL;
