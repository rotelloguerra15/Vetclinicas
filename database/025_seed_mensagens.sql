-- Migration 025 — Seed de mensagens automaticas e codigos

-- Insere templates padrao para todos os tenants que ainda nao tem config
INSERT INTO config_mensagens (id, tenant_id, gatilho, canal, ativo, horas_antes, template)
SELECT
    gen_random_uuid(),
    t.id,
    g.gatilho::gatilho_mensagem,
    'whatsapp',
    true,
    g.horas_antes,
    g.template
FROM tenants t
CROSS JOIN (VALUES
    ('aniversario_pet',      NULL, 'Feliz aniversario, {pet}! A {clinica} deseja um dia cheio de mimos e carinho para voce e seu tutor {tutor}. Ate a proxima consulta!'),
    ('aniversario_tutor',    NULL, 'Ola, {tutor}! A equipe da {clinica} deseja a voce um feliz aniversario! Obrigado por confiar em nos o cuidado do {pet}.'),
    ('boas_vindas_tutor',    NULL, 'Ola, {tutor}! Seja bem-vindo(a) a {clinica}. Estamos felizes em ter voce e seu pet conosco. Qualquer duvida, estamos a disposicao!'),
    ('boas_vindas_pet',      NULL, 'Ola, {tutor}! O {pet} foi cadastrado com sucesso na {clinica}. Agora vamos cuidar juntos da saude e bem-estar do seu companheiro!'),
    ('lembrete_agendamento', 24,   'Ola, {tutor}! Lembrando que {pet} tem horario marcado amanha, {data} as {hora}, na {clinica}. Qualquer duvida, nos avise!'),
    ('pos_atendimento_nps',  NULL, 'Ola, {tutor}! Como foi a experiencia com o {pet} na {clinica}? De 0 a 10, qual nota voce daria? Sua opiniao e muito importante para nos!'),
    ('vacina_vencendo',      NULL, 'Ola, {tutor}! A vacina do {pet} esta proxima do vencimento. Agende agora na {clinica} para manter a protecao em dia!')
) AS g(gatilho, horas_antes, template)
ON CONFLICT (tenant_id, gatilho) DO NOTHING;
