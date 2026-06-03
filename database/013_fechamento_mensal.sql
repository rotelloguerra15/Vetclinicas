-- Migration 13: fechamento_mensal
CREATE TABLE fechamento_mensal (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    funcionario_id UUID NOT NULL REFERENCES funcionarios(id),

    competencia DATE NOT NULL,

    salario NUMERIC(10,2) NOT NULL,
    total_comissoes NUMERIC(10,2) NOT NULL DEFAULT 0,
    total_pagar NUMERIC(10,2) NOT NULL,

    status VARCHAR(20) NOT NULL DEFAULT 'aberto'
        CHECK (status IN ('aberto', 'fechado', 'pago')),

    observacoes TEXT,
    fechado_em TIMESTAMPTZ,
    fechado_por UUID,
    criado_em TIMESTAMPTZ DEFAULT NOW(),

    UNIQUE(tenant_id, funcionario_id, competencia)
);

CREATE INDEX idx_fechamento_competencia ON fechamento_mensal(competencia);
CREATE INDEX idx_fechamento_funcionario ON fechamento_mensal(funcionario_id);
CREATE INDEX idx_fechamento_tenant ON fechamento_mensal(tenant_id);
