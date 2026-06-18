-- Migration 039: trial_expira_em + reset de senha na platform.tenants
-- Execute no Railway via psql ou DBeaver

ALTER TABLE platform.tenants
    ADD COLUMN IF NOT EXISTS trial_expira_em  TIMESTAMP WITH TIME ZONE,
    ADD COLUMN IF NOT EXISTS token_reset_senha TEXT,
    ADD COLUMN IF NOT EXISTS token_reset_expira TIMESTAMP WITH TIME ZONE;

-- Preenche trial_expira_em para tenants existentes que ainda nao tem (14 dias apos criacao)
UPDATE platform.tenants
SET trial_expira_em = criado_em + INTERVAL '14 days'
WHERE plano = 'trial'
  AND trial_expira_em IS NULL;

-- Confirma
SELECT id, nome, plano, criado_em, trial_expira_em
FROM platform.tenants
ORDER BY criado_em DESC;
