-- Modulo Gestao de Contratos (compra e venda / fornecimento recorrente).
-- Cria contratos + contrato_parcelas no molde (vet_barbarafonseca) E em
-- todas as clinicas ativas que ja existem (clinica nova ja pega isso
-- automatico, via ProvisionamentoService, na proxima vez que for criada).
-- Tambem adiciona contrato_parcela_id em contas, pra linkar o titulo em
-- "previsao" a parcela que o gerou.
--
-- Idempotente (IF NOT EXISTS / ADD COLUMN IF NOT EXISTS). Cada schema no
-- seu proprio bloco de exception -- um erro num schema nao trava os outros.

DO $$
DECLARE
    esquemas TEXT[];
    s TEXT;
BEGIN
    esquemas := ARRAY['vet_barbarafonseca'];
    esquemas := esquemas || ARRAY(
        SELECT t.schema_name FROM platform.tenants t
        WHERE t.ativo AND t.schema_name IS NOT NULL AND t.schema_name <> 'vet_barbarafonseca'
    );

    FOREACH s IN ARRAY esquemas
    LOOP
        BEGIN
            EXECUTE format($ddl$
                CREATE TABLE IF NOT EXISTS %I.contratos (
                    id uuid PRIMARY KEY,
                    tenant_id uuid NOT NULL,
                    fornecedor_id uuid NOT NULL REFERENCES %I.fornecedores(id),
                    descricao varchar(255) NOT NULL DEFAULT '',
                    valor_total numeric NOT NULL DEFAULT 0,
                    condicao_pagamento_id uuid REFERENCES %I.condicoes_pagamento(id),
                    numero_parcelas integer NOT NULL DEFAULT 1,
                    data_inicio date NOT NULL DEFAULT CURRENT_DATE,
                    status varchar(20) NOT NULL DEFAULT 'ativo',
                    obs text,
                    criado_por uuid,
                    criado_em timestamptz NOT NULL DEFAULT now(),
                    atualizado_em timestamptz NOT NULL DEFAULT now()
                );
            $ddl$, s, s, s);

            EXECUTE format($ddl$
                CREATE TABLE IF NOT EXISTS %I.contrato_parcelas (
                    id uuid PRIMARY KEY,
                    tenant_id uuid NOT NULL,
                    contrato_id uuid NOT NULL REFERENCES %I.contratos(id),
                    numero integer NOT NULL,
                    valor_previsto numeric NOT NULL DEFAULT 0,
                    data_prevista date NOT NULL,
                    conta_id uuid,
                    status_medicao varchar(20) NOT NULL DEFAULT 'pendente',
                    quantidade_medida numeric,
                    obs_medicao text,
                    medido_por uuid,
                    medido_em timestamptz,
                    aprovado_por uuid,
                    aprovado_em timestamptz,
                    motivo_rejeicao text
                );
            $ddl$, s, s);

            -- ADD CONSTRAINT nao aceita IF NOT EXISTS no Postgres -- bloco
            -- proprio pra nao travar o resto do schema numa segunda execucao.
            BEGIN
                EXECUTE format(
                    'ALTER TABLE %I.contrato_parcelas ADD CONSTRAINT fk_parcela_conta FOREIGN KEY (conta_id) REFERENCES %I.contas(id)',
                    s, s
                );
            EXCEPTION WHEN duplicate_object THEN
                NULL; -- constraint ja existe, segue normal
            END;

            EXECUTE format(
                'ALTER TABLE %I.contas ADD COLUMN IF NOT EXISTS contrato_parcela_id uuid REFERENCES %I.contrato_parcelas(id)',
                s, s
            );

            RAISE NOTICE 'OK: %', s;
        EXCEPTION WHEN OTHERS THEN
            RAISE WARNING 'FALHOU em % -- %', s, SQLERRM;
        END;
    END LOOP;
END $$;
