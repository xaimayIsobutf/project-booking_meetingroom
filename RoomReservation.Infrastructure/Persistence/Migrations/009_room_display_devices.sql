CREATE TABLE IF NOT EXISTS room_display_devices (
    id uuid PRIMARY KEY,
    organization_id uuid NOT NULL REFERENCES organizations(id),
    room_id uuid NOT NULL REFERENCES rooms(id),
    display_name text NOT NULL,
    display_token_hash text NOT NULL,
    pairing_code_hash text UNIQUE,
    pairing_expires_at timestamptz,
    paired_at timestamptz,
    last_seen_at timestamptz,
    revoked_at timestamptz,
    is_active boolean NOT NULL DEFAULT true,
    created_at timestamptz NOT NULL
);
CREATE INDEX IF NOT EXISTS ix_room_display_devices_org_room ON room_display_devices(organization_id, room_id);
DO $$ BEGIN
    ALTER TABLE room_display_devices ENABLE ROW LEVEL SECURITY;
    DROP POLICY IF EXISTS tenant_isolation ON room_display_devices;
    CREATE POLICY tenant_isolation ON room_display_devices USING (organization_id = nullif(current_setting('app.current_org_id', true), '')::uuid) WITH CHECK (organization_id = nullif(current_setting('app.current_org_id', true), '')::uuid);
EXCEPTION WHEN duplicate_object THEN NULL; END $$;
