-- Migration 018 — Campos para receituário completo

-- Registro no MAPA no cadastro do funcionário/veterinário
ALTER TABLE funcionarios
    ADD COLUMN IF NOT EXISTS registro_mapa VARCHAR(30);

-- Código sequencial legível do pet (ex: 6893958 como no modelo)
-- Usamos uma sequence por tenant para gerar IDs amigáveis
ALTER TABLE pets
    ADD COLUMN IF NOT EXISTS codigo_sequencial INTEGER;

-- Popula os pets existentes com um código sequencial por tenant
DO $$
DECLARE
    r RECORD;
    seq INTEGER;
BEGIN
    FOR r IN SELECT DISTINCT tenant_id FROM pets LOOP
        seq := 1000000;
        UPDATE pets
           SET codigo_sequencial = seq + (ROW_NUMBER() OVER (ORDER BY criado_em))::INTEGER - 1
         WHERE tenant_id = r.tenant_id AND codigo_sequencial IS NULL;
    END LOOP;
END $$;
