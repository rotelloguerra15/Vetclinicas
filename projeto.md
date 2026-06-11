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
9. **DbSets corretos:** `OrdensServico`, `Vendas`, `Tutores`, `Pets`, `Contas`, `PedidosCompra`, `MetasFaturamento`, `Pelagens`, `Cargos`, `ConciliacoesDiarias`
10. **Arquivos:** sempre entregar com nome final correto (sem `_cur`, `_fix`, `_v2` etc.)
11. **Railway cache:** quando `COPY VetClinica.API/` aparece como `cached`, forçar com `Add-Content` em qualquer arquivo + commit
12. **using:** sempre incluir `using VetClinica.API.Models;` em controllers que usam entities
13. **PowerShell:** NÃO usar `&&` — separar comandos em linhas individuais
14. **SQL com emojis:** NÃO colocar emojis em arquivos .sql — encoding WIN1252 quebra no psql Windows
15. **iText7 assinatura:** NÃO tentar integrar iText7+BouncyCastle para assinatura PDF sem testar localmente primeiro — causa erros de compilação em cascata

---

## Migrations Aplicadas (35 total)
| # | Arquivo | Descrição |
|---|---------|-----------||
| 01 | 01-schema.sql | Schema base |
| 02 | 02-estoque-mensagens.sql | Estoque, mensagens, vendas |
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
| 28 | 028_conciliacao_bancaria.sql | Tabela `conciliacoes_diarias` + campo `origem` em movimentacoes_bancarias |
| 29 | 029_ia_diagnostico.sql | Campos `anthropic_api_key` e `ia_ativo` em parametros_sistema |
| 30 | 030_assina_receituario.sql | Campo `assina_receituario` em funcionarios |
| 31 | 031_cargos_funcionario_usuario.sql | Tabela `cargos` + `cargo_id` e `usuario_id` em funcionarios |
| 32 | 032_agendamento_servico.sql | Campo `servico_id` em agendamentos |
| 33 | 033_servico_icone.sql | Campo `icone` em servicos |
| 34 | 034_pelagens.sql | Tabela `pelagens` (cadastro de pelagens de pets) |
| 35 | 035_certificado_digital.sql | Campos certificado digital A1 em parametros_sistema |

---

## Módulos Implementados

### Clínica
- Auth, multi-tenant, JWT
- Tutores (endereço desmembrado, código, aniversário, painel de pets vinculados)
- Pets (microchip, plano saúde, idade estimada, código, pelagem via cadastro)
- Prontuário, Vacinas
- **Agenda** — grade de horários do dia, iniciar atendimento, botão por status, vago/ocupado
- OS Kanban, Atendimentos (fluxo: Agenda → iniciar → PetDetalhe → receituário → encerra OS → PDV)
- **Receituário PDF** — QR Code de validação + código único `REC-2026-XXXXXXXX` no rodapé
- **Validação eletrônica** — `/validar/:codigo` página pública sem login

### Vendas
- PDV/Caixa (retirada, depósito, fechamento → gera contas por forma de pagamento + MovimentacaoBancaria)
- Serviços (com ícone emoji, categorias, picker visual)
- Estoque (código, sugestão de compra)

### Compras (M4)
- Fornecedores, Condições de pagamento, Pedidos de compra, Recebimento de Mercadoria

### Financeiro (M2)
- Contas a pagar/receber (baixa gera MovimentacaoBancaria automática)
- Movimentação bancária + Conciliação Diária (fechamento por conta/data)
- Categorias financeiras (cadastro separado)
- Histórico por cliente

### RH (M3)
- **Cargos** — cadastro com toggle `pode_receituario`
- **Funcionários** — vínculo com usuário do sistema (`usuario_id`), cargo (`cargo_id`)
- Comissões, fechamento mensal, relatórios

### IA Diagnóstico
- Assistente no receituário (bloco roxo 🤖)
- Campo de texto + microfone (Web Speech API)
- Chama Claude API (chave por tenant em Parâmetros)
- Retorna diagnósticos diferenciais, exames, conduta
- Botão "Usar como Diagnóstico" preenche o campo automaticamente

