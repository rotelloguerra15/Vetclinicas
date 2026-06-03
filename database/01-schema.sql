-- ============================================================
--  VetClinica — Schema PostgreSQL
--  Stack: .NET 8 + React + Railway/Neon
--  Reaproveitado da lavanderia: multi-tenant, auth, financeiro
--  Novo: pets, prontuário, vacinas, OS, notificações WhatsApp
-- ============================================================

-- ──────────────────────────────────────────────
--  ENUMS
-- ──────────────────────────────────────────────
CREATE TYPE papel_usuario    AS ENUM ('owner', 'admin', 'veterinario', 'tosador', 'recepcionista');
CREATE TYPE especie_pet      AS ENUM ('cao', 'gato', 'ave', 'roedor', 'reptil', 'outro');
CREATE TYPE sexo_pet         AS ENUM ('macho', 'femea', 'indefinido');
CREATE TYPE tipo_agendamento AS ENUM ('consulta', 'banho', 'tosa', 'banho_tosa', 'vacina', 'retorno', 'cirurgia', 'exame');
CREATE TYPE status_agenda    AS ENUM ('pendente', 'confirmado', 'em_atendimento', 'concluido', 'cancelado', 'faltou');
CREATE TYPE origem_agenda    AS ENUM ('interno', 'tutor_self_service', 'whatsapp_bot');
CREATE TYPE status_os        AS ENUM ('aguardando', 'em_andamento', 'pronto', 'entregue', 'cancelado');
CREATE TYPE tipo_prontuario  AS ENUM ('consulta', 'exame', 'cirurgia', 'internacao', 'observacao', 'receita');
CREATE TYPE canal_notif      AS ENUM ('whatsapp', 'email', 'sms');
CREATE TYPE status_notif     AS ENUM ('pendente', 'enviado', 'falhou', 'cancelado');
CREATE TYPE tipo_lancamento  AS ENUM ('receita', 'despesa');
CREATE TYPE plano_tenant     AS ENUM ('trial', 'basico', 'pro', 'enterprise');

-- ──────────────────────────────────────────────
--  TENANTS  (multi-tenant — reaproveitado)
-- ──────────────────────────────────────────────
CREATE TABLE tenants (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    nome            VARCHAR(120) NOT NULL,
    plano           plano_tenant NOT NULL DEFAULT 'trial',
    telefone        VARCHAR(20),
    email           VARCHAR(120),
    whatsapp_number VARCHAR(20),          -- número da API do WhatsApp Business
    whatsapp_token  TEXT,                 -- token Z-API / Evolution API
    criado_em       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    ativo           BOOLEAN NOT NULL DEFAULT TRUE
);

-- ──────────────────────────────────────────────
--  USERS  (reaproveitado + novo campo papel)
-- ──────────────────────────────────────────────
CREATE TABLE users (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id   UUID NOT NULL REFERENCES tenants(id),
    email       VARCHAR(120) NOT NULL,
    nome        VARCHAR(120) NOT NULL,
    senha_hash  TEXT NOT NULL,
    papel       papel_usuario NOT NULL DEFAULT 'recepcionista',
    ativo       BOOLEAN NOT NULL DEFAULT TRUE,
    criado_em   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (tenant_id, email)
);

