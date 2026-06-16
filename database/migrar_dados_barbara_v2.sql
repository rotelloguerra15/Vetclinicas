-- ============================================================
-- Migracao definitiva: public -> vet_barbarafonseca
-- Colunas mapeadas exatamente do banco real
-- ============================================================

-- Pets (castrado pode ser null no public, forcar FALSE)
INSERT INTO vet_barbarafonseca.pets (
    id, tenant_id, tutor_id, nome, especie, raca, sexo,
    data_nascimento, peso, castrado, foto_url, observacoes,
    microchip, ativo, criado_em, atualizado_em
)
SELECT
    id, tenant_id, tutor_id, nome, especie, raca, sexo,
    data_nascimento::timestamptz,
    peso_kg,
    COALESCE(castrado, FALSE),
    foto_url,
    obs,
    microchip_num,
    ativo, criado_em, criado_em
FROM public.pets
WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Servicos (nao tem codigo no public)
INSERT INTO vet_barbarafonseca.servicos (
    id, tenant_id, nome, categoria, descricao,
    preco_base, duracao_min, ativo, icone
)
SELECT
    id, tenant_id, nome, categoria, descricao,
    preco_base, duracao_min, ativo, icone
FROM public.servicos
WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Produtos (codigo_barras ignorado, estoque_ideal ignorado, controla_validade ignorado)
INSERT INTO vet_barbarafonseca.produtos (
    id, tenant_id, codigo, nome, categoria, unidade,
    preco_custo, preco_venda, estoque_atual, estoque_minimo,
    ativo, criado_em
)
SELECT
    id, tenant_id, codigo, nome, categoria, unidade,
    preco_custo, preco_venda, estoque_atual, estoque_minimo,
    ativo, criado_em
FROM public.produtos
WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Vendas (valor_produtos->valor_subtotal, valor_desconto->desconto, finalizada_em ignorada)
INSERT INTO vet_barbarafonseca.vendas (
    id, tenant_id, tutor_id, valor_subtotal, valor_total,
    desconto, forma_pagamento, status, criado_em
)
SELECT
    id, tenant_id, tutor_id,
    COALESCE(valor_produtos, 0),
    COALESCE(valor_total, 0),
    COALESCE(valor_desconto, 0),
    forma_pagamento, status, criado_em
FROM public.vendas
WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Venda itens (subtotal->preco_total, sem desconto, sem servico_id)
INSERT INTO vet_barbarafonseca.venda_itens (
    id, tenant_id, venda_id, produto_id,
    quantidade, preco_unitario, preco_total
)
SELECT
    vi.id,
    v.tenant_id,
    vi.venda_id,
    vi.produto_id,
    vi.quantidade,
    vi.preco_unitario,
    vi.subtotal
FROM public.venda_itens vi
JOIN public.vendas v ON vi.venda_id = v.id
WHERE v.tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Agendamentos (sem servico_id no public original, mas tem aqui)
INSERT INTO vet_barbarafonseca.agendamentos (
    id, tenant_id, pet_id, user_id, servico_id, tipo,
    data_hora, duracao_min, status, origem, obs,
    confirmado_em, confirmado_por, criado_em
)
SELECT
    id, tenant_id, pet_id, user_id, servico_id, tipo,
    data_hora, duracao_min, status, origem, obs,
    confirmado_em, confirmado_por, criado_em
FROM public.agendamentos
WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Ordens servico (fotos_urls, plano_id, desconto_percent, valor_sem_desconto ignorados)
INSERT INTO vet_barbarafonseca.ordens_servico (
    id, tenant_id, agendamento_id, pet_id, user_id, funcionario_id,
    status, valor_total, obs, inicio, entregue_em, criado_em, atualizado_em
)
SELECT
    id, tenant_id, agendamento_id, pet_id, user_id, funcionario_id,
    status,
    COALESCE(valor_total, 0),
    obs, inicio, entregue_em, criado_em, criado_em
