-- ============================================================
--  VetClinica — Migração 07
--  Converte colunas de enum PostgreSQL para VARCHAR
--  (compatibilidade com Entity Framework / Npgsql)
-- ============================================================

-- tenants
ALTER TABLE tenants ALTER COLUMN plano TYPE VARCHAR(20) USING plano::text;

-- users
ALTER TABLE users ALTER COLUMN papel TYPE VARCHAR(30) USING papel::text;

-- pets
ALTER TABLE pets ALTER COLUMN especie TYPE VARCHAR(20) USING especie::text;
ALTER TABLE pets ALTER COLUMN sexo TYPE VARCHAR(20) USING sexo::text;

-- agendamentos
ALTER TABLE agendamentos ALTER COLUMN tipo TYPE VARCHAR(30) USING tipo::text;
ALTER TABLE agendamentos ALTER COLUMN status TYPE VARCHAR(30) USING status::text;
ALTER TABLE agendamentos ALTER COLUMN origem TYPE VARCHAR(30) USING origem::text;

-- ordens_servico
ALTER TABLE ordens_servico ALTER COLUMN status TYPE VARCHAR(30) USING status::text;

-- prontuario_itens
ALTER TABLE prontuario_itens ALTER COLUMN tipo TYPE VARCHAR(30) USING tipo::text;

-- notificacoes_fila
ALTER TABLE notificacoes_fila ALTER COLUMN canal TYPE VARCHAR(20) USING canal::text;
ALTER TABLE notificacoes_fila ALTER COLUMN status TYPE VARCHAR(20) USING status::text;

-- categorias_financeiras
ALTER TABLE categorias_financeiras ALTER COLUMN tipo TYPE VARCHAR(20) USING tipo::text;

-- lancamentos
ALTER TABLE lancamentos ALTER COLUMN tipo TYPE VARCHAR(20) USING tipo::text;

-- movimentacoes_estoque
ALTER TABLE movimentacoes_estoque ALTER COLUMN tipo TYPE VARCHAR(30) USING tipo::text;

-- vendas
ALTER TABLE vendas ALTER COLUMN status TYPE VARCHAR(20) USING status::text;

-- config_mensagens
ALTER TABLE config_mensagens ALTER COLUMN gatilho TYPE VARCHAR(40) USING gatilho::text;
ALTER TABLE config_mensagens ALTER COLUMN canal TYPE VARCHAR(20) USING canal::text;
-- remover o UNIQUE antigo que pode ter dependência do tipo enum
ALTER TABLE config_mensagens DROP CONSTRAINT IF EXISTS config_mensagens_tenant_id_gatilho_key;
ALTER TABLE config_mensagens ADD CONSTRAINT config_mensagens_tenant_id_gatilho_key UNIQUE (tenant_id, gatilho);

-- campanhas
ALTER TABLE campanhas ALTER COLUMN status TYPE VARCHAR(30) USING status::text;
ALTER TABLE campanhas ALTER COLUMN canal TYPE VARCHAR(20) USING canal::text;
