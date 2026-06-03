# VetClinica — Contexto do Projeto

## Portas
| Serviço   | Porta  |
|-----------|--------|
| Frontend  | 5173   |
| Backend   | 5010   |
| PostgreSQL| 5433   |

## Docker
- Container PostgreSQL: `vetclinica-db`
- Rodar migration: `Get-Content "C:\Projetos\vetclinica\database\ARQUIVO.sql" | docker exec -i vetclinica-db psql -U postgres -d vetclinica`

## Estrutura
```
C:\Projetos\vetclinica\
  VetClinica.API\        → backend .NET 8
  frontend\              → React + Vite
  database\              → migrations SQL
```

## Credenciais
- Login clínica: admin@clinicafonseca.com.br / admin123
- Plataforma: rotello.guerra@ketra.com.br / Ketra@2026
- Banco: Host=localhost;Port=5433;Database=vetclinica;Username=postgres;Password=postgres

## Migrations aplicadas
| # | Arquivo | Descrição |
|---|---------|-----------| 
| 01 | 01-schema.sql | Schema base |
| 02 | 02-estoque-mensagens.sql | Estoque e mensagens |
| 03 | 03-aniversario-tutor.sql | Aniversário tutor |
| 04 | 04-branding-fonseca.sql | Branding Clínica Fonseca |
| 05 | 05-plataforma-saas.sql | Plataforma SaaS |
| 06 | 06-promocoes.sql | Promoções |
| 07 | 07-fix-enums-to-varchar.sql | Fix enums |
| 08 | 08-prontuario-campos.sql | Campos prontuário |
| 09 | 09-caixa.sql | Caixa |
| 10 | 010_funcionarios.sql | Funcionários (RH) |
| 11 | 011_parametros_sistema.sql | Parâmetros do sistema |
| 12 | 012_comissoes.sql | Comissões |
| 13 | 013_fechamento_mensal.sql | Fechamento mensal |
| 14 | 014_os_funcionario.sql | Vínculo funcionário na OS |
| 15 | 015_movimentacoes_caixa.sql | Retiradas e depósitos do caixa |
| 16 | 016_m1_ajustes.sql | Endereço tutor + microchip + plano saúde pet |
| 17 | 017_crmv_funcionario.sql | CRMV em funcionários |
| 18 | 018_receituario_campos.sql | Registro MAPA + código sequencial pet |
| 19 | 019_vias_administracao.sql | Cadastro de vias de administração |
| 20 | 020_bot_whatsapp.sql | Bot WhatsApp (config, conversas, logs) |

## Padrão do projeto
- Multi-tenant: coluna `tenant_id` em todas as tabelas
- Auth: JWT — `_t.TenantId` e `_t.UserId` via `TenantContext`
- ORM: Entity Framework Core (AppDbContext) — NÃO usar Dapper
- Frontend: chamadas via `api` (axios) em `src/api/client.js`
- Rotello prefere receber **arquivos completos prontos para substituir**, nunca trechos para colar
- Nunca usar emojis dentro de strings C# interpoladas (causa erro de compilação)

---

## Módulos implementados ✅

- Auth, Tutores (endereço desmembrado), Pets (microchip, plano saúde, idade calculada), Prontuário
- Agenda + Self-service agendamento (link WhatsApp + bot chatbot)
- OS Kanban, PDV, Estoque
- **M2 Financeiro** — Contas a pagar/receber, baixa, estorno, dashboard, fluxo de caixa ✅
- Mensagens WhatsApp automáticas, Promoções
- Painel super-admin, white-label
- **M3 RH** — Funcionários (CRMV + Registro MAPA), Comissões, Fechamento ✅
- **PDV/Caixa** — Retirada, Depósito, Fechamento ✅
- **Receituário** — PDF fiel ao modelo (logo, 3 colunas, tipo receita, via, farmácia), WhatsApp, reimpressão ✅
- **Cadastros auxiliares** — Vias de administração ✅
- **Parâmetros** — Logo upload, branding, Bot WhatsApp (config + mensagens + log) ✅
- **Bot WhatsApp** — chatbot de agendamento via webhook Z-API, máquina de estados ✅

