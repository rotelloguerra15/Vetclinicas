# VetClinica — Contexto do Projeto

## Stack
- Backend: .NET 8 + EF Core + PostgreSQL
- Frontend: React + Vite + Tailwind
- Hospedagem: Railway (backend + banco) + Vercel (frontend)

## URLs de Produção
- Frontend: https://vetclinicas.vercel.app
- Backend:  https://vetclinicas-production.up.railway.app
- Webhook bot: https://vetclinicas-production.up.railway.app/api/webhook/whatsapp/{tenantId}

## Portas locais
| Serviço   | Porta |
|-----------|-------|
| Frontend  | 5173  |
| Backend   | 5010  |
| PostgreSQL| 5433  |

## Repositório
https://github.com/rotelloguerra15/Vetclinicas.git

## Banco de dados (Railway)
- Host público: gondola.proxy.rlwy.net:16524
- Host interno: postgres.railway.internal:5432
- Database: railway / User: postgres
- String local: Host=gondola.proxy.rlwy.net;Port=16524;Database=railway;Username=postgres;Password=eOpnNTdorWCkkphKLRlNogUWOtiwGibS

## Tenant principal
- ID: 4b19602f-0425-4101-914c-5b9aeaf5d4e7
- Nome: Pet Shop e Consultorio Dra. Barbara Fonseca
- Login: barbaratatschveterinaria@gmail.com / admin123

## Docker local
- Container: vetclinica-db (porta 5433)
- Migration: Get-Content "C:\Projetos\vetclinica\database\ARQUIVO.sql" | docker exec -i vetclinica-db psql -U postgres -d vetclinica

## Migration no Railway
```powershell
$DB = "postgresql://postgres:eOpnNTdorWCkkphKLRlNogUWOtiwGibS@gondola.proxy.rlwy.net:16524/railway"
psql $DB -f "C:\Projetos\vetclinica\database\ARQUIVO.sql"
```

## Padrões do projeto
- Multi-tenant: coluna `tenant_id` em todas as tabelas
- Auth: JWT via TenantContext (_t.TenantId, _t.UserId, _t.Papel)
- ORM: Entity Framework Core — NUNCA usar Dapper
- Frontend: axios em src/api/client.js
- Rotello prefere arquivos completos prontos para substituir
- NUNCA usar emojis dentro de strings C# interpoladas (CS1010/CS1056)
- DbSet nomes: OrdensServico, Vendas, Tutores, Pets, Funcionarios, Contas, etc.
- Venda tem TutorId direto (não PetId)

---

## Migrations aplicadas
| # | Arquivo | Descrição |
|---|---------|-----------|
| 01 | 01-schema.sql | Schema base |
| 02 | 02-estoque-mensagens.sql | Estoque e mensagens |
| 03 | 03-aniversario-tutor.sql | Aniversário tutor |
| 04 | 04-branding-fonseca.sql | Branding |
| 05 | 05-plataforma-saas.sql | Plataforma SaaS |
| 06 | 06-promocoes.sql | Promoções |
| 07 | 07-fix-enums-to-varchar.sql | Fix enums |
| 08 | 08-prontuario-campos.sql | Prontuário |
| 09 | 09-caixa.sql | Caixa |
| 10 | 010_funcionarios.sql | Funcionários |
| 11 | 011_parametros_sistema.sql | Parâmetros |
| 12 | 012_comissoes.sql | Comissões |
| 13 | 013_fechamento_mensal.sql | Fechamento mensal |
| 14 | 014_os_funcionario.sql | OS funcionário |
| 15 | 015_movimentacoes_caixa.sql | Caixa movimentações |
| 16 | 016_m1_ajustes.sql | Endereço tutor + microchip + plano saúde |
| 17 | 017_crmv_funcionario.sql | CRMV funcionários |
| 18 | 018_receituario_campos.sql | Registro MAPA + código pet |
| 19 | 019_vias_administracao.sql | Vias de administração |
| 20 | 020_bot_whatsapp.sql | Bot WhatsApp |
| 21 | 021_movimentacao_bancaria.sql | Contas bancárias e movimentações |

