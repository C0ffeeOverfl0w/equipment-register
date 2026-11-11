-- Optional local demonstration data. Not executed by application startup.
BEGIN;
SELECT pg_advisory_xact_lock(733101, 2);

WITH available_types AS (
    SELECT array_agg(id ORDER BY id) AS ids FROM equipment_types
), demo AS (
    SELECT
        'DEMO' || lpad(series::text, 4, '0') AS inventory_number,
        'Рабочее место №' || series AS name,
        ids[1 + (series - 1) % cardinality(ids)] AS equipment_type_id,
        1 + (series * 7) % 1000 AS room
    FROM generate_series(1, 45) AS series
    CROSS JOIN available_types
    WHERE cardinality(ids) > 0
    ORDER BY series
    LIMIT GREATEST(0, 2000 - (SELECT count(*) FROM equipment))
)
INSERT INTO equipment(inventory_number, name, equipment_type_id, room)
SELECT inventory_number, name, equipment_type_id, room FROM demo
ON CONFLICT (inventory_number) DO NOTHING;

COMMIT;
