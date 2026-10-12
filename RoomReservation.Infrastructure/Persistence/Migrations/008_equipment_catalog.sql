CREATE TABLE IF NOT EXISTS equipment_items (
    id uuid PRIMARY KEY,
    organization_id uuid NOT NULL REFERENCES organizations(id),
    name text NOT NULL,
    is_active boolean NOT NULL DEFAULT TRUE,
    created_at timestamp without time zone NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_equipment_items_org_name UNIQUE (organization_id, name)
);

INSERT INTO equipment_items (id, organization_id, name)
SELECT gen_random_uuid(), o.id, x.name
FROM organizations o
CROSS JOIN (VALUES ('TV'), ('Projector'), ('Video conference'), ('Whiteboard'), ('Video call'), ('Speakerphone'), ('Microphone'), ('Webcam')) AS x(name)
ON CONFLICT (organization_id, name) DO NOTHING;
