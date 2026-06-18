-- ============================================================
-- MIGRACAO: PDV Pagamentos - Mercado Pago (Fase 1b)
-- Adiciona config do Mercado Pago por tenant em parametros_sistema.
-- Aplica em TODOS os schemas + template. Idempotente.
-- ============================================================

DO $$
DECLARE
    r RECORD;
BEGIN
    FOR r IN
        SELECT table_schema AS schema_name
        FROM information_schema.tables
        WHERE table_name = 'parametros_sistema'
    LOOP
        EXECUTE format($f$
            ALTER TABLE %I.parametros_sistema
                ADD COLUMN IF NOT EXISTS mp_access_token TEXT,
                ADD COLUMN IF NOT EXISTS mp_device_id    TEXT,
                ADD COLUMN IF NOT EXISTS mp_ambiente     TEXT DEFAULT 'sandbox',
                ADD COLUMN IF NOT EXISTS mp_ativo        BOOLEAN NOT NULL DEFAULT FALSE
        $f$, r.schema_name);

        RAISE NOTICE 'Mercado Pago migrado no schema %', r.schema_name;
    END LOOP;
END $$;
