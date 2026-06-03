-- Migration 016 — M1 Ajustes
-- 1. Endereço desmembrado na tabela tutores
-- 2. Microchip e plano de saúde na tabela pets

-- ── Tutor: endereço desmembrado ───────────────────────────────────────────────
ALTER TABLE tutores
    ADD COLUMN IF NOT EXISTS logradouro  VARCHAR(200),
    ADD COLUMN IF NOT EXISTS num_end     VARCHAR(20),
    ADD COLUMN IF NOT EXISTS complemento VARCHAR(100),
    ADD COLUMN IF NOT EXISTS bairro      VARCHAR(100),
    ADD COLUMN IF NOT EXISTS cidade      VARCHAR(100),
    ADD COLUMN IF NOT EXISTS estado      CHAR(2),
    ADD COLUMN IF NOT EXISTS cep         VARCHAR(9);

-- Migra o campo legado "endereco" para "logradouro" onde ainda não foi preenchido
UPDATE tutores
   SET logradouro = endereco
 WHERE endereco IS NOT NULL
   AND logradouro IS NULL;

-- ── Pet: microchip ────────────────────────────────────────────────────────────
ALTER TABLE pets
    ADD COLUMN IF NOT EXISTS tem_microchip   BOOLEAN DEFAULT FALSE,
    ADD COLUMN IF NOT EXISTS microchip_num   VARCHAR(50);

-- ── Pet: plano de saúde ───────────────────────────────────────────────────────
ALTER TABLE pets
    ADD COLUMN IF NOT EXISTS tem_plano_saude     BOOLEAN DEFAULT FALSE,
    ADD COLUMN IF NOT EXISTS plano_saude_nome     VARCHAR(100),
    ADD COLUMN IF NOT EXISTS plano_saude_carteira VARCHAR(50);
