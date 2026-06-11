-- ============================================================
-- Migration 038 - M5: Planos de Saude
-- Tabela de operadoras/planos + vinculo com pet e tutor
-- ============================================================

-- Tabela mestra de planos (operadoras cadastradas pelo tenant)
CREATE TABLE IF NOT EXISTS planos_saude (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID NOT NULL,
    nome            TEXT NOT NULL,
    operadora       TEXT,
    -- desconto padrao aplicado nas OS/vendas do plano (percentual 0-100)
    desconto_percent NUMERIC(5,2) NOT NULL DEFAULT 0,
    ativo           BOOLEAN NOT NULL DEFAULT true,
    obs             TEXT,
    criado_em       TIMESTAMPTZ NOT NULL DEFAULT now(),
    atualizado_em   TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS idx_planos_saude_tenant ON planos_saude(tenant_id);

-- Vinculo pet <-> plano (um pet pode ter so um plano ativo por vez)
CREATE TABLE IF NOT EXISTS pet_planos (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID NOT NULL,
    pet_id          UUID NOT NULL REFERENCES pets(id) ON DELETE CASCADE,
    plano_id        UUID NOT NULL REFERENCES planos_saude(id),
    -- numero da carteirinha do pet neste plano
    num_carteirinha TEXT,
    validade        DATE,
    -- desconto especifico para este pet (sobrescreve o do plano se preenchido)
    desconto_percent NUMERIC(5,2),
    ativo           BOOLEAN NOT NULL DEFAULT true,
    criado_em       TIMESTAMPTZ NOT NULL DEFAULT now(),
    atualizado_em   TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS idx_pet_planos_pet    ON pet_planos(pet_id);
CREATE INDEX IF NOT EXISTS idx_pet_planos_tenant ON pet_planos(tenant_id);

-- Vinculo tutor <-> plano (cobre todos os pets do tutor)
CREATE TABLE IF NOT EXISTS tutor_planos (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID NOT NULL,
    tutor_id        UUID NOT NULL REFERENCES tutores(id) ON DELETE CASCADE,
    plano_id        UUID NOT NULL REFERENCES planos_saude(id),
    num_carteirinha TEXT,
    validade        DATE,
    desconto_percent NUMERIC(5,2),
    ativo           BOOLEAN NOT NULL DEFAULT true,
    criado_em       TIMESTAMPTZ NOT NULL DEFAULT now(),
    atualizado_em   TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS idx_tutor_planos_tutor  ON tutor_planos(tutor_id);
CREATE INDEX IF NOT EXISTS idx_tutor_planos_tenant ON tutor_planos(tenant_id);

-- Campo desconto aplicado na OS (rastreabilidade)
ALTER TABLE ordens_servico
    ADD COLUMN IF NOT EXISTS plano_id           UUID REFERENCES planos_saude(id),
    ADD COLUMN IF NOT EXISTS desconto_percent   NUMERIC(5,2),
    ADD COLUMN IF NOT EXISTS valor_sem_desconto NUMERIC(12,2);

-- Campo desconto aplicado na Venda
ALTER TABLE vendas
    ADD COLUMN IF NOT EXISTS plano_id           UUID REFERENCES planos_saude(id),
    ADD COLUMN IF NOT EXISTS desconto_percent   NUMERIC(5,2),
    ADD COLUMN IF NOT EXISTS valor_sem_desconto NUMERIC(12,2);
