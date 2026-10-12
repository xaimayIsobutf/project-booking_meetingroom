ALTER TABLE users ALTER COLUMN organization_id DROP NOT NULL;
ALTER TABLE bookings ALTER COLUMN user_id DROP NOT NULL;
ALTER TABLE audit_logs ALTER COLUMN actor_user_id DROP NOT NULL;

ALTER TABLE rooms ADD COLUMN IF NOT EXISTS is_public boolean NOT NULL DEFAULT false;
ALTER TABLE rooms ADD COLUMN IF NOT EXISTS public_booking_enabled boolean NOT NULL DEFAULT false;
ALTER TABLE rooms ADD COLUMN IF NOT EXISTS public_requires_approval boolean NOT NULL DEFAULT true;

ALTER TABLE bookings ADD COLUMN IF NOT EXISTS booking_source text NOT NULL DEFAULT 'Internal';
ALTER TABLE bookings ADD COLUMN IF NOT EXISTS guest_name text;
ALTER TABLE bookings ADD COLUMN IF NOT EXISTS guest_email text;
ALTER TABLE bookings ADD COLUMN IF NOT EXISTS guest_phone text;
ALTER TABLE bookings ADD COLUMN IF NOT EXISTS guest_manage_token_hash text;
ALTER TABLE bookings ADD COLUMN IF NOT EXISTS guest_manage_token_expires_at timestamptz;
CREATE UNIQUE INDEX IF NOT EXISTS ix_bookings_guest_manage_token_hash ON bookings(guest_manage_token_hash) WHERE guest_manage_token_hash IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_rooms_public_active ON rooms(is_public, is_active) WHERE is_public = true AND is_active = true;

-- Allow an unaffiliated account to be created and later attached to a tenant,
-- while keeping every other user's row tenant-isolated.
ALTER TABLE users ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON users;
CREATE POLICY tenant_isolation ON users
    USING (organization_id = nullif(current_setting('app.current_org_id', true), '')::uuid
        OR (organization_id IS NULL AND id = nullif(current_setting('app.current_user_id', true), '')::uuid)
        OR lower(email) = lower(nullif(current_setting('app.login_email', true), ''))
        OR email_verification_token = nullif(current_setting('app.email_verification_token', true), ''))
    WITH CHECK (organization_id = nullif(current_setting('app.current_org_id', true), '')::uuid
        OR (organization_id IS NULL AND id = nullif(current_setting('app.current_user_id', true), '')::uuid));
CREATE UNIQUE INDEX IF NOT EXISTS ix_users_unassigned_email ON users(lower(email)) WHERE organization_id IS NULL;

-- Anonymous requests can only read active rooms explicitly published by their owner.
DROP POLICY IF EXISTS tenant_isolation ON rooms;
CREATE POLICY tenant_isolation ON rooms
    USING ((organization_id = nullif(current_setting('app.current_org_id', true), '')::uuid
        OR EXISTS (SELECT 1 FROM room_accesses a WHERE a.room_id = rooms.id AND a.organization_id = nullif(current_setting('app.current_org_id', true), '')::uuid AND a.can_view)
        OR (is_public = true AND is_active = true))
        AND (is_active = true OR organization_id = nullif(current_setting('app.current_org_id', true), '')::uuid));

-- A guest manage token can address exactly one booking without opening the
-- tenant's bookings to anonymous requests. Updates still require the tenant
-- setting, which the cancel endpoint sets after resolving the token.
ALTER TABLE bookings ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON bookings;
CREATE POLICY tenant_isolation ON bookings
    USING (organization_id = nullif(current_setting('app.current_org_id', true), '')::uuid
        OR guest_manage_token_hash = nullif(current_setting('app.public_booking_token', true), ''))
    WITH CHECK (organization_id = nullif(current_setting('app.current_org_id', true), '')::uuid);
