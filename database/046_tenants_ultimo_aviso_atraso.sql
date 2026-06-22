-- Controla o ultimo dia em que o aviso de atraso foi enviado, pra garantir
-- no maximo 1 email por dia por clinica (AssinaturaAtrasoWorker).
ALTER TABLE platform.tenants
  ADD COLUMN IF NOT EXISTS ultimo_aviso_atraso_em TIMESTAMPTZ;
