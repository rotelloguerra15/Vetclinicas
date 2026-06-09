# VetClinica — Contexto do Projeto

## Stack
- Backend: .NET 8 + EF Core + PostgreSQL
- Frontend: React + Vite + Tailwind + Recharts
- Hospedagem: Railway (backend + banco) + Vercel (frontend via CLI)

## URLs de Producao
- Frontend: https://vetclinicas.vercel.app
- Backend:  https://vetclinicas-production.up.railway.app
- Webhook bot: https://vetclinicas-production.up.railway.app/api/webhook/whatsapp/{tenantId}

## Deploy manual (Vercel nao faz auto-deploy - webhook quebrado)
```powershell
cd C:\Projetos\vetclinica\frontend
# Garante .env.production com URL correta
echo "VITE_API_URL=https://vetclinicas-production.up.railway.app" > .env.production
npm run build
cd C:\Projetos\vetclinica
Copy-Item -Recurse -Force "frontend\dist\*" ".vercel\output\static\"
vercel deploy --prod --yes --prebuilt
```

## Portas locais
| Servico   | Porta |
|-----------|-------|
| Frontend  | 5173  |
| Backend   | 5010  |
| PostgreSQL| 5433  |

## Repositorio
https://github.com/rotelloguerra15/Vetclinicas.git

## Banco Railway
- Host publico: gondola.proxy.rlwy.net:16524
- Database: railway / User: postgres
- Password: eOpnNTdorWCkkphKLRlNogUWOtiwGibS
- String: postgresql://postgres:eOpnNTdorWCkkphKLRlNogUWOtiwGibS@gondola.proxy.rlwy.net:16524/railway

## Tenant principal
- ID: 4b19602f-0425-4101-914c-5b9aeaf5d4e7
- Nome: Pet Shop e Consultorio Dra. Barbara Fonseca
- Login: barbaratatschveterinaria@gmail.com / admin123

## Padroes CRITICOS do projeto
- Multi-tenant: coluna tenant_id em TODAS as tabelas
- Auth: JWT via TenantContext (_t.TenantId, _t.UserId, _t.Papel)
- ORM: EF Core — NUNCA usar Dapper
- NUNCA emojis dentro de strings C# interpoladas (CS1010/CS1056)
- FK violations: sempre SaveChanges() no pai ANTES de adicionar filhos
- DbSet corretos: OrdensServico, Vendas, Tutores, Pets, Contas, PedidosCompra, etc.
- Venda.TutorId (nao PetId)
- Tabela financeira: "contas" (nao "lancamentos" - lancamentos e schema antigo)
- PedidoItem FK: configurar HasForeignKey no OnModelCreating
- Arquivos entregues sempre com nome final correto (sem _cur, _fix, etc.)

---

## Migrations aplicadas (26 total)
| # | Arquivo | Descricao |
|---|---------|-----------|
| 01 | 01-schema.sql | Schema base (inclui tabela lancamentos legada) |
| 02 | 02-estoque-mensagens.sql | Estoque, mensagens, vendas, triggers |
| 03-09 | ... | Ajustes diversos |
| 10 | 010_funcionarios.sql | Funcionarios RH |
| 11 | 011_parametros_sistema.sql | Parametros |
| 12 | 012_comissoes.sql | Comissoes |
| 13 | 013_fechamento_mensal.sql | Fechamento |
| 14 | 014_os_funcionario.sql | OS funcionario |
| 15 | 015_movimentacoes_caixa.sql | Caixa |
| 16 | 016_m1_ajustes.sql | Endereco tutor + microchip + plano saude |
| 17 | 017_crmv_funcionario.sql | CRMV funcionarios |
| 18 | 018_receituario_campos.sql | Registro MAPA + codigo pet |
| 19 | 019_vias_administracao.sql | Vias de administracao |
| 20 | 020_bot_whatsapp.sql | Bot WhatsApp (config, conversas, logs) |
| 21 | 021_movimentacao_bancaria.sql | Contas bancarias e movimentacoes |
| 22 | 022_fornecedores_compras.sql | Fornecedores e pedidos de compra |
| 23 | 023_condicoes_recebimento.sql | Condicoes pagamento + recebimento mercadoria |
| 24 | 024_codigo_cadastros.sql | Campo codigo em produtos/fornecedores/funcionarios |
| 25 | 025_seed_mensagens.sql | Templates padrao de mensagens automaticas |
| 26 | 026_contas_financeiro.sql | Tabela contas (financeiro a pagar/receber) |

