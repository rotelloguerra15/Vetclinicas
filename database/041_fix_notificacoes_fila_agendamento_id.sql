-- Corrige drift: alguma(s) clinica(s) ficaram com notificacoes_fila sem a
-- coluna agendamento_id (presente no design original em 01-schema.sql).
-- Varre todos os tenants ativos e so altera quem realmente estiver faltando
-- a coluna -- seguro rodar mais de uma vez (idempotente).

DO $$
DECLARE
    r RECORD;
BEGIN
    FOR r IN
        SELECT t.schema_name
        FROM platform.tenants t
        WHERE t.ativo AND t.suspenso_em IS NULL AND t.schema_name IS NOT NULL
    LOOP
        -- so age se a tabela existir nesse schema e a coluna estiver faltando
        IF EXISTS (
            SELECT 1 FROM information_schema.tables
            WHERE table_schema = r.schema_name AND table_name = 'notificacoes_fila'
        ) AND NOT EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_schema = r.schema_name
              AND table_name = 'notificacoes_fila'
              AND column_name = 'agendamento_id'
        ) THEN
            RAISE NOTICE 'Corrigindo schema: %', r.schema_name;
            EXECUTE format(
                'ALTER TABLE %I.notificacoes_fila ADD COLUMN agendamento_id UUID REFERENCES %I.agendamentos(id)',
                r.schema_name, r.schema_name
            );
        END IF;
    END LOOP;
END $$;