FROM public.ordens_servico
WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- OS Servicos
INSERT INTO vet_barbarafonseca.os_servicos (id, os_id, servico_id, preco_cobrado, obs)
SELECT oss.id, oss.os_id, oss.servico_id, oss.preco_cobrado, oss.obs
FROM public.os_servicos oss
JOIN public.ordens_servico os ON oss.os_id = os.id
WHERE os.tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Prontuario itens (sem tenant_id no public, pega via pet)
INSERT INTO vet_barbarafonseca.prontuario_itens (
    id, tenant_id, pet_id, tipo, titulo, descricao, criado_em
)
SELECT
    pi.id,
    p.tenant_id,
    pi.pet_id,
    pi.tipo,
    pi.titulo,
    pi.descricao,
    pi.criado_em
FROM public.prontuario_itens pi
JOIN public.pets p ON pi.pet_id = p.id
WHERE p.tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Vacinacoes (sem tenant_id no public, nome_vacina vem de vacina)
INSERT INTO vet_barbarafonseca.vacinacoes (
    id, tenant_id, pet_id, nome_vacina, lote,
    data_aplicacao, proxima_dose, criado_em
)
SELECT
    v.id,
    p.tenant_id,
    v.pet_id,
    v.vacina,
    v.lote,
    v.data_aplicacao,
    v.proxima_dose,
    v.criado_em
FROM public.vacinacoes v
JOIN public.pets p ON v.pet_id = p.id
WHERE p.tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Movimentacoes bancarias (categoria_id e conta_destino_id ignorados)
INSERT INTO vet_barbarafonseca.movimentacoes_bancarias (
    id, tenant_id, conta_bancaria_id, tipo, descricao,
    valor, data_movimentacao, conta_id, origem,
    conciliado, criado_por, criado_em
)
SELECT
    id, tenant_id, conta_bancaria_id, tipo, descricao,
    valor, data_movimentacao, conta_id, origem,
    COALESCE(conciliado, FALSE), criado_por, criado_em
FROM public.movimentacoes_bancarias
WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Funcionarios (data_nascimento e data_admissao sao date no public - ok)
INSERT INTO vet_barbarafonseca.funcionarios (
    id, tenant_id, codigo, nome, cpf, rg, data_nascimento,
    logradouro, numero, complemento, bairro, cidade, estado, cep,
    telefone, email, cargo, cargo_id, usuario_id, crmv, registro_mapa,
    data_admissao, salario, percentual_comissao,
    assina_receituario, status, criado_em, atualizado_em
)
SELECT
    id, tenant_id, codigo, nome, cpf, rg, data_nascimento,
    logradouro, numero, complemento, bairro, cidade, estado::text, cep,
    telefone, email, cargo, cargo_id, usuario_id, crmv, registro_mapa,
    data_admissao,
    COALESCE(salario, 0),
    COALESCE(percentual_comissao, 0),
    COALESCE(assina_receituario, FALSE),
    COALESCE(status, 'trabalhando'),
    criado_em, atualizado_em
FROM public.funcionarios
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

-- Parametros sistema (mesmas colunas)
INSERT INTO vet_barbarafonseca.parametros_sistema (
    id, tenant_id, comissao_os_ativo, comissao_pdv_ativo,
    anthropic_api_key, ia_ativo,
    cert_pfx_encrypted, cert_senha_hash, cert_tipo, cert_titular,
    cert_cpf, cert_validade, cert_ativo, cert_atualizado_em,
    smtp_host, smtp_porta, smtp_usuario, smtp_senha_enc, smtp_ssl,
    email_remetente, email_contabil, contabil_nome, atualizado_em
)
SELECT
    id, tenant_id, comissao_os_ativo, comissao_pdv_ativo,
    anthropic_api_key, ia_ativo,
    cert_pfx_encrypted, cert_senha_hash, cert_tipo, cert_titular,
    cert_cpf, cert_validade, cert_ativo, cert_atualizado_em,
    smtp_host, smtp_porta, smtp_usuario, smtp_senha_enc, smtp_ssl,
    email_remetente, email_contabil, contabil_nome, atualizado_em
