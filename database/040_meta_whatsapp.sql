-- ============================================================
-- Migration 040 — WhatsApp: Meta Cloud API
-- Adiciona campo meta_phone_number_id ao bot_config
-- para o webhook resolver qual tenant recebeu a mensagem
-- ============================================================

ALTER TABLE bot_config
    ADD COLUMN IF NOT EXISTS meta_phone_number_id TEXT,
    ADD COLUMN IF NOT EXISTS meta_waba_id          TEXT;

-- Índice para lookup rápido no webhook
CREATE INDEX IF NOT EXISTS idx_bot_config_meta_phone
    ON bot_config(meta_phone_number_id)
    WHERE meta_phone_number_id IS NOT NULL;
