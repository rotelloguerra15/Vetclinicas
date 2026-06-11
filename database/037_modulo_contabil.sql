-- Migration 037 -- Modulo Contabil

-- Configuracao SMTP e contabilidade (em parametros_sistema)
ALTER TABLE parametros_sistema
    ADD COLUMN IF NOT EXISTS smtp_host        VARCHAR(200),
    ADD COLUMN IF NOT EXISTS smtp_porta       INTEGER DEFAULT 587,
    ADD COLUMN IF NOT EXISTS smtp_usuario     VARCHAR(200),
    ADD COLUMN IF NOT EXISTS smtp_senha_enc   TEXT,        -- criptografada AES
    ADD COLUMN IF NOT EXISTS smtp_ssl         BOOLEAN NOT NULL DEFAULT TRUE,
    ADD COLUMN IF NOT EXISTS email_remetente  VARCHAR(200),
    ADD COLUMN IF NOT EXISTS email_contabil   VARCHAR(200), -- email da contabilidade
    ADD COLUMN IF NOT EXISTS contabil_nome    VARCHAR(200); -- nome do escritorio

-- Fechamentos contabeis mensais
CREATE TABLE IF NOT EXISTS fechamentos_contabeis (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID NOT NULL REFERENCES tenants(id),
    ano             INTEGER NOT NULL,
    mes             INTEGER NOT NULL CHECK (mes BETWEEN 1 AND 12),
    status          VARCHAR(30) NOT NULL DEFAULT 'aberto', -- aberto | enviado
    enviado_em      TIMESTAMPTZ,
    enviado_por     UUID REFERENCES users(id),
    email_destino   VARCHAR(200),
    obs             TEXT,
    criado_em       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (tenant_id, ano, mes)
);

-- Documentos do fechamento (gerados + uploads manuais)
CREATE TABLE IF NOT EXISTS fechamento_documentos (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id           UUID NOT NULL REFERENCES tenants(id),
    fechamento_id       UUID NOT NULL REFERENCES fechamentos_contabeis(id) ON DELETE CASCADE,
    tipo                VARCHAR(50) NOT NULL,  -- extrato_bancario | dre | estoque | repasses | nf_tomada | fatura_cartao | outros
    nome                VARCHAR(255) NOT NULL,
    descricao           VARCHAR(255),
    tipo_arquivo        VARCHAR(100) NOT NULL DEFAULT 'application/pdf',
    tamanho_bytes       INTEGER NOT NULL DEFAULT 0,
    dados_base64        TEXT NOT NULL,
    origem              VARCHAR(20) NOT NULL DEFAULT 'upload', -- upload | sistema
    criado_por          UUID REFERENCES users(id),
    criado_em           TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_fech_doc_fechamento ON fechamento_documentos(fechamento_id);
CREATE INDEX IF NOT EXISTS idx_fechamentos_tenant  ON fechamentos_contabeis(tenant_id, ano, mes DESC);
