-- Apenas LEITURA. Para cada clinica ativa, conta quantos usuarios no schema
-- dela tem o mesmo email cadastrado em platform.tenants.email (o que devia
-- ser o owner). Mostra so os casos suspeitos:
--   0 = "owner nao encontrado" (o que apareceu no painel)
--   2+ = duplicidade -- explica "resetei mas a senha nao mudou pro usuario certo"
-- Tambem mostra o Papel de cada linha encontrada, pra confirmar se "owner" e
-- realmente o valor usado em todas, ou se varia.

DO $$
DECLARE
    r RECORD;
    linha RECORD;
    qtd INT;
BEGIN
    FOR r IN
        SELECT t.nome, t.email, t.schema_name
        FROM platform.tenants t
        WHERE t.ativo AND t.schema_name IS NOT NULL
    LOOP
        IF EXISTS (
            SELECT 1 FROM information_schema.tables
            WHERE table_schema = r.schema_name AND table_name = 'users'
        ) THEN
            EXECUTE format('SELECT COUNT(*) FROM %I.users WHERE email = $1', r.schema_name)
                INTO qtd USING r.email;

            IF qtd = 0 THEN
                RAISE NOTICE '% (%) -- SEM USUARIO com email %', r.nome, r.schema_name, r.email;
            ELSIF qtd > 1 THEN
                RAISE NOTICE '% (%) -- % USUARIOS com o mesmo email % (duplicidade)', r.nome, r.schema_name, qtd, r.email;
                FOR linha IN EXECUTE format(
                    'SELECT id, nome, papel, ativo FROM %I.users WHERE email = $1', r.schema_name
                ) USING r.email
                LOOP
                    RAISE NOTICE '    -> id=% nome=% papel=% ativo=%', linha.id, linha.nome, linha.papel, linha.ativo;
                END LOOP;
            END IF;
        END IF;
    END LOOP;
END $$;
