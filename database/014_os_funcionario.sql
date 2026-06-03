-- Migration 14: adicionar funcionario_id na OS
ALTER TABLE ordens_servico
ADD COLUMN IF NOT EXISTS funcionario_id UUID REFERENCES funcionarios(id);

CREATE INDEX idx_os_funcionario ON ordens_servico(funcionario_id);
