-- ============================================================
--  VetClinica — Migração 04
--  Branding: Clínica Veterinária Fonseca
-- ============================================================

UPDATE tenants
   SET nome = 'Clínica Veterinária Fonseca',
       email = 'contato@clinicafonseca.com.br'
 WHERE id = '00000000-0000-0000-0000-000000000001';

UPDATE users
   SET email = 'admin@clinicafonseca.com.br',
       nome = 'Administrador Fonseca'
 WHERE tenant_id = '00000000-0000-0000-0000-000000000001'
   AND papel = 'owner';
