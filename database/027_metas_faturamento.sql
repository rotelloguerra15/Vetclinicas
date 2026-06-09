-- Migration 027 — Metas mensais de faturamento

CREATE TABLE IF NOT EXISTS metas_faturamento (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id   UUID NOT NULL REFERENCES tenants(id),
    ano         INT NOT NULL,
    mes         INT NOT NULL,  -- 1-12
    valor_meta  NUMERIC(12,2) NOT NULL DEFAULT 0,
    criado_em   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    atualizado_em TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE(tenant_id, ano, mes)
);

CREATE INDEX IF NOT EXISTS idx_metas_tenant ON metas_faturamento(tenant_id, ano, mes);

-- Seed: cria metas zeradas para os ultimos 6 meses para tenants existentes
INSERT INTO metas_faturamento (id, tenant_id, ano, mes, valor_meta)
SELECT gen_random_uuid(), t.id, m.ano, m.mes, 0
FROM tenants t
CROSS JOIN (
    SELECT
        EXTRACT(YEAR FROM d)::INT as ano,
        EXTRACT(MONTH FROM d)::INT as mes
    FROM generate_series(
        date_trunc('month', now()) - interval '5 months',
        date_trunc('month', now()),
        '1 month'::interval
    ) d
) m
ON CONFLICT (tenant_id, ano, mes) DO NOTHING;
