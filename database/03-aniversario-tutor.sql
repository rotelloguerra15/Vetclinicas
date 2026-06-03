-- ============================================================
--  VetClinica — Migração 03
--  Aniversário do tutor (apenas dia/mês — menos invasivo, sem ano)
-- ============================================================

ALTER TABLE tutores
    ADD COLUMN aniversario_dia SMALLINT CHECK (aniversario_dia BETWEEN 1 AND 31),
    ADD COLUMN aniversario_mes SMALLINT CHECK (aniversario_mes BETWEEN 1 AND 12);

-- índice para a varredura diária de aniversariantes
CREATE INDEX idx_tutores_aniversario
    ON tutores(tenant_id, aniversario_mes, aniversario_dia)
    WHERE aniversario_dia IS NOT NULL;