FROM public.parametros_sistema
WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Fornecedores (num_end->numero, obs ignorado)
INSERT INTO vet_barbarafonseca.fornecedores (
    id, tenant_id, codigo, nome, cnpj, telefone, email,
    contato, logradouro, cidade, estado, cep, ativo, criado_em
)
SELECT
    id, tenant_id, codigo, nome, cnpj, telefone, email,
    contato, logradouro, cidade, estado::text, cep, ativo, criado_em
FROM public.fornecedores
WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Condicoes pagamento (tem criado_em no public)
INSERT INTO vet_barbarafonseca.condicoes_pagamento (
    id, tenant_id, nome, parcelas, intervalo_dias, ativo
)
SELECT id, tenant_id, nome, parcelas, intervalo_dias, ativo
FROM public.condicoes_pagamento
WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Pedidos compra (numero int->text, sem data_recebimento na nova tabela)
INSERT INTO vet_barbarafonseca.pedidos_compra (
    id, tenant_id, numero, fornecedor_id, condicao_pagamento_id,
    forma_pagamento, status, data_pedido, valor_total,
    parcelas, obs, criado_por, criado_em, atualizado_em
)
SELECT
    id, tenant_id, numero::text, fornecedor_id, condicao_pagamento_id,
    forma_pagamento, status, data_pedido,
    COALESCE(valor_total, 0),
    COALESCE(parcelas, 1),
    obs, criado_por, criado_em, atualizado_em
FROM public.pedidos_compra
WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Recebimentos mercadoria (sem numero_nf no public)
INSERT INTO vet_barbarafonseca.recebimentos_mercadoria (
    id, tenant_id, pedido_id, data_recebimento,
    status, observacoes, criado_por, criado_em
)
SELECT
    id, tenant_id, pedido_id, data_recebimento,
    status, obs, criado_por, criado_em
FROM public.recebimentos_mercadoria
WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Planos saude (desconto_percent->desconto_padrao, obs ignorado)
INSERT INTO vet_barbarafonseca.planos_saude (
    id, tenant_id, nome, operadora, desconto_padrao, ativo, criado_em
)
SELECT
    id, tenant_id, nome, operadora,
    COALESCE(desconto_percent, 0),
    ativo, criado_em
FROM public.planos_saude
WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Pet planos (num_carteirinha->numero_carteira, validade->data_fim)
INSERT INTO vet_barbarafonseca.pet_planos (
    id, tenant_id, pet_id, plano_id, numero_carteira,
    data_fim, ativo, criado_em
)
SELECT
    id, tenant_id, pet_id, plano_id, num_carteirinha,
    validade,
    COALESCE(ativo, TRUE), criado_em
FROM public.pet_planos
WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Tutor planos
INSERT INTO vet_barbarafonseca.tutor_planos (
    id, tenant_id, tutor_id, plano_id, numero_carteira,
    ativo, criado_em
)
SELECT
    id, tenant_id, tutor_id, plano_id, num_carteirinha,
    COALESCE(ativo, TRUE), criado_em
FROM public.tutor_planos
WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Bot config (muitas colunas extras no public, pega so as que existem na nova)
INSERT INTO vet_barbarafonseca.bot_config (
    id, tenant_id, ativo, meta_phone_number_id, meta_waba_id, criado_em
)
SELECT
    id, tenant_id, ativo, meta_phone_number_id, meta_waba_id, criado_em
FROM public.bot_config
WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Bot conversas (muitas colunas extras, pega as basicas)
INSERT INTO vet_barbarafonseca.bot_conversas (
    id, tenant_id, telefone, estado, criado_em, atualizado_em
)
SELECT
    id, tenant_id, telefone, estado, criado_em,
    COALESCE(ultima_msg_em, criado_em)
