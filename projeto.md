# VetClinica — Contexto do Projeto

## LEIA ISSO PRIMEIRO
Este arquivo é o contexto completo do projeto. Sempre leia antes de iniciar qualquer tarefa.
O projeto é um SaaS veterinário multi-tenant chamado "Gestão Inteligente" / VetClinica.

---

## Stack
- **Backend:** .NET 8 + EF Core + PostgreSQL
- **Frontend:** React + Vite + Tailwind + Recharts
- **Hospedagem:** Railway (backend + banco) + Vercel (frontend via CLI manual)
- **Repositório:** https://github.com/rotelloguerra15/Vetclinicas.git

## URLs de Produção
- Frontend: https://vetclinicas.vercel.app
- Backend: https://vetclinicas-production.up.railway.app
- Trial: https://vetclinicas.vercel.app/trial
- Planos: https://vetclinicas.vercel.app/planos
- Admin: https://vetclinicas.vercel.app/admin/login

## Deploy Manual (OBRIGATÓRIO — webhook Vercel quebrado)
```powershell
cd C:\Projetos\vetclinica\frontend
npm run build
cd ..
New-Item -ItemType Directory -Force -Path ".vercel\output\static"
Copy-Item -Recurse -Force "frontend\dist\*" ".vercel\output\static\"
vercel deploy --prod --yes --prebuilt
```

## Banco de Dados (Railway)
- PostgreSQL no serviço **Postgres** (projeto `bountiful-generosity`)
- Conexão **externa** (psql/DBeaver local): host/porta/senha em Railway → Postgres → Variables (`RAILWAY_TCP_PROXY_DOMAIN`, `RAILWAY_TCP_PROXY_PORT`, `POSTGRES_PASSWORD`, user `postgres`, db `railway`)
- Conexão **interna** (app no Railway): `ConnectionStrings__Default` (host `postgres.railway.internal:5432`)
- ⚠️ Valores secretos vivem SÓ no Railway — nunca commitar senha/token/chave neste arquivo.
- Rodar migração local: `$env:PGPASSWORD="<senha>"; psql -h <proxy_domain> -p <proxy_port> -U postgres -d railway -f "caminho\arquivo.sql"`

## Arquitetura Multi-Schema
- Cada clínica tem seu próprio schema PostgreSQL (ex: `vet_barbarafonseca`)
- Schema `platform` contém `platform.tenants`, `platform.platform_admins` e `platform.configuracoes`
- JWT contém claim `schema` com nome do schema da clínica logada
- Novos tenants criados via `/api/trial` — copia estrutura do schema `vet_barbarafonseca` como template

## Tenants
| Email | Schema | Login |
|-------|--------|-------|
| barbaratatschveterinaria@gmail.com | vet_barbarafonseca | admin123 |
| rotello.guerra@ketra.com.br | superadmin | Ketra@2026 |

---

## PADRÕES CRÍTICOS (não ignorar)

