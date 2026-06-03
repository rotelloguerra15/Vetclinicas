-- Migration 10: funcionarios
CREATE TABLE funcionarios (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,

    nome VARCHAR(150) NOT NULL,
    cpf VARCHAR(14),
    rg VARCHAR(20),
    data_nascimento DATE,

    logradouro VARCHAR(200),
    numero VARCHAR(10),
    complemento VARCHAR(100),
    bairro VARCHAR(100),
    cidade VARCHAR(100),
    estado CHAR(2),
    cep VARCHAR(9),

    telefone VARCHAR(20),
    email VARCHAR(150),

    cargo VARCHAR(100),
    data_admissao DATE,
    salario NUMERIC(10,2) DEFAULT 0,
    percentual_comissao NUMERIC(5,2) DEFAULT 0,

    status VARCHAR(20) NOT NULL DEFAULT 'trabalhando'
        CHECK (status IN ('trabalhando', 'ferias', 'demitido')),

    criado_em TIMESTAMPTZ DEFAULT NOW(),
    atualizado_em TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_funcionarios_tenant ON funcionarios(tenant_id);
CREATE INDEX idx_funcionarios_status ON funcionarios(status);
