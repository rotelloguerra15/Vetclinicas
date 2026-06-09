-- Migration 024 — Campo codigo de controle em cadastros principais

-- Produtos
ALTER TABLE produtos
    ADD COLUMN IF NOT EXISTS codigo VARCHAR(30);

-- Gera codigo sequencial para produtos existentes por tenant
DO $$
DECLARE
    r RECORD;
    seq INT;
BEGIN
    FOR r IN SELECT DISTINCT tenant_id FROM produtos WHERE codigo IS NULL LOOP
        seq := 1;
        FOR r2 IN SELECT id FROM produtos WHERE tenant_id = r.tenant_id AND codigo IS NULL ORDER BY criado_em LOOP
            UPDATE produtos SET codigo = 'PRD' || LPAD(seq::TEXT, 4, '0') WHERE id = r2.id;
            seq := seq + 1;
        END LOOP;
    END LOOP;
END $$;

-- Fornecedores
ALTER TABLE fornecedores
    ADD COLUMN IF NOT EXISTS codigo VARCHAR(30);

DO $$
DECLARE
    r RECORD;
    seq INT;
BEGIN
    FOR r IN SELECT DISTINCT tenant_id FROM fornecedores WHERE codigo IS NULL LOOP
        seq := 1;
        FOR r2 IN SELECT id FROM fornecedores WHERE tenant_id = r.tenant_id AND codigo IS NULL ORDER BY criado_em LOOP
            UPDATE fornecedores SET codigo = 'FOR' || LPAD(seq::TEXT, 4, '0') WHERE id = r2.id;
            seq := seq + 1;
        END LOOP;
    END LOOP;
END $$;

-- Funcionarios
ALTER TABLE funcionarios
    ADD COLUMN IF NOT EXISTS codigo VARCHAR(30);

DO $$
DECLARE
    r RECORD;
    seq INT;
BEGIN
    FOR r IN SELECT DISTINCT tenant_id FROM funcionarios WHERE codigo IS NULL LOOP
        seq := 1;
        FOR r2 IN SELECT id FROM funcionarios WHERE tenant_id = r.tenant_id AND codigo IS NULL ORDER BY criado_em LOOP
            UPDATE funcionarios SET codigo = 'FUN' || LPAD(seq::TEXT, 4, '0') WHERE id = r2.id;
            seq := seq + 1;
        END LOOP;
    END LOOP;
END $$;