1. **Multi-tenant:** coluna `tenant_id` em TODAS as tabelas
2. **Auth:** JWT via `TenantContext` — usar `_t.TenantId`, `_t.UserId`, `_t.Papel`
3. **DbContext:** usar `TenantDbContextFactory` em todos os controllers (NUNCA `AppDbContext`)
4. **ORM:** EF Core — NUNCA usar Dapper
5. **NUNCA emojis em strings C# interpoladas** — causa CS1010/CS1056
6. **FK violations:** sempre `SaveChanges()` no pai ANTES de adicionar filhos
7. **Tabela financeira:** chama `contas` (não `lancamentos`)
8. **Venda:** tem `TutorId` direto (não via Pet)
9. **DbSets:** `OrdensServico`, `Vendas`, `Tutores`, `Pets`, `Contas`, `PedidosCompra`, `MetasFaturamento`, `Pelagens`, `Cargos`, `ConciliacoesDiarias`, `RecebimentoAnexos`, `FechamentosContabeis`, `FechamentoDocumentos`, `PlanosSaude`, `PetPlanos`, `TutorPlanos`, `Racas`, `BotConfigs`, `BotConversas`, `BotLogs`
10. **Arquivos:** sempre entregar com nome final correto (sem sufixos `_cur`, `_fix`, `_v2`)
11. **Railway cache:** forçar rebuild mudando comentario no `.csproj` + commit
12. **using:** sempre incluir `using VetClinica.API.Models;` em controllers que usam entities
13. **PowerShell:** NÃO usar `&&` — separar em linhas individuais
14. **SQL com emojis:** NÃO colocar emojis em .sql — encoding WIN1252 quebra no psql Windows
15. **Sempre ler o arquivo atual antes de modificar** — nunca editar às cegas
16. **BotConfigController foi removido** — endpoints de bot estão em `ParametrosController` com rotas `~/api/bot-config/*`
17. **Add-Content no Program.cs PROIBIDO** — corrompe o arquivo com chars nulos. Para cache bust: editar comentario no `.csproj`
18. **ProvisionamentoService:** usa `LIKE vet_barbarafonseca.tabela INCLUDING DEFAULTS INCLUDING CONSTRAINTS` para criar tabelas. Usuário criado em `dbUser` separado antes dos seeds em background.
19. **prontuario_itens.titulo:** tipo TEXT (não VARCHAR 120) — IA gera textos longos
20. **BCrypt hash:** usar $2a$ com rounds 11 para compatibilidade com BCrypt.Net
21. **Entidades no PlatformDbContext:** `Tenant`, `PlatformAdmin`, `Configuracao` (platform.configuracoes — chave/valor para SMTP e configs globais)
22. **SMTP:** Railway bloqueia porta 587. Usar porta 25. Config salva em `platform.configuracoes`, não em variáveis de ambiente.
23. **SQL via PowerShell:** NUNCA colar SQL direto no PowerShell. Sempre usar `psql ... -c "..."` com aspas simples no valor, ou `-f caminho\arquivo.sql`
24. **Hash BCrypt via PowerShell:** o `$` é interpretado como variável. Sempre salvar o hash em variável PS primeiro: `$hash = '$2a$11$...'` depois usar `$hash` na query.

---

## BACKLOG ATUALIZADO — 18/06/2026

### 🔴 Alta Prioridade

1. **✅ RESOLVIDO (18/06/2026) — Email não disparava**
   - Causa raiz: Railway bloqueia TODAS as portas SMTP de saída (25/465/587/2525) nos planos Free/Trial/Hobby. SMTP só no plano Pro. Porta 25 NÃO resolve (também bloqueada).
   - Solução: novo `EmailService` central (`Services/EmailService.cs`) com 3 providers — `graph` (Microsoft 365 via HTTP/443), `resend` (HTTP) e `smtp` (fallback p/ Pro/local). Provider ativo escolhido em `platform.configuracoes.email_provider`.
   - **Em produção usamos `graph`** (Office 365 do Ketra, HTTPS, custo R$0, funciona no Hobby). Ver seção "Administração de Sistema — E-mail".
   - Boas-vindas do trial, reset de senha e notificações passam pelo `EmailService`.

2. **WhatsApp — registrar número** `+55 31 8624-0841`
   - Rodar após rate limit Meta: `Invoke-RestMethod -Uri "https://graph.facebook.com/v18.0/1169474326252832/register" -Method POST -Headers @{"Authorization"="Bearer EAAN7j..."} -Body '{"messaging_product":"whatsapp","pin":"000000"}' -ContentType "application/json"`
   - Token salvo em `vet_barbarafonseca.bot_config.meta_token`
   - Webhook verificado e assinado

3. **Página Trial — validar fluxo completo**
   - Tela aparece, credenciais geradas, trial_expira_em gravado
   - Email de boas-vindas: ✅ chega via Microsoft Graph (ver item 1 e seção Administração de Sistema)
   - Login após cadastro: funciona
   - Parâmetros da empresa com campos para NF (CNPJ, Razão Social, endereço) — seeds ok

### 🟡 Média Prioridade

