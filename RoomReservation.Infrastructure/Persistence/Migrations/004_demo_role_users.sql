-- Local/demo accounts only. Replace password hashes with BCrypt before production.
INSERT INTO users (id, organization_id, email, display_name, auth_provider, role, password_hash, is_active, created_at)
SELECT '5dcbb2ea-cf76-4a9a-9b5d-72fcbedb4d9f', id, 'superadmin@example.com', 'Super Admin', 'Password', 'SuperAdmin', 'NbaxmzTCxNCBSDd4+/IJlBHorpz+UCBVQDhu5A7tJOA=', true, now()
FROM organizations WHERE name = 'Startbox'
ON CONFLICT (organization_id, email) DO UPDATE SET role = EXCLUDED.role, password_hash = EXCLUDED.password_hash, is_active = true;
INSERT INTO users (id, organization_id, email, display_name, auth_provider, role, password_hash, is_active, created_at)
SELECT 'b8bbf0e4-f1f5-4f55-a3d9-9e6846c1a4d8', id, 'employee@example.com', 'Employee User', 'Password', 'Employee', 'WQG30a31tcqi6b+cyAa1/swuNgownQQGA9SSTmeAK8s=', true, now()
FROM organizations WHERE name = 'Startbox'
ON CONFLICT (organization_id, email) DO UPDATE SET role = EXCLUDED.role, password_hash = EXCLUDED.password_hash, is_active = true;
