-- Migration 028 — Conciliação Bancária Diária

CREATE TABLE IF NOT EXISTS conciliacoes_diarias (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id           UUID NOT NULL REFERENCES tenants(id),
    conta_bancaria_id   UUID NOT NULL REFERENCES contas_bancarias(id),
    data_conciliacao    DATE NOT NULL,
    saldo_anterior      NUMERIC(12,2) NOT NULL DEFAULT 0,
    total_entradas      NUMERIC(12,2) NOT NULL DEFAULT 0,
    total_saidas        NUMERIC(12,2) NOT NULL DEFAULT 0,
    saldo_final         NUMERIC(12,2) NOT NULL DEFAULT 0,
    saldo_extrato       NUMERIC(12,2),
    diferenca           NUMERIC(12,2),
    observacao          TEXT,
    fechado_por         UUID REFERENCES users(id),
    fechado_em          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    criado_em           TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    UNIQUE (tenant_id, conta_bancaria_id, data_conciliacao)
);

CREATE INDEX IF NOT EXISTS idx_conciliacoes_tenant_conta
    ON conciliacoes_diarias(tenant_id, conta_bancaria_id, data_conciliacao DESC);

-- Adiciona coluna origem em movimentacoes_bancarias (safe — ignora se já existir)
ALTER TABLE movimentacoes_bancarias
    ADD COLUMN IF NOT EXISTS origem VARCHAR(50);