-- ──────────────────────────────────────────────
--  TUTORES  (equivale a "clientes" na lavanderia)
-- ──────────────────────────────────────────────
CREATE TABLE tutores (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id   UUID NOT NULL REFERENCES tenants(id),
    nome        VARCHAR(120) NOT NULL,
    cpf_cnpj    VARCHAR(18),
    telefone    VARCHAR(20),
    email       VARCHAR(120),
    endereco    TEXT,
    obs         TEXT,
    pontos      INTEGER NOT NULL DEFAULT 0,  -- fidelidade
    ativo       BOOLEAN NOT NULL DEFAULT TRUE,
    criado_em   TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ──────────────────────────────────────────────
--  PETS  (novo — coração do sistema)
-- ──────────────────────────────────────────────
CREATE TABLE pets (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID NOT NULL REFERENCES tenants(id),
    tutor_id        UUID NOT NULL REFERENCES tutores(id),
    nome            VARCHAR(80) NOT NULL,
    especie         especie_pet NOT NULL DEFAULT 'cao',
    raca            VARCHAR(80),
    sexo            sexo_pet NOT NULL DEFAULT 'indefinido',
    data_nascimento DATE,
    pelagem         VARCHAR(80),          -- curto, longo, duplo, etc.
    peso_kg         NUMERIC(5,2),
    castrado        BOOLEAN,
    cor             VARCHAR(60),
    foto_url        TEXT,
    obs             TEXT,
    ativo           BOOLEAN NOT NULL DEFAULT TRUE,
    criado_em       TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ──────────────────────────────────────────────
--  SERVIÇOS  (tabela de preços)
-- ──────────────────────────────────────────────
CREATE TABLE servicos (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id   UUID NOT NULL REFERENCES tenants(id),
    nome        VARCHAR(120) NOT NULL,
    categoria   VARCHAR(60),              -- 'banho_tosa', 'consulta', 'vacina', 'cirurgia'
    descricao   TEXT,
    preco_base  NUMERIC(10,2) NOT NULL DEFAULT 0,
    duracao_min INTEGER,                  -- duração estimada em minutos
    ativo       BOOLEAN NOT NULL DEFAULT TRUE
);

-- ──────────────────────────────────────────────
--  AGENDAMENTOS
-- ──────────────────────────────────────────────
CREATE TABLE agendamentos (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID NOT NULL REFERENCES tenants(id),
    pet_id          UUID NOT NULL REFERENCES pets(id),
    user_id         UUID REFERENCES users(id),  -- profissional responsável
    tipo            tipo_agendamento NOT NULL,
    data_hora       TIMESTAMPTZ NOT NULL,
    duracao_min     INTEGER NOT NULL DEFAULT 60,
    status          status_agenda NOT NULL DEFAULT 'pendente',
    origem          origem_agenda NOT NULL DEFAULT 'interno',
    obs             TEXT,
    -- para self-service: tutor propõe, clínica confirma
    confirmado_em   TIMESTAMPTZ,
    confirmado_por  UUID REFERENCES users(id),
    criado_em       TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ──────────────────────────────────────────────
--  ORDENS DE SERVIÇO  (banho/tosa/consulta em execução)
-- ──────────────────────────────────────────────
CREATE TABLE ordens_servico (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID NOT NULL REFERENCES tenants(id),
    agendamento_id  UUID REFERENCES agendamentos(id),
    pet_id          UUID NOT NULL REFERENCES pets(id),
    user_id         UUID REFERENCES users(id),  -- quem executou
    status          status_os NOT NULL DEFAULT 'aguardando',
    obs             TEXT,
    fotos_urls      TEXT[],              -- array de URLs das fotos do pet pronto
    valor_total     NUMERIC(10,2),
    inicio          TIMESTAMPTZ,
    fim             TIMESTAMPTZ,
    entregue_em     TIMESTAMPTZ,
    criado_em       TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- serviços realizados dentro de uma OS (N:N)
CREATE TABLE os_servicos (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    os_id           UUID NOT NULL REFERENCES ordens_servico(id),
    servico_id      UUID NOT NULL REFERENCES servicos(id),
    preco_cobrado   NUMERIC(10,2) NOT NULL,
    obs             TEXT
);

-- ──────────────────────────────────────────────
--  PRONTUÁRIO
-- ──────────────────────────────────────────────
CREATE TABLE prontuario_itens (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    pet_id      UUID NOT NULL REFERENCES pets(id),
    user_id     UUID REFERENCES users(id),
    data        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    tipo        tipo_prontuario NOT NULL,
    titulo      VARCHAR(120),
    descricao   TEXT NOT NULL,
    anexo_url   TEXT,                    -- exame PDF, raio-X, etc.
    criado_em   TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ──────────────────────────────────────────────
--  VACINAÇÕES
-- ──────────────────────────────────────────────
CREATE TABLE vacinacoes (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    pet_id          UUID NOT NULL REFERENCES pets(id),
    user_id         UUID REFERENCES users(id),
    vacina          VARCHAR(120) NOT NULL,
    fabricante      VARCHAR(80),
    lote            VARCHAR(40),
    data_aplicacao  DATE NOT NULL,
    proxima_dose    DATE,
    obs             TEXT,
    criado_em       TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ──────────────────────────────────────────────
--  FINANCEIRO  (reaproveitado da lavanderia)
-- ──────────────────────────────────────────────
CREATE TABLE categorias_financeiras (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id   UUID NOT NULL REFERENCES tenants(id),
    nome        VARCHAR(80) NOT NULL,
    tipo        tipo_lancamento NOT NULL,
    ativo       BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE lancamentos (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID NOT NULL REFERENCES tenants(id),
    os_id           UUID REFERENCES ordens_servico(id),
    categoria_id    UUID REFERENCES categorias_financeiras(id),
    data            DATE NOT NULL,
    tipo            tipo_lancamento NOT NULL,
    valor           NUMERIC(10,2) NOT NULL,
    descricao       TEXT,
    criado_em       TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ──────────────────────────────────────────────
--  FILA DE NOTIFICAÇÕES  (WhatsApp / e-mail)
-- ──────────────────────────────────────────────
CREATE TABLE notificacoes_fila (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id           UUID NOT NULL REFERENCES tenants(id),
    agendamento_id      UUID REFERENCES agendamentos(id),
    os_id               UUID REFERENCES ordens_servico(id),
    pet_id              UUID REFERENCES pets(id),
    tutor_id            UUID REFERENCES tutores(id),
    canal               canal_notif NOT NULL DEFAULT 'whatsapp',
    tipo                VARCHAR(60),     -- 'pet_pronto', 'lembrete_banho', 'confirmacao_agenda', 'vacina_vencendo'
    status              status_notif NOT NULL DEFAULT 'pendente',
    destinatario        VARCHAR(120),    -- telefone ou email
    payload             JSONB,           -- corpo da mensagem, fotos, etc.
    agendado_para       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    tentativas          INTEGER NOT NULL DEFAULT 0,
    enviado_em          TIMESTAMPTZ,
    erro                TEXT,
    criado_em           TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ──────────────────────────────────────────────
--  LINKS DE AUTO-AGENDAMENTO  (tutor → bot WhatsApp)
-- ──────────────────────────────────────────────
CREATE TABLE links_agendamento (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id   UUID NOT NULL REFERENCES tenants(id),
    tutor_id    UUID NOT NULL REFERENCES tutores(id),
    pet_id      UUID REFERENCES pets(id),
    token       VARCHAR(64) NOT NULL UNIQUE,  -- gerado no backend (AgendamentoLinkService)
    expira_em   TIMESTAMPTZ NOT NULL,
    usado_em    TIMESTAMPTZ,
    criado_em   TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ──────────────────────────────────────────────
--  ÍNDICES
-- ──────────────────────────────────────────────
CREATE INDEX idx_pets_tutor        ON pets(tutor_id);
CREATE INDEX idx_pets_tenant       ON pets(tenant_id);
CREATE INDEX idx_agenda_pet        ON agendamentos(pet_id);
CREATE INDEX idx_agenda_data       ON agendamentos(tenant_id, data_hora);
CREATE INDEX idx_agenda_status     ON agendamentos(tenant_id, status);
CREATE INDEX idx_os_pet            ON ordens_servico(pet_id);
CREATE INDEX idx_os_status         ON ordens_servico(tenant_id, status);
CREATE INDEX idx_notif_status      ON notificacoes_fila(status, agendado_para);
CREATE INDEX idx_prontuario_pet    ON prontuario_itens(pet_id);
CREATE INDEX idx_vacinas_pet       ON vacinacoes(pet_id);
CREATE INDEX idx_vacinas_proxima   ON vacinacoes(proxima_dose) WHERE proxima_dose IS NOT NULL;
CREATE INDEX idx_lancamentos       ON lancamentos(tenant_id, data);

-- ──────────────────────────────────────────────
--  TRIGGER: gera lançamento ao fechar OS
-- ──────────────────────────────────────────────
CREATE OR REPLACE FUNCTION fn_os_gera_lancamento()
RETURNS TRIGGER LANGUAGE plpgsql AS $$
BEGIN
    IF NEW.status = 'entregue' AND OLD.status != 'entregue' AND NEW.valor_total > 0 THEN
        INSERT INTO lancamentos (tenant_id, os_id, data, tipo, valor, descricao)
        VALUES (NEW.tenant_id, NEW.id, NOW()::DATE, 'receita', NEW.valor_total,
                'OS automática: ' || NEW.id);
    END IF;
    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_os_lancamento
AFTER UPDATE ON ordens_servico
FOR EACH ROW EXECUTE FUNCTION fn_os_gera_lancamento();

-- ──────────────────────────────────────────────
--  TRIGGER: agenda lembrete 30 dias após OS entregue
-- ──────────────────────────────────────────────
CREATE OR REPLACE FUNCTION fn_agenda_lembrete_banho()
RETURNS TRIGGER LANGUAGE plpgsql AS $$
DECLARE
    v_tutor_telefone VARCHAR(20);
    v_pet_nome       VARCHAR(80);
    v_tutor_id       UUID;
BEGIN
    IF NEW.status = 'entregue' AND OLD.status != 'entregue' THEN
        SELECT t.telefone, p.nome, p.tutor_id
        INTO   v_tutor_telefone, v_pet_nome, v_tutor_id
        FROM   pets p
        JOIN   tutores t ON t.id = p.tutor_id
        WHERE  p.id = NEW.pet_id;

        -- insere na fila para disparar daqui 30 dias
        INSERT INTO notificacoes_fila (
            tenant_id, os_id, pet_id, tutor_id,
            canal, tipo, destinatario, payload, agendado_para
        ) VALUES (
            NEW.tenant_id, NEW.id, NEW.pet_id, v_tutor_id,
            'whatsapp', 'lembrete_banho', v_tutor_telefone,
            jsonb_build_object('pet_nome', v_pet_nome, 'dias', 30),
            NOW() + INTERVAL '30 days'
        );
    END IF;
    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_lembrete_banho
AFTER UPDATE ON ordens_servico
FOR EACH ROW EXECUTE FUNCTION fn_agenda_lembrete_banho();

-- ──────────────────────────────────────────────
--  SEED INICIAL
-- ──────────────────────────────────────────────
INSERT INTO tenants (id, nome, plano, telefone, email)
VALUES ('00000000-0000-0000-0000-000000000001', 'Clínica Veterinária Fonseca', 'pro',
        '31999999999', 'contato@clinicafonseca.com.br');

INSERT INTO users (tenant_id, email, nome, senha_hash, papel)
VALUES ('00000000-0000-0000-0000-000000000001',
        'admin@clinicafonseca.com.br', 'Administrador Fonseca',
        '$2b$12$0Qe9AkQnut9HFxwGkdliC.kBYFVongbNyk4SXR7SG4CjocwFThFDC', 'owner');

INSERT INTO categorias_financeiras (tenant_id, nome, tipo) VALUES
('00000000-0000-0000-0000-000000000001', 'Consultas',        'receita'),
('00000000-0000-0000-0000-000000000001', 'Banho e Tosa',     'receita'),
('00000000-0000-0000-0000-000000000001', 'Vacinas',          'receita'),
('00000000-0000-0000-0000-000000000001', 'Cirurgias',        'receita'),
('00000000-0000-0000-0000-000000000001', 'Medicamentos',     'despesa'),
('00000000-0000-0000-0000-000000000001', 'Insumos',          'despesa'),
('00000000-0000-0000-0000-000000000001', 'Folha de pagamento','despesa'),
('00000000-0000-0000-0000-000000000001', 'Aluguel',          'despesa');

INSERT INTO servicos (tenant_id, nome, categoria, preco_base, duracao_min) VALUES
('00000000-0000-0000-0000-000000000001', 'Banho pequeno porte',        'banho_tosa',  60.00, 90),
('00000000-0000-0000-0000-000000000001', 'Banho médio porte',          'banho_tosa',  90.00, 120),
('00000000-0000-0000-0000-000000000001', 'Banho grande porte',         'banho_tosa', 140.00, 150),
('00000000-0000-0000-0000-000000000001', 'Tosa higiênica',             'banho_tosa',  40.00, 45),
('00000000-0000-0000-0000-000000000001', 'Tosa completa',              'banho_tosa',  90.00, 60),
('00000000-0000-0000-0000-000000000001', 'Consulta clínica geral',     'consulta',   150.00, 30),
('00000000-0000-0000-0000-000000000001', 'Consulta retorno',           'consulta',    80.00, 20),
('00000000-0000-0000-0000-000000000001', 'Vacina V8/V10',              'vacina',      85.00, 15),
('00000000-0000-0000-0000-000000000001', 'Vacina antirrábica',         'vacina',      65.00, 15),
('00000000-0000-0000-0000-000000000001', 'Microchipagem',              'consulta',   120.00, 20);