4. **✅ RESOLVIDO (18/06/2026) — PDV/Caixa**
   - Abertura com saldo do dia anterior, modal retirada/depósito, fechamento→Financeiro, reabertura com auditoria: já existiam.
   - Bugs corrigidos nesta data:
     - **Saldo de dinheiro duplicado:** serviço pago via PDV era contado 2× (como venda + como OS). OS removida da matemática do saldo (vendas já capturam o serviço, pois OS é cobrada via PDV com `OsId`).
     - **Duplicação no Financeiro:** reabrir + fechar de novo re-lançava receitas/movimentações bancárias. `Reabrir` agora estorna os lançamentos do fechamento anterior.
     - **Reabertura sem trava:** restrita a `owner`/`admin` (403 caso contrário).
     - **Exibição (PDV.jsx):** separado em "Movimento do dia (bruto)" vs "Caixa físico (dinheiro)"; serviço aparece como subitem das vendas.
   - ⚠️ Conhecido (pré-existente, fora de escopo): fechamento delimita o dia por `DateTime.Today` em UTC. Railway roda UTC, BH é UTC−3 → vendas ~21h–00h caem no dia seguinte do caixa. Tratar num passe dedicado de fuso (afeta o app inteiro, não só o caixa).

5. **Testar fluxo completo** — criar tutor → pet → agendamento → OS → venda PDV → financeiro

6. **WhatsApp — templates Meta**
   - Criar `pet_pronto` e `promocao_marketing` no Gerenciador do WhatsApp

7. **Pagamento — assinatura SaaS (cobrar as clínicas) — Asaas**
   - ESTE item é a cobrança da MENSALIDADE do VetClinica das clínicas (não confundir com o PDV abaixo)
   - Página /planos criada (sem pagamento ainda)
   - Vincular plano ao tenant após pagamento + cobrança recorrente por tenant

7b. **🆕 PDV — Pagamentos integrados (receber do tutor) — EM DESENVOLVIMENTO**
   - Fluxo do BALCÃO: a clínica recebe do cliente (Pix/cartão), não a assinatura. Modelo de mercado (pagamento integrado / TEF), igual Linx.
   - **DECISÃO (18/06/2026): Mercado Pago primeiro** (Pix QR + maquininha Point via Orders API). Arquitetura multi-provider (`IPaymentProvider`) p/ plugar Cielo/Stone/outros depois sem reescrever.
   - Detalhes técnicos e config: ver seção "Administração de Sistema — Pagamentos PDV".
   - **Fase 1 (✅ fundação pronta):** tabela `cobrancas_pix`, `IPaymentProvider`, `PixController` (criar/status/webhook), baixa automática (conta recebida + lançamento + movimentação bancária). Implementado com Asaas como primeiro provider de teste.
   - **Fase 1b (próxima):** `MercadoPagoProvider` (Pix QR + Point), wiring no `VendasController` (venda Pix nasce pendente), tela do PDV com QR + polling, botão "Cartão" que dispara na maquininha.
   - **Fase 2:** tela do cliente touch (visão de frente) + relatórios/dashboards por meio de pagamento.
   - **Escala:** clínica conecta a própria conta MP via OAuth (1 clique) → token guardado por tenant → dinheiro cai na conta dela, Ketra não lida com KYC. Split opcional = receita por transação.

### 🟢 Baixa Prioridade

8. Módulo Gestão de Contratos (compra e venda) — não desenvolvido
9. Subdomínio por clínica
10. Storage imagens Cloudflare R2
11. Reconectar webhook Vercel → GitHub
12. Certificado A3 nuvem (Safeweb Cloud API)

---

## Administração de Sistema — E-mail (Microsoft Graph / Office 365)

> Esta seção entra na "Administração de Sistema" da documentação central.

### Como funciona
O envio de email NÃO usa SMTP (Railway bloqueia nos planos Free/Trial/Hobby). Usamos a **Microsoft Graph API** sobre HTTPS (porta 443), enviando pela caixa do Office 365 do Ketra. Tudo passa pelo `EmailService` (`VetClinica.API/Services/EmailService.cs`), que escolhe o provider via `platform.configuracoes.email_provider` (valor em produção: `graph`).

Fluxo do provider `graph`: OAuth2 client credentials (app-only) → token em `login.microsoftonline.com/{tenant}/oauth2/v2.0/token` → `POST graph.microsoft.com/v1.0/users/{remetente}/sendMail`.

### App no Azure (Microsoft Entra ID)
- **App:** VetClinica Email (tenant: KETRA Soluções Inteligentes)
- **Directory (tenant) ID:** `95a027b0-1152-4b45-87b4-52edfcef7266`
- **Application (client) ID:** `27a6bac0-1516-4a54-a37c-2cf8f018e99b`
- **Object ID (não usado):** `120f1a33-158d-4c7e-99ae-031d165e2564`
- **Permissão:** Microsoft Graph → **Mail.Send (Aplicativo)** com consentimento de admin concedido
- **Remetente:** `workflow@ketra.com.br`
- **Client Secret:** guardado em `platform.configuracoes.graph_client_secret` (NUNCA versionar o valor)

