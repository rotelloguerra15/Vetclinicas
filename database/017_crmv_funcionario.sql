-- Migration 017 — Campo CRMV em funcionários
ALTER TABLE funcionarios
    ADD COLUMN IF NOT EXISTS crmv VARCHAR(30);
