-- Exclusao definitiva e irreversivel das 3 clinicas TRIAL (dados de teste):
--   012.162.006-96  (vet_01216200696)
--   Soares           (vet_soares)
--   Soaresas         (vet_soaresas)
-- Mantem intacta: Pet Shop e Consultorio Dra. Barbara Fonseca (vet_barbarafonseca)
-- -- essa NUNCA aparece aqui, de proposito (e tambem o molde usado pra criar
-- clinica nova).
--
-- Tudo numa transacao so: se algo falhar no meio, nada e aplicado.

BEGIN;

DROP SCHEMA IF EXISTS vet_01216200696 CASCADE;
DROP SCHEMA IF EXISTS vet_soares      CASCADE;
DROP SCHEMA IF EXISTS vet_soaresas    CASCADE;

DELETE FROM platform.tenants
WHERE id IN (
    '1264b19e-fbe7-46a6-b6af-b07fec6a5a18', -- 012.162.006-96
    '0e119e50-ddcb-412c-ac20-aa162f15aefa', -- Soares
    'd0e63d19-cd80-4c46-8716-08a9717f06cc'  -- Soaresas
);

COMMIT;
