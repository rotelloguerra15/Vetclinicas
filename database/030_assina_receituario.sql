-- Migration 030 — Campo assina_receituario em funcionarios
-- Marca quais funcionários são veterinários que assinam receituário

ALTER TABLE funcionarios
    ADD COLUMN IF NOT EXISTS assina_receituario BOOLEAN NOT NULL DEFAULT FALSE;

-- Quem já tem CRMV preenchido provavelmente assina — marca automaticamente
UPDATE funcionarios
SET assina_receituario = TRUE
WHERE crmv IS NOT NULL AND crmv != '';
