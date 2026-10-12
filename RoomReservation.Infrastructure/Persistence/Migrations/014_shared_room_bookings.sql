-- Shared rooms use the same physical calendar for every organization that has
-- access. Keep booking ownership with the requester, but allow the room owner
-- and shared viewers to read the room's bookings for calendar/collision checks.
DROP POLICY IF EXISTS tenant_isolation ON bookings;
CREATE POLICY tenant_isolation ON bookings
    USING (
        organization_id = nullif(current_setting('app.current_org_id', true), '')::uuid
        OR EXISTS (
            SELECT 1
            FROM rooms r
            WHERE r.id = bookings.room_id
              AND (
                  r.organization_id = nullif(current_setting('app.current_org_id', true), '')::uuid
                  OR EXISTS (
                      SELECT 1
                      FROM room_accesses a
                      WHERE a.room_id = r.id
                        AND a.organization_id = nullif(current_setting('app.current_org_id', true), '')::uuid
                        AND a.can_view
                  )
              )
        )
    )
    WITH CHECK (organization_id = nullif(current_setting('app.current_org_id', true), '')::uuid);

-- A member of an organization that can book a shared room must be able to
-- validate the owner's room QR, while only the owner can rotate/update it.
DROP POLICY IF EXISTS tenant_isolation ON qr_devices;
CREATE POLICY tenant_isolation ON qr_devices
    USING (
        organization_id = nullif(current_setting('app.current_org_id', true), '')::uuid
        OR EXISTS (
            SELECT 1
            FROM room_accesses a
            WHERE a.room_id = qr_devices.room_id
              AND a.organization_id = nullif(current_setting('app.current_org_id', true), '')::uuid
              AND a.can_book
        )
    )
    WITH CHECK (organization_id = nullif(current_setting('app.current_org_id', true), '')::uuid);
