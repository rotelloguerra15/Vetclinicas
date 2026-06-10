-- Migration 033 -- campo icone em servicos
ALTER TABLE servicos
    ADD COLUMN IF NOT EXISTS icone VARCHAR(10) DEFAULT 'pata';

-- Seed: preenche icone baseado na categoria (sem emojis no SQL)
UPDATE servicos SET icone = 'consulta'   WHERE categoria = 'consulta';
UPDATE servicos SET icone = 'banho_tosa' WHERE categoria = 'banho_tosa';
UPDATE servicos SET icone = 'banho'      WHERE categoria = 'banho';
UPDATE servicos SET icone = 'tosa'       WHERE categoria = 'tosa';
UPDATE servicos SET icone = 'vacina'     WHERE categoria = 'vacina';
UPDATE servicos SET icone = 'cirurgia'   WHERE categoria = 'cirurgia';
UPDATE servicos SET icone = 'exame'      WHERE categoria = 'exame';
UPDATE servicos SET icone = 'retorno'    WHERE categoria = 'retorno';
