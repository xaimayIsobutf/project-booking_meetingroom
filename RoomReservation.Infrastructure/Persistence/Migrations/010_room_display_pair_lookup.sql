-- Pairing starts with a short-lived, high-entropy code and therefore must be
-- resolvable before the display knows its organization. Do not expose any
-- device data beyond the matching identifiers, and never return expired or
-- revoked devices.
CREATE OR REPLACE FUNCTION find_room_display_pairing(p_pairing_code_hash text)
RETURNS TABLE(device_id uuid, organization_id uuid, room_id uuid)
LANGUAGE sql
SECURITY DEFINER
SET search_path = public, pg_temp
AS $$
    SELECT d.id, d.organization_id, d.room_id
    FROM room_display_devices AS d
    WHERE d.pairing_code_hash = p_pairing_code_hash
      AND d.is_active = true
      AND d.revoked_at IS NULL
      AND d.pairing_expires_at > now()
    LIMIT 1
    FOR UPDATE
$$;

REVOKE ALL ON FUNCTION find_room_display_pairing(text) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION find_room_display_pairing(text) TO postgres;
