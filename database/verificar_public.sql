-- Verifica estrutura das tabelas no schema public
SELECT column_name, data_type 
FROM information_schema.columns 
WHERE table_schema = 'public' AND table_name = 'tutores'
ORDER BY ordinal_position;

SELECT '---pets---';

SELECT column_name, data_type 
FROM information_schema.columns 
WHERE table_schema = 'public' AND table_name = 'pets'
ORDER BY ordinal_position;

SELECT '---contas---';

SELECT column_name, data_type 
FROM information_schema.columns 
WHERE table_schema = 'public' AND table_name = 'contas'
ORDER BY ordinal_position;

-- Conta quantos registros tem no public
SELECT 'public.tutores', COUNT(*) FROM public.tutores WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
UNION ALL SELECT 'public.pets', COUNT(*) FROM public.pets WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
UNION ALL SELECT 'public.contas', COUNT(*) FROM public.contas WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7'
UNION ALL SELECT 'public.vendas', COUNT(*) FROM public.vendas WHERE tenant_id = '4b19602f-0425-4101-914c-5b9aeaf5d4e7';
