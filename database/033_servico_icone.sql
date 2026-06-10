-- Migration 033 — campo icone em servicos
ALTER TABLE servicos
    ADD COLUMN IF NOT EXISTS icone VARCHAR(10) DEFAULT '🐾';

-- Seed: preenche ícones baseado na categoria existente
UPDATE servicos SET icone = '🩺' WHERE categoria = 'consulta'   AND (icone IS NULL OR icone = '🐾');
UPDATE servicos SET icone = '🛁' WHERE categoria = 'banho_tosa' AND (icone IS NULL OR icone = '🐾');
UPDATE servicos SET icone = '✂️' WHERE categoria = 'tosa'       AND (icone IS NULL OR icone = '🐾');
UPDATE servicos SET icone = '💉' WHERE categoria = 'vacina'     AND (icone IS NULL OR icone = '🐾');
UPDATE servicos SET icone = '⚕️' WHERE categoria = 'cirurgia'   AND (icone IS NULL OR icone = '🐾');
UPDATE servicos SET icone = '🔬' WHERE categoria = 'exame'      AND (icone IS NULL OR icone = '🐾');
UPDATE servicos SET icone = '🔄' WHERE categoria = 'retorno'    AND (icone IS NULL OR icone = '🐾');
