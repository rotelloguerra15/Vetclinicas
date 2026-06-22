-- TEMPLATE para qualquer migracao futura que adicione/altere coluna ou
-- tabela. Use isso a partir de agora em vez de alterar so vet_barbarafonseca.
--
-- POR QUE: clinica nova sempre nasce certa (ProvisionamentoService.CriarClinica
-- e dinamico -- pega a estrutura ATUAL de vet_barbarafonseca na hora de criar).
-- O problema e o caminho inverso: clinicas que JA EXISTEM nao recebem
-- automaticamente uma alteracao feita so no molde depois. Esse template
-- aplica nos dois lugares na mesma rodada, pra nao repetir o drift que
-- corrigimos em 044_reparar_drift_colunas_todas_clinicas.sql.
--
-- COMO USAR: duplique este arquivo com o proximo numero (ex: 045_...sql),
-- troque NOME_DA_TABELA / NOME_DA_COLUNA / TIPO pelo que voce precisa,
-- e troque o ALTER TABLE de exemplo pelo seu (pode ser mais de uma linha).

DO $$
DECLARE
    tenant_schema RECORD;
BEGIN
    -- 1) aplica primeiro no molde (fonte de verdade usada para clinicas futuras)
    ALTER TABLE vet_barbarafonseca.NOME_DA_TABELA
        ADD COLUMN IF NOT EXISTS NOME_DA_COLUNA TIPO;

    -- 2) propaga para todas as clinicas que JA existem hoje
    FOR tenant_schema IN
        SELECT t.schema_name
        FROM platform.tenants t
        WHERE t.ativo AND t.schema_name IS NOT NULL
              AND t.schema_name <> 'vet_barbarafonseca'
    LOOP
        BEGIN
            EXECUTE format(
                'ALTER TABLE %I.NOME_DA_TABELA ADD COLUMN IF NOT EXISTS NOME_DA_COLUNA TIPO',
                tenant_schema.schema_name
            );
            RAISE NOTICE 'OK: %', tenant_schema.schema_name;
        EXCEPTION WHEN OTHERS THEN
            RAISE WARNING 'Falhou em % -- %', tenant_schema.schema_name, SQLERRM;
        END;
    END LOOP;
END $$;
