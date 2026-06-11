-- ============================================================
-- Migration 039 - Tabela racas
-- ============================================================

CREATE TABLE IF NOT EXISTS racas (
    id         UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id  UUID NOT NULL,
    nome       TEXT NOT NULL,
    especie    TEXT NOT NULL DEFAULT 'cao',  -- cao | gato | ave | roedor | reptil | outro
    ativo      BOOLEAN NOT NULL DEFAULT true,
    criado_em  TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS idx_racas_tenant ON racas(tenant_id);

-- Seeds básicos para cao e gato
INSERT INTO racas (tenant_id, nome, especie)
SELECT '4b19602f-0425-4101-914c-5b9aeaf5d4e7', nome, especie FROM (VALUES
  ('Labrador Retriever',   'cao'),
  ('Golden Retriever',     'cao'),
  ('Bulldog Frances',      'cao'),
  ('Poodle',               'cao'),
  ('Pastor Alemao',        'cao'),
  ('Yorkshire Terrier',    'cao'),
  ('Shih Tzu',             'cao'),
  ('Dachshund',            'cao'),
  ('Rottweiler',           'cao'),
  ('Boxer',                'cao'),
  ('Border Collie',        'cao'),
  ('Beagle',               'cao'),
  ('Lhasa Apso',           'cao'),
  ('Maltese',              'cao'),
  ('Pinscher',             'cao'),
  ('Bichon Frise',         'cao'),
  ('Pit Bull',             'cao'),
  ('American Bully',       'cao'),
  ('Husky Siberiano',      'cao'),
  ('Chow Chow',            'cao'),
  ('Sem Raca Definida',    'cao'),
  ('Persian',              'gato'),
  ('Siamese',              'gato'),
  ('Maine Coon',           'gato'),
  ('Ragdoll',              'gato'),
  ('Bengal',               'gato'),
  ('British Shorthair',    'gato'),
  ('Scottish Fold',        'gato'),
  ('Abyssinian',           'gato'),
  ('Sphynx',               'gato'),
  ('Sem Raca Definida',    'gato')
) AS t(nome, especie)
ON CONFLICT DO NOTHING;