FROM public.bot_conversas
WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Config mensagens
INSERT INTO vet_barbarafonseca.config_mensagens (
    id, tenant_id, gatilho, canal, ativo, template, criado_em
)
SELECT id, tenant_id, gatilho, canal, ativo, template, criado_em
FROM public.config_mensagens
WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Campanhas (sem enviada_em no public original)
INSERT INTO vet_barbarafonseca.campanhas (
    id, tenant_id, titulo, mensagem, imagem_url, publico, canal,
    status, total_destinatarios, total_enviados,
    motivo_reprovacao, criado_por, aprovado_por, aprovado_em,
    enviada_em, criado_em
)
SELECT
    id, tenant_id, titulo, mensagem, imagem_url, publico, canal,
    status,
    COALESCE(total_destinatarios, 0),
    COALESCE(total_enviados, 0),
    motivo_reprovacao, criado_por, aprovado_por, aprovado_em,
    enviada_em, criado_em
FROM public.campanhas
WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Metas faturamento
INSERT INTO vet_barbarafonseca.metas_faturamento (
    id, tenant_id, ano, mes, valor_meta, criado_em
)
SELECT id, tenant_id, ano, mes, valor_meta, criado_em
FROM public.metas_faturamento
WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Notificacoes fila (payload jsonb->text, campos extras ignorados)
INSERT INTO vet_barbarafonseca.notificacoes_fila (
    id, tenant_id, tutor_id, canal, tipo, destinatario,
    payload, status, tentativas, agendado_para, criado_em
)
SELECT
    id, tenant_id, tutor_id, canal, tipo, destinatario,
    payload::text,
    COALESCE(status, 'pendente'),
    COALESCE(tentativas, 0),
    agendado_para, criado_em
FROM public.notificacoes_fila
WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Cargos
INSERT INTO vet_barbarafonseca.cargos
    SELECT * FROM public.cargos
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Racas
INSERT INTO vet_barbarafonseca.racas (id, tenant_id, nome, especie, ativo)
    SELECT id, tenant_id, nome, especie, ativo FROM public.racas
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Pelagens
INSERT INTO vet_barbarafonseca.pelagens (id, tenant_id, nome, ativo)
    SELECT id, tenant_id, nome, ativo FROM public.pelagens
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Vias administracao
INSERT INTO vet_barbarafonseca.vias_administracao (id, tenant_id, nome, ativo)
    SELECT id, tenant_id, nome, ativo FROM public.vias_administracao
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- Contas bancarias
INSERT INTO vet_barbarafonseca.contas_bancarias
    SELECT * FROM public.contas_bancarias
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

-- Bot logs
INSERT INTO vet_barbarafonseca.bot_logs (id, tenant_id, direcao, mensagem, criado_em)
    SELECT id, tenant_id, direcao, mensagem, criado_em
    FROM public.bot_logs
    WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
ON CONFLICT (id) DO NOTHING;

-- ============================================================
-- VERIFICACAO FINAL
-- ============================================================
SELECT 'tutores'     , COUNT(*) FROM vet_barbarafonseca.tutores
UNION ALL SELECT 'pets'      , COUNT(*) FROM vet_barbarafonseca.pets
UNION ALL SELECT 'servicos'  , COUNT(*) FROM vet_barbarafonseca.servicos
UNION ALL SELECT 'contas'    , COUNT(*) FROM vet_barbarafonseca.contas
UNION ALL SELECT 'vendas'    , COUNT(*) FROM vet_barbarafonseca.vendas
UNION ALL SELECT 'produtos'  , COUNT(*) FROM vet_barbarafonseca.produtos
UNION ALL SELECT 'agendamentos', COUNT(*) FROM vet_barbarafonseca.agendamentos
UNION ALL SELECT 'funcionarios', COUNT(*) FROM vet_barbarafonseca.funcionarios;