### Chaves em `platform.configuracoes`
| Chave | Conteúdo |
|-------|----------|
| `email_provider` | `graph` (ou `resend` / `smtp`) |
| `graph_tenant_id` | Directory (tenant) ID |
| `graph_client_id` | Application (client) ID |
| `graph_client_secret` | valor do secret do Azure |
| `graph_remetente` | `workflow@ketra.com.br` |

Configuração feita pelo painel `/admin` → Configuracoes → provider "Microsoft 365" → Salvar → Testar envio.

### ⚠️ Renovação do Client Secret (AÇÃO FUTURA)
- **Criado em:** 18/06/2026
- **Vence em:** ~18/12/2026 (validade de 6 meses)
- Quando expirar, o email PARA de sair (erro 401 / AADSTS7000222).
- **Como renovar:** Azure → App registration "VetClinica Email" → Certificates & secrets → New client secret → copiar o **Value** na hora → colar em `/admin` → Configuracoes → Client Secret → Salvar.
- Melhoria futura: trocar secret por **certificado** (validade maior, mais seguro).

### Diagnóstico
O `EmailService` retorna o motivo exato da Graph em caso de falha (visível no "Testar envio" e nos logs do Railway). Erros comuns: `AADSTS7000215` = secret errado (colou o ID em vez do Value); `403 Access denied` = falta permissão Mail.Send (Aplicativo) + consentimento.

---

## Administração de Sistema — Pagamentos PDV (Mercado Pago)

> Como a clínica recebe do tutor no balcão (Pix + cartão). Modelo de pagamento integrado, igual aos grandes PDVs (Linx).

### Princípio
Backend na nuvem NÃO fala direto com pinpad USB. Por isso usamos **maquininha smart** (Mercado Pago Point), que tem internet própria e fica pareada à nuvem do MP: o backend manda a cobrança → nuvem MP → empurra pro terminal. Sem agente local.

### Modelos de cobrança (Mercado Pago)
- **Pix QR dinâmico** — gera código único por transação, sem maquininha. Cliente escaneia. Confirma por webhook.
- **API Point (cartão)** — `POST /v1/orders` com `device_id` + valor → valor aparece na Point do balcão → cliente passa/aproxima cartão → resultado por webhook.

### Arquitetura no código
- Abstração `IPaymentProvider` (Services/Payments). Providers plugáveis: `AsaasPaymentProvider` (teste inicial Pix), `MercadoPagoProvider` (Pix + Point — a implementar). Adicionar Cielo/Stone = nova classe, sem reescrever o resto.
- Tabela `cobrancas_pix` (por tenant), `PixController` (criar / status / webhook), baixa automática: conta recebida + lançamento + `movimentacoes_bancarias`.
- Webhook resolve o tenant pela `externalReference` = `{tenantId}|{cobrancaId}` (não tem JWT).

### Config por clínica (parametros_sistema)
- Modelo de escala: clínica conecta a **própria conta MP via OAuth** (1 clique). Token guardado por tenant. Dinheiro cai na conta dela; Ketra não lida com KYC.
- Campos no painel: token MP (via OAuth) + seleção do terminal (device_id, listado via API). Maquininha Point compatível necessária (Point Pro 2+).
- Split opcional (walletId) = Ketra retém % por transação → receita extra além da mensalidade.