---

## Módulos implementados ✅

- Auth, multi-tenant, JWT
- Tutores (endereço desmembrado), Pets (microchip, plano saúde, idade)
- Prontuário, Vacinas
- Agenda + Self-service (link + bot WhatsApp chatbot)
- OS Kanban, PDV, Estoque, Promoções
- Receituário PDF (logo, 3 colunas, tipo, via, farmácia) + WhatsApp + reimpressão
- M2 Financeiro completo (contas a pagar/receber, baixa, estorno, dashboard, fluxo de caixa)
- M2 complementos: Movimentação bancária + histórico por cliente ✅
- M3 RH (funcionários CRMV/MAPA, comissões, fechamento)
- PDV/Caixa (retirada, depósito, fechamento, matemática)
- Mensagens WhatsApp automáticas
- Painel super-admin, white-label
- Cadastros auxiliares: Vias de administração
- Parâmetros: logo upload, branding, toggles comissão OS/PDV, Bot WhatsApp
- Bot WhatsApp: chatbot agendamento via webhook Z-API, máquina de estados, log
- Deploy: Railway + Vercel ✅

---

## ORDEM DE EXECUÇÃO

### Próximos passos:
1. **M4 Compras** — Fornecedores, pedido de compra → estoque, uso interno vs venda ← **PRÓXIMO**
2. **M5 Cadastros auxiliares** — Fornecedores, planos de saúde, demais
3. **Testar bot WhatsApp** — configurar Z-API com URL pública
4. **M6 Gestão à vista** — TV recepção: pet em atendimento, animação, fullscreen
5. **M7 Metas** — Meta faturamento vs realizado, dashboard gerencial

---

## BACKLOG

### M1 ✅ CONCLUÍDO
- [x] Self-service agendamento WhatsApp
- [x] Endereço tutor desmembrado
- [x] Pet: microchip, plano saúde, idade calculada
- [x] Receituário PDF + WhatsApp + reimpressão
- [x] Renomear Equipe → Usuários
- [x] Cadastro de Vias de administração
- [x] Parâmetros: toggles comissão OS/PDV

### M2 ✅ CONCLUÍDO
- [x] Contas a pagar e receber
- [x] Baixa com data pagamento/recebimento
- [x] Vinculação OS → PDV → financeiro
- [x] Movimentação bancária (contas, entradas, saídas, transferências, conciliação)
- [x] Histórico por cliente

### M3 ✅ CONCLUÍDO
- [x] Funcionários (CRMV, Registro MAPA)
- [x] Comissões e fechamento mensal

### M4 — Compras e fornecedores
- [ ] Cadastro de fornecedores
- [ ] Pedido de compra → ao finalizar alimenta o estoque
- [ ] Classificação: venda ou uso interno
- [ ] Composição do custo mensal

### M5 — Cadastros auxiliares
- [x] Vias de administração
- [ ] Planos de saúde
- [ ] Demais cadastros de apoio

### M6 — Gestão à vista (TV recepção)
- [ ] Tela animada: pet, serviço, status
- [ ] Animação cachorro/gato
- [ ] Fullscreen, auto-refresh

### M7 — Metas e relatórios
- [ ] Meta faturamento vs realizado
- [ ] Custo mensal vs receita
- [ ] Dashboard gerencial

---

## Backlog técnico
- [ ] Assinatura digital no receituário (gov.br/ITI ou hash + QR code)
- [ ] Migrar bot para Meta Cloud API em produção
- [ ] Storage de imagens (Cloudflare R2 ou Supabase)
- [ ] Integração Asaas (PIX/boleto) + suspensão por inadimplência
- [ ] Self-service cadastro tenant (teste grátis 7 dias)
