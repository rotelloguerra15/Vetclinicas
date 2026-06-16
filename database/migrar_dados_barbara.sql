-- ============================================================
-- Migra dados do schema public -> vet_barbarafonseca
-- Colunas ajustadas para bater com o banco real da Dra. Barbara
-- ============================================================

-- Tutores (num_end -> numero, endereco ignorado, pontos ignorado)
INSERT INTO vet_barbarafonseca.tutores (
    id, tenant_id, nome, cpf_cnpj, telefone, email,
    logradouro, numero, complemento, bairro, cidade, estado, cep,
    observacoes, aceita_promocoes, aniversario_dia, aniversario_mes,
    ativo, criado_em, atualizado_em
)
SELECT
    id, tenant_id, nome, cpf_cnpj, telefone, email,
    logradouro, num_end, complemento, bairro, cidade, estado, cep,
    obs, aceita_promocoes, aniversario_dia, aniversario_mes,
    ativo, criado_em, criado_em
FROM public.tutores
WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Pets (peso_kg -> peso, obs -> observacoes, microchip_num -> microchip)
INSERT INTO vet_barbarafonseca.pets (
    id, tenant_id, tutor_id, nome, especie, raca, sexo,
    data_nascimento, peso, castrado, foto_url, observacoes,
    microchip, ativo, criado_em, atualizado_em
)
SELECT
    id, tenant_id, tutor_id, nome, especie, raca, sexo,
    data_nascimento::timestamptz, peso_kg, castrado, foto_url, obs,
    microchip_num, ativo, criado_em, criado_em
FROM public.pets
WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Servicos
INSERT INTO vet_barbarafonseca.servicos
    SELECT * FROM public.servicos
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Categorias financeiras
INSERT INTO vet_barbarafonseca.categorias_financeiras
    SELECT * FROM public.categorias_financeiras
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Contas (obs_baixa ignorado, os_id/venda_id ignorados pois nao existem na nova tabela)
INSERT INTO vet_barbarafonseca.contas (
    id, tenant_id, pedido_id, tipo, categoria_id, descricao, valor,
    data_competencia, data_vencimento, forma_pagamento, status,
    valor_pago, data_baixa, conta_bancaria, criado_por,
    criado_em, atualizado_em
)
SELECT
    id, tenant_id, pedido_id, tipo, categoria_id, descricao, valor,
    data_competencia, data_vencimento, forma_pagamento, status,
    valor_pago, data_baixa, conta_bancaria, criado_por,
    criado_em, atualizado_em
FROM public.contas
WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Produtos
INSERT INTO vet_barbarafonseca.produtos
    SELECT * FROM public.produtos
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Vendas
INSERT INTO vet_barbarafonseca.vendas
    SELECT * FROM public.vendas
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Venda itens
INSERT INTO vet_barbarafonseca.venda_itens
    SELECT vi.* FROM public.venda_itens vi
    JOIN public.vendas v ON vi.venda_id = v.id
    WHERE v.tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Agendamentos
INSERT INTO vet_barbarafonseca.agendamentos
    SELECT * FROM public.agendamentos
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Ordens de servico
INSERT INTO vet_barbarafonseca.ordens_servico
    SELECT * FROM public.ordens_servico
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- OS Servicos
INSERT INTO vet_barbarafonseca.os_servicos
    SELECT oss.* FROM public.os_servicos oss
    JOIN public.ordens_servico os ON oss.os_id = os.id
    WHERE os.tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Prontuario
INSERT INTO vet_barbarafonseca.prontuario_itens
    SELECT * FROM public.prontuario_itens
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Vacinacoes
INSERT INTO vet_barbarafonseca.vacinacoes
    SELECT * FROM public.vacinacoes
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Contas bancarias
INSERT INTO vet_barbarafonseca.contas_bancarias
    SELECT * FROM public.contas_bancarias
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Movimentacoes bancarias
INSERT INTO vet_barbarafonseca.movimentacoes_bancarias
    SELECT * FROM public.movimentacoes_bancarias
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Caixas
INSERT INTO vet_barbarafonseca.caixas
    SELECT * FROM public.caixas
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Movimentacoes caixa
INSERT INTO vet_barbarafonseca.movimentacoes_caixa
    SELECT * FROM public.movimentacoes_caixa
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Cargos
INSERT INTO vet_barbarafonseca.cargos
    SELECT * FROM public.cargos
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Funcionarios
INSERT INTO vet_barbarafonseca.funcionarios
    SELECT * FROM public.funcionarios
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Comissoes
INSERT INTO vet_barbarafonseca.comissoes
    SELECT * FROM public.comissoes
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Fechamento mensal
INSERT INTO vet_barbarafonseca.fechamento_mensal
    SELECT * FROM public.fechamento_mensal
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Parametros sistema
INSERT INTO vet_barbarafonseca.parametros_sistema
    SELECT * FROM public.parametros_sistema
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Fornecedores
INSERT INTO vet_barbarafonseca.fornecedores
    SELECT * FROM public.fornecedores
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Condicoes pagamento
INSERT INTO vet_barbarafonseca.condicoes_pagamento
    SELECT * FROM public.condicoes_pagamento
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Pedidos compra
INSERT INTO vet_barbarafonseca.pedidos_compra
    SELECT * FROM public.pedidos_compra
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Pedido itens
INSERT INTO vet_barbarafonseca.pedido_itens
    SELECT pi.* FROM public.pedido_itens pi
    JOIN public.pedidos_compra pc ON pi.pedido_id = pc.id
    WHERE pc.tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Recebimentos mercadoria
