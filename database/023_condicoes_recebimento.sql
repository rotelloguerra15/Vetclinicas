-- Migration 023 — Condicoes de pagamento + Recebimento de mercadoria

-- Condicoes de pagamento cadastraveis
CREATE TABLE IF NOT EXISTS condicoes_pagamento (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id   UUID NOT NULL REFERENCES tenants(id),
    nome        VARCHAR(100) NOT NULL,   -- ex: "A vista", "30/60/90"
    parcelas    INT NOT NULL DEFAULT 1,
    intervalo_dias INT NOT NULL DEFAULT 30, -- intervalo entre parcelas
    ativo       BOOLEAN NOT NULL DEFAULT TRUE,
    criado_em   TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_condicoes_tenant ON condicoes_pagamento(tenant_id);

-- Seeds para todos os tenants
INSERT INTO condicoes_pagamento (id, tenant_id, nome, parcelas, intervalo_dias)
SELECT gen_random_uuid(), t.id, c.nome, c.parcelas, c.intervalo_dias
FROM tenants t
CROSS JOIN (VALUES
    ('A vista', 1, 0),
    ('30 dias', 1, 30),
    ('30/60', 2, 30),
    ('30/60/90', 3, 30),
    ('30/60/90/120', 4, 30),
    ('5x mensal', 5, 30),
    ('10x mensal', 10, 30)
) AS c(nome, parcelas, intervalo_dias)
ON CONFLICT DO NOTHING;

-- Adiciona campos no pedido de compra
ALTER TABLE pedidos_compra
    ADD COLUMN IF NOT EXISTS condicao_pagamento_id UUID REFERENCES condicoes_pagamento(id),
    ADD COLUMN IF NOT EXISTS parcelas INT NOT NULL DEFAULT 1,
    ADD COLUMN IF NOT EXISTS forma_pagamento VARCHAR(30) DEFAULT 'boleto';

-- Tabela de recebimentos de mercadoria (separada do pedido)
CREATE TABLE IF NOT EXISTS recebimentos_mercadoria (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID NOT NULL REFERENCES tenants(id),
    pedido_id       UUID REFERENCES pedidos_compra(id),
    data_recebimento DATE NOT NULL DEFAULT CURRENT_DATE,
    status          VARCHAR(20) NOT NULL DEFAULT 'pendente',
    -- pendente | conferido | finalizado
    obs             TEXT,
    criado_por      UUID REFERENCES users(id),
    criado_em       TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS recebimento_itens (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    recebimento_id      UUID NOT NULL REFERENCES recebimentos_mercadoria(id) ON DELETE CASCADE,
    pedido_item_id      UUID REFERENCES pedido_itens(id),
    produto_id          UUID REFERENCES produtos(id),
    nome_produto        VARCHAR(200) NOT NULL,
    quantidade_pedida   NUMERIC(10,3) NOT NULL DEFAULT 0,
    quantidade_recebida NUMERIC(10,3) NOT NULL DEFAULT 0,
    valor_unitario      NUMERIC(12,2) NOT NULL DEFAULT 0,
    uso                 VARCHAR(20) NOT NULL DEFAULT 'venda',
    criado_em           TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_recebimentos_tenant ON recebimentos_mercadoria(tenant_id);
CREATE INDEX IF NOT EXISTS idx_recebimentos_pedido ON recebimentos_mercadoria(pedido_id);

-- Vinculo conta a pagar com pedido de compra
ALTER TABLE contas
    ADD COLUMN IF NOT EXISTS pedido_id UUID REFERENCES pedidos_compra(id);
