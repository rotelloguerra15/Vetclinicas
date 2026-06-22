-- Repara de uma vez TODA coluna que esteja faltando em qualquer tabela de
-- qualquer clinica, comparando contra vet_barbarafonseca (o schema usado
-- como molde pelo ProvisionamentoService para clonar clinicas novas).
--
-- So adiciona colunas em tabelas que JA EXISTEM no schema da clinica --
-- nao cria tabela nova (drift de tabela inteira faltando e outro problema,
-- mais raro, que precisaria de uma revisao separada se aparecer).
--
-- Limitacao conhecida: nao recria FOREIGN KEY/CHECK constraints (isso nao
-- fica no information_schema.columns) -- so o tipo, NOT NULL e DEFAULT.
-- A coluna funciona normalmente pra leitura/escrita, so fica sem a
-- trava extra de integridade referencial que a tabela original tinha.
--
-- Idempotente: seguro rodar de novo a qualquer momento.

DO $$
DECLARE
    tenant_schema RECORD;
    tbl           RECORD;
    col           RECORD;
    tipo_sql      TEXT;
    default_sql   TEXT;
    nulo_sql      TEXT;
    ddl           TEXT;
    total         INT := 0;
BEGIN
    FOR tenant_schema IN
        SELECT t.schema_name
        FROM platform.tenants t
        WHERE t.ativo AND t.schema_name IS NOT NULL
              AND t.schema_name <> 'vet_barbarafonseca'
    LOOP
        FOR tbl IN
            SELECT DISTINCT table_name
            FROM information_schema.tables
            WHERE table_schema = 'vet_barbarafonseca'
        LOOP
            IF EXISTS (
                SELECT 1 FROM information_schema.tables
                WHERE table_schema = tenant_schema.schema_name AND table_name = tbl.table_name
            ) THEN
                FOR col IN
                    SELECT c.column_name, c.data_type, c.character_maximum_length,
                           c.numeric_precision, c.numeric_scale, c.is_nullable,
                           c.column_default, c.udt_name
                    FROM information_schema.columns c
                    WHERE c.table_schema = 'vet_barbarafonseca' AND c.table_name = tbl.table_name
                      AND NOT EXISTS (
                          SELECT 1 FROM information_schema.columns c2
                          WHERE c2.table_schema = tenant_schema.schema_name
                            AND c2.table_name = tbl.table_name
                            AND c2.column_name = c.column_name
                      )
                LOOP
                    tipo_sql := CASE
                        WHEN col.data_type = 'character varying' AND col.character_maximum_length IS NOT NULL
                            THEN format('varchar(%s)', col.character_maximum_length)
                        WHEN col.data_type = 'numeric' AND col.numeric_precision IS NOT NULL
                            THEN format('numeric(%s,%s)', col.numeric_precision, COALESCE(col.numeric_scale, 0))
                        WHEN col.data_type = 'USER-DEFINED'
                            THEN col.udt_name
                        ELSE col.data_type
                    END;

                    default_sql := CASE WHEN col.column_default IS NOT NULL
                        THEN format(' DEFAULT %s', col.column_default) ELSE '' END;

                    nulo_sql := CASE WHEN col.is_nullable = 'NO' AND col.column_default IS NOT NULL
                        THEN ' NOT NULL' ELSE '' END;

                    IF col.is_nullable = 'NO' AND col.column_default IS NULL THEN
                        RAISE WARNING '% . % . % era NOT NULL sem DEFAULT no molde -- adicionada como NULLABLE (revisar manualmente se precisar)',
                            tenant_schema.schema_name, tbl.table_name, col.column_name;
                    END IF;

                    ddl := format('ALTER TABLE %I.%I ADD COLUMN %I %s%s%s',
                        tenant_schema.schema_name, tbl.table_name, col.column_name,
                        tipo_sql, default_sql, nulo_sql);

                    RAISE NOTICE '%', ddl;
                    EXECUTE ddl;
                    total := total + 1;
                END LOOP;
            END IF;
        END LOOP;
    END LOOP;

    RAISE NOTICE '--- TOTAL DE COLUNAS ADICIONADAS: % ---', total;
END $$;