---

## ORDEM DE EXECUÇÃO DEFINIDA

### Próximos passos (em ordem):

1. **Testar o bot** — configurar Z-API, apontar webhook, testar agendamento real pelo WhatsApp
2. **Deploy** — Railway (backend .NET + PostgreSQL) + Vercel (frontend). Webhook precisa de URL pública.
3. **M1 item 8** — Parâmetros: toggles de comissão OS/PDV
4. **M2 complementos** — Movimentação bancária + histórico por cliente
5. **M4 Compras** — Fornecedores, pedido compra → estoque, uso interno vs venda
6. **M5 Cadastros auxiliares** — Fornecedores, planos de saúde, demais cadastros
7. **M6 Gestão à vista** — TV recepção: pet em atendimento, animação, fullscreen, auto-refresh
8. **M7 Metas** — Meta faturamento vs realizado, custo mensal vs receita, dashboard gerencial

---

## BACKLOG COMPLETO

### M1 — Correções e ajustes
- [x] Self-service de agendamento via WhatsApp ✅
- [x] Endereço do tutor desmembrado ✅
- [x] Pet: idade calculada, microchip, plano de saúde ✅
- [x] Receituário PDF + WhatsApp + reimpressão ✅
- [x] Renomear menu "Equipe" → "Usuários" ✅
- [ ] Parâmetros — toggles comissão OS/PDV ← **PRÓXIMO após deploy**

### M2 — Financeiro (complementos)
- [x] Contas a pagar e receber ✅
- [x] Baixa com data de pagamento/recebimento ✅
- [x] Vinculação OS → PDV → financeiro ✅
- [ ] Movimentação bancária
- [ ] Histórico por cliente (quem pagou, quanto, qual serviço/produto)

### M3 — Gestão de pessoas (complementos)
- [x] Cadastro de funcionários (CRMV, Registro MAPA) ✅
- [x] Fechamento mensal ✅
- [ ] Fechamento mensal incluindo impostos

### M4 — Compras e fornecedores
- [ ] Cadastro de fornecedores
- [ ] Pedido de compra → ao finalizar alimenta o estoque
- [ ] Classificação: produto para venda ou uso interno
- [ ] Composição do custo mensal da clínica

### M5 — Cadastros auxiliares
- [x] Vias de administração ✅
- [ ] Fornecedores
- [ ] Planos de saúde
- [ ] Demais cadastros de apoio

### M6 — Gestão à vista (TV da recepção)
- [ ] Tela animada mostrando pet, serviço em execução e status
- [ ] Animação de cachorro/gato passando
- [ ] Fullscreen, auto-refresh

### M7 — Metas e relatórios
- [ ] Meta de faturamento por mês vs realizado
- [ ] Custo mensal vs receita
- [ ] Dashboard gerencial consolidado

---

## Backlog técnico / Roadmap SaaS

- [ ] Assinatura digital no receituário (avaliar gov.br/ITI ou hash + QR code)
- [ ] Migrar bot WhatsApp para Meta Cloud API (quando em produção)
- [ ] Storage de imagens (logo + fotos pets) — Cloudflare R2 ou Supabase
- [ ] Integração de cobrança (Asaas — PIX/boleto) + suspensão automática por inadimplência
- [ ] Self-service de cadastro (teste grátis 7 dias)
- [ ] Subdomínio por clínica (`fonseca.seusistema.com.br`)

---

## Notas importantes para retomada

- **Bot WhatsApp**: webhook configurado em `POST /api/webhook/whatsapp/{tenantId}`. Configurar na Z-API em "Webhook de mensagens recebidas". URL precisa ser pública (deploy primeiro).
- **Receituário**: usa QuestPDF 2024.3.5. Logo salva como base64 data-URL no banco (campo `logo_url` da tabela `tenants`). Upload em Parâmetros.
- **AgendaService**: atualizado para respeitar duração do serviço e ler horários do `bot_config`.
- **Emojis em C#**: NUNCA usar emojis dentro de strings interpoladas `$"..."` em C# — causa erro CS1010/CS1056. Usar texto simples.
