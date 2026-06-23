-- Trava duplicidade de usuario por email na raiz (o que causou o bug do
-- owner duplicado na clinica de teste). Aplica no molde e propaga pra
-- todas as clinicas ativas, seguindo o padrao do
-- database/_TEMPLATE_migracao_multi_schema.sql (ver Padrao Critico #26
-- no projeto.md).
--
-- Se algum schema ainda tiver emails duplicados, o ALTER TABLE dele vai
-- falhar e aparecer um WARNING -- nesse caso precisa limpar a duplicata
-- antes (ver database/047 e 051 como exemplo) e rodar este script de novo
-- so pra esse schema.

DO $$
DECLARE
    tenant_schema RECORD;
BEGIN
    -- 1) molde
    BEGIN
        ALTER TABLE vet_barbarafonseca.users ADD CONSTRAINT users_email_unique UNIQUE (email);
        RAISE NOTICE 'OK: vet_barbarafonseca';
    EXCEPTION WHEN duplicate_object THEN
        RAISE NOTICE 'vet_barbarafonseca ja tinha a constraint';
    WHEN OTHERS THEN
        RAISE WARNING 'FALHOU vet_barbarafonseca -- % (provavelmente tem duplicata -- limpar antes)', SQLERRM;
    END;

    -- 2) todas as clinicas ativas
    FOR tenant_schema IN
        SELECT t.schema_name
        FROM platform.tenants t
        WHERE t.ativo AND t.schema_name IS NOT NULL
              AND t.schema_name <> 'vet_barbarafonseca'
    LOOP
        BEGIN
            EXECUTE format('ALTER TABLE %I.users ADD CONSTRAINT users_email_unique UNIQUE (email)', tenant_schema.schema_name);
            RAISE NOTICE 'OK: %', tenant_schema.schema_name;
        EXCEPTION WHEN duplicate_object THEN
            RAISE NOTICE '% ja tinha a constraint', tenant_schema.schema_name;
        WHEN OTHERS THEN
            RAISE WARNING 'FALHOU % -- % (provavelmente tem duplicata -- limpar antes)', tenant_schema.schema_name, SQLERRM;
        END;
    END LOOP;
END $$;
