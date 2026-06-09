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
- Webhook bot WhatsApp: https://vetclinicas-production.up.railway.app/api/webhook/whatsapp/{tenantId}

## Deploy Manual (OBRIGATÓRIO — webhook Vercel quebrado, não faz auto-deploy)
```powershell
cd C:\Projetos\vetclinica\frontend
echo "VITE_API_URL=https://vetclinicas-production.up.railway.app" > .env.production
npm run build
cd C:\Projetos\vetclinica
New-Item -ItemType Directory -Force -Path ".vercel\output\static"
Copy-Item -Recurse -Force "frontend\dist\*" ".vercel\output\static\"
vercel deploy --prod --yes --prebuilt
```

## Banco de Dados (Railway)
- Host público: gondola.proxy.rlwy.net:16524
- Host interno: postgres.railway.internal:5432
- Database: railway / User: postgres
- Password: eOpnNTdorWCkkphKLRlNogUWOtiwGibS
- Connection string: `postgresql://postgres:eOpnNTdorWCkkphKLRlNogUWOtiwGibS@gondola.proxy.rlwy.net:16524/railway`

## Rodar Migration no Railway
```powershell
$DB = "postgresql://postgres:eOpnNTdorWCkkphKLRlNogUWOtiwGibS@gondola.proxy.rlwy.net:16524/railway"
psql $DB -f "C:\Projetos\vetclinica\database\ARQUIVO.sql"
```

## Tenant Principal (Produção)
- ID: `4b19602f-0425-4101-914c-5b9aeaf5d4e7`
- Nome: Pet Shop e Consultório Dra. Barbara Fonseca
- Login: barbaratatschveterinaria@gmail.com / admin123

---

## PADRÕES CRÍTICOS (não ignorar)

1. **Multi-tenant:** coluna `tenant_id` em TODAS as tabelas
2. **Auth:** JWT via `TenantContext` — usar `_t.TenantId`, `_t.UserId`, `_t.Papel`
3. **ORM:** EF Core — NUNCA usar Dapper
4. **NUNCA emojis em strings C# interpoladas** — causa CS1010/CS1056
5. **FK violations:** sempre `SaveChanges()` no pai ANTES de adicionar filhos
6. **Tabela financeira:** chama `contas` (não `lancamentos` — legado do schema antigo)
7. **Venda:** tem `TutorId` direto (não `PetId`)
8. **PedidoItem FK:** configurar `HasForeignKey` no `OnModelCreating`
9. **DbSets corretos:** `OrdensServico`, `Vendas`, `Tutores`, `Pets`, `Contas`, `PedidosCompra`, `MetasFaturamento`, etc.
10. **Arquivos:** sempre entregar com nome final correto (sem `_cur`, `_fix`, `_v2` etc.)
11. **Railway cache:** quando `COPY VetClinica.API/` aparece como `cached`, forçar com `Add-Content` em qualquer arquivo + commit
12. **using:** sempre incluir `using VetClinica.API.Models;` em controllers que usam entities

---

## Migrations Aplicadas (27 total)
| # | Arquivo | Descrição |
|---|---------|-----------|
| 01 | 01-schema.sql | Schema base (tabela `lancamentos` — legado) |
| 02 | 02-estoque-mensagens.sql | Estoque, mensagens, vendas, triggers |
| 03 | 03-aniversario-tutor.sql | Campos aniversário tutor |
| 04 | 04-branding-fonseca.sql | Branding inicial |
| 05 | 05-plataforma-saas.sql | Plataforma SaaS multi-tenant |
| 06 | 06-promocoes.sql | Promoções/campanhas |
| 07 | 07-fix-enums-to-varchar.sql | Fix enums |
| 08 | 08-prontuario-campos.sql | Prontuário |
| 09 | 09-caixa.sql | Caixa PDV |
| 10 | 010_funcionarios.sql | Funcionários RH |
| 11 | 011_parametros_sistema.sql | Parâmetros sistema |
| 12 | 012_comissoes.sql | Comissões |
| 13 | 013_fechamento_mensal.sql | Fechamento mensal |
| 14 | 014_os_funcionario.sql | Vínculo OS/funcionário |
| 15 | 015_movimentacoes_caixa.sql | Movimentações caixa |
| 16 | 016_m1_ajustes.sql | Endereço tutor + microchip + plano saúde |
| 17 | 017_crmv_funcionario.sql | CRMV funcionários |
| 18 | 018_receituario_campos.sql | Registro MAPA + código pet |
| 19 | 019_vias_administracao.sql | Vias de administração |
| 20 | 020_bot_whatsapp.sql | Bot WhatsApp |
| 21 | 021_movimentacao_bancaria.sql | Contas bancárias e movimentações |
| 22 | 022_fornecedores_compras.sql | Fornecedores e pedidos de compra |
| 23 | 023_condicoes_recebimento.sql | Condições pagamento + recebimento mercadoria |
| 24 | 024_codigo_cadastros.sql | Campo `codigo` em produtos/fornecedores/funcionários |
| 25 | 025_seed_mensagens.sql | Templates padrão mensagens automáticas |
| 26 | 026_contas_financeiro.sql | Tabela `contas` (financeiro a pagar/receber) |
| 27 | 027_metas_faturamento.sql | Tabela `metas_faturamento` |

