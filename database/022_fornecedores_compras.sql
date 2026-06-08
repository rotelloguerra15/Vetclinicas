-- Migration 022 — Fornecedores e Pedidos de Compra

CREATE TABLE IF NOT EXISTS fornecedores (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id   UUID NOT NULL REFERENCES tenants(id),
    nome        VARCHAR(150) NOT NULL,
    cnpj        VARCHAR(18),
    telefone    VARCHAR(20),
    email       VARCHAR(100),
    contato     VARCHAR(100),   -- nome do responsável
    logradouro  VARCHAR(200),
    num_end     VARCHAR(20),
    bairro      VARCHAR(100),
    cidade      VARCHAR(100),
    estado      CHAR(2),
    cep         VARCHAR(9),
    obs         TEXT,
    ativo       BOOLEAN NOT NULL DEFAULT TRUE,
    criado_em   TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_fornecedores_tenant ON fornecedores(tenant_id);

CREATE TABLE IF NOT EXISTS pedidos_compra (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID NOT NULL REFERENCES tenants(id),
    fornecedor_id   UUID REFERENCES fornecedores(id),
    numero          SERIAL,
    status          VARCHAR(20) NOT NULL DEFAULT 'rascunho',
    -- rascunho | enviado | recebido | cancelado
    data_pedido     DATE NOT NULL DEFAULT CURRENT_DATE,
    data_recebimento DATE,
    valor_total     NUMERIC(12,2) NOT NULL DEFAULT 0,
    obs             TEXT,
    criado_por      UUID REFERENCES users(id),
    criado_em       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    atualizado_em   TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_pedidos_tenant ON pedidos_compra(tenant_id, data_pedido DESC);

CREATE TABLE IF NOT EXISTS pedido_itens (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    pedido_id       UUID NOT NULL REFERENCES pedidos_compra(id) ON DELETE CASCADE,
    produto_id      UUID REFERENCES produtos(id),
    nome_produto    VARCHAR(200) NOT NULL,  -- copia do nome no momento do pedido
    quantidade      NUMERIC(10,3) NOT NULL,
    unidade         VARCHAR(20) NOT NULL DEFAULT 'un',
    valor_unitario  NUMERIC(12,2) NOT NULL,
    valor_total     NUMERIC(12,2) NOT NULL,
    uso             VARCHAR(20) NOT NULL DEFAULT 'venda',  -- venda | interno
    criado_em       TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_pedido_itens_pedido ON pedido_itens(pedido_id);