### Certificado Digital
- Upload de `.pfx` A1 em Parâmetros → Assinatura Digital
- Criptografia AES-256 da chave + senha no banco
- Interface plugável `ICertificadoService` para futura integração A3 nuvem
- **STATUS:** assinatura visual PDF pendente (backlog — validar com certificado real da Dra. Barbara)

### Marketing
- Mensagens automáticas WhatsApp, Promoções/Campanhas

### Cadastros
- Usuários, Vias de administração, Pelagens, Cargos, Condições de pagamento

### Configurações/Parâmetros
- Logo, branding, comissões, bot WhatsApp
- IA (chave Anthropic por tenant)
- Certificado Digital A1

### Dashboard BI / Relatórios
- KPIs, gráficos Recharts (evolução 12 meses, pizza por categoria, barras por dia)
- **4 abas:** Visão Geral, Rankings (top serviços/produtos), Custos por categoria, Metas
- Metas editáveis inline por mês, gauge radial, gráfico anual meta vs realizado

### Gestão à Vista (TV)
- Colunas: Agendados | Aguardando | Em Atendimento | Pronto | Stats
- Bug loops notícias/animação — **CORRIGIDO** (useRef para cache evita loop infinito)

---

## BACKLOG

### Bugs Pendentes
- [ ] **Validar assinatura digital** — aguarda Dra. Barbara testar com `.pfx` real
- [ ] **Gestão à Vista** — validar correção dos loops em produção

### Funcionalidades Pendentes
- [ ] M5 — Planos de saúde (cadastro e vínculo com pet)
- [ ] M7 — Tela de gestão de metas por mês (backend pronto, falta frontend)
- [ ] Webhook Vercel → GitHub (deploy ainda é manual)
- [ ] **Assinatura PDF visual** — iText7 + BouncyCastle (testar localmente antes de commitar)
- [ ] **Tempo de produtividade** — homem-hora por atendimento (dados já coletados: OS.Inicio/Fim)

### Backlog Técnico
- [ ] WhatsApp Meta Cloud API (substituir Z-API)
- [ ] Storage de imagens (Cloudflare R2 ou Supabase)
- [ ] Integração Asaas (PIX/boleto) + suspensão automática
- [ ] Self-service cadastro de tenant (teste grátis 7 dias)
- [ ] Certificado A3 nuvem (Safeweb Cloud API) — interface já plugável

---

## Estrutura de Arquivos Relevantes
```
C:\Projetos\vetclinica\
  VetClinica.API\
    Controllers\   (40+ controllers)
    Models\        Entities.cs — todas as entities
    DTOs\          Dtos.cs — todos os DTOs/records
    Data\          AppDbContext.cs — DbSets e OnModelCreating
    Services\
      ReceituarioPdfService.cs
      BotWhatsAppService.cs
      Certificado\
        ICertificadoService.cs
        PfxCertificadoService.cs
        NullCertificadoService.cs
        CertificadoCryptoHelper.cs
  frontend\
    src\
      pages\       (40+ páginas)
      components\  Layout.jsx, Logo.jsx
      api\         client.js — axios com JWT
  database\        (35 migrations .sql)
  projeto.md       este arquivo
```

## Novos Controllers (adicionados nesta sessão)
- `IaController.cs` — `/api/ia/status`, `/api/ia/diagnostico`, `/api/ia/configurar`
- `ValidacaoController.cs` — `/api/validar/{codigo}` (público, sem auth)
- `CertificadoController.cs` — `/api/certificado/status`, `/upload-a1`, `/ativar`
- `CargosController.cs` — `/api/cargos` CRUD
- `PelagensController.cs` — `/api/cadastros/pelagens` CRUD
- `RelatoriosController.cs` — endpoints de BI (top-servicos, custos-categoria, evolucao, metas)

## Novas Páginas Frontend (adicionadas nesta sessão)
- `Validar.jsx` — página pública `/validar/:codigo`
- `Categorias.jsx` — cadastro de categorias financeiras
- `Cargos.jsx` — cadastro de cargos com toggle pode_receituario
- `Pelagens.jsx` — cadastro de pelagens
- `Relatorios.jsx` — BI completo com 4 abas e pet art SVG
