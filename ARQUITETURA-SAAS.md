# 🏢 VetClinica como SaaS — Arquitetura de escala

## A pirâmide

```
┌─────────────────────────────────────────────┐
│  PLATAFORMA (Ketra)                           │
│  Super-admin: rotello.guerra@ketra.com.br     │  ← você, acima de tudo
│  Painel: /admin                               │
└───────────────┬───────────────────────────────┘
                │ cria e gerencia
   ┌────────────┼────────────┬────────────┐
   ▼            ▼            ▼            ▼
┌────────┐  ┌────────┐  ┌────────┐  ┌────────┐
│Fonseca │  │PetLove │  │ Clínica│  │  ...   │   ← cada clínica = 1 tenant
│ (pro)  │  │(basico)│  │   N    │  │ (vender│      dados 100% isolados
└────────┘  └────────┘  └────────┘  │  mais) │
                                     └────────┘
```

## Camadas

| Camada | Quem | Acesso | Login |
|--------|------|--------|-------|
| **Plataforma** | Rotello (Ketra) | Todas as clínicas, métricas, criar/suspender | `/admin/login` |
| **Clínica (tenant)** | Dono/equipe da clínica | Só os próprios dados | `/login` |

## O que já está pronto (nesta entrega)

- **Super-admin** (`platform_admins`) — separado dos usuários de clínica
- **Login de plataforma** (`/api/auth/login-plataforma`) com token próprio
- **Painel `/admin`**: lista clínicas, métricas, criar/suspender/reativar, trocar plano
- **Provisionamento automático**: criar clínica gera tenant + dono + categorias +
  serviços + mensagens padrão e devolve a senha temporária do responsável
- **Isolamento**: cada clínica só vê seus dados (filtro por `tenant_id` em tudo) — testado
- **White-label**: nome/tagline/logo/cor vêm do tenant (não mais hardcoded)
- **Suspensão**: clínica suspensa não consegue logar (controle de inadimplência)
- **Planos**: trial / basico / pro / enterprise (troca pelo painel)

## Decisões que precisamos tomar para escalar de verdade

### 1. Cobrança (o mais importante)
Hoje os planos existem mas não cobram. Opções:
- **Stripe** ou **Asaas** (BR, suporta PIX/boleto) com webhook que suspende
  automaticamente quem não paga. Recomendo Asaas pelo mercado brasileiro.

### 2. Como a clínica entra
- **Hoje:** você cria manualmente pelo painel (bom para começar e vender no 1:1).
- **Depois:** página pública de "teste grátis 7 dias" que cria o tenant sozinho (self-service).

### 3. Endereço de cada clínica
- **Hoje:** todas no mesmo site, separadas pelo login.
- **Escala:** subdomínio por clínica (`fonseca.seusistema.com.br`) — aí a logo
  aparece já na tela de login. Exige config de DNS wildcard.

### 4. Onde a logo/foto fica hospedada
- Falta o storage real (Cloudflare R2 / Supabase). Hoje o `logo_url` aceita uma URL.

## Ordem sugerida de ataque

1. **Storage de imagens** (logo + fotos dos pets) — destrava o white-label visual
2. **Integração de cobrança** (Asaas) + suspensão automática
3. **Self-service de cadastro** (teste grátis) — aí a máquina de vendas gira sozinha
4. **Subdomínio por clínica** — quando o volume justificar
