ALTER TABLE organization_settings ADD COLUMN IF NOT EXISTS business_start_time text NOT NULL DEFAULT '08:00';
ALTER TABLE organization_settings ADD COLUMN IF NOT EXISTS business_end_time text NOT NULL DEFAULT '18:00';
ALTER TABLE organization_settings ADD COLUMN IF NOT EXISTS advance_booking_days int NOT NULL DEFAULT 30;
ALTER TABLE organization_settings ADD COLUMN IF NOT EXISTS require_approval boolean NOT NULL DEFAULT false;

-- Organizations created before settings support must still receive a settings row.
INSERT INTO organization_settings (id, organization_id, no_show_timeout_minutes, qr_token_ttl_seconds, allow_self_signup, business_start_time, business_end_time, advance_booking_days, require_approval)
SELECT gen_random_uuid(), o.id, 10, 20, false, '08:00', '18:00', 30, false
FROM organizations o
WHERE NOT EXISTS (SELECT 1 FROM organization_settings s WHERE s.organization_id = o.id);