### Divisão de responsabilidades
- **Mercado Pago = PDV** (dinheiro da clínica / cliente final).
- **Asaas = mensalidade SaaS** (dinheiro da Ketra, item #7) — boleto/recorrência.

### Status
- ✅ Fundação Pix (tabela, provider abstrato, controller, webhook, baixa) — deployada 18/06/2026.
- ⏳ `MercadoPagoProvider`, OAuth de conexão, wiring no PDV, telas (QR + cliente touch), relatórios.

---

## WhatsApp Meta Cloud API

- **App:** VetClinica (ID: 980278631564376) — publicado
- **WABA:** Gestão Inteligente para Clinica e Pet Shop (ID: 3463684367146236)
- **Número:** +55 31 8624-0841 (Phone Number ID: 1169474326252832) — **Pendente registro**
- **Token:** salvo em `vet_barbarafonseca.bot_config.meta_token`
- **Webhook URL:** https://vetclinicas-production.up.railway.app/api/bot/webhook
- **Verify Token:** vetclinica_webhook_2026
- **Campo assinado:** messages

---

## Variáveis de Ambiente (Railway) — serviço Vetclinicas

> Apenas NOMES. Os valores vivem no Railway (nunca commitar valores aqui).

| Variável | Para que serve |
|----------|-------|
| `ConnectionStrings__Default` | conexão interna ao Postgres (`postgres.railway.internal:5432`) |
| `App__FrontendUrl` | URL do front (https://vetclinicas.vercel.app) |
| `Jwt__Key` | chave de assinatura JWT (min 32 chars) — **segredo** |
| `Jwt__Issuer` | VetClinica |
| `Jwt__Audience` | VetClinicaApp |
| `Jwt__ExpireHours` | validade do token (12) |
| `Meta__PhoneNumberId` | WhatsApp Cloud API — id do número |
| `Meta__Token` | token do Meta — **segredo** |
| `Meta__WabaId` | id da conta WhatsApp Business |
| `Meta__WebhookVerifyToken` | token de verificação do webhook Meta |
| `CERT_ENCRYPTION_KEY` | chave AES para certificado .pfx — **segredo** |
| `QuestPDF__LicenseType` | Community |
| `Smtp__*` | DESATIVADAS — email via Microsoft Graph (ver Administração de Sistema). Pode remover. |

**Infra Railway (não-secreto, p/ referência):** projeto `bountiful-generosity` · domínio público `vetclinicas-production.up.railway.app` · serviço backend `Vetclinicas` · serviço banco `Postgres` (volume `postgres-volume`).

🔐 **Pendência de segurança (18/06/2026):** rotacionar senha do Postgres, `Jwt__Key`, `Meta__Token` e senha SMTP — foram expostos em troca de mensagens durante o desenvolvimento.

---

## O que foi feito — 17-18/06/2026

- Arquitetura multi-schema PostgreSQL implementada e funcionando
- Dra. Barbara migrada para schema `vet_barbarafonseca`
- JWT com claim `schema` funcionando
- IA configurada e gerando diagnóstico + medicamentos
- Webhook Meta verificado e assinado
- `ProvisionamentoService` usando LIKE template para criar schemas
- Página `/trial` pública implementada com countdown de 14 dias
- Página `/planos` criada com 3 planos (sem pagamento)
- Página `/esqueci-senha` + `/redefinir-senha` com token 30min
- Email de boas-vindas com link de redefinição de senha (72h)
- `trial_expira_em` gravado no banco ao criar tenant
- `token_reset_senha` + `token_reset_expira` em `platform.tenants`
- `platform.configuracoes` criada para SMTP e configs globais
- Painel admin `/admin` com abas Clinicas + Configuracoes
- Painel admin mostra trial_expira_em e dias restantes por clínica
- Botão "Testar envio" SMTP no painel admin com campo destino livre
- Login admin corrigido (localStorage.clear antes de autenticar)
- Hash BCrypt do admin resetado via psql
- IaController com endpoint `diagnostico-receituario` retornando JSON estruturado
- PetDetalhe.jsx populando receituário automaticamente com medicamentos da IA
- ReceituarioController migrado para TenantDbContextFactory
- WebhookController migrado para PlatformDbContext
- **EmailService central criado** (`Services/EmailService.cs`) com 3 providers: graph / resend / smtp
- **Email resolvido via Microsoft Graph** (Office 365, HTTPS) — contorna bloqueio SMTP do Railway Hobby, custo R$0
- App "VetClinica Email" registrado no Entra ID com permissão Mail.Send (Aplicativo) + consentimento
- TrialController e AuthController migrados para o EmailService (envio de boas-vindas agora síncrono)
- Painel admin: provider de email selecionável (Microsoft 365 / Resend / SMTP) com campos próprios
- Client Secret do Azure salvo em `platform.configuracoes.graph_client_secret` — **vence ~18/12/2026**
- **Caixa: bugs de correção resolvidos** — saldo duplicado de serviços removido, estorno de lançamentos na reabertura, reabertura restrita a owner/admin, exibição separando movimento bruto x caixa físico
