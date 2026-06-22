-- Cobranca SaaS ainda eh manual (Asaas / recorrencia = backlog item 7, nao integrado).
-- Esses campos sao preenchidos a mao pelo superadmin no Painel da Plataforma
-- enquanto a automacao nao existe.
ALTER TABLE platform.tenants
  ADD COLUMN IF NOT EXISTS status_pagamento   VARCHAR(20),
  ADD COLUMN IF NOT EXISTS proximo_faturamento DATE;