---

## Modulos implementados

### CLINICA
- Auth, multi-tenant, JWT
- Tutores (endereco desmembrado, codigo), Pets (microchip, plano saude, idade)
- Prontuario, Vacinas
- Agenda + Self-service (link WhatsApp + bot chatbot)
- OS Kanban, Atendimentos
- Receituario PDF (logo, 3 colunas, tipo, via, farmacia) + WhatsApp + reimpressao

### VENDAS
- PDV/Caixa (retirada, deposito, fechamento)
- Servicos e valores
- Estoque com controle, codigo, sugestao de compra

### COMPRAS (M4)
- Fornecedores (com codigo de controle)
- Condicoes de pagamento cadastraveis (parcelas + intervalo)
- Pedidos de compra: ao confirmar gera titulos a pagar no financeiro automaticamente
- Recebimento de Mercadoria: conferencia fisica -> alimenta estoque

### FINANCEIRO (M2)
- Tabela: contas (a pagar/receber, baixa, estorno) -- migration 026
- Movimentacao bancaria (contas bancarias, entradas, saidas, conciliacao)
- Historico por cliente

### RH (M3)
- Funcionarios (CRMV, Registro MAPA, codigo)
- Comissoes, fechamento mensal, relatorios

### MARKETING
- Mensagens automaticas WhatsApp (7 gatilhos com templates)
- Promocoes/Campanhas

### CADASTROS
- Usuarios, Vias de administracao, Condicoes de pagamento, Fornecedores

### CONFIGURACOES
- Parametros: logo, branding, toggles comissao OS/PDV
- Bot WhatsApp: config + mensagens editaveis + log detalhado

### GESTAO A VISTA
- Tela para TV da recepcao (GestaoVista.jsx + GestaoVistaController.cs)
- STATUS: implementado mas com ERRO nos loops das noticias - precisa validar

### DASHBOARD
- KPIs: vendas, receita, OS, agendamentos, alertas
- Grafico de barras: vendas 6 meses (Recharts)
- Grafico pizza: agendamentos por status
- Proximos agendamentos do dia
- Indicadores rapidos + resultado do mes

---

## BACKLOG — O que falta

### Bugs conhecidos
- [ ] **Movimentacao bancaria nao alimentada na baixa de contas** — ao dar baixa em contas a pagar/receber, o saldo da conta bancaria nao e atualizado automaticamente. O saldo superior nao reflete o pagamento. Investigar FinanceiroController metodo de baixa.
- [ ] **M6 Gestao a Vista** — implementado mas com erro nos loops das noticias/animacao. Precisa validar e corrigir.

### Funcionalidades pendentes
- [ ] M5 Planos de saude (cadastro e vinculo com pet)
- [ ] M7 Metas de faturamento vs realizado por mes
- [ ] M7 Dashboard gerencial com custo vs receita

### Tecnico / SaaS
- [ ] Reconectar webhook Vercel ao GitHub (atualmente deploy e manual via CLI)
- [ ] Assinatura digital no receituario (gov.br/ITI ou hash + QR code)
- [ ] Migrar bot para Meta Cloud API em producao
- [ ] Storage de imagens (Cloudflare R2 ou Supabase)
- [ ] Integracao Asaas (PIX/boleto) + suspensao por inadimplencia
- [ ] Self-service cadastro tenant (teste gratis 7 dias)
