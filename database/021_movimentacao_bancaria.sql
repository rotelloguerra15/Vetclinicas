-- Migration 021 — Movimentação Bancária

CREATE TABLE IF NOT EXISTS contas_bancarias (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id   UUID NOT NULL REFERENCES tenants(id),
    nome        VARCHAR(100) NOT NULL,       -- ex: "Nubank", "Bradesco CC"
    banco       VARCHAR(60),                 -- nome do banco
    agencia     VARCHAR(10),
    conta       VARCHAR(20),
    tipo        VARCHAR(20) NOT NULL DEFAULT 'corrente', -- corrente | poupanca | caixa | outro
    saldo_inicial NUMERIC(12,2) NOT NULL DEFAULT 0,
    ativo       BOOLEAN NOT NULL DEFAULT TRUE,
    criado_em   TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_contas_bancarias_tenant ON contas_bancarias(tenant_id);

CREATE TABLE IF NOT EXISTS movimentacoes_bancarias (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id           UUID NOT NULL REFERENCES tenants(id),
    conta_bancaria_id   UUID NOT NULL REFERENCES contas_bancarias(id),
    tipo                VARCHAR(10) NOT NULL, -- entrada | saida | transferencia
    valor               NUMERIC(12,2) NOT NULL,
    descricao           VARCHAR(200) NOT NULL,
    data_movimentacao   DATE NOT NULL,
    categoria_id        UUID REFERENCES categorias_financeiras(id),
    conta_id            UUID REFERENCES contas(id), -- vinculo com contas a pagar/receber
    conta_destino_id    UUID REFERENCES contas_bancarias(id), -- para transferencias
    conciliado          BOOLEAN NOT NULL DEFAULT FALSE,
    criado_por          UUID REFERENCES users(id),
    criado_em           TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_movbancaria_tenant ON movimentacoes_bancarias(tenant_id, data_movimentacao DESC);
CREATE INDEX IF NOT EXISTS idx_movbancaria_conta  ON movimentacoes_bancarias(conta_bancaria_id);

-- Seed: cria conta padrão "Caixa" para tenants existentes
INSERT INTO contas_bancarias (id, tenant_id, nome, tipo, saldo_inicial)
SELECT gen_random_uuid(), id, 'Caixa', 'caixa', 0
FROM tenants
ON CONFLICT DO NOTHING;