---

## Módulos Implementados ✅

### Clínica
- Auth, multi-tenant, JWT
- Tutores (endereço desmembrado, código, aniversário), Pets (microchip, plano saúde, idade, código)
- Prontuário, Vacinas
- Agenda + Self-service (link WhatsApp + bot chatbot via webhook Z-API)
- OS Kanban, Atendimentos
- Receituário PDF (QuestPDF — logo, 3 colunas, tipo, via, farmácia) + WhatsApp + reimpressão

### Vendas
- PDV/Caixa (retirada, depósito, fechamento)
- Serviços, Estoque (código, sugestão de compra)

### Compras (M4)
- Fornecedores (com código de controle)
- Condições de pagamento cadastráveis (parcelas + intervalo)
- Pedidos de compra → ao confirmar gera títulos a pagar no financeiro automaticamente
- Recebimento de Mercadoria: conferência física → alimenta estoque

### Financeiro (M2)
- Tabela `contas` (a pagar/receber, baixa, estorno) — migration 026
- Movimentação bancária (contas bancárias, entradas, saídas, conciliação)
- Histórico por cliente

### RH (M3)
- Funcionários (CRMV, Registro MAPA, código)
- Comissões, fechamento mensal, relatórios

### Marketing
- Mensagens automáticas WhatsApp (7 gatilhos com templates pré-cadastrados via migration 025)
- Promoções/Campanhas

### Cadastros
- Usuários, Vias de administração, Condições de pagamento, Fornecedores

### Configurações
- Parâmetros: logo upload, branding, toggles comissão OS/PDV
- Bot WhatsApp: config + mensagens editáveis + log

### Dashboard BI
- Seletor mês/ano com auto-refresh a cada 5 min
- KPIs: vendas, meta, agendamentos, resultado
- Gráfico Metas x Realizado (ComposedChart — barras + linha %)
- Gráfico pizza: agendamentos por status
- Próximos agendamentos do dia
- Aniversariantes do dia (pets e tutores) com cards animados
- Alertas: contas vencidas, estoque em falta, agendamentos pendentes

### Layout Responsivo
- Desktop: sidebar fixa
- Mobile/Tablet: topbar + drawer com menu hamburguer
- Fecha automaticamente ao navegar

### Gestão à Vista (TV Recepção)
- Implementado (GestaoVista.jsx + GestaoVistaController.cs)
- STATUS: com ERRO nos loops das notícias/animação — pendente validação

---

## BACKLOG — O que falta

### Bugs Conhecidos
- [ ] **Movimentação bancária não atualizada na baixa** — ao dar baixa em contas a pagar/receber, o saldo da conta bancária não é atualizado automaticamente. Investigar `FinanceiroController` método de baixa — deve criar `MovimentacaoBancaria` ao dar baixa.
- [ ] **Gestão à Vista** — erro nos loops das notícias/animação. Precisa validar e corrigir.

### Funcionalidades Pendentes
- [ ] M5 — Planos de saúde (cadastro e vínculo com pet)
- [ ] M7 — Metas de faturamento: tela de gestão de metas por mês (já tem tabela e endpoint)
- [ ] M7 — Dashboard gerencial com custo vs receita completo
- [ ] Reconectar webhook Vercel ao GitHub (atualmente deploy é manual via CLI)

### Backlog Técnico / SaaS
- [ ] Assinatura digital no receituário (gov.br/ITI ou hash + QR code)
- [ ] Migrar bot WhatsApp para Meta Cloud API (substituir Z-API em produção)
- [ ] Storage de imagens (Cloudflare R2 ou Supabase) — logo e fotos de pets
- [ ] Integração Asaas (PIX/boleto) + suspensão automática por inadimplência
- [ ] Self-service cadastro de tenant (teste grátis 7 dias)

---

## Estrutura de Arquivos Relevantes
```
C:\Projetos\vetclinica\
  VetClinica.API\
    Controllers\   (32 controllers)
    Models\        Entities.cs — todas as entities
    DTOs\          Dtos.cs — todos os DTOs/records
    Data\          AppDbContext.cs — DbSets e OnModelCreating
    Services\      ReceituarioPdfService.cs, BotWhatsAppService.cs, etc.
    Middleware\    TenantMiddleware.cs — injeta TenantContext
  frontend\
    src\
      pages\       (31 páginas)
      components\  Layout.jsx, Logo.jsx
      api\         client.js — axios com JWT
  database\        (27 migrations .sql)
  projeto.md       este arquivo
```

## Páginas Existentes
AdminLogin, AdminPanel, Agenda, AgendarPublico, Atendimentos, Bancario,
Cadastros/Vias, Compras, CondicoesPagamento, Dashboard, Estoque, Fechamento,
Financeiro, Fornecedores, GestaoVista, Login, Mensagens, PDV, Parametros,
Parametros/Bot, PetDetalhe, Pets, Promocoes, RH/Fechamento, RH/Funcionarios,
RH/RelatoriosRH, Recebimentos, Relatorios, Servicos, Tutores, Usuarios
