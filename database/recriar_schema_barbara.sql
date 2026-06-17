-- ============================================================
-- Recria vet_barbarafonseca com estrutura IDENTICA ao public
-- e migra todos os dados corretamente
-- ============================================================

-- Drop e recria o schema limpo
DROP SCHEMA IF EXISTS vet_barbarafonseca CASCADE;
CREATE SCHEMA vet_barbarafonseca;

-- ── TABELAS ──────────────────────────────────────────────────

CREATE TABLE vet_barbarafonseca.tenants (
    id           UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    nome         VARCHAR(120) NOT NULL,
    plano        VARCHAR(20)  NOT NULL DEFAULT 'trial',
    telefone     VARCHAR(20),
    email        VARCHAR(120),
    whatsapp_number VARCHAR(20),
    whatsapp_token  TEXT,
    criado_em    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    ativo        BOOLEAN     NOT NULL DEFAULT TRUE,
    logo_url     TEXT,
    tagline      VARCHAR(160),
    cor_primaria VARCHAR(9)  DEFAULT '#0f172a',
    suspenso_em  TIMESTAMPTZ,
    schema_name  TEXT
);

CREATE TABLE vet_barbarafonseca.users (
    id         UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id  UUID        NOT NULL,
    email      VARCHAR(120) NOT NULL,
    nome       VARCHAR(120) NOT NULL,
    senha_hash TEXT        NOT NULL,
    papel      VARCHAR(30)  NOT NULL DEFAULT 'recepcionista',
    ativo      BOOLEAN     NOT NULL DEFAULT TRUE,
    criado_em  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE vet_barbarafonseca.cargos (
    id               UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id        UUID        NOT NULL,
    nome             VARCHAR(100) NOT NULL,
    pode_receituario BOOLEAN     NOT NULL DEFAULT FALSE,
    ativo            BOOLEAN     NOT NULL DEFAULT TRUE,
    criado_em        TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE vet_barbarafonseca.racas (
    id        UUID    NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id UUID    NOT NULL,
    nome      TEXT    NOT NULL,
    especie   TEXT    NOT NULL DEFAULT 'cao',
    ativo     BOOLEAN NOT NULL DEFAULT TRUE,
    criado_em TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE vet_barbarafonseca.pelagens (
    id        UUID    NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id UUID    NOT NULL,
    nome      VARCHAR(100) NOT NULL,
    ativo     BOOLEAN NOT NULL DEFAULT TRUE,
    criado_em TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE vet_barbarafonseca.tutores (
    id               UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id        UUID        NOT NULL,
    nome             VARCHAR(120) NOT NULL,
    cpf_cnpj         VARCHAR(18),
    telefone         VARCHAR(20),
    email            VARCHAR(120),
    endereco         TEXT,
    obs              TEXT,
    pontos           INT         NOT NULL DEFAULT 0,
    ativo            BOOLEAN     NOT NULL DEFAULT TRUE,
    criado_em        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    aniversario_dia  SMALLINT,
    aniversario_mes  SMALLINT,
    aceita_promocoes BOOLEAN     NOT NULL DEFAULT TRUE,
    logradouro       VARCHAR(200),
    num_end          VARCHAR(20),
    complemento      VARCHAR(100),
    bairro           VARCHAR(100),
    cidade           VARCHAR(100),
    estado           CHAR(2),
    cep              VARCHAR(9)
);

CREATE TABLE vet_barbarafonseca.pets (
    id                  UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id           UUID        NOT NULL,
    tutor_id            UUID        NOT NULL REFERENCES vet_barbarafonseca.tutores(id),
    nome                VARCHAR(80)  NOT NULL,
    especie             VARCHAR(20)  NOT NULL DEFAULT 'cao',
    raca                VARCHAR(80),
    sexo                VARCHAR(20)  NOT NULL DEFAULT 'indefinido',
    data_nascimento     DATE,
    pelagem             VARCHAR(80),
    peso_kg             NUMERIC,
    castrado            BOOLEAN,
    cor                 VARCHAR(60),
    foto_url            TEXT,
    obs                 TEXT,
    ativo               BOOLEAN     NOT NULL DEFAULT TRUE,
    criado_em           TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    tem_microchip       BOOLEAN     DEFAULT FALSE,
    microchip_num       VARCHAR(50),
    tem_plano_saude     BOOLEAN     DEFAULT FALSE,
    plano_saude_nome    VARCHAR(100),
    plano_saude_carteira VARCHAR(50),
    codigo_sequencial   INT
);

CREATE TABLE vet_barbarafonseca.servicos (
    id          UUID         NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id   UUID         NOT NULL,
    nome        VARCHAR(120) NOT NULL,
    categoria   VARCHAR(60),
    descricao   TEXT,
    preco_base  NUMERIC      NOT NULL DEFAULT 0,
    duracao_min INT,
    ativo       BOOLEAN      NOT NULL DEFAULT TRUE,
    icone       VARCHAR(10)  DEFAULT 'pata'
);

CREATE TABLE vet_barbarafonseca.vias_administracao (
    id        UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id UUID        NOT NULL,
    nome      VARCHAR(100) NOT NULL,
    ativo     BOOLEAN     NOT NULL DEFAULT TRUE,
    criado_em TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE vet_barbarafonseca.planos_saude (
    id               UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id        UUID        NOT NULL,
    nome             TEXT        NOT NULL,
    operadora        TEXT,
    desconto_percent NUMERIC     NOT NULL DEFAULT 0,
    ativo            BOOLEAN     NOT NULL DEFAULT TRUE,
    obs              TEXT,
    criado_em        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    atualizado_em    TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE vet_barbarafonseca.pet_planos (
    id               UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id        UUID        NOT NULL,
    pet_id           UUID        NOT NULL REFERENCES vet_barbarafonseca.pets(id),
    plano_id         UUID        NOT NULL REFERENCES vet_barbarafonseca.planos_saude(id),
    num_carteirinha  TEXT,
    validade         DATE,
    desconto_percent NUMERIC,
    ativo            BOOLEAN     NOT NULL DEFAULT TRUE,
    criado_em        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    atualizado_em    TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE vet_barbarafonseca.tutor_planos (
    id               UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id        UUID        NOT NULL,
    tutor_id         UUID        NOT NULL REFERENCES vet_barbarafonseca.tutores(id),
    plano_id         UUID        NOT NULL REFERENCES vet_barbarafonseca.planos_saude(id),
    num_carteirinha  TEXT,
    validade         DATE,
    desconto_percent NUMERIC,
    ativo            BOOLEAN     NOT NULL DEFAULT TRUE,
    criado_em        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    atualizado_em    TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE vet_barbarafonseca.agendamentos (
    id             UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id      UUID        NOT NULL,
    pet_id         UUID        NOT NULL REFERENCES vet_barbarafonseca.pets(id),
    user_id        UUID        REFERENCES vet_barbarafonseca.users(id),
    tipo           VARCHAR(30) NOT NULL,
    data_hora      TIMESTAMPTZ NOT NULL,
    duracao_min    INT         NOT NULL DEFAULT 60,
    status         VARCHAR(30) NOT NULL DEFAULT 'pendente',
    origem         VARCHAR(30) NOT NULL DEFAULT 'interno',
    obs            TEXT,
    confirmado_em  TIMESTAMPTZ,
    confirmado_por UUID,
    criado_em      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    servico_id     UUID        REFERENCES vet_barbarafonseca.servicos(id)
);

CREATE TABLE vet_barbarafonseca.ordens_servico (
    id               UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id        UUID        NOT NULL,
    agendamento_id   UUID        REFERENCES vet_barbarafonseca.agendamentos(id),
    pet_id           UUID        NOT NULL REFERENCES vet_barbarafonseca.pets(id),
    user_id          UUID        REFERENCES vet_barbarafonseca.users(id),
    status           VARCHAR(30) NOT NULL DEFAULT 'aguardando',
    obs              TEXT,
    fotos_urls       TEXT[],
    valor_total      NUMERIC,
    inicio           TIMESTAMPTZ,
    fim              TIMESTAMPTZ,
    entregue_em      TIMESTAMPTZ,
    criado_em        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    funcionario_id   UUID,
    plano_id         UUID,
    desconto_percent NUMERIC,
    valor_sem_desconto NUMERIC
);

CREATE TABLE vet_barbarafonseca.os_servicos (
    id            UUID    NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    os_id         UUID    NOT NULL REFERENCES vet_barbarafonseca.ordens_servico(id),
    servico_id    UUID    NOT NULL REFERENCES vet_barbarafonseca.servicos(id),
    preco_cobrado NUMERIC NOT NULL,
    obs           TEXT
);

CREATE TABLE vet_barbarafonseca.prontuario_itens (
    id          UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    pet_id      UUID        NOT NULL REFERENCES vet_barbarafonseca.pets(id),
    user_id     UUID        REFERENCES vet_barbarafonseca.users(id),
    data        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    tipo        VARCHAR(30) NOT NULL,
    titulo      VARCHAR(120),
    descricao   TEXT        NOT NULL,
    anexo_url   TEXT,
    criado_em   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    motivo      TEXT,
    receituario TEXT
);

CREATE TABLE vet_barbarafonseca.vacinacoes (
    id             UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    pet_id         UUID        NOT NULL REFERENCES vet_barbarafonseca.pets(id),
    user_id        UUID        REFERENCES vet_barbarafonseca.users(id),
    vacina         VARCHAR(120) NOT NULL,
    fabricante     VARCHAR(80),
    lote           VARCHAR(40),
    data_aplicacao DATE        NOT NULL,
    proxima_dose   DATE,
    obs            TEXT,
    criado_em      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE vet_barbarafonseca.links_agendamento (
    id        UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id UUID        NOT NULL,
    tutor_id  UUID        NOT NULL REFERENCES vet_barbarafonseca.tutores(id),
    pet_id    UUID        REFERENCES vet_barbarafonseca.pets(id),
    token     VARCHAR(64) NOT NULL,
    expira_em TIMESTAMPTZ NOT NULL,
    usado_em  TIMESTAMPTZ,
    criado_em TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE vet_barbarafonseca.categorias_financeiras (
    id        UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id UUID        NOT NULL,
    nome      VARCHAR(80) NOT NULL,
    tipo      VARCHAR(20) NOT NULL,
    ativo     BOOLEAN     NOT NULL DEFAULT TRUE
);

CREATE TABLE vet_barbarafonseca.contas_bancarias (
    id            UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id     UUID        NOT NULL,
    nome          VARCHAR(100) NOT NULL,
    banco         VARCHAR(60),
    agencia       VARCHAR(10),
    conta         VARCHAR(20),
    tipo          VARCHAR(20)  NOT NULL DEFAULT 'corrente',
    saldo_inicial NUMERIC      NOT NULL DEFAULT 0,
    ativo         BOOLEAN      NOT NULL DEFAULT TRUE,
    criado_em     TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

CREATE TABLE vet_barbarafonseca.contas (
    id               UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id        UUID        NOT NULL,
    os_id            UUID,
    venda_id         UUID,
    pedido_id        UUID,
    tipo             VARCHAR(20) NOT NULL DEFAULT 'receita',
    categoria_id     UUID        REFERENCES vet_barbarafonseca.categorias_financeiras(id),
    descricao        TEXT        NOT NULL DEFAULT '',
    valor            NUMERIC     NOT NULL DEFAULT 0,
    data_competencia DATE        NOT NULL,
    data_vencimento  DATE        NOT NULL,
    forma_pagamento  VARCHAR(30),
    status           VARCHAR(20) NOT NULL DEFAULT 'aberta',
    valor_pago       NUMERIC,
    data_baixa       DATE,
    conta_bancaria   VARCHAR(100),
    obs_baixa        TEXT,
    criado_por       UUID,
    criado_em        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    atualizado_em    TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE vet_barbarafonseca.lancamentos (
    id           UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id    UUID        NOT NULL,
    os_id        UUID,
    categoria_id UUID        REFERENCES vet_barbarafonseca.categorias_financeiras(id),
    data         DATE        NOT NULL,
    tipo         VARCHAR(20) NOT NULL,
    valor        NUMERIC     NOT NULL,
    descricao    TEXT,
    criado_em    TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE vet_barbarafonseca.movimentacoes_bancarias (
    id                UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id         UUID        NOT NULL,
    conta_bancaria_id UUID        NOT NULL REFERENCES vet_barbarafonseca.contas_bancarias(id),
    tipo              VARCHAR(10) NOT NULL,
    valor             NUMERIC     NOT NULL,
    descricao         VARCHAR(200) NOT NULL,
    data_movimentacao DATE        NOT NULL,
    categoria_id      UUID,
    conta_id          UUID,
    conta_destino_id  UUID,
    conciliado        BOOLEAN     NOT NULL DEFAULT FALSE,
    criado_por        UUID,
    criado_em         TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    origem            VARCHAR(50)
);

CREATE TABLE vet_barbarafonseca.conciliacoes_diarias (
    id                UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id         UUID        NOT NULL,
    conta_bancaria_id UUID        NOT NULL REFERENCES vet_barbarafonseca.contas_bancarias(id),
    data_conciliacao  DATE        NOT NULL,
    saldo_anterior    NUMERIC     NOT NULL DEFAULT 0,
    total_entradas    NUMERIC     NOT NULL DEFAULT 0,
    total_saidas      NUMERIC     NOT NULL DEFAULT 0,
    saldo_final       NUMERIC     NOT NULL DEFAULT 0,
    saldo_extrato     NUMERIC,
    diferenca         NUMERIC,
    observacao        TEXT,
    fechado_por       UUID,
    fechado_em        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    criado_em         TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_conciliacao_barbara UNIQUE (conta_bancaria_id, data_conciliacao)
);

CREATE TABLE vet_barbarafonseca.produtos (
    id                UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id         UUID        NOT NULL,
    nome              VARCHAR(150) NOT NULL,
    codigo_barras     VARCHAR(60),
    categoria         VARCHAR(60),
    unidade           VARCHAR(20) DEFAULT 'un',
    preco_custo       NUMERIC     NOT NULL DEFAULT 0,
    preco_venda       NUMERIC     NOT NULL DEFAULT 0,
    estoque_atual     NUMERIC     NOT NULL DEFAULT 0,
    estoque_minimo    NUMERIC     NOT NULL DEFAULT 0,
    estoque_ideal     NUMERIC,
    controla_validade BOOLEAN     NOT NULL DEFAULT FALSE,
    ativo             BOOLEAN     NOT NULL DEFAULT TRUE,
    criado_em         TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    codigo            VARCHAR(30)
);

CREATE TABLE vet_barbarafonseca.movimentacoes_estoque (
    id             UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id      UUID        NOT NULL,
    produto_id     UUID        NOT NULL REFERENCES vet_barbarafonseca.produtos(id),
    tipo           VARCHAR(30) NOT NULL,
    quantidade     NUMERIC     NOT NULL,
    estoque_apos   NUMERIC     NOT NULL,
    custo_unitario NUMERIC,
    motivo         TEXT,
    venda_id       UUID,
    user_id        UUID,
    data           TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE vet_barbarafonseca.caixas (
    id               UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id        UUID        NOT NULL,
    user_id          UUID        REFERENCES vet_barbarafonseca.users(id),
    data             DATE        NOT NULL DEFAULT CURRENT_DATE,
    saldo_inicial    NUMERIC     NOT NULL DEFAULT 0,
    total_vendas     NUMERIC     NOT NULL DEFAULT 0,
    total_servicos   NUMERIC     NOT NULL DEFAULT 0,
    saldo_final      NUMERIC,
    aberto_em        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fechado_em       TIMESTAMPTZ,
    obs              TEXT,
    requer_aprovacao BOOLEAN     DEFAULT FALSE,
    aprovado_por     UUID,
    aprovado_em      TIMESTAMPTZ
);

CREATE TABLE vet_barbarafonseca.movimentacoes_caixa (
    id        UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id UUID        NOT NULL,
    caixa_id  UUID        NOT NULL REFERENCES vet_barbarafonseca.caixas(id),
    user_id   UUID        REFERENCES vet_barbarafonseca.users(id),
    tipo      VARCHAR(10) NOT NULL,
    valor     NUMERIC     NOT NULL,
    descricao TEXT,
    criado_em TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE vet_barbarafonseca.vendas (
    id               UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id        UUID        NOT NULL,
    tutor_id         UUID        REFERENCES vet_barbarafonseca.tutores(id),
    user_id          UUID        REFERENCES vet_barbarafonseca.users(id),
    status           VARCHAR(20) NOT NULL DEFAULT 'aberta',
    valor_produtos   NUMERIC     NOT NULL DEFAULT 0,
    valor_desconto   NUMERIC     NOT NULL DEFAULT 0,
    valor_total      NUMERIC     NOT NULL DEFAULT 0,
    forma_pagamento  VARCHAR(40),
    finalizada_em    TIMESTAMPTZ,
    criado_em        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    plano_id         UUID,
    desconto_percent NUMERIC,
    valor_sem_desconto NUMERIC
);

CREATE TABLE vet_barbarafonseca.venda_itens (
    id             UUID    NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    venda_id       UUID    NOT NULL REFERENCES vet_barbarafonseca.vendas(id),
    produto_id     UUID    NOT NULL REFERENCES vet_barbarafonseca.produtos(id),
    quantidade     NUMERIC NOT NULL,
    preco_unitario NUMERIC NOT NULL,
    subtotal       NUMERIC NOT NULL
);

CREATE TABLE vet_barbarafonseca.funcionarios (
    id                  UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id           UUID        NOT NULL,
    nome                VARCHAR(150) NOT NULL,
    cpf                 VARCHAR(14),
    rg                  VARCHAR(20),
    data_nascimento     DATE,
    logradouro          VARCHAR(200),
    numero              VARCHAR(10),
    complemento         VARCHAR(100),
    bairro              VARCHAR(100),
    cidade              VARCHAR(100),
    estado              CHAR(2),
    cep                 VARCHAR(9),
    telefone            VARCHAR(20),
    email               VARCHAR(150),
    cargo               VARCHAR(100),
    data_admissao       DATE,
    salario             NUMERIC     DEFAULT 0,
    percentual_comissao NUMERIC     DEFAULT 0,
    status              VARCHAR(20) NOT NULL DEFAULT 'trabalhando',
    criado_em           TIMESTAMPTZ DEFAULT NOW(),
    atualizado_em       TIMESTAMPTZ DEFAULT NOW(),
    crmv                VARCHAR(30),
    registro_mapa       VARCHAR(30),
    codigo              VARCHAR(30),
    assina_receituario  BOOLEAN     NOT NULL DEFAULT FALSE,
    cargo_id            UUID        REFERENCES vet_barbarafonseca.cargos(id),
    usuario_id          UUID        REFERENCES vet_barbarafonseca.users(id)
);

CREATE TABLE vet_barbarafonseca.comissoes (
    id             UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id      UUID        NOT NULL,
    funcionario_id UUID        NOT NULL REFERENCES vet_barbarafonseca.funcionarios(id),
    origem         VARCHAR(10) NOT NULL,
    origem_id      UUID        NOT NULL,
    valor_base     NUMERIC     NOT NULL,
    percentual     NUMERIC     NOT NULL,
    valor_comissao NUMERIC     NOT NULL,
    status         VARCHAR(20) NOT NULL DEFAULT 'pendente',
    fechamento_id  UUID,
    competencia    DATE        NOT NULL,
    criado_em      TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE vet_barbarafonseca.fechamento_mensal (
    id             UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id      UUID        NOT NULL,
    funcionario_id UUID        NOT NULL REFERENCES vet_barbarafonseca.funcionarios(id),
    competencia    DATE        NOT NULL,
    salario        NUMERIC     NOT NULL,
    total_comissoes NUMERIC    NOT NULL DEFAULT 0,
    total_pagar    NUMERIC     NOT NULL,
    status         VARCHAR(20) NOT NULL DEFAULT 'aberto',
    observacoes    TEXT,
    fechado_em     TIMESTAMPTZ,
    fechado_por    UUID,
    criado_em      TIMESTAMPTZ DEFAULT NOW(),
    CONSTRAINT uq_fechamento_barbara UNIQUE (funcionario_id, competencia)
);

CREATE TABLE vet_barbarafonseca.parametros_sistema (
    id                   UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id            UUID        NOT NULL UNIQUE,
    comissao_os_ativo    BOOLEAN     NOT NULL DEFAULT TRUE,
    comissao_pdv_ativo   BOOLEAN     NOT NULL DEFAULT FALSE,
    criado_em            TIMESTAMPTZ DEFAULT NOW(),
    atualizado_em        TIMESTAMPTZ DEFAULT NOW(),
    anthropic_api_key    VARCHAR(200),
    ia_ativo             BOOLEAN     NOT NULL DEFAULT FALSE,
    cert_pfx_encrypted   TEXT,
    cert_senha_hash      TEXT,
    cert_tipo            VARCHAR(10),
    cert_titular         VARCHAR(200),
    cert_cpf             VARCHAR(20),
    cert_validade        DATE,
    cert_ativo           BOOLEAN     NOT NULL DEFAULT FALSE,
    cert_atualizado_em   TIMESTAMPTZ,
    smtp_host            VARCHAR(200),
    smtp_porta           INT         DEFAULT 587,
    smtp_usuario         VARCHAR(200),
    smtp_senha_enc       TEXT,
    smtp_ssl             BOOLEAN     NOT NULL DEFAULT TRUE,
    email_remetente      VARCHAR(200),
    email_contabil       VARCHAR(200),
    contabil_nome        VARCHAR(200)
);

CREATE TABLE vet_barbarafonseca.fornecedores (
    id        UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id UUID        NOT NULL,
    nome      VARCHAR(150) NOT NULL,
    cnpj      VARCHAR(18),
    telefone  VARCHAR(20),
    email     VARCHAR(100),
    contato   VARCHAR(100),
    logradouro VARCHAR(200),
    num_end   VARCHAR(20),
    bairro    VARCHAR(100),
    cidade    VARCHAR(100),
    estado    CHAR(2),
    cep       VARCHAR(9),
    obs       TEXT,
    ativo     BOOLEAN     NOT NULL DEFAULT TRUE,
    criado_em TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    codigo    VARCHAR(30)
);

CREATE TABLE vet_barbarafonseca.condicoes_pagamento (
    id             UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id      UUID        NOT NULL,
    nome           VARCHAR(100) NOT NULL,
    parcelas       INT         NOT NULL DEFAULT 1,
    intervalo_dias INT         NOT NULL DEFAULT 30,
    ativo          BOOLEAN     NOT NULL DEFAULT TRUE,
    criado_em      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE SEQUENCE IF NOT EXISTS vet_barbarafonseca.pedidos_compra_numero_seq;

CREATE TABLE vet_barbarafonseca.pedidos_compra (
    id                    UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id             UUID        NOT NULL,
    fornecedor_id         UUID        REFERENCES vet_barbarafonseca.fornecedores(id),
    numero                INT         NOT NULL DEFAULT nextval('vet_barbarafonseca.pedidos_compra_numero_seq'),
    status                VARCHAR(20) NOT NULL DEFAULT 'rascunho',
    data_pedido           DATE        NOT NULL DEFAULT CURRENT_DATE,
    data_recebimento      DATE,
    valor_total           NUMERIC     NOT NULL DEFAULT 0,
    obs                   TEXT,
    criado_por            UUID,
    criado_em             TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    atualizado_em         TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    condicao_pagamento_id UUID        REFERENCES vet_barbarafonseca.condicoes_pagamento(id),
    parcelas              INT         NOT NULL DEFAULT 1,
    forma_pagamento       VARCHAR(30) DEFAULT 'boleto'
);

CREATE TABLE vet_barbarafonseca.pedido_itens (
    id             UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    pedido_id      UUID        NOT NULL REFERENCES vet_barbarafonseca.pedidos_compra(id),
    produto_id     UUID        REFERENCES vet_barbarafonseca.produtos(id),
    nome_produto   VARCHAR(200) NOT NULL,
    quantidade     NUMERIC     NOT NULL,
    unidade        VARCHAR(20) NOT NULL DEFAULT 'un',
    valor_unitario NUMERIC     NOT NULL,
    valor_total    NUMERIC     NOT NULL,
    uso            VARCHAR(20) NOT NULL DEFAULT 'venda',
    criado_em      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE vet_barbarafonseca.recebimentos_mercadoria (
    id               UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id        UUID        NOT NULL,
    pedido_id        UUID        REFERENCES vet_barbarafonseca.pedidos_compra(id),
    data_recebimento DATE        NOT NULL DEFAULT CURRENT_DATE,
    status           VARCHAR(20) NOT NULL DEFAULT 'pendente',
    obs              TEXT,
    criado_por       UUID,
    criado_em        TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE vet_barbarafonseca.recebimento_itens (
    id                  UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    recebimento_id      UUID        NOT NULL REFERENCES vet_barbarafonseca.recebimentos_mercadoria(id),
    pedido_item_id      UUID,
    produto_id          UUID        REFERENCES vet_barbarafonseca.produtos(id),
    nome_produto        VARCHAR(200) NOT NULL,
    quantidade_pedida   NUMERIC     NOT NULL DEFAULT 0,
    quantidade_recebida NUMERIC     NOT NULL DEFAULT 0,
    valor_unitario      NUMERIC     NOT NULL DEFAULT 0,
    uso                 VARCHAR(20) NOT NULL DEFAULT 'venda',
    criado_em           TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE vet_barbarafonseca.recebimento_anexos (
    id             UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id      UUID        NOT NULL,
    recebimento_id UUID        NOT NULL REFERENCES vet_barbarafonseca.recebimentos_mercadoria(id),
    nome           VARCHAR(255) NOT NULL,
    tipo_arquivo   VARCHAR(100) NOT NULL,
    tamanho_bytes  INT         NOT NULL,
    dados_base64   TEXT        NOT NULL,
    descricao      VARCHAR(255),
    criado_por     UUID,
    criado_em      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE vet_barbarafonseca.fechamentos_contabeis (
    id            UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id     UUID        NOT NULL,
    ano           INT         NOT NULL,
    mes           INT         NOT NULL,
    status        VARCHAR(30) NOT NULL DEFAULT 'aberto',
    enviado_em    TIMESTAMPTZ,
    enviado_por   UUID,
    email_destino VARCHAR(200),
    obs           TEXT,
    criado_em     TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE vet_barbarafonseca.fechamento_documentos (
    id            UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id     UUID        NOT NULL,
    fechamento_id UUID        NOT NULL REFERENCES vet_barbarafonseca.fechamentos_contabeis(id),
    tipo          VARCHAR(50) NOT NULL,
    nome          VARCHAR(255) NOT NULL,
    descricao     VARCHAR(255),
    tipo_arquivo  VARCHAR(100) NOT NULL DEFAULT 'application/pdf',
    tamanho_bytes INT         NOT NULL DEFAULT 0,
    dados_base64  TEXT        NOT NULL,
    origem        VARCHAR(20) NOT NULL DEFAULT 'upload',
    criado_por    UUID,
    criado_em     TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE vet_barbarafonseca.bot_config (
    id                    UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id             UUID        NOT NULL,
    ativo                 BOOLEAN     NOT NULL DEFAULT FALSE,
    hora_inicio           TIME        NOT NULL DEFAULT '08:00:00',
    hora_fim              TIME        NOT NULL DEFAULT '18:00:00',
    dias_semana           TEXT        NOT NULL DEFAULT '1,2,3,4,5,6',
    dias_antecedencia_min INT         NOT NULL DEFAULT 0,
    dias_antecedencia_max INT         NOT NULL DEFAULT 30,
    msg_boas_vindas       TEXT        NOT NULL DEFAULT 'Ola! Bem-vindo a {clinica}.',
    msg_qual_pet          TEXT        NOT NULL DEFAULT 'Qual o nome do seu pet?',
    msg_qual_servico      TEXT        NOT NULL DEFAULT 'Que tipo de servico voce precisa?',
    msg_qual_data         TEXT        NOT NULL DEFAULT 'Para quando voce gostaria?',
    msg_horarios_disponiveis TEXT     NOT NULL DEFAULT 'Horarios disponiveis para {data}:',
    msg_confirmacao       TEXT        NOT NULL DEFAULT 'Agendamento solicitado!',
    msg_sem_horarios      TEXT        NOT NULL DEFAULT 'Nao ha horarios disponiveis para essa data.',
    msg_fora_horario      TEXT        NOT NULL DEFAULT 'Nosso atendimento e das {inicio} as {fim}.',
    msg_erro              TEXT        NOT NULL DEFAULT 'Desculpe, nao entendi. Digite menu para recomecar.',
    msg_cancelar          TEXT        NOT NULL DEFAULT 'Agendamento cancelado.',
    timeout_conversa_min  INT         NOT NULL DEFAULT 30,
    criado_em             TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    atualizado_em         TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    meta_phone_number_id  TEXT,
    meta_waba_id          TEXT,
    meta_token            TEXT,
    webhook_verify_token  TEXT
);

CREATE TABLE vet_barbarafonseca.bot_conversas (
    id               UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id        UUID        NOT NULL,
    telefone         VARCHAR(20) NOT NULL,
    estado           VARCHAR(30) NOT NULL DEFAULT 'inicio',
    tutor_id         UUID        REFERENCES vet_barbarafonseca.tutores(id),
    pet_id           UUID        REFERENCES vet_barbarafonseca.pets(id),
    pet_nome_digitado VARCHAR(100),
    servico_id       UUID        REFERENCES vet_barbarafonseca.servicos(id),
    servico_nome     VARCHAR(100),
    duracao_min      INT,
    data_escolhida   DATE,
    ultima_msg_em    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    criado_em        TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE vet_barbarafonseca.bot_logs (
    id          UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id   UUID        NOT NULL,
    telefone    VARCHAR(20) NOT NULL,
    direcao     VARCHAR(10) NOT NULL,
    mensagem    TEXT        NOT NULL,
    estado_antes VARCHAR(30),
    estado_apos  VARCHAR(30),
    erro        TEXT,
    criado_em   TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE vet_barbarafonseca.config_mensagens (
    id         UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id  UUID        NOT NULL,
    gatilho    VARCHAR(40) NOT NULL,
    canal      VARCHAR(20) NOT NULL DEFAULT 'whatsapp',
    ativo      BOOLEAN     NOT NULL DEFAULT TRUE,
    horas_antes INT,
    template   TEXT        NOT NULL,
    hora_envio TIME        DEFAULT '09:00:00',
    criado_em  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE vet_barbarafonseca.campanhas (
    id                  UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id           UUID        NOT NULL,
    titulo              VARCHAR(120) NOT NULL,
    mensagem            TEXT        NOT NULL,
    imagem_url          TEXT,
    publico             VARCHAR(40) NOT NULL DEFAULT 'todos',
    canal               VARCHAR(20) NOT NULL DEFAULT 'whatsapp',
    status              VARCHAR(30) NOT NULL DEFAULT 'aguardando_aprovacao',
    total_destinatarios INT         NOT NULL DEFAULT 0,
    total_enviados      INT         NOT NULL DEFAULT 0,
    criado_por          UUID,
    aprovado_por        UUID,
    aprovado_em         TIMESTAMPTZ,
    motivo_reprovacao   TEXT,
    enviada_em          TIMESTAMPTZ,
    criado_em           TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE vet_barbarafonseca.notificacoes_fila (
    id             UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id      UUID        NOT NULL,
    agendamento_id UUID,
    os_id          UUID,
    pet_id         UUID,
    tutor_id       UUID        REFERENCES vet_barbarafonseca.tutores(id),
    canal          VARCHAR(20) NOT NULL DEFAULT 'whatsapp',
    tipo           VARCHAR(60),
    status         VARCHAR(20) NOT NULL DEFAULT 'pendente',
    destinatario   VARCHAR(120),
    payload        JSONB,
    agendado_para  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    tentativas     INT         NOT NULL DEFAULT 0,
    enviado_em     TIMESTAMPTZ,
    erro           TEXT,
    criado_em      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE vet_barbarafonseca.metas_faturamento (
    id            UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id     UUID        NOT NULL,
    ano           INT         NOT NULL,
    mes           INT         NOT NULL,
    valor_meta    NUMERIC     NOT NULL DEFAULT 0,
    criado_em     TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    atualizado_em TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ============================================================
-- MIGRA DADOS do public -> vet_barbarafonseca
-- ============================================================

INSERT INTO vet_barbarafonseca.tenants SELECT * FROM public.tenants WHERE id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7' ON CONFLICT DO NOTHING;
INSERT INTO vet_barbarafonseca.users SELECT * FROM public.users WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7' ON CONFLICT DO NOTHING;
INSERT INTO vet_barbarafonseca.cargos SELECT * FROM public.cargos WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7' ON CONFLICT DO NOTHING;
INSERT INTO vet_barbarafonseca.racas SELECT * FROM public.racas WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7' ON CONFLICT DO NOTHING;
INSERT INTO vet_barbarafonseca.pelagens SELECT * FROM public.pelagens WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7' ON CONFLICT DO NOTHING;
INSERT INTO vet_barbarafonseca.tutores SELECT * FROM public.tutores WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7' ON CONFLICT DO NOTHING;
INSERT INTO vet_barbarafonseca.pets SELECT * FROM public.pets WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7' ON CONFLICT DO NOTHING;
INSERT INTO vet_barbarafonseca.servicos SELECT * FROM public.servicos WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7' ON CONFLICT DO NOTHING;
INSERT INTO vet_barbarafonseca.vias_administracao SELECT * FROM public.vias_administracao WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7' ON CONFLICT DO NOTHING;
INSERT INTO vet_barbarafonseca.planos_saude SELECT * FROM public.planos_saude WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7' ON CONFLICT DO NOTHING;
INSERT INTO vet_barbarafonseca.pet_planos SELECT * FROM public.pet_planos WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7' ON CONFLICT DO NOTHING;
INSERT INTO vet_barbarafonseca.tutor_planos SELECT * FROM public.tutor_planos WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7' ON CONFLICT DO NOTHING;
INSERT INTO vet_barbarafonseca.agendamentos SELECT * FROM public.agendamentos WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7' ON CONFLICT DO NOTHING;
INSERT INTO vet_barbarafonseca.ordens_servico SELECT * FROM public.ordens_servico WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7' ON CONFLICT DO NOTHING;
INSERT INTO vet_barbarafonseca.os_servicos SELECT oss.* FROM public.os_servicos oss JOIN public.ordens_servico os ON oss.os_id = os.id WHERE os.tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7' ON CONFLICT DO NOTHING;
INSERT INTO vet_barbarafonseca.prontuario_itens SELECT * FROM public.prontuario_itens pi WHERE EXISTS (SELECT 1 FROM public.pets p WHERE p.id = pi.pet_id AND p.tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7') ON CONFLICT DO NOTHING;
INSERT INTO vet_barbarafonseca.vacinacoes SELECT * FROM public.vacinacoes v WHERE EXISTS (SELECT 1 FROM public.pets p WHERE p.id = v.pet_id AND p.tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7') ON CONFLICT DO NOTHING;
INSERT INTO vet_barbarafonseca.links_agendamento SELECT * FROM public.links_agendamento WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7' ON CONFLICT DO NOTHING;
INSERT INTO vet_barbarafonseca.categorias_financeiras SELECT * FROM public.categorias_financeiras WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7' ON CONFLICT DO NOTHING;
INSERT INTO vet_barbarafonseca.contas_bancarias SELECT * FROM public.contas_bancarias WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7' ON CONFLICT DO NOTHING;
INSERT INTO vet_barbarafonseca.contas SELECT * FROM public.contas WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7' ON CONFLICT DO NOTHING;
INSERT INTO vet_barbarafonseca.lancamentos SELECT * FROM public.lancamentos WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7' ON CONFLICT DO NOTHING;
INSERT INTO vet_barbarafonseca.movimentacoes_bancarias SELECT * FROM public.movimentacoes_bancarias WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7' ON CONFLICT DO NOTHING;
INSERT INTO vet_barbarafonseca.conciliacoes_diarias SELECT * FROM public.conciliacoes_diarias WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7' ON CONFLICT DO NOTHING;
INSERT INTO vet_barbarafonseca.produtos SELECT * FROM public.produtos WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7' ON CONFLICT DO NOTHING;
INSERT INTO vet_barbarafonseca.movimentacoes_estoque SELECT * FROM public.movimentacoes_estoque WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7' ON CONFLICT DO NOTHING;
INSERT INTO vet_barbarafonseca.caixas SELECT * FROM public.caixas WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7' ON CONFLICT DO NOTHING;
INSERT INTO vet_barbarafonseca.movimentacoes_caixa SELECT * FROM public.movimentacoes_caixa WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7' ON CONFLICT DO NOTHING;
INSERT INTO vet_barbarafonseca.vendas SELECT * FROM public.vendas WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7' ON CONFLICT DO NOTHING;
INSERT INTO vet_barbarafonseca.venda_itens SELECT vi.* FROM public.venda_itens vi JOIN public.vendas v ON vi.venda_id = v.id WHERE v.tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7' ON CONFLICT DO NOTHING;
INSERT INTO vet_barbarafonseca.funcionarios SELECT * FROM public.funcionarios WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7' ON CONFLICT DO NOTHING;
INSERT INTO vet_barbarafonseca.comissoes SELECT * FROM public.comissoes WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7' ON CONFLICT DO NOTHING;
INSERT INTO vet_barbarafonseca.fechamento_mensal SELECT * FROM public.fechamento_mensal WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7' ON CONFLICT DO NOTHING;
INSERT INTO vet_barbarafonseca.parametros_sistema SELECT * FROM public.parametros_sistema WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7' ON CONFLICT DO NOTHING;
INSERT INTO vet_barbarafonseca.fornecedores SELECT * FROM public.fornecedores WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7' ON CONFLICT DO NOTHING;
INSERT INTO vet_barbarafonseca.condicoes_pagamento SELECT * FROM public.condicoes_pagamento WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7' ON CONFLICT DO NOTHING;
INSERT INTO vet_barbarafonseca.pedidos_compra SELECT * FROM public.pedidos_compra WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7' ON CONFLICT DO NOTHING;
INSERT INTO vet_barbarafonseca.pedido_itens SELECT pi.* FROM public.pedido_itens pi JOIN public.pedidos_compra pc ON pi.pedido_id = pc.id WHERE pc.tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7' ON CONFLICT DO NOTHING;
INSERT INTO vet_barbarafonseca.recebimentos_mercadoria SELECT * FROM public.recebimentos_mercadoria WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7' ON CONFLICT DO NOTHING;
INSERT INTO vet_barbarafonseca.recebimento_itens SELECT ri.* FROM public.recebimento_itens ri JOIN public.recebimentos_mercadoria rm ON ri.recebimento_id = rm.id WHERE rm.tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7' ON CONFLICT DO NOTHING;
INSERT INTO vet_barbarafonseca.recebimento_anexos SELECT * FROM public.recebimento_anexos WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7' ON CONFLICT DO NOTHING;
INSERT INTO vet_barbarafonseca.fechamentos_contabeis SELECT * FROM public.fechamentos_contabeis WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7' ON CONFLICT DO NOTHING;
INSERT INTO vet_barbarafonseca.fechamento_documentos SELECT fd.* FROM public.fechamento_documentos fd JOIN public.fechamentos_contabeis fc ON fd.fechamento_id = fc.id WHERE fc.tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7' ON CONFLICT DO NOTHING;
INSERT INTO vet_barbarafonseca.bot_config SELECT id, tenant_id, ativo, hora_inicio, hora_fim, dias_semana, dias_antecedencia_min, dias_antecedencia_max, msg_boas_vindas, msg_qual_pet, msg_qual_servico, msg_qual_data, msg_horarios_disponiveis, msg_confirmacao, msg_sem_horarios, msg_fora_horario, msg_erro, msg_cancelar, timeout_conversa_min, criado_em, atualizado_em, meta_phone_number_id, meta_waba_id FROM public.bot_config WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7' ON CONFLICT DO NOTHING;
INSERT INTO vet_barbarafonseca.bot_conversas SELECT * FROM public.bot_conversas WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7' ON CONFLICT DO NOTHING;
INSERT INTO vet_barbarafonseca.bot_logs SELECT * FROM public.bot_logs WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7' ON CONFLICT DO NOTHING;
INSERT INTO vet_barbarafonseca.config_mensagens SELECT * FROM public.config_mensagens WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7' ON CONFLICT DO NOTHING;
INSERT INTO vet_barbarafonseca.campanhas SELECT * FROM public.campanhas WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7' ON CONFLICT DO NOTHING;
INSERT INTO vet_barbarafonseca.notificacoes_fila SELECT * FROM public.notificacoes_fila WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7' ON CONFLICT DO NOTHING;
INSERT INTO vet_barbarafonseca.metas_faturamento SELECT * FROM public.metas_faturamento WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7' ON CONFLICT DO NOTHING;

-- ============================================================
-- VERIFICACAO
-- ============================================================
SELECT 'tutores'      , COUNT(*) FROM vet_barbarafonseca.tutores
UNION ALL SELECT 'pets'        , COUNT(*) FROM vet_barbarafonseca.pets
UNION ALL SELECT 'servicos'    , COUNT(*) FROM vet_barbarafonseca.servicos
UNION ALL SELECT 'contas'      , COUNT(*) FROM vet_barbarafonseca.contas
UNION ALL SELECT 'vendas'      , COUNT(*) FROM vet_barbarafonseca.vendas
UNION ALL SELECT 'funcionarios', COUNT(*) FROM vet_barbarafonseca.funcionarios
UNION ALL SELECT 'bot_config'  , COUNT(*) FROM vet_barbarafonseca.bot_config
UNION ALL SELECT 'parametros'  , COUNT(*) FROM vet_barbarafonseca.parametros_sistema;
