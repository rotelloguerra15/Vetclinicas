SELECT 'pets' AS tabela, column_name, data_type FROM information_schema.columns WHERE table_schema='public' AND table_name='pets' ORDER BY ordinal_position
UNION ALL
SELECT 'servicos', column_name, data_type FROM information_schema.columns WHERE table_schema='public' AND table_name='servicos' ORDER BY ordinal_position
UNION ALL
SELECT 'produtos', column_name, data_type FROM information_schema.columns WHERE table_schema='public' AND table_name='produtos' ORDER BY ordinal_position
UNION ALL
SELECT 'vendas', column_name, data_type FROM information_schema.columns WHERE table_schema='public' AND table_name='vendas' ORDER BY ordinal_position
UNION ALL
SELECT 'venda_itens', column_name, data_type FROM information_schema.columns WHERE table_schema='public' AND table_name='venda_itens' ORDER BY ordinal_position
UNION ALL
SELECT 'agendamentos', column_name, data_type FROM information_schema.columns WHERE table_schema='public' AND table_name='agendamentos' ORDER BY ordinal_position
UNION ALL
SELECT 'ordens_servico', column_name, data_type FROM information_schema.columns WHERE table_schema='public' AND table_name='ordens_servico' ORDER BY ordinal_position
UNION ALL
SELECT 'prontuario_itens', column_name, data_type FROM information_schema.columns WHERE table_schema='public' AND table_name='prontuario_itens' ORDER BY ordinal_position
UNION ALL
SELECT 'vacinacoes', column_name, data_type FROM information_schema.columns WHERE table_schema='public' AND table_name='vacinacoes' ORDER BY ordinal_position
UNION ALL
SELECT 'movimentacoes_bancarias', column_name, data_type FROM information_schema.columns WHERE table_schema='public' AND table_name='movimentacoes_bancarias' ORDER BY ordinal_position
UNION ALL
SELECT 'funcionarios', column_name, data_type FROM information_schema.columns WHERE table_schema='public' AND table_name='funcionarios' ORDER BY ordinal_position
UNION ALL
SELECT 'parametros_sistema', column_name, data_type FROM information_schema.columns WHERE table_schema='public' AND table_name='parametros_sistema' ORDER BY ordinal_position
UNION ALL
SELECT 'fornecedores', column_name, data_type FROM information_schema.columns WHERE table_schema='public' AND table_name='fornecedores' ORDER BY ordinal_position
UNION ALL
SELECT 'condicoes_pagamento', column_name, data_type FROM information_schema.columns WHERE table_schema='public' AND table_name='condicoes_pagamento' ORDER BY ordinal_position
UNION ALL
SELECT 'pedidos_compra', column_name, data_type FROM information_schema.columns WHERE table_schema='public' AND table_name='pedidos_compra' ORDER BY ordinal_position
UNION ALL
SELECT 'recebimentos_mercadoria', column_name, data_type FROM information_schema.columns WHERE table_schema='public' AND table_name='recebimentos_mercadoria' ORDER BY ordinal_position
UNION ALL
SELECT 'recebimento_anexos', column_name, data_type FROM information_schema.columns WHERE table_schema='public' AND table_name='recebimento_anexos' ORDER BY ordinal_position
UNION ALL
SELECT 'planos_saude', column_name, data_type FROM information_schema.columns WHERE table_schema='public' AND table_name='planos_saude' ORDER BY ordinal_position
UNION ALL
SELECT 'pet_planos', column_name, data_type FROM information_schema.columns WHERE table_schema='public' AND table_name='pet_planos' ORDER BY ordinal_position
UNION ALL
SELECT 'tutor_planos', column_name, data_type FROM information_schema.columns WHERE table_schema='public' AND table_name='tutor_planos' ORDER BY ordinal_position
UNION ALL
SELECT 'bot_config', column_name, data_type FROM information_schema.columns WHERE table_schema='public' AND table_name='bot_config' ORDER BY ordinal_position
UNION ALL
SELECT 'bot_conversas', column_name, data_type FROM information_schema.columns WHERE table_schema='public' AND table_name='bot_conversas' ORDER BY ordinal_position
UNION ALL
SELECT 'bot_logs', column_name, data_type FROM information_schema.columns WHERE table_schema='public' AND table_name='bot_logs' ORDER BY ordinal_position
UNION ALL
SELECT 'config_mensagens', column_name, data_type FROM information_schema.columns WHERE table_schema='public' AND table_name='config_mensagens' ORDER BY ordinal_position
UNION ALL
SELECT 'campanhas', column_name, data_type FROM information_schema.columns WHERE table_schema='public' AND table_name='campanhas' ORDER BY ordinal_position
UNION ALL
SELECT 'metas_faturamento', column_name, data_type FROM information_schema.columns WHERE table_schema='public' AND table_name='metas_faturamento' ORDER BY ordinal_position
UNION ALL
SELECT 'notificacoes_fila', column_name, data_type FROM information_schema.columns WHERE table_schema='public' AND table_name='notificacoes_fila' ORDER BY ordinal_position;
