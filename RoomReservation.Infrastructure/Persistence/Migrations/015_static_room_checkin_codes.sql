-- Every room gets a stable, opaque QR value for the physical room sign.
-- The code identifies the room only; authentication and booking ownership are
-- enforced by the Check-in API before any status change is allowed.
ALTER TABLE rooms ADD COLUMN IF NOT EXISTS static_check_in_code text;

UPDATE rooms
SET static_check_in_code = replace(gen_random_uuid()::text, '-', '')
WHERE static_check_in_code IS NULL OR static_check_in_code = '';

ALTER TABLE rooms ALTER COLUMN static_check_in_code SET NOT NULL;
ALTER TABLE rooms ALTER COLUMN static_check_in_code SET DEFAULT replace(gen_random_uuid()::text, '-', '');
CREATE UNIQUE INDEX IF NOT EXISTS ix_rooms_static_check_in_code
    ON rooms(static_check_in_code);
