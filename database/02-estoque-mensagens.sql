-- ============================================================
--  VetClinica — Migração 02
--  Módulo de Estoque + PDV (vendas) + Motor de Mensagens
-- ============================================================

-- ──────────────────────────────────────────────
--  ENUMS NOVOS
-- ──────────────────────────────────────────────
CREATE TYPE tipo_mov_estoque AS ENUM ('entrada', 'saida_venda', 'saida_consumo', 'saida_avaria', 'saida_validade', 'saida_doacao', 'ajuste');
CREATE TYPE status_venda      AS ENUM ('aberta', 'finalizada', 'cancelada');
CREATE TYPE gatilho_mensagem  AS ENUM ('aniversario_pet', 'aniversario_tutor', 'boas_vindas_tutor', 'boas_vindas_pet', 'lembrete_agendamento', 'pos_atendimento_nps', 'vacina_vencendo');

-- ──────────────────────────────────────────────
--  PRODUTOS
-- ──────────────────────────────────────────────
CREATE TABLE produtos (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID NOT NULL REFERENCES tenants(id),
    nome            VARCHAR(150) NOT NULL,
    codigo_barras   VARCHAR(60),
    categoria       VARCHAR(60),              -- 'racao', 'medicamento', 'higiene', 'acessorio'
    unidade         VARCHAR(20) DEFAULT 'un', -- un, kg, ml, cx
    preco_custo     NUMERIC(10,2) NOT NULL DEFAULT 0,
    preco_venda     NUMERIC(10,2) NOT NULL DEFAULT 0,
    -- controle de estoque
    estoque_atual   NUMERIC(10,2) NOT NULL DEFAULT 0,
    estoque_minimo  NUMERIC(10,2) NOT NULL DEFAULT 0,   -- ponto de reposição
    estoque_ideal   NUMERIC(10,2),                       -- alvo p/ sugestão de compra
    controla_validade BOOLEAN NOT NULL DEFAULT FALSE,
    ativo           BOOLEAN NOT NULL DEFAULT TRUE,
    criado_em       TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ──────────────────────────────────────────────
--  MOVIMENTAÇÕES DE ESTOQUE  (kardex)
-- ──────────────────────────────────────────────
CREATE TABLE movimentacoes_estoque (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID NOT NULL REFERENCES tenants(id),
    produto_id      UUID NOT NULL REFERENCES produtos(id),
    tipo            tipo_mov_estoque NOT NULL,
    quantidade      NUMERIC(10,2) NOT NULL,       -- positivo entrada, negativo saída
    estoque_apos    NUMERIC(10,2) NOT NULL,       -- saldo após a movimentação
    custo_unitario  NUMERIC(10,2),
    motivo          TEXT,
    venda_id        UUID,                          -- se veio de uma venda
    user_id         UUID REFERENCES users(id),
    data            TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ──────────────────────────────────────────────
--  VENDAS (PDV)
-- ──────────────────────────────────────────────
CREATE TABLE vendas (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID NOT NULL REFERENCES tenants(id),
    tutor_id        UUID REFERENCES tutores(id),
    user_id         UUID REFERENCES users(id),
    status          status_venda NOT NULL DEFAULT 'aberta',
    valor_produtos  NUMERIC(10,2) NOT NULL DEFAULT 0,
    valor_desconto  NUMERIC(10,2) NOT NULL DEFAULT 0,
    valor_total     NUMERIC(10,2) NOT NULL DEFAULT 0,
    forma_pagamento VARCHAR(40),               -- dinheiro, pix, cartao_credito, cartao_debito
    finalizada_em   TIMESTAMPTZ,
    criado_em       TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE venda_itens (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    venda_id        UUID NOT NULL REFERENCES vendas(id) ON DELETE CASCADE,
    produto_id      UUID NOT NULL REFERENCES produtos(id),
    quantidade      NUMERIC(10,2) NOT NULL,
    preco_unitario  NUMERIC(10,2) NOT NULL,
    subtotal        NUMERIC(10,2) NOT NULL
);

-- ──────────────────────────────────────────────
--  CONFIGURAÇÃO DE MENSAGENS AUTOMÁTICAS
-- ──────────────────────────────────────────────
CREATE TABLE config_mensagens (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID NOT NULL REFERENCES tenants(id),
    gatilho         gatilho_mensagem NOT NULL,
    canal           canal_notif NOT NULL DEFAULT 'whatsapp',
    ativo           BOOLEAN NOT NULL DEFAULT TRUE,
    -- offset opcional: ex. lembrete enviado X horas antes do agendamento
    horas_antes     INTEGER,
    -- template com placeholders: {tutor}, {pet}, {data}, {hora}, {clinica}
    template        TEXT NOT NULL,
    hora_envio      TIME DEFAULT '09:00',     -- p/ disparos diários (aniversário)
    criado_em       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (tenant_id, gatilho)
);

-- ──────────────────────────────────────────────
--  ÍNDICES
-- ──────────────────────────────────────────────
CREATE INDEX idx_produtos_tenant     ON produtos(tenant_id) WHERE ativo;
CREATE INDEX idx_produtos_estoque_min ON produtos(tenant_id) WHERE estoque_atual <= estoque_minimo;
CREATE INDEX idx_mov_produto         ON movimentacoes_estoque(produto_id, data);
CREATE INDEX idx_vendas_tenant       ON vendas(tenant_id, criado_em);
CREATE INDEX idx_venda_itens         ON venda_itens(venda_id);
CREATE INDEX idx_config_msg_tenant   ON config_mensagens(tenant_id, gatilho);

-- ──────────────────────────────────────────────
--  TRIGGER: ao finalizar venda, dá baixa no estoque + gera lançamento
-- ──────────────────────────────────────────────
CREATE OR REPLACE FUNCTION fn_venda_finaliza()
RETURNS TRIGGER LANGUAGE plpgsql AS $$
DECLARE
    item RECORD;
    novo_saldo NUMERIC(10,2);
BEGIN
    IF NEW.status = 'finalizada' AND OLD.status != 'finalizada' THEN
        -- baixa de estoque para cada item
        FOR item IN SELECT * FROM venda_itens WHERE venda_id = NEW.id LOOP
            UPDATE produtos
               SET estoque_atual = estoque_atual - item.quantidade
             WHERE id = item.produto_id
            RETURNING estoque_atual INTO novo_saldo;

            INSERT INTO movimentacoes_estoque (
                tenant_id, produto_id, tipo, quantidade, estoque_apos,
                motivo, venda_id, user_id
            ) VALUES (
                NEW.tenant_id, item.produto_id, 'saida_venda',
                -item.quantidade, novo_saldo,
                'Venda #' || NEW.id, NEW.id, NEW.user_id
            );
        END LOOP;

        -- lançamento financeiro de receita
        IF NEW.valor_total > 0 THEN
            INSERT INTO lancamentos (tenant_id, data, tipo, valor, descricao)
            VALUES (NEW.tenant_id, NOW()::DATE, 'receita', NEW.valor_total,
                    'Venda PDV #' || NEW.id);
        END IF;

        NEW.finalizada_em := NOW();
    END IF;
    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_venda_finaliza
BEFORE UPDATE ON vendas
FOR EACH ROW EXECUTE FUNCTION fn_venda_finaliza();

-- ──────────────────────────────────────────────
--  SEED: produtos exemplo + configs de mensagem padrão
-- ──────────────────────────────────────────────
INSERT INTO produtos (tenant_id, nome, categoria, unidade, preco_custo, preco_venda, estoque_atual, estoque_minimo, estoque_ideal) VALUES
('00000000-0000-0000-0000-000000000001', 'Ração Premium Cães Adultos 15kg', 'racao', 'un', 95.00, 159.90, 12, 5, 20),
('00000000-0000-0000-0000-000000000001', 'Antipulgas Cães 10-25kg', 'medicamento', 'un', 28.00, 64.90, 3, 8, 25),
('00000000-0000-0000-0000-000000000001', 'Shampoo Neutro Pet 500ml', 'higiene', 'un', 12.50, 29.90, 18, 6, 24),
('00000000-0000-0000-0000-000000000001', 'Vermífugo Cães comprimido', 'medicamento', 'un', 6.00, 18.00, 40, 15, 60),
('00000000-0000-0000-0000-000000000001', 'Coleira Ajustável M', 'acessorio', 'un', 9.00, 34.90, 2, 5, 15);

INSERT INTO config_mensagens (tenant_id, gatilho, canal, template, ativo) VALUES
('00000000-0000-0000-0000-000000000001', 'aniversario_pet', 'whatsapp',
 '🎉 Hoje é aniversário do {pet}! 🐾 A equipe da {clinica} deseja muitas festas e petiscos. Traga o {pet} para comemorar com um banho especial — temos um mimo te esperando! 🎁', TRUE),
('00000000-0000-0000-0000-000000000001', 'aniversario_tutor', 'whatsapp',
 '🎂 Parabéns, {tutor}! A {clinica} deseja um feliz aniversário. Você e o {pet} merecem o melhor! 💙', TRUE),
('00000000-0000-0000-0000-000000000001', 'boas_vindas_tutor', 'whatsapp',
 'Olá {tutor}! 🐾 Seja muito bem-vindo(a) à {clinica}. Estamos felizes em cuidar do {pet} com você!', TRUE),
('00000000-0000-0000-0000-000000000001', 'lembrete_agendamento', 'whatsapp',
 '🐕 Oi {tutor}! Lembrando o agendamento do {pet} em {data} às {hora}, aqui na {clinica}. Até lá!', TRUE),
('00000000-0000-0000-0000-000000000001', 'pos_atendimento_nps', 'whatsapp',
 'Oi {tutor}! Como foi o atendimento do {pet} hoje na {clinica}? De 0 a 10, o quanto você nos recomendaria? Sua opinião ajuda muito! 🙏', TRUE);
