-- ============================================================
-- MIGRACAO: PDV Pagamentos Integrados - Fase 1 (Pix dinamico)
-- Aplica em TODOS os schemas de tenant + template automaticamente.
-- Idempotente: pode rodar mais de uma vez sem erro.
-- Executar NO RAILWAY PostgreSQL via psql ou DBeaver.
-- ============================================================

DO $$
DECLARE
    r RECORD;
BEGIN
    -- Varre todo schema que tem a tabela parametros_sistema (tenants + template)
    FOR r IN
        SELECT table_schema AS schema_name
        FROM information_schema.tables
        WHERE table_name = 'parametros_sistema'
    LOOP
        -- 1) Config do Asaas por tenant (a clinica recebe na PROPRIA conta)
        EXECUTE format($f$
            ALTER TABLE %I.parametros_sistema
                ADD COLUMN IF NOT EXISTS asaas_api_key  TEXT,
                ADD COLUMN IF NOT EXISTS asaas_ambiente TEXT DEFAULT 'sandbox',
                ADD COLUMN IF NOT EXISTS asaas_pix_ativo BOOLEAN NOT NULL DEFAULT FALSE
        $f$, r.schema_name);

        -- 2) Tabela de cobrancas Pix
        EXECUTE format($f$
            CREATE TABLE IF NOT EXISTS %I.cobrancas_pix (
                id                 UUID PRIMARY KEY,
                tenant_id          UUID NOT NULL,
                venda_id           UUID,
                valor              NUMERIC(12,2) NOT NULL,
                descricao          TEXT,
                status             TEXT NOT NULL DEFAULT 'pendente', -- pendente|pago|expirado|cancelado|erro
                provider           TEXT NOT NULL DEFAULT 'asaas',
                provider_charge_id TEXT,
                txid               TEXT,
                qr_payload         TEXT,   -- copia-e-cola (EMV)
                qr_image           TEXT,   -- imagem base64 (data uri)
                expira_em          TIMESTAMPTZ,
                pago_em            TIMESTAMPTZ,
                criado_por         UUID,
                criado_em          TIMESTAMPTZ NOT NULL DEFAULT NOW()
            )
        $f$, r.schema_name);

        EXECUTE format($f$
            CREATE INDEX IF NOT EXISTS ix_cobrancas_pix_status
                ON %I.cobrancas_pix (status)
        $f$, r.schema_name);

        EXECUTE format($f$
            CREATE INDEX IF NOT EXISTS ix_cobrancas_pix_charge
                ON %I.cobrancas_pix (provider_charge_id)
        $f$, r.schema_name);

        RAISE NOTICE 'Pix migrado no schema %', r.schema_name;
    END LOOP;
END $$;
