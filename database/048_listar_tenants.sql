-- Apenas LEITURA. Lista todas as clinicas pra confirmar exatamente
-- qual schema corresponde a qual nome antes de qualquer exclusao.
SELECT id, nome, email, schema_name, plano, criado_em
FROM platform.tenants
ORDER BY criado_em;
