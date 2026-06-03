-- ============================================================
--  VetClinica — Migração 08
--  Prontuário: motivo da consulta + receituário
-- ============================================================
ALTER TABLE prontuario_itens
    ADD COLUMN IF NOT EXISTS motivo TEXT,
    ADD COLUMN IF NOT EXISTS receituario TEXT;
