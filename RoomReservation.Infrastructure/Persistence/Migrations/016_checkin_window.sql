-- Use a ten-minute check-in window for organizations that still have the
-- original fifteen-minute default. The marker keeps this migration from
-- overwriting a value an administrator changes later.
CREATE TABLE IF NOT EXISTS schema_migrations (name text PRIMARY KEY);

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM schema_migrations WHERE name = '016_checkin_window') THEN
        UPDATE organization_settings
        SET no_show_timeout_minutes = 10
        WHERE no_show_timeout_minutes = 15;

        INSERT INTO schema_migrations (name) VALUES ('016_checkin_window');
    END IF;
END $$;
