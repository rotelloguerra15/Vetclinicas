-- Migration 036 -- Anexos de Nota Fiscal no Recebimento de Mercadorias

CREATE TABLE IF NOT EXISTS recebimento_anexos (
    id                UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id         UUID NOT NULL REFERENCES tenants(id),
    recebimento_id    UUID NOT NULL REFERENCES recebimentos_mercadoria(id) ON DELETE CASCADE,
    nome              VARCHAR(255) NOT NULL,
    tipo_arquivo      VARCHAR(100) NOT NULL,  -- application/pdf, image/jpeg, etc
    tamanho_bytes     INTEGER NOT NULL,
    dados_base64      TEXT NOT NULL,           -- arquivo em base64 (max 5MB)
    descricao         VARCHAR(255),            -- ex: NF-e 12345, Boleto, DANFE
    criado_por        UUID REFERENCES users(id),
    criado_em         TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_recebimento_anexos_recebimento
    ON recebimento_anexos(recebimento_id);
