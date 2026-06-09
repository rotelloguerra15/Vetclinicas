-- Migration 031 — Cargos + vínculo Funcionário/Usuário
-- Cargos substituem o campo texto livre "cargo" em funcionarios
-- usuario_id vincula o funcionário ao login do sistema

-- Tabela de cargos
CREATE TABLE IF NOT EXISTS cargos (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id           UUID NOT NULL REFERENCES tenants(id),
    nome                VARCHAR(100) NOT NULL,
    pode_receituario    BOOLEAN NOT NULL DEFAULT FALSE,  -- cargos com este flag assinam receituário
    ativo               BOOLEAN NOT NULL DEFAULT TRUE,
    criado_em           TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_cargos_tenant ON cargos(tenant_id);

-- Seed: migrar cargos existentes do campo texto
INSERT INTO cargos (id, tenant_id, nome, pode_receituario, ativo)
SELECT DISTINCT
    gen_random_uuid(),
    tenant_id,
    cargo,
    -- se tinha CRMV no funcionário, o cargo provavelmente é veterinário
    EXISTS(
        SELECT 1 FROM funcionarios f2
        WHERE f2.tenant_id = f.tenant_id
          AND f2.cargo = f.cargo
          AND f2.crmv IS NOT NULL
    ),
    true
FROM funcionarios f
WHERE cargo IS NOT NULL AND cargo != ''
ON CONFLICT DO NOTHING;

-- Adiciona cargo_id e usuario_id em funcionarios
ALTER TABLE funcionarios
    ADD COLUMN IF NOT EXISTS cargo_id   UUID REFERENCES cargos(id),
    ADD COLUMN IF NOT EXISTS usuario_id UUID REFERENCES users(id);

-- Vincula cargo_id aos funcionarios existentes pelo texto do cargo
UPDATE funcionarios f
SET cargo_id = c.id
FROM cargos c
WHERE c.tenant_id = f.tenant_id
  AND c.nome = f.cargo
  AND f.cargo_id IS NULL;

-- Seed: cria cargo padrão "Médico Veterinário" para tenants sem cargos
INSERT INTO cargos (id, tenant_id, nome, pode_receituario, ativo)
SELECT gen_random_uuid(), id, 'Médico Veterinário', TRUE, TRUE
FROM tenants
WHERE NOT EXISTS (
    SELECT 1 FROM cargos c WHERE c.tenant_id = tenants.id
);

-- Remove coluna assina_receituario (substituída pelo cargo.pode_receituario)
-- Mantém por compatibilidade — pode remover em migration futura
-- ALTER TABLE funcionarios DROP COLUMN IF EXISTS assina_receituario;
