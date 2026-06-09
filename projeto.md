# VetClinica — Contexto do Projeto

## Stack
- Backend: .NET 8 + EF Core + PostgreSQL
- Frontend: React + Vite + Tailwind
- Hospedagem: Railway (backend + banco) + Vercel (frontend)

## URLs de Producao
- Frontend: https://vetclinicas.vercel.app
- Backend:  https://vetclinicas-production.up.railway.app
- Webhook bot: https://vetclinicas-production.up.railway.app/api/webhook/whatsapp/{tenantId}

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
- Host interno: postgres.railway.internal:5432
- Database: railway / User: postgres
- Password: eOpnNTdorWCkkphKLRlNogUWOtiwGibS

## Tenant principal
- ID: 4b19602f-0425-4101-914c-5b9aeaf5d4e7
- Nome: Pet Shop e Consultorio Dra. Barbara Fonseca
- Login: barbaratatschveterinaria@gmail.com / admin123

## Padroes do projeto
- Multi-tenant: coluna tenant_id em todas as tabelas
- Auth: JWT via TenantContext (_t.TenantId, _t.UserId, _t.Papel)
- ORM: EF Core — NUNCA usar Dapper
- NUNCA usar emojis dentro de strings C# interpoladas
- DbSet nomes corretos: OrdensServico, Vendas, Tutores, Pets, Contas, PedidosCompra, etc.
- Venda tem TutorId direto (nao PetId)
- Arquivos completos prontos para substituir (nunca trechos)

---

## Migrations aplicadas
| # | Arquivo | Descricao |
|---|---------|-----------|
| 01 | 01-schema.sql | Schema base |
| 02 | 02-estoque-mensagens.sql | Estoque, mensagens, vendas |
| 03-09 | ... | Varios ajustes |
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
| 20 | 020_bot_whatsapp.sql | Bot WhatsApp |
| 21 | 021_movimentacao_bancaria.sql | Contas bancarias e movimentacoes |
| 22 | 022_fornecedores_compras.sql | Fornecedores e pedidos de compra |
| 23 | 023_condicoes_recebimento.sql | Condicoes pagamento + recebimento mercadoria |
| 24 | 024_codigo_cadastros.sql | Campo codigo em produtos/fornecedores/funcionarios |
| 25 | 025_seed_mensagens.sql | Templates padrao de mensagens automaticas |

---

## Modulos implementados

### CLINICA
- Auth, multi-tenant, JWT
- Tutores (endereco desmembrado), Pets (microchip, plano saude, idade calculada)
- Prontuario, Vacinas
- Agenda + Self-service (link WhatsApp + bot chatbot)
- OS Kanban, Atendimentos
- Receituario PDF (logo, 3 colunas, tipo, via, farmacia) + WhatsApp + reimpressao

### VENDAS
- PDV/Caixa (retirada, deposito, fechamento)
- Servicos e valores
- Estoque com controle e sugestao de compra [CAMPO CODIGO ADICIONADO]

### COMPRAS (M4)
- Fornecedores [COM CODIGO DE CONTROLE]
- Pedidos de compra com condicoes de pagamento
- Ao confirmar pedido: gera titulos a pagar no financeiro automaticamente
- Recebimento de Mercadoria: conferencia fisica -> alimenta estoque

### FINANCEIRO (M2)
- Contas a pagar/receber, baixa, estorno
- Movimentacao bancaria (contas, entradas, saidas, transferencias, conciliacao)
- Historico por cliente

### RH (M3)
- Funcionarios (CRMV, Registro MAPA) [COM CODIGO DE CONTROLE]
- Comissoes e fechamento mensal

### MARKETING
- Mensagens automaticas WhatsApp (7 gatilhos com templates pre-cadastrados)
- Promocoes

### CADASTROS
- Usuarios
- Vias de administracao
- Condicoes de pagamento

### CONFIGURACOES
- Parametros: logo upload, branding, toggles comissao OS/PDV
- Bot WhatsApp: config + mensagens editaveis + log

### INFRAESTRUTURA
- Deploy Railway + Vercel
- Bot WhatsApp chatbot de agendamento (webhook Z-API)
- Painel super-admin, white-label

---

## BACKLOG — O que falta

### Validacao pendente
- [ ] **M6 Gestao a Vista (TV recepcao)** — ja implementado em outro chat mas com ERRO nos loops das noticias. Precisa validar e corrigir animacao

### M5 — Cadastros auxiliares
- [ ] Planos de saude (cadastro e vinculo com pet)

### M7 — Metas e relatorios
- [ ] Meta de faturamento vs realizado por mes
- [ ] Custo mensal vs receita
- [ ] Dashboard gerencial consolidado

### Tecnico / SaaS
- [ ] Assinatura digital no receituario (gov.br/ITI ou hash + QR code)
- [ ] Migrar bot para Meta Cloud API em producao
- [ ] Storage de imagens (Cloudflare R2 ou Supabase) — logo e fotos pets
- [ ] Integracao Asaas (PIX/boleto) + suspensao por inadimplencia
- [ ] Self-service cadastro tenant (teste gratis 7 dias)
