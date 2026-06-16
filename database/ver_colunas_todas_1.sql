SELECT table_name, column_name, data_type 
FROM information_schema.columns 
WHERE table_schema = 'public' 
AND table_name IN (
    'pets','servicos','produtos','vendas','venda_itens',
    'agendamentos','ordens_servico','prontuario_itens','vacinacoes',
    'movimentacoes_bancarias','funcionarios','parametros_sistema',
    'fornecedores','condicoes_pagamento','pedidos_compra',
    'recebimentos_mercadoria','recebimento_anexos','planos_saude',
    'pet_planos','tutor_planos','bot_config','bot_conversas',
    'bot_logs','config_mensagens','campanhas','metas_faturamento',
    'notificacoes_fila'
)
ORDER BY table_name, ordinal_position;
