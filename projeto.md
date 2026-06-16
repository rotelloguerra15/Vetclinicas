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

## Rodar Migration
```powershell
$DB = "postgresql://postgres:eOpnNTdorWCkkphKLRlNogUWOtiwGibS@gondola.proxy.rlwy.net:16524/railway"
psql $DB -f "C:\Projetos\vetclinica\database\ARQUIVO.sql"
```

## Tenant Principal
- ID: `4b19602f-0425-4101-914c-5b9aeaf5d4e7`
- Nome: Pet Shop e Consultório Dra. Barbara Fonseca
- Login: barbaratatschveterinaria@gmail.com / admin123
- Super-admin: rotello.guerra@ketra.com.br / Ketra@2026

---

## PADRÕES CRÍTICOS (não ignorar)

1. **Multi-tenant:** coluna `tenant_id` em TODAS as tabelas
2. **Auth:** JWT via `TenantContext` — usar `_t.TenantId`, `_t.UserId`, `_t.Papel`
3. **ORM:** EF Core — NUNCA usar Dapper
4. **NUNCA emojis em strings C# interpoladas** — causa CS1010/CS1056
5. **FK violations:** sempre `SaveChanges()` no pai ANTES de adicionar filhos
6. **Tabela financeira:** chama `contas` (não `lancamentos`)
7. **Venda:** tem `TutorId` direto (não via Pet)
8. **PedidoItem FK:** configurar `HasForeignKey` no `OnModelCreating`
9. **DbSets:** `OrdensServico`, `Vendas`, `Tutores`, `Pets`, `Contas`, `PedidosCompra`, `MetasFaturamento`, `Pelagens`, `Cargos`, `ConciliacoesDiarias`, `RecebimentoAnexos`, `FechamentosContabeis`, `FechamentoDocumentos`, `PlanosSaude`, `PetPlanos`, `TutorPlanos`, `Racas`, `BotConfigs`, `BotConversas`, `BotLogs`
10. **Arquivos:** sempre entregar com nome final correto (sem sufixos `_cur`, `_fix`, `_v2`)
11. **Railway cache:** forçar rebuild mudando comentario no `.csproj` + commit
12. **using:** sempre incluir `using VetClinica.API.Models;` em controllers que usam entities
13. **PowerShell:** NÃO usar `&&` — separar em linhas individuais
14. **SQL com emojis:** NÃO colocar emojis em .sql — encoding WIN1252 quebra no psql Windows
15. **iText7 assinatura:** NÃO tentar integrar iText7+BouncyCastle sem testar localmente
16. **Sempre ler o arquivo atual antes de modificar** — nunca editar às cegas
17. **BotConfigController foi removido** — endpoints de bot estão em `ParametrosController` com rotas `~/api/bot-config/*`
18. **BotWhatsAppService** foi renomeado internamente para `BotWAService` (classe) mas o arquivo ainda chama `BotWhatsAppService.cs`
19. **bot_config seed:** INSERT manual necessário — migration não cria o registro automaticamente
20. **Add-Content no Program.cs PROIBIDO** — corrompe o arquivo com chars nulos. Para cache bust: editar comentario no `.csproj`

---

## Migrations Aplicadas (40 total)

| # | Arquivo | Descrição |
|---|---------|-----------| 
| 01-37 | (ver versão anterior) | Schema base até módulo contábil |
| 38 | 038_m5_planos_saude.sql | Planos de saúde, pet_planos, tutor_planos |
| 39 | 039_racas.sql | Tabela racas com seed cão/gato |
| 40 | 040_meta_whatsapp.sql | meta_phone_number_id e meta_waba_id em bot_config |

---

## Módulos Implementados

### Clínica
- Auth, multi-tenant, JWT (expiração via `Jwt__ExpireHours`, padrão 12h)
- **Tutores** — endereço, código, aniversário, painel de pets vinculados
- **Pets** — microchip, plano saúde (M5), raça por select filtrado por espécie, idade estimada, pelagem, castrado
- Prontuário, Vacinas
- **Agenda** — grade horários, slots vagos, KPIs do dia, iniciar atendimento
- OS Kanban, Atendimentos
- **Receituário PDF** — QR Code + código único, validação pública
- **IA Diagnóstico** — assistente Claude API por tenant

### Vendas / PDV
- PDV/Caixa — tela de abertura com saldo anterior, tela de fechado com bloqueio
- Reabrir caixa restrito a `owner` e `admin`
- Retirada/Depósito via modal
- Fechamento gera `Conta` por forma de pagamento + `MovimentacaoBancaria`
- Serviços, Estoque, Dashboard de Vendas

### M4 Compras
- Fornecedores, Condições de pagamento, Pedidos de compra
- Recebimento de Mercadoria — anexos NF

### M5 Planos de Saúde
- Cadastro de planos/operadoras com desconto padrão
- Vínculo por pet (1 ativo por vez) e por tutor
- Endpoint `GET /api/planos-saude/desconto?petId=` para PDV/OS aplicar desconto automático
- Vínculo integrado no cadastro de pets (select dos planos cadastrados)

### Financeiro (M2)
- Contas a pagar/receber, baixa, estorno
- Movimentação bancária + Conciliação Diária
- Categorias financeiras

