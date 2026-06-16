-- ============================================================
-- PASSO 1: Cria schema platform + tabelas
-- ============================================================

CREATE SCHEMA IF NOT EXISTS platform;

CREATE TABLE IF NOT EXISTS platform.tenants (
    id           UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    nome         TEXT        NOT NULL,
    plano        TEXT        NOT NULL DEFAULT 'trial',
    telefone     TEXT,
    email        TEXT,
    logo_url     TEXT,
    tagline      TEXT,
    cor_primaria TEXT,
    suspenso_em  TIMESTAMPTZ,
    ativo        BOOLEAN     NOT NULL DEFAULT TRUE,
    criado_em    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    schema_name  TEXT,
    whatsapp_number TEXT,
    whatsapp_token  TEXT
);

CREATE TABLE IF NOT EXISTS platform.platform_admins (
    id         UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    nome       TEXT        NOT NULL,
    email      TEXT        NOT NULL,
    senha_hash TEXT        NOT NULL,
    ativo      BOOLEAN     NOT NULL DEFAULT TRUE,
    criado_em  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ============================================================
-- PASSO 2: Insere tenant da Dra. Barbara no platform
-- ============================================================

INSERT INTO platform.tenants (id, nome, plano, email, schema_name, ativo, criado_em)
VALUES (
    '4b19602f-0425-4101-914c-5b9aeaf5d4e7',
    'Pet Shop e Consultorio Dra. Barbara Fonseca',
    'pro',
    'barbaratatschveterinaria@gmail.com',
    'vet_barbarafonseca',
    TRUE,
    NOW()
) ON CONFLICT (id) DO UPDATE SET schema_name = 'vet_barbarafonseca';

-- Insere super-admin Rotello
-- Hash de "Ketra@2026" gerado com BCrypt rounds=12
INSERT INTO platform.platform_admins (id, nome, email, senha_hash, ativo, criado_em)
VALUES (
    gen_random_uuid(),
    'Rotello Guerra',
    'rotello.guerra@ketra.com.br',
    '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TiGc2L.P3Zi0VzF1Ln3GFu8nGWXy',
    TRUE,
    NOW()
) ON CONFLICT DO NOTHING;

-- ============================================================
-- PASSO 3: Cria schema vet_barbarafonseca + todas as tabelas
-- ============================================================

CREATE SCHEMA IF NOT EXISTS vet_barbarafonseca;

CREATE TABLE IF NOT EXISTS vet_barbarafonseca.tenants (
    id           UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    nome         TEXT        NOT NULL,
    plano        TEXT        NOT NULL DEFAULT 'trial',
    telefone     TEXT,
    email        TEXT,
    logo_url     TEXT,
    tagline      TEXT,
    cor_primaria TEXT,
    suspenso_em  TIMESTAMPTZ,
    ativo        BOOLEAN     NOT NULL DEFAULT TRUE,
    criado_em    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    schema_name  TEXT,
    whatsapp_number TEXT,
    whatsapp_token  TEXT
);

CREATE TABLE IF NOT EXISTS vet_barbarafonseca.users (
    id         UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id  UUID        NOT NULL,
    nome       TEXT        NOT NULL,
    email      TEXT        NOT NULL,
    senha_hash TEXT        NOT NULL,
    papel      TEXT        NOT NULL DEFAULT 'atendente',
    ativo      BOOLEAN     NOT NULL DEFAULT TRUE,
    criado_em  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS vet_barbarafonseca.racas (
    id        UUID    NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id UUID    NOT NULL,
    nome      TEXT    NOT NULL,
    especie   TEXT    NOT NULL DEFAULT 'cao',
    ativo     BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS vet_barbarafonseca.pelagens (
    id        UUID    NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id UUID    NOT NULL,
    nome      TEXT    NOT NULL,
    ativo     BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS vet_barbarafonseca.tutores (
    id               UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id        UUID        NOT NULL,
    codigo           TEXT,
    nome             TEXT        NOT NULL,
    cpf_cnpj         TEXT,
    telefone         TEXT,
    email            TEXT,
    logradouro       TEXT,
    numero           TEXT,
    complemento      TEXT,
    bairro           TEXT,
    cidade           TEXT,
    estado           TEXT,
    cep              TEXT,
    data_nascimento  DATE,
    aniversario_dia  SMALLINT,
    aniversario_mes  SMALLINT,
    observacoes      TEXT,
    aceita_promocoes BOOLEAN     NOT NULL DEFAULT TRUE,
    ativo            BOOLEAN     NOT NULL DEFAULT TRUE,
    criado_em        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    atualizado_em    TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS vet_barbarafonseca.pets (
    id              UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id       UUID        NOT NULL,
    tutor_id        UUID        NOT NULL REFERENCES vet_barbarafonseca.tutores(id),
    nome            TEXT        NOT NULL,
    especie         TEXT,
    raca            TEXT,
    raca_id         UUID        REFERENCES vet_barbarafonseca.racas(id),
    pelagem_id      UUID        REFERENCES vet_barbarafonseca.pelagens(id),
    sexo            TEXT,
    data_nascimento TIMESTAMPTZ,
    peso            NUMERIC(8,2),
    microchip       TEXT,
    castrado        BOOLEAN     NOT NULL DEFAULT FALSE,
    foto_url        TEXT,
    observacoes     TEXT,
    ativo           BOOLEAN     NOT NULL DEFAULT TRUE,
    criado_em       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    atualizado_em   TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS vet_barbarafonseca.servicos (
    id          UUID          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id   UUID          NOT NULL,
    codigo      TEXT,
    nome        TEXT          NOT NULL,
    categoria   TEXT,
    descricao   TEXT,
    icone       TEXT,
    preco_base  NUMERIC(12,2) NOT NULL DEFAULT 0,
    duracao_min INT           NOT NULL DEFAULT 30,
    ativo       BOOLEAN       NOT NULL DEFAULT TRUE,
    criado_em   TIMESTAMPTZ   NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS vet_barbarafonseca.vias_administracao (
    id        UUID    NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id UUID    NOT NULL,
    nome      TEXT    NOT NULL,
    ativo     BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS vet_barbarafonseca.agendamentos (
    id             UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id      UUID        NOT NULL,
    pet_id         UUID        NOT NULL REFERENCES vet_barbarafonseca.pets(id),
    user_id        UUID        REFERENCES vet_barbarafonseca.users(id),
    servico_id     UUID        REFERENCES vet_barbarafonseca.servicos(id),
    tipo           TEXT,
    data_hora      TIMESTAMPTZ NOT NULL,
    duracao_min    INT         NOT NULL DEFAULT 30,
    status         TEXT        NOT NULL DEFAULT 'pendente',
    origem         TEXT,
    obs            TEXT,
    confirmado_em  TIMESTAMPTZ,
    confirmado_por UUID,
    criado_em      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS vet_barbarafonseca.ordens_servico (
    id             UUID          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id      UUID          NOT NULL,
    agendamento_id UUID          REFERENCES vet_barbarafonseca.agendamentos(id),
    pet_id         UUID          NOT NULL REFERENCES vet_barbarafonseca.pets(id),
    user_id        UUID          REFERENCES vet_barbarafonseca.users(id),
    funcionario_id UUID,
    status         TEXT          NOT NULL DEFAULT 'aguardando',
    valor_total    NUMERIC(12,2) NOT NULL DEFAULT 0,
    inicio         TIMESTAMPTZ,
    entregue_em    TIMESTAMPTZ,
    obs            TEXT,
    criado_em      TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    atualizado_em  TIMESTAMPTZ   NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS vet_barbarafonseca.os_servicos (
    id            UUID          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    os_id         UUID          NOT NULL REFERENCES vet_barbarafonseca.ordens_servico(id),
    servico_id    UUID          NOT NULL REFERENCES vet_barbarafonseca.servicos(id),
    preco_cobrado NUMERIC(12,2) NOT NULL DEFAULT 0,
    obs           TEXT
);

CREATE TABLE IF NOT EXISTS vet_barbarafonseca.prontuario_itens (
    id           UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id    UUID        NOT NULL,
    pet_id       UUID        NOT NULL REFERENCES vet_barbarafonseca.pets(id),
    tipo         TEXT        NOT NULL,
    titulo       TEXT,
    descricao    TEXT,
    data_evento  DATE,
    codigo_unico TEXT,
    criado_em    TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS vet_barbarafonseca.vacinacoes (
    id             UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id      UUID        NOT NULL,
    pet_id         UUID        NOT NULL REFERENCES vet_barbarafonseca.pets(id),
    nome_vacina    TEXT        NOT NULL,
    lote           TEXT,
    data_aplicacao DATE        NOT NULL,
    proxima_dose   DATE,
    veterinario    TEXT,
    criado_em      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS vet_barbarafonseca.links_agendamento (
    id         UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id  UUID        NOT NULL,
    token      TEXT        NOT NULL,
    pet_id     UUID        REFERENCES vet_barbarafonseca.pets(id),
    tutor_id   UUID        REFERENCES vet_barbarafonseca.tutores(id),
    expires_at TIMESTAMPTZ,
    usado      BOOLEAN     NOT NULL DEFAULT FALSE,
    criado_em  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS vet_barbarafonseca.produtos (
    id             UUID          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id      UUID          NOT NULL,
    codigo         TEXT,
    nome           TEXT          NOT NULL,
    categoria      TEXT,
    unidade        TEXT          NOT NULL DEFAULT 'un',
    preco_venda    NUMERIC(12,2) NOT NULL DEFAULT 0,
    preco_custo    NUMERIC(12,2) NOT NULL DEFAULT 0,
    estoque_atual  NUMERIC(12,2) NOT NULL DEFAULT 0,
    estoque_minimo NUMERIC(12,2) NOT NULL DEFAULT 0,
    ativo          BOOLEAN       NOT NULL DEFAULT TRUE,
    criado_em      TIMESTAMPTZ   NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS vet_barbarafonseca.movimentacoes_estoque (
    id         UUID          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id  UUID          NOT NULL,
    produto_id UUID          NOT NULL REFERENCES vet_barbarafonseca.produtos(id),
    tipo       TEXT          NOT NULL,
    quantidade NUMERIC(12,2) NOT NULL,
    motivo     TEXT,
    criado_em  TIMESTAMPTZ   NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS vet_barbarafonseca.caixas (
    id               UUID          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id        UUID          NOT NULL,
    user_id          UUID          REFERENCES vet_barbarafonseca.users(id),
    data             DATE          NOT NULL,
    saldo_inicial    NUMERIC(12,2) NOT NULL DEFAULT 0,
    total_vendas     NUMERIC(12,2) NOT NULL DEFAULT 0,
    total_servicos   NUMERIC(12,2) NOT NULL DEFAULT 0,
    saldo_final      NUMERIC(12,2),
    aberto_em        TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    fechado_em       TIMESTAMPTZ,
    obs              TEXT,
    requer_aprovacao BOOLEAN       NOT NULL DEFAULT FALSE,
    aprovado_por     UUID,
    aprovado_em      TIMESTAMPTZ
);

CREATE TABLE IF NOT EXISTS vet_barbarafonseca.movimentacoes_caixa (
    id        UUID          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id UUID          NOT NULL,
    caixa_id  UUID          NOT NULL REFERENCES vet_barbarafonseca.caixas(id),
    user_id   UUID          REFERENCES vet_barbarafonseca.users(id),
    tipo      TEXT          NOT NULL,
    valor     NUMERIC(12,2) NOT NULL,
    descricao TEXT,
    criado_em TIMESTAMPTZ   NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS vet_barbarafonseca.vendas (
    id             UUID          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id      UUID          NOT NULL,
    tutor_id       UUID          REFERENCES vet_barbarafonseca.tutores(id),
    caixa_id       UUID          REFERENCES vet_barbarafonseca.caixas(id),
    os_id          UUID          REFERENCES vet_barbarafonseca.ordens_servico(id),
    numero         TEXT,
    valor_subtotal NUMERIC(12,2) NOT NULL DEFAULT 0,
    desconto       NUMERIC(12,2) NOT NULL DEFAULT 0,
    valor_total    NUMERIC(12,2) NOT NULL DEFAULT 0,
    forma_pagamento TEXT,
    status         TEXT          NOT NULL DEFAULT 'finalizada',
    criado_em      TIMESTAMPTZ   NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS vet_barbarafonseca.venda_itens (
    id             UUID          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id      UUID          NOT NULL,
    venda_id       UUID          NOT NULL REFERENCES vet_barbarafonseca.vendas(id),
    produto_id     UUID          REFERENCES vet_barbarafonseca.produtos(id),
    servico_id     UUID          REFERENCES vet_barbarafonseca.servicos(id),
    descricao      TEXT,
    quantidade     NUMERIC(12,2) NOT NULL DEFAULT 1,
    preco_unitario NUMERIC(12,2) NOT NULL DEFAULT 0,
    desconto       NUMERIC(12,2) NOT NULL DEFAULT 0,
    preco_total    NUMERIC(12,2) NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS vet_barbarafonseca.categorias_financeiras (
    id        UUID    NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id UUID    NOT NULL,
    nome      TEXT    NOT NULL,
    tipo      TEXT    NOT NULL,
    ativo     BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS vet_barbarafonseca.contas (
    id               UUID          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id        UUID          NOT NULL,
    pedido_id        UUID,
    tipo             TEXT          NOT NULL,
    descricao        TEXT          NOT NULL,
    valor            NUMERIC(12,2) NOT NULL,
    valor_pago       NUMERIC(12,2),
    data_competencia DATE,
    data_vencimento  DATE,
    data_baixa       DATE,
    forma_pagamento  TEXT,
    status           TEXT          NOT NULL DEFAULT 'aberta',
    categoria_id     UUID          REFERENCES vet_barbarafonseca.categorias_financeiras(id),
    conta_bancaria   TEXT,
    criado_por       UUID,
    criado_em        TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    atualizado_em    TIMESTAMPTZ   NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS vet_barbarafonseca.conta_parcelas (
    id              UUID          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id       UUID          NOT NULL,
    conta_id        UUID          NOT NULL REFERENCES vet_barbarafonseca.contas(id),
    numero          INT           NOT NULL,
    valor           NUMERIC(12,2) NOT NULL,
    data_vencimento DATE          NOT NULL,
    data_pagamento  DATE,
    status          TEXT          NOT NULL DEFAULT 'aberto'
);

CREATE TABLE IF NOT EXISTS vet_barbarafonseca.lancamentos (
    id           UUID          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id    UUID          NOT NULL,
    tipo         TEXT          NOT NULL,
    descricao    TEXT          NOT NULL,
    valor        NUMERIC(12,2) NOT NULL,
    data         DATE          NOT NULL,
    categoria_id UUID          REFERENCES vet_barbarafonseca.categorias_financeiras(id),
    criado_em    TIMESTAMPTZ   NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS vet_barbarafonseca.contas_bancarias (
    id            UUID          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id     UUID          NOT NULL,
    nome          TEXT          NOT NULL,
    banco         TEXT,
    agencia       TEXT,
    conta         TEXT,
    tipo          TEXT,
    saldo_inicial NUMERIC(12,2) NOT NULL DEFAULT 0,
    ativo         BOOLEAN       NOT NULL DEFAULT TRUE,
    criado_em     TIMESTAMPTZ   NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS vet_barbarafonseca.movimentacoes_bancarias (
    id                UUID          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id         UUID          NOT NULL,
    conta_bancaria_id UUID          NOT NULL REFERENCES vet_barbarafonseca.contas_bancarias(id),
    tipo              TEXT          NOT NULL,
    descricao         TEXT          NOT NULL,
    valor             NUMERIC(12,2) NOT NULL,
    data_movimentacao DATE          NOT NULL,
    conta_id          UUID,
    origem            TEXT,
    conciliado        BOOLEAN       NOT NULL DEFAULT FALSE,
    criado_por        UUID,
    criado_em         TIMESTAMPTZ   NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS vet_barbarafonseca.conciliacoes_diarias (
    id                UUID          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id         UUID          NOT NULL,
    conta_bancaria_id UUID          NOT NULL REFERENCES vet_barbarafonseca.contas_bancarias(id),
    data_conciliacao  DATE          NOT NULL,
    saldo_sistema     NUMERIC(12,2) NOT NULL DEFAULT 0,
    saldo_extrato     NUMERIC(12,2) NOT NULL DEFAULT 0,
    diferenca         NUMERIC(12,2) NOT NULL DEFAULT 0,
    status            TEXT          NOT NULL DEFAULT 'pendente',
    observacoes       TEXT,
    criado_em         TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_conciliacao_barbara UNIQUE (conta_bancaria_id, data_conciliacao)
);

CREATE TABLE IF NOT EXISTS vet_barbarafonseca.cargos (
    id               UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id        UUID        NOT NULL,
    nome             TEXT        NOT NULL,
    pode_receituario BOOLEAN     NOT NULL DEFAULT FALSE,
    ativo            BOOLEAN     NOT NULL DEFAULT TRUE,
    criado_em        TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS vet_barbarafonseca.funcionarios (
    id                  UUID          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id           UUID          NOT NULL,
    codigo              TEXT,
    nome                TEXT          NOT NULL,
    cpf                 TEXT,
    rg                  TEXT,
    data_nascimento     DATE,
    logradouro          TEXT,
    numero              TEXT,
    complemento         TEXT,
    bairro              TEXT,
    cidade              TEXT,
    estado              TEXT,
    cep                 TEXT,
    telefone            TEXT,
    email               TEXT,
    cargo               TEXT,
    cargo_id            UUID          REFERENCES vet_barbarafonseca.cargos(id),
    usuario_id          UUID          REFERENCES vet_barbarafonseca.users(id),
    crmv                TEXT,
    registro_mapa       TEXT,
    data_admissao       DATE,
    salario             NUMERIC(12,2) NOT NULL DEFAULT 0,
    percentual_comissao NUMERIC(5,2)  NOT NULL DEFAULT 0,
    assina_receituario  BOOLEAN       NOT NULL DEFAULT FALSE,
    status              TEXT          NOT NULL DEFAULT 'trabalhando',
    criado_em           TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    atualizado_em       TIMESTAMPTZ   NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS vet_barbarafonseca.comissoes (
    id             UUID          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id      UUID          NOT NULL,
    funcionario_id UUID          NOT NULL REFERENCES vet_barbarafonseca.funcionarios(id),
    origem         TEXT          NOT NULL,
    origem_id      UUID          NOT NULL,
    valor_base     NUMERIC(12,2) NOT NULL DEFAULT 0,
    percentual     NUMERIC(5,2)  NOT NULL DEFAULT 0,
    valor_comissao NUMERIC(12,2) NOT NULL DEFAULT 0,
    status         TEXT          NOT NULL DEFAULT 'pendente',
    fechamento_id  UUID,
    competencia    DATE          NOT NULL,
    criado_em      TIMESTAMPTZ   NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS vet_barbarafonseca.fechamento_mensal (
    id             UUID          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id      UUID          NOT NULL,
    funcionario_id UUID          NOT NULL REFERENCES vet_barbarafonseca.funcionarios(id),
    competencia    DATE          NOT NULL,
    salario        NUMERIC(12,2) NOT NULL DEFAULT 0,
    total_comissoes NUMERIC(12,2) NOT NULL DEFAULT 0,
    total_pagar    NUMERIC(12,2) NOT NULL DEFAULT 0,
    status         TEXT          NOT NULL DEFAULT 'aberto',
    observacoes    TEXT,
    fechado_em     TIMESTAMPTZ,
    fechado_por    UUID,
    criado_em      TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_fechamento_barbara UNIQUE (funcionario_id, competencia)
);

CREATE TABLE IF NOT EXISTS vet_barbarafonseca.parametros_sistema (
    id                   UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id            UUID        NOT NULL UNIQUE,
    comissao_os_ativo    BOOLEAN     NOT NULL DEFAULT TRUE,
    comissao_pdv_ativo   BOOLEAN     NOT NULL DEFAULT FALSE,
    anthropic_api_key    TEXT,
    ia_ativo             BOOLEAN     NOT NULL DEFAULT FALSE,
    cert_pfx_encrypted   TEXT,
    cert_senha_hash      TEXT,
    cert_tipo            TEXT,
    cert_titular         TEXT,
    cert_cpf             TEXT,
    cert_validade        DATE,
    cert_ativo           BOOLEAN     NOT NULL DEFAULT FALSE,
    cert_atualizado_em   TIMESTAMPTZ,
    smtp_host            TEXT,
    smtp_porta           INT         NOT NULL DEFAULT 587,
    smtp_usuario         TEXT,
    smtp_senha_enc       TEXT,
    smtp_ssl             BOOLEAN     NOT NULL DEFAULT TRUE,
    email_remetente      TEXT,
    email_contabil       TEXT,
    contabil_nome        TEXT,
    atualizado_em        TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS vet_barbarafonseca.fornecedores (
    id        UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id UUID        NOT NULL,
    codigo    TEXT,
    nome      TEXT        NOT NULL,
    cnpj      TEXT,
    telefone  TEXT,
    email     TEXT,
    contato   TEXT,
    logradouro TEXT,
    numero    TEXT,
    cidade    TEXT,
    estado    TEXT,
    cep       TEXT,
    ativo     BOOLEAN     NOT NULL DEFAULT TRUE,
    criado_em TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS vet_barbarafonseca.condicoes_pagamento (
    id             UUID    NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id      UUID    NOT NULL,
    nome           TEXT    NOT NULL,
    parcelas       INT     NOT NULL DEFAULT 1,
    intervalo_dias INT     NOT NULL DEFAULT 30,
    ativo          BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS vet_barbarafonseca.pedidos_compra (
    id                    UUID          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id             UUID          NOT NULL,
    numero                TEXT,
    fornecedor_id         UUID          REFERENCES vet_barbarafonseca.fornecedores(id),
    condicao_pagamento_id UUID          REFERENCES vet_barbarafonseca.condicoes_pagamento(id),
    forma_pagamento       TEXT,
    status                TEXT          NOT NULL DEFAULT 'rascunho',
    data_pedido           DATE          NOT NULL,
    valor_total           NUMERIC(12,2) NOT NULL DEFAULT 0,
    parcelas              INT           NOT NULL DEFAULT 1,
    obs                   TEXT,
    criado_por            UUID,
    criado_em             TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    atualizado_em         TIMESTAMPTZ   NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS vet_barbarafonseca.pedido_itens (
    id             UUID          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    pedido_id      UUID          NOT NULL REFERENCES vet_barbarafonseca.pedidos_compra(id),
    produto_id     UUID          REFERENCES vet_barbarafonseca.produtos(id),
    nome_produto   TEXT,
    quantidade     NUMERIC(12,2) NOT NULL DEFAULT 1,
    unidade        TEXT,
    valor_unitario NUMERIC(12,2) NOT NULL DEFAULT 0,
    valor_total    NUMERIC(12,2) NOT NULL DEFAULT 0,
    uso            TEXT,
    criado_em      TIMESTAMPTZ   NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS vet_barbarafonseca.recebimentos_mercadoria (
    id               UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id        UUID        NOT NULL,
    pedido_id        UUID        NOT NULL REFERENCES vet_barbarafonseca.pedidos_compra(id),
    numero_nf        TEXT,
    data_recebimento DATE        NOT NULL,
    status           TEXT        NOT NULL DEFAULT 'pendente',
    observacoes      TEXT,
    criado_por       UUID,
    criado_em        TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS vet_barbarafonseca.recebimento_itens (
    id                  UUID          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    recebimento_id      UUID          NOT NULL REFERENCES vet_barbarafonseca.recebimentos_mercadoria(id),
    pedido_item_id      UUID,
    produto_id          UUID          REFERENCES vet_barbarafonseca.produtos(id),
    nome_produto        TEXT,
    quantidade_pedida   NUMERIC(12,2) NOT NULL DEFAULT 0,
    quantidade_recebida NUMERIC(12,2) NOT NULL DEFAULT 0,
    valor_unitario      NUMERIC(12,2) NOT NULL DEFAULT 0,
    uso                 TEXT,
    criado_em           TIMESTAMPTZ   NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS vet_barbarafonseca.recebimento_anexos (
    id             UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id      UUID        NOT NULL,
    recebimento_id UUID        NOT NULL REFERENCES vet_barbarafonseca.recebimentos_mercadoria(id),
    nome_arquivo   TEXT        NOT NULL,
    url            TEXT        NOT NULL,
    criado_em      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS vet_barbarafonseca.planos_saude (
    id              UUID          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id       UUID          NOT NULL,
    nome            TEXT          NOT NULL,
    operadora       TEXT,
    desconto_padrao NUMERIC(5,2)  NOT NULL DEFAULT 0,
    ativo           BOOLEAN       NOT NULL DEFAULT TRUE,
    criado_em       TIMESTAMPTZ   NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS vet_barbarafonseca.pet_planos (
    id              UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id       UUID        NOT NULL,
    pet_id          UUID        NOT NULL REFERENCES vet_barbarafonseca.pets(id),
    plano_id        UUID        NOT NULL REFERENCES vet_barbarafonseca.planos_saude(id),
    numero_carteira TEXT,
    data_inicio     DATE,
    data_fim        DATE,
    ativo           BOOLEAN     NOT NULL DEFAULT TRUE,
    criado_em       TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS vet_barbarafonseca.tutor_planos (
    id              UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id       UUID        NOT NULL,
    tutor_id        UUID        NOT NULL REFERENCES vet_barbarafonseca.tutores(id),
    plano_id        UUID        NOT NULL REFERENCES vet_barbarafonseca.planos_saude(id),
    numero_carteira TEXT,
    data_inicio     DATE,
    ativo           BOOLEAN     NOT NULL DEFAULT TRUE,
    criado_em       TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS vet_barbarafonseca.fechamentos_contabeis (
    id            UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id     UUID        NOT NULL,
    ano           INT         NOT NULL,
    mes           INT         NOT NULL,
    status        TEXT        NOT NULL DEFAULT 'aberto',
    enviado_em    TIMESTAMPTZ,
    enviado_por   UUID,
    email_destino TEXT,
    obs           TEXT,
    criado_em     TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS vet_barbarafonseca.fechamento_documentos (
    id            UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id     UUID        NOT NULL,
    fechamento_id UUID        NOT NULL REFERENCES vet_barbarafonseca.fechamentos_contabeis(id),
    tipo          TEXT        NOT NULL,
    nome          TEXT        NOT NULL,
    descricao     TEXT,
    tipo_arquivo  TEXT        NOT NULL,
    tamanho_bytes INT         NOT NULL DEFAULT 0,
    dados_base64  TEXT        NOT NULL,
    origem        TEXT,
    criado_por    UUID,
    criado_em     TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS vet_barbarafonseca.bot_config (
    id                   UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id            UUID        NOT NULL,
    ativo                BOOLEAN     NOT NULL DEFAULT FALSE,
    meta_token           TEXT,
    meta_phone_number_id TEXT,
    meta_waba_id         TEXT,
    webhook_verify_token TEXT,
    criado_em            TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS vet_barbarafonseca.bot_conversas (
    id            UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id     UUID        NOT NULL,
    telefone      TEXT        NOT NULL,
    estado        TEXT        NOT NULL DEFAULT 'inicio',
    contexto      TEXT,
    criado_em     TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    atualizado_em TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS vet_barbarafonseca.bot_logs (
    id          UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id   UUID        NOT NULL,
    conversa_id UUID        REFERENCES vet_barbarafonseca.bot_conversas(id),
    direcao     TEXT        NOT NULL,
    mensagem    TEXT,
    criado_em   TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS vet_barbarafonseca.config_mensagens (
    id        UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id UUID        NOT NULL,
    gatilho   TEXT        NOT NULL,
    canal     TEXT        NOT NULL DEFAULT 'whatsapp',
    ativo     BOOLEAN     NOT NULL DEFAULT FALSE,
    template  TEXT,
    criado_em TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS vet_barbarafonseca.campanhas (
    id                  UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id           UUID        NOT NULL,
    titulo              TEXT        NOT NULL,
    mensagem            TEXT,
    imagem_url          TEXT,
    publico             TEXT        NOT NULL DEFAULT 'todos',
    canal               TEXT        NOT NULL DEFAULT 'whatsapp',
    status              TEXT        NOT NULL DEFAULT 'aguardando_aprovacao',
    total_destinatarios INT         NOT NULL DEFAULT 0,
    total_enviados      INT         NOT NULL DEFAULT 0,
    motivo_reprovacao   TEXT,
    criado_por          UUID,
    aprovado_por        UUID,
    aprovado_em         TIMESTAMPTZ,
    enviada_em          TIMESTAMPTZ,
    criado_em           TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS vet_barbarafonseca.notificacoes_fila (
    id            UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id     UUID        NOT NULL,
    tutor_id      UUID        REFERENCES vet_barbarafonseca.tutores(id),
    canal         TEXT        NOT NULL DEFAULT 'whatsapp',
    tipo          TEXT        NOT NULL,
    destinatario  TEXT        NOT NULL,
    payload       TEXT,
    status        TEXT        NOT NULL DEFAULT 'pendente',
    tentativas    INT         NOT NULL DEFAULT 0,
    agendado_para TIMESTAMPTZ,
    processado_em TIMESTAMPTZ,
    criado_em     TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS vet_barbarafonseca.metas_faturamento (
    id         UUID          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id  UUID          NOT NULL,
    ano        INT           NOT NULL,
    mes        INT           NOT NULL,
    valor_meta NUMERIC(12,2) NOT NULL DEFAULT 0,
    criado_em  TIMESTAMPTZ   NOT NULL DEFAULT NOW()
);

-- ============================================================
-- PASSO 4: Migra dados do schema public para vet_barbarafonseca
-- ============================================================

INSERT INTO vet_barbarafonseca.tenants
    SELECT id, nome, plano, telefone, email, logo_url, tagline, cor_primaria,
           suspenso_em, ativo, criado_em, NULL, whatsapp_number, whatsapp_token
    FROM public.tenants
    WHERE id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

INSERT INTO vet_barbarafonseca.users (id, tenant_id, nome, email, senha_hash, papel, ativo, criado_em)
    SELECT id, tenant_id, nome, email, senha_hash, papel, ativo, criado_em
    FROM public.users
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

INSERT INTO vet_barbarafonseca.racas (id, tenant_id, nome, especie, ativo)
    SELECT id, tenant_id, nome, especie, ativo
    FROM public.racas
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

INSERT INTO vet_barbarafonseca.pelagens (id, tenant_id, nome, ativo)
    SELECT id, tenant_id, nome, ativo
    FROM public.pelagens
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

INSERT INTO vet_barbarafonseca.tutores
    SELECT * FROM public.tutores
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

INSERT INTO vet_barbarafonseca.pets
    SELECT * FROM public.pets
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

INSERT INTO vet_barbarafonseca.servicos
    SELECT * FROM public.servicos
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

INSERT INTO vet_barbarafonseca.vias_administracao (id, tenant_id, nome, ativo)
    SELECT id, tenant_id, nome, ativo
    FROM public.vias_administracao
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

INSERT INTO vet_barbarafonseca.agendamentos
    SELECT * FROM public.agendamentos
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

INSERT INTO vet_barbarafonseca.ordens_servico
    SELECT * FROM public.ordens_servico
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

INSERT INTO vet_barbarafonseca.os_servicos
    SELECT oss.* FROM public.os_servicos oss
    JOIN public.ordens_servico os ON oss.os_id = os.id
    WHERE os.tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

INSERT INTO vet_barbarafonseca.prontuario_itens
    SELECT * FROM public.prontuario_itens
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

INSERT INTO vet_barbarafonseca.vacinacoes
    SELECT * FROM public.vacinacoes
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

INSERT INTO vet_barbarafonseca.produtos
    SELECT * FROM public.produtos
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

INSERT INTO vet_barbarafonseca.movimentacoes_estoque
    SELECT * FROM public.movimentacoes_estoque
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

INSERT INTO vet_barbarafonseca.caixas
    SELECT * FROM public.caixas
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

INSERT INTO vet_barbarafonseca.movimentacoes_caixa
    SELECT * FROM public.movimentacoes_caixa
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

INSERT INTO vet_barbarafonseca.vendas
    SELECT * FROM public.vendas
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

INSERT INTO vet_barbarafonseca.venda_itens
    SELECT vi.* FROM public.venda_itens vi
    JOIN public.vendas v ON vi.venda_id = v.id
    WHERE v.tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

INSERT INTO vet_barbarafonseca.categorias_financeiras
    SELECT * FROM public.categorias_financeiras
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

INSERT INTO vet_barbarafonseca.contas
    SELECT * FROM public.contas
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

INSERT INTO vet_barbarafonseca.conta_parcelas
    SELECT cp.* FROM public.conta_parcelas cp
    JOIN public.contas c ON cp.conta_id = c.id
    WHERE c.tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

INSERT INTO vet_barbarafonseca.contas_bancarias
    SELECT * FROM public.contas_bancarias
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

INSERT INTO vet_barbarafonseca.movimentacoes_bancarias
    SELECT * FROM public.movimentacoes_bancarias
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

INSERT INTO vet_barbarafonseca.cargos
    SELECT * FROM public.cargos
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

INSERT INTO vet_barbarafonseca.funcionarios
    SELECT * FROM public.funcionarios
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

INSERT INTO vet_barbarafonseca.comissoes
    SELECT * FROM public.comissoes
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

INSERT INTO vet_barbarafonseca.fechamento_mensal
    SELECT * FROM public.fechamento_mensal
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

INSERT INTO vet_barbarafonseca.parametros_sistema
    SELECT * FROM public.parametros_sistema
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

INSERT INTO vet_barbarafonseca.fornecedores
    SELECT * FROM public.fornecedores
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

INSERT INTO vet_barbarafonseca.condicoes_pagamento
    SELECT * FROM public.condicoes_pagamento
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

INSERT INTO vet_barbarafonseca.pedidos_compra
    SELECT * FROM public.pedidos_compra
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

INSERT INTO vet_barbarafonseca.pedido_itens
    SELECT pi.* FROM public.pedido_itens pi
    JOIN public.pedidos_compra pc ON pi.pedido_id = pc.id
    WHERE pc.tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

INSERT INTO vet_barbarafonseca.recebimentos_mercadoria
    SELECT * FROM public.recebimentos_mercadoria
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

INSERT INTO vet_barbarafonseca.recebimento_itens
    SELECT ri.* FROM public.recebimento_itens ri
    JOIN public.recebimentos_mercadoria rm ON ri.recebimento_id = rm.id
    WHERE rm.tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

INSERT INTO vet_barbarafonseca.recebimento_anexos
    SELECT * FROM public.recebimento_anexos
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

INSERT INTO vet_barbarafonseca.planos_saude
    SELECT * FROM public.planos_saude
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

INSERT INTO vet_barbarafonseca.pet_planos
    SELECT * FROM public.pet_planos
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

INSERT INTO vet_barbarafonseca.tutor_planos
    SELECT * FROM public.tutor_planos
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

INSERT INTO vet_barbarafonseca.fechamentos_contabeis
    SELECT * FROM public.fechamentos_contabeis
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

INSERT INTO vet_barbarafonseca.fechamento_documentos
    SELECT fd.* FROM public.fechamento_documentos fd
    JOIN public.fechamentos_contabeis fc ON fd.fechamento_id = fc.id
    WHERE fc.tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

INSERT INTO vet_barbarafonseca.bot_config
    SELECT * FROM public.bot_config
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

INSERT INTO vet_barbarafonseca.bot_conversas
    SELECT * FROM public.bot_conversas
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

INSERT INTO vet_barbarafonseca.bot_logs
    SELECT * FROM public.bot_logs
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

INSERT INTO vet_barbarafonseca.config_mensagens
    SELECT * FROM public.config_mensagens
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

INSERT INTO vet_barbarafonseca.campanhas
    SELECT * FROM public.campanhas
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

INSERT INTO vet_barbarafonseca.notificacoes_fila
    SELECT * FROM public.notificacoes_fila
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

INSERT INTO vet_barbarafonseca.metas_faturamento
    SELECT * FROM public.metas_faturamento
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- ============================================================
-- VERIFICACAO FINAL
-- ============================================================
SELECT 'platform.tenants'                    , COUNT(*) FROM platform.tenants
UNION ALL SELECT 'vet_barbarafonseca.users'  , COUNT(*) FROM vet_barbarafonseca.users
UNION ALL SELECT 'vet_barbarafonseca.tutores', COUNT(*) FROM vet_barbarafonseca.tutores
UNION ALL SELECT 'vet_barbarafonseca.pets'   , COUNT(*) FROM vet_barbarafonseca.pets
UNION ALL SELECT 'vet_barbarafonseca.contas' , COUNT(*) FROM vet_barbarafonseca.contas
UNION ALL SELECT 'vet_barbarafonseca.vendas' , COUNT(*) FROM vet_barbarafonseca.vendas;
