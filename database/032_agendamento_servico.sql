-- Migration 032 — servico_id em agendamentos
-- Vincula o agendamento a um serviço real do cadastro

ALTER TABLE agendamentos
    ADD COLUMN IF NOT EXISTS servico_id UUID REFERENCES servicos(id);

-- Índice para buscas
CREATE INDEX IF NOT EXISTS idx_agendamentos_servico ON agendamentos(servico_id);
