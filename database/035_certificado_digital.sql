-- Migration 035 -- Certificado Digital A1
-- Armazena o .pfx criptografado e metadados do certificado

ALTER TABLE parametros_sistema
    ADD COLUMN IF NOT EXISTS cert_pfx_encrypted  TEXT,        -- .pfx criptografado em AES-256
    ADD COLUMN IF NOT EXISTS cert_senha_hash      TEXT,        -- senha criptografada
    ADD COLUMN IF NOT EXISTS cert_tipo            VARCHAR(10), -- 'a1' | 'a3_nuvem'
    ADD COLUMN IF NOT EXISTS cert_titular         VARCHAR(200),-- nome do titular (ex: BARBARA FONSECA TATSCH)
    ADD COLUMN IF NOT EXISTS cert_cpf             VARCHAR(20), -- CPF do titular
    ADD COLUMN IF NOT EXISTS cert_validade        DATE,        -- data de vencimento
    ADD COLUMN IF NOT EXISTS cert_ativo           BOOLEAN NOT NULL DEFAULT FALSE,
    ADD COLUMN IF NOT EXISTS cert_atualizado_em   TIMESTAMPTZ;
