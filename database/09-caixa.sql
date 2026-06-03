CREATE TABLE caixas (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID NOT NULL REFERENCES tenants(id),
    user_id         UUID REFERENCES users(id),
    data            DATE NOT NULL DEFAULT CURRENT_DATE,
    saldo_inicial   NUMERIC(10,2) NOT NULL DEFAULT 0,
    total_vendas    NUMERIC(10,2) NOT NULL DEFAULT 0,
    total_servicos  NUMERIC(10,2) NOT NULL DEFAULT 0,
    saldo_final     NUMERIC(10,2),
    aberto_em       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fechado_em      TIMESTAMPTZ,
    obs             TEXT,
    UNIQUE(tenant_id, data)
);
CREATE INDEX idx_caixas_tenant ON caixas(tenant_id, data);
