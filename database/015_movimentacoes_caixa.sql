-- Migration 15: movimentacoes_caixa (retiradas e depósitos)
CREATE TABLE movimentacoes_caixa (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id   UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    caixa_id    UUID NOT NULL REFERENCES caixas(id) ON DELETE CASCADE,
    user_id     UUID REFERENCES users(id),

    tipo        VARCHAR(10) NOT NULL CHECK (tipo IN ('retirada', 'deposito')),
    valor       NUMERIC(10,2) NOT NULL,
    descricao   TEXT,

    criado_em   TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_mov_caixa ON movimentacoes_caixa(caixa_id);
CREATE INDEX idx_mov_tenant ON movimentacoes_caixa(tenant_id);

-- Adiciona coluna status no caixa para controle de reabertura
ALTER TABLE caixas ADD COLUMN IF NOT EXISTS requer_aprovacao BOOLEAN DEFAULT false;
ALTER TABLE caixas ADD COLUMN IF NOT EXISTS aprovado_por UUID REFERENCES users(id);
ALTER TABLE caixas ADD COLUMN IF NOT EXISTS aprovado_em TIMESTAMPTZ;
