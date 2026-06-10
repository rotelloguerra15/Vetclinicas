-- Migration 034 -- Tabela de Pelagens

CREATE TABLE IF NOT EXISTS pelagens (
    id         UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id  UUID NOT NULL REFERENCES tenants(id),
    nome       VARCHAR(100) NOT NULL,
    ativo      BOOLEAN NOT NULL DEFAULT TRUE,
    criado_em  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_pelagens_tenant ON pelagens(tenant_id);

-- Seeds padrao para todos os tenants existentes
INSERT INTO pelagens (id, tenant_id, nome)
SELECT gen_random_uuid(), id, unnest(ARRAY[
    'Curta', 'Longa', 'Media', 'Dupla', 'Crespa',
    'Lisa', 'Ondulada', 'Felpuda', 'Bicolor', 'Tricolor',
    'Tigrada', 'Malhada', 'Sem pelo'
])
FROM tenants
ON CONFLICT DO NOTHING;
