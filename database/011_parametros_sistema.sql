-- Migration 11: parametros_sistema
CREATE TABLE parametros_sistema (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,

    comissao_os_ativo BOOLEAN NOT NULL DEFAULT true,
    comissao_pdv_ativo BOOLEAN NOT NULL DEFAULT false,

    criado_em TIMESTAMPTZ DEFAULT NOW(),
    atualizado_em TIMESTAMPTZ DEFAULT NOW(),

    UNIQUE(tenant_id)
);
