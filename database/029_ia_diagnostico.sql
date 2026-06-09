-- Migration 029 — Assistente IA (Anthropic)
-- Adiciona chave API Anthropic por tenant em parametros_sistema
-- Cada clínica usa sua própria chave — custo vai direto para a Anthropic deles

ALTER TABLE parametros_sistema
    ADD COLUMN IF NOT EXISTS anthropic_api_key  VARCHAR(200),
    ADD COLUMN IF NOT EXISTS ia_ativo           BOOLEAN NOT NULL DEFAULT FALSE;

-- Índice para busca rápida por tenant
CREATE INDEX IF NOT EXISTS idx_parametros_tenant ON parametros_sistema(tenant_id);
