-- Metas de vendas agora podem ser por Produto, Servico ou Ambos (em vez de
-- um valor unico por mes). Propaga no molde e em todas as clinicas ativas,
-- igual o padrao do _TEMPLATE_migracao_multi_schema.sql.

DO $$
DECLARE
    tenant_schema RECORD;
BEGIN
    BEGIN
        ALTER TABLE vet_barbarafonseca.metas_faturamento
            ADD COLUMN IF NOT EXISTS tipo VARCHAR(20) NOT NULL DEFAULT 'ambos';
        RAISE NOTICE 'OK: vet_barbarafonseca';
    EXCEPTION WHEN OTHERS THEN
        RAISE WARNING 'FALHOU vet_barbarafonseca -- %', SQLERRM;
    END;

    FOR tenant_schema IN
        SELECT t.schema_name FROM platform.tenants t
        WHERE t.ativo AND t.schema_name IS NOT NULL AND t.schema_name <> 'vet_barbarafonseca'
    LOOP
        BEGIN
            EXECUTE format(
                'ALTER TABLE %I.metas_faturamento ADD COLUMN IF NOT EXISTS tipo VARCHAR(20) NOT NULL DEFAULT ''ambos''',
                tenant_schema.schema_name
            );
            RAISE NOTICE 'OK: %', tenant_schema.schema_name;
        EXCEPTION WHEN OTHERS THEN
            RAISE WARNING 'FALHOU % -- %', tenant_schema.schema_name, SQLERRM;
        END;
    END LOOP;
END $$;