### RH (M3)
- Cargos, Funcionários, Comissões, Fechamento mensal, Relatórios RH

### Módulo Contábil
- Fechamento mensal, DRE CSV, envio por email via SMTP

### Cadastros (menu unificado)
- Usuários (com tabela de permissões por papel — owner/admin podem reabrir caixa)
- Serviços
- Raças (por espécie, seed cão/gato)
- Pelagens
- Vias de administração
- Planos de Saúde

### WhatsApp Bot (Meta Cloud API)
- **Migrado de Z-API para Meta Cloud API**
- Webhook: `GET/POST /api/bot/webhook`
- Bot config endpoints em `ParametrosController` (rotas `~/api/bot-config/*`)
- Máquina de estados: inicio → aguardando_servico → aguardando_data → aguardando_horario → concluido
- Log de conversas com resumo e filtros
- App Meta publicado, webhook verificado e assinado (campo `messages`)
- **STATUS número:** `+55 31 8624-0841` adicionado no Meta, Phone Number ID `1169474326252832`, WABA ID `3463684367146236`, pendente de verificação (limite de tentativas atingido — aguardar 1h)
- Templates criados: `lembrete_de_agendamento` (em análise), `hello_world` (ativo)
- Templates pendentes de criação: `pet_pronto`, `promocao_marketing`

### Dashboards
- Clínica, Financeiro BI, RH, Vendas

### Gestão à Vista (TV)
- Kanban OS em tempo real, RSS news

---

## BACKLOG

### Pendente Imediato (próxima sessão)
- [ ] **WhatsApp — verificar número** `+55 31 8624-0841` após desbloqueio Meta (aguardar 1h desde última tentativa ~21h de 15/06/2026). Deletar WhatsApp consumer do número → registrar via API
- [ ] **Templates Meta** — criar `pet_pronto` e `promocao_marketing` no Gerenciador do WhatsApp
- [ ] **Foto do pet pronto** — botão "Avisar tutor" na OS status=entregue, envia foto via WhatsApp

### Bugs / Validações
- [ ] **Validar assinatura digital PDF** — aguarda .pfx real da Dra. Barbara
- [ ] **Gestão à Vista** — confirmar loops em produção

### Funcionalidades
- [ ] **Campanhas de marketing WhatsApp** — disparo segmentado via template `promocao_marketing`
- [ ] **Agenda online pública** — cliente agenda sem login via link `/agendar/[slug]`
- [ ] **M7 Metas** — UI de metas (backend `metas_faturamento` já existe)
- [ ] **Tempo de produtividade** — homem-hora por OS
- [ ] **Integração Asaas** — PIX/boleto + suspensão tenant
- [ ] **Self-service tenant** — trial 7 dias
- [ ] **Storage imagens** — Cloudflare R2
- [ ] **Consulta NF-e SEFAZ** — via certificado A1

### Técnico
- [ ] **Reconectar webhook Vercel → GitHub**
- [ ] **Certificado A3 nuvem** — Safeweb Cloud API

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
| `Meta__PhoneNumberId` | 1169474326252832 |
| `Meta__Token` | token permanente VetClinica Bot |
| `Meta__WabaId` | 3463684367146236 |
| `Meta__WebhookVerifyToken` | vetclinica_webhook_2026 |

---

## Meta Cloud API — Configuração Atual

- **App:** VetClinica (ID: 980278631564376) — publicado
- **WABA:** Gestão Inteligente para Clinica e Pet Shop (ID: 3463684367146236)
- **Número:** +55 31 8624-0841 (Phone Number ID: 1169474326252832) — status: Pendente
- **Número de teste:** +1 555 202 3293 (Phone Number ID: 1184328571429350) — ativo
- **Webhook URL:** https://vetclinicas-production.up.railway.app/api/bot/webhook
- **Verify Token:** vetclinica_webhook_2026
- **Campo assinado:** messages

## Estrutura de Arquivos Relevantes

```
C:\Projetos\vetclinica\
  VetClinica.API\
    Controllers\
      WebhookController.cs         (Meta Cloud API webhook — AllowAnonymous)
      ParametrosController.cs      (inclui endpoints ~/api/bot-config/*)
      PlanosSaudeController.cs     (M5)
      RacasController.cs           (cadastro de racas)
    Models\
      Entities.cs                  (inclui PlanoSaude, PetPlano, TutorPlano, Raca, BotWAService→BotConfig)
    Services\
      WhatsAppService.cs           (Meta Cloud API — usa Dictionary para JSON snake_case)
      BotWhatsAppService.cs        (classe BotWAService internamente)
      NotificacaoDispatcher.cs     (usa WhatsAppService, não ZApiService)
  frontend\src\
    pages\
      PDV.jsx                      (tela fechado + reabrir owner/admin + saldo anterior)
      Usuarios.jsx                 (tabela permissoes por papel)
      Pets.jsx                     (select de racas por especie + vinculo M5)
      Cadastros\Racas.jsx          (novo)
      PlanosSaude.jsx              (M5 — 3 abas)
      Parametros\Bot.jsx           (4 abas: Config, Meta API, Mensagens, Log)
    components\
      Layout.jsx                   (Cadastros unificado: Usuarios, Servicos, Racas, Pelagens, Vias, Planos)
```