INSERT INTO vet_barbarafonseca.recebimentos_mercadoria
    SELECT * FROM public.recebimentos_mercadoria
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Recebimento itens
INSERT INTO vet_barbarafonseca.recebimento_itens
    SELECT ri.* FROM public.recebimento_itens ri
    JOIN public.recebimentos_mercadoria rm ON ri.recebimento_id = rm.id
    WHERE rm.tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Recebimento anexos
INSERT INTO vet_barbarafonseca.recebimento_anexos
    SELECT * FROM public.recebimento_anexos
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Planos saude
INSERT INTO vet_barbarafonseca.planos_saude
    SELECT * FROM public.planos_saude
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Pet planos
INSERT INTO vet_barbarafonseca.pet_planos
    SELECT * FROM public.pet_planos
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Tutor planos
INSERT INTO vet_barbarafonseca.tutor_planos
    SELECT * FROM public.tutor_planos
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Bot config
INSERT INTO vet_barbarafonseca.bot_config
    SELECT * FROM public.bot_config
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Bot conversas
INSERT INTO vet_barbarafonseca.bot_conversas
    SELECT * FROM public.bot_conversas
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Bot logs
INSERT INTO vet_barbarafonseca.bot_logs
    SELECT * FROM public.bot_logs
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Config mensagens
INSERT INTO vet_barbarafonseca.config_mensagens
    SELECT * FROM public.config_mensagens
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Campanhas
INSERT INTO vet_barbarafonseca.campanhas
    SELECT * FROM public.campanhas
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Metas faturamento
INSERT INTO vet_barbarafonseca.metas_faturamento
    SELECT * FROM public.metas_faturamento
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Notificacoes fila
INSERT INTO vet_barbarafonseca.notificacoes_fila
    SELECT * FROM public.notificacoes_fila
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Racas
INSERT INTO vet_barbarafonseca.racas (id, tenant_id, nome, especie, ativo)
    SELECT id, tenant_id, nome, especie, ativo
    FROM public.racas
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Pelagens
INSERT INTO vet_barbarafonseca.pelagens (id, tenant_id, nome, ativo)
    SELECT id, tenant_id, nome, ativo
    FROM public.pelagens
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Vias administracao
INSERT INTO vet_barbarafonseca.vias_administracao (id, tenant_id, nome, ativo)
    SELECT id, tenant_id, nome, ativo
    FROM public.vias_administracao
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Fechamentos contabeis
INSERT INTO vet_barbarafonseca.fechamentos_contabeis
    SELECT * FROM public.fechamentos_contabeis
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Fechamento documentos
INSERT INTO vet_barbarafonseca.fechamento_documentos
    SELECT fd.* FROM public.fechamento_documentos fd
    JOIN public.fechamentos_contabeis fc ON fd.fechamento_id = fc.id
    WHERE fc.tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- ============================================================
-- VERIFICACAO FINAL
-- ============================================================
SELECT '=== RESULTADO ===' AS status;
SELECT 'tutores' , COUNT(*) FROM vet_barbarafonseca.tutores
UNION ALL SELECT 'pets'     , COUNT(*) FROM vet_barbarafonseca.pets
UNION ALL SELECT 'servicos' , COUNT(*) FROM vet_barbarafonseca.servicos
UNION ALL SELECT 'contas'   , COUNT(*) FROM vet_barbarafonseca.contas
UNION ALL SELECT 'vendas'   , COUNT(*) FROM vet_barbarafonseca.vendas
UNION ALL SELECT 'produtos' , COUNT(*) FROM vet_barbarafonseca.produtos;
