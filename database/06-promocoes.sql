-- ============================================================
--  VetClinica — Migração 06
--  Campanhas de promoção (com aprovação do admin da clínica)
-- ============================================================

CREATE TYPE status_campanha AS ENUM
    ('rascunho', 'aguardando_aprovacao', 'aprovada', 'enviada', 'reprovada');

CREATE TABLE campanhas (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id           UUID NOT NULL REFERENCES tenants(id),
    titulo              VARCHAR(120) NOT NULL,
    mensagem            TEXT NOT NULL,
    imagem_url          TEXT,                       -- opcional: promoção com imagem
    publico             VARCHAR(40) NOT NULL DEFAULT 'todos',  -- 'todos' | 'inativos_30d' | 'aniversariantes_mes'
    canal               canal_notif NOT NULL DEFAULT 'whatsapp',
    status              status_campanha NOT NULL DEFAULT 'aguardando_aprovacao',
    total_destinatarios INTEGER NOT NULL DEFAULT 0,
    total_enviados      INTEGER NOT NULL DEFAULT 0,
    criado_por          UUID REFERENCES users(id),
    aprovado_por        UUID REFERENCES users(id),
    aprovado_em         TIMESTAMPTZ,
    motivo_reprovacao   TEXT,
    enviada_em          TIMESTAMPTZ,
    criado_em           TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- opt-out de marketing (LGPD): só recebe promoção quem aceita
ALTER TABLE tutores
    ADD COLUMN IF NOT EXISTS aceita_promocoes BOOLEAN NOT NULL DEFAULT TRUE;

CREATE INDEX idx_campanhas_tenant ON campanhas(tenant_id, status);
CREATE INDEX idx_tutores_promo ON tutores(tenant_id) WHERE aceita_promocoes;
