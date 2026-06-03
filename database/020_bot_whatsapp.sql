-- Migration 020 — Bot WhatsApp: configuração, conversas e logs

-- ── Configuração do bot por tenant ───────────────────────────────────────────
CREATE TABLE IF NOT EXISTS bot_config (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id               UUID NOT NULL UNIQUE REFERENCES tenants(id),

    -- Ativação
    ativo                   BOOLEAN NOT NULL DEFAULT FALSE,

    -- Horário de atendimento do bot
    hora_inicio             TIME NOT NULL DEFAULT '08:00',
    hora_fim                TIME NOT NULL DEFAULT '18:00',
    dias_semana             TEXT NOT NULL DEFAULT '1,2,3,4,5,6',  -- 0=dom..6=sab

    -- Janela de agendamento
    dias_antecedencia_min   INT NOT NULL DEFAULT 0,   -- mínimo de dias de antecedência
    dias_antecedencia_max   INT NOT NULL DEFAULT 30,  -- até quantos dias à frente oferecer

    -- Mensagens configuráveis
    msg_boas_vindas         TEXT NOT NULL DEFAULT 'Ola! Bem-vindo a {clinica}. Vou te ajudar a agendar. 😊',
    msg_qual_pet            TEXT NOT NULL DEFAULT 'Qual o nome do seu pet?',
    msg_qual_servico        TEXT NOT NULL DEFAULT 'Que tipo de servico voce precisa? Digite o numero:',
    msg_qual_data           TEXT NOT NULL DEFAULT 'Para quando voce gostaria? (ex: amanha, 25/06 ou escolha abaixo)',
    msg_horarios_disponiveis TEXT NOT NULL DEFAULT 'Horarios disponiveis para {data}:',
    msg_confirmacao         TEXT NOT NULL DEFAULT 'Agendamento solicitado! ✅\n\nPet: {pet}\nServico: {servico}\nData: {data} as {hora}\n\nA clinica vai confirmar em breve pelo WhatsApp.',
    msg_sem_horarios        TEXT NOT NULL DEFAULT 'Nao ha horarios disponiveis para essa data. Tente outra data.',
    msg_fora_horario        TEXT NOT NULL DEFAULT 'Nosso atendimento e das {inicio} as {fim}. Envie uma mensagem neste horario!',
    msg_erro                TEXT NOT NULL DEFAULT 'Desculpe, nao entendi. Digite *menu* para recomecar.',
    msg_cancelar            TEXT NOT NULL DEFAULT 'Agendamento cancelado. Digite *menu* para recomecar.',

    -- Timeout de conversa inativa (minutos)
    timeout_conversa_min    INT NOT NULL DEFAULT 30,

    criado_em               TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    atualizado_em           TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ── Estado das conversas em andamento ────────────────────────────────────────
CREATE TABLE IF NOT EXISTS bot_conversas (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID NOT NULL REFERENCES tenants(id),
    telefone        VARCHAR(20) NOT NULL,

    -- Estado atual da conversa
    -- estados: inicio | aguardando_pet | aguardando_servico | aguardando_data | aguardando_horario | concluido | cancelado
    estado          VARCHAR(30) NOT NULL DEFAULT 'inicio',

    -- Dados coletados durante a conversa
    tutor_id        UUID REFERENCES tutores(id),
    pet_id          UUID REFERENCES pets(id),
    pet_nome_digitado VARCHAR(100),
    servico_id      UUID REFERENCES servicos(id),
    servico_nome    VARCHAR(100),
    duracao_min     INT,
    data_escolhida  DATE,

    ultima_msg_em   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    criado_em       TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    UNIQUE(tenant_id, telefone)
);

CREATE INDEX IF NOT EXISTS idx_bot_conversas_tenant_tel ON bot_conversas(tenant_id, telefone);

-- ── Log de todas as mensagens trocadas ───────────────────────────────────────
CREATE TABLE IF NOT EXISTS bot_logs (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id   UUID NOT NULL REFERENCES tenants(id),
    telefone    VARCHAR(20) NOT NULL,
    direcao     VARCHAR(10) NOT NULL,  -- 'entrada' | 'saida'
    mensagem    TEXT NOT NULL,
    estado_antes VARCHAR(30),
    estado_apos  VARCHAR(30),
    erro        TEXT,
    criado_em   TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_bot_logs_tenant ON bot_logs(tenant_id, criado_em DESC);

-- Seed: cria config padrão para tenants existentes
INSERT INTO bot_config (id, tenant_id)
SELECT gen_random_uuid(), id FROM tenants
ON CONFLICT (tenant_id) DO NOTHING;
