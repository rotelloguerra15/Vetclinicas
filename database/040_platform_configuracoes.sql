-- Migration 040: tabela de configuracoes da plataforma (SMTP, etc)
CREATE TABLE IF NOT EXISTS platform.configuracoes (
    chave       TEXT PRIMARY KEY,
    valor       TEXT,
    atualizado_em TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Insere defaults do SMTP se nao existirem
INSERT INTO platform.configuracoes (chave, valor) VALUES
    ('smtp_host',      'smtp.office365.com'),
    ('smtp_porta',     '587'),
    ('smtp_usuario',   'workflow@ketra.com.br'),
    ('smtp_senha',     ''),
    ('smtp_ssl',       'false'),
    ('smtp_remetente', 'workflow@ketra.com.br')
ON CONFLICT (chave) DO NOTHING;
