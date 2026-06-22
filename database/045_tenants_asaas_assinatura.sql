-- Assinatura SaaS da Ketra (conta Asaas DA PLATAFORMA, diferente de asaas_api_key
-- que ja existe e e a conta de CADA CLINICA pra Pix do PDV).
-- platform.tenants e tabela central -- nao precisa propagar pra schemas de clinica.
ALTER TABLE platform.tenants
  ADD COLUMN IF NOT EXISTS asaas_cliente_id    VARCHAR(60),
  ADD COLUMN IF NOT EXISTS asaas_assinatura_id VARCHAR(60);
