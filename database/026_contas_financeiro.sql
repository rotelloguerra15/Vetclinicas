-- Migration 026 — Cria tabela contas (financeiro a pagar/receber)
-- A entity Conta usa [Table("contas")] mas a tabela nao existia no banco

CREATE TABLE IF NOT EXISTS contas (
    id                UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id         UUID NOT NULL REFERENCES tenants(id),
    os_id             UUID REFERENCES ordens_servico(id),
    venda_id          UUID REFERENCES vendas(id),
    pedido_id         UUID REFERENCES pedidos_compra(id),
    tipo              VARCHAR(20) NOT NULL DEFAULT 'receita', -- receita | despesa
    categoria_id      UUID REFERENCES categorias_financeiras(id),
    descricao         TEXT NOT NULL DEFAULT '',
    valor             NUMERIC(12,2) NOT NULL DEFAULT 0,
    data_competencia  DATE NOT NULL,
    data_vencimento   DATE NOT NULL,
    forma_pagamento   VARCHAR(30),
    status            VARCHAR(20) NOT NULL DEFAULT 'aberta', -- aberta | paga | recebida | cancelada
    valor_pago        NUMERIC(12,2),
    data_baixa        DATE,
    conta_bancaria    VARCHAR(100),
    obs_baixa         TEXT,
    criado_por        UUID REFERENCES users(id),
    criado_em         TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    atualizado_em     TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_contas_tenant     ON contas(tenant_id, data_vencimento);
CREATE INDEX IF NOT EXISTS idx_contas_status     ON contas(tenant_id, status);
CREATE INDEX IF NOT EXISTS idx_contas_pedido     ON contas(pedido_id);

-- Migra lancamentos existentes para contas (compatibilidade)
INSERT INTO contas (id, tenant_id, os_id, categoria_id, tipo, descricao, valor,
    data_competencia, data_vencimento, status, criado_em, atualizado_em)
SELECT
    id,
    tenant_id,
    os_id,
    categoria_id,
    CASE WHEN tipo::TEXT = 'receita' THEN 'receita' ELSE 'despesa' END,
    COALESCE(descricao, ''),
    valor,
    data::DATE,
    data::DATE,
    'paga',
    criado_em,
    criado_em
FROM lancamentos
ON CONFLICT (id) DO NOTHING;
