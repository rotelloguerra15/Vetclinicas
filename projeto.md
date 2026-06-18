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
- Connection string: `postgresql://postgres:eOpnNTdorWCkkphKLRlNogUWOtiwGibS@gondola.proxy.rlwy.net:16524/railway`

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

1. **SMTP — email não dispara**
   - Railway bloqueia porta 587 de saída (timeout confirmado)
   - Porta 25 pode funcionar — testar via painel admin → Configuracoes → mudar porta para 25
   - Config de SMTP salva em `platform.configuracoes` (não mais em variáveis de ambiente)
   - Painel admin tem botão "Testar envio" para diagnosticar
   - Se porta 25 também falhar: avaliar Resend.com (API REST, sem SMTP, funciona em qualquer cloud)

2. **WhatsApp — registrar número** `+55 31 8624-0841`
   - Rodar após rate limit Meta: `Invoke-RestMethod -Uri "https://graph.facebook.com/v18.0/1169474326252832/register" -Method POST -Headers @{"Authorization"="Bearer EAAN7j..."} -Body '{"messaging_product":"whatsapp","pin":"000000"}' -ContentType "application/json"`
   - Token salvo em `vet_barbarafonseca.bot_config.meta_token`
   - Webhook verificado e assinado

3. **Página Trial — validar fluxo completo**
   - Tela aparece, credenciais geradas, trial_expira_em gravado
   - Email de boas-vindas NÃO chega (bloqueio SMTP Railway — ver item 1)
   - Login após cadastro: funciona
   - Parâmetros da empresa com campos para NF (CNPJ, Razão Social, endereço) — seeds ok

### 🟡 Média Prioridade

4. **PDV/Caixa — pendências:**
   - Fechamento de caixa não alimenta o Financeiro
   - Caixa fechado não reabre sem aprovação do owner/admin
   - Saldo = dinheiro físico, abre com saldo do dia anterior
   - Aba de Retirada e Depósito
   - Matemática: saldo anterior + entradas dinheiro − retiradas = saldo final
   - Saldo final vira saldo inicial do próximo dia automaticamente

5. **Testar fluxo completo** — criar tutor → pet → agendamento → OS → venda PDV → financeiro

6. **WhatsApp — templates Meta**
   - Criar `pet_pronto` e `promocao_marketing` no Gerenciador do WhatsApp

7. **Pagamento — integração Asaas**
   - Página /planos criada (sem pagamento ainda)
   - Vincular plano ao tenant após pagamento
   - Cobrança automática por tenant

### 🟢 Baixa Prioridade

8. Módulo Gestão de Contratos (compra e venda) — não desenvolvido
9. Subdomínio por clínica
10. Storage imagens Cloudflare R2
11. Reconectar webhook Vercel → GitHub
12. Certificado A3 nuvem (Safeweb Cloud API)

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

## Variáveis de Ambiente (Railway)

| Variável | Valor |
|----------|-------|
| `ConnectionStrings__Default` | string PostgreSQL |
| `Jwt__Key` | chave JWT min 32 chars |
| `Jwt__ExpireHours` | 720 (30 dias) |
| `App__FrontendUrl` | https://vetclinicas.vercel.app |
| `CERT_ENCRYPTION_KEY` | chave AES para .pfx |
| `QuestPDF__LicenseType` | Community |
| `Smtp__Host` | smtp.office365.com (legado — config agora em platform.configuracoes) |
| `Smtp__Porta` | 587 (legado) |
| `Smtp__Usuario` | workflow@ketra.com.br (legado) |
| `Smtp__Ssl` | true (legado) |
| `Smtp__Remetente` | workflow@ketra.com.br (legado) |

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
