-- Migration 019 — Vias de Administração (cadastro auxiliar)

CREATE TABLE IF NOT EXISTS vias_administracao (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id   UUID NOT NULL REFERENCES tenants(id),
    nome        VARCHAR(100) NOT NULL,
    ativo       BOOLEAN NOT NULL DEFAULT TRUE,
    criado_em   TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_vias_tenant ON vias_administracao(tenant_id);

-- Seed: inserir as vias padrão para cada tenant já existente
INSERT INTO vias_administracao (id, tenant_id, nome)
SELECT gen_random_uuid(), t.id, v.nome
FROM tenants t
CROSS JOIN (VALUES
    ('Oral'),
    ('Tópico'),
    ('Oftálmico'),
    ('Ambiental'),
    ('Injetável'),
    ('Auricular'),
    ('Nasal'),
    ('Retal')
) AS v(nome)
ON CONFLICT DO NOTHING;
