-- Apenas LEITURA. Lista, por clinica e tipo, quantas notificacoes estao
-- represadas em status 'pendente' e a janela de datas (agendado_para).
-- Roda isso ANTES de deixar o dispatcher seguir rodando sem supervisao,
-- pra saber se vai sair uma enxurrada de WhatsApp atrasado pros clientes.

DO $$
DECLARE
    r RECORD;
    c RECORD;
BEGIN
    FOR r IN
        SELECT t.schema_name
        FROM platform.tenants t
        WHERE t.ativo AND t.suspenso_em IS NULL AND t.schema_name IS NOT NULL
    LOOP
        IF EXISTS (
            SELECT 1 FROM information_schema.tables
            WHERE table_schema = r.schema_name AND table_name = 'notificacoes_fila'
        ) THEN
            FOR c IN EXECUTE format(
                'SELECT tipo, COUNT(*) AS qtd, MIN(agendado_para) AS mais_antiga, MAX(agendado_para) AS mais_recente
                 FROM %I.notificacoes_fila WHERE status = ''pendente'' GROUP BY tipo ORDER BY qtd DESC',
                r.schema_name
            )
            LOOP
                RAISE NOTICE '% | tipo=% | qtd=% | mais_antiga=% | mais_recente=%',
                    r.schema_name, c.tipo, c.qtd, c.mais_antiga, c.mais_recente;
            END LOOP;
        END IF;
    END LOOP;
END $$;
