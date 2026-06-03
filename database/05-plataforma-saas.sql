-- ============================================================
--  VetClinica — Migração 05
--  Camada de PLATAFORMA (SaaS): super-admin + marca por clínica
-- ============================================================

-- ──────────────────────────────────────────────
--  SUPER-ADMINS DA PLATAFORMA (você)
--  Ficam ACIMA dos tenants. Não pertencem a nenhuma clínica.
-- ──────────────────────────────────────────────
CREATE TABLE platform_admins (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    nome        VARCHAR(120) NOT NULL,
    email       VARCHAR(120) NOT NULL UNIQUE,
    senha_hash  TEXT NOT NULL,
    ativo       BOOLEAN NOT NULL DEFAULT TRUE,
    criado_em   TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ──────────────────────────────────────────────
--  MARCA POR CLÍNICA (white-label)
--  Cada tenant tem sua própria identidade visual.
-- ──────────────────────────────────────────────
ALTER TABLE tenants
    ADD COLUMN IF NOT EXISTS logo_url   TEXT,
    ADD COLUMN IF NOT EXISTS tagline    VARCHAR(160),
    ADD COLUMN IF NOT EXISTS cor_primaria VARCHAR(9) DEFAULT '#0f172a',
    ADD COLUMN IF NOT EXISTS suspenso_em TIMESTAMPTZ;   -- bloqueio por inadimplência, etc.

-- ──────────────────────────────────────────────
--  SEED: super-admin (Rotello) + marca da Fonseca
-- ──────────────────────────────────────────────
INSERT INTO platform_admins (nome, email, senha_hash)
VALUES ('Rotello Guerra', 'rotello.guerra@ketra.com.br', '$2b$12$LL6MYmGZVGLjxuLML413i.MWlRNZyYhgnA9GFw730YQMNVsrTYNqa')
ON CONFLICT (email) DO NOTHING;

UPDATE tenants
   SET tagline = 'Gestão Inteligente para Clínicas Veterinárias'
 WHERE id = '00000000-0000-0000-0000-000000000001';
