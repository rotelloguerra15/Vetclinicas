-- Migration 12: comissoes
CREATE TABLE comissoes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    funcionario_id UUID NOT NULL REFERENCES funcionarios(id),

    origem VARCHAR(10) NOT NULL CHECK (origem IN ('os', 'pdv')),
    origem_id UUID NOT NULL,

    valor_base NUMERIC(10,2) NOT NULL,
    percentual NUMERIC(5,2) NOT NULL,
    valor_comissao NUMERIC(10,2) NOT NULL,

    status VARCHAR(20) NOT NULL DEFAULT 'pendente'
        CHECK (status IN ('pendente', 'pago', 'cancelado')),
    fechamento_id UUID,

    competencia DATE NOT NULL,
    criado_em TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_comissoes_funcionario ON comissoes(funcionario_id);
CREATE INDEX idx_comissoes_competencia ON comissoes(competencia);
CREATE INDEX idx_comissoes_status ON comissoes(status);
CREATE INDEX idx_comissoes_tenant ON comissoes(tenant_id);
