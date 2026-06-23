-- 1) Remove o owner duplicado e desatualizado da clinica de teste
--    (mantem o id que bate com a senha atual "Felipe@13").
DELETE FROM vet_rotelloteste.users
WHERE id = '8a736fd9-99e3-46ce-b19d-ebf11b78d8e7';

-- 2) Verifica (somente leitura) se existe a MESMA duplicidade de email em
--    qualquer outra clinica ativa, pra saber se isso e isolado ou recorrente.
DO $$
DECLARE
    r RECORD;
    dup RECORD;
BEGIN
    FOR r IN
        SELECT t.schema_name
        FROM platform.tenants t
        WHERE t.ativo AND t.schema_name IS NOT NULL
    LOOP
        FOR dup IN EXECUTE format(
            'SELECT email, COUNT(*) AS qtd FROM %I.users GROUP BY email HAVING COUNT(*) > 1',
            r.schema_name
        )
        LOOP
            RAISE NOTICE '% -- email % duplicado % vezes', r.schema_name, dup.email, dup.qtd;
        END LOOP;
    END LOOP;
END $$;
