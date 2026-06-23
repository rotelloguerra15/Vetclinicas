-- Apenas LEITURA. Mostra TODOS os usuarios do schema de teste e confere
-- diretamente se a senha "Felipe@13" bate com o hash gravado -- sem passar
-- pela API, pra isolar se o problema e a senha ou e outra coisa (lookup,
-- email, etc).
CREATE EXTENSION IF NOT EXISTS pgcrypto;

SELECT id, email, nome, papel, ativo,
       (senha_hash = crypt('Felipe@13', senha_hash)) AS senha_felipe13_bate
FROM vet_rotelloteste.users;
